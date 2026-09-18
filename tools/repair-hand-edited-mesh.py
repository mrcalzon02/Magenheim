#!/usr/bin/env python3
"""Clean the mesh defects a hand edit leaves behind, in the authoritative source.

    tools/blender.ps1 repair-hand-edited-mesh geode-sample [more-model-ids ...]

Editing a source by hand is normal and expected; the defects it leaves are mechanical and
predictable. A rebuilt geode shell arrived with 52 zero-area triangles and 35 edges shared by more
than two faces, which `verify-model-assets` and `verify-geode-shell-topology` both reject. Those are
not art decisions, they are leftovers of merging and displacing geometry, and the fix belongs in the
.blend rather than in the exported payload.

The pass is deliberately conservative. It dissolves faces with no area and removes
loose vertices that render nothing. It does **not** merge coincident vertices, recalculate normals,
dissolve limited-angle geometry, triangulate or decimate -- anything that would change what the
artist shaped. Merging was tried and reverted: the geode shell is authored as displaced independent
plates, so welding their touching boundaries turned 8 non-manifold edges into 32, and every part
carries duplicate vertices that hold split normals. Each mesh is reported before and
after so a pass that removed more than expected is visible rather than silent.
"""
import sys
from pathlib import Path

import bmesh
import bpy

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "models" / "source"
DEGENERATE_DISTANCE = 1e-6
WORLD_SLIVER = 1e-10


def counts(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    try:
        degenerate = sum(1 for f in bm.faces if f.calc_area() <= 1e-12)
        nonmanifold = sum(1 for e in bm.edges if len(e.link_faces) > 2)
        loose = sum(1 for v in bm.verts if not v.link_edges)
        return len(bm.verts), len(bm.faces), degenerate, nonmanifold, loose
    finally:
        bm.free()


def repair(model_id):
    path = SOURCE / f"{model_id}.blend"
    if not path.exists():
        raise SystemExit(f"No such model source: {path}")
    bpy.ops.wm.open_mainfile(filepath=str(path))
    # A file saved in Edit Mode reopens in Edit Mode, and every operator below needs Object Mode.
    if bpy.context.mode != 'OBJECT':
        try:
            bpy.ops.object.mode_set(mode='OBJECT')
        except RuntimeError:
            pass

    changed = 0
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        before = counts(obj.data)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        try:
            # Deliberately no remove_doubles. Merging coincident vertices looks like an obvious
            # cleanup and is destructive here twice over: the geode shell is authored as displaced
            # independent plates, so welding their touching boundaries turned 8 non-manifold edges
            # into 32, and every part carries duplicated vertices that exist to hold split normals,
            # so welding them silently changes the shading the artist saw.
            bmesh.ops.dissolve_degenerate(bm, dist=DEGENERATE_DISTANCE, edges=bm.edges[:])

            # Slivers that are only degenerate once transformed. verify-model-assets rejects a face
            # whose world-space edge cross-product falls below 1e-11, and the exporter emits world
            # space -- but bmesh measures area in local space, so an object with a small or squashed
            # transform can hide a sliver that the payload then fails on. The stone guardian's armour
            # chip had four, one of them exactly zero. Measured the way the gate measures it.
            world = obj.matrix_world
            flattened = []
            for face in bm.faces:
                if len(face.verts) < 3:
                    flattened.append(face)
                    continue
                a, b, c = (world @ v.co for v in face.verts[:3])
                if (b - a).cross(c - a).length <= WORLD_SLIVER:
                    flattened.append(face)
            if flattened:
                bmesh.ops.delete(bm, geom=flattened, context='FACES')

            # Stacked duplicate faces. The rebuilt geode shell had 35 welded edges carrying 4 or 8
            # faces, which is two or four copies of the same surface sitting on each other -- the
            # usual residue of duplicating a plate and not deleting the original. Dropping the later
            # copies changes nothing visible, because the survivor renders exactly the same surface,
            # and it is the only thing making those edges non-manifold. Keyed on rounded vertex
            # positions rather than indices so copies that were re-welded still match.
            seen, duplicates = set(), []
            for face in bm.faces:
                key = tuple(sorted(
                    (round(v.co.x, 5), round(v.co.y, 5), round(v.co.z, 5)) for v in face.verts))
                if key in seen:
                    duplicates.append(face)
                else:
                    seen.add(key)
            if duplicates:
                bmesh.ops.delete(bm, geom=duplicates, context='FACES')

            loose = [v for v in bm.verts if not v.link_edges]
            if loose:
                bmesh.ops.delete(bm, geom=loose, context='VERTS')
            bm.to_mesh(obj.data)
        finally:
            bm.free()
        obj.data.update()
        after = counts(obj.data)
        if before != after:
            changed += 1
            print(
                f"REPAIRED {model_id}/{obj.name}: "
                f"verts {before[0]}->{after[0]} faces {before[1]}->{after[1]} "
                f"degenerate {before[2]}->{after[2]} non-manifold {before[3]}->{after[3]} "
                f"loose {before[4]}->{after[4]}",
                flush=True)
        else:
            print(f"REPAIRED {model_id}/{obj.name}: already clean", flush=True)

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print(f"REPAIRED {model_id}: saved, {changed} mesh(es) changed", flush=True)


requested = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
if not requested:
    raise SystemExit("Name at least one model id, e.g. geode-sample")
for model in requested:
    repair(model)
