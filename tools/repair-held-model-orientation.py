#!/usr/bin/env python3
"""Repair long held Magenheim Blender assets to the canonical +Y attach-space convention.

Run with Blender from the repository root:

    blender --background --factory-startup --python tools/repair-held-model-orientation.py

This is a source-asset repair, not a Unity rotation shim.  For each explicitly owned long-held
asset, the script measures evaluated mesh bounds, rotates the complete authored hierarchy when
X or Z is the dominant axis, applies rotation, and verifies +Y dominance before saving. Assets
already authored along Y are left unchanged. Grip/working-end direction is not guessed from
bounds; this pass fixes the sideways-axis defect only and deliberately fails closed if the
result remains ambiguous.
"""
from pathlib import Path
import math
import bpy

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"
MIN_Y_DOMINANCE = 1.15
NAMES = (
    "crystal-weapon-sword", "crystal-weapon-greatsword", "crystal-weapon-axe",
    "crystal-weapon-battleaxe", "crystal-weapon-mace", "crystal-weapon-spear",
    "crystal-weapon-knife", "crystal-weapon-atgeir",
)


def world_bounds():
    points = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        evaluated = obj.evaluated_get(depsgraph)
        points.extend(evaluated.matrix_world @ v.co for v in evaluated.data.vertices)
    if not points:
        raise RuntimeError("held asset contains no mesh vertices")
    lo = [min(p[i] for p in points) for i in range(3)]
    hi = [max(p[i] for p in points) for i in range(3)]
    return lo, hi


def spans():
    lo, hi = world_bounds()
    return [hi[i] - lo[i] for i in range(3)]


def rotate_roots(axis):
    roots = [obj for obj in bpy.context.scene.objects if obj.parent is None]
    if not roots:
        raise RuntimeError("held asset has no root objects")
    # X-long -> +Y by +90 around Z. Z-long -> +Y by -90 around X.
    rotation = (0.0, 0.0, math.pi / 2.0) if axis == 0 else (-math.pi / 2.0, 0.0, 0.0)
    for obj in roots:
        obj.rotation_euler.rotate_axis("Z" if axis == 0 else "X", rotation[2] if axis == 0 else rotation[0])
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    bpy.ops.object.select_all(action="DESELECT")


def repair(path):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    before = spans()
    dominant = max(range(3), key=lambda i: before[i])
    changed = False
    if dominant != 1:
        rotate_roots(dominant)
        changed = True
    after = spans()
    minor = max(after[0], after[2], 1e-6)
    if after[1] < minor * MIN_Y_DOMINANCE:
        raise RuntimeError("%s remains ambiguous after repair: x/y/z %.3f/%.3f/%.3f" % (path.name, *after))
    if changed:
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print("%s: %s x/y/z %.3f/%.3f/%.3f" % (path.name, "REPAIRED" if changed else "PASS", *after))


def main():
    paths = [SOURCE / (name + ".blend") for name in NAMES]
    paths += sorted(p for p in SOURCE.glob("*staff*.blend") if p not in paths)
    missing = [str(p.relative_to(ROOT)) for p in paths if not p.exists()]
    if missing:
        raise RuntimeError("missing held source assets: " + ", ".join(missing))
    for path in paths:
        repair(path)
    print("HELD SOURCE ORIENTATION REPAIR COMPLETE: %d assets canonicalized" % len(paths))


if __name__ == "__main__":
    main()
