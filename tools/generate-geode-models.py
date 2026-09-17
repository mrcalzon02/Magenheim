"""Rebuild the geode as fractured rock around a banded, druzy-lined cavity.

    blender --background --factory-startup --python tools/generate-geode-models.py

`geode-sample` is one model loaded by GeodeVisuals for both the carried item and the world
nodule, and shared by all eight biome geodes, so this single rebuild carries the whole set.

What it replaced (measured on the exported payload before this pass):

  * `stone-cavity` had 246 of 246 faces pointing inward, so the "exposed interior" was a
    fully inverted mesh being seen from behind - the reported "transparency" was back-face
    rendering, not alpha. The atlas is RGB with no alpha and the material is forced opaque.
  * every `interior-crystal-*` part was 63-73% inverted, all with negative signed volume;
  * the interior was a flat plane carrying five crystals, against a smooth lumpy shell.

What this builds instead:

  * Exterior - a Voronoi partition of an icosphere into plates, each raised and jittered
    with its rim inset toward the plate centre, so deep crevices separate them. A darker
    core sphere sits behind the crevices so they read as depth rather than holes.
  * Cavity   - a bowl whose normals face into the void, because the player looks in through
    the opening at the inside of the shell.
  * Banding  - a concentric agate collar around the opening. UV u runs with radius, so the
    band pattern lives in the texture rather than in geometry.
  * Druzy    - a dense field of small crystals seated on the cavity wall and pointing inward,
    varied in size, rather than a handful of spikes on a plane.

Interior surfaces are authored **greyscale on purpose**. GeodeVisuals.Apply assigns the biome
colour to any material whose name contains `.interior-`, and lightens `interior-bright-`.
Unity multiplies the albedo map by that colour, so a greyscale map reads as the biome tint
while keeping the banding and crystal detail. Colouring the source would muddy every biome.

Winding is controlled per polygon against an explicit reference point rather than left to a
normals-recalculate pass, because the defect being repaired here is winding.
"""
import bpy
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'

MODEL_ID = 'geode-sample'
SEED = 20260916

RADIUS = 0.50              # outer shell radius; overall model stays near the previous 1.1 span
CORE_RADIUS = 0.445        # darker mass behind the crevices
CAVITY_RADIUS = 0.415      # inner void wall
PLATE_COUNT = 30
CREVICE_INSET = 0.13       # fraction of the way toward the plate centre
OPENING_COS = 0.52         # plates whose axis is within this of the cut direction are removed
TEXTURE_SIZE = 256


# ------------------------------------------------------------------ mesh plumbing

class MeshBuilder:
    def __init__(self):
        self.vertices, self.faces, self.uvs = [], [], []

    def add(self, points, uvs, toward):
        """Append a polygon, flipping it if its normal points away from `toward`."""
        if _points_away(points, toward):
            points, uvs = list(reversed(points)), list(reversed(uvs))
        base = len(self.vertices)
        self.vertices.extend(points)
        self.uvs.extend(uvs)
        self.faces.append(tuple(range(base, base + len(points))))

    def empty(self):
        return not self.faces


def _points_away(points, toward):
    ax, ay, az = points[0]
    bx, by, bz = points[1]
    cx, cy, cz = points[2]
    ux, uy, uz = bx - ax, by - ay, bz - az
    vx, vy, vz = cx - ax, cy - ay, cz - az
    nx = uy * vz - uz * vy
    ny = uz * vx - ux * vz
    nz = ux * vy - uy * vx
    return (nx * (toward[0] - ax) + ny * (toward[1] - ay) + nz * (toward[2] - az)) < 0.0


def normalise(v):
    length = math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]) or 1.0
    return (v[0] / length, v[1] / length, v[2] / length)


def scale(v, k):
    return (v[0] * k, v[1] * k, v[2] * k)


def icosphere(subdivisions):
    """Unit icosphere as (vertices, triangles)."""
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
        midpoints, new_faces = {}, []

        def middle(a, b):
            key = (min(a, b), max(a, b))
            if key not in midpoints:
                va, vb = verts[a], verts[b]
                verts.append(normalise((va[0] + vb[0], va[1] + vb[1], va[2] + vb[2])))
                midpoints[key] = len(verts) - 1
            return midpoints[key]

        for a, b, c in faces:
            ab, bc, ca = middle(a, b), middle(b, c), middle(c, a)
            new_faces += [(a, ab, ca), (b, bc, ab), (c, ca, bc), (ab, bc, ca)]
        faces = new_faces
    return verts, faces


# ------------------------------------------------------------------ geode assembly

def fibonacci_directions(count, rnd):
    """Evenly spread plate seeds, jittered so the fracture pattern is not obviously regular."""
    directions = []
    golden = math.pi * (3.0 - math.sqrt(5.0))
    for i in range(count):
        y = 1.0 - (i / float(count - 1)) * 2.0
        radius = math.sqrt(max(0.0, 1.0 - y * y))
        theta = golden * i + rnd.uniform(-0.18, 0.18)
        directions.append(normalise((math.cos(theta) * radius,
                                     y + rnd.uniform(-0.05, 0.05),
                                     math.sin(theta) * radius)))
    return directions


def build_shell(rnd, cut_direction):
    """Fractured plates, and the crevice walls that give them thickness."""
    verts, faces = icosphere(2)
    seeds = fibonacci_directions(PLATE_COUNT, rnd)

    def nearest_seed(point):
        best, best_dot = 0, -2.0
        for index, seed in enumerate(seeds):
            dot = point[0] * seed[0] + point[1] * seed[1] + point[2] * seed[2]
            if dot > best_dot:
                best, best_dot = index, dot
        return best

    # Assign each triangle to the plate nearest its centroid.
    plates = {}
    for face in faces:
        centroid = normalise(tuple(sum(verts[i][k] for i in face) / 3.0 for k in range(3)))
        plates.setdefault(nearest_seed(centroid), []).append(face)

    shell = MeshBuilder()
    kept = 0
    for index, plate_faces in plates.items():
        axis = normalise(tuple(sum(normalise(tuple(sum(verts[i][k] for i in f) / 3.0
                                                   for k in range(3)))[k]
                                   for f in plate_faces) / len(plate_faces)
                               for k in range(3)))
        # Carve the opening: drop the plates covering the cut direction.
        if sum(axis[k] * cut_direction[k] for k in range(3)) > OPENING_COS:
            continue
        kept += 1

        lift = RADIUS * rnd.uniform(1.0, 1.075)
        # Edges on the plate boundary are the ones used by exactly one of its faces.
        edge_use = {}
        for a, b, c in plate_faces:
            for edge in ((a, b), (b, c), (c, a)):
                key = (min(edge), max(edge))
                edge_use[key] = edge_use.get(key, 0) + 1
        boundary = {v for key, count in edge_use.items() if count == 1 for v in key}

        def placed(vertex_index, radius):
            direction = verts[vertex_index]
            if vertex_index in boundary:
                # Pull the rim toward the plate centre so a crevice opens up beside it.
                direction = normalise(tuple(direction[k] * (1.0 - CREVICE_INSET) + axis[k] * CREVICE_INSET
                                            for k in range(3)))
            return scale(direction, radius)

        for a, b, c in plate_faces:
            points = [placed(a, lift), placed(b, lift), placed(c, lift)]
            shell.add(points, [(0.0, 0.0), (1.0, 0.0), (0.5, 1.0)], toward=scale(axis, 10.0))

        # Crevice walls: drop each boundary edge to the core so plates read as slabs.
        for key, count in edge_use.items():
            if count != 1:
                continue
            a, b = key
            top = [placed(a, lift), placed(b, lift)]
            low = [placed(b, CORE_RADIUS), placed(a, CORE_RADIUS)]
            quad = [top[0], top[1], low[0], low[1]]
            midpoint = tuple(sum(p[k] for p in quad) / 4.0 for k in range(3))
            shell.add(quad, [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)],
                      toward=scale(normalise(midpoint), 10.0))
    return shell, kept


def build_core(cut_direction):
    """Darker mass behind the crevices so the gaps read as depth, not holes.

    It must carry the same mouth as the shell and the cavity. Left whole, this sphere sits
    outside the cavity wall and seals the opening completely: the first render of the rebuilt
    geode showed an unbroken dark ball with no visible interior at all.
    """
    verts, faces = icosphere(2)
    core = MeshBuilder()
    for a, b, c in faces:
        centroid_dir = normalise(tuple(sum(verts[i][k] for i in (a, b, c)) / 3.0 for k in range(3)))
        if sum(centroid_dir[k] * cut_direction[k] for k in range(3)) > OPENING_COS - 0.10:
            continue
        points = [scale(verts[a], CORE_RADIUS), scale(verts[b], CORE_RADIUS), scale(verts[c], CORE_RADIUS)]
        core.add(points, [(0.0, 0.0), (1.0, 0.0), (0.5, 1.0)], toward=scale(centroid_dir, 10.0))
    return core


def build_cavity(cut_direction):
    """The inside of the shell. Normals face the void because the player looks in at it."""
    verts, faces = icosphere(2)
    cavity = MeshBuilder()
    for a, b, c in faces:
        centroid = normalise(tuple(sum(verts[i][k] for i in (a, b, c)) / 3.0 for k in range(3)))
        # Leave the mouth open; the banding collar closes that edge.
        if sum(centroid[k] * cut_direction[k] for k in range(3)) > OPENING_COS - 0.06:
            continue
        points = [scale(verts[a], CAVITY_RADIUS), scale(verts[b], CAVITY_RADIUS), scale(verts[c], CAVITY_RADIUS)]
        uvs = [(0.5 + verts[i][0] * 0.5, 0.5 + verts[i][2] * 0.5) for i in (a, b, c)]
        cavity.add(points, uvs, toward=(0.0, 0.0, 0.0))
    return cavity


def orthonormal_basis(direction):
    helper = (0.0, 0.0, 1.0) if abs(direction[2]) < 0.85 else (1.0, 0.0, 0.0)
    right = normalise((direction[1] * helper[2] - direction[2] * helper[1],
                       direction[2] * helper[0] - direction[0] * helper[2],
                       direction[0] * helper[1] - direction[1] * helper[0]))
    up = (direction[1] * right[2] - direction[2] * right[1],
          direction[2] * right[0] - direction[0] * right[2],
          direction[0] * right[1] - direction[1] * right[0])
    return right, normalise(up)


def build_banding(cut_direction, rnd, segments=48, rings=7):
    """Concentric agate collar around the mouth. UV u runs with radius, so the bands are
    carried by the texture and stay crisp at any geometric resolution."""
    right, up = orthonormal_basis(cut_direction)
    band = MeshBuilder()
    inner, outer = CAVITY_RADIUS * 0.80, RADIUS * 1.02

    def point(radius_fraction, angle, wobble):
        radius = inner + (outer - inner) * radius_fraction
        radius *= 1.0 + wobble
        # Seat the collar on the sphere so it hugs the shell instead of floating flat.
        depth = math.sqrt(max(0.0, RADIUS * RADIUS - min(radius, RADIUS) ** 2)) * 0.55
        offset = tuple(right[k] * math.cos(angle) * radius + up[k] * math.sin(angle) * radius
                       for k in range(3))
        return tuple(offset[k] + cut_direction[k] * depth for k in range(3))

    wobbles = [rnd.uniform(-0.035, 0.035) for _ in range(segments)]
    for ring in range(rings):
        t0, t1 = ring / float(rings), (ring + 1) / float(rings)
        for s in range(segments):
            a0 = math.tau * s / segments
            a1 = math.tau * (s + 1) / segments
            w0, w1 = wobbles[s], wobbles[(s + 1) % segments]
            quad = [point(t0, a0, w0), point(t0, a1, w1), point(t1, a1, w1), point(t1, a0, w0)]
            uvs = [(t0, s / float(segments)), (t0, (s + 1) / float(segments)),
                   (t1, (s + 1) / float(segments)), (t1, s / float(segments))]
            band.add(quad, uvs, toward=scale(cut_direction, 10.0))
    return band


def build_druzy(cut_direction, rnd, count=220):
    """Small crystals seated on the cavity wall, pointing inward."""
    druzy = MeshBuilder()
    placed = 0
    attempts = 0
    while placed < count and attempts < count * 12:
        attempts += 1
        # Uniform direction on the sphere, kept to the visible part of the cavity.
        z = rnd.uniform(-1.0, 1.0)
        phi = rnd.uniform(0.0, math.tau)
        r = math.sqrt(max(0.0, 1.0 - z * z))
        direction = (r * math.cos(phi), r * math.sin(phi), z)
        facing = sum(direction[k] * cut_direction[k] for k in range(3))
        if facing > OPENING_COS - 0.10:
            continue        # would sit in the mouth itself
        # The wall opposite the mouth is the surface the player looks straight at through
        # the opening. An earlier filter excluded it as "hidden", which emptied the most
        # visible part of the cavity; the first render showed crystals only around the rim.
        base = scale(direction, CAVITY_RADIUS * rnd.uniform(0.985, 1.0))
        length = CAVITY_RADIUS * rnd.uniform(0.10, 0.34)
        width = CAVITY_RADIUS * rnd.uniform(0.028, 0.072)
        inward = scale(direction, -1.0)
        # Lean each crystal slightly so the field is not a hedgehog.
        lean = normalise(tuple(inward[k] + rnd.uniform(-0.28, 0.28) for k in range(3)))
        right, up = orthonormal_basis(lean)
        sides = 5
        ring = []
        for s in range(sides):
            angle = math.tau * s / sides + rnd.uniform(-0.15, 0.15)
            spoke = width * rnd.uniform(0.75, 1.3)
            ring.append(tuple(base[k] + right[k] * math.cos(angle) * spoke
                              + up[k] * math.sin(angle) * spoke for k in range(3)))
        tip = tuple(base[k] + lean[k] * length for k in range(3))
        for s in range(sides):
            a, b = ring[s], ring[(s + 1) % sides]
            outward = tuple(base[k] + (a[k] - base[k]) * 8.0 for k in range(3))
            druzy.add([a, b, tip], [(0.0, 0.0), (1.0, 0.0), (0.5, 1.0)], toward=outward)
        druzy.add(list(ring), [(math.cos(math.tau * s / sides) * 0.5 + 0.5,
                                math.sin(math.tau * s / sides) * 0.5 + 0.5) for s in range(sides)],
                  toward=tuple(base[k] - lean[k] * 4.0 for k in range(3)))
        placed += 1
    return druzy


# ------------------------------------------------------------------ textures

def greyscale_image(name, sampler):
    """Build a greyscale image from sampler(u, v) -> 0..1."""
    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        v = (y + 0.5) / TEXTURE_SIZE
        for x in range(TEXTURE_SIZE):
            u = (x + 0.5) / TEXTURE_SIZE
            value = min(1.0, max(0.0, sampler(u, v)))
            index = (y * TEXTURE_SIZE + x) * 4
            pixels[index] = pixels[index + 1] = pixels[index + 2] = value
            pixels[index + 3] = 1.0
    image.pixels = pixels
    return image


def agate_sampler(rnd):
    """Concentric bands across u, which the collar maps to radius."""
    offsets = [rnd.uniform(0.0, math.tau) for _ in range(4)]

    def sample(u, v):
        wobble = 0.018 * math.sin(v * math.tau * 3.0 + offsets[0])
        radius = u + wobble
        bands = 0.5 + 0.34 * math.sin(radius * math.tau * 5.5 + offsets[1])
        bands += 0.13 * math.sin(radius * math.tau * 17.0 + offsets[2])
        bands += 0.05 * math.sin(radius * math.tau * 41.0 + offsets[3])
        # Bright chalcedony rim on the outside, darker toward the cavity.
        return 0.30 + 0.62 * bands * (0.45 + 0.55 * u)
    return sample


def rock_sampler(rnd):
    """Blotchy value noise for the shell; keeps plates from reading as flat facets."""
    lattice = 16
    grid = [[rnd.random() for _ in range(lattice)] for _ in range(lattice)]

    def value(u, v, frequency):
        fx, fy = u * frequency, v * frequency
        x0, y0 = int(fx) % lattice, int(fy) % lattice
        x1, y1 = (x0 + 1) % lattice, (y0 + 1) % lattice
        tx, ty = fx - math.floor(fx), fy - math.floor(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        top = grid[x0][y0] * (1 - tx) + grid[x1][y0] * tx
        bottom = grid[x0][y1] * (1 - tx) + grid[x1][y1] * tx
        return top * (1 - ty) + bottom * ty

    def sample(u, v):
        n = value(u, v, 5.0) * 0.55 + value(u, v, 13.0) * 0.30 + value(u, v, 29.0) * 0.15
        return 0.34 + 0.52 * n
    return sample


def druzy_sampler(rnd):
    """Bright facets with fine sparkle, so tinting reads as crystal rather than paint."""
    def sample(u, v):
        facet = 0.62 + 0.24 * math.sin(u * math.tau * 7.0) * math.cos(v * math.tau * 5.0)
        sparkle = 0.14 * ((math.sin(u * 211.0) * math.cos(v * 197.0)) ** 8)
        return facet + sparkle
    return sample


def material(name, image, roughness, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.use_backface_culling = False
    tree = mat.node_tree
    shader = tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (1.0, 1.0, 1.0, 1.0)
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Metallic'].default_value = metallic
    node = tree.nodes.new('ShaderNodeTexImage')
    node.image = image
    tree.links.new(node.outputs['Color'], shader.inputs['Base Color'])
    return mat


def emit(name, builder, mat, collision=True):
    if builder.empty():
        raise RuntimeError('Geode part produced no geometry: ' + name)
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(builder.vertices, [], builder.faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        uv_layer.data[loop.index].uv = builder.uvs[loop.vertex_index]
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj['game_node_path'] = name
    obj['game_collision'] = collision
    obj['game_crystal'] = 'null'
    return obj


def main():
    rnd = random.Random(SEED)
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # Mouth faces mostly sideways and slightly up: readable held in the hand and as a nodule.
    cut_direction = normalise((1.0, 0.0, 0.34))

    rock = material('magenheim.geode.shell', greyscale_image('geode-shell', rock_sampler(rnd)), 0.86)
    core = material('magenheim.geode.core', greyscale_image('geode-core', rock_sampler(rnd)), 0.92)
    # Names carry the GeodeVisuals tint contract: '.interior-' is tinted per biome and
    # 'interior-bright-' is lightened. Do not rename without updating GeodeVisuals.
    band = material('magenheim.geode.interior-band', greyscale_image('geode-band', agate_sampler(rnd)), 0.42)
    wall = material('magenheim.geode.interior-cavity', greyscale_image('geode-cavity', rock_sampler(rnd)), 0.58)
    druz = material('magenheim.geode.interior-bright-druzy', greyscale_image('geode-druzy', druzy_sampler(rnd)), 0.22, 0.05)

    shell, plates = build_shell(rnd, cut_direction)
    emit('GeodeShell', shell, rock)
    emit('GeodeCore', build_core(cut_direction), core)
    emit('GeodeCavity', build_cavity(cut_direction), wall)
    emit('GeodeBanding', build_banding(cut_direction, rnd), band)
    emit('GeodeDruzy', build_druzy(cut_direction, rnd), druz, collision=False)

    scene = bpy.context.scene
    scene['model_id'] = MODEL_ID
    scene['runtime_lights'] = '[]'
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (MODEL_ID + '.blend')), compress=True)

    triangles = sum(len(o.data.polygons) for o in scene.objects if o.type == 'MESH')
    print('GEODE %s objects=%d polygons=%d plates=%d' % (
        MODEL_ID, len([o for o in scene.objects if o.type == 'MESH']), triangles, plates))


main()
