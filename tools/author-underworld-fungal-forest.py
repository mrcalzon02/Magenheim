"""Author the Fungal Forest's own flora and resources, replacing its vanilla donor stand-ins.

    tools/blender.ps1 author-underworld-fungal-forest [model-id ...]
    tools/blender.ps1 export-model-assets <the same ids>

Design authority: docs/UNDERWORLD_FLORA_TERRAIN_PLAN.md sections 4.1 and 5. The ecology preview
assembled this biome from ten vanilla prefabs -- Yggdrasil shoots, Magecap, Jotun Puffs and blue
mushrooms blown up five to twelve times, a Plains fern, a heath shrub, a rock and a root. This is
the plan's custom pass for that biome: the fungal tree species it names, the ground layer it
describes, and the four raw resources the biome yields.

Everything is authored at real size in meters, standing on the origin, Z up. Only tree stalks
collide (`game_collision`), so ground cover stays walk-through. Light comes from emissive gills,
caps and spore bulbs rather than per-plant point lights: the canopy is dense and a light per tree
would be the most expensive thing in the cavern. Regrowth and the Motherbloom are out of scope
(plan section 10 decisions 1 and 6); nothing here assumes either.
"""
import bmesh
import bpy
import math
import random
import sys
from pathlib import Path

from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from magenheim_blender_kit import (  # noqa: E402
    BAKE_SIZE, bake_atlas, blob, curve, gem, lathe, merge, plate, spec_painter, spike, sweep, transformed,
    unwrap, uv_overlap)

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
REVISION = 'fungal-forest-custom-1'
Z = Vector((0.0, 0.0, 1.0))

# ------------------------------------------------------------------------------------------------
# Surfaces
# ------------------------------------------------------------------------------------------------

SURFACES = {
    # Fibrous stalk flesh: grain runs up the stalk.
    'stalk': dict(low=(0.30, 0.27, 0.32), high=(0.58, 0.53, 0.56), scale=3, stretch=(9.0, 9.0, 0.5),
                  edge=((0.86, 0.84, 0.90), 0.35), occlusion=0.9, metallic=0.0, rough=0.82),
    'cap-teal': dict(low=(0.04, 0.17, 0.21), high=(0.12, 0.40, 0.42), scale=4,
                     veins=((0.36, 0.74, 0.70), 0.018, 3.0), edge=((0.50, 0.86, 0.80), 0.4), occlusion=0.85,
                     metallic=0.0, rough=0.55),
    'cap-violet': dict(low=(0.16, 0.08, 0.26), high=(0.40, 0.22, 0.54), scale=4,
                       veins=((0.72, 0.52, 0.90), 0.018, 3.0), edge=((0.80, 0.66, 0.95), 0.4), occlusion=0.85,
                       metallic=0.0, rough=0.55),
    'gill': dict(low=(0.30, 0.82, 0.76), high=(0.72, 1.00, 0.94), scale=12, edge=((0.9, 1.0, 1.0), 0.3),
                 occlusion=0.3, metallic=0.0, rough=0.5, emission=(0.08, 0.42, 0.38)),
    'gill-violet': dict(low=(0.60, 0.40, 0.90), high=(0.90, 0.78, 1.00), scale=12, edge=((1.0, 0.95, 1.0), 0.3),
                        occlusion=0.3, metallic=0.0, rough=0.5, emission=(0.30, 0.14, 0.46)),
    'amber': dict(low=(0.50, 0.26, 0.05), high=(0.94, 0.64, 0.20), scale=6,
                  veins=((1.00, 0.86, 0.46), 0.030, 7.0), edge=((1.0, 0.88, 0.55), 0.4), occlusion=0.8,
                  metallic=0.0, rough=0.6, emission=(0.18, 0.08, 0.01)),
    'worldroot': dict(low=(0.13, 0.19, 0.13), high=(0.34, 0.44, 0.30), scale=3, stretch=(8.0, 8.0, 0.6),
                      edge=((0.56, 0.66, 0.50), 0.4), occlusion=0.95, metallic=0.0, rough=0.85),
    'fern': dict(low=(0.10, 0.36, 0.27), high=(0.36, 0.72, 0.56), scale=9, edge=((0.70, 0.95, 0.80), 0.5),
                 occlusion=0.7, metallic=0.0, rough=0.7),
    'brush': dict(low=(0.28, 0.16, 0.38), high=(0.58, 0.40, 0.70), scale=10, edge=((0.80, 0.66, 0.90), 0.4),
                  occlusion=0.7, metallic=0.0, rough=0.7),
    'spore': dict(low=(0.80, 0.42, 0.78), high=(1.00, 0.80, 0.96), scale=10, edge=((1.0, 0.95, 1.0), 0.3),
                  occlusion=0.2, metallic=0.0, rough=0.4, emission=(0.40, 0.14, 0.36)),
    'lantern': dict(low=(0.20, 0.50, 0.72), high=(0.58, 0.88, 0.98), scale=7, edge=((0.9, 1.0, 1.0), 0.4),
                    occlusion=0.3, metallic=0.0, rough=0.4, emission=(0.06, 0.24, 0.34)),
    'nursery': dict(low=(0.18, 0.52, 0.48), high=(0.48, 0.88, 0.80), scale=7, edge=((0.8, 1.0, 0.96), 0.4),
                    occlusion=0.5, metallic=0.0, rough=0.5, emission=(0.03, 0.12, 0.10)),
    'stone': dict(low=(0.20, 0.22, 0.22), high=(0.44, 0.46, 0.44), scale=5,
                  veins=((0.14, 0.15, 0.15), 0.02, 4.0), edge=((0.62, 0.64, 0.60), 0.8), occlusion=1.0,
                  metallic=0.0, rough=0.92),
    'moss': dict(low=(0.14, 0.30, 0.18), high=(0.38, 0.60, 0.34), scale=18, edge=((0.55, 0.78, 0.50), 0.3),
                 occlusion=0.9, metallic=0.0, rough=0.95),
    'root': dict(low=(0.28, 0.18, 0.09), high=(0.58, 0.42, 0.24), scale=3, stretch=(8.0, 8.0, 0.6),
                 edge=((0.78, 0.62, 0.40), 0.4), occlusion=0.95, metallic=0.0, rough=0.85),
    'understone': dict(low=(0.22, 0.24, 0.28), high=(0.46, 0.48, 0.52), scale=6,
                       veins=((0.62, 0.52, 0.80), 0.015, 5.0), edge=((0.70, 0.72, 0.76), 0.9), occlusion=1.0,
                       metallic=0.0, rough=0.9),
}
# Occlusion radius scaled for trees and rocks rather than hand-held objects.
paint = spec_painter(SURFACES, ao_distance=0.25, edge_radius=0.012)


class Model:
    def __init__(self, model_id, atlas_size):
        self.model_id = model_id
        self.atlas_size = atlas_size
        self.atlas = bpy.data.images.new(model_id + '-albedo', BAKE_SIZE, BAKE_SIZE, alpha=True)
        self.atlas.generated_color = (0, 0, 0, 0)
        self.materials = {}
        self.parts = []

    def material(self, surface):
        if surface in self.materials:
            return self.materials[surface]
        spec = SURFACES[surface]
        mat = bpy.data.materials.new(f'magenheim.underworld.{self.model_id}.{surface}')
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

    def part(self, name, mesh, surface, smooth=True, collider=False, sharp=55.0):
        if bpy.data.objects.get(name):
            raise ValueError(f'{self.model_id}: duplicate part {name}')
        data = bpy.data.meshes.new(name)
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
        obj = bpy.data.objects.new(name, data)
        bpy.context.scene.collection.objects.link(obj)
        obj['game_node_path'] = f'magenheim.underworld.{self.model_id}/{name}'
        obj['game_collision'] = collider
        obj['game_crystal'] = 'null'
        self.parts.append(obj)
        return obj


# ------------------------------------------------------------------------------------------------
# Shared forms
# ------------------------------------------------------------------------------------------------

def tube(points, radii, sides=12, samples=12):
    """A tapering tube along a curve. radii: (t, r) stations, interpolated."""
    path = curve(points, samples)

    def radius(t):
        for (t0, r0), (t1, r1) in zip(radii, radii[1:]):
            if t0 <= t <= t1:
                return r0 + (r1 - r0) * (t - t0) / max(1e-6, t1 - t0)
        return radii[-1][1]

    def profile(k, t):
        r = radius(t)
        if r <= 0:
            return [(0.0, 0.0)]
        return [(r * math.cos(math.tau * j / sides), r * math.sin(math.tau * j / sides)) for j in range(sides)]
    # The reference axis must never lie along the tube or its frame collapses: X for a rising
    # stalk, Z for a branch or root that runs mostly sideways.
    direction = (Vector(points[-1]) - Vector(points[0])).normalized()
    return sweep(path, profile, Vector((1.0, 0.0, 0.0)) if abs(direction.z) > 0.7 else Z)


def cap(centre, radius, height, underside=0.18, sides=24, bell=0.0):
    """A mushroom cap: a domed top meeting a shallow concave underside at a rolled rim.
    bell > 0 draws the rim down into a bell."""
    cx, cy, cz = centre
    r, h = radius, height
    profile = [(cz + h, 0.0), (cz + h * 0.97, r * 0.32), (cz + h * 0.86, r * 0.62), (cz + h * 0.62, r * 0.86),
               (cz + h * 0.30 - bell * h, r * 0.98), (cz + h * 0.10 - bell * h * 1.2, r * 1.0),
               (cz - bell * h * 1.1, r * 0.94), (cz + underside * h * 0.3, r * 0.70), (cz + underside * h, r * 0.30),
               (cz + underside * h * 1.2, 0.0)]
    return lathe(profile, sides, centre=(cx, cy))


def gills(centre, inner, outer, depth, count=28, thickness=0.016):
    """Radial gill blades hanging under a cap."""
    blades = []
    for i in range(count):
        a = math.tau * i / count
        outline = [(inner, 0.0), (outer, -depth * 0.2), (outer * 0.98, -depth * 0.45), (inner * 1.1, -depth)]
        m = Matrix.Translation(Vector(centre)) @ Matrix.Rotation(a, 4, 'Z')
        blades.append(plate(outline, thickness, m))
    return merge(*blades)


def buttresses(base, count, reach, height, width, rng):
    roots = []
    for i in range(count):
        a = math.tau * i / count + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        start = Vector(base) + Z * height
        roots.append(tube([start, start + d * reach * 0.45 - Z * height * 0.45, start + d * reach - Z * (height + 0.12)],
                          [(0.0, width), (0.6, width * 0.55), (1.0, 0.0)], 8, 8))
    return merge(*roots)


def glowcap(m, scale=1.0, collider=True):
    rng = random.Random(7)
    s = scale
    top = Vector((0.30 * s, 0.10 * s, 6.30 * s))
    m.part('stalk', tube([Vector((0, 0, -0.10 * s)), Vector((0.05 * s, 0.0, 2.2 * s)), Vector((0.18 * s, 0.06 * s, 4.4 * s)), top],
                         [(0.0, 0.62 * s), (0.08, 0.44 * s), (0.5, 0.35 * s), (0.92, 0.30 * s), (1.0, 0.36 * s)], 14, 14),
           'stalk', collider=collider)
    m.part('roots', buttresses((0, 0, 0), 5, 1.4 * s, 0.9 * s, 0.22 * s, rng), 'stalk')
    m.part('cap', cap((top.x, top.y, top.z - 0.05 * s), 3.2 * s, 1.55 * s), 'cap-teal')
    m.part('gills', gills((top.x, top.y, top.z + 0.02 * s), 0.5 * s, 2.95 * s, 0.26 * s, 30, 0.02 * s), 'gill', smooth=False)


def spirestalk(m):
    rng = random.Random(11)
    points = [Vector((0, 0, -0.1)), Vector((0.25, -0.1, 3.5)), Vector((-0.15, 0.1, 7.0)), Vector((0.35, -0.2, 9.6)),
              Vector((0.55, -0.3, 11.7))]
    m.part('stalk', tube(points, [(0.0, 0.46), (0.06, 0.32), (0.5, 0.24), (0.85, 0.17), (1.0, 0.13)], 12, 18),
           'stalk', collider=True)
    m.part('roots', buttresses((0, 0, 0), 4, 1.1, 0.7, 0.16, rng), 'stalk')
    path = curve(points, 60)
    tiers = []
    under = []
    for t, r in ((0.60, 1.5), (0.80, 1.1), (0.985, 0.78)):
        p = path[int(t * (len(path) - 1))]
        tiers.append(cap((p.x, p.y, p.z - 0.10), r, r * 0.55, bell=0.10, sides=20))
        under.append(gills((p.x, p.y, p.z - 0.02), 0.2, r * 0.9, 0.14, 20, 0.014))
    m.part('caps', merge(*tiers), 'cap-violet')
    m.part('gills', merge(*under), 'gill-violet', smooth=False)


def puffcap(m):
    rng = random.Random(13)
    trunk_top = Vector((0.1, 0.0, 2.1))
    m.part('trunk', tube([Vector((0, 0, -0.1)), Vector((0.05, 0, 1.1)), trunk_top], [(0.0, 0.55), (0.12, 0.42), (1.0, 0.40)], 12, 8),
           'stalk', collider=True)
    m.part('roots', buttresses((0, 0, 0), 4, 1.0, 0.6, 0.18, rng), 'stalk')
    branches, puffs = [], []
    for i, (a, reach, rise, r) in enumerate([(0.3, 1.2, 2.3, 1.15), (2.4, 1.0, 1.6, 0.95), (4.4, 1.3, 1.9, 1.05)]):
        d = Vector((math.cos(a), math.sin(a), 0.0))
        end = trunk_top + d * reach + Z * rise
        branches.append(tube([trunk_top - Z * 0.2, trunk_top + d * reach * 0.4 + Z * rise * 0.5, end],
                             [(0.0, 0.30), (1.0, 0.22)], 10, 8))
        puffs.append(blob(tuple(end + Z * r * 0.55), (r, r, r * 0.92), 16, 10))
    m.part('branches', merge(*branches), 'stalk', collider=True)
    m.part('puffs', merge(*puffs), 'amber')


def tanglecap(m):
    rng = random.Random(17)
    strands = []
    tips = []
    for i in range(4):
        phase = math.tau * i / 4
        points = []
        for k in range(7):
            t = k / 6
            r = 0.34 * (1.0 - 0.35 * t)
            a = phase + t * 2.4
            points.append(Vector((r * math.cos(a), r * math.sin(a), -0.1 + t * (5.2 + 0.5 * i))))
        strands.append(tube(points, [(0.0, 0.24), (0.8, 0.14), (1.0, 0.10)], 10, 16))
        tips.append(points[-1])
    m.part('strands', merge(*strands), 'worldroot', collider=True)
    m.part('roots', buttresses((0, 0, 0), 5, 1.2, 0.6, 0.18, rng), 'worldroot')
    caps, under = [], []
    for i, p in enumerate(tips[:3]):
        r = (0.95, 0.75, 0.6)[i]
        caps.append(cap((p.x, p.y, p.z - 0.06), r, r * 0.5, sides=18))
        under.append(gills((p.x, p.y, p.z), 0.12, r * 0.88, 0.1, 16, 0.012))
    m.part('caps', merge(*caps), 'cap-teal')
    m.part('gills', merge(*under), 'gill', smooth=False)


def fern(m):
    rng = random.Random(19)
    fronds = []
    for i in range(9):
        a = math.tau * i / 9 + rng.uniform(-0.15, 0.15)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        length = rng.uniform(0.85, 1.15)
        points = [Vector((0, 0, 0.04)), d * 0.12 * length + Z * 0.45 * length, d * 0.42 * length + Z * 0.78 * length,
                  d * 0.62 * length + Z * 0.70 * length, d * 0.60 * length + Z * 0.54 * length]
        # Each frond curls within the plane of d and Z, so that plane's normal is a safe reference.
        fronds.append(spike(points, 0.085, 0.006, 12, flat=True, up=Z.cross(d).normalized()))
    m.part('fronds', merge(*fronds), 'fern', smooth=True)
    m.part('crown', blob((0, 0, 0.05), (0.09, 0.09, 0.07), 8, 5), 'fern')


def sporebrush(m):
    rng = random.Random(23)
    stalks, bulbs = [], []
    for i in range(14):
        a = rng.uniform(0, math.tau)
        lean = rng.uniform(0.05, 0.35)
        h = rng.uniform(0.6, 1.05)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        top = d * lean + Z * h
        stalks.append(spike([Vector((0, 0, 0)), d * lean * 0.3 + Z * h * 0.5, top + Z * 0.02], 0.016, 0.016, 8))
        bulbs.append(blob(tuple(top), (0.05, 0.05, 0.06), 8, 5))
    m.part('stalks', merge(*stalks), 'brush', smooth=True)
    m.part('bulbs', merge(*bulbs), 'spore')


def small_mushrooms(m, count, surface, stalk_surface, size_range, bell, seed, spread, height=(1.6, 2.6)):
    rng = random.Random(seed)
    stalks, caps = [], []
    for i in range(count):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.0, spread)
        x, y = d * math.cos(a), d * math.sin(a)
        size = rng.uniform(*size_range)
        h = size * rng.uniform(*height)
        stalks.append(lathe([(0.0, size * 0.28), (h * 0.5, size * 0.22), (h, size * 0.20)], 8, centre=(x, y)))
        caps.append(cap((x, y, h - size * 0.1), size, size * 0.62, bell=bell, sides=14))
    m.part('stalks', merge(*stalks), stalk_surface)
    m.part('caps', merge(*caps), surface)


def amber_bed(m):
    small_mushrooms(m, 7, 'amber', 'stalk', (0.06, 0.16), 0.0, 29, 0.32)


def lantern_caps(m):
    # Slender bells on tall stems, standing to knee height: lanterns, not buttons.
    small_mushrooms(m, 5, 'lantern', 'stalk', (0.045, 0.075), 0.45, 31, 0.18, height=(5.0, 8.0))


def nursery_caps(m):
    rng = random.Random(37)
    caps = []
    stalks = []
    for i in range(6):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.0, 0.35)
        x, y = d * math.cos(a), d * math.sin(a)
        r = rng.uniform(0.12, 0.28)
        h = r * rng.uniform(0.35, 0.8)
        stalks.append(lathe([(0.0, r * 0.25), (h, r * 0.18)], 8, centre=(x, y)))
        caps.append(cap((x, y, h), r, r * 0.28, sides=16))
    m.part('stalks', merge(*stalks), 'stalk')
    m.part('caps', merge(*caps), 'nursery')


def moss_stone(m):
    rng = random.Random(41)
    m.part('stone', blob((0, 0, 0.30), (0.72, 0.55, 0.52), 7, 5), 'stone', smooth=False)
    crust = [blob((rng.uniform(-0.35, 0.35), rng.uniform(-0.25, 0.25), 0.72 + rng.uniform(-0.05, 0.04)),
                  (rng.uniform(0.18, 0.32), rng.uniform(0.15, 0.26), 0.06), 8, 4) for _ in range(5)]
    m.part('moss', merge(*crust), 'moss')
    small_caps = [cap((0.55 * math.cos(a), 0.40 * math.sin(a), 0.42), 0.09, 0.05, sides=12) for a in (0.4, 1.3, 2.2)]
    m.part('shelf-caps', merge(*small_caps), 'nursery')


def root_skirt(m):
    rng = random.Random(43)
    arches = []
    for i in range(5):
        a = math.tau * i / 5 + rng.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        reach = rng.uniform(0.9, 1.3)
        arches.append(tube([Vector((0, 0, 0.35)), d * reach * 0.4 + Z * 0.55, d * reach * 0.85 + Z * 0.25, d * reach - Z * 0.10],
                           [(0.0, 0.10), (0.7, 0.07), (1.0, 0.03)], 8, 12))
    m.part('roots', merge(*arches), 'root')
    m.part('knot', blob((0, 0, 0.30), (0.20, 0.20, 0.18), 8, 5), 'root')


def sapling(m):
    glowcap(m, scale=0.24, collider=False)


# ---- resources -----------------------------------------------------------------------------------

def worldroot_timber(m):
    logs = []
    for i, (y, z, length) in enumerate([(-0.10, 0.09, 0.82), (0.10, 0.09, 0.76), (0.0, 0.25, 0.70)]):
        logs.append(transformed(lathe([(-length / 2, 0.085), (-length / 2 + 0.02, 0.092), (length / 2 - 0.02, 0.088),
                                       (length / 2, 0.080)], 10),
                                Matrix.Translation(Vector((0, y, z))) @ Matrix.Rotation(math.radians(90), 4, 'Y')))
    m.part('logs', merge(*logs), 'worldroot')
    m.part('band', transformed(lathe([(-0.03, 0.205), (0.03, 0.205)], 16, squash=0.92),
                               Matrix.Translation(Vector((0.0, 0.0, 0.16))) @ Matrix.Rotation(math.radians(90), 4, 'Y')), 'root')


def glowcap_flesh(m):
    m.part('flesh', cap((0, 0, 0.04), 0.26, 0.16, sides=16), 'cap-teal')
    m.part('gills', gills((0, 0, 0.07), 0.05, 0.24, 0.05, 14, 0.008), 'gill', smooth=False)


def spire_fibre(m):
    rng = random.Random(47)
    strands = [spike([Vector((-0.40, rng.uniform(-0.06, 0.06), 0.05 + rng.uniform(0, 0.08))),
                      Vector((0.0, rng.uniform(-0.05, 0.05), 0.07 + rng.uniform(0, 0.06))),
                      Vector((0.42, rng.uniform(-0.07, 0.07), 0.05 + rng.uniform(0, 0.08)))], 0.012, 0.012, 8)
               for _ in range(14)]
    m.part('fibres', merge(*strands), 'stalk')
    m.part('tie', transformed(lathe([(-0.025, 0.085), (0.025, 0.085)], 12, squash=0.75),
                              Matrix.Translation(Vector((0.0, 0.0, 0.08))) @ Matrix.Rotation(math.radians(90), 4, 'Y')), 'root')


def understone(m):
    m.part('stone', blob((0, 0, 0.13), (0.24, 0.18, 0.14), 7, 4), 'understone', smooth=False)


# model id -> (builder, atlas size). Trees get a larger atlas: a 12m stalk at 512px is mush.
MODELS = {
    'underworld-flora-fungal-glowcap': (glowcap, 1024),
    'underworld-flora-fungal-spirestalk': (spirestalk, 1024),
    'underworld-flora-fungal-puffcap': (puffcap, 1024),
    'underworld-flora-fungal-tanglecap': (tanglecap, 1024),
    'underworld-flora-fungal-fern': (fern, 512),
    'underworld-flora-fungal-sporebrush': (sporebrush, 512),
    'underworld-flora-fungal-amber-bed': (amber_bed, 512),
    'underworld-flora-fungal-lantern-caps': (lantern_caps, 512),
    'underworld-flora-fungal-nursery-caps': (nursery_caps, 512),
    'underworld-flora-fungal-moss-stone': (moss_stone, 512),
    'underworld-flora-fungal-root-skirt': (root_skirt, 512),
    'underworld-flora-fungal-sapling': (sapling, 512),
    'underworld-resource-worldroot-timber': (worldroot_timber, 512),
    'underworld-resource-glowcap-flesh': (glowcap_flesh, 512),
    'underworld-resource-spire-fibre': (spire_fibre, 512),
    'underworld-resource-understone': (understone, 512),
}


def author(model_id):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    builder, size = MODELS[model_id]
    m = Model(model_id, size)
    builder(m)
    scene = bpy.context.scene
    scene['model_id'] = model_id
    scene['runtime_lights'] = '[]'
    scene['surface_finish'] = '2'
    scene['underworld_authoring'] = REVISION
    unwrap(m.parts)
    overlap, coverage = uv_overlap(m.parts)
    if overlap > 0.01:
        raise ValueError(f'{model_id}: {overlap:.1%} of the atlas is claimed by two triangles')
    bake_atlas(m.parts, m.atlas, paint, size=m.atlas_size)
    triangles = sum(len(o.data.loop_triangles) for o in m.parts)
    heights = [(o.matrix_world @ v.co).z for o in m.parts for v in o.data.vertices]
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (model_id + '.blend')), compress=True)
    print(f'AUTHORED {model_id} parts={len(m.parts)} triangles={triangles} height={max(heights):.2f}m '
          f'atlas={m.atlas_size} uv_coverage={coverage:.0%}', flush=True)


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    unknown = [a for a in args if a not in MODELS]
    if unknown:
        raise SystemExit('Unknown Fungal Forest model(s): ' + ', '.join(unknown))
    for model_id in args or MODELS:
        author(model_id)


main()
