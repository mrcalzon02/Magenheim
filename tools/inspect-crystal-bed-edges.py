import bpy
from pathlib import Path
from mathutils import Vector
root = Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root / 'assets/models/source/crystal-bed-earth.blend'))
for obj in bpy.data.objects:
    if obj.type != 'MESH': continue
    points = [obj.matrix_world @ Vector(v) for v in obj.bound_box]
    print('VERIFIED', obj.name, 'min', tuple(round(min(p[i] for p in points), 4) for i in range(3)),
          'max', tuple(round(max(p[i] for p in points), 4) for i in range(3)), flush=True)
