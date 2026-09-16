import bpy,json,sys,math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sources=root/'dist/model-migration/procedural'
out=root/'assets/models'
for folder in ['source','glb','runtime','textures']:(out/folder).mkdir(parents=True,exist_ok=True)
import csv
ids={row['model_id'] for row in csv.DictReader((root/'dist/model-migration/MODEL_MANIFEST.tsv').open(),delimiter='\t')}
records=[]
for file in sorted(sources.glob('*.scene.json')):
 model=file.name.removesuffix('.scene.json')
 if model not in ids:continue
 data=json.loads(file.read_text())
 if (out/'source'/f'{model}.blend').exists():raise RuntimeError('Migration refuses to overwrite an editable asset; use export-model-assets.py')
 bpy.ops.wm.read_factory_settings(use_empty=True)
 materials={};images={}
 for part in data['parts']:
  md=part['material'];key=json.dumps(md,sort_keys=True)
  if key not in materials:
   m=bpy.data.materials.new(md['name'] or part['name']);m.use_nodes=True
   bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=md['color'];bs.inputs['Metallic'].default_value=md['metallic'];bs.inputs['Roughness'].default_value=max(.05,md['roughness']);bs.inputs['Alpha'].default_value=md['color'][3]
   bs.inputs['Emission Color'].default_value=(*md['emission'],1);bs.inputs['Emission Strength'].default_value=1
   if md['color'][3]<1:m.surface_render_method='DITHERED'
   tex=md['texture']
   if tex:
    if tex['name'] not in images:
     im=bpy.data.images.new(tex['name'],width=tex['width'],height=tex['height']);im.pixels.foreach_set(tex['pixels']);im.filepath_raw=str(out/'textures'/(tex['name']+'.png'));im.file_format='PNG';im.save();im.pack();images[tex['name']]=im
    n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=images[tex['name']]
    mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=md['color'];m.node_tree.links.new(n.outputs['Color'],mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
   materials[key]=m
  vertices=[(v[0],-v[2],v[1]) for v in part['vertices']];tri=part['triangles'];faces=[tri[i:i+3] for i in range(0,len(tri),3)]
  mesh=bpy.data.meshes.new(part['name']);mesh.from_pydata(vertices,[],faces);mesh.update();obj=bpy.data.objects.new(part['name'],mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(materials[key]);obj['game_node_path']=part['path']
  bpy.context.view_layer.objects.active=obj;obj.select_set(True)
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');
  if model!='geode-sample':bpy.ops.mesh.normals_make_consistent(inside=False)
  bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT');obj.select_set(False)
 # Preserve a directly inspectable copy of shader settings for deterministic game export.
 bpy.context.scene['model_id']=model;bpy.context.scene['binding_metadata']=json.dumps([{k:p.get(k) for k in ('name','path','collider','crystal','material')} for p in data['parts']]);bpy.context.scene['runtime_lights']=json.dumps(data.get('lights',[]))
 bpy.ops.wm.save_as_mainfile(filepath=str(out/'source'/f'{model}.blend'),compress=True)
 bpy.ops.export_scene.gltf(filepath=str(out/'glb'/f'{model}.glb'),export_format='GLB',export_extras=True,export_yup=True)
 # Game payload derives from the actual Blender mesh (including UVs and corrected normals).
 parts=[]
 for obj in bpy.context.scene.objects:
  if obj.type!='MESH':continue
  mesh=obj.data;mesh.calc_loop_triangles();vs=[];ns=[];uv=[];tri=[]
  for face in mesh.loop_triangles:
   for li in face.loops:
    loop=mesh.loops[li];v=mesh.vertices[loop.vertex_index].co;n=face.normal;vs.append([v.x,v.z,-v.y]);ns.append([n.x,n.z,-n.y]);uv.append(list(mesh.uv_layers.active.data[li].uv));tri.append(len(vs)-1)
  original=next(p for p in data['parts'] if p['path']==obj['game_node_path']);mat=dict(original['material']);tex=mat.pop('texture',None);mat['texture']=tex['name']+'.png' if tex else None
  parts.append(dict(name=obj.name,path=obj['game_node_path'],vertices=vs,normals=ns,uv=uv,triangles=tri,material=mat,collider=original.get('collider',False),crystal=original.get('crystal')))
 (out/'runtime'/f'{model}.model.json').write_text(json.dumps(dict(parts=parts,lights=data.get('lights',[])),separators=(',',':')))
 records.append(dict(id=model,parts=len(parts),triangles=sum(len(p['triangles'])//3 for p in parts)))
 print('MODEL_DONE',model,flush=True)
(out/'catalog.json').write_text(json.dumps(records,indent=2))
