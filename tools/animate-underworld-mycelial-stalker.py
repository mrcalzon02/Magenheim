#!/usr/bin/env python3
"""Author the Mycelial Stalker's production action set onto its generated source model."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-mycelial-stalker.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-mycelial-stalker.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-mycelial-stalker' or sc.get('magenheim_host_rig')!='HOST-QUADRUPED': raise RuntimeError('Wrong model/host identity')
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

def action(name,end,poses):
 old=bpy.data.actions.get(name)
 if old:bpy.data.actions.remove(old)
 act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
 for frame,pose in poses.items():
  for bname,rot in pose.items():
   pb=arm.pose.bones.get(bname)
   if not pb:raise RuntimeError(f'{name}: missing bone {bname}')
   pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bname)
 if not curves(act):raise RuntimeError(f'{name}: no readable animation curves')
 for fc in curves(act):
  for kp in fc.keyframe_points:kp.interpolation='BEZIER'
 act.frame_start=1;act.frame_end=end

def gait(a):
 return {'Front_L_Upper':(a,0,0),'Front_L_Lower':(-a*.55,0,0),'Front_L_Foot':(a*.25,0,0),'Rear_R_Upper':(a,0,0),'Rear_R_Lower':(-a*.55,0,0),'Rear_R_Foot':(a*.25,0,0),'Front_R_Upper':(-a,0,0),'Front_R_Lower':(a*.55,0,0),'Front_R_Foot':(-a*.25,0,0),'Rear_L_Upper':(-a,0,0),'Rear_L_Lower':(a*.55,0,0),'Rear_L_Foot':(-a*.25,0,0)}
neutral={b:(0,0,0) for b in ('Spine','Pelvis','Neck','Head','Jaw_L','Jaw_R')}
action('MycelialStalker_ConcealIdle',84,{1:neutral,24:{'Spine':(.035,0,0),'Neck':(-.05,0,0)},48:neutral,68:{'Head':(.03,0,.05),'Jaw_L':(0,0,.025),'Jaw_R':(0,0,-.025)},84:neutral})
action('MycelialStalker_Crouch',36,{1:neutral,16:{'Spine':(.16,0,0),'Pelvis':(.12,0,0),'Neck':(-.18,0,0),'Head':(-.08,0,0)},36:{'Spine':(.20,0,0),'Pelvis':(.16,0,0),'Neck':(-.22,0,0),'Head':(-.10,0,0)}})
action('MycelialStalker_Walk',32,{1:gait(.22),9:gait(-.22),17:gait(.22),25:gait(-.22),32:gait(.22)})
action('MycelialStalker_Pounce',34,{1:neutral,10:{'Spine':(.20,0,0),'Pelvis':(.18,0,0),'Neck':(-.22,0,0)},18:{'Spine':(-.30,0,0),'Neck':(.24,0,0),'Head':(.12,0,0),'Jaw_L':(0,0,.30),'Jaw_R':(0,0,-.30)},24:{'Spine':(-.10,0,0),'Jaw_L':(0,0,.12),'Jaw_R':(0,0,-.12)},34:neutral})
action('MycelialStalker_FailedPounceRetreat',42,{1:neutral,10:{'Head':(.12,0,.18),'Neck':(.12,0,.10)},22:{'Spine':(.18,0,0),'Pelvis':(-.16,0,0),**gait(-.20)},34:{'Spine':(.10,0,0),**gait(.18)},42:neutral})
action('MycelialStalker_Hit',18,{1:neutral,6:{'Spine':(.12,0,.16),'Head':(-.10,0,-.12)},18:neutral})
action('MycelialStalker_Stagger',32,{1:neutral,9:{'Spine':(.24,0,.22),'Pelvis':(-.12,0,-.12),'Head':(.16,0,-.20)},20:{'Spine':(-.10,0,-.10)},32:neutral})
action('MycelialStalker_Death',58,{1:neutral,18:{'Spine':(.20,0,.20),'Pelvis':(.16,0,-.12)},36:{'Spine':(.35,0,1.05),'Pelvis':(.20,0,.55),'Neck':(.30,0,.25),'Jaw_L':(.18,0,.24),'Jaw_R':(.18,0,-.24)},58:{'Spine':(.15,0,1.48),'Pelvis':(.12,0,.82),'Neck':(.42,0,.18),'Head':(.25,0,0)}})
arm.animation_data.action=None
sc['magenheim_fidelity']='production-creature-r2'
sc['magenheim_authored_actions']='MycelialStalker_ConcealIdle,MycelialStalker_Crouch,MycelialStalker_Walk,MycelialStalker_Pounce,MycelialStalker_FailedPounceRetreat,MycelialStalker_Hit,MycelialStalker_Stagger,MycelialStalker_Death'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Mycelial Stalker r2 animation set: 8 actions',flush=True)
