import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[1];out=root/'assets/models';preview=out/'previews';preview.mkdir(exist_ok=True)
models=json.loads((out/'catalog.json').read_text());cols=6;rows=4;per=cols*rows
pages=[int(a)-1 for a in sys.argv[sys.argv.index('--')+1:]] if '--' in sys.argv else range((len(models)+per-1)//per)
for page in pages:
 bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
 scene.render.resolution_x=2400;scene.render.resolution_y=1700;scene.render.resolution_percentage=100
 scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.18,.22,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
 for index,entry in enumerate(models[page*per:(page+1)*per]):
  id=entry['id'];file=out/entry['source']
  with bpy.data.libraries.load(str(file),link=False) as (src,dst):dst.objects=src.objects
  objects=[o for o in dst.objects if o and o.type=='MESH']
  rot=Matrix.Rotation(math.radians(-25),4,'Z')@Matrix.Rotation(math.radians(-57),4,'X')
  for obj in objects:scene.collection.objects.link(obj)
  # Library-loaded world matrices are stale until objects enter an evaluated scene.
  bpy.context.view_layer.update()
  transforms={obj:obj.matrix_world.copy() for obj in objects}
  for obj in objects:obj.matrix_world=rot@transforms[obj]
  bpy.context.view_layer.update();points=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
  minimum=Vector([min(p[i] for p in points) for i in range(3)]);maximum=Vector([max(p[i] for p in points) for i in range(3)]);center=(minimum+maximum)/2;scale=2.25/max((maximum-minimum).x,(maximum-minimum).y)
  x=(index%cols-(cols-1)/2)*2.9;y=(1.5-index//cols)*3.0+.25
  placement=Matrix.Translation(Vector((x,y,0)))@Matrix.Scale(scale,4)@Matrix.Translation(-center)
  for obj in objects:obj.matrix_world=placement@obj.matrix_world
  text=bpy.data.curves.new('label','FONT');text.body=id.replace('deep-fracture-','df-').replace('architecture-','arch-').replace('Magenheim_Staff_','staff-');text.align_x='CENTER';text.size=.095;text.extrude=0
  label=bpy.data.objects.new('label',text);scene.collection.objects.link(label);label.location=(x,y-1.37,1.7)
  mat=bpy.data.materials.get('Labels') or bpy.data.materials.new('Labels');mat.diffuse_color=(.85,.9,.95,1);label.data.materials.append(mat)
 camera_data=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',camera_data);scene.collection.objects.link(camera);camera.location=(0,0,30);camera_data.type='ORTHO';camera_data.ortho_scale=18;scene.camera=camera
 for name,pos,power,size in [('Key',(-5,-3,12),1800,12),('Fill',(6,4,8),1300,10)]:
  ld=bpy.data.lights.new(name,'AREA');lo=bpy.data.objects.new(name,ld);scene.collection.objects.link(lo);lo.location=pos;ld.energy=power;ld.shape='DISK';ld.size=size
 scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(preview/f'catalog-{page+1:02}.png')
 bpy.ops.render.render(write_still=True);print('RENDERED',page+1,flush=True)
