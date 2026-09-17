"""Refine the rebuilt geode's authored surface response without changing its gameplay contract.

Run after tools/generate-geode-models.py and before the normal model export pipeline:

    blender --background assets/models/source/geode-sample.blend --python tools/refine-geode-surface-fidelity.py

The first 0.0.52 live pass found parts of the rebuilt geode still reading as textureless even
though every exported part references a non-flat albedo.  The problem is therefore legibility,
not texture presence.  This pass gives each of the five authored surfaces a distinct material
response and derives a packed tangent-space micro-normal from its existing greyscale albedo.
It deliberately preserves material names because GeodeVisuals.Apply uses `.interior-` and
`interior-bright-` as the biome-tint contract.

No geometry, object names, collision flags, UVs, or biome tint identities are changed here.
"""
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/models/source/geode-sample.blend"
EXPECTED = {
    "GeodeShell": (0.92, 0.22),
    "GeodeCore": (0.98, 0.34),
    "GeodeCavity": (0.72, 0.42),
    "GeodeBanding": (0.38, 0.28),
    "GeodeDruzy": (0.16, 0.58),
}


def require_principled(material):
    if not material or not material.use_nodes or not material.node_tree:
        raise RuntimeError("Geode material is not node-authored")
    shader = material.node_tree.nodes.get("Principled BSDF")
    if shader is None:
        raise RuntimeError("Geode material has no Principled BSDF: " + material.name)
    return shader


def source_image(material):
    images = [node for node in material.node_tree.nodes if node.type == "TEX_IMAGE" and node.image]
    if len(images) != 1:
        raise RuntimeError("Expected exactly one authored geode albedo on %s, found %d" % (material.name, len(images)))
    return images[0]


def add_micro_normal(material, albedo_node, strength):
    tree = material.node_tree
    # Idempotent: remove only nodes owned by this refinement pass.
    for node in list(tree.nodes):
        if node.name.startswith("MagenheimGeodeSurface_"):
            tree.nodes.remove(node)

    bump = tree.nodes.new("ShaderNodeBump")
    bump.name = "MagenheimGeodeSurface_Bump"
    bump.label = "Magenheim geode authored micro-surface"
    bump.inputs["Strength"].default_value = strength
    bump.inputs["Distance"].default_value = 0.055
    bump.inputs["Invert"].default_value = 0.0
    tree.links.new(albedo_node.outputs["Color"], bump.inputs["Height"])
    shader = require_principled(material)
    tree.links.new(bump.outputs["Normal"], shader.inputs["Normal"])


def refine_object(name, roughness, bump_strength):
    obj = bpy.data.objects.get(name)
    if obj is None or obj.type != "MESH":
        raise RuntimeError("Missing required geode mesh: " + name)
    if len(obj.data.materials) != 1:
        raise RuntimeError("Expected one material on %s, found %d" % (name, len(obj.data.materials)))

    material = obj.data.materials[0]
    shader = require_principled(material)
    albedo = source_image(material)
    shader.inputs["Roughness"].default_value = roughness
    add_micro_normal(material, albedo, bump_strength)

    # Keep the source image visibly detailed at the distances where a held geode is read.
    albedo.interpolation = "Linear"
    albedo.extension = "REPEAT"
    if not albedo.image.packed_file:
        albedo.image.pack()
    albedo.image.update()


for object_name, response in EXPECTED.items():
    refine_object(object_name, response[0], response[1])

# Fail closed if the model has silently drifted to extra/missing mesh parts.
actual = {obj.name for obj in bpy.context.scene.objects if obj.type == "MESH"}
if actual != set(EXPECTED):
    raise RuntimeError("Geode mesh contract drift: expected %s, found %s" % (sorted(EXPECTED), sorted(actual)))

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), compress=True)
print("GEODE SURFACE FIDELITY PASS: 5/5 authored parts refined; material/tint identities preserved")
