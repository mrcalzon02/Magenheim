"""Uniformly rescale the crystal weapon sources about the world origin.

  blender --background --python tools/rescale-weapon-models.py -- [factor] [model-id ...]

The weapon family shipped oversized: crystal-weapon-knife measured 1.25m on its longest
axis, longer than a vanilla sword, and crystal-weapon-sword measured 2.09m, longer than a
vanilla greatsword. The staff family is the in-game reference that reads correctly at
1.80-2.20m, and the sword was observed to be about a third longer than it needed to be,
which is where the default 0.75 factor comes from.

Weapons straddle the origin at the grip (sword Y spans -0.75..+1.34), so scaling about the
world origin keeps the grip at the hand and preserves the attach relationship. The scale is
baked into mesh data rather than left on the object so the .blend stays a clean editing
source. Shared mesh datablocks are scaled once.

This only rescales. Re-export afterwards with tools/export-model-assets.py, which is the
only thing that writes runtime model JSON and GLB.
"""
import bpy, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
DEFAULT_FACTOR = 0.75

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
factor = DEFAULT_FACTOR
if args:
    try:
        factor = float(args[0]); args = args[1:]
    except ValueError:
        pass
if not (0.05 <= factor <= 20.0):
    raise SystemExit(f'Refusing implausible scale factor {factor}.')

names = args or sorted(p.stem for p in SOURCE.glob('crystal-weapon-*.blend'))
if not names:
    raise SystemExit('No weapon sources matched.')

for name in names:
    path = SOURCE / f'{name}.blend'
    if path.resolve().parent != SOURCE.resolve():
        raise SystemExit(f'Invalid model path: {path}')
    if not path.is_file():
        raise SystemExit(f'Missing source: {path}')

    bpy.ops.wm.open_mainfile(filepath=str(path))
    scene = bpy.context.scene

    scaled_meshes = set()
    meshes = [o for o in scene.objects if o.type == 'MESH']
    if not meshes:
        raise SystemExit(f'{name}: source contains no mesh objects')

    for obj in meshes:
        # world = location + rotation @ scale @ vertex, so scaling both the location and the
        # local vertex coordinates by f is exactly a uniform scale about the world origin.
        obj.location = obj.location * factor
        data = obj.data
        if data.name in scaled_meshes:
            continue
        scaled_meshes.add(data.name)
        for vertex in data.vertices:
            vertex.co = vertex.co * factor

    for obj in scene.objects:
        if obj.type == 'LIGHT':
            obj.location = obj.location * factor

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print(f'RESCALED {name} by {factor} ({len(scaled_meshes)} mesh datablocks)', flush=True)

print(f'Rescaled {len(names)} weapon sources by {factor}. Re-export with export-model-assets.py.', flush=True)
