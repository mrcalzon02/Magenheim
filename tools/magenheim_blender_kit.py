"""Shared Blender authoring kit: designed-form geometry, one-atlas UVs and a painted EMIT bake.

Imported by the Blender author tools (tools/author-crystal-weapons.py, tools/author-underworld-
surtlings.py). Every generator that imports it lists it under `inputs` in
assets/generated.manifest.json, so a change here marks their outputs stale.

Geometry is returned as (verts, faces) pairs so builders compose freely; closed lofts are capped
with centre fans so non-planar ends triangulate predictably. Surfaces are painted as node graphs,
evaluated in 3D and baked, so UV seams carry no pattern discontinuity.
"""
import bmesh  # noqa: F401  (kept for tools that import * from here)
import bpy
import math

import numpy
from mathutils import Vector


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


# Every part bakes into one atlas in one call, and Blender applies the bake margin per object, so
# one part's margin overwrites any neighbouring island it can reach -- a white crystal edge's margin
# painted streaks down the hafts beside it. The bake therefore writes no margin at all, each part
# touching only its own texels, and pad() grows every island into its gutter afterwards.
ISLAND_GAP = 6


BAKE_MARGIN = 0


def srgb(r, g, b):
    def lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (lin(r), lin(g), lin(b), 1.0)


# intent -> exported shading. Base colour is always white: the baked albedo carries the colour,
# and ModelAssets multiplies the two. Emission is kept to the accent intents; a whole blade that
# glows uniformly loses its shading and reads flat, which is most of why the family read pale.


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
    mat.pop('surface', None)


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


def finish(tree, base, edge_colour, edge_gain, occlusion, ao_distance=0.045, edge_radius=0.0045):
    """Darken a pattern by occlusion, lift it at worn edges, and route it to an emission output
    for an EMIT bake. The whole assembly contributes occlusion, so a guard shades the blade it holds."""
    ao = tree.nodes.new('ShaderNodeAmbientOcclusion')
    ao.samples = 24
    ao.inputs['Distance'].default_value = ao_distance
    shade = _math(tree, 'ADD', _math(tree, 'MULTIPLY', ao.outputs['AO'], occlusion), 1.0 - occlusion)
    shaded = _mix(tree, 1.0, base, _combine(tree, shade), 'MULTIPLY')

    # Worn edge: where a bevelled normal departs from the true one, the surface is on an edge.
    bevel = tree.nodes.new('ShaderNodeBevel')
    bevel.samples = 12
    bevel.inputs['Radius'].default_value = edge_radius
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


def bake_atlas(parts, atlas, paint):
    """Bake every part's painted material into one shared atlas, then reduce each material to
    Principled plus the atlas, pad the gutters and filter down to the shipped size."""
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = BAKE_SAMPLES
    scene.cycles.use_denoising = False
    materials = {obj.data.materials[0] for obj in parts}
    for mat in materials:
        paint(mat)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.bake(type='EMIT', margin=BAKE_MARGIN, margin_type='EXTEND', use_clear=True)
    for mat in materials:
        unpaint(mat)
    pad(atlas)
    atlas.scale(ATLAS, ATLAS)
    atlas.pack()
