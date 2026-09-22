"""Render Rootforged icons from the same Blender sources used at runtime."""
import json,sys
from pathlib import Path
import importlib.util
# Limit the imported renderer entry point to this family; its own catalog excludes these IDs.
sys.argv=[__file__,"--","rootforged-only"]
spec=importlib.util.spec_from_file_location("placeable_icons",Path(__file__).with_name("render-placeable-icons.py"))
renderer=importlib.util.module_from_spec(spec);spec.loader.exec_module(renderer)
frame_and_render=renderer.frame_and_render
R=Path(__file__).resolve().parents[1]
entries=json.loads((R/'assets/models/catalog.json').read_text())
models=[e for e in entries if e['id'].startswith('rootforged-')]
assert len(models)==11
for e in models:
 frame_and_render(e,R/'assets/earth'/(e['id']+'.icon.png'))
 print('RENDERED',e['id'],flush=True)
