import bpy,math,json
from pathlib import Path
root=Path(__file__).resolve().parents[1];out=root/'assets/models/source'
def save(name):
 if (out/(name+".blend")).exists():raise RuntimeError("Refusing to overwrite an existing Blender source")
 bpy.context.scene['model_id']=name;bpy.context.scene['runtime_lights']='[]';bpy.context.preferences.filepaths.save_version=0
 for o in bpy.context.scene.objects:
  if o.type=='MESH':o['game_node_path']=o.name;o['game_collision']=False;o['game_crystal']='null'
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'.blend')),compress=True)
def material(name,color,metal=.0):
 m=bpy.data.materials.new(name);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=.3;return m
def mesh_object(name,v,f,m):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.data.materials.append(m);bpy.context.view_layer.objects.active=o;o.select_set(True)
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False);return o
bpy.ops.wm.read_factory_settings(use_empty=True)
# An actual hollow crown with modeled broken points and a thick inner rim.
v=[];n=48
for layer in range(4):
 for i in range(n):
  a=2*math.pi*i/n;rad=.5 if layer<2 else .39
  z=-.65 if layer%2==0 else (-.10+(1.10 if i%6==0 else .25 if i%6 in (1,5) else 0))
  if i in (12,30,31):z=min(z,.12)
  v.append((rad*math.cos(a),rad*math.sin(a),z))
f=[]
for i in range(n):
 j=(i+1)%n;f.extend([(i,j,n+j,n+i),(2*n+j,2*n+i,3*n+i,3*n+j),(n+i,n+j,3*n+j,3*n+i),(j,i,2*n+i,2*n+j)])
mesh_object('broken-crown',v,f,material('dark-forged-crown',(.16,.13,.10),.8));save('broken-crown')
bpy.ops.wm.read_factory_settings(use_empty=True)
v=[(0,0,.5),(.42,0,0),(0,-.42,0),(-.42,0,0),(0,.42,0),(0,0,-.5)]
f=[(0,1,2),(0,2,3),(0,3,4),(0,4,1),(5,2,1),(5,3,2),(5,4,3),(5,1,4)]
mesh_object('elemental-focus',v,f,material('elemental-focus',(1,1,1)));save('elemental-focus')
for file in sorted((root/'assets/earth').glob('*.obj')):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.wm.obj_import(filepath=str(file),forward_axis='NEGATIVE_Z',up_axis='Y')
 for im in bpy.data.images:
  if im.source=='FILE':im.pack()
 for obj in bpy.context.scene.objects:
  if obj.type=='MESH' and not obj.data.uv_layers:raise RuntimeError('Existing asset lacks UVs: '+str(file))
 save('earth-'+file.stem)
