"""Materialize the six authored Underworld Deepstone runtime models as editable Blender sources.

Usage:
    tools/blender.ps1 materialize-underworld-deepstone-sources

The 2026-09-29 Deepstone quality pass moved the Conclave away from six instances of the same
single-part standing-stone mesh. Runtime geometry is now six canonical multi-part model payloads:
bloom, tide, cinder, rime, fracture and decay. This script recreates editable .blend sources
directly from those exact runtime payloads, preserving vertices, UVs, material families, emission,
collider metadata and runtime-light metadata without hand-transcribing a second geometry source.

The generated .blend files are therefore round-trip inspection/edit sources for the shipped model
payloads. Normal production edits should thereafter be made in Blender and exported through the
normal Magenheim model pipeline.
"""
import bpy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "assets" / "models" / "runtime"
SOURCE = ROOT / "assets" / "models" / "source"
TEXTURES = ROOT / "assets" / "models" / "textures"

MODEL_IDS = (
    "underworld-deepstone-bloom",
    "underworld-deepstone-tide",
    "underworld-deepstone-cinder",
    "underworld-deepstone-rime",
    "underworld-deepstone-fracture",
    "underworld-deepstone-decay",
)


def load_material(data):
    material = bpy.data.materials.new(data["name"])
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    color = data.get("color", [1, 1, 1, 1])
    shader.inputs["Base Color"].default_value = tuple(color)
    shader.inputs["Roughness"].default_value = float(data.get("roughness", 0.7))
    shader.inputs["Metallic"].default_value = float(data.get("metallic", 0.0))

    emission = data.get("emission", [0, 0, 0])
    if "Emission Color" in shader.inputs:
        shader.inputs["Emission Color"].default_value = (*emission[:3], 1.0)
        shader.inputs["Emission Strength"].default_value = 1.0 if sum(emission[:3]) > 0 else 0.0
    elif "Emission" in shader.inputs:
        shader.inputs["Emission"].default_value = (*emission[:3], 1.0)

    texture_name = data.get("texture")
    if texture_name:
        texture_path = TEXTURES / texture_name
        if not texture_path.exists():
            raise FileNotFoundError(texture_path)
        image = bpy.data.images.load(str(texture_path), check_existing=True)
        node = material.node_tree.nodes.new("ShaderNodeTexImage")
        node.image = image
        material.node_tree.links.new(node.outputs["Color"], shader.inputs["Base Color"])

    return material


def build_part(part):
    mesh = bpy.data.meshes.new(part["name"])
    vertices = [tuple(row) for row in part["vertices"]]
    triangles = part["triangles"]
    faces = [tuple(triangles[i:i+3]) for i in range(0, len(triangles), 3)]
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    uv_rows = part["uv"]
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        for loop_index in polygon.loop_indices:
            vertex_index = mesh.loops[loop_index].vertex_index
            uv_layer.data[loop_index].uv = tuple(uv_rows[vertex_index])

    obj = bpy.data.objects.new(part["name"], mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(load_material(part["material"]))
    obj["game_node_path"] = part.get("path", part["name"])
    obj["game_collision"] = bool(part.get("collider", False))
    obj["game_crystal"] = "null"
    return obj


def build_model(model_id):
    payload_path = RUNTIME / f"{model_id}.model.json"
    if not payload_path.exists():
        raise FileNotFoundError(payload_path)
    payload = json.loads(payload_path.read_text(encoding="utf-8"))

    bpy.ops.wm.read_factory_settings(use_empty=True)
    for part in payload["parts"]:
        build_part(part)

    scene = bpy.context.scene
    scene["model_id"] = model_id
    scene["runtime_lights"] = json.dumps(payload.get("lights", []), separators=(",", ":"))
    bpy.context.preferences.filepaths.save_version = 0

    destination = SOURCE / f"{model_id}.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(destination), compress=True)
    print(f"AUTHORED {model_id}: {len(payload['parts'])} parts -> {destination}")


def main():
    SOURCE.mkdir(parents=True, exist_ok=True)
    for model_id in MODEL_IDS:
        build_model(model_id)


main()
