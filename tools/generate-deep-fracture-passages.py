"""Rebuild the Deep Fracture passage and traversal node to match the rebuilt districts.

    blender --background --factory-startup --python tools/generate-deep-fracture-passages.py

The districts became enclosed caverns of roughly 10,000 triangles each, but the pieces that
join them were left behind: deep-fracture-passage was five boxes totalling 60 triangles and
deep-fracture-traversal 172. The corridor is the first thing seen after a chamber, so the
mismatch is immediately legible.

  passage    a swept rock tunnel with an arched roof, uneven floor and irregular walls,
             open at both ends so it docks into the district mouths, with a few pendants
             and floor rubble for silhouette
  traversal  the same plinth, ring and core device as before, kept in role and envelope but
             built as faceted stone and crystal rather than three primitives

Both keep the envelopes the runtime declares in DeepFractureRoomVisuals: the passage Room is
10 x 10 x 16 and the traversal node 12 x 10 x 12, and the previous models measured
9.6 x 8.8 x 16.0 and 4.5 x 7.9 x 4.5.

The tunnel shell faces into the passage, because the player walks inside it. Its parts are
declared in verify-model-geometry's INTERIOR_SURFACES for that reason. Faces are emitted as
triangles and each closed solid is measured and corrected once, for the reasons recorded in
docs/RENDERING_AND_CONTENT_DEFECTS.md.

Blender is Z up and the exporter maps (x, y, z) to (x, z, -y), so these are authored directly
in Blender space: X across the tunnel, Y along it, Z vertical.
"""
import bpy
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
TEXTURE_SIZE = 256

HALF_LENGTH = 8.0        # tunnel runs -8..+8 along Y, matching the 16 m Room
HALF_WIDTH = 4.6        # swell adds ~11%, so this keeps the tunnel inside the 10 m Room
FLOOR_Z = -1.2
ROOF_Z = 7.6
RING = 18                # samples around the tunnel cross-section
STATIONS = 15            # samples along its length


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
            points, uvs = list(reversed(points)), list(reversed(uvs))
        base = len(target.vertices)
        target.vertices.extend(points)
        target.uvs.extend(uvs)
        target.faces.append((base, base + 1, base + 2))


def noise_field(seed, lattice=16):
    rnd = random.Random(seed)
    grid = [[rnd.random() * 2.0 - 1.0 for _ in range(lattice)] for _ in range(lattice)]

    def sample(u, v, frequency):
        fx, fy = u * frequency, v * frequency
        x0, y0 = int(math.floor(fx)) % lattice, int(math.floor(fy)) % lattice
        x1, y1 = (x0 + 1) % lattice, (y0 + 1) % lattice
        tx, ty = fx - math.floor(fx), fy - math.floor(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        top = grid[x0][y0] * (1 - tx) + grid[x1][y0] * tx
        bottom = grid[x0][y1] * (1 - tx) + grid[x1][y1] * tx
        return top * (1 - ty) + bottom * ty
    return sample


def profile(angle, along, wobble):
    """Tunnel cross-section: a flattened floor rising into an arched roof."""
    centre_z = (FLOOR_Z + ROOF_Z) * 0.5
    half_height = (ROOF_Z - FLOOR_Z) * 0.5
    x = math.sin(angle)
    z = math.cos(angle)
    if z < 0.0:
        z *= 0.42                       # flatten the floor so it is walkable
    swell = 1.0 + 0.11 * wobble
    return (x * HALF_WIDTH * swell, centre_z + z * half_height * swell)


def build_tunnel(rnd):
    field = noise_field(7717)
    shell = MeshBuilder()
    stations = []
    for s in range(STATIONS):
        t = s / float(STATIONS - 1)
        y = -HALF_LENGTH + 2.0 * HALF_LENGTH * t
        ring = []
        for k in range(RING):
            angle = math.tau * k / RING
            wobble = field(t * 2.3, k / float(RING) * 2.0, 3.4)
            x, z = profile(angle, t, wobble)
            ring.append((x, y, z))
        stations.append(ring)

    for s in range(STATIONS - 1):
        lo, hi = stations[s], stations[s + 1]
        v0, v1 = s / float(STATIONS - 1), (s + 1) / float(STATIONS - 1)
        for k in range(RING):
            n = (k + 1) % RING
            u0, u1 = k / float(RING), (k + 1) / float(RING)
            # Wound so the visible face is the one seen from inside the tunnel.
            shell.tri([lo[k], hi[k], lo[n]], [(u0, v0), (u0, v1), (u1, v0)])
            shell.tri([lo[n], hi[k], hi[n]], [(u1, v0), (u0, v1), (u1, v1)])

    # Confirm the shell faces the axis; a swept tube has no volume test to fall back on.
    sample = shell.vertices[0]
    a, b, c = shell.vertices[0], shell.vertices[1], shell.vertices[2]
    ux, uy, uz = (b[i] - a[i] for i in range(3))
    vx, vy, vz = (c[i] - a[i] for i in range(3))
    nx = uy * vz - uz * vy
    ny = uz * vx - ux * vz
    nz = ux * vy - uy * vx
    axis = (0.0, sample[1], (FLOOR_Z + ROOF_Z) * 0.5)
    if nx * (axis[0] - a[0]) + ny * (axis[1] - a[1]) + nz * (axis[2] - a[2]) < 0.0:
        flipped = MeshBuilder()
        for face in shell.faces:
            pts = [shell.vertices[i] for i in face][::-1]
            uvs = [shell.uvs[i] for i in face][::-1]
            flipped.tri(pts, uvs)
        shell = flipped
    return shell


def add_spike(target, base, direction, length, radius, rnd, sides=6):
    length = abs(length)
    right = (1.0, 0.0, 0.0) if abs(direction[0]) < 0.9 else (0.0, 1.0, 0.0)
    ax = (direction[1] * right[2] - direction[2] * right[1],
          direction[2] * right[0] - direction[0] * right[2],
          direction[0] * right[1] - direction[1] * right[0])
    m = math.sqrt(sum(c * c for c in ax)) or 1.0
    ax = tuple(c / m for c in ax)
    ay = (direction[1] * ax[2] - direction[2] * ax[1],
          direction[2] * ax[0] - direction[0] * ax[2],
          direction[0] * ax[1] - direction[1] * ax[0])
    ring = []
    for k in range(sides):
        angle = math.tau * k / sides
        r = radius * rnd.uniform(0.7, 1.3)
        ring.append(tuple(base[i] + ax[i] * math.cos(angle) * r + ay[i] * math.sin(angle) * r
                          for i in range(3)))
    tip = tuple(base[i] + direction[i] * length for i in range(3))
    solid = MeshBuilder()
    for k in range(sides):
        n = (k + 1) % sides
        solid.tri([ring[k], ring[n], tip], [(0, 0), (1, 0), (0.5, 1)])
    for k in range(1, sides - 1):
        solid.tri([ring[0], ring[k], ring[k + 1]], [(0, 0), (1, 0), (1, 1)])
    append_solid(target, solid)


def build_passage():
    rnd = random.Random(31337)
    shell = build_tunnel(rnd)
    detail = MeshBuilder()
    for _ in range(14):
        y = rnd.uniform(-HALF_LENGTH + 1.5, HALF_LENGTH - 1.5)
        x = rnd.uniform(-HALF_WIDTH * 0.7, HALF_WIDTH * 0.7)
        add_spike(detail, (x, y, ROOF_Z - 0.5), (0.0, 0.0, -1.0),
                  rnd.uniform(0.7, 2.1), rnd.uniform(0.18, 0.5), rnd)
    for _ in range(10):
        y = rnd.uniform(-HALF_LENGTH + 1.2, HALF_LENGTH - 1.2)
        x = rnd.uniform(-HALF_WIDTH * 0.82, HALF_WIDTH * 0.82)
        add_spike(detail, (x, y, FLOOR_Z + 0.25), (0.0, 0.0, 1.0),
                  rnd.uniform(0.4, 1.2), rnd.uniform(0.22, 0.55), rnd)
    return shell, detail


def build_traversal():
    """Plinth, ring and core, kept in role and envelope but faceted."""
    rnd = random.Random(90125)
    stone, crystal = MeshBuilder(), MeshBuilder()

    # Stepped octagonal plinth.
    for step, (radius, z0, z1) in enumerate([(2.25, -0.8, -0.2), (1.75, -0.2, 0.45),
                                             (1.30, 0.45, 1.0)]):
        sides = 14
        lower = [(math.cos(math.tau * k / sides) * radius, math.sin(math.tau * k / sides) * radius, z0)
                 for k in range(sides)]
        upper = [(math.cos(math.tau * k / sides) * radius * 0.94,
                  math.sin(math.tau * k / sides) * radius * 0.94, z1) for k in range(sides)]
        solid = MeshBuilder()
        for k in range(sides):
            n = (k + 1) % sides
            solid.tri([lower[k], lower[n], upper[n]], [(0, 0), (1, 0), (1, 1)])
            solid.tri([lower[k], upper[n], upper[k]], [(0, 0), (1, 1), (0, 1)])
        for k in range(1, sides - 1):
            solid.tri([upper[0], upper[k], upper[k + 1]], [(0, 0), (1, 0), (1, 1)])
            solid.tri([lower[0], lower[k + 1], lower[k]], [(0, 0), (1, 1), (1, 0)])
        append_solid(stone, solid)

    # Ring of standing shards around the core.
    for k in range(12):
        angle = math.tau * k / 12
        base = (math.cos(angle) * 1.05, math.sin(angle) * 1.05, 1.0)
        add_spike(crystal, base, (math.cos(angle) * 0.22, math.sin(angle) * 0.22, 1.0),
                  rnd.uniform(1.1, 1.9), 0.20, rnd, sides=7)

    # Suspended core.
    add_spike(crystal, (0.0, 0.0, 3.4), (0.0, 0.0, 1.0), 2.0, 0.55, rnd, sides=14)
    add_spike(crystal, (0.0, 0.0, 3.4), (0.0, 0.0, -1.0), 1.5, 0.55, rnd, sides=14)
    return stone, crystal


def greyscale(name, sampler):
    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        v = (y + 0.5) / TEXTURE_SIZE
        for x in range(TEXTURE_SIZE):
            value = min(1.0, max(0.0, sampler((x + 0.5) / TEXTURE_SIZE, v)))
            i = (y * TEXTURE_SIZE + x) * 4
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value
            pixels[i + 3] = 1.0
    image.pixels = pixels
    return image


def rock_sampler(seed):
    field = noise_field(seed)

    def sample(u, v):
        n = field(u, v, 6.0) * 0.55 + field(u, v, 15.0) * 0.30 + field(u, v, 33.0) * 0.15
        return 0.36 + 0.30 * (n * 0.5 + 0.5)
    return sample


def crystal_sampler():
    def sample(u, v):
        return 0.58 + 0.30 * abs(math.sin(u * math.tau * 5.0)) + 0.08 * math.sin(v * math.tau * 3.0)
    return sample


def material(name, image, roughness, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.use_backface_culling = False
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (1.0, 1.0, 1.0, 1.0)
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Metallic'].default_value = metallic
    node = mat.node_tree.nodes.new('ShaderNodeTexImage')
    node.image = image
    mat.node_tree.links.new(node.outputs['Color'], shader.inputs['Base Color'])
    return mat


def emit(name, builder, mat, collision=True):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(builder.vertices, [], builder.faces)
    mesh.update()
    layer = mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        layer.data[loop.index].uv = builder.uvs[loop.vertex_index]
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj['game_node_path'] = name
    obj['game_collision'] = collision
    obj['game_crystal'] = 'null'


def save(model_id):
    scene = bpy.context.scene
    scene['model_id'] = model_id
    scene['runtime_lights'] = '[]'
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (model_id + '.blend')), compress=True)
    triangles = sum(len(o.data.polygons) for o in scene.objects if o.type == 'MESH')
    print('PASSAGE %-26s objects=%d triangles=%d'
          % (model_id, len([o for o in scene.objects if o.type == 'MESH']), triangles))


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    rock = material('magenheim.passage.rock', greyscale('passage-rock', rock_sampler(4001)), 0.88)
    formation = material('magenheim.passage.formation',
                         greyscale('passage-formation', rock_sampler(4002)), 0.78)
    shell, detail = build_passage()
    emit('PassageShell', shell, rock)
    emit('PassageFormations', detail, formation)
    save('deep-fracture-passage')

    bpy.ops.wm.read_factory_settings(use_empty=True)
    stone = material('magenheim.traversal.stone',
                     greyscale('traversal-stone', rock_sampler(4003)), 0.84)
    crystal = material('magenheim.traversal.crystal', greyscale('traversal-crystal', crystal_sampler()), 0.26, 0.1)
    plinth, shards = build_traversal()
    emit('TraversalPlinth', plinth, stone)
    emit('TraversalCore', shards, crystal)
    save('deep-fracture-traversal')


main()
