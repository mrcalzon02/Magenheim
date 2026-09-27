#!/usr/bin/env python3
"""Bind the authored material-family library into editable Blender sources.

Run with:
    blender --background --python tools/author-library-surfaces.py -- [model-id ...]

Only blank materials and obsolete magenheim.surface.* bakes are changed. Existing detailed image
textures are authoritative and are left alone.
"""
import bpy
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"
sys.path.insert(0, str(Path(__file__).resolve().parent))
from model_surface_authoring import bind_surface_if_needed

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
files = [SOURCE / (model_id + ".blend") for model_id in args] if args else sorted(SOURCE.glob("*.blend"))
changed_models = 0
changed_materials = 0

for path in files:
    if path.resolve().parent != SOURCE.resolve() or not path.is_file():
        raise SystemExit("Invalid model source: " + str(path))
    bpy.ops.wm.open_mainfile(filepath=str(path))
    changed = 0
    for material in bpy.data.materials:
        if material.use_nodes and bind_surface_if_needed(bpy, material):
            changed += 1
    if changed:
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
        changed_models += 1
        changed_materials += changed
        print(f"AUTHORED {path.stem}: {changed} material(s)", flush=True)

print(f"Authored surface migration changed {changed_materials} material(s) across {changed_models} model(s); "
      f"existing detailed image materials were preserved.", flush=True)
