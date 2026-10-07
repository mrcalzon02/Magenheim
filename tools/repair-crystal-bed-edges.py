"""Separate coincident bed coping and trim corner overlaps in existing sources."""
import bpy, json
from pathlib import Path
from mathutils import Vector
root = Path(__file__).resolve().parents[1]
catalog = json.loads((root / 'assets/models/catalog.json').read_text())
for entry in catalog:
    if not entry['id'].startswith('crystal-bed-'): continue
    source = root / 'assets/models' / entry['source']
    bpy.ops.wm.open_mainfile(filepath=str(source))
    for name in ('west-wall', 'east-wall', 'west-band', 'east-band', 'north-band', 'south-band'):
        obj = bpy.data.objects[name]
        inverse = obj.matrix_world.inverted()
        for vertex in obj.data.vertices:
            point = obj.matrix_world @ vertex.co
            if name.endswith('-band'):
                # Metal coping stands 3 cm proud of the stone on every face.
                point.z += .03 if obj.get('bed_edge_revision', 0) < 1 else 0
            if name in ('west-wall', 'east-wall'):
                point.y = max(-.87, min(.87, point.y))
            if name in ('west-band', 'east-band'):
                point.y = max(-.94, min(.94, point.y))
            vertex.co = inverse @ point
        obj['bed_edge_revision'] = 1
        obj.data.update()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    print('REPAIRED', entry['id'], 'stone corner joints and raised coping', flush=True)
