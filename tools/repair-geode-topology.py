"""Repair literal exterior holes in the rebuilt geode.

Run after generate-geode-models.py and before surface refinement/export:

    blender --background assets/models/source/geode-sample.blend --python tools/repair-geode-topology.py

The fractured GeodeShell is intentionally made from displaced independent plates. Those plates
must never be the only closure of the rock body: their seams are visual fracture grooves, not
holes through the model. This pass replaces GeodeCore with a continuous backing shell whose only
boundary is the deliberate cutaway mouth. It preserves the existing GeodeCore material and object
contract, so runtime tint/collision/gameplay code is unchanged.
"""
from pathlib import Path
import math
import bpy
import bmesh

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/models/source/geode-sample.blend"
CORE_RADIUS = 0.445
OPENING_COS = 0.52
CUT_DIRECTION = (1.0, 0.0, 0.34)
SUBDIVISIONS = 3


def normalise(v):
    length = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / length for c in v)


def dot(a, b):
    return sum(a[i] * b[i] for i in range(3))


def face_direction(face):
    center = face.calc_center_median()
    return normalise((center.x, center.y, center.z))


def boundary_edges(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    count = sum(1 for edge in bm.edges if len(edge.link_faces) == 1)
    bm.free()
    return count


cut = normalise(CUT_DIRECTION)
old = bpy.data.objects.get("GeodeCore")
if old is None or old.type != "MESH":
    raise RuntimeError("Missing GeodeCore; regenerate geode source first")
if len(old.data.materials) != 1:
    raise RuntimeError("GeodeCore material contract drift")
material = old.data.materials[0]
parent = old.parent
matrix = old.matrix_world.copy()
props = {key: old[key] for key in old.keys()}

# Build one shared-vertex closed icosphere, then carve only the intentional mouth. Using shared
# topology here is deliberate: unlike the decorative plate layer, this backing body must be
# manifold everywhere away from the mouth.
mesh = bpy.data.meshes.new("GeodeCore")
bm = bmesh.new()
bmesh.ops.create_icosphere(bm, subdivisions=SUBDIVISIONS, radius=CORE_RADIUS)
remove = [face for face in bm.faces if dot(face_direction(face), cut) > OPENING_COS]
bmesh.ops.delete(bm, geom=remove, context="FACES")
bm.to_mesh(mesh)
bm.free()
mesh.update()

new = bpy.data.objects.new("GeodeCore.TopologyRepair", mesh)
bpy.context.collection.objects.link(new)
new.data.materials.append(material)
new.parent = parent
new.matrix_world = matrix
for key, value in props.items():
    new[key] = value

# Remove the old independently-authored backing mesh only after the replacement exists.
bpy.data.objects.remove(old, do_unlink=True)
new.name = "GeodeCore"
new.data.name = "GeodeCore"

# Acceptance: the backing shell may have boundary edges, but every boundary vertex must lie on
# the cutaway side. Any boundary elsewhere is another missing plane and fails the build.
bm = bmesh.new()
bm.from_mesh(new.data)
illegal = []
for edge in bm.edges:
    if len(edge.link_faces) != 1:
        continue
    for vert in edge.verts:
        direction = normalise(tuple(vert.co))
        if dot(direction, cut) < OPENING_COS - 0.16:
            illegal.append(edge.index)
            break
mouth_edges = sum(1 for edge in bm.edges if len(edge.link_faces) == 1)
bm.free()
if illegal:
    raise RuntimeError("Geode backing still has illegal exterior boundary edges: %s" % illegal[:16])
if mouth_edges == 0:
    raise RuntimeError("Geode topology repair accidentally sealed the intentional cutaway mouth")

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), compress=True)
print("GEODE TOPOLOGY REPAIR: continuous backing installed; %d intentional mouth boundary edges; 0 illegal exterior boundaries" % mouth_edges)
