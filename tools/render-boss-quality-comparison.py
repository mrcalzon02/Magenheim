"""Identical framing, lights and exposure for native donors and owned bosses."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/detail-r2';OUT.mkdir(parents=True,exist_ok=True)
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['donors']
kind=args[0];records=[]
if kind=='donors':
 records=[dict(id='donor-'+r['donor'],source=r['source'],native=True) for r in json.loads((ROOT/'dist/boss-donor-quality/native-source/metrics.json').read_text())]
else:
 records=json.loads((OUT/'boss-construction-manifest.json').read_text())
 if len(args)>1:records=[r for r in records if any(n in r['id'] for n in args[1:])]
for row in records:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/row['source']))
 scene=bpy.context.scene
 for arm in [o for o in scene.objects if o.type=='ARMATURE']:
  arm.animation_data.action=None;arm.data.pose_position='REST'
  for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
 meshes=[o for o in scene.objects if o.type=='MESH']
 bpy.context.view_layer.update()
 points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 center=(lo+hi)/2;size=max(hi-lo)
 scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
 scene.render.resolution_x=scene.render.resolution_y=900;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
 scene.world=bpy.data.worlds.new('Matched comparison');scene.world.use_nodes=True
 scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.064,.073,1)
 scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
 for label,direction,power in [('Key',(-1,1.4,2),120),('Fill',(1,1,.6),45),('Rim',(0,-1.8,1.3),140)]:
  data=bpy.data.lights.new(label,'AREA');data.energy=power*size*size;data.shape='DISK';data.size=size
  o=bpy.data.objects.new(label,data);scene.collection.objects.link(o);o.location=center+Vector(direction)*size
  o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
 cd=bpy.data.cameras.new('Matched camera');cd.type='ORTHO';cd.ortho_scale=size*1.18
 cam=bpy.data.objects.new('Matched camera',cd);scene.collection.objects.link(cam);scene.camera=cam
 material_slots={o.name:list(o.data.materials) for o in meshes}
 clay=bpy.data.materials.new('Neutral anatomy clay');clay.use_nodes=True
 bs=clay.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.30,.32,.34,1);bs.inputs['Roughness'].default_value=.83
 views=[('three-quarter',(1,2,.55)),('front',(0,2,.06)),('side',(2,0,.06)),('detail',(1,2,.35)),('clay',(1,2,.55))]
 if not row.get('native'):views.append(('attack',(1,2,.55)))
 for view,direction in views:
  target=center;span=size
  if view=='detail':
   if row.get('native'):target=Vector(scene['native_head_focus']);span=size*.27
   else:
    heads=[o for o in meshes if o.name.startswith('Head__')]
    if heads:
     pts=[o.matrix_world@Vector(p) for o in heads for p in o.bound_box]
     target=Vector([(min(p[i] for p in pts)+max(p[i] for p in pts))*.5 for i in range(3)])
    else:target=center+Vector((0,0,size*.23))
    span=size*(.31 if 'blackwater-maw' in row['id'] else .43)
  if view=='clay':
   for o in meshes:
    o.data.materials.clear()
    for _ in material_slots[o.name]:o.data.materials.append(clay)
  else:
   for o in meshes:
    o.data.materials.clear()
    for mat in material_slots[o.name]:o.data.materials.append(mat)
  if view=='attack':
   arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.data.pose_position='POSE'
   names=str(scene['magenheim_authored_actions']).split(',');names=[n for n in names if any(t in n.lower() for t in ['slam','sweep','bite','cast','attack'])] or names
   action=bpy.data.actions[names[0]];arm.animation_data.action=action
   if action.slots:arm.animation_data.action_slot=action.slots[0]
   start,end=action.curve_frame_range;scene.frame_set(round(start+(end-start)*.4))
  cd.ortho_scale=span*1.18
  cam.location=target+Vector(direction)*span*2;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
  scene.render.filepath=str(OUT/(row['id']+'--'+view+'.png'));bpy.ops.render.render(write_still=True)
  print('RENDERED matched '+row['id']+' '+view,flush=True)
