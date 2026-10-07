"""Fixed-light front/side/three-quarter and inherited attack pose review."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04';OUT.mkdir(parents=True,exist_ok=True)
records=json.loads((OUT/'boss-construction-manifest.json').read_text())
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for row in records:
 name=row['id'].removeprefix('underworld-boss-')
 if args and name not in args:continue
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/row['source']))
 scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE')
 arm.animation_data.action=None;arm.data.pose_position='REST'
 for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update()
 meshes=[o for o in scene.objects if o.type=='MESH']
 points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 center=(lo+hi)/2;size=max(hi-lo)
 scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
 scene.render.resolution_x=scene.render.resolution_y=640;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
 scene.world=bpy.data.worlds.new('Concept review world');scene.world.use_nodes=True
 scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.064,.073,1)
 scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
 for label,direction,power in [('Key',(-1,1.4,2),120),('Fill',(1,1,.6),45),('Rim',(0,-1.8,1.3),140)]:
  data=bpy.data.lights.new(label,'AREA');data.energy=power*size*size;data.shape='DISK';data.size=size
  obj=bpy.data.objects.new(label,data);scene.collection.objects.link(obj);obj.location=center+Vector(direction)*size
  obj.rotation_euler=(center-obj.location).to_track_quat('-Z','Y').to_euler()
 camdata=bpy.data.cameras.new('Construction camera');camdata.type='ORTHO';camdata.ortho_scale=size*1.18
 camera=bpy.data.objects.new('Construction camera',camdata);scene.collection.objects.link(camera);scene.camera=camera
 for view,direction in [('three-quarter',(1,2,.55)),('front',(0,2,.06)),('side',(2,0,.06)),('attack',(1,2,.55))]:
  if view=='attack':
   declared=str(scene['magenheim_authored_actions']).split(',')
   candidates=[a for a in declared if any(k in a.lower() for k in ('slam','sweep','bite','cast','attack','pulse'))]
   action=bpy.data.actions.get((candidates or declared)[0]);arm.data.pose_position='POSE';arm.animation_data.action=action
   if action.slots:arm.animation_data.action_slot=action.slots[0]
   # Frame metadata in legacy source actions is often 0..0 despite keyed motion.
   start,end=action.curve_frame_range
   scene.frame_set(round(start+(end-start)*.40))
   row['pose_preview_action']=action.name
  camera.location=center+Vector(direction)*size*2
  camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
  scene.render.filepath=str(OUT/(name+'--'+view+'.png'));bpy.ops.render.render(write_still=True)
  print('RENDERED '+name+' '+view,flush=True)
 row['dimensions_metres']=list(hi-lo)
 row['render_status']='offline rest and inherited attack pose rendered; native acceptance pending'
(OUT/'boss-construction-manifest.json').write_text(json.dumps(records,indent=2)+'\n')
