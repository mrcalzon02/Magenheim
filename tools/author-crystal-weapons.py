"""Author the ten crystal weapons: readable silhouettes, purpose-authored UVs, a baked painted albedo.

    tools/blender.ps1 author-crystal-weapons [model-id ...]
    tools/blender.ps1 export-model-assets <the same ids>

The family was box primitives inflated by bevel modifiers -- a sword blade was a 14-vertex prism as
thick edge-on as it was wide -- carrying a uniform 512px grain and a whole-part emission that washed
every crystal out to the same pale tone. Held closest to camera, they read as props beside vanilla
weapons. This rebuilds every weapon from designed forms and bakes each one a painted albedo in the
way vanilla weapons carry theirs: occlusion in the joins, worn highlights on the edges, and a
material pattern -- grain, hide, hammered blackmetal, clouded crystal -- rather than noise.

What is deliberately NOT changed, because in-hand placement was confirmed in play against it:

  * Blender Z is the length axis with the working end at +Z, X is the edge plane, Y is thickness.
    HeldModelAlignment ranks these extents to find the donor frame; ModelAssetTests replays that
    against every exported payload, so a drift here fails the build rather than a hand.
  * The grip stays on the origin and each weapon keeps its length envelope (ENVELOPE below), since
    the per-weapon trims in HeldModelAlignment are fractions of exactly that envelope.
  * Part paths and material identities. Material names stay
    `magenheim.crystal-weapon.<model>.<intent>[.<n>].<family>`, which verify-weapon-materials and the
    runtime classifier both read.
  * The greatsword's hand-authored design: the wide silver quillon bar with its raised ricasso plate
    and the faceted crystal pommel gem are rebuilt as themselves, not replaced.

Bakes are evaluated in 3D and written through each part's own UVs, so there is no island seam for
a pattern to break across -- the defect refine-retro-textures.py had to work around with fine grain.
"""
import bmesh
import bpy
import math
import sys

import numpy
from pathlib import Path

from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
# Baked at twice the shipped size and filtered down, so worn edges and facet lines are
# anti-aliased rather than stair-stepped. ModelAssets uploads textures uncompressed (RGBA32), so the
# shipped 512px holds GPU memory at a quarter of shipping the bake itself.
BAKE_SIZE = 1024
ATLAS = 512
BAKE_SAMPLES = 48
CHUNK = 0.28
# Every part bakes into one atlas in one call, and Blender applies the bake margin per object, so
# one part's margin overwrites any neighbouring island it can reach -- a white crystal edge's margin
# painted streaks down the hafts beside it. The bake therefore writes no margin at all, each part
# touching only its own texels, and pad() grows every island into its gutter afterwards.
ISLAND_GAP = 6
BAKE_MARGIN = 0
REVISION = 'crystal-weapons-readability-1'
# revise-model-library.py skips any source already at its own marker; keeping it stops that
# historical pass from re-running its old weapon edits over these forms.
LIBRARY_REVISION = 'surface-and-silhouette-1'

# Blender-space Z extent each weapon was confirmed in the hand at. The rebuild must stay within
# ENVELOPE_TOLERANCE of both ends or the HeldModelAlignment trims no longer describe it.
ENVELOPE = {
    'crystal-weapon-sword': (-0.562, 0.998),
    'crystal-weapon-greatsword': (-0.739, 1.394),
    'crystal-weapon-knife': (-0.390, 0.540),
    'crystal-weapon-axe': (-0.544, 0.630),
    'crystal-weapon-battleaxe': (-0.806, 0.750),
    'crystal-weapon-mace': (-0.608, 0.696),
    'crystal-weapon-spear': (-0.844, 1.139),
    'crystal-weapon-atgeir': (-0.941, 1.319),
    'crystal-weapon-bow': (-0.923, 0.923),
    'crystal-weapon-crossbow': (-0.488, 0.440),
}
ENVELOPE_TOLERANCE = 0.02


# --------------------------------------------------------------------------------------------
# Surfaces
# --------------------------------------------------------------------------------------------

def srgb(r, g, b):
    def lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (lin(r), lin(g), lin(b), 1.0)


# intent -> exported shading. Base colour is always white: the baked albedo carries the colour,
# and ModelAssets multiplies the two. Emission is kept to the accent intents; a whole blade that
# glows uniformly loses its shading and reads flat, which is most of why the family read pale.
SHADING = {
    'leather': dict(metallic=0.0, roughness=0.80, emission=(0, 0, 0)),
    'timber': dict(metallic=0.0, roughness=0.75, emission=(0, 0, 0)),
    'blackmetal': dict(metallic=0.60, roughness=0.42, emission=(0, 0, 0)),
    'silver': dict(metallic=0.75, roughness=0.30, emission=(0, 0, 0)),
    'crystal': dict(metallic=0.0, roughness=0.24, emission=(0.018, 0.034, 0.044)),
    'crystal-bright': dict(metallic=0.0, roughness=0.20, emission=(0.20, 0.30, 0.34)),
    'rainbow': dict(metallic=0.0, roughness=0.20, emission=(0.10, 0.06, 0.14)),
}

# intent -> family token. The token is what verify-weapon-materials checks and what
# GeneratedSurfaceTextures.Classify reads, so it must name the surface honestly.
FAMILY = {'leather': 'leather', 'timber': 'timber', 'blackmetal': 'metal', 'silver': 'silver',
          'crystal': 'crystal', 'crystal-bright': 'crystal', 'rainbow': 'crystal'}


class Painter:
    """Builds one material per part, all sampling the weapon's shared atlas."""

    def __init__(self, model_id, atlas):
        self.model_id = model_id
        self.atlas = atlas

    def material(self, surface, index=None):
        # 'leather' and 'timber' are both the grip intent; the family token says which.
        intent = 'grip' if surface in ('leather', 'timber') else surface
        name = '.'.join(['magenheim.crystal-weapon', self.model_id, intent]
                        + ([str(index)] if index is not None else []) + [FAMILY[surface]])
        if bpy.data.materials.get(name):
            raise ValueError(f'{name}: two parts share one material identity; index them')
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.use_backface_culling = True
        mat['surface'] = surface
        nodes = mat.node_tree.nodes
        bsdf = nodes.get('Principled BSDF')
        shading = SHADING[surface]
        bsdf.inputs['Base Color'].default_value = (1, 1, 1, 1)
        bsdf.inputs['Metallic'].default_value = shading['metallic']
        bsdf.inputs['Roughness'].default_value = shading['roughness']
        bsdf.inputs['Emission Color'].default_value = (*shading['emission'], 1)
        bsdf.inputs['Emission Strength'].default_value = 1.0
        image = nodes.new('ShaderNodeTexImage')
        image.image = self.atlas
        image.name = 'Atlas'
        mat.node_tree.links.new(image.outputs['Color'], bsdf.inputs['Base Color'])
        nodes.active = image
        return mat


def _mix(tree, factor, a, b, blend='MIX'):
    node = tree.nodes.new('ShaderNodeMix')
    node.data_type = 'RGBA'
    node.blend_type = blend
    sockets = [s for s in node.inputs if s.type == 'RGBA']
    _feed(tree, node.inputs['Factor'], factor)
    _feed(tree, sockets[0], a)
    _feed(tree, sockets[1], b)
    return [s for s in node.outputs if s.type == 'RGBA'][0]


def _feed(tree, socket, value):
    if isinstance(value, bpy.types.NodeSocket):
        tree.links.new(value, socket)
    else:
        socket.default_value = value


def _math(tree, op, a, b=None, clamp=False):
    node = tree.nodes.new('ShaderNodeMath')
    node.operation = op
    node.use_clamp = clamp
    _feed(tree, node.inputs[0], a)
    if b is not None:
        _feed(tree, node.inputs[1], b)
    return node.outputs[0]


def _ramp(tree, factor, stops):
    node = tree.nodes.new('ShaderNodeValToRGB')
    elements = node.color_ramp.elements
    while len(elements) > len(stops):
        elements.remove(elements[-1])
    while len(elements) < len(stops):
        elements.new(0.5)
    for element, (position, colour) in zip(elements, stops):
        element.position = position
        element.color = colour
    _feed(tree, node.inputs['Fac'], factor)
    return node.outputs['Color']


def _noise(tree, vector, scale, detail=4.0, distortion=0.0, roughness=0.55):
    node = tree.nodes.new('ShaderNodeTexNoise')
    _feed(tree, node.inputs['Vector'], vector)
    node.inputs['Scale'].default_value = scale
    node.inputs['Detail'].default_value = detail
    node.inputs['Roughness'].default_value = roughness
    node.inputs['Distortion'].default_value = distortion
    return node.outputs['Fac']


def _stretched(tree, coords, scale):
    node = tree.nodes.new('ShaderNodeMapping')
    _feed(tree, node.inputs['Vector'], coords)
    node.inputs['Scale'].default_value = scale
    return node.outputs['Vector']


def paint(mat):
    """Wire the bake graph: a family pattern, darkened by occlusion, lifted at worn edges.

    Everything is evaluated in object space and baked, so it is continuous across UV seams."""
    surface = mat['surface']
    tree = mat.node_tree
    coords = tree.nodes.new('ShaderNodeTexCoord').outputs['Object']

    if surface == 'leather':
        mottle = _noise(tree, coords, 38.0, detail=6.0)
        base = _ramp(tree, mottle, [(0.30, srgb(0.17, 0.105, 0.07)), (0.70, srgb(0.33, 0.21, 0.13))])
        tooth = _noise(tree, coords, 240.0, detail=2.0)
        base = _mix(tree, 0.18, base, _ramp(tree, tooth, [(0.3, srgb(0.10, 0.06, 0.04)),
                                                          (0.7, srgb(0.40, 0.27, 0.18))]))
        edge_colour, edge_gain, occlusion = srgb(0.52, 0.38, 0.27), 0.55, 0.85
    elif surface == 'timber':
        # Hafts all run along Z, so stretching Z turns noise into grain that runs the length.
        grain = _noise(tree, _stretched(tree, coords, (70.0, 70.0, 3.5)), 1.0, detail=8.0, distortion=1.2)
        base = _ramp(tree, grain, [(0.25, srgb(0.24, 0.15, 0.085)), (0.50, srgb(0.40, 0.27, 0.15)),
                                   (0.78, srgb(0.52, 0.37, 0.22))])
        figure = _noise(tree, _stretched(tree, coords, (9.0, 9.0, 0.8)), 1.0, detail=3.0)
        base = _mix(tree, _math(tree, 'MULTIPLY', figure, 0.35), base, srgb(0.20, 0.12, 0.07), 'MULTIPLY')
        edge_colour, edge_gain, occlusion = srgb(0.62, 0.47, 0.30), 0.35, 0.80
    elif surface == 'blackmetal':
        voronoi = tree.nodes.new('ShaderNodeTexVoronoi')
        _feed(tree, voronoi.inputs['Vector'], coords)
        voronoi.inputs['Scale'].default_value = 60.0
        hammered = _ramp(tree, voronoi.outputs['Distance'], [(0.0, srgb(0.16, 0.17, 0.20)),
                                                             (0.8, srgb(0.085, 0.09, 0.105))])
        scratches = _noise(tree, _stretched(tree, coords, (4.0, 4.0, 90.0)), 2.0, detail=2.0)
        base = _mix(tree, _math(tree, 'MULTIPLY', scratches, 0.25), hammered, srgb(0.30, 0.31, 0.34))
        edge_colour, edge_gain, occlusion = srgb(0.62, 0.64, 0.68), 1.0, 0.90
    elif surface == 'silver':
        tarnish = _noise(tree, coords, 22.0, detail=5.0)
        base = _ramp(tree, tarnish, [(0.35, srgb(0.46, 0.47, 0.50)), (0.70, srgb(0.70, 0.71, 0.74))])
        edge_colour, edge_gain, occlusion = srgb(0.95, 0.95, 0.96), 0.9, 1.0
    elif surface == 'rainbow':
        hue = _noise(tree, coords, 9.0, detail=3.0, distortion=0.6)
        base = _ramp(tree, hue, [(0.20, srgb(0.44, 0.24, 0.82)), (0.40, srgb(0.84, 0.28, 0.62)),
                                 (0.60, srgb(0.20, 0.72, 0.78)), (0.80, srgb(0.95, 0.76, 0.30))])
        edge_colour, edge_gain, occlusion = srgb(0.98, 0.96, 1.0), 0.8, 0.45
    else:
        bright = surface == 'crystal-bright'
        # Clouded body, darker toward the heart of each facet group, with drawn striations.
        cloud = _noise(tree, coords, 7.0, detail=5.0, distortion=0.4)
        deep, light = ((srgb(0.36, 0.64, 0.82), srgb(0.84, 0.97, 1.0)) if bright
                       else (srgb(0.12, 0.29, 0.46), srgb(0.34, 0.60, 0.76)))
        # A soft ramp: clouding is depth inside the crystal, not blotches on its surface.
        base = _ramp(tree, cloud, [(0.15, deep), (0.85, light)])
        wave = tree.nodes.new('ShaderNodeTexWave')
        wave.wave_type = 'BANDS'
        wave.bands_direction = 'DIAGONAL'
        _feed(tree, wave.inputs['Vector'], coords)
        wave.inputs['Scale'].default_value = 3.0
        wave.inputs['Distortion'].default_value = 9.0
        wave.inputs['Detail'].default_value = 3.0
        streak = _math(tree, 'MULTIPLY', _math(tree, 'POWER', wave.outputs['Fac'], 12.0), 0.20)
        base = _mix(tree, streak, base, srgb(0.93, 0.99, 1.0))
        cells = tree.nodes.new('ShaderNodeTexVoronoi')
        _feed(tree, cells.inputs['Vector'], coords)
        cells.inputs['Scale'].default_value = 14.0
        base = _mix(tree, 0.10, base, cells.outputs['Color'], 'OVERLAY')
        edge_colour, edge_gain, occlusion = srgb(0.90, 0.99, 1.0), 1.0, 0.55

    # Occlusion from every part of the weapon, so a guard shades the blade it holds.
    ao = tree.nodes.new('ShaderNodeAmbientOcclusion')
    ao.samples = 24
    ao.inputs['Distance'].default_value = 0.045
    shade = _math(tree, 'ADD', _math(tree, 'MULTIPLY', ao.outputs['AO'], occlusion), 1.0 - occlusion)
    shaded = _mix(tree, 1.0, base, _combine(tree, shade), 'MULTIPLY')

    # Worn edge: where a bevelled normal departs from the true one, the surface is on an edge.
    bevel = tree.nodes.new('ShaderNodeBevel')
    bevel.samples = 12
    bevel.inputs['Radius'].default_value = 0.0045
    geometry = tree.nodes.new('ShaderNodeNewGeometry')
    dot = tree.nodes.new('ShaderNodeVectorMath')
    dot.operation = 'DOT_PRODUCT'
    tree.links.new(bevel.outputs['Normal'], dot.inputs[0])
    tree.links.new(geometry.outputs['Normal'], dot.inputs[1])
    edge = _math(tree, 'MULTIPLY', _math(tree, 'SUBTRACT', 1.0, dot.outputs['Value']), 9.0, clamp=True)
    final = _mix(tree, _math(tree, 'MULTIPLY', edge, edge_gain), shaded, edge_colour)

    emit = tree.nodes.new('ShaderNodeEmission')
    tree.links.new(final, emit.inputs['Color'])
    output = [n for n in tree.nodes if n.type == 'OUTPUT_MATERIAL'][0]
    tree.links.new(emit.outputs['Emission'], output.inputs['Surface'])
    tree.nodes.active = tree.nodes['Atlas']


def _combine(tree, value):
    node = tree.nodes.new('ShaderNodeCombineColor')
    for i in range(3):
        tree.links.new(value, node.inputs[i])
    return node.outputs['Color']


def unpaint(mat):
    """Reduce a baked material to exactly what the exporter reads: Principled plus the atlas."""
    tree = mat.node_tree
    keep = {'Principled BSDF', 'Material Output', 'Atlas'}
    for node in list(tree.nodes):
        if node.name not in keep:
            tree.nodes.remove(node)
    tree.links.new(tree.nodes['Principled BSDF'].outputs['BSDF'], tree.nodes['Material Output'].inputs['Surface'])
    tree.links.new(tree.nodes['Atlas'].outputs['Color'], tree.nodes['Principled BSDF'].inputs['Base Color'])
    del mat['surface']


# --------------------------------------------------------------------------------------------
# Geometry
# --------------------------------------------------------------------------------------------

class Weapon:
    def __init__(self, model_id):
        self.model_id = model_id
        # Alpha starts at zero so the bake marks exactly which texels it wrote; see pad().
        self.atlas = bpy.data.images.new(model_id + '-albedo', BAKE_SIZE, BAKE_SIZE, alpha=True)
        self.atlas.generated_color = (0, 0, 0, 0)
        self.painter = Painter(model_id, self.atlas)
        self.parts = []

    def part(self, name, verts, faces, surface, index=None, smooth=False, sharp=40.0):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata([tuple(v) for v in verts], [], faces)
        mesh.update()
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(mesh)
        bm.free()
        if smooth:
            mesh.shade_smooth()
            mesh.set_sharp_from_angle(angle=math.radians(sharp))
        else:
            mesh.shade_flat()
        mesh.materials.append(self.painter.material(surface, index))
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        obj['game_node_path'] = f'attach/magenheim.{self.model_id}.visual/{name}'
        obj['game_collision'] = False
        obj['game_crystal'] = 'null'
        self.parts.append(obj)
        return obj


def loft(sections):
    """Closed surface through a list of rings. A one-point ring is an apex; an end ring is capped
    with a centre fan so non-planar ends triangulate predictably."""
    verts, faces, rings = [], [], []
    for ring in sections:
        start = len(verts)
        verts.extend(ring)
        rings.append(list(range(start, start + len(ring))))
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1:
            raise ValueError('two consecutive apexes')
        if len(a) == 1:
            faces.extend((a[0], b[j], b[(j + 1) % len(b)]) for j in range(len(b)))
        elif len(b) == 1:
            faces.extend((a[j], a[(j + 1) % len(a)], b[0]) for j in range(len(a)))
        else:
            if len(a) != len(b):
                raise ValueError('ring sizes differ')
            n = len(a)
            faces.extend((a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]) for j in range(n))
    for ring in (rings[0], rings[-1]):
        if len(ring) > 1:
            centre = sum((Vector(verts[i]) for i in ring), Vector()) / len(ring)
            verts.append(centre)
            c = len(verts) - 1
            faces.extend((ring[j], ring[(j + 1) % len(ring)], c) for j in range(len(ring)))
    return verts, faces


def ring(centre, u, v, profile):
    return [centre + a * u + b * v for a, b in profile]


X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))


def lathe(profile, sides=12, squash=1.0, centre=(0.0, 0.0), phase=0.0):
    """Surface of revolution about Z. profile: (z, r); r == 0 is an apex. squash scales Y."""
    cx, cy = centre
    sections = []
    for z, r in profile:
        if r <= 0:
            sections.append([Vector((cx, cy, z))])
            continue
        sections.append([Vector((cx + r * math.cos(phase + math.tau * j / sides),
                                 cy + squash * r * math.sin(phase + math.tau * j / sides), z))
                         for j in range(sides)])
    return loft(sections)


def wrapped_grip(z0, z1, radius, ridge, bands, squash=0.82, sides=12, flare=0.0):
    """A leather-wrapped grip: each wrap is a raised band, so the wrap reads in silhouette."""
    profile = [(z0, radius * 0.72), (z0 + 0.004, radius + flare)]
    step = (z1 - z0 - 0.008) / bands
    for i in range(bands):
        base = z0 + 0.004 + i * step
        profile += [(base + step * 0.18, radius + ridge), (base + step * 0.62, radius + ridge),
                    (base + step * 0.92, radius)]
    profile += [(z1 - 0.004, radius + flare), (z1, radius * 0.72)]
    return lathe(profile, sides, squash)


def blade(stations, sections_between=0, back=False, tip=None):
    """Edged blade along Z. Each station is (z, half_width, spine, face, groove, x_offset):

    spine  -- half-thickness at the centreline
    face   -- half-thickness where the flat meets the fuller or midrib
    groove -- half-width of the fuller (spine < face) or midrib (spine > face)
    The edge itself is a thin secondary bevel so it catches a highlight on both faces.
    back   -- single-edged: the -X side is a flat spine rather than an edge.
    tip    -- (x, z) apex; omitted means the last station closes the blade."""
    sections = []
    for z, w, spine, face, groove, dx in stations:
        edge = min(0.0028, face * 0.4)
        bevel = w * 0.26
        groove = min(groove, w * 0.45)
        right = [(w, 0.0), (w - bevel, edge + (face - edge) * 0.45), (groove, face), (0.0, spine)]
        if back:
            left = [(-groove, face), (-w, face * 0.92), (-w, 0.0)]
        else:
            left = [(-groove, face), (-(w - bevel), edge + (face - edge) * 0.45), (-w, 0.0)]
        upper = right + left
        # mirror through the blade plane
        lower = [(x, -y) for x, y in reversed(upper[1:-1])]
        profile = upper + lower
        sections.append([Vector((dx + x, y, z)) for x, y in profile])
    if tip is not None:
        sections.append([Vector((tip[0], 0.0, tip[1]))])
    return loft(sections)


def sweep(path, profile_at, up):
    """Rings perpendicular to a polyline. profile_at(k, t) -> [(a, b)] in (up, side) coordinates,
    or a single point for an apex. up is projected off each tangent to keep the frame stable."""
    sections = []
    n = len(path)
    for k, p in enumerate(path):
        tangent = (path[min(k + 1, n - 1)] - path[max(k - 1, 0)]).normalized()
        u = (up - tangent * up.dot(tangent)).normalized()
        v = tangent.cross(u)
        sections.append(ring(p, u, v, profile_at(k, k / (n - 1))))
    return loft(sections)


def chamfered(half_a, half_b, chamfer):
    """An octagonal section: a rectangle with its corners cut. (a, b) half-extents."""
    ca, cb = min(chamfer, half_a * 0.45), min(chamfer, half_b * 0.45)
    return [(half_a, -half_b + cb), (half_a, half_b - cb), (half_a - ca, half_b), (-half_a + ca, half_b),
            (-half_a, half_b - cb), (-half_a, -half_b + cb), (-half_a + ca, -half_b), (half_a - ca, -half_b)]


def diamond(half_a, half_b):
    return [(half_a, 0.0), (0.0, half_b), (-half_a, 0.0), (0.0, -half_b)]


def slab(outline, centre, thickness, rings=(1.0, 0.94, 0.72, 0.42)):
    """A plate in the XZ plane grown from a star-shaped outline toward centre, Y as thickness.

    thickness(x, z, s) is the half-thickness at scale s (1 = the outline itself), so a wedge that
    thins toward its cutting edge and a bevel along that edge are both expressed directly."""
    cx, cz = centre
    top, bottom = [], []
    for s in rings:
        pts = [(cx + s * (x - cx), cz + s * (z - cz)) for x, z in outline]
        top.append([Vector((x, thickness(x, z, s), z)) for x, z in pts])
        bottom.append([Vector((x, -thickness(x, z, s), z)) for x, z in pts])
    verts, faces = [], []

    def add(points):
        start = len(verts)
        verts.extend(points)
        return list(range(start, start + len(points)))

    top_ids = [add(r) for r in top]
    bottom_ids = [add(r) for r in bottom]
    apex_top = add([Vector((cx, thickness(cx, cz, 0.0), cz))])[0]
    apex_bottom = add([Vector((cx, -thickness(cx, cz, 0.0), cz))])[0]
    n = len(outline)
    for ids, apex in ((top_ids, apex_top), (bottom_ids, apex_bottom)):
        for a, b in zip(ids, ids[1:]):
            faces.extend((a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]) for j in range(n))
        faces.extend((ids[-1][j], ids[-1][(j + 1) % n], apex) for j in range(n))
    rim_top, rim_bottom = top_ids[0], bottom_ids[0]
    faces.extend((rim_top[j], rim_bottom[j], rim_bottom[(j + 1) % n], rim_top[(j + 1) % n]) for j in range(n))
    return verts, faces


def wedge(eye_x, reach, eye_half, edge_half=0.0028, bevel=0.55):
    """Half-thickness of an axe head: full at the eye, falling to a thin edge, with an edge bevel."""
    def thickness(x, z, s):
        body = edge_half + (eye_half - edge_half) * max(0.0, 1.0 - abs(x - eye_x) / reach) ** 1.4
        if s >= 0.999:
            return edge_half
        if s > 0.9:
            return edge_half + (body - edge_half) * bevel
        return body
    return thickness


def gem(centre, radius, depth, sides=8, axis='Y', twist=0.0):
    """A cut gem: girdle, crown and pavilion facets, pointed along axis."""
    c = Vector(centre)
    along = {'X': X, 'Y': Y, 'Z': Z}[axis]
    u = Z if axis != 'Z' else X
    v = along.cross(u)

    def circle(r, offset, extra=0.0):
        return [c + along * offset + r * (math.cos(twist + extra + math.tau * j / sides) * u
                                          + math.sin(twist + extra + math.tau * j / sides) * v)
                for j in range(sides)]
    half = math.pi / sides
    return loft([[c - along * depth], circle(radius * 0.55, -depth * 0.62, half), circle(radius, 0.0),
                 circle(radius * 0.55, depth * 0.62, half), [c + along * depth]])


def curve(points, samples):
    """Catmull-Rom through control points, returned as `samples` evenly-parameterised Vectors."""
    pts = [Vector(p) for p in points]
    pts = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    segments = len(pts) - 3
    for i in range(samples):
        t = i / (samples - 1) * segments
        k = min(int(t), segments - 1)
        f = t - k
        p0, p1, p2, p3 = pts[k:k + 4]
        out.append(0.5 * ((2 * p1) + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f
                          + (-p0 + 3 * p1 - 3 * p2 + p3) * f * f * f))
    return out


def translate(mesh, offset):
    verts, faces = mesh
    return [v + Vector(offset) for v in verts], faces


def merge(*meshes):
    verts, faces = [], []
    for v, f in meshes:
        base = len(verts)
        verts.extend(v)
        faces.extend(tuple(i + base for i in face) for face in f)
    return verts, faces


def rotate_z(mesh, angle):
    verts, faces = mesh
    c, s = math.cos(angle), math.sin(angle)
    return [Vector((v.x * c - v.y * s, v.x * s + v.y * c, v.z)) for v in verts], faces


def mirror_x(mesh):
    verts, faces = mesh
    return [Vector((-v.x, v.y, v.z)) for v in verts], faces


def band(z0, z1, radius, lip, sides=12, squash=1.0):
    """A metal ferrule with raised lips at both ends."""
    h = z1 - z0
    return lathe([(z0, radius * 0.8), (z0 + h * 0.02, radius + lip), (z0 + h * 0.22, radius + lip),
                  (z0 + h * 0.30, radius), (z0 + h * 0.70, radius), (z0 + h * 0.78, radius + lip),
                  (z0 + h * 0.98, radius + lip), (z1, radius * 0.8)], sides, squash)


def wrap(z0, z1, radius, bands):
    """Leather bound over a haft where the hand sits."""
    return wrapped_grip(z0, z1, radius, 0.003, bands, squash=0.92, sides=12, flare=0.002)


def ferrule(z0, length, radius):
    """A pointed iron butt cap."""
    return lathe([(z0, 0.0), (z0 + length * 0.25, radius * 0.55), (z0 + length * 0.65, radius * 1.05),
                  (z0 + length * 0.85, radius * 1.12), (z0 + length, radius * 1.0)], 12)


def langets(z_top, length, radius):
    """Iron straps running down the haft from a socket, one each side."""
    straps = []
    for side in (1, -1):
        x = side * (radius + 0.0025)
        straps.append(loft([[Vector((x, 0.0, z_top - length))]]
                           + [ring(Vector((x, 0.0, z)), X, Y, chamfered(0.0035, w, 0.0015))
                              for z, w in ((z_top - length * 0.8, 0.006), (z_top - length * 0.3, 0.008),
                                           (z_top, 0.008))]))
    return merge(*straps)


def haft(z0, z1, radius, squash=0.9, sides=10, swell=0.0, butt=0.0):
    """A turned wooden haft, thickening toward the butt where a knob stops the hand slipping."""
    length = z1 - z0
    profile = [(z0, radius * 0.6), (z0 + 0.006, radius + butt), (z0 + 0.030, radius + butt),
               (z0 + 0.045, radius * 0.96)]
    for f in (0.15, 0.35, 0.55, 0.75, 0.92):
        profile.append((z0 + length * f, radius * (1.0 + swell * math.sin(math.pi * f))))
    profile += [(z1 - 0.004, radius), (z1, radius * 0.7)]
    return lathe(profile, sides, squash)


# --------------------------------------------------------------------------------------------
# The ten weapons
# --------------------------------------------------------------------------------------------

def crossguard(half_span, centre_z, height, depth, droop=0.0, rise=0.0, knob=0.0):
    """A sword guard swept along X. Tips may droop (-Z) or rise (+Z) and end in a knob."""
    path = [Vector((half_span * t, 0.0, centre_z + (rise - droop) * abs(t) ** 2)) for t in
            (-1.0, -0.95, -0.85, -0.6, -0.3, 0.0, 0.3, 0.6, 0.85, 0.95, 1.0)]

    def profile(k, t):
        s = abs(2 * t - 1)
        if s >= 0.999:
            return chamfered(height * 0.25 + knob * 0.5, depth * 0.3 + knob * 0.5, 0.003)
        h = height * (1.0 - 0.45 * s) + (knob if s > 0.9 else 0.0)
        d = depth * (1.0 - 0.35 * s) + (knob if s > 0.9 else 0.0)
        return chamfered(h, d, 0.006)
    return sweep(path, profile, Z)


def sword():
    w = Weapon('crystal-weapon-sword')
    w.part('pommel', *lathe([(-0.562, 0.0), (-0.559, 0.026), (-0.551, 0.043), (-0.539, 0.051),
                            (-0.523, 0.052), (-0.507, 0.046), (-0.495, 0.034), (-0.484, 0.025),
                            (-0.472, 0.023)], 16, 0.62), 'blackmetal', smooth=True)
    w.part('grip', *wrapped_grip(-0.478, -0.072, 0.024, 0.0035, 12, flare=0.003), 'leather', smooth=True)
    w.part('guard', *crossguard(0.157, -0.060, 0.026, 0.027, rise=0.016, knob=0.006), 'blackmetal', 2)
    stations = [(-0.060, 0.058, 0.014, 0.015, 0.012, 0.0), (0.020, 0.062, 0.010, 0.017, 0.020, 0.0),
                (0.250, 0.064, 0.009, 0.016, 0.020, 0.0), (0.500, 0.060, 0.009, 0.015, 0.019, 0.0),
                (0.700, 0.052, 0.010, 0.014, 0.016, 0.0), (0.790, 0.044, 0.012, 0.013, 0.010, 0.0),
                (0.870, 0.032, 0.011, 0.011, 0.006, 0.0), (0.940, 0.017, 0.008, 0.008, 0.003, 0.0)]
    w.part('blade', *blade(stations, tip=(0.0, 0.998)), 'crystal')
    # A bright vein set into the fuller: proud of the fuller floor, below the blade faces.
    w.part('blade-core', *loft([[Vector((0, 0, -0.020))],
                                ring(Vector((0, 0, 0.060)), X, Y, diamond(0.010, 0.012)),
                                ring(Vector((0, 0, 0.400)), X, Y, diamond(0.011, 0.012)),
                                ring(Vector((0, 0, 0.690)), X, Y, diamond(0.009, 0.012)),
                                [Vector((0, 0, 0.800))]]), 'crystal-bright')
    w.part('rainbow-inlay', *gem((0, 0, -0.060), 0.017, 0.036), 'rainbow', 4)
    return w


def greatsword():
    w = Weapon('crystal-weapon-greatsword')
    w.part('twohand-grip', *merge(wrapped_grip(-0.556, -0.300, 0.026, 0.0035, 7, flare=0.002),
                                  lathe([(-0.304, 0.026), (-0.298, 0.033), (-0.282, 0.033),
                                         (-0.276, 0.026)], 12, 0.82),
                                  wrapped_grip(-0.280, -0.018, 0.026, 0.0035, 7, flare=0.002)),
           'leather', smooth=True)
    # A cup the gem seats into; its lower rim sits inside the gem so the gem reads as set.
    w.part('pommel-collar', *lathe([(-0.572, 0.040), (-0.566, 0.049), (-0.552, 0.050), (-0.540, 0.042),
                                   (-0.532, 0.034)], 12), 'blackmetal', 3, smooth=True)
    # Faceted crystal pommel, hanging point-down below the grip (the hand-authored design).
    w.part('tip-core', *loft([[Vector((0, 0, -0.739))],
                              ring(Vector((0, 0, -0.662)), X, Y, [(0.044 * math.cos(a), 0.044 * math.sin(a)) for a in [math.tau * (j + 0.5) / 6 for j in range(6)]]),
                              ring(Vector((0, 0, -0.620)), X, Y, [(0.052 * math.cos(a), 0.052 * math.sin(a)) for a in [math.tau * j / 6 for j in range(6)]]),
                              ring(Vector((0, 0, -0.540)), X, Y, [(0.038 * math.cos(a), 0.038 * math.sin(a)) for a in [math.tau * (j + 0.5) / 6 for j in range(6)]]),
                              [Vector((0, 0, -0.470))]]), 'crystal-bright')
    # Central guard block, widening toward the blade as the original trapezoid did.
    w.part('guard', *loft([ring(Vector((0, 0, z)), X, Y, chamfered(hx, hy, 0.008))
                           for z, hx, hy in ((-0.087, 0.078, 0.030), (-0.080, 0.085, 0.034),
                                             (-0.030, 0.095, 0.037), (-0.023, 0.090, 0.034))]),
           'blackmetal', 2)
    quillons = sweep([Vector((0.284 * t, 0.0, -0.013 - 0.012 * abs(t) ** 2.2)) for t in
                      (-1.0, -0.96, -0.85, -0.6, -0.3, 0.0, 0.3, 0.6, 0.85, 0.96, 1.0)],
                     lambda k, t: chamfered(0.010 + 0.008 * abs(2 * t - 1) ** 3 if abs(2 * t - 1) < 0.99 else 0.006,
                                            0.034 + 0.011 * abs(2 * t - 1) ** 3 if abs(2 * t - 1) < 0.99 else 0.020,
                                            0.004), Z)
    ricasso = loft([ring(Vector((0, 0, z)), X, Y, chamfered(hx, 0.031, 0.006)) for z, hx in
                    ((0.000, 0.066), (0.045, 0.069), (0.200, 0.066), (0.250, 0.046), (0.276, 0.020))]
                   + [[Vector((0, 0, 0.288))]])
    w.part('guard-silver', *merge(quillons, ricasso), 'silver')
    stations = [(-0.043, 0.086, 0.016, 0.017, 0.030, 0.0), (0.300, 0.094, 0.011, 0.020, 0.028, 0.0),
                (0.700, 0.090, 0.011, 0.019, 0.027, 0.0), (1.000, 0.080, 0.012, 0.017, 0.022, 0.0),
                (1.150, 0.066, 0.014, 0.015, 0.012, 0.0), (1.250, 0.046, 0.013, 0.013, 0.006, 0.0),
                (1.330, 0.022, 0.009, 0.009, 0.003, 0.0)]
    w.part('great-blade', *blade(stations, tip=(0.0, 1.394)), 'crystal')
    return w


def knife():
    w = Weapon('crystal-weapon-knife')
    w.part('grip', *wrapped_grip(-0.372, -0.024, 0.019, 0.003, 8, squash=0.78, flare=0.004),
           'leather', smooth=True)
    w.part('inlay', *gem((0, 0, -0.372), 0.020, 0.018, 6, axis='Z'), 'rainbow', 6)
    w.part('guard', *crossguard(0.070, -0.016, 0.012, 0.018, droop=0.004, knob=0.003), 'blackmetal')
    # Seax: straight spine on -X, the edge sweeping up to meet it at the point.
    stations = [(-0.016, 0.040, 0.010, 0.010, 0.010, 0.010), (0.100, 0.046, 0.008, 0.010, 0.014, 0.016),
                (0.260, 0.050, 0.008, 0.010, 0.014, 0.020), (0.380, 0.046, 0.008, 0.009, 0.012, 0.016),
                (0.450, 0.036, 0.007, 0.008, 0.008, 0.006)]
    w.part('blade', *blade(stations, back=True, tip=(-0.028, 0.528)), 'crystal')
    edge = [(0.050, 0.000), (0.062, 0.100), (0.070, 0.260), (0.062, 0.380), (0.042, 0.450),
            (0.010, 0.500), (-0.026, 0.536)]
    w.part('tip', *edge_strip(edge, 0, len(edge) - 1, (-0.030, 0.250), inset=0.014, lip=0.005, half=0.0055),
           'crystal-bright')
    return w


def axe_head(eye_x, eye_top, eye_bottom, reach, beard):
    """A bearded axe outline in (x, z), ordered around the head."""
    top, bottom = eye_top, eye_bottom
    return [(eye_x, top), (eye_x + reach * 0.35, top - 0.012), (eye_x + reach * 0.70, top + 0.004),
            (eye_x + reach * 0.96, top + 0.040), (eye_x + reach * 1.03, top - 0.040),
            (eye_x + reach * 1.05, (top + bottom) / 2 - beard * 0.2),
            (eye_x + reach * 1.02, bottom - beard * 0.55), (eye_x + reach * 0.92, bottom - beard),
            (eye_x + reach * 0.70, bottom - beard * 0.62), (eye_x + reach * 0.45, bottom - beard * 0.20),
            (eye_x + reach * 0.20, bottom + 0.004), (eye_x, bottom)]


def edge_strip(outline, first, last, centre, inset=0.026, lip=0.010, half=0.0065):
    """A bright crystal edge laid along part of an outline, proud of the wedge on both faces."""
    path = curve([Vector((x, 0.0, z)) for x, z in outline[first:last + 1]], 14)
    # sweep() puts its side axis on tangent x up; the lip must point away from the head.
    mid = len(path) // 2
    side = (path[mid + 1] - path[mid - 1]).normalized().cross(Y)
    outward = 1.0 if side.dot(path[mid] - Vector((centre[0], 0.0, centre[1]))) > 0 else -1.0

    def profile(k, t):
        taper = math.sin(math.pi * min(max(t, 0.04), 0.96)) ** 0.5
        return [(0.0, outward * lip * taper), (half * taper, 0.0),
                (0.0, -outward * inset * taper), (-half * taper, 0.0)]
    return sweep(path, profile, Y)


def axe():
    w = Weapon('crystal-weapon-axe')
    w.part('haft', *haft(-0.544, 0.469, 0.028, swell=0.06, butt=0.006), 'timber', smooth=True)
    w.part('lower-band', *band(-0.473, -0.397, 0.031, 0.004, 12, 0.9), 'blackmetal', smooth=True)
    w.part('grip-wrap', *wrap(-0.395, -0.120, 0.029, 9), 'leather', smooth=True)
    w.part('head-band', *band(0.410, 0.612, 0.038, 0.005, 12, 0.95), 'blackmetal', 2, smooth=True)
    outline = axe_head(0.020, 0.600, 0.420, 0.400, 0.200)
    centre = (0.140, 0.500)
    w.part('axe-head', *slab(outline, centre, wedge(0.02, 0.44, 0.034)), 'crystal')
    w.part('edge', *edge_strip(outline, 3, 7, centre), 'crystal-bright')
    w.part('head-inlay', *gem((0.115, 0.0, 0.505), 0.024, 0.042), 'rainbow', 2)
    return w


def battleaxe():
    w = Weapon('crystal-weapon-battleaxe')
    w.part('long-haft', *haft(-0.806, 0.581, 0.034, swell=0.05, butt=0.008), 'timber', smooth=True)
    w.part('band-low', *band(-0.690, -0.615, 0.037, 0.005, 12, 0.9), 'blackmetal', smooth=True)
    w.part('grip-wrap', *wrap(-0.612, -0.180, 0.035, 13), 'leather', smooth=True)
    w.part('band-mid', *band(0.300, 0.370, 0.037, 0.005, 12, 0.9), 'blackmetal', 2, smooth=True)
    w.part('band-high', *band(0.380, 0.440, 0.037, 0.005, 12, 0.9), 'blackmetal', 3, smooth=True)
    # Crescent heads: horns sweep up and down away from a narrow neck, as vanilla's do.
    right = [(0.030, 0.605), (0.150, 0.590), (0.270, 0.640), (0.345, 0.748), (0.420, 0.690),
             (0.452, 0.600), (0.455, 0.500), (0.435, 0.410), (0.360, 0.330), (0.270, 0.420),
             (0.150, 0.480), (0.030, 0.475)]
    centre = (0.240, 0.540)
    thickness = wedge(0.03, 0.46, 0.040)
    w.part('right-head', *slab(right, centre, thickness), 'crystal')
    w.part('left-head', *mirror_x(slab(right, centre, thickness)), 'crystal', 1)
    w.part('right-edge', *edge_strip(right, 3, 8, centre), 'crystal-bright')
    w.part('left-edge', *mirror_x(edge_strip(right, 3, 8, centre)), 'crystal-bright', 1)
    w.part('head-core', *sweep([Vector((x, 0.0, 0.540)) for x in (-0.19, -0.17, -0.10, 0.0, 0.10, 0.17, 0.19)],
                               lambda k, t: chamfered(0.050 + 0.040 * (1 - abs(2 * t - 1)) if 0 < t < 1 else 0.040,
                                                      0.048 + 0.008 * (1 - abs(2 * t - 1)) if 0 < t < 1 else 0.034,
                                                      0.010), Z), 'silver')
    w.part('prismatic-eye', *gem((0.0, 0.0, 0.540), 0.030, 0.074), 'rainbow', 5)
    return w


def mace():
    w = Weapon('crystal-weapon-mace')
    w.part('haft', *haft(-0.540, 0.360, 0.030, swell=0.05), 'timber', smooth=True)
    w.part('grip-wrap', *wrap(-0.505, -0.200, 0.031, 10), 'leather', smooth=True)
    w.part('pommel', *lathe([(-0.608, 0.0), (-0.604, 0.030), (-0.592, 0.046), (-0.574, 0.048),
                            (-0.556, 0.040), (-0.536, 0.036), (-0.520, 0.032)], 12), 'blackmetal', smooth=True)
    w.part('neck', *merge(band(0.270, 0.405, 0.042, 0.007, 12), band(0.200, 0.235, 0.034, 0.004, 12)),
           'blackmetal', 2, smooth=True)
    # Faceted crystal core with a crowning point, flanges radiating from it.
    w.part('head-core', *lathe([(0.380, 0.040), (0.420, 0.078), (0.500, 0.092), (0.580, 0.088),
                               (0.640, 0.062), (0.672, 0.030), (0.696, 0.0)], 8, phase=math.pi / 8),
           'crystal-bright')
    flange = slab([(0.060, 0.400), (0.110, 0.420), (0.170, 0.455), (0.210, 0.500), (0.178, 0.530),
                   (0.210, 0.575), (0.160, 0.625), (0.100, 0.655), (0.060, 0.645)], (0.100, 0.530),
                  lambda x, z, s: 0.004 if s > 0.99 else (0.009 if s > 0.9 else 0.015))
    # slab lies in XZ with Y thickness; a flange at angle a is that plate turned about Z.
    # Deep crystal flanges with bright worn edges around a glowing core; three prismatic between.
    # The old bright-and-prismatic set gave the head no dark value at all and it read as a pale bulb.
    materials = ['crystal', 'rainbow', 'crystal', 'rainbow', 'crystal', 'rainbow']
    for k in range(6):
        index = k if materials[k] == 'rainbow' else (None if k == 0 else k)
        w.part(f'striker-{k}', *rotate_z(flange, math.radians(60 * k)), materials[k], index)
    return w


def spear():
    w = Weapon('crystal-weapon-spear')
    w.part('shaft', *haft(-0.844, 0.544, 0.022, squash=1.0, swell=0.04, butt=0.004), 'timber', smooth=True)
    w.part('grip-wrap', *wrap(-0.170, 0.170, 0.023, 10), 'leather', smooth=True)
    w.part('butt-cap', *merge(ferrule(-0.844, 0.070, 0.024), langets(0.450, 0.150, 0.023)), 'blackmetal', 2,
           smooth=True, sharp=35)
    w.part('socket', *lathe([(0.442, 0.026), (0.450, 0.032), (0.470, 0.032), (0.478, 0.028),
                            (0.560, 0.034), (0.570, 0.040), (0.582, 0.040), (0.592, 0.034),
                            (0.607, 0.022)], 12), 'blackmetal', smooth=True)
    stations = [(0.560, 0.022, 0.024, 0.014, 0.010, 0.0), (0.640, 0.060, 0.020, 0.010, 0.010, 0.0),
                (0.760, 0.077, 0.018, 0.009, 0.010, 0.0), (0.880, 0.064, 0.016, 0.008, 0.009, 0.0),
                (0.980, 0.040, 0.013, 0.007, 0.007, 0.0), (1.060, 0.018, 0.010, 0.006, 0.004, 0.0)]
    w.part('spearhead', *blade(stations, tip=(0.0, 1.118)), 'crystal')
    w.part('spear-tip', *blade([(0.980, 0.044, 0.016, 0.010, 0.006, 0.0), (1.060, 0.024, 0.013, 0.008, 0.004, 0.0),
                                (1.110, 0.009, 0.007, 0.005, 0.002, 0.0)], tip=(0.0, 1.139)), 'crystal-bright')
    w.part('prism-bind', *gem((0.0, 0.0, 0.525), 0.018, 0.050), 'rainbow', 3)
    return w


def atgeir():
    w = Weapon('crystal-weapon-atgeir')
    w.part('pole', *haft(-0.941, 0.671, 0.026, squash=0.95, swell=0.04, butt=0.006), 'timber', smooth=True)
    w.part('grip-wrap', *wrap(-0.190, 0.190, 0.027, 11), 'leather', smooth=True)
    w.part('butt-cap', *merge(ferrule(-0.941, 0.080, 0.028), langets(0.556, 0.170, 0.027)), 'blackmetal', 2,
           smooth=True, sharp=35)
    w.part('socket', *lathe([(0.548, 0.030), (0.556, 0.038), (0.574, 0.038), (0.582, 0.033),
                            (0.680, 0.040), (0.692, 0.050), (0.712, 0.050), (0.728, 0.036)], 12),
           'blackmetal', smooth=True)
    stations = [(0.700, 0.030, 0.026, 0.016, 0.010, 0.0), (0.800, 0.080, 0.022, 0.012, 0.012, 0.0),
                (0.950, 0.086, 0.020, 0.011, 0.012, 0.0), (1.080, 0.064, 0.017, 0.010, 0.010, 0.0),
                (1.190, 0.036, 0.014, 0.008, 0.006, 0.0), (1.260, 0.016, 0.010, 0.006, 0.004, 0.0)]
    w.part('main-point', *blade(stations, tip=(0.0, 1.300)), 'crystal')
    w.part('tip', *blade([(1.180, 0.042, 0.017, 0.010, 0.006, 0.0), (1.260, 0.021, 0.013, 0.008, 0.004, 0.0),
                          (1.300, 0.008, 0.007, 0.005, 0.002, 0.0)], tip=(0.0, 1.319)), 'crystal-bright')
    # Hooked side lugs sweeping outward then up -- the atgeir's readable profile.
    hook = curve([(0.04, 0.0, 0.715), (0.14, 0.0, 0.705), (0.23, 0.0, 0.735), (0.285, 0.0, 0.800),
                  (0.294, 0.0, 0.841)], 12)

    def hook_profile(k, t):
        if t >= 0.999:
            return [(0.0, 0.0)]
        return diamond(0.020 * (1 - 0.55 * t), 0.030 * (1 - 0.72 * t))
    right = sweep(hook, hook_profile, Y)
    w.part('right-wing', *right, 'crystal', 1)
    w.part('left-wing', *mirror_x(right), 'crystal', 2)
    w.part('wing-core', *sweep([Vector((x, 0.0, 0.715)) for x in (-0.13, -0.11, -0.06, 0.0, 0.06, 0.11, 0.13)],
                               lambda k, t: chamfered(0.024 if 0 < t < 1 else 0.016, 0.046 if 0 < t < 1 else 0.030, 0.008),
                               Z), 'silver')
    w.part('rainbow-bind', *gem((0.0, 0.0, 0.640), 0.020, 0.052), 'rainbow', 4)
    return w


def bow():
    w = Weapon('crystal-weapon-bow')
    # The riser sits furthest forward (+X) and the limbs sweep back to nocks at -X. The string runs
    # nock to nock outside the limbs; the old model's string passed straight through them.
    upper = curve([(0.0, 0.0, 0.17), (-0.030, 0.0, 0.36), (-0.095, 0.0, 0.56), (-0.165, 0.0, 0.74),
                   (-0.215, 0.0, 0.870), (-0.232, 0.0, 0.905)], 25)
    width = lambda t: 0.040 - 0.020 * t
    depth = lambda t: 0.020 - 0.010 * t

    def limb(segment, t0, t1):
        pts = [p for i, p in enumerate(segment)]
        n = len(pts) - 1
        return sweep(pts, lambda k, t: [(math.cos(a) * width(t0 + (t1 - t0) * t),
                                         math.sin(a) * depth(t0 + (t1 - t0) * t))
                                        for a in [math.tau * j / 12 for j in range(12)]], Y)

    cuts = [(0, 8, 'crystal', None, 'inner'), (8, 16, 'rainbow', 4, 'mid'), (16, 24, 'rainbow', 6, 'outer')]
    lower_index = {'mid': 1, 'outer': 3}
    for a, b, surface, index, name in cuts:
        segment = upper[a:b + 1]
        mesh = limb(segment, a / 24, b / 24)
        w.part(f'upper-{name}', *mesh, surface, index, smooth=True, sharp=55)
        flipped = ([Vector((v.x, v.y, -v.z)) for v in mesh[0]], mesh[1])
        w.part(f'lower-{name}', *flipped, surface,
               lower_index.get(name) if surface == 'rainbow' else 1, smooth=True, sharp=55)
    riser = loft([ring(Vector((0.004, 0, z)), X, Y, chamfered(hx, hy, 0.010)) for z, hx, hy in
                  ((-0.200, 0.016, 0.022), (-0.170, 0.022, 0.030), (-0.080, 0.030, 0.034),
                   (0.0, 0.034, 0.036), (0.080, 0.030, 0.034), (0.170, 0.022, 0.030), (0.200, 0.016, 0.022))])
    w.part('riser', *riser, 'leather', smooth=True, sharp=50)
    w.part('grip-band', *merge(band(0.052, 0.084, 0.034, 0.004, 12), band(-0.084, -0.052, 0.034, 0.004, 12)),
           'blackmetal', smooth=True)
    # Nock caps: bright crystal points at the limb tips, which also seat the string.
    tip = upper[-1]
    cap = gem((tip.x, 0.0, tip.z), 0.017, 0.024, 6, axis='Z')
    w.part('upper-cap', *cap, 'crystal-bright')
    w.part('lower-cap', *([Vector((v.x, v.y, -v.z)) for v in cap[0]], cap[1]), 'crystal-bright', 1)
    string_x = tip.x
    string = lathe([(0.0, 0.0028), (tip.z - 0.030, 0.0028)], 6, centre=(string_x, 0.0))
    w.part('string-upper', *string, 'blackmetal', 2, smooth=True)
    w.part('string-lower', *([Vector((v.x, v.y, -v.z)) for v in string[0]], string[1]), 'blackmetal', 3, smooth=True)
    return w


def crossbow():
    w = Weapon('crystal-weapon-crossbow')
    # Tiller: deep at the butt, dropping into a shoulder stock, slim toward the prod. Y is up.
    stations = [(-0.488, 0.030, -0.066, 0.040), (-0.460, 0.034, -0.068, 0.046), (-0.330, 0.032, -0.060, 0.050),
                (-0.200, 0.026, -0.030, 0.052), (-0.060, 0.026, -0.022, 0.056), (0.200, 0.024, -0.020, 0.056),
                (0.400, 0.022, -0.018, 0.054), (0.438, 0.018, -0.012, 0.046)]
    tiller = loft([ring(Vector((0, (lo + hi) / 2, z)), X, Y, chamfered(hx, (hi - lo) / 2, 0.009))
                   for z, hx, lo, hi in stations])
    w.part('stock', *tiller, 'timber')
    trigger = loft([ring(Vector((0, y, z)), X, Z, chamfered(0.006, 0.010, 0.003)) for y, z in
                    ((-0.020, -0.070), (-0.045, -0.080), (-0.062, -0.090))])
    w.part('trigger', *trigger, 'blackmetal', 2)
    rail = loft([ring(Vector((0, 0.076, z)), X, Y, chamfered(0.022, 0.020, 0.006)) for z in
                 (-0.379, -0.360, 0.412, 0.430)])
    nut = loft([ring(Vector((0, 0.094, z)), X, Y, chamfered(0.026, 0.012, 0.005)) for z in (-0.080, -0.030)])
    w.part('stock-spine', *merge(rail, nut), 'blackmetal')
    w.part('prism-track', *loft([ring(Vector((0, 0.097, z)), X, Y, chamfered(0.007, 0.006, 0.002)) for z in
                                 (-0.020, 0.0, 0.262, 0.282)]), 'rainbow', 5)
    w.part('prod-core', *sweep([Vector((x, 0.060, 0.360)) for x in (-0.15, -0.13, -0.07, 0.0, 0.07, 0.13, 0.15)],
                               lambda k, t: chamfered(0.036 if 0 < t < 1 else 0.026, 0.040 if 0 < t < 1 else 0.030, 0.008),
                               Z), 'silver')
    # Prod limbs sweep outward and back toward the tips, where the string is strung.
    path = curve([(0.12, 0.060, 0.372), (0.28, 0.060, 0.378), (0.44, 0.060, 0.352), (0.56, 0.060, 0.315),
                  (0.600, 0.060, 0.300)], 16)

    def limb_profile(k, t):
        return [(math.cos(a) * (0.040 - 0.016 * t), math.sin(a) * (0.022 - 0.008 * t))
                for a in [math.tau * j / 10 for j in range(10)]]
    right = sweep(path, limb_profile, Y)
    w.part('right-limb', *right, 'crystal', 1, smooth=True, sharp=55)
    w.part('left-limb', *mirror_x(right), 'crystal', smooth=True, sharp=55)
    tip = gem((0.618, 0.060, 0.296), 0.020, 0.052, 6, axis='X')
    w.part('right-tip', *tip, 'crystal-bright', 1)
    w.part('left-tip', *mirror_x(tip), 'crystal-bright')
    w.part('bowstring', *sweep([Vector((x, y, 0.300)) for x, y in ((-0.600, 0.060), (-0.030, 0.099),
                                                                     (0.030, 0.099), (0.600, 0.060))],
                               lambda k, t: chamfered(0.0028, 0.0028, 0.001), Z), 'blackmetal', 3, smooth=True)
    return w


WEAPONS = {
    'crystal-weapon-sword': sword, 'crystal-weapon-greatsword': greatsword,
    'crystal-weapon-knife': knife, 'crystal-weapon-axe': axe,
    'crystal-weapon-battleaxe': battleaxe, 'crystal-weapon-mace': mace,
    'crystal-weapon-spear': spear, 'crystal-weapon-atgeir': atgeir,
    'crystal-weapon-bow': bow, 'crystal-weapon-crossbow': crossbow,
}


# --------------------------------------------------------------------------------------------
# UVs, bake, save
# --------------------------------------------------------------------------------------------

def unwrap(parts):
    """One shared, non-overlapping layout for every part of the weapon."""
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.data.uv_layers.new(name='UVMap')
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(52), island_margin=0.0, area_weight=0.0,
                             correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in parts:
        chunk(obj)
    bpy.ops.object.mode_set(mode='EDIT')
    # With selection sync on, every selected face's UVs are packed; no UV editor context needed.
    bpy.context.scene.tool_settings.use_uv_select_sync = True
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.pack_islands(rotate=True, margin_method='FRACTION', margin=ISLAND_GAP / BAKE_SIZE)
    bpy.ops.object.mode_set(mode='OBJECT')


def chunk(obj):
    """Cut long islands into CHUNK-length pieces before packing.

    Smart projection leaves a pole or blade as a few full-length strips, and packing scales the whole
    atlas so the longest one fits: one 1.6m haft set the texel density for everything else on the
    atlas. UVs are per face corner, so moving a group of faces apart is enough to make it its own
    island. The bake is evaluated in 3D, so these extra seams carry no pattern discontinuity."""
    mesh = obj.data
    corners = [v.co for v in mesh.vertices]
    extent = [max(c[i] for c in corners) - min(c[i] for c in corners) for i in range(3)]
    axis = extent.index(max(extent))
    if extent[axis] <= CHUNK:
        return
    low = min(c[axis] for c in corners)
    uv = mesh.uv_layers.active.data
    for poly in mesh.polygons:
        k = int((poly.center[axis] - low) / CHUNK)
        for li in poly.loop_indices:
            uv[li].uv.x += 4.0 * k


def uv_overlap(parts, size=256):
    """Fraction of covered texels claimed by more than one triangle. Packing should make it ~0."""
    grid = bytearray(size * size)
    for obj in parts:
        mesh = obj.data
        mesh.calc_loop_triangles()
        uv = mesh.uv_layers.active.data
        for tri in mesh.loop_triangles:
            a, b, c = (uv[i].uv * size for i in tri.loops)
            area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)
            if abs(area) < 1e-9:
                continue
            for py in range(max(0, int(min(a.y, b.y, c.y))), min(size, int(max(a.y, b.y, c.y)) + 1)):
                for px in range(max(0, int(min(a.x, b.x, c.x))), min(size, int(max(a.x, b.x, c.x)) + 1)):
                    x, y = px + 0.5, py + 0.5
                    w0 = ((b.x - x) * (c.y - y) - (c.x - x) * (b.y - y)) / area
                    w1 = ((c.x - x) * (a.y - y) - (a.x - x) * (c.y - y)) / area
                    if w0 > 1e-4 and w1 > 1e-4 and 1 - w0 - w1 > 1e-4:
                        grid[py * size + px] = min(255, grid[py * size + px] + 1)
    covered = sum(1 for g in grid if g)
    return sum(1 for g in grid if g > 1) / max(1, covered), covered / (size * size)


def bake(weapon):
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = BAKE_SAMPLES
    scene.cycles.use_denoising = False
    materials = {obj.data.materials[0] for obj in weapon.parts}
    for mat in materials:
        paint(mat)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in weapon.parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = weapon.parts[0]
    bpy.ops.object.bake(type='EMIT', margin=BAKE_MARGIN, margin_type='EXTEND', use_clear=True)
    for mat in materials:
        unpaint(mat)
    pad(weapon.atlas)
    weapon.atlas.scale(ATLAS, ATLAS)
    weapon.atlas.pack()


def pad(image):
    """Grow every island outward until the whole atlas is covered, then make it opaque.

    Unwritten texels are black, and mip levels average them into island borders, which reads as dark
    seams at distance. Filling each gutter from its nearest island keeps every mip level clean."""
    size = image.size[0]
    pixels = numpy.empty(size * size * 4, dtype=numpy.float32)
    image.pixels.foreach_get(pixels)
    pixels = pixels.reshape(size, size, 4)
    filled = pixels[:, :, 3] > 0.5
    colour = pixels[:, :, :3] * filled[:, :, None]
    for _ in range(size):
        if filled.all():
            break
        total = numpy.zeros_like(colour)
        count = numpy.zeros(filled.shape, dtype=numpy.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            total += numpy.roll(colour, (dy, dx), axis=(0, 1))
            count += numpy.roll(filled, (dy, dx), axis=(0, 1))
        grow = ~filled & (count > 0)
        colour[grow] = total[grow] / count[grow][:, None]
        filled |= grow
    pixels[:, :, :3] = colour
    pixels[:, :, 3] = 1.0
    image.pixels.foreach_set(pixels.ravel())


def author(model_id):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    weapon = WEAPONS[model_id]()
    scene = bpy.context.scene
    scene['model_id'] = model_id
    scene['runtime_lights'] = '[]'
    scene['art_revision'] = LIBRARY_REVISION
    scene['surface_finish'] = '2'
    scene['weapon_authoring'] = REVISION

    lows = [min((o.matrix_world @ v.co).z for v in o.data.vertices) for o in weapon.parts]
    highs = [max((o.matrix_world @ v.co).z for v in o.data.vertices) for o in weapon.parts]
    low, high = ENVELOPE[model_id]
    if abs(min(lows) - low) > ENVELOPE_TOLERANCE or abs(max(highs) - high) > ENVELOPE_TOLERANCE:
        raise ValueError(f'{model_id}: Z envelope {min(lows):.3f}..{max(highs):.3f} left the confirmed '
                         f'{low:.3f}..{high:.3f}; HeldModelAlignment trims assume it')

    unwrap(weapon.parts)
    overlap, coverage = uv_overlap(weapon.parts)
    if overlap > 0.01:
        raise ValueError(f'{model_id}: {overlap:.1%} of the atlas is claimed by two triangles')
    bake(weapon)

    triangles = sum(len(o.data.loop_triangles) for o in weapon.parts)
    bpy.context.preferences.filepaths.save_version = 0
    target = SOURCE / (model_id + '.blend')
    bpy.ops.wm.save_as_mainfile(filepath=str(target), compress=True)
    print(f'AUTHORED {model_id} parts={len(weapon.parts)} triangles={triangles} '
          f'uv_coverage={coverage:.0%} uv_overlap={overlap:.2%}', flush=True)


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    unknown = [a for a in args if a not in WEAPONS]
    if unknown:
        raise SystemExit('Unknown crystal weapon(s): ' + ', '.join(unknown))
    for model_id in args or WEAPONS:
        author(model_id)


main()
