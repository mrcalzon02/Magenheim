#!/usr/bin/env python3
"""Verify the editable Nowhere King sword Blender sources before runtime export."""
import bpy
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"
REVISION = "nowhere-king-last-argument-r1"
SPECS = {
    "nowhere-king-sword-firmament": {
        "required": {"firmament-frame", "firmament-cosmos", "firmament-heart"},
        "min_parts": 18,
        "token": "firmament-star-",
    },
    "nowhere-king-sword-null-gate": {
        "required": {"null-gate-absence", "null-gate-filament", "null-gate-ring-0", "null-gate-ring-1", "null-gate-ring-2"},
        "min_parts": 16,
        "token": "null-gate-",
    },
}


def verify(model_id):
    path = SOURCE / (model_id + ".blend")
    if not path.is_file():
        raise FileNotFoundError(path)
    bpy.ops.wm.open_mainfile(filepath=str(path))
    scene = bpy.context.scene
    if scene.get("model_id") != model_id:
        raise RuntimeError(model_id + ": wrong model_id")
    if scene.get("derived_from") != "crystal-weapon-sword":
        raise RuntimeError(model_id + ": lost crystal sword ancestry")
    if scene.get("nowhere_king_sword_authoring") != REVISION:
        raise RuntimeError(model_id + ": stale authoring revision")

    meshes = [o for o in scene.objects if o.type == "MESH"]
    names = {o.name for o in meshes}
    spec = SPECS[model_id]
    if len(meshes) < spec["min_parts"]:
        raise RuntimeError("%s: detail regression %d < %d" % (model_id, len(meshes), spec["min_parts"]))
    if not spec["required"].issubset(names):
        raise RuntimeError(model_id + ": missing required parts " + repr(sorted(spec["required"] - names)))
    if not any(o.name.startswith(spec["token"]) for o in meshes):
        raise RuntimeError(model_id + ": identity geometry missing")

    points = []
    for obj in meshes:
        if "game_node_path" not in obj or not str(obj["game_node_path"]).startswith("attach/magenheim."):
            raise RuntimeError(model_id + "/" + obj.name + ": missing authoritative node path")
        if bool(obj.get("game_collision", True)):
            raise RuntimeError(model_id + "/" + obj.name + ": boss sword visual unexpectedly collides")
        if json.loads(obj.get("game_crystal", "null")) is not None:
            raise RuntimeError(model_id + "/" + obj.name + ": boss sword visual unexpectedly owns crystal gameplay")
        if not obj.data.uv_layers:
            raise RuntimeError(model_id + "/" + obj.name + ": missing UVs")
        for corner in obj.bound_box:
            points.append(obj.matrix_world @ __import__("mathutils").Vector(corner))

    lo_z = min(v.z for v in points)
    hi_z = max(v.z for v in points)
    width = max(v.x for v in points) - min(v.x for v in points)
    if lo_z > -0.45 or hi_z < 0.95 or hi_z > 1.08:
        raise RuntimeError("%s: invalid held envelope %.3f..%.3f" % (model_id, lo_z, hi_z))
    if width < 0.16 or width > 0.36:
        raise RuntimeError("%s: unreadable blade width %.3f" % (model_id, width))

    materials = [slot.material for obj in meshes for slot in obj.material_slots if slot.material]
    names_lower = " ".join(m.name.lower() for m in materials)
    if model_id.endswith("firmament"):
        stars = sum(1 for o in meshes if o.name.startswith("firmament-star-"))
        if stars < 10 or "cosmos" not in names_lower or "starlight" not in names_lower:
            raise RuntimeError(model_id + ": galaxy identity incomplete")
    else:
        rings = sum(1 for o in meshes if o.name.startswith("null-gate-ring-"))
        if rings != 3 or "event-horizon" not in names_lower or "void" not in names_lower:
            raise RuntimeError(model_id + ": void-portal identity incomplete")

    print("VERIFIED", model_id, len(meshes), "parts", "%.3f..%.3f" % (lo_z, hi_z), flush=True)


requested = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(SPECS)
unknown = [value for value in requested if value not in SPECS]
if unknown:
    raise SystemExit("Unknown Nowhere King sword model(s): " + ", ".join(unknown))
for model_id in requested:
    verify(model_id)
