#!/usr/bin/env python3
"""Report the authored frame of each held Magenheim weapon source, correct ones included.

    tools/blender.ps1 report-held-model-frames

Bounds alone cannot decide this. They do not encode roll about the long axis, which is what
"held sideways" means, and they cannot distinguish a blade whose flat faces X from one whose
flat faces Z once the silhouette is similar. This reads the authored sources instead and prints,
per asset, the object-level rotations actually baked in and the orientation of the dominant flat
of the working end.

The point of comparison is the three assets reported correct in hand -- axe, battleaxe and bow --
against the five reported wrong -- crossbow, greatsword, knife, mace and spear. Whatever the
correct three share and the wrong five do not is the convention, read off the assets rather than
assumed.
"""
import math
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"

CORRECT = ("crystal-weapon-axe", "crystal-weapon-battleaxe", "crystal-weapon-bow")
WRONG = ("crystal-weapon-crossbow", "crystal-weapon-greatsword", "crystal-weapon-knife",
         "crystal-weapon-mace", "crystal-weapon-spear")
UNREPORTED = ("crystal-weapon-sword", "crystal-weapon-atgeir")


def mesh_objects():
    return [o for o in bpy.context.scene.objects if o.type == 'MESH']


def evaluated_points(obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        return [obj.matrix_world @ v.co for v in mesh.vertices]
    finally:
        evaluated.to_mesh_clear()


def normal_histogram(objs, y_lo, y_hi):
    """Which axis do the faces of the working end point along? That is the flat's orientation."""
    depsgraph = bpy.context.evaluated_depsgraph_get()
    area = [0.0, 0.0, 0.0]
    for obj in objs:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            for poly in mesh.polygons:
                centre = obj.matrix_world @ poly.center
                if not (y_lo <= centre.y <= y_hi):
                    continue
                normal = (obj.matrix_world.to_3x3() @ poly.normal).normalized()
                for axis in range(3):
                    area[axis] += abs(normal[axis]) * poly.area
        finally:
            evaluated.to_mesh_clear()
    total = sum(area) or 1.0
    return [a / total for a in area]


def report(name, label):
    path = SOURCE / f"{name}.blend"
    if not path.exists():
        print(f"REPORTED {name} [{label}] MISSING SOURCE", flush=True)
        return
    bpy.ops.wm.open_mainfile(filepath=str(path))
    objs = mesh_objects()
    if not objs:
        print(f"REPORTED {name} [{label}] NO MESHES", flush=True)
        return

    points = [p for o in objs for p in evaluated_points(o)]
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    size = hi - lo

    # Working end = top quarter along the long axis, which every asset authors as Y.
    span = size.y
    work = normal_histogram(objs, hi.y - span * 0.25, hi.y)
    grip = normal_histogram(objs, lo.y, lo.y + span * 0.25)

    rotations = sorted({
        tuple(round(math.degrees(a), 1) for a in o.rotation_euler)
        for o in objs
    })
    print(
        f"REPORTED {name} [{label}] "
        f"size=({size.x:.2f},{size.y:.2f},{size.z:.2f}) "
        f"workFaceArea=(X{work[0]:.2f},Y{work[1]:.2f},Z{work[2]:.2f}) "
        f"gripFaceArea=(X{grip[0]:.2f},Y{grip[1]:.2f},Z{grip[2]:.2f}) "
        f"objectRotations={rotations[:4]} objects={len(objs)}",
        flush=True)


for asset in CORRECT:
    report(asset, "CORRECT")
for asset in WRONG:
    report(asset, "WRONG")
for asset in UNREPORTED:
    report(asset, "unreported")
print("REPORTED held-model frame comparison complete", flush=True)
