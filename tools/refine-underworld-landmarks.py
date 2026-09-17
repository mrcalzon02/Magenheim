#!/usr/bin/env python3
"""Raise the Underworld's structural landmarks toward the districts they sit in.

    tools/blender.ps1 refine-underworld-landmarks [model-id ...]

The Underworld is 130 of the 281 models. Its districts are the best assets in the library at
10,012-11,070 triangles and its creatures are healthy at 1,404-4,212, but a small set of
structural landmarks was left far behind: the entrance a player arrives through, the passages
connecting those districts, the standing stone, the dais, and the Dark Throne arena
centrepiece. They are 76 to 880 triangles. The mismatch is most visible exactly where it
matters, because a corridor is the first thing seen after a chamber.

The pass adds edge definition rather than re-authoring form. A Bevel modifier chamfers hard
edges so stone reads as cut stone under the Underworld's low light instead of as flat facets,
and export-model-assets.py evaluates modifiers, so the exported payload carries the detail
while the authored base mesh stays editable. Subdivision is deliberately not used: it would
round architectural corners that are meant to be sharp.

Bevel width is a fraction of each model's own size, so one setting suits a standing stone and
a throne. Models that already carry a Bevel are left alone, which makes the pass idempotent.
"""
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'

DEFAULT_TARGETS = (
    'underworld-standing-stone',
    'underworld-dais',
    'deep-fracture-entrance',
    'deep-fracture-passage',
    'deep-fracture-traversal',
    'dark-throne',
)

WIDTH_FRACTION = 0.012   # of the model's longest dimension
SEGMENTS = 2
ANGLE_LIMIT = 0.6109     # 35 degrees: chamfer real corners, ignore near-flat joins


def evaluated_triangles(objects):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    total = 0
    for obj in objects:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        total += len(mesh.loop_triangles)
        evaluated.to_mesh_clear()
    return total


def model_span(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    low = Vector([min(p[i] for p in points) for i in range(3)])
    high = Vector([max(p[i] for p in points) for i in range(3)])
    return max(high - low)


def process(name):
    path = SOURCE / f'{name}.blend'
    if path.resolve().parent != SOURCE.resolve() or not path.is_file():
        raise SystemExit(f'Invalid or missing source: {path}')

    bpy.ops.wm.open_mainfile(filepath=str(path))
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not objects:
        raise SystemExit(f'{name}: no mesh objects')

    bpy.context.view_layer.update()
    before = evaluated_triangles(objects)
    span = model_span(objects)
    if span <= 0:
        raise SystemExit(f'{name}: degenerate bounds')
    width = span * WIDTH_FRACTION

    # Recalculate outward winding first, and do it whether or not this asset needs a bevel.
    # dark-throne carried four inverted faces that its stale export had been hiding, so a
    # revision pass that only added geometry would have shipped the defect with more detail.
    inverted_fixed = 0
    for obj in objects:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        before_normals = [f.normal.copy() for f in bm.faces]
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        inverted_fixed += sum(1 for old, face in zip(before_normals, bm.faces)
                              if old.dot(face.normal) < 0)
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()

    added = 0
    for obj in objects:
        if any(m.type == 'BEVEL' for m in obj.modifiers):
            continue
        bevel = obj.modifiers.new(name='MagenheimLandmarkBevel', type='BEVEL')
        bevel.width = width
        bevel.segments = SEGMENTS
        bevel.limit_method = 'ANGLE'
        bevel.angle_limit = ANGLE_LIMIT
        bevel.harden_normals = False
        bevel.miter_outer = 'MITER_ARC'
        added += 1

    if added == 0:
        if inverted_fixed:
            bpy.context.preferences.filepaths.save_version = 0
            bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
            print(f'REVISED {name}: already beveled; corrected {inverted_fixed} inverted face(s)',
                  flush=True)
        else:
            print(f'SKIPPED {name}: every part already carries a bevel', flush=True)
        return

    bpy.context.view_layer.update()
    after = evaluated_triangles(objects)
    if after <= before:
        # A bevel that produces nothing means the form has no hard edges to cut, so this pass
        # has nothing to offer it and the asset needs real re-authoring instead. Report and
        # leave it untouched rather than aborting the run and losing the remaining models.
        print(f'UNCHANGED {name}: bevel produced no geometry ({before}); needs re-authoring, '
              f'not a chamfer', flush=True)
        return

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print(f'REVISED {name}: {before} -> {after} triangles ({added} part(s) beveled, '
          f'{inverted_fixed} inverted face(s) corrected)', flush=True)


targets = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else list(DEFAULT_TARGETS)
for target in targets:
    process(target)
print(f'Underworld landmark pass complete over {len(targets)} asset(s).', flush=True)
