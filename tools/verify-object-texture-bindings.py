"""Check per-object textures in saved Blender sources, runtime payloads and GLBs."""
import json
import struct
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[1] / "assets/models"
bindings = json.loads((ROOT / "texture-overrides.json").read_text())
count = 0
for model_id, expected in bindings.items():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT / "source" / (model_id + ".blend")))
    runtime = json.loads((ROOT / "runtime" / (model_id + ".model.json")).read_text())
    parts = {part["name"]: part for part in runtime["parts"]}
    data = (ROOT / "glb" / (model_id + ".glb")).read_bytes()
    length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + length])
    materials = {material["name"]: material for material in gltf["materials"]}
    for name, filename in expected.items():
        context = f"{model_id}/{name}"
        obj = bpy.context.scene.objects[name]
        material = obj.data.materials[0]
        tree = material.node_tree
        nodes = [node for node in tree.nodes if node.type == "TEX_IMAGE"]
        assert len(nodes) == 1, (context, "ambiguous source texture")
        image = nodes[0].image
        assert image and image.packed_file, (context, "source texture is not packed")
        assert bytes(image.packed_file.data) == (ROOT / "textures" / filename).read_bytes(), (context, "stale source pixels")
        part = parts[name]["material"]
        assert part["texture"] == filename, (context, "runtime binding drift")
        assert material["magenheim_material_name"] == part["name"], (context, "semantic drift")
        tint = tree.nodes["Magenheim Surface Tint"]
        assert tint.blend_type == "MULTIPLY" and tint.inputs[0].default_value == 1, context
        assert all(abs(a-b) < 1e-6 for a,b in zip(tint.inputs[2].default_value, part["color"])), (context, "tint drift")
        bsdf = tree.nodes.get("Principled BSDF")
        assert any(link.from_node == nodes[0] and link.to_socket == tint.inputs[1] for link in tree.links), context
        assert any(link.from_node == tint and link.to_socket == bsdf.inputs["Base Color"] for link in tree.links), context
        pbr = materials[material.name]["pbrMetallicRoughness"]
        texture = gltf["textures"][pbr["baseColorTexture"]["index"]]
        assert "bufferView" in gltf["images"][texture["source"]], (context, "GLB texture is not embedded")
        count += 1
print(f"VERIFIED {count} object textures across {len(bindings)} models: packed pixels, tint, semantics, runtime and GLB bindings")
