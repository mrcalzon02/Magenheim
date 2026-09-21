#!/usr/bin/env python3
"""Author Puffback production actions with readable inflation and territorial escalation."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-puffback.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-puffback.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-puffback' or sc.get('magenheim_host_rig')!='HOST-QUADRUPED': raise RuntimeError('Wrong model/host identity')
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one armature, found {len(arms)}')
arm=arms[0]

def curves(act):
 legacy=getattr(act,'fcurves',None)
 if legacy is not None:return list(legacy)
 out=[]
 for layer in getattr(act,'layers',()):
  for strip in getattr(layer,'strips',()):
   for bag in getattr(strip,'channelbags',()):out.extend(bag.fcurves)
 return out

def action(name,end,poses,scales=None):
 old=bpy.data.actions.get(name)
 if old:bpy.data.actions.remove(old)
 act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
 for frame,pose in poses.items():
  for bname,rot in pose.items():
   pb=arm.pose.bones.get(bname)
   if not pb:raise RuntimeError(f'{name}: missing bone {bname}')
   pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bname)
 for frame,pose in (scales or {}).items():
  for bname,scale in pose.items():
   pb=arm.pose.bones.get(bname)
   if not pb:raise RuntimeError(f'{name}: missing scale bone {bname}')
   pb.scale=scale; pb.keyframe_insert('scale',frame=frame,group=bname)
 if not curves(act):raise RuntimeError(f'{name}: no readable animation curves')
 for fc in curves(act):
  for kp in fc.keyframe_points:kp.interpolation='BEZIER'
 act.frame_start=1;act.frame_end=end

def gait(a):
 return {'Front_L_Upper':(a,0,0),'Front_L_Lower':(-a*.45,0,0),'Front_L_Foot':(a*.18,0,0),'Rear_R_Upper':(a*.82,0,0),'Rear_R_Lower':(-a*.38,0,0),'Rear_R_Foot':(a*.15,0,0),'Front_R_Upper':(-a,0,0),'Front_R_Lower':(a*.45,0,0),'Front_R_Foot':(-a*.18,0,0),'Rear_L_Upper':(-a*.82,0,0),'Rear_L_Lower':(a*.38,0,0),'Rear_L_Foot':(-a*.15,0,0)}
neutral={b:(0,0,0) for b in ('Spine_1','Spine_2','Pelvis','Neck','Head','Bladder_Main')}
action('Puffback_GrazeRoot',78,{1:neutral,18:{'Neck':(.34,0,0),'Head':(.28,0,0)},38:{'Neck':(.42,0,.08),'Head':(.34,0,-.10)},58:{'Neck':(.38,0,-.08),'Head':(.30,0,.10)},78:neutral})
action('Puffback_Idle',72,{1:neutral,24:{'Spine_2':(.025,0,0),'Bladder_Main':(-.025,0,0)},48:neutral,72:{'Head':(0,0,.06)}})
action('Puffback_Walk',36,{1:gait(.16),10:gait(-.16),19:gait(.16),28:gait(-.16),36:gait(.16)})
action('Puffback_WarningDisplay',48,{1:neutral,14:{'Spine_2':(-.10,0,0),'Neck':(-.16,0,0),'Head':(-.10,0,0),'Bladder_Main':(-.10,0,0)},30:{'Spine_2':(-.15,0,0),'Neck':(-.20,0,0),'Head':(-.14,0,0),'Bladder_Main':(-.16,0,0)},48:neutral},{1:{'Bladder_Main':(1,1,1)},14:{'Bladder_Main':(1.08,1.08,1.12)},30:{'Bladder_Main':(1.16,1.16,1.22)},48:{'Bladder_Main':(1,1,1)}})
action('Puffback_Charge',30,{1:{**neutral,**gait(.12)},8:{'Spine_2':(-.14,0,0),'Neck':(.16,0,0),'Head':(.10,0,0),**gait(-.28)},16:{'Spine_2':(-.18,0,0),'Neck':(.20,0,0),**gait(.30)},24:{'Spine_2':(-.16,0,0),**gait(-.30)},30:{**neutral,**gait(.12)}})
action('Puffback_DefensiveInflate',44,{1:neutral,12:{'Spine_1':(.06,0,0),'Spine_2':(-.06,0,0),'Bladder_Main':(.08,0,0)},28:{'Spine_1':(.12,0,0),'Spine_2':(-.12,0,0),'Bladder_Main':(.14,0,0)},44:{'Spine_1':(.10,0,0),'Spine_2':(-.10,0,0),'Bladder_Main':(.12,0,0)}},{1:{'Bladder_Main':(.88,.88,.84)},12:{'Bladder_Main':(1.02,1.02,1.06)},28:{'Bladder_Main':(1.28,1.28,1.38)},44:{'Bladder_Main':(1.34,1.34,1.46)}})
action('Puffback_SporePuff',34,{1:{'Spine_2':(-.10,0,0),'Bladder_Main':(.12,0,0)},10:{'Spine_2':(-.15,0,0),'Bladder_Main':(.18,0,0)},16:{'Spine_2':(.08,0,0),'Bladder_Main':(-.12,0,0)},24:{'Spine_2':(.02,0,0)},34:neutral},{1:{'Bladder_Main':(1.34,1.34,1.46)},10:{'Bladder_Main':(1.42,1.42,1.54)},16:{'Bladder_Main':(.94,.94,.88)},24:{'Bladder_Main':(1.04,1.04,1.06)},34:{'Bladder_Main':(1,1,1)}})
action('Puffback_Hit',18,{1:neutral,6:{'Spine_2':(.10,0,.14),'Head':(-.08,0,-.10)},18:neutral})
action('Puffback_Stagger',32,{1:neutral,9:{'Spine_1':(.18,0,.16),'Spine_2':(.22,0,.18),'Pelvis':(-.10,0,-.10),'Head':(.14,0,-.16)},20:{'Spine_2':(-.08,0,-.08)},32:neutral})
action('Puffback_Death',62,{1:neutral,18:{'Spine_1':(.14,0,.18),'Spine_2':(.20,0,.20)},38:{'Spine_1':(.28,0,.72),'Spine_2':(.34,0,.92),'Pelvis':(.16,0,.48),'Neck':(.30,0,.18)},62:{'Spine_1':(.16,0,1.20),'Spine_2':(.18,0,1.38),'Pelvis':(.12,0,.70),'Neck':(.36,0,.12),'Head':(.22,0,0)}},{1:{'Bladder_Main':(1,1,1)},38:{'Bladder_Main':(.92,.92,.86)},62:{'Bladder_Main':(.76,.76,.68)}})
arm.animation_data.action=None
sc['magenheim_fidelity']='production-creature-r2'
sc['magenheim_authored_actions']='Puffback_GrazeRoot,Puffback_Idle,Puffback_Walk,Puffback_WarningDisplay,Puffback_Charge,Puffback_DefensiveInflate,Puffback_SporePuff,Puffback_Hit,Puffback_Stagger,Puffback_Death'
sc['magenheim_inflation_tell']='compress->inflate-before-release;Bladder_Main scale keyed independently'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Puffback r2 animation set: 10 actions with pre-release bladder inflation tell',flush=True)
