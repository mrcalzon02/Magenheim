"""Export saved Blender assets; never executes the historical geometry generators.
blender --background --python tools/export-model-assets.py -- [model-id ...]
"""
import bpy,json,sys,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from model_surface_authoring import bind_surface_if_needed, bind_object_texture
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
out=root/'assets/models'
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
files=[out/'source'/(x+'.blend') for x in args] if args else sorted((out/'source').glob('*.blend'))
override_path=out/'texture-overrides.json'
texture_overrides=json.loads(override_path.read_text()) if override_path.is_file() else {}
def runtime_texture_from_file(source):
 data=source.read_bytes()
 if data[:8]!=b'\x89PNG\r\n\x1a\n':raise ValueError('Runtime PBR source is not PNG: '+str(source))
 name=hashlib.sha256(data).hexdigest()[:16]+'.png'
 target=out/'textures'/name
 if not target.exists():target.write_bytes(data)
 return name
for file in files:
 if file.resolve().parent != (out/'source').resolve():raise ValueError('Invalid model path')
 bpy.ops.wm.open_mainfile(filepath=str(file))
 # Blender restores the mode a file was saved in, and a hand-edited source is very often saved in
 # Edit Mode. origin_set below then fails with "Operation cannot be performed in edit mode" and the
 # whole export dies, which is how a manual geode edit went unexported. Editing a source by hand is
 # normal; refusing to export it is not.
 if bpy.context.mode!='OBJECT':
  try:bpy.ops.object.mode_set(mode='OBJECT')
  except RuntimeError:pass
 scene=bpy.context.scene
 skin_contract=None
 if 'magenheim_skinning' in scene:
  if scene['magenheim_skinning']!='valheim-player-attach-skin':
   raise ValueError(file.name+': unsupported runtime skinning contract '+repr(scene['magenheim_skinning']))
  bone_names=json.loads(scene.get('magenheim_bone_order','[]'))
  if not bone_names or len(bone_names)!=len(set(bone_names)) or 'Hips' not in bone_names:
   raise ValueError(file.name+': invalid canonical skin bone order')
  skin_contract=dict(kind='valheim-player-attach-skin',bones=bone_names,root='Hips')
  skin_bone_index={name:i for i,name in enumerate(bone_names)}
 bindings={p['path']:p for p in json.loads(scene.get('binding_metadata','[]'))}
 if 'binding_metadata' in scene:del scene['binding_metadata']
 parts=[]
 for obj in scene.objects:
  if obj.type!='MESH':continue
  bpy.context.view_layer.objects.active=obj;obj.select_set(True)
  if skin_contract is None:bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
  obj.select_set(False)
  path=obj.get('game_node_path',obj.name)
  previous=bindings.get(path,{})
  if 'game_collision' not in obj:obj['game_collision']=bool(previous.get('collider',False))
  if 'game_crystal' not in obj:obj['game_crystal']=json.dumps(previous.get('crystal'))
  texture=texture_overrides.get(file.stem,{}).get(obj.name)
  if texture is not None:bind_object_texture(bpy,obj,texture)
  evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
  if not mesh.uv_layers:raise ValueError(f'{file.name}/{obj.name}: UV map required')
  if len(mesh.materials)!=1:raise ValueError(f'{file.name}/{obj.name}: split objects by material before export')
  material=mesh.materials[0];bs=material.node_tree.nodes.get('Principled BSDF')
  if bs is None:raise ValueError('Principled material required: '+material.name)
  # Preserve real authored art. Only materials that are blank or still carry one of the obsolete
  # magenheim.surface.* runtime bakes are rebound to the semantic authored surface library.
  # Because this exporter saves the opened .blend at the end, exporting is also the source migration.
  bind_surface_if_needed(bpy,material)
  color=list(bs.inputs['Base Color'].default_value);color[3]=bs.inputs['Alpha'].default_value
  texture=texture_overrides.get(file.stem,{}).get(obj.name)
  if texture is not None and not (out/'textures'/texture).is_file():raise ValueError(f'{file.name}/{obj.name}: missing texture override {texture}')
  for node in material.node_tree.nodes:
   if texture is not None:break
   if node.type=='TEX_IMAGE' and node.image:
    image=node.image
    # bind_surface_if_needed must have migrated this before export. Keep a hard guard so a future
    # refactor cannot silently turn the old misclassified runtime bakes back into authored art.
    if image.name.startswith('magenheim.surface.'):
     raise ValueError(f'{file.name}/{material.name}: obsolete generated surface was not rebound')
    if min(image.size)<256:raise ValueError(f'{file.name}/{image.name}: packed source texture must be at least 256px')
    # Name the texture by its CONTENT, not by the Blender image's name. Hashing the name meant every
    # image called the same thing across different .blend files wrote to the same PNG and each export
    # overwrote the last: 281 models and 3,680 parts collapsed onto 45 texture files, one of them
    # shared by 125 models. In Blender each model showed its own image; in game they all sampled
    # whichever model exported last, which is why buildables looked perfect in the source and wrong
    # in the world. Hashing content still deduplicates genuinely identical images, which is the only
    # sharing that was ever intended.
    staging=out/'textures'/('.staging-'+file.stem+'-'+str(len(parts))+'.png')
    image.filepath_raw=str(staging);image.file_format='PNG';image.save()
    texture=hashlib.sha256(staging.read_bytes()).hexdigest()[:16]+'.png'
    final=out/'textures'/texture
    if final.exists():staging.unlink()
    else:staging.replace(final)
    image.filepath_raw=str(final);
    if file.stem.startswith('earth-'):
     image.filepath_raw=str(root/'assets/earth'/(file.stem[6:]+'.png'));image.save()
    image.pack();break
  pbr_key=material.get('magenheim_material_source_key')
  normal_texture=metallic_gloss_texture=emission_texture=None
  emission_value=[v*bs.inputs['Emission Strength'].default_value for v in bs.inputs['Emission Color'].default_value[:3]]
  if pbr_key:
   pbr_dir=root/'assets'/'material-source'/'underworld'
   normal_texture=runtime_texture_from_file(pbr_dir/(pbr_key+'-normal.png'))
   metallic_gloss_texture=runtime_texture_from_file(pbr_dir/(pbr_key+'-metallic-smoothness.png'))
   emission_file=pbr_dir/(pbr_key+'-emission.png')
   if emission_file.is_file():
    emission_texture=runtime_texture_from_file(emission_file)
    emission_value=[bs.inputs['Emission Strength'].default_value]*3
  md=dict(doubleSided=not material.use_backface_culling,name=material.get('magenheim_material_name',material.name),color=color,metallic=bs.inputs['Metallic'].default_value,roughness=bs.inputs['Roughness'].default_value,
          emission=emission_value,texture=texture,normalTexture=normal_texture,normalScale=.42 if pbr_key else None,
          metallicGlossTexture=metallic_gloss_texture,emissionTexture=emission_texture)
  vertices=[];normals=[];uv=[];triangles=[];skin_weights=[] if skin_contract else None
  normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
  group_names={group.index:group.name for group in obj.vertex_groups}
  for face in mesh.loop_triangles:
   # Drop triangles with no world-space area. verify-model-assets rejects these, and they render
   # nothing, but they cannot always be removed at the source: the exporter emits the *evaluated*
   # mesh, so a sliver produced by a modifier is not present in the .blend to delete. The stone
   # guardian's armour chip carried four, one of them exactly zero. Dropping them here removes
   # nothing visible and leaves the gate to catch anything that still gets through.
   fa,fb,fc=(obj.matrix_world@mesh.vertices[mesh.loops[li].vertex_index].co for li in face.loops)
   if (fb-fa).cross(fc-fa).length<=1e-11:continue
   # Preserve authored split/smooth normals; mirrored transforms need reversed
   # winding so exported fronts agree with the inverse-transpose normals.
   loops=list(face.loops)
   if obj.matrix_world.to_3x3().determinant()<0:loops.reverse()
   for li in loops:
    loop=mesh.loops[li];source_vertex=mesh.vertices[loop.vertex_index]
    v=obj.matrix_world@source_vertex.co;n=(normal_matrix@mesh.corner_normals[li].vector).normalized()
    vertices.append([v.x,v.z,-v.y]);normals.append([n.x,n.z,-n.y]);uv.append(list(mesh.uv_layers.active.data[li].uv));triangles.append(len(vertices)-1)
    if skin_contract:
     influences=[]
     for element in source_vertex.groups:
      if element.weight<=1e-7:continue
      bone=group_names.get(element.group)
      if bone not in skin_bone_index:raise ValueError(f'{file.name}/{obj.name}: non-canonical weighted bone {bone!r}')
      influences.append((skin_bone_index[bone],float(element.weight)))
     influences=sorted(influences,key=lambda value:value[1],reverse=True)[:4]
     total=sum(weight for _,weight in influences)
     if total<=1e-7:raise ValueError(f'{file.name}/{obj.name}: skin vertex has no canonical bone weight')
     skin_weights.append([[index,weight/total] for index,weight in influences])
  part=dict(name=obj.name,path=path,vertices=vertices,normals=normals,uv=uv,triangles=triangles,material=md,collider=bool(obj['game_collision']),crystal=json.loads(obj['game_crystal']))
  if skin_contract:part['skinWeights']=skin_weights
  parts.append(part)
  evaluated.to_mesh_clear()
 lights=json.loads(scene.get('runtime_lights','[]'))
 for obj in scene.objects:
  if obj.type=='LIGHT' and obj.data.type=='POINT':
   v=obj.matrix_world.translation;lights.append(dict(path=obj.name,position=[v.x,v.z,-v.y],color=list(obj.data.color),range=obj.data.cutoff_distance,intensity=obj.data.energy/100))
 payload=dict(parts=parts,lights=lights)
 # A bone-bound model carries the canonical skeleton its parts were authored against; only models
 # that declare one change, so every other payload stays byte-identical.
 if 'runtime_rig' in scene:payload['rig']=json.loads(scene['runtime_rig'])
 if skin_contract:payload['skinRig']=skin_contract
 if file.stem.startswith('earth-'):
  vs=[];uvs=[];indices=[]
  for part in parts:
   offset=len(vs);vs.extend(part['vertices']);uvs.extend(part['uv']);indices.extend(offset+i for i in part['triangles'])
  (root/'assets/earth'/(file.stem[6:]+'.mesh.json')).write_text(json.dumps(dict(vertices=vs,uv=uvs,triangles=indices),separators=(',',':')))
  # verify-earth-assets requires an .obj/.mtl pair beside every earth asset. They used to come
  # from generate-earth-assets.py, which the Blender-authored tiers no longer go through, so a
  # model authored here would ship without them -- and a hand-kept pair would silently drift from
  # the mesh it claims to describe. Same format, same source, one tool.
  name=file.stem[6:]
  obj=['# Original Magenheim Earth asset; meters; Y up','mtllib '+name+'.mtl','usemtl '+name]
  obj+=['v '+' '.join('%.6f'%x for x in p) for p in vs]+['vt '+' '.join('%.6f'%x for x in p) for p in uvs]
  obj+=['f '+' '.join('%d/%d'%(indices[i+j]+1,indices[i+j]+1) for j in range(3)) for i in range(0,len(indices),3)]
  nl=chr(10)
  (root/'assets/earth'/(name+'.obj')).write_text(nl.join(obj)+nl)
  (root/'assets/earth'/(name+'.mtl')).write_text(nl.join(['newmtl '+name,'Kd 1 1 1','map_Kd '+name+'.png','']))

 if not parts:raise ValueError('Empty model: '+str(file))
 (out/'runtime'/(file.stem+'.model.json')).write_text(json.dumps(payload,separators=(',',':')))
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(file),compress=True)
 bpy.ops.export_scene.gltf(filepath=str(out/'glb'/(file.stem+'.glb')),export_format='GLB',export_extras=False,export_yup=True)
 print('EXPORTED',file.stem,flush=True)
