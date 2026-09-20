"""Re-author the Conclave's stones with carved detail.

    tools/blender.ps1 author-underworld-conclave-stones

The nine Deepstones, the Descent Monolith and the centre dais are the first thing a player sees on
arriving in the Underworld, and they were primitives: create-underworld-models.py built the standing
stone as an eight-sided chamfered prism and the dais as a 48-sided cylinder with two small steps.
0.0.61 chamfered their edges, which helped them catch light, but a chamfered box is still a box.
Vanilla Valheim's standing stones and runestones are not: they lean, they taper unevenly, their
crowns are broken rather than cut flat, and they carry a recessed panel with relief inside it. That
is what makes them read as placed by somebody rather than extruded.

Three models, one object and one material each, because UnderworldWorldCenterRegistrar loads them
through ModelAssets.LoadSingleMesh, which requires exactly one part:

  underworld-standing-stone    a Deepstone: leaning tapered slab, broken crown, carved panel, plinth
  underworld-descent-monolith  the centre pillar: four-sided, fissured, notched crown
  underworld-dais              the floor: three stepped rings, raised rim, radial spokes

**Scale convention.** The registrar scales each mesh by a world size (2.1 x 7.4 x 1.25 for a
Deepstone, 2.8 x 8.2 x 2.8 for the monolith, 17 x 0.9 x 17 for the dais) and the shipped models are
unit cubes spanning -0.5..0.5, so anything authored in unit space is stretched by up to 5.9x and
carved detail with it. Everything here is therefore drawn in metres at roughly the declared aspect
and fitted axis-by-axis into that unit box on the way out, so each piece lands at exactly the world
size the registrar declares rather than at whatever the plinth and chips happened to add -- and so
the carved panel and the relief bars can be reasoned about at the size a player actually sees them.
The monolith gets its own model for the same reason: it was sharing the Deepstone mesh at a 2.24x
different depth ratio, which was tolerable on a plain prism and would not be on a carved one.

Composite solids are appended independently and each is closed, which is what
verify-model-geometry.py adjudicates: it welds by position and requires positive signed volume per
closed surface. Solids therefore interpenetrate rather than meet exactly, so no two of them share a
vertex position and every edge stays paired within its own solid.
"""
import bpy
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
TEXTURE_SIZE = 256

ROCK_U = (0.04, 0.46)        # left of the map is weathered rock
CARVED_U = (0.54, 0.98)      # right of the map is dressed/carved surface


class MeshBuilder:
    def __init__(self):
        self.vertices, self.faces, self.uvs = [], [], []

    def tri(self, points, uvs):
        base = len(self.vertices)
        self.vertices.extend(points)
        self.uvs.extend(uvs)
        self.faces.append((base, base + 1, base + 2))

    def signed_volume(self):
        total = 0.0
        for face in self.faces:
            a, b, c = (self.vertices[i] for i in face)
            total += (a[0] * (b[1] * c[2] - b[2] * c[1])
                      - a[1] * (b[0] * c[2] - b[2] * c[0])
                      + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0
        return total


def append_solid(target, solid):
    reverse = solid.signed_volume() < 0.0
    for face in solid.faces:
        points = [solid.vertices[i] for i in face]
        uvs = [solid.uvs[i] for i in face]
        if reverse:
            points = [points[0], points[2], points[1]]
            uvs = [uvs[0], uvs[2], uvs[1]]
        target.tri(points, uvs)


def uv(band, u, v):
    return (band[0] + (band[1] - band[0]) * min(1.0, max(0.0, u)), min(.98, max(.02, v)))


def quad(solid, a, b, c, d, uvs):
    solid.tri([a, b, c], [uvs[0], uvs[1], uvs[2]])
    solid.tri([a, c, d], [uvs[0], uvs[2], uvs[3]])


def stack(target, profile, levels, band=ROCK_U):
    """Extrude a 2D profile through levels of (y, scale_x, scale_z, offset_x, offset_z).

    The top level may carry a sixth entry, a per-vertex y offset callable, which is how a crown is
    broken across rather than cut flat.
    """
    rings = []
    for level in levels:
        y, sx, sz, ox, oz = level[:5]
        slant = level[5] if len(level) > 5 else None
        ring = []
        for index, (px, pz) in enumerate(profile):
            x = px * sx + ox
            z = pz * sz + oz
            ring.append((x, y + (slant(px, pz) if slant else 0.0), z))
        rings.append(ring)

    count = len(profile)
    solid = MeshBuilder()
    for s in range(len(rings) - 1):
        lo, hi = rings[s], rings[s + 1]
        v0, v1 = s / (len(rings) - 1.0), (s + 1) / (len(rings) - 1.0)
        for k in range(count):
            n = (k + 1) % count
            u0, u1 = k / float(count), (k + 1) / float(count)
            quad(solid, lo[k], lo[n], hi[n], hi[k],
                 [uv(band, u0, v0), uv(band, u1, v0), uv(band, u1, v1), uv(band, u0, v1)])
    for ring, v, flip in ((rings[0], 0.0, True), (rings[-1], 1.0, False)):
        for k in range(1, count - 1):
            points = [ring[0], ring[k], ring[k + 1]]
            uvs = [uv(band, 0.0, v), uv(band, k / float(count), v), uv(band, (k + 1) / float(count), v)]
            if flip:
                points.reverse()
                uvs.reverse()
            solid.tri(points, uvs)
    append_solid(target, solid)


def box(target, lo, hi, band=CARVED_U):
    (x0, y0, z0), (x1, y1, z1) = lo, hi
    profile = [(-.5, -.5), (.5, -.5), (.5, .5), (-.5, .5)]
    stack(target,
          profile,
          [(y0, x1 - x0, z1 - z0, (x0 + x1) / 2, (z0 + z1) / 2),
           (y1, x1 - x0, z1 - z0, (x0 + x1) / 2, (z0 + z1) / 2)],
          band)


def bar(target, cx, cz, angle, length, width, y0, y1, band=CARVED_U):
    """A box turned to face `angle`, so a radial groove runs along the radius rather than along X."""
    cos, sin = math.cos(angle), math.sin(angle)
    corners = []
    for dx, dz in ((-.5, -.5), (.5, -.5), (.5, .5), (-.5, .5)):
        lx, lz = dx * length, dz * width
        corners.append((cx + lx * cos - lz * sin, cz + lx * sin + lz * cos))
    solid = MeshBuilder()
    lo = [(x, y0, z) for x, z in corners]
    hi = [(x, y1, z) for x, z in corners]
    for k in range(4):
        n = (k + 1) % 4
        quad(solid, lo[k], lo[n], hi[n], hi[k],
             [uv(band, 0, 0), uv(band, 1, 0), uv(band, 1, 1), uv(band, 0, 1)])
    for ring, flip in ((lo, True), (hi, False)):
        for k in range(1, 3):
            points = [ring[0], ring[k], ring[k + 1]]
            uvs = [uv(band, 0, .5), uv(band, .5, .5), uv(band, 1, .5)]
            if flip:
                points.reverse()
                uvs.reverse()
            solid.tri(points, uvs)
    append_solid(target, solid)


def polygon(sides, radius=0.5, jitter=None, rnd=None):
    points = []
    for i in range(sides):
        angle = math.tau * i / sides
        r = radius
        if jitter and rnd:
            r *= 1.0 + rnd.uniform(-jitter, jitter)
        points.append((math.cos(angle) * r, math.sin(angle) * r))
    return points


# ---------------------------------------------------------------- forms

def standing_stone(rnd):
    """A Deepstone: a leaning, unevenly tapered slab with a broken crown and a carved panel."""
    mesh = MeshBuilder()
    # Flattened hexagonal section: wide across, thin front-to-back, like a split slab.
    profile = [(-.50, -.26), (-.16, -.34), (.30, -.30), (.50, -.10),
               (.28, .32), (-.14, .34), (-.46, .22)]

    lean = 0.16   # metres of drift from base to crown, so it does not stand plumb
    # Broken crown: the top is cut across at an angle rather than flat.
    crown = lambda px, pz: px * 0.42 + pz * 0.18

    stack(mesh, profile, [
        (-3.70, 2.30, 1.34, 0.00, 0.00),
        (-3.20, 2.16, 1.26, 0.02, 0.01),
        (-1.40, 2.04, 1.18, lean * .25, 0.02),
        (0.60, 1.86, 1.06, lean * .60, 0.01),
        (2.40, 1.62, 0.92, lean * .85, -0.02),
        (3.28, 1.28, 0.74, lean, -0.04, crown),
    ])

    # Plinth: the stone does not emerge from the ground cleanly, it sits in a broken footing.
    plinth = polygon(9, 0.5, jitter=.16, rnd=rnd)
    stack(mesh, plinth, [
        (-3.86, 2.86, 1.86, 0.0, 0.0),
        (-3.52, 2.72, 1.74, 0.03, 0.02),
        (-3.18, 2.30, 1.40, 0.0, 0.0),
    ])

    # A carved cartouche on the front face. The frame is four bars rather than a slab, and it is
    # sunk into the face and left standing only ~45mm proud: the front of the shaft sits near
    # z=0.37 through this height, so a panel placed in front of that reads as a plaque floating off
    # the stone rather than as carving, which is exactly how the first version rendered. The
    # registrar gives every stone one flat material, so relief is all there is -- none of this can
    # lean on colour to read.
    inner, outer, thick = 0.30, 0.42, 0.13
    left, right, bottom, top = -0.58, 0.58, -1.70, 1.60
    box(mesh, (left, bottom, inner), (left + thick, top, outer))
    box(mesh, (right - thick, bottom, inner), (right, top, outer))
    box(mesh, (left, bottom, inner), (right, bottom + thick, outer))
    box(mesh, (left, top - thick, inner), (right, top, outer))
    # Four relief bars inside it, standing slightly less proud than the frame.
    for i in range(4):
        y = 1.05 - i * 0.70
        width = 0.34 if i % 2 == 0 else 0.25
        box(mesh, (-width, y - 0.08, inner), (width, y + 0.08, outer - 0.03))

    # Two breaks in the long edges. Pulled inside the shaft's own width so they read as damage to
    # the silhouette rather than as blocks stuck on its sides.
    box(mesh, (-0.98, 0.10, -0.26), (-0.66, 0.72, 0.26), ROCK_U)
    box(mesh, (0.70, -1.50, -0.24), (1.00, -1.02, 0.24), ROCK_U)
    return mesh, (2.1, 7.4, 1.25)


def descent_monolith(rnd):
    """The Conclave's centre pillar: four-sided, split by a fissure, crown notched."""
    mesh = MeshBuilder()
    profile = [(-.48, -.44), (.10, -.50), (.50, -.20), (.44, .30), (-.06, .50), (-.50, .24)]
    notch = lambda px, pz: -abs(px) * 0.62 + 0.26

    stack(mesh, profile, [
        (-4.10, 2.60, 2.60, 0.00, 0.00),
        (-3.60, 2.44, 2.44, 0.00, 0.00),
        (-1.20, 2.26, 2.26, 0.04, 0.02),
        (1.40, 2.00, 2.00, 0.06, 0.00),
        (3.10, 1.66, 1.66, 0.04, -0.03),
        (4.10, 1.30, 1.30, 0.00, -0.05, notch),
    ])

    footing = polygon(10, 0.5, jitter=.13, rnd=rnd)
    stack(mesh, footing, [
        (-4.24, 3.30, 3.30, 0.0, 0.0),
        (-3.94, 3.10, 3.10, 0.0, 0.0),
        (-3.52, 2.58, 2.58, 0.0, 0.0),
    ])

    # A dressed collar girding the whole shaft, rather than four blocks parked near it: the shaft
    # is about 2.14 across at the waist, so blocks centred at 0.92 stood clear of the surface and
    # read as pasted on. A ring at 2.34 sits 100mm proud all the way round and reads as carved.
    stack(mesh, profile, [
        (-0.46, 2.26, 2.26, 0.05, 0.01),
        (-0.34, 2.34, 2.34, 0.05, 0.01),
        (0.34, 2.34, 2.34, 0.05, 0.01),
        (0.46, 2.26, 2.26, 0.05, 0.01),
    ], CARVED_U)
    # The fissure is a channel, and a channel cannot be added -- only subtracted, which this
    # builder cannot do. Two ridges flanking a gap read as the same thing from outside.
    for side in (-1, 1):
        box(mesh, (side * 0.10 - 0.07, -2.50, 0.94), (side * 0.10 + 0.07, 2.60, 1.12), ROCK_U)
    return mesh, (2.8, 8.2, 2.8)


def conclave_dais(rnd):
    """The floor: three stepped rings under a raised rim, with radial spokes."""
    mesh = MeshBuilder()
    ring = polygon(40, 0.5)
    # Three courses, each smaller and slightly proud of the one below.
    stack(mesh, ring, [
        (-0.45, 17.0, 17.0, 0, 0),
        (-0.12, 17.0, 17.0, 0, 0),
        (-0.06, 15.4, 15.4, 0, 0),
        (0.14, 15.4, 15.4, 0, 0),
        (0.20, 13.2, 13.2, 0, 0),
        (0.45, 13.2, 13.2, 0, 0),
    ])
    # Raised outer rim, so the dais has an edge rather than fading into the terrain.
    rim = polygon(40, 0.5)
    stack(mesh, rim, [
        (-0.20, 17.6, 17.6, 0, 0),
        (0.30, 17.6, 17.6, 0, 0),
        (0.30, 16.6, 16.6, 0, 0),
        (-0.20, 16.6, 16.6, 0, 0),
    ])
    # One continuous radial bar per Deepstone, turned to lie along its own radius. Axis-aligned
    # boxes at intervals along the radius read as scattered tiles instead of a carved compass.
    for i in range(9):
        angle = math.tau * i / 9
        mid = 4.9
        bar(mesh, math.cos(angle) * mid, math.sin(angle) * mid, angle, 5.4, 0.62, 0.40, 0.56)
    return mesh, (17.0, 0.9, 17.0)


# ---------------------------------------------------------------- texture & output

def stone_map(rnd):
    """Weathered rock on the left of the map, dressed and carved stone on the right."""
    lattice = 16
    grid = [[rnd.random() for _ in range(lattice)] for _ in range(lattice)]

    def noise(u, v, frequency):
        fx, fy = u * frequency, v * frequency
        x0, y0 = int(fx) % lattice, int(fy) % lattice
        x1, y1 = (x0 + 1) % lattice, (y0 + 1) % lattice
        tx, ty = fx - math.floor(fx), fy - math.floor(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        top = grid[x0][y0] * (1 - tx) + grid[x1][y0] * tx
        bottom = grid[x0][y1] * (1 - tx) + grid[x1][y1] * tx
        return top * (1 - ty) + bottom * ty

    def sample(u, v):
        if u < 0.5:
            # Rough rock: horizontal bedding plus pitting, kept dark so carved faces read brighter.
            n = noise(u * 2.0, v, 5.0) * .55 + noise(u * 2.0, v, 17.0) * .45
            bedding = 0.08 * math.sin(v * math.tau * 7.0 + n * 3.0)
            return min(1.0, max(0.0, 0.22 + 0.34 * n + bedding))
        # Dressed stone: flatter, brighter, with fine tool marks so relief separates from the shaft.
        f = (u - 0.5) * 2.0
        tool = 0.05 * math.sin(f * math.tau * 22.0)
        return min(1.0, max(0.0, 0.56 + 0.26 * noise(f, v, 9.0) + tool))
    return sample


def emit(model_id, mesh, world_size, sampler):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    image = bpy.data.images.new('stone-' + model_id, TEXTURE_SIZE, TEXTURE_SIZE)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        v = (y + 0.5) / TEXTURE_SIZE
        for x in range(TEXTURE_SIZE):
            value = min(1.0, max(0.0, sampler((x + 0.5) / TEXTURE_SIZE, v)))
            i = (y * TEXTURE_SIZE + x) * 4
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value
            pixels[i + 3] = 1.0
    image.pixels = pixels
    # A generated image stores only its settings in the .blend unless packed, and exports black.
    image.update()
    image.pack()

    mat = bpy.data.materials.new('magenheim.' + model_id)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (1.0, 1.0, 1.0, 1.0)
    shader.inputs['Roughness'].default_value = 0.78
    shader.inputs['Metallic'].default_value = 0.10
    node = mat.node_tree.nodes.new('ShaderNodeTexImage')
    node.image = image
    mat.node_tree.links.new(node.outputs['Color'], shader.inputs['Base Color'])

    # Fit the authored form into the unit box the registrar scales, one axis at a time, so the
    # piece lands at exactly the world size the registrar declares (2.1 x 7.4 x 1.25 for a
    # Deepstone) rather than at whatever the plinth and chips happened to add. The forms above are
    # drawn in metres at roughly that aspect, so the fit is a trim rather than a reshaping; drawing
    # them in metres is still worth it, because it is the only way the carved panel and the relief
    # bars can be reasoned about at the size a player actually sees them.
    # Then game space (Y up) -> Blender (Z up), matching the exporter's inverse mapping.
    extents = [(min(v[i] for v in mesh.vertices), max(v[i] for v in mesh.vertices)) for i in range(3)]
    def fit(value, axis):
        lo, hi = extents[axis]
        span = hi - lo
        return (value - (lo + hi) / 2.0) / span if span > 1e-9 else 0.0
    placed = [(fit(v[0], 0), -fit(v[2], 2), fit(v[1], 1)) for v in mesh.vertices]

    data = bpy.data.meshes.new(model_id)
    data.from_pydata(placed, [], mesh.faces)
    data.update()
    uv_layer = data.uv_layers.new(name='UVMap')
    for loop in data.loops:
        uv_layer.data[loop.index].uv = mesh.uvs[loop.vertex_index]
    data.materials.append(mat)
    obj = bpy.data.objects.new(model_id, data)
    bpy.context.collection.objects.link(obj)
    obj['game_node_path'] = model_id
    obj['game_collision'] = False
    obj['game_crystal'] = 'null'

    scene = bpy.context.scene
    scene['model_id'] = model_id
    scene['runtime_lights'] = '[]'
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (model_id + '.blend')), compress=True)

    drawn = tuple(hi - lo for lo, hi in extents)
    print('AUTHORED %-30s tris=%-5d drawn=(%.2f, %.2f, %.2f)m -> world=(%.1f, %.1f, %.1f)m'
          % (model_id, len(mesh.faces), drawn[0], drawn[1], drawn[2], *world_size))


def main():
    for model_id, builder in (
        ('underworld-standing-stone', standing_stone),
        ('underworld-descent-monolith', descent_monolith),
        ('underworld-dais', conclave_dais),
    ):
        rnd = random.Random(7700 + sum(ord(c) for c in model_id))
        mesh, world_size = builder(rnd)
        emit(model_id, mesh, world_size, stone_map(random.Random(31 + len(model_id))))


main()
