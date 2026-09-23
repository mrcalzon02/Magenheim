"""Author the twelve elemental Surtlings as bone-bound segment bodies on one canonical skeleton.

    tools/blender.ps1 author-underworld-surtlings [model-id ...]
    tools/blender.ps1 export-model-assets <the same ids>

Design authority: docs/UNDERWORLD_ELEMENTAL_SURTLINGS_DESIGN.md -- two shared adult bodies
(feminine, masculine) and six elemental kits (Fire, Water, Earth, Wind, Radiance, Umbral), one
skeleton contract, at most two primary material families plus a localized glow. This is the design's
first production step, silhouette-proof models, carried far enough to be spawned and fought.

Runtime contract (src/Magenheim.Runtime/HumanoidSegmentBinder.cs). Every part is authored in the
canonical skeleton's space and named `bone:<HumanRole>/<part>`. The skeleton itself is written into
the payload as `rig`: for each humanoid role, its joint, the point its segment points toward, how a
donor finds that point, and whether the segment stretches to the donor's bone length. At
registration each part is retargeted onto the donor's matching bone and rides it, so the donor's own
animation, attacks, AI and ragdoll drive the authored body. Nothing is skinned; the elemental bodies
are built from masses, plates and currents, and overlapping joint masses cover the articulation.

Blender Z is up and the figure faces -Y; the exporter maps (x, y, z) to game (x, z, -y), so game
forward is +Z. Left-side roles sit at -X, which lands on the donor's left after that mapping.
"""
import bmesh
import bpy
import json
import math
import sys
from pathlib import Path

from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from magenheim_blender_kit import (  # noqa: E402
    BAKE_SIZE, X, Y, Z, _feed, _math, _mix, _noise, _ramp, bake_atlas, curve, diamond, finish, gem, loft,
    merge, ring, slab, srgb, sweep, unwrap, uv_overlap)

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
REVISION = 'surtling-silhouette-1'
FORWARD = Vector((0.0, -1.0, 0.0))
UP = Vector((0.0, 0.0, 1.0))

# ------------------------------------------------------------------------------------------------
# Canonical skeleton
# ------------------------------------------------------------------------------------------------

# role -> (joint, toward, end length, stretch). `toward` is a child role, `up`, `forward` or
# `continue:<parent role>`; end length is only used by the non-child rules. Only limbs stretch along
# their bone: torso, head, hands and feet keep their authored proportion, because short or variable
# donor bones (hips to spine) would otherwise squash them.
_LEFT = {
    'UpperArm': ((-0.190, 0.000, 1.440), 'LowerArm', None, True),
    'LowerArm': ((-0.250, 0.010, 1.170), 'Hand', None, True),
    'Hand': ((-0.280, -0.010, 0.930), 'continue:LowerArm', 0.180, False),
    'UpperLeg': ((-0.095, 0.000, 0.940), 'LowerLeg', None, True),
    'LowerLeg': ((-0.105, -0.010, 0.520), 'Foot', None, True),
    'Foot': ((-0.110, 0.020, 0.090), 'forward', 0.170, False),
}
RIG = {
    'Hips': ((0.0, 0.0, 0.980), 'Spine', None, False),
    'Spine': ((0.0, 0.0, 1.100), 'Chest', None, False),
    'Chest': ((0.0, 0.0, 1.250), 'Neck', None, False),
    'Neck': ((0.0, 0.0, 1.500), 'Head', None, False),
    'Head': ((0.0, 0.0, 1.600), 'up', 0.240, False),
}
for _side, _sign in (('Left', 1.0), ('Right', -1.0)):
    for _role, ((_x, _y, _z), _toward, _length, _stretch) in _LEFT.items():
        _target = _toward if _toward in ('up', 'forward') else (
            'continue:' + _side + _toward.split(':')[1] if _toward.startswith('continue:') else _side + _toward)
        RIG[_side + _role] = ((_x * _sign, _y, _z), _target, _length, _stretch)


def joint(role):
    return Vector(RIG[role][0])


def tip(role):
    position, toward, length, _ = RIG[role]
    position = Vector(position)
    if toward == 'up':
        return position + UP * length
    if toward == 'forward':
        return position + FORWARD * length
    if toward.startswith('continue:'):
        return position + (position - joint(toward.split(':')[1])).normalized() * length
    return joint(toward)


def game(v):
    return [round(v[0], 5), round(v[2], 5), round(-v[1], 5)]


# Canonical parent of each role. A donor avatar need not map every role -- Draugr map 19 and have no
# Neck -- so a part on a missing role rides its nearest mapped ancestor instead.
PARENT = {'Spine': 'Hips', 'Chest': 'Spine', 'Neck': 'Chest', 'Head': 'Neck'}
for _side in ('Left', 'Right'):
    PARENT.update({_side + 'UpperArm': 'Chest', _side + 'LowerArm': _side + 'UpperArm', _side + 'Hand': _side + 'LowerArm',
                   _side + 'UpperLeg': 'Hips', _side + 'LowerLeg': _side + 'UpperLeg', _side + 'Foot': _side + 'LowerLeg'})


def rig_payload():
    height = joint('Head').z - joint('LeftFoot').z
    return {'height': round(height, 5), 'bones': {
        role: {'joint': game(joint(role)), 'tip': game(tip(role)), 'toward': RIG[role][1], 'stretch': RIG[role][3],
               'parent': PARENT.get(role)}
        for role in RIG}}


def bone_frame(role):
    """(joint, axis, lateral, forward) using the same second-axis rule as the runtime binder."""
    j = joint(role)
    y = (tip(role) - j).normalized()
    f = FORWARD - y * FORWARD.dot(y)
    if f.length_squared < 0.09:
        f = -UP - y * (-UP).dot(y)
    f.normalize()
    return j, y, y.cross(f).normalized(), f


# ------------------------------------------------------------------------------------------------
# Body geometry
# ------------------------------------------------------------------------------------------------

def section(rx, back, front, power=2.0, sides=16, shift=0.0):
    """A torso/head cross-section in (x, y): superellipse, deeper in front or behind, facing -Y."""
    points = []
    for j in range(sides):
        a = math.tau * j / sides
        c, s = math.cos(a), math.sin(a)
        x = rx * math.copysign(abs(c) ** (2.0 / power), c)
        depth = front if s < 0 else back
        points.append((x, depth * math.copysign(abs(s) ** (2.0 / power), s) + shift))
    return points


def column(stations, sides=16, power=2.0, top_apex=None):
    """Loft along Z. stations: (z, rx, back, front[, shift])."""
    rings = []
    for st in stations:
        z, rx, back, front = st[:4]
        shift = st[4] if len(st) > 4 else 0.0
        rings.append([Vector((x, y, z)) for x, y in section(rx, back, front, power, sides, shift)])
    if top_apex is not None:
        rings.append([Vector(top_apex)])
    return loft(rings)


def interpolate(stations, z):
    for a, b in zip(stations, stations[1:]):
        if a[0] <= z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            return tuple([z] + [a[i] + (b[i] - a[i]) * t for i in range(1, len(a))])
    raise ValueError(f'z {z} outside torso')


def torso_piece(stations, z0, z1, rings=7, sides=16, power=2.0):
    zs = [z0 + (z1 - z0) * i / (rings - 1) for i in range(rings)]
    return column([interpolate(stations, z) for z in zs], sides, power)


def limb(role, stations, sides=12, squash=1.0, facets=False):
    """Loft along a canonical bone. stations: (t, radius[, forward offset]) with t along joint->tip,
    free to overshoot either end so neighbouring segments overlap at the joint."""
    j, y, u, f = bone_frame(role)
    length = (tip(role) - j).length
    rings = []
    phase = math.pi / sides if facets else 0.0
    for st in stations:
        t, r = st[0], st[1]
        offset = st[2] if len(st) > 2 else 0.0
        centre = j + y * (t * length) + f * offset
        if r <= 0:
            rings.append([centre])
            continue
        rings.append(ring(centre, u, f, [(r * math.cos(phase + math.tau * k / sides),
                                         squash * r * math.sin(phase + math.tau * k / sides)) for k in range(sides)]))
    return loft(rings)


def blob(centre, radii, sides=10, rings=6):
    """A closed ellipsoid mass."""
    cx, cy, cz = centre
    rx, ry, rz = radii
    sections = [[Vector((cx, cy, cz - rz))]]
    for i in range(1, rings):
        a = math.pi * i / rings - math.pi / 2
        sections.append([Vector((cx + rx * math.cos(a) * math.cos(math.tau * k / sides),
                                 cy + ry * math.cos(a) * math.sin(math.tau * k / sides), cz + rz * math.sin(a)))
                         for k in range(sides)])
    sections.append([Vector((cx, cy, cz + rz))])
    return loft(sections)


def transformed(mesh, matrix):
    verts, faces = mesh
    return [matrix @ Vector(v) for v in verts], faces


def spike(points, width, thickness, samples=10, flat=False):
    """A tapering flame lock, ribbon or crest along a curve, ending in a point."""
    path = curve(points, samples)
    # The sweep's reference axis must never lie along the lock, or its frame collapses and the
    # surface folds inside-out (the Earth ridge runs front to back, straight down Y). Prefer X for a
    # flat ribbon so it lies broad, then take whichever axis crosses the lock's direction most.
    direction = (path[-1] - path[0]).normalized()
    preferred = [X, Z, Y] if flat else [Y, X, Z]
    up = next((a for a in preferred if abs(direction.dot(a)) < 0.7), min(preferred, key=lambda a: abs(direction.dot(a))))

    def profile(k, t):
        if t >= 0.999:
            return [(0.0, 0.0)]
        taper = (1.0 - t) ** 0.8
        return diamond(width * taper, thickness * taper) if not flat else \
            [(width * taper, 0.0), (0.0, thickness * taper), (-width * taper, 0.0), (0.0, -thickness * taper)]
    return sweep(path, profile, up)


def plate(outline, thickness, matrix):
    """A thick plate grown from an XZ outline (y thickness), placed by matrix."""
    cx = sum(p[0] for p in outline) / len(outline)
    cz = sum(p[1] for p in outline) / len(outline)
    return transformed(slab(outline, (cx, cz), lambda x, z, s: thickness * (0.55 if s >= 0.999 else 1.0),
                            rings=(1.0, 0.85, 0.4)), matrix)


def arc(centre, radius, start, end, width, thickness, samples=12):
    """A flat broken ring segment in the XZ plane (halo pieces, collars)."""
    cx, cy, cz = centre
    path = [Vector((cx + radius * math.cos(a), cy, cz + radius * math.sin(a)))
            for a in [start + (end - start) * i / (samples - 1) for i in range(samples)]]

    def profile(k, t):
        taper = 0.35 + 0.65 * math.sin(math.pi * t)
        return [(width * taper, 0.0), (0.0, thickness), (-width * taper, 0.0), (0.0, -thickness)]
    return sweep(path, profile, Y)


def at(role, t, lateral=0.0, forward=0.0):
    j, y, u, f = bone_frame(role)
    return j + y * (t * (tip(role) - j).length) + u * lateral + f * forward


# ------------------------------------------------------------------------------------------------
# Bodies: the two shared forms, shaped per element by a proportion kit
# ------------------------------------------------------------------------------------------------

TORSO = {
    # (z, half-width, back depth, front depth[, forward shift])
    # Masculine: V-taper, broad shoulder line, narrow hips. Feminine: narrow waist, full hips.
    'masculine': [(0.84, 0.120, 0.092, 0.085), (0.92, 0.142, 0.102, 0.095), (1.02, 0.136, 0.093, 0.095),
                  (1.12, 0.150, 0.100, 0.108), (1.24, 0.182, 0.106, 0.122), (1.36, 0.214, 0.110, 0.124),
                  (1.45, 0.205, 0.100, 0.102), (1.52, 0.128, 0.075, 0.070), (1.57, 0.060, 0.050, 0.050)],
    'feminine': [(0.84, 0.148, 0.114, 0.090), (0.93, 0.190, 0.126, 0.098), (1.03, 0.152, 0.100, 0.086),
                 (1.12, 0.106, 0.080, 0.082), (1.22, 0.130, 0.086, 0.096), (1.33, 0.150, 0.090, 0.108),
                 (1.43, 0.152, 0.086, 0.090), (1.51, 0.100, 0.066, 0.060), (1.57, 0.053, 0.046, 0.046)],
}
HEAD = [(1.565, 0.048, 0.050, 0.050), (1.620, 0.072, 0.082, 0.086), (1.700, 0.084, 0.094, 0.098),
        (1.770, 0.083, 0.092, 0.088), (1.820, 0.068, 0.078, 0.066), (1.852, 0.038, 0.044, 0.036)]

# element -> torso width, torso depth, limb girth, head scale, cross-section power, faceting sides
KIT = {
    'fire': dict(width=1.02, depth=1.00, girth=1.05, head=1.00, power=2.0, sides=14),
    'water': dict(width=0.96, depth=0.95, girth=0.94, head=0.98, power=2.0, sides=18),
    'earth': dict(width=1.38, depth=1.30, girth=1.50, head=0.92, power=2.6, sides=8),
    'wind': dict(width=0.84, depth=0.84, girth=0.78, head=0.96, power=2.0, sides=14),
    'radiance': dict(width=1.00, depth=0.98, girth=1.00, head=1.00, power=2.0, sides=16),
    'umbral': dict(width=0.88, depth=0.86, girth=0.84, head=1.02, power=2.2, sides=12),
}


class Surtling:
    def __init__(self, element, body):
        self.element, self.body = element, body
        self.model_id = f'underworld-surtling-{element}-{body}'
        self.kit = KIT[element]
        self.atlas = bpy.data.images.new(self.model_id + '-albedo', BAKE_SIZE, BAKE_SIZE, alpha=True)
        self.atlas.generated_color = (0, 0, 0, 0)
        self.materials = {}
        self.parts = []

    def material(self, surface):
        if surface in self.materials:
            return self.materials[surface]
        spec = SURFACES[surface]
        mat = bpy.data.materials.new(f'magenheim.underworld-surtling.{self.model_id}.{surface}')
        mat.use_nodes = True
        mat.use_backface_culling = True
        mat['surface'] = surface
        nodes = mat.node_tree.nodes
        bsdf = nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (1, 1, 1, 1)
        bsdf.inputs['Metallic'].default_value = spec['metallic']
        bsdf.inputs['Roughness'].default_value = spec['rough']
        bsdf.inputs['Emission Color'].default_value = (*spec.get('emission', (0, 0, 0)), 1)
        bsdf.inputs['Emission Strength'].default_value = 1.0
        image = nodes.new('ShaderNodeTexImage')
        image.image = self.atlas
        image.name = 'Atlas'
        mat.node_tree.links.new(image.outputs['Color'], bsdf.inputs['Base Color'])
        nodes.active = image
        self.materials[surface] = mat
        return mat

    def part(self, role, name, mesh, surface, smooth=True, sharp=50.0):
        if role not in RIG:
            raise ValueError(f'{self.model_id}/{name}: {role} is not a canonical bone')
        full = f'{role}.{name}'
        if bpy.data.objects.get(full):
            raise ValueError(f'{self.model_id}: duplicate part {full}')
        data = bpy.data.meshes.new(full)
        verts, faces = mesh
        data.from_pydata([tuple(v) for v in verts], [], faces)
        data.update()
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(data)
        bm.free()
        if smooth:
            data.shade_smooth()
            data.set_sharp_from_angle(angle=math.radians(sharp))
        else:
            data.shade_flat()
        data.materials.append(self.material(surface))
        obj = bpy.data.objects.new(full, data)
        bpy.context.scene.collection.objects.link(obj)
        obj['game_node_path'] = f'bone:{role}/{name}'
        obj['game_collision'] = False
        obj['game_crystal'] = 'null'
        self.parts.append(obj)
        return obj

    # ---- shared body ------------------------------------------------------------------------

    def build_body(self, surface, joint_surface=None):
        k = self.kit
        smooth = k['sides'] > 10
        stations = [(z, rx * k['width'], b * k['depth'], f * k['depth']) for z, rx, b, f in TORSO[self.body]]
        sides, power = k['sides'] + 2, k['power']
        self.part('Hips', 'pelvis', torso_piece(stations, 0.84, 1.07, 6, sides, power), surface, smooth)
        self.part('Spine', 'abdomen', torso_piece(stations, 1.02, 1.27, 6, sides, power), surface, smooth)
        self.part('Chest', 'chest', torso_piece(stations, 1.22, 1.57, 8, sides, power), surface, smooth)
        g = k['girth']
        self.part('Neck', 'neck', limb('Neck', [(-0.3, 0.058 * g), (0.5, 0.052 * g), (1.2, 0.050 * g)], k['sides']),
                  surface, smooth)
        h = k['head']
        head = [(1.60 + (z - 1.60) * h, rx * h, b * h, f * h) for z, rx, b, f in HEAD]
        self.part('Head', 'head', column(head, k['sides'] + 2, 2.0, (0.0, 0.004, 1.60 + 0.262 * h)), surface, smooth)
        # A recognisable face (the design's shared family construction): brow, nose, jaw.
        self.part('Head', 'brow', blob((0.0, -0.080 * h, 1.60 + 0.128 * h), (0.072 * h, 0.028 * h, 0.017 * h), 10, 4),
                  surface, smooth)
        self.part('Head', 'nose', blob((0.0, -0.100 * h, 1.60 + 0.082 * h), (0.013 * h, 0.022 * h, 0.028 * h), 6, 4),
                  surface, smooth)
        self.part('Head', 'jaw', blob((0.0, -0.058 * h, 1.60 + 0.030 * h), (0.062 * h, 0.052 * h, 0.030 * h), 10, 4),
                  surface, smooth)
        w, d = k['width'], k['depth']
        if self.body == 'masculine':
            for side in (-1, 1):
                self.part('Chest', f'pectoral-{"l" if side < 0 else "r"}',
                          blob((0.082 * side * w, -0.094 * d, 1.365), (0.098 * g, 0.028 * g, 0.056 * g), k['sides'], 5),
                          surface, smooth)
            self.part('Spine', 'abdomen-mass', blob((0.0, -0.092 * d, 1.170), (0.075 * g, 0.036 * g, 0.105 * g), k['sides'], 5),
                      surface, smooth)
        if self.body == 'feminine':
            for side in (-1, 1):
                self.part('Chest', f'bust-{"l" if side < 0 else "r"}',
                          blob((0.075 * side * k['width'], -0.085 * k['depth'], 1.335), (0.066 * g, 0.058 * g, 0.060 * g),
                               k['sides'], 6), surface, smooth)
        for side in ('Left', 'Right'):
            sign = 1 if side == 'Right' else -1
            self.part(f'{side}UpperArm', 'upper-arm', limb(f'{side}UpperArm', [
                (-0.12, 0.052 * g), (0.0, 0.066 * g), (0.30, 0.062 * g), (0.70, 0.050 * g), (1.00, 0.044 * g),
                (1.10, 0.030 * g)], k['sides']), surface, smooth)
            self.part(f'{side}LowerArm', 'forearm', limb(f'{side}LowerArm', [
                (-0.08, 0.040 * g), (0.05, 0.049 * g), (0.40, 0.047 * g), (0.85, 0.036 * g), (1.00, 0.031 * g),
                (1.07, 0.020 * g)], k['sides']), surface, smooth)
            self.part(f'{side}Hand', 'hand', limb(f'{side}Hand', [
                (-0.10, 0.028 * g), (0.05, 0.034 * g), (0.45, 0.036 * g), (0.85, 0.030 * g), (1.00, 0.0)],
                max(6, k['sides'] - 4), squash=0.55), surface, smooth)
            self.part(f'{side}Hand', 'thumb', limb(f'{side}Hand', [
                (0.05, 0.013 * g, -0.028), (0.35, 0.012 * g, -0.036), (0.55, 0.0, -0.032)], 6), surface, smooth)
            self.part(f'{side}UpperLeg', 'thigh', limb(f'{side}UpperLeg', [
                (-0.10, 0.070 * g), (0.00, 0.086 * g), (0.35, 0.080 * g), (0.80, 0.060 * g), (1.00, 0.053 * g),
                (1.07, 0.036 * g)], k['sides']), surface, smooth)
            self.part(f'{side}LowerLeg', 'shin', limb(f'{side}LowerLeg', [
                (-0.06, 0.052 * g), (0.10, 0.057 * g), (0.35, 0.056 * g), (0.80, 0.039 * g), (1.00, 0.034 * g),
                (1.06, 0.024 * g)], k['sides']), surface, smooth)
            self.part(f'{side}Foot', 'foot', limb(f'{side}Foot', [
                (-0.45, 0.030 * g, 0.045), (-0.25, 0.042 * g, 0.050), (0.10, 0.047 * g, 0.055),
                (0.60, 0.045 * g, 0.068), (1.00, 0.030 * g, 0.072), (1.10, 0.0, 0.074)], k['sides'], squash=0.8),
                surface, smooth)
            # Joint masses hide the articulation between rigid segments -- and are where each
            # element puts its seam light.
            js = joint_surface or surface
            self.part(f'{side}LowerArm', 'elbow', limb(f'{side}LowerArm', [
                (-0.09, 0.044 * g), (-0.06, 0.053 * g), (0.03, 0.053 * g), (0.06, 0.044 * g)], k['sides']), js, smooth)
            self.part(f'{side}LowerLeg', 'knee', limb(f'{side}LowerLeg', [
                (-0.07, 0.052 * g), (-0.045, 0.062 * g), (0.03, 0.062 * g), (0.055, 0.052 * g)], k['sides']), js, smooth)
            self.part(f'{side}LowerLeg', 'calf', blob(tuple(at(f'{side}LowerLeg', 0.30, 0.0, -0.022 * g)),
                                                      (0.046 * g, 0.050 * g, 0.095 * g), k['sides'], 5), surface, smooth)
            self.part(f'{side}UpperArm', 'shoulder', blob(tuple(joint(f'{side}UpperArm') + Vector((0.012 * sign * -1, 0, 0.01))),
                                                        (0.068 * g, 0.066 * g, 0.064 * g), 10, 6), surface, smooth)

    def eyes(self, surface, size=1.0):
        h = self.kit['head']
        for side in (-1, 1):
            self.part('Head', f'eye-{"l" if side < 0 else "r"}',
                      gem((0.034 * side * h, -0.095 * h, 1.60 + 0.105 * h), 0.013 * size * h, 0.010 * h, 6, axis='Y'),
                      surface, False)

    # ---- regalia helpers --------------------------------------------------------------------

    def girdle(self, surface, z=0.95, height=0.05, grow=1.10):
        stations = [(z, rx * self.kit['width'], b * self.kit['depth'], f * self.kit['depth']) for z, rx, b, f in TORSO[self.body]]
        lo, hi = interpolate(stations, z - height / 2), interpolate(stations, z + height / 2)
        rings = [[Vector((x * grow, y * grow, s[0])) for x, y in section(s[1], s[2], s[3], 2.0, 18)] for s in (lo, hi)]
        self.part('Hips', 'girdle', loft(rings), surface, True)

    def hip_plates(self, surface, count=3, length=0.20, width=0.11, thickness=0.012, flare=0.0):
        k = self.kit
        rx = 0.165 * k['width'] * (1.08 if self.body == 'feminine' else 1.0)
        for i in range(count):
            angle = math.pi * (0.5 + (i - (count - 1) / 2) * (0.8 if count > 1 else 0))
            outline = [(-width / 2, 0.0), (width / 2, 0.0), (width * 0.42, -length * 0.8), (0.0, -length),
                       (-width * 0.42, -length * 0.8)]
            m = (Matrix.Translation(Vector((rx * math.cos(angle) * 1.02, -0.12 * k['depth'] * math.sin(angle) * 1.1, 0.965)))
                 @ Matrix.Rotation(angle - math.pi / 2, 4, 'Z') @ Matrix.Rotation(-flare, 4, 'X'))
            self.part('Hips', f'hip-plate-{i}', plate(outline, thickness, m), surface, False)


# ------------------------------------------------------------------------------------------------
# Surfaces
# ------------------------------------------------------------------------------------------------

# low/high: body ramp; scale: noise scale; veins: (colour, width, scale) drawn along cell edges;
# edge: (colour, gain); occlusion strength; shading exported to the runtime material.
SURFACES = {
    'charcoal': dict(low=(0.030, 0.026, 0.026), high=(0.105, 0.085, 0.075), scale=14,
                     veins=((1.00, 0.34, 0.05), 0.030, 11.0), edge=((0.30, 0.12, 0.06), 0.35), occlusion=0.85,
                     metallic=0.0, rough=0.88),
    'obsidian': dict(low=(0.020, 0.018, 0.028), high=(0.090, 0.075, 0.105), scale=9, edge=((0.55, 0.42, 0.60), 0.8),
                     occlusion=0.9, metallic=0.35, rough=0.22),
    'ember': dict(low=(0.92, 0.20, 0.02), high=(1.00, 0.64, 0.12), scale=6, edge=((1.0, 0.95, 0.7), 0.5),
                  occlusion=0.2, metallic=0.0, rough=0.5, emission=(0.95, 0.34, 0.06)),
    'abyss': dict(low=(0.012, 0.030, 0.048), high=(0.030, 0.085, 0.115), scale=6,
                  veins=((0.20, 0.70, 0.78), 0.030, 9.0), edge=((0.30, 0.60, 0.70), 0.45), occlusion=0.7,
                  metallic=0.0, rough=0.18),
    'current': dict(low=(0.10, 0.55, 0.66), high=(0.55, 0.92, 0.96), scale=5, edge=((0.85, 1.0, 1.0), 0.6),
                    occlusion=0.2, metallic=0.0, rough=0.15, emission=(0.05, 0.30, 0.36)),
    'nacre': dict(low=(0.55, 0.60, 0.64), high=(0.86, 0.84, 0.90), scale=16, edge=((0.95, 0.96, 1.0), 0.6),
                  occlusion=0.8, metallic=0.2, rough=0.3),
    'basalt': dict(low=(0.085, 0.080, 0.078), high=(0.230, 0.215, 0.200), scale=22,
                   veins=((0.40, 0.33, 0.24), 0.020, 5.0), edge=((0.46, 0.43, 0.40), 0.8), occlusion=1.0,
                   metallic=0.0, rough=0.92),
    'mineral': dict(low=(0.24, 0.16, 0.08), high=(0.52, 0.38, 0.20), scale=18, edge=((0.80, 0.66, 0.40), 0.8),
                    occlusion=0.9, metallic=0.4, rough=0.5),
    'crystal': dict(low=(0.18, 0.52, 0.62), high=(0.60, 0.92, 0.95), scale=8, edge=((0.9, 1.0, 1.0), 1.0),
                    occlusion=0.4, metallic=0.0, rough=0.2, emission=(0.06, 0.24, 0.28)),
    'vapor': dict(low=(0.56, 0.66, 0.74), high=(0.86, 0.92, 0.96), scale=5,
                  veins=((0.62, 0.86, 0.96), 0.022, 4.0), edge=((1.0, 1.0, 1.0), 0.4),
                  occlusion=0.55, metallic=0.0, rough=0.6),
    'ribbon': dict(low=(0.60, 0.74, 0.80), high=(0.88, 0.95, 0.98), scale=10, edge=((1.0, 1.0, 1.0), 0.5),
                   occlusion=0.4, metallic=0.0, rough=0.45),
    'gale': dict(low=(0.55, 0.85, 0.95), high=(0.90, 1.00, 1.00), scale=6, edge=((1.0, 1.0, 1.0), 0.5),
                 occlusion=0.2, metallic=0.0, rough=0.3, emission=(0.12, 0.26, 0.30)),
    'alabaster': dict(low=(0.70, 0.66, 0.58), high=(0.94, 0.91, 0.84), scale=7,
                      veins=((0.98, 0.86, 0.55), 0.020, 4.0), edge=((1.0, 0.98, 0.92), 0.5), occlusion=0.6,
                      metallic=0.0, rough=0.35),
    'gold': dict(low=(0.45, 0.30, 0.08), high=(0.92, 0.72, 0.30), scale=14, edge=((1.0, 0.92, 0.6), 0.9),
                 occlusion=0.9, metallic=0.9, rough=0.28),
    'radiant': dict(low=(0.98, 0.86, 0.55), high=(1.00, 0.98, 0.90), scale=5, edge=((1.0, 1.0, 1.0), 0.5),
                    occlusion=0.1, metallic=0.0, rough=0.3, emission=(0.60, 0.50, 0.28)),
    'umbral': dict(low=(0.010, 0.010, 0.016), high=(0.040, 0.036, 0.055), scale=8, edge=((0.16, 0.12, 0.24), 0.25),
                   occlusion=0.5, metallic=0.0, rough=0.97),
    'umbral-metal': dict(low=(0.035, 0.035, 0.048), high=(0.12, 0.12, 0.15), scale=16,
                         edge=((0.55, 0.55, 0.66), 0.9), occlusion=0.9, metallic=0.7, rough=0.3),
    'violet': dict(low=(0.30, 0.16, 0.60), high=(0.62, 0.52, 0.95), scale=6, edge=((0.85, 0.80, 1.0), 0.4),
                   occlusion=0.2, metallic=0.0, rough=0.3, emission=(0.16, 0.08, 0.34)),
}


def paint(mat):
    spec = SURFACES[mat['surface']]
    tree = mat.node_tree
    coords = tree.nodes.new('ShaderNodeTexCoord').outputs['Object']
    mottle = _noise(tree, coords, spec['scale'], detail=5.0, distortion=0.4)
    base = _ramp(tree, mottle, [(0.2, srgb(*spec['low'])), (0.8, srgb(*spec['high']))])
    if 'veins' in spec:
        colour, width, scale = spec['veins']
        cells = tree.nodes.new('ShaderNodeTexVoronoi')
        cells.feature = 'DISTANCE_TO_EDGE'
        # Push the cell lattice around with noise so seams wander instead of tiling into polygons.
        warp = tree.nodes.new('ShaderNodeTexNoise')
        _feed(tree, warp.inputs['Vector'], coords)
        warp.inputs['Scale'].default_value = 4.0
        offset = tree.nodes.new('ShaderNodeVectorMath')
        offset.operation = 'MULTIPLY_ADD'
        tree.links.new(warp.outputs['Color'], offset.inputs[0])
        offset.inputs[1].default_value = (0.12, 0.12, 0.12)
        tree.links.new(coords, offset.inputs[2])
        _feed(tree, cells.inputs['Vector'], offset.outputs['Vector'])
        cells.inputs['Scale'].default_value = scale
        crack = _math(tree, 'SUBTRACT', 1.0, _math(tree, 'DIVIDE', cells.outputs['Distance'], width, clamp=True), clamp=True)
        base = _mix(tree, crack, base, srgb(*colour))
    edge_colour, edge_gain = spec['edge']
    finish(tree, base, srgb(*edge_colour), edge_gain, spec['occlusion'], ao_distance=0.06, edge_radius=0.006)


# ------------------------------------------------------------------------------------------------
# Elemental kits
# ------------------------------------------------------------------------------------------------

def fire(s):
    """Charcoal flame-flesh with glowing seams, a flame crown, and fused obsidian coverage."""
    s.build_body('charcoal', joint_surface='ember')
    s.eyes('ember', 1.2)
    s.part('Head', 'mouth', gem((0.0, -0.093, 1.64), 0.020, 0.006, 6, axis='Y'), 'ember', False)
    top = Vector((0.0, 0.01, 1.83))
    # Flame, not horns: flattened locks that waver side to side as they rise and sweep back.
    if s.body == 'masculine':
        locks = [(0.0, 0.26, 0.10), (-0.045, 0.20, 0.12), (0.045, 0.21, 0.12), (-0.08, 0.14, 0.14), (0.08, 0.15, 0.14),
                 (0.0, 0.16, 0.20)]
    else:
        locks = [(0.0, 0.20, 0.26), (-0.05, 0.16, 0.30), (0.05, 0.17, 0.30), (-0.09, 0.10, 0.34), (0.09, 0.11, 0.34),
                 (-0.03, 0.12, 0.40), (0.03, 0.13, 0.40)]
    for i, (dx, rise, back) in enumerate(locks):
        wobble = 0.018 * (1 if i % 2 else -1)
        points = [top + Vector((dx, 0.0, -0.01)), top + Vector((dx + wobble, back * 0.25, rise * 0.45)),
                  top + Vector((dx - wobble, back * 0.55, rise * 0.80)), top + Vector((dx * 1.2 + wobble, back, rise * 0.90
                  - (0.25 if s.body == 'feminine' else 0.0)))]
        s.part('Head', f'flame-{i}', spike(points, 0.034, 0.012, 12, flat=True), 'ember', False)
    s.part('Head', 'flame-core', blob(tuple(top + Vector((0.0, 0.03, 0.02))), (0.07, 0.07, 0.04), 8, 4), 'ember', True)
    s.girdle('obsidian', 0.955, 0.045)
    s.hip_plates('obsidian', 3, 0.20, 0.12, 0.013, 0.12)
    # Asymmetric volcanic pauldron fused to the left shoulder: layered domes, not a blade.
    for i, (lift, out, r) in enumerate([(0.060, 0.00, 1.00), (0.030, -0.03, 0.82)]):
        s.part('LeftUpperArm', f'pauldron-{i}', blob(tuple(joint('LeftUpperArm') + Vector((out - 0.01, 0.0, lift))),
                                                     (0.095 * r, 0.090 * r, 0.050 * r), 9, 4), 'obsidian', False)
    if s.body == 'feminine':
        s.part('Chest', 'breastguard', plate([(-0.15, 0.0), (0.15, 0.0), (0.12, -0.10), (0.0, -0.15), (-0.12, -0.10)], 0.014,
                                             Matrix.Translation(Vector((0.0, -0.15, 1.40)))), 'obsidian', False)


def water(s):
    """Deep-water body with current-light veins, flowing current hair and fins, shell coverage."""
    s.build_body('abyss')
    s.eyes('current')
    top = Vector((0.0, 0.03, 1.80))
    count, reach = (5, 0.50) if s.body == 'feminine' else (4, 0.28)
    for i in range(count):
        dx = (i - (count - 1) / 2) * 0.045
        s.part('Head', f'current-{i}', spike([top + Vector((dx, 0, 0)), top + Vector((dx * 1.4, 0.12, 0.06)),
                                              top + Vector((dx * 2.0, reach * 0.7, -0.02)), top + Vector((dx * 2.4, reach, -0.16))],
                                             0.052, 0.008, 12, flat=True), 'current', False)
    for side in ('Left', 'Right'):
        sign = -1 if side == 'Left' else 1
        fin = [(0.0, 0.0), (0.0, -0.20), (0.05, -0.16), (0.07, -0.04)]
        s.part(f'{side}LowerArm', 'fin', plate(fin, 0.004, Matrix.Translation(at(f'{side}LowerArm', 0.25, sign * 0.035, 0.0))
                                            @ Matrix.Rotation(math.radians(-90), 4, 'X') @ Matrix.Rotation(math.radians(90 + 90 * sign), 4, 'Z')),
               'current', False)
        s.part(f'{side}LowerLeg', 'fin', plate([(0.0, 0.0), (0.0, -0.26), (0.06, -0.20), (0.08, -0.05)], 0.004,
                                            Matrix.Translation(at(f'{side}LowerLeg', 0.15, 0.0, 0.045))
                                            @ Matrix.Rotation(math.radians(180), 4, 'Z')), 'current', False)
    s.girdle('nacre', 0.95, 0.04, 1.08)
    s.hip_plates('nacre', 3, 0.17, 0.10, 0.010, 0.08)
    if s.body == 'feminine':
        for side in (-1, 1):
            s.part('Chest', f'shell-{"l" if side < 0 else "r"}', blob((0.075 * side, -0.14, 1.335), (0.068, 0.030, 0.062), 10, 5),
                   'nacre', True)
    else:
        s.part('Chest', 'pearl', gem((0.0, -0.13, 1.40), 0.022, 0.018, 8, axis='Y'), 'current', False)


def earth(s):
    """Interlocking stone masses, a crystal crown over a mineral ridge, grown plate coverage."""
    s.build_body('basalt')
    s.eyes('crystal', 1.1)
    h = s.kit['head']
    for i, (dx, dz, r) in enumerate([(0.0, 0.26, 0.030), (-0.045, 0.235, 0.022), (0.045, 0.235, 0.022),
                                      (-0.075, 0.20, 0.016), (0.075, 0.20, 0.016)]):
        s.part('Head', f'crown-{i}', gem((dx * h, 0.02, 1.60 + dz * h), r, r * 2.2, 6, axis='Z'), 'crystal', False)
    s.part('Head', 'ridge', spike([Vector((0, -0.02, 1.60 + 0.24 * h)), Vector((0, 0.06, 1.60 + 0.24 * h)),
                                   Vector((0, 0.13, 1.60 + 0.16 * h))], 0.022, 0.030, 8), 'mineral', False)
    w, d = s.kit['width'], s.kit['depth']
    # Grown chest plates hugging the torso, with a sternum crystal between them.
    for side in (-1, 1):
        s.part('Chest', f'chest-plate-{"l" if side < 0 else "r"}',
               blob((0.090 * side * w, -0.118 * d, 1.385), (0.105, 0.036, 0.080), 7, 4), 'mineral', False)
    s.part('Chest', 'sternum', gem((0.0, -0.150 * d, 1.39), 0.026, 0.030, 6, axis='Y'), 'crystal', False)
    s.girdle('mineral', 0.95, 0.07, 1.06)
    s.hip_plates('basalt', 4, 0.19, 0.13, 0.022, 0.10)
    for side in ('Left', 'Right'):
        s.part(f'{side}UpperArm', 'boulder', blob(tuple(joint(f'{side}UpperArm') + Vector((0.0, 0.0, 0.05))),
                                                (0.10, 0.09, 0.07), 7, 4), 'basalt', False)
        g = s.kit['girth']
        s.part(f'{side}LowerArm', 'band', limb(f'{side}LowerArm', [
            (0.46, 0.046 * g), (0.50, 0.056 * g), (0.60, 0.054 * g), (0.64, 0.044 * g)], 7, facets=True), 'crystal', False)


def wind(s):
    """A pale near-weightless core wrapped in streaming ribbons that ignore gravity."""
    s.build_body('vapor')
    s.eyes('gale')
    top = Vector((0.0, 0.03, 1.82))
    count = 5 if s.body == 'feminine' else 4
    for i in range(count):
        dx = (i - (count - 1) / 2) * 0.04
        s.part('Head', f'plume-{i}', spike([top + Vector((dx, 0, 0)), top + Vector((dx * 1.2, 0.18, 0.10)),
                                            top + Vector((dx * 1.6, 0.42, 0.16)), top + Vector((dx * 2.2, 0.66, 0.24))],
                                           0.045, 0.006, 12, flat=True), 'ribbon', False)
    # Waist sash ends stream back and up rather than hanging.
    for i, dx in enumerate((-0.08, 0.08)):
        s.part('Hips', f'sash-{i}', spike([Vector((dx, 0.10, 0.96)), Vector((dx * 1.4, 0.30, 0.94)),
                                           Vector((dx * 1.9, 0.55, 1.02)), Vector((dx * 2.4, 0.78, 1.16))],
                                          0.070, 0.005, 12, flat=True), 'ribbon', False)
    s.girdle('ribbon', 0.97, 0.03, 1.07)
    # Crossing chest ribbons.
    for i, sign in enumerate((-1, 1)):
        s.part('Chest', f'cross-{i}', spike([Vector((0.15 * sign, -0.10, 1.44)), Vector((0.0, -0.135, 1.33)),
                                             Vector((-0.14 * sign, -0.10, 1.21)), Vector((-0.20 * sign, 0.05, 1.18))],
                                            0.036, 0.005, 10, flat=True), 'ribbon', False)
    for side in ('Left', 'Right'):
        s.part(f'{side}LowerLeg', 'vapor', spike([at(f'{side}LowerLeg', 0.6, 0, 0.04), at(f'{side}LowerLeg', 0.8, 0, 0.12),
                                                  at(f'{side}LowerLeg', 1.0, 0, 0.26)], 0.018, 0.004, 8, flat=True), 'gale', False)


def radiance(s):
    """An idealised alabaster body, a broken halo, and gold regalia held just off the body."""
    s.build_body('alabaster', joint_surface='gold')
    s.eyes('radiant', 1.2)
    h = s.kit['head']
    centre = (0.0, 0.10, 1.60 + 0.14 * h)
    for i, (a0, a1) in enumerate([(0.15, 1.25), (1.45, 2.55), (2.75, 3.10), (-0.40, -0.05)]):
        s.part('Head', f'halo-{i}', arc(centre, 0.24, a0, a1, 0.010, 0.006), 'radiant', False)
    for i, dx in enumerate((-0.05, 0.0, 0.05)):
        s.part('Head', f'filament-{i}', spike([Vector((dx, 0.0, 1.60 + 0.24 * h)), Vector((dx * 1.5, 0.02, 1.60 + 0.33 * h)),
                                               Vector((dx * 2.0, 0.03, 1.60 + 0.40 * h))], 0.008, 0.008, 8), 'radiant', False)
    # Floating collar: separate plates held clear of the neck.
    for i in range(5):
        angle = math.pi * (0.25 + 0.125 * i)
        m = (Matrix.Translation(Vector((-0.17 * math.cos(angle), -0.12 * math.sin(angle), 1.53)))
             @ Matrix.Rotation(angle - math.pi / 2, 4, 'Z') @ Matrix.Rotation(math.radians(-35), 4, 'X'))
        s.part('Chest', f'collar-{i}', plate([(-0.045, 0.0), (0.045, 0.0), (0.03, -0.06), (-0.03, -0.06)], 0.006, m), 'gold', False)
    s.girdle('radiant', 0.955, 0.028, 1.12)
    s.hip_plates('gold', 3, 0.18, 0.10, 0.008, 0.18)
    if s.body == 'feminine':
        s.part('Chest', 'sun-plate', plate([(-0.13, 0.0), (0.13, 0.0), (0.10, -0.08), (0.0, -0.12), (-0.10, -0.08)], 0.008,
                                           Matrix.Translation(Vector((0.0, -0.165, 1.40)))), 'gold', False)


def umbral(s):
    """A matte absorptive body, a mutable smoke mass for hair, faint violet seams, ritual plates."""
    s.build_body('umbral', joint_surface='violet')
    s.eyes('violet', 0.9)
    h = s.kit['head']
    base = Vector((0.0, 0.05, 1.60 + 0.20 * h))
    for i, (dx, lift, reach) in enumerate([(0.0, 0.10, 0.30), (-0.06, 0.04, 0.24), (0.06, 0.04, 0.24), (0.0, -0.08, 0.36)]):
        s.part('Head', f'smoke-{i}', spike([base + Vector((dx, 0, 0)), base + Vector((dx * 1.3, reach * 0.4, lift)),
                                            base + Vector((dx * 1.2, reach, lift * 0.5 - 0.10))],
                                           0.055 if s.body == 'feminine' else 0.045, 0.040, 10), 'umbral', True)
    for i, (dx, dz) in enumerate([(-0.04, 0.02), (0.05, -0.04)]):
        s.part('Head', f'mote-{i}', gem((dx, 0.26, 1.60 + (0.12 + dz) * h), 0.006, 0.004, 6, axis='Y'), 'violet', False)
    s.girdle('umbral-metal', 0.95, 0.020, 1.06)
    s.hip_plates('umbral-metal', 2, 0.24, 0.09, 0.008, 0.05)
    for side in (-1, 1):
        s.part('Chest', f'ornament-{"l" if side < 0 else "r"}', gem((0.26 * side, 0.02, 1.52), 0.022, 0.040, 4, axis='Z'),
               'umbral-metal', False)
    if s.body == 'feminine':
        s.part('Chest', 'ritual-plate', plate([(-0.12, 0.0), (0.12, 0.0), (0.0, -0.16)], 0.008,
                                              Matrix.Translation(Vector((0.0, -0.155, 1.41)))), 'umbral-metal', False)


KITS = {'fire': fire, 'water': water, 'earth': earth, 'wind': wind, 'radiance': radiance, 'umbral': umbral}
MODELS = [f'underworld-surtling-{e}-{b}' for e in KITS for b in ('feminine', 'masculine')]


def author(model_id):
    _, _, element, body = model_id.split('-')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = Surtling(element, body)
    KITS[element](s)
    scene = bpy.context.scene
    scene['model_id'] = model_id
    scene['runtime_lights'] = '[]'
    scene['runtime_rig'] = json.dumps(rig_payload())
    scene['surface_finish'] = '2'
    scene['surtling_authoring'] = REVISION
    unwrap(s.parts)
    overlap, coverage = uv_overlap(s.parts)
    if overlap > 0.01:
        raise ValueError(f'{model_id}: {overlap:.1%} of the atlas is claimed by two triangles')
    bake_atlas(s.parts, s.atlas, paint)
    triangles = sum(len(o.data.loop_triangles) for o in s.parts)
    materials = len(s.materials)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (model_id + '.blend')), compress=True)
    print(f'AUTHORED {model_id} parts={len(s.parts)} triangles={triangles} materials={materials} '
          f'uv_coverage={coverage:.0%} uv_overlap={overlap:.2%}', flush=True)


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    unknown = [a for a in args if a not in MODELS]
    if unknown:
        raise SystemExit('Unknown Surtling model(s): ' + ', '.join(unknown))
    for model_id in args or MODELS:
        author(model_id)


main()
