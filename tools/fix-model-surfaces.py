import bpy,bmesh
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'assets/models/source'
for file in sorted(root.glob('*.blend')):
 bpy.ops.wm.open_mainfile(filepath=str(file));bpy.context.preferences.filepaths.save_version=0
 for material in bpy.data.materials:material.use_backface_culling=True
 if file.stem=='geode-sample':
  obj=bpy.data.objects['stone-opening-rim'];bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();seen=set();duplicates=[]
  for face in bm.faces:
   key=tuple(sorted(v.index for v in face.verts))
   if key in seen:duplicates.append(face)
   else:seen.add(key)
  bmesh.ops.delete(bm,geom=duplicates,context='FACES_ONLY');bm.to_mesh(obj.data);bm.free();obj.data.materials[0].use_backface_culling=False
 bpy.ops.wm.save_as_mainfile(filepath=str(file),compress=True)
