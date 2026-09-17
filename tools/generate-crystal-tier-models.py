"""Rebuild the crystal progression items: rough, simple, crystal, advanced, master, shards.

    blender --background --factory-startup --python tools/generate-crystal-tier-models.py

Six models carry forty-eight items. EarthContentRegistrar registers every tier for all eight
elements from one shared mesh per tier, tinted per element, plus the shard item. They were
also the least developed assets in the library: earth-simple was 36 triangles, rough,
crystal and shards 108, advanced and master 180 - the lowest counts of anything, for the
items the player handles through the entire refinement loop.

The rebuild gives the progression a readable silhouette:

  rough     a matrix chunk with stubby points barely breaking the surface
  simple    one clear terminated point on a small matrix base
  crystal   a well formed point with a secondary, cleaner facets
  advanced  a main point with three secondaries, a developing cluster
  master    a radiating cluster of seven terminated points, matrix nearly gone
  shards    six angular fragments

Two constraints from the surrounding code, both load-bearing:

  * EarthAssets.LoadMesh calls RecalculateNormals, so winding alone decides lighting. Every
    solid is therefore built in isolation, measured, and reversed once if its signed volume
    came out negative, rather than reasoned about per face. Faces are emitted as triangles,
    never quads, because a non-planar quad triangulates into halves that can disagree.
  * ReplaceVisual builds a single MeshFilter, so each model must be one object with one
    material. Rock and crystal share a texture, split by UV: the left of the map is matrix,
    the right is crystal facets.

The map is greyscale because EarthAssets tints it per element. Colour in the source would
fight all eight tints.

Bounds match the models being replaced, about 0.25 x 0.34 x 0.23 with the base near
y = -0.14, so the existing registration scales still apply.
"""
import bpy
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
TEXTURE_SIZE = 256
BASE_Y = -0.14                 # where the matrix sits, matching the replaced models
SIDES = 8
SEGMENTS = 3

ROCK_U = (0.04, 0.46)          # left of the map is matrix
CRYSTAL_U = (0.54, 0.98)       # right of the map is crystal facets


# ---------------------------------------------------------------- mesh plumbing

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
    """Append a closed solid, reversing it once if it was built inside-out."""
    reverse = solid.signed_volume() < 0.0
    for face in solid.faces:
        points = [solid.vertices[i] for i in face]
        uvs = [solid.uvs[i] for i in face]
        if reverse:
            points, uvs = list(reversed(points)), list(reversed(uvs))
        base = len(target.vertices)
        target.vertices.extend(points)
        target.uvs.extend(uvs)
        target.faces.append((base, base + 1, base + 2))


def normalise(v):
    length = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / length for c in v)


def basis(direction):
    helper = (0.0, 0.0, 1.0) if abs(direction[2]) < 0.85 else (1.0, 0.0, 0.0)
    right = normalise((direction[1] * helper[2] - direction[2] * helper[1],
                       direction[2] * helper[0] - direction[0] * helper[2],
                       direction[0] * helper[1] - direction[1] * helper[0]))
    up = (direction[1] * right[2] - direction[2] * right[1],
          direction[2] * right[0] - direction[0] * right[2],
          direction[0] * right[1] - direction[1] * right[0])
    return right, normalise(up)


def icosphere(subdivisions):
    t = (1.0 + math.sqrt(5.0)) / 2.0
    verts = [normalise(v) for v in [
        (-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0),
        (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t),
        (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]]
    faces = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11),
             (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
             (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9),
             (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)]
    for _ in range(subdivisions):
        mid, out = {}, []

        def middle(a, b):
            key = (min(a, b), max(a, b))
            if key not in mid:
                va, vb = verts[a], verts[b]
                verts.append(normalise((va[0] + vb[0], va[1] + vb[1], va[2] + vb[2])))
                mid[key] = len(verts) - 1
            return mid[key]

        for a, b, c in faces:
            ab, bc, ca = middle(a, b), middle(b, c), middle(c, a)
            out += [(a, ab, ca), (b, bc, ab), (c, ca, bc), (ab, bc, ca)]
        faces = out
    return verts, faces


# ---------------------------------------------------------------- forms

def add_matrix(target, centre, radius, rnd, subdivisions=2, roughness=0.28):
    """An irregular chunk of host rock."""
    verts, faces = icosphere(subdivisions)
    warped = []
    for v in verts:
        # Deterministic per-direction warp, so the chunk reads as broken stone.
        k = 1.0 + roughness * (math.sin(v[0] * 7.3 + rnd.seedval) * 0.5
                               + math.sin(v[1] * 5.1 + rnd.seedval * 1.7) * 0.3
                               + math.sin(v[2] * 9.7 + rnd.seedval * 2.3) * 0.2)
        warped.append(tuple(centre[i] + v[i] * radius * k for i in range(3)))
    solid = MeshBuilder()
    for a, b, c in faces:
        pa, pb, pc = warped[a], warped[b], warped[c]
        uvs = [(ROCK_U[0] + (ROCK_U[1] - ROCK_U[0]) * (0.5 + verts[i][0] * 0.5),
                0.5 + verts[i][1] * 0.5) for i in (a, b, c)]
        solid.tri([pa, pb, pc], uvs)
    append_solid(target, solid)


def add_point(target, origin, direction, length, radius, rnd, sides=SIDES, segments=SEGMENTS,
              tip=0.32, taper=0.72):
    """A terminated crystal: a tapering prism closed by a pyramidal tip and a base cap."""
    direction = normalise(direction)
    right, up = basis(direction)
    body = length * (1.0 - tip)

    rings = []
    for s in range(segments + 1):
        t = s / float(segments)
        r = radius * (1.0 - (1.0 - taper) * t) * rnd.uniform(0.94, 1.06)
        centre = tuple(origin[i] + direction[i] * body * t for i in range(3))
        ring = []
        for k in range(sides):
            angle = math.tau * k / sides
            ring.append(tuple(centre[i] + right[i] * math.cos(angle) * r
                              + up[i] * math.sin(angle) * r for i in range(3)))
        rings.append(ring)

    apex = tuple(origin[i] + direction[i] * length for i in range(3))
    solid = MeshBuilder()

    def facet_uv(k, t):
        return (CRYSTAL_U[0] + (CRYSTAL_U[1] - CRYSTAL_U[0]) * (k % sides) / float(sides),
                0.06 + 0.88 * t)

    for s in range(segments):
        lo, hi = rings[s], rings[s + 1]
        t0, t1 = s / float(segments), (s + 1) / float(segments)
        for k in range(sides):
            n = (k + 1) % sides
            solid.tri([lo[k], lo[n], hi[n]], [facet_uv(k, t0), facet_uv(k + 1, t0), facet_uv(k + 1, t1)])
            solid.tri([lo[k], hi[n], hi[k]], [facet_uv(k, t0), facet_uv(k + 1, t1), facet_uv(k, t1)])
    crown = rings[-1]
    for k in range(sides):
        n = (k + 1) % sides
        solid.tri([crown[k], crown[n], apex], [facet_uv(k, 0.9), facet_uv(k + 1, 0.9), facet_uv(k, 1.0)])
    root = rings[0]
    for k in range(1, sides - 1):
        solid.tri([root[0], root[k], root[k + 1]],
                  [facet_uv(0, 0.0), facet_uv(k, 0.0), facet_uv(k + 1, 0.0)])
    append_solid(target, solid)


def add_shard(target, centre, size, rnd):
    """A small angular fragment: a randomised low-facet solid."""
    verts, faces = icosphere(1)
    warped = []
    for v in verts:
        k = rnd.uniform(0.45, 1.35)
        warped.append(tuple(centre[i] + v[i] * size * k for i in range(3)))
    solid = MeshBuilder()
    for a, b, c in faces:
        uvs = [(CRYSTAL_U[0] + (CRYSTAL_U[1] - CRYSTAL_U[0]) * (0.5 + verts[i][0] * 0.5),
                0.5 + verts[i][1] * 0.5) for i in (a, b, c)]
        solid.tri([warped[a], warped[b], warped[c]], uvs)
    append_solid(target, solid)


def spread(rnd, tilt):
    """A direction leaning off vertical by roughly `tilt` radians."""
    angle = rnd.uniform(0.0, math.tau)
    lean = rnd.uniform(tilt * 0.45, tilt)
    return normalise((math.sin(lean) * math.cos(angle), math.cos(lean), math.sin(lean) * math.sin(angle)))


# ---------------------------------------------------------------- tiers

def build_tier(name, rnd):
    mesh = MeshBuilder()
    rnd.seedval = {'rough': 1.0, 'simple': 2.0, 'crystal': 3.0,
                   'advanced': 4.0, 'master': 5.0, 'shards': 6.0}[name]
    base = (0.0, BASE_Y + 0.055, 0.0)

    if name == 'rough':
        add_matrix(mesh, base, 0.092, rnd, subdivisions=2, roughness=0.30)
        for _ in range(3):
            direction = spread(rnd, 0.9)
            origin = tuple(base[i] + direction[i] * 0.07 for i in range(3))
            add_point(mesh, origin, direction, rnd.uniform(0.07, 0.11), 0.028, rnd,
                      sides=6, segments=2, tip=0.45, taper=0.6)

    elif name == 'simple':
        add_matrix(mesh, base, 0.072, rnd, subdivisions=2, roughness=0.3)
        add_point(mesh, (0.0, BASE_Y + 0.03, 0.0), (0.06, 1.0, 0.03), 0.30, 0.046, rnd)

    elif name == 'crystal':
        add_matrix(mesh, base, 0.066, rnd, subdivisions=2, roughness=0.26)
        add_point(mesh, (0.0, BASE_Y + 0.02, 0.0), (0.04, 1.0, 0.02), 0.335, 0.052, rnd)
        direction = spread(rnd, 0.75)
        add_point(mesh, (0.045, BASE_Y + 0.05, -0.03), direction, 0.13, 0.026, rnd, segments=2)

    elif name == 'advanced':
        add_matrix(mesh, base, 0.062, rnd, subdivisions=2, roughness=0.24)
        add_point(mesh, (0.0, BASE_Y + 0.02, 0.0), (0.02, 1.0, 0.01), 0.34, 0.055, rnd)
        for _ in range(3):
            direction = spread(rnd, 0.85)
            origin = tuple(base[i] + direction[i] * 0.05 for i in range(3))
            add_point(mesh, origin, direction, rnd.uniform(0.13, 0.19), 0.03, rnd, segments=2)

    elif name == 'master':
        add_matrix(mesh, base, 0.05, rnd, subdivisions=1, roughness=0.2)
        add_point(mesh, (0.0, BASE_Y + 0.01, 0.0), (0.0, 1.0, 0.0), 0.34, 0.058, rnd,
                  sides=10, tip=0.38, taper=0.78)
        for index in range(6):
            angle = math.tau * index / 6.0 + 0.24
            direction = normalise((math.cos(angle) * 0.62, 1.0, math.sin(angle) * 0.62))
            origin = (math.cos(angle) * 0.042, BASE_Y + 0.035, math.sin(angle) * 0.042)
            add_point(mesh, origin, direction, rnd.uniform(0.16, 0.24), 0.032, rnd,
                      sides=8, tip=0.36, taper=0.76)

    elif name == 'shards':
        for _ in range(6):
            centre = (rnd.uniform(-0.045, 0.045), BASE_Y + rnd.uniform(0.01, 0.075),
                      rnd.uniform(-0.04, 0.04))
            add_shard(mesh, centre, rnd.uniform(0.016, 0.032), rnd)

    return mesh


# ---------------------------------------------------------------- texture & output

def crystal_map(rnd):
    """Left half matrix rock, right half crystal facets. Greyscale for per-element tinting."""
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
            n = noise(u * 2.0, v, 6.0) * 0.6 + noise(u * 2.0, v, 15.0) * 0.4
            return 0.26 + 0.34 * n                      # dark, matte host rock
        f = (u - 0.5) * 2.0
        facet = 0.5 + 0.5 * math.sin(f * math.tau * 4.0)
        depth = 0.62 + 0.30 * facet
        depth += 0.10 * math.sin(v * math.tau * 2.0 + f * 3.0)
        depth += 0.06 * noise(f, v, 22.0)
        return min(1.0, depth)                          # bright, banded crystal
    return sample


def emit(model_id, mesh, sampler):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    image = bpy.data.images.new('crystal-' + model_id, TEXTURE_SIZE, TEXTURE_SIZE)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        v = (y + 0.5) / TEXTURE_SIZE
        for x in range(TEXTURE_SIZE):
            value = min(1.0, max(0.0, sampler((x + 0.5) / TEXTURE_SIZE, v)))
            i = (y * TEXTURE_SIZE + x) * 4
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value
            pixels[i + 3] = 1.0
    image.pixels = pixels

    mat = bpy.data.materials.new('magenheim.' + model_id)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (1.0, 1.0, 1.0, 1.0)
    shader.inputs['Roughness'].default_value = 0.34
    shader.inputs['Metallic'].default_value = 0.0
    node = mat.node_tree.nodes.new('ShaderNodeTexImage')
    node.image = image
    mat.node_tree.links.new(node.outputs['Color'], shader.inputs['Base Color'])

    # The forms above are authored in game space, Y up, so their bounds can be compared
    # directly against the models being replaced. Blender is Z up and the exporter maps
    # (x, y, z) -> (x, z, -y), so (x, y, z) -> (x, -z, y) here makes the exported game
    # coordinates identical to the authored ones. Without it every crystal ships lying on
    # its side, which is what the first preview showed.
    data = bpy.data.meshes.new(model_id)
    data.from_pydata([(v[0], -v[2], v[1]) for v in mesh.vertices], [], mesh.faces)
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

    xs = [v[0] for v in mesh.vertices]
    ys = [v[1] for v in mesh.vertices]
    zs = [v[2] for v in mesh.vertices]
    print('CRYSTAL %-16s tris=%-5d size=(%.2f, %.2f, %.2f) ymin=%.2f'
          % (model_id, len(mesh.faces), max(xs) - min(xs), max(ys) - min(ys),
             max(zs) - min(zs), min(ys)))


def main():
    for tier in ('rough', 'simple', 'crystal', 'advanced', 'master', 'shards'):
        rnd = random.Random(90210 + sum(ord(c) for c in tier))
        rnd.seedval = 0.0
        mesh = build_tier(tier, rnd)
        emit('earth-' + tier, mesh, crystal_map(random.Random(4242 + len(tier))))


main()
