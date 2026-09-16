"""Export saved Blender assets; never executes the historical geometry generators.
blender --background --python tools/export-model-assets.py -- [model-id ...]
"""
import bpy,json,sys,hashlib
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
out=root/'assets/models'
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
files=[out/'source'/(x+'.blend') for x in args] if args else sorted((out/'source').glob('*.blend'))
for file in files:
 if file.resolve().parent != (out/'source').resolve():raise ValueError('Invalid model path')
 bpy.ops.wm.open_mainfile(filepath=str(file))
 scene=bpy.context.scene
 bindings={p['path']:p for p in json.loads(scene.get('binding_metadata','[]'))}
 if 'binding_metadata' in scene:del scene['binding_metadata']
 parts=[]
 for obj in scene.objects:
  if obj.type!='MESH':continue
  bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS');obj.select_set(False)
  path=obj.get('game_node_path',obj.name)
  previous=bindings.get(path,{})
  if 'game_collision' not in obj:obj['game_collision']=bool(previous.get('collider',False))
  if 'game_crystal' not in obj:obj['game_crystal']=json.dumps(previous.get('crystal'))
  evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
  if not mesh.uv_layers:raise ValueError(f'{file.name}/{obj.name}: UV map required')
  if len(mesh.materials)!=1:raise ValueError(f'{file.name}/{obj.name}: split objects by material before export')
  material=mesh.materials[0];bs=material.node_tree.nodes.get('Principled BSDF')
  if bs is None:raise ValueError('Principled material required: '+material.name)
  color=list(bs.inputs['Base Color'].default_value);color[3]=bs.inputs['Alpha'].default_value
  texture=None
  for node in material.node_tree.nodes:
   if node.type=='TEX_IMAGE' and node.image:
    image=node.image;texture=hashlib.sha256(image.name.encode()).hexdigest()[:16]+'.png'
    image.filepath_raw=str(out/'textures'/texture);image.file_format='PNG';image.save();
    if file.stem.startswith('earth-'):
     image.filepath_raw=str(root/'assets/earth'/(file.stem[6:]+'.png'));image.save()
    image.pack();break
  md=dict(doubleSided=not material.use_backface_culling,name=material.name,color=color,metallic=bs.inputs['Metallic'].default_value,roughness=bs.inputs['Roughness'].default_value,
          emission=[v*bs.inputs['Emission Strength'].default_value for v in bs.inputs['Emission Color'].default_value[:3]],texture=texture)
  vertices=[];normals=[];uv=[];triangles=[];normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
  for face in mesh.loop_triangles:
   for li in face.loops:
    loop=mesh.loops[li];v=obj.matrix_world@mesh.vertices[loop.vertex_index].co;n=(normal_matrix@face.normal).normalized()
    vertices.append([v.x,v.z,-v.y]);normals.append([n.x,n.z,-n.y]);uv.append(list(mesh.uv_layers.active.data[li].uv));triangles.append(len(vertices)-1)
  parts.append(dict(name=obj.name,path=path,vertices=vertices,normals=normals,uv=uv,triangles=triangles,material=md,collider=bool(obj['game_collision']),crystal=json.loads(obj['game_crystal'])))
 lights=json.loads(scene.get('runtime_lights','[]'))
 for obj in scene.objects:
  if obj.type=='LIGHT' and obj.data.type=='POINT':
   v=obj.matrix_world.translation;lights.append(dict(path=obj.name,position=[v.x,v.z,-v.y],color=list(obj.data.color),range=obj.data.cutoff_distance,intensity=obj.data.energy/100))
 payload=dict(parts=parts,lights=lights)
 if file.stem.startswith('earth-'):
  vs=[];uvs=[];indices=[]
  for part in parts:
   offset=len(vs);vs.extend(part['vertices']);uvs.extend(part['uv']);indices.extend(offset+i for i in part['triangles'])
  (root/'assets/earth'/(file.stem[6:]+'.mesh.json')).write_text(json.dumps(dict(vertices=vs,uv=uvs,triangles=indices),separators=(',',':')))

 if not parts:raise ValueError('Empty model: '+str(file))
 (out/'runtime'/(file.stem+'.model.json')).write_text(json.dumps(payload,separators=(',',':')))
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(file),compress=True)
 bpy.ops.export_scene.gltf(filepath=str(out/'glb'/(file.stem+'.glb')),export_format='GLB',export_extras=False,export_yup=True)
 print('EXPORTED',file.stem,flush=True)
