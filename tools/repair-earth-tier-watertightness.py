#!/usr/bin/env python3
"""Close the crystal tier meshes.

    tools/blender.ps1 repair-earth-tier-watertightness [model-id ...]

The five crystal tier assets are not watertight: rough 18, simple 8, crystal 16, advanced 32
and master 58 edges are either boundary edges or duplicated windings.
`verify-model-geometry.py` misses this because it tests that faces point outward, which they
do; `verify-earth-assets.py` catches it but was never wired into build.ps1.

It matters because these are live. EarthAssets.ReplaceVisual loads `<asset>.mesh.json` from
assets/earth at runtime, so this is the geometry the player handles through the entire
refinement loop, and an unclosed solid reads as holes and flickering backfaces.

The pass is conservative and shape-preserving: weld only genuinely coincident vertices, fill
any boundary that remains, and recalculate outward normals. It refuses to save a mesh it did
not actually close, so a model needing real re-authoring is reported rather than silently
half-repaired.
"""
import sys
from collections import Counter
from pathlib import Path

import bmesh
import bpy

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
WELD_DISTANCE = 1e-5

DEFAULT_TARGETS = ('earth-rough', 'earth-simple', 'earth-crystal', 'earth-advanced', 'earth-master')


def open_edge_count(bm):
    """Edges that bound nothing or bound more than two faces: holes and non-manifold seams."""
    return sum(1 for edge in bm.edges if len(edge.link_faces) != 2)


def process(name):
    path = SOURCE / f'{name}.blend'
    if path.resolve().parent != SOURCE.resolve() or not path.is_file():
        raise SystemExit(f'Invalid or missing source: {path}')

    bpy.ops.wm.open_mainfile(filepath=str(path))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not meshes:
        raise SystemExit(f'{name}: no mesh objects')

    total_before = total_after = 0
    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        before = open_edge_count(bm)
        total_before += before
        if before:
            bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=WELD_DISTANCE)
            open_edges = [e for e in bm.edges if len(e.link_faces) < 2]
            if open_edges:
                bmesh.ops.holes_fill(bm, edges=open_edges)
            bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        after = open_edge_count(bm)
        total_after += after
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()

    if total_after:
        raise SystemExit(
            f'{name}: still {total_after} open/non-manifold edges after cleanup '
            f'(was {total_before}); it needs re-authoring, refusing to save')

    if total_before == 0:
        print(f'SKIPPED {name}: already watertight', flush=True)
        return

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print(f'REPAIRED {name}: {total_before} open edges -> 0', flush=True)


targets = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else list(DEFAULT_TARGETS)
for target in targets:
    process(target)
print(f'Watertightness pass complete over {len(targets)} asset(s).', flush=True)
