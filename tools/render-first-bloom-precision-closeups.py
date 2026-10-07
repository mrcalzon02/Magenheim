"""Identical close-up cameras for r3 and precision r4, textured and clay."""
import bpy,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-precision-r4'
for label,ID in [('before','underworld-boss-first-bloom-concept-r3'),('after','underworld-boss-first-bloom-precision-r4')]:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies'/(ID+'.blend')))
 scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data.action=None;arm.data.pose_position='REST'
 for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
 o=next(o for o in scene.objects if o.type=='MESH');mat=o.data.materials[0]
 points=[o.matrix_world@Vector(p) for p in o.bound_box];lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)]);center=(hi+lo)/2;size=4.65
 scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
 scene.render.resolution_x=scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
 scene.world=bpy.data.worlds.new('Matched precision world');scene.world.use_nodes=True
 scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.064,.073,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
 for name,direction,power in [('Key',(-1,1.4,2),120),('Fill',(1,1,.6),45),('Rim',(0,-1.8,1.3),140)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power*size*size;data.shape='DISK';data.size=size
  light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=center+Vector(direction)*size;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
 cd=bpy.data.cameras.new('Precision camera');cd.type='ORTHO';cam=bpy.data.objects.new('Precision camera',cd);scene.collection.objects.link(cam);scene.camera=cam
 clay=bpy.data.materials.new('Precision clay');clay.use_nodes=True;bs=clay.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.30,.32,.34,1);bs.inputs['Roughness'].default_value=.83
 scale=scene['refinement_head_focus'][2]/2.22
 for name,z,span in [('caps',2.93,size*.40),('tissue',2.16,size*.27)]:
  target=Vector((0,.19*scale,z*scale));cd.ortho_scale=span*1.18;cam.location=target+Vector((1,2,.35))*span*2;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
  for kind in ('textured','clay'):
   o.data.materials[0]=mat if kind=='textured' else clay
   scene.render.filepath=str(OUT/(label+'--'+name+'--'+kind+'.png'));bpy.ops.render.render(write_still=True)
   print('RENDERED precision closeup '+label+' '+name+' '+kind,flush=True)
