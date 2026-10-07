"""Adjust lights in existing authored chambers without rebuilding their geometry."""
from pathlib import Path
import bpy

root = Path(__file__).resolve().parents[1]
bpy.context.preferences.filepaths.save_version = 0
for index in range(1, 21):
    path = root / 'assets/models/source' / f'DF-{index:02}.blend'
    bpy.ops.wm.open_mainfile(filepath=str(path))
    for obj in bpy.context.scene.objects:
        if obj.type == 'LIGHT' and obj.name.startswith('FissureLight'):
            obj.data.energy = 80.0
            obj.data.cutoff_distance = 32.0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print('REPAIRED LIGHTING', path.stem, flush=True)
