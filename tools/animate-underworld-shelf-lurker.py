#!/usr/bin/env python3
"""Author Shelf Lurker production actions with six-point cling and readable drop ambush."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-shelf-lurker.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-shelf-lurker.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-shelf-lurker' or sc.get('magenheim_host_rig')!='HOST-WALL-CLINGER': raise RuntimeError('Wrong model/host identity')
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one armature, found {len(arms)}')
arm=arms[0]

LIMBS=('L_1','L_2','L_3','R_1','R_2','R_3')

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

def neutral():
 p={'Body':(0,0,0),'Abdomen':(0,0,0),'Head':(0,0,0)}
 for n in LIMBS:
  for j in ('Upper','Lower','Tarsus','Grip'):p[f'Leg_{n}_{j}']=(0,0,0)
 return p

def crawl(phase):
 # Alternating tripods carry the flattened body while the opposite grips release/replant.
 p={'Body':(0,0,phase*.035),'Abdomen':(0,0,-phase*.045),'Head':(0,0,phase*.055)}
 signs={'L_1':1,'L_2':-1,'L_3':1,'R_1':-1,'R_2':1,'R_3':-1}
 for n in LIMBS:
  a=phase*signs[n]
  p[f'Leg_{n}_Upper']=(a*.10,0,a*.16)
  p[f'Leg_{n}_Lower']=(-a*.18,a*.06,-a*.08)
  p[f'Leg_{n}_Tarsus']=(a*.12,-a*.08,a*.06)
  p[f'Leg_{n}_Grip']=(0,a*.12,-a*.10)
 return p

n=neutral()
action('ShelfLurker_ClingIdle',72,{1:n,24:{'Body':(.018,0,.018),'Abdomen':(-.014,0,-.020),'Head':(.025,0,.035),'Leg_L_2_Grip':(0,.035,0),'Leg_R_2_Grip':(0,-.035,0)},48:n,72:{'Head':(-.018,0,-.035),'Leg_L_1_Grip':(0,-.025,0),'Leg_R_3_Grip':(0,.025,0)}})
action('ShelfLurker_LateralCrawl',42,{1:crawl(1),8:crawl(-1),15:crawl(1),22:crawl(-1),29:crawl(1),36:crawl(-1),42:crawl(1)})
# Reposition deliberately peels one grip at a time; never rotates the entire mesh to fake wall locomotion.
poses={1:n}
order=('L_1','R_3','L_2','R_1','L_3','R_2')
for i,limb in enumerate(order):
 f=7+i*7; p={}
 p[f'Leg_{limb}_Upper']=(-.10 if limb.startswith('L') else .10,0,.18 if limb.startswith('L') else -.18)
 p[f'Leg_{limb}_Lower']=(.18,0,-.12); p[f'Leg_{limb}_Tarsus']=(-.16,.10,0); p[f'Leg_{limb}_Grip']=(0,.28,0)
 p['Body']=(0,0,.035*(-1 if i%2 else 1)); poses[f]=p
poses[52]=n
action('ShelfLurker_Reposition',52,poses)
# Drop begins flattened with grips tucked, then opens all six limbs and drives the downward mouth/head into the strike.
fold={**n,'Body':(-.10,0,0),'Head':(.12,0,0)}
for limb in LIMBS:
 fold[f'Leg_{limb}_Upper']=(.18,0,0); fold[f'Leg_{limb}_Lower']=(-.28,0,0); fold[f'Leg_{limb}_Tarsus']=(.22,0,0); fold[f'Leg_{limb}_Grip']=(0,.12,0)
spread={'Body':(.46,0,0),'Abdomen':(.24,0,0),'Head':(.62,0,0)}
for limb in LIMBS:
 side=-1 if limb.startswith('L') else 1
 spread[f'Leg_{limb}_Upper']=(-.34,0,side*.18); spread[f'Leg_{limb}_Lower']=(.42,0,-side*.12); spread[f'Leg_{limb}_Tarsus']=(-.30,0,side*.08); spread[f'Leg_{limb}_Grip']=(0,-.18,0)
action('ShelfLurker_DropPounce',32,{1:n,8:fold,17:spread,24:{'Body':(.62,0,0),'Head':(.78,0,0),'Abdomen':(.30,0,0),**{f'Leg_{x}_Upper':(-.42,0,0) for x in LIMBS}},32:spread})
action('ShelfLurker_Recover',38,{1:spread,12:{'Body':(.24,0,0),'Head':(.30,0,0),**{f'Leg_{x}_Lower':(.22,0,0) for x in LIMBS}},26:crawl(.55),38:n})
action('ShelfLurker_Attack',24,{1:n,7:{'Head':(-.18,0,0),'Body':(-.08,0,0)},13:{'Head':(.58,0,0),'Body':(.28,0,0),'Leg_L_1_Upper':(-.20,0,.12),'Leg_R_1_Upper':(-.20,0,-.12)},24:n})
action('ShelfLurker_Hit',18,{1:n,6:{'Body':(.12,0,.16),'Head':(-.16,0,-.18),'Abdomen':(-.10,0,.12)},18:n})
death={'Body':(.18,0,1.18),'Abdomen':(.12,0,1.32),'Head':(.32,0,.92)}
for limb in LIMBS:
 side=-1 if limb.startswith('L') else 1
 death[f'Leg_{limb}_Upper']=(.42,0,side*.34); death[f'Leg_{limb}_Lower']=(-.56,0,-side*.20); death[f'Leg_{limb}_Tarsus']=(.34,0,side*.12); death[f'Leg_{limb}_Grip']=(0,.30,0)
action('ShelfLurker_Death',58,{1:n,16:{'Body':(.10,0,.32),'Head':(.18,0,.24)},34:{'Body':(.16,0,.78),'Abdomen':(.10,0,.82),'Head':(.26,0,.60)},58:death})
arm.animation_data.action=None
sc['magenheim_fidelity']='production-creature-r2'
sc['magenheim_authored_actions']='ShelfLurker_ClingIdle,ShelfLurker_LateralCrawl,ShelfLurker_Reposition,ShelfLurker_DropPounce,ShelfLurker_Recover,ShelfLurker_Attack,ShelfLurker_Hit,ShelfLurker_Death'
sc['magenheim_cling_contract']='six-independent-grip-chains;sequential-release-replant;drop-pounce-without-whole-mesh-rotation'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Shelf Lurker r2: 8 actions with six-point cling/replant and articulated drop-pounce',flush=True)
