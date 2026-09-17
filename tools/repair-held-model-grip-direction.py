#!/usr/bin/env python3
"""Reverse long held assets that are authored end-for-end, so the working end points out.

    tools/blender.ps1 repair-held-model-grip-direction [model-id ...]

`repair-held-model-orientation.py` fixes the sideways-axis defect and deliberately stops
there: it will not guess grip direction from bounds. This is the sibling pass for the other
half of the contract. Magenheim's canonical held sources put the grip/pommel at negative Y
in attach space and the blade, head or working end toward positive Y;
`verify-held-model-grip-direction.py` measures end-slice mass and rejects assets whose
negative end is materially heavier.

The asset is rotated 180 degrees about X, which reverses the long axis while leaving it the
long axis, and the rotation is baked into mesh data so the .blend stays a clean editing
source. The set of assets is explicit rather than discovered, because a near-symmetric asset
can trip the measurement and a silent automatic flip across the library would be worse than
the defect. The pass re-measures afterwards and refuses to save an asset it did not actually
improve.
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'

# Measured as reversed by verify-held-model-grip-direction.py, and corroborated by the live
# report that crystal weapons and staves orient wrongly in the player's hand.
DEFAULT_TARGETS = (
    'crystal-weapon-sword',
    'Magenheim_Staff_Fire_Crystal',
    'Magenheim_Staff_Storm_Crystal',
    'staff-radiance-crystal',
    'staff-venom-crystal',
)

SLICE = 0.22          # fraction of the long axis counted as each end
REVERSE_RATIO = 1.30  # both mirror verify-held-model-grip-direction.py


def end_mass(objects):
    """Vertex counts in the end slices of the long (Blender Z) axis.

    Measured on modifier-evaluated meshes, because that is what export-model-assets.py
    writes and therefore what the verifier sees. These assets carry Bevel modifiers, so the
    base mesh is an order of magnitude smaller and can point the other way.
    """
    depsgraph = bpy.context.evaluated_depsgraph_get()
    zs = []
    for obj in objects:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        zs.extend((obj.matrix_world @ v.co).z for v in mesh.vertices)
        evaluated.to_mesh_clear()
    low, high = min(zs), max(zs)
    span = high - low
    if span <= 0:
        raise SystemExit('degenerate long axis')
    negative = sum(1 for z in zs if z <= low + span * SLICE)
    positive = sum(1 for z in zs if z >= high - span * SLICE)
    return negative, positive


def process(name):
    path = SOURCE / f'{name}.blend'
    if path.resolve().parent != SOURCE.resolve() or not path.is_file():
        raise SystemExit(f'Invalid or missing source: {path}')

    bpy.ops.wm.open_mainfile(filepath=str(path))
    scene = bpy.context.scene
    objects = [o for o in scene.objects if o.type == 'MESH']
    if not objects:
        raise SystemExit(f'{name}: no mesh objects')

    before = end_mass(objects)
    if before[1] >= before[0]:
        print(f'SKIPPED {name}: already working-end-out ({before[0]}/{before[1]})', flush=True)
        return

    # 180 degrees about X maps (y, z) -> (-y, -z): the long axis reverses and stays the long
    # axis. Baked into mesh data, and object locations rotated with it, so the whole authored
    # hierarchy moves together about the attach origin.
    rotation = Matrix.Rotation(math.radians(180), 4, 'X')
    done = set()
    for obj in objects:
        obj.location = rotation @ obj.location
        data = obj.data
        if data.name in done:
            continue
        done.add(data.name)
        for vertex in data.vertices:
            vertex.co = rotation @ vertex.co
        data.update()
    for obj in scene.objects:
        if obj.type == 'LIGHT':
            obj.location = rotation @ obj.location
    bpy.context.view_layer.update()

    after = end_mass(objects)
    if after[1] < after[0]:
        raise SystemExit(
            f'{name}: still reversed after the flip ({after[0]}/{after[1]}); refusing to save')

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print(f'REPAIRED {name}: {before[0]}/{before[1]} -> {after[0]}/{after[1]}', flush=True)


targets = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else list(DEFAULT_TARGETS)
for target in targets:
    process(target)
print(f'Grip-direction pass complete over {len(targets)} asset(s).', flush=True)
