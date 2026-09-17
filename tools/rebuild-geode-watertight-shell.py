"""Replace the decorative plate shell with a continuous fractured geode shell.

This is the topology repair for the literal missing-plane defect reported in live play.
The earlier Voronoi shell was assembled from independently displaced plates. Even with a
backing core, those plate boundaries are real open boundaries and can read as missing faces.
This pass makes the visible rock skin one shared-vertex manifold surface and leaves exactly
one intentional boundary: the crystal cutaway mouth.

Run after generate-geode-models.py and before repair-geode-topology.py / surface refinement:

    blender --background assets/models/source/geode-sample.blend --python tools/rebuild-geode-watertight-shell.py

The existing GeodeShell material, transform and game metadata are preserved. Gameplay,
collision, biome tinting and progression contracts are untouched.
"""
from pathlib import Path
import math
import random
import bpy
import bmesh

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/models/source/geode-sample.blend"
RADIUS = 0.50
OPENING_COS = 0.52
CUT_DIRECTION = (1.0, 0.0, 0.34)
SUBDIVISIONS = 4
SEED = 20260917


def normalise(v):
    length = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / length for c in v)


def dot(a, b):
    return sum(a[i] * b[i] for i in range(3))


def direction(co):
    return normalise((co.x, co.y, co.z))


def noise(d):
    # Stable multi-frequency rock breakup. Continuous because displacement is evaluated per
    # shared vertex; unlike the old plate construction it cannot pull neighboring edges apart.
    x, y, z = d
    coarse = math.sin(x * 8.7 + y * 3.1) * math.sin(z * 7.3 - y * 4.9)
    medium = math.sin(x * 19.1 - z * 11.7 + y * 6.3)
    fine = math.sin((x + z) * 37.0 + y * 29.0)
    return coarse * 0.55 + medium * 0.30 + fine * 0.15


cut = normalise(CUT_DIRECTION)
old = bpy.data.objects.get("GeodeShell")
if old is None or old.type != "MESH":
    raise RuntimeError("Missing GeodeShell; regenerate geode source first")
if len(old.data.materials) != 1:
    raise RuntimeError("GeodeShell material contract drift")
material = old.data.materials[0]
parent = old.parent
matrix = old.matrix_world.copy()
props = {key: old[key] for key in old.keys()}

mesh = bpy.data.meshes.new("GeodeShell.Watertight")
bm = bmesh.new()
bmesh.ops.create_icosphere(bm, subdivisions=SUBDIVISIONS, radius=RADIUS)

# Displace shared vertices radially. The stronger low-frequency term preserves the rough,
# naturally fractured silhouette without manufacturing disconnected plates.
for vert in bm.verts:
    d = direction(vert.co)
    radial = 1.0 + noise(d) * 0.045
    vert.co *= radial

# Carve the deliberate display mouth only. Deleting complete faces guarantees the remaining
# boundary is an edge loop in the shared mesh rather than a collection of plate-sized holes.
remove = []
for face in bm.faces:
    c = face.calc_center_median()
    if dot(direction(c), cut) > OPENING_COS:
        remove.append(face)
bmesh.ops.delete(bm, geom=remove, context="FACES")

# Recalculate normals from the now-final topology.
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(mesh)
bm.free()
mesh.update()

new = bpy.data.objects.new("GeodeShell.Watertight", mesh)
bpy.context.collection.objects.link(new)
new.data.materials.append(material)
new.parent = parent
new.matrix_world = matrix
for key, value in props.items():
    new[key] = value
bpy.data.objects.remove(old, do_unlink=True)
new.name = "GeodeShell"
new.data.name = "GeodeShell"

# Hard topology acceptance. Every edge must have two faces except the single intentional mouth.
bm = bmesh.new()
bm.from_mesh(new.data)
boundary = [e for e in bm.edges if len(e.link_faces) == 1]
nonmanifold = [e.index for e in bm.edges if len(e.link_faces) not in (1, 2)]
illegal = []
adjacency = {}
for edge in boundary:
    for vert in edge.verts:
        d = direction(vert.co)
        if dot(d, cut) < OPENING_COS - 0.18:
            illegal.append(edge.index)
        adjacency.setdefault(vert.index, set()).update(v.index for v in edge.verts if v.index != vert.index)

# One boundary loop means every boundary vertex has degree two and the loop is connected.
bad_degree = [v for v, neighbors in adjacency.items() if len(neighbors) != 2]
components = 0
unseen = set(adjacency)
while unseen:
    components += 1
    stack = [unseen.pop()]
    while stack:
        current = stack.pop()
        for nxt in adjacency[current]:
            if nxt in unseen:
                unseen.remove(nxt)
                stack.append(nxt)
bm.free()

if nonmanifold:
    raise RuntimeError("GeodeShell has non-manifold edges: %s" % nonmanifold[:16])
if illegal:
    raise RuntimeError("GeodeShell has missing planes outside the intentional mouth: %s" % illegal[:16])
if not boundary or bad_degree or components != 1:
    raise RuntimeError("GeodeShell mouth topology invalid: boundary=%d bad-degree=%d components=%d" %
                       (len(boundary), len(bad_degree), components))

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), compress=True)
print("GEODE WATERTIGHT SHELL: %d faces; one intentional mouth loop; zero exterior missing planes" % len(new.data.polygons))
