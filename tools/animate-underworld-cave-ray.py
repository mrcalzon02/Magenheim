#!/usr/bin/env python3
"""Author Cave Ray actions with progressive membrane deformation and independent tail motion."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-cave-ray.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-cave-ray.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='cave-ray' or arm.get('host_rig')!='HOST-AQUATIC-RAY': raise RuntimeError('Wrong Cave Ray model/host identity')

def action(name,end,poses):
 old=bpy.data.actions.get(name)
 if old:bpy.data.actions.remove(old)
 act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
 for frame,pose in poses.items():
  for bone,rot in pose.items():
   pb=arm.pose.bones.get(bone)
   if not pb: raise RuntimeError(f'{name}: missing bone {bone}')
   pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bone)
 act.frame_start=1; act.frame_end=end

def wings(l,r):
 # Increasing root->tip amplitude keeps the broad membrane alive rather than hinged like an aircraft wing.
 return {'Wing_L_1':(0,l*.34,l*.08),'Wing_L_2':(0,l*.58,l*.12),'Wing_L_3':(0,l*.82,l*.17),
         'Wing_R_1':(0,r*.34,-r*.08),'Wing_R_2':(0,r*.58,-r*.12),'Wing_R_3':(0,r*.82,-r*.17)}
def tail(a): return {f'Tail_{i}':(0,0,a*(.45+.11*i)*(-1 if i%2 else 1)) for i in range(1,6)}
N={}
action('CaveRay_Glide',72,{1:{**wings(.08,.08),**tail(.05)},24:{**wings(-.04,-.04),**tail(-.07)},48:{**wings(.06,.06),**tail(.06)},72:{**wings(.08,.08),**tail(.05)}})
action('CaveRay_FlapImpulse',34,{1:{**wings(.12,.12),**tail(.03)},9:wings(-.48,-.48),17:{**wings(.72,.72),**tail(.12)},25:wings(.18,.18),34:wings(.12,.12)})
action('CaveRay_BankLeft',42,{1:N,12:{**wings(-.18,.42),'Body':(.08,-.12,-.28),**tail(.10)},24:{**wings(-.34,.58),'Body':(.12,-.18,-.46),'Head':(.04,0,-.16),**tail(-.13)},42:N})
action('CaveRay_BankRight',42,{1:N,12:{**wings(.42,-.18),'Body':(.08,.12,.28),**tail(-.10)},24:{**wings(.58,-.34),'Body':(.12,.18,.46),'Head':(.04,0,.16),**tail(.13)},42:N})
action('CaveRay_Dive',40,{1:N,12:{**wings(.10,.10),'Body':(.20,0,0),'Head':(.28,0,0)},25:{**wings(-.12,-.12),'Body':(.38,0,0),'Head':(.34,0,0),**tail(.09)},40:N})
action('CaveRay_Rise',40,{1:N,12:{**wings(-.08,-.08),'Body':(-.16,0,0),'Head':(-.22,0,0)},25:{**wings(.24,.24),'Body':(-.32,0,0),'Head':(-.30,0,0),**tail(-.08)},40:N})
action('CaveRay_Flee',30,{1:{**wings(.12,.12),**tail(.10)},7:{**wings(-.55,-.55),**tail(-.20)},14:{**wings(.78,.78),**tail(.24)},21:{**wings(-.42,-.42),**tail(-.22)},30:{**wings(.55,.55),**tail(.18)}})
action('CaveRay_Hit',18,{1:N,6:{**wings(-.24,.31),'Body':(.10,0,.20),'Head':(-.12,0,-.12),**tail(.18)},18:N})
action('CaveRay_Death',72,{1:N,20:{**wings(-.20,-.16),'Body':(.18,0,.22),**tail(.15)},44:{**wings(-.52,-.46),'Body':(.46,0,.72),'Head':(.30,0,.24),**tail(-.25)},72:{**wings(-.70,-.64),'Body':(.72,0,1.18),'Head':(.48,0,.36),**tail(.30)}})
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'
arm['authored_actions']='CaveRay_Glide,CaveRay_FlapImpulse,CaveRay_BankLeft,CaveRay_BankRight,CaveRay_Dive,CaveRay_Rise,CaveRay_Flee,CaveRay_Hit,CaveRay_Death'
arm['animation_readability_contract']='progressive-root-mid-tip-membrane;asymmetric-banks;independent-tail-chain'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Cave Ray r2: 9 actions with progressive membrane deformation, asymmetric banks, independent tail',flush=True)
