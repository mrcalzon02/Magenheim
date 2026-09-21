#!/usr/bin/env python3
"""Author Crowncap Brute production actions with asymmetric sweeps, weighted slam, and gill-exposing stagger."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-crowncap-brute.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-crowncap-brute.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
sc=bpy.context.scene
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one Crowncap Brute armature, found {len(arms)}')
arm=arms[0]
if arm.get('magenheim_asset')!='crowncap-brute' or arm.get('host_rig')!='HOST-BIPED-MASS': raise RuntimeError('Wrong Crowncap Brute model/host identity')

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
   if not pb: raise RuntimeError(f'{name}: missing bone {bname}')
   pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bname)
 if not curves(act): raise RuntimeError(f'{name}: no readable animation curves')
 for fc in curves(act):
  for kp in fc.keyframe_points: kp.interpolation='BEZIER'
 act.frame_start=1; act.frame_end=end

def neutral():
 return {n:(0,0,0) for n in ('Pelvis','Spine1','Chest','Neck','Head','Jaw','UpperArm_L','ForeArm_L','Hand_L','UpperArm_R','ForeArm_R','Hand_R','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R')}

n=neutral()
action('CrowncapBrute_Idle',72,{1:n,24:{'Chest':(.018,0,.015),'Head':(-.012,0,-.018),'UpperArm_L':(.012,0,.018),'UpperArm_R':(.012,0,-.018)},48:n,72:{'Chest':(-.014,0,-.012),'Head':(.015,0,.015)}})
# Slow planted gait: pelvis/chest counter-rotate while huge arms lag behind.
def walk(phase):
 return {'Pelvis':(0,0,.07*phase),'Spine1':(0,0,-.045*phase),'Chest':(.018,0,-.06*phase),'Thigh_L':(.24*phase,0,0),'Shin_L':(-.18*phase,0,0),'Foot_L':(.08*phase,0,0),'Thigh_R':(-.24*phase,0,0),'Shin_R':(.18*phase,0,0),'Foot_R':(-.08*phase,0,0),'UpperArm_L':(-.10*phase,0,.025),'ForeArm_L':(.05*phase,0,0),'UpperArm_R':(.10*phase,0,-.025),'ForeArm_R':(-.05*phase,0,0)}
action('CrowncapBrute_Walk',48,{1:walk(1),13:walk(-1),25:walk(1),37:walk(-1),48:walk(1)})
action('CrowncapBrute_Turn',34,{1:n,12:{'Pelvis':(0,0,-.18),'Spine1':(0,0,.26),'Chest':(0,0,.34),'Head':(0,0,.22),'Thigh_L':(.08,0,-.10),'Thigh_R':(-.08,0,-.10)},24:{'Pelvis':(0,0,.16),'Chest':(0,0,-.24),'Head':(0,0,-.18)},34:n})
action('CrowncapBrute_Alert',40,{1:n,12:{'Chest':(-.08,0,0),'Head':(.10,0,0),'UpperArm_L':(-.10,0,.12),'UpperArm_R':(-.10,0,-.12)},24:{'Chest':(.06,0,0),'Head':(-.06,0,0),'Jaw':(.12,0,0),'UpperArm_L':(-.18,0,.18),'UpperArm_R':(-.18,0,-.18)},40:n})
# Sweeps are intentionally asymmetric: attacking shoulder leads, opposite leg braces, torso follows through.
def sweep(side):
 s=-1 if side=='L' else 1; atk=f'UpperArm_{side}'; fore=f'ForeArm_{side}'; hand=f'Hand_{side}'; brace='R' if side=='L' else 'L'
 wind={'Pelvis':(0,0,-.20*s),'Spine1':(0,0,-.28*s),'Chest':(-.10,0,-.38*s),atk:(-.28,0,-.48*s),fore:(-.34,0,-.18*s),hand:(0,0,-.10*s),f'Thigh_{brace}':(.16,0,.12*s),f'Shin_{brace}':(-.12,0,0)}
 strike={'Pelvis':(0,0,.30*s),'Spine1':(.08,0,.42*s),'Chest':(.18,0,.58*s),atk:(.18,0,.82*s),fore:(-.12,0,.66*s),hand:(0,0,.34*s),f'Thigh_{brace}':(-.20,0,-.14*s),f'Shin_{brace}':(.16,0,0),'Head':(.06,0,.18*s)}
 return {1:n,10:wind,18:strike,27:{'Chest':(.08,0,.24*s),atk:(.04,0,.28*s),fore:(-.04,0,.18*s)},38:n}
action('CrowncapBrute_SweepLeft',38,sweep('L')); action('CrowncapBrute_SweepRight',38,sweep('R'))
# Slam compresses the entire mass before both forearms descend; this is the readable anticipation window.
compress={'Pelvis':(-.16,0,0),'Spine1':(-.20,0,0),'Chest':(-.30,0,0),'Head':(.18,0,0),'UpperArm_L':(-.58,0,.20),'ForeArm_L':(-.72,0,.08),'UpperArm_R':(-.58,0,-.20),'ForeArm_R':(-.72,0,-.08),'Thigh_L':(.22,0,0),'Shin_L':(-.30,0,0),'Thigh_R':(.22,0,0),'Shin_R':(-.30,0,0)}
impact={'Pelvis':(.16,0,0),'Spine1':(.34,0,0),'Chest':(.52,0,0),'Head':(-.20,0,0),'UpperArm_L':(.72,0,.08),'ForeArm_L':(.86,0,0),'UpperArm_R':(.72,0,-.08),'ForeArm_R':(.86,0,0),'Thigh_L':(-.10,0,0),'Shin_L':(.18,0,0),'Thigh_R':(-.10,0,0),'Shin_R':(.18,0,0)}
action('CrowncapBrute_HeavySlam',46,{1:n,12:compress,22:{**compress,'Chest':(-.38,0,0),'UpperArm_L':(-.72,0,.18),'UpperArm_R':(-.72,0,-.18)},28:impact,36:{'Chest':(.20,0,0),'UpperArm_L':(.30,0,0),'UpperArm_R':(.30,0,0),'ForeArm_L':(.34,0,0),'ForeArm_R':(.34,0,0)},46:n})
action('CrowncapBrute_Hit',18,{1:n,6:{'Chest':(.10,0,.16),'Head':(-.14,0,-.18),'UpperArm_L':(.08,0,.12),'UpperArm_R':(.08,0,-.12)},18:n})
# Heavy stagger peels chest/head backward so the underside gill curtains become substantially more visible.
stagger={'Pelvis':(-.10,0,.08),'Spine1':(-.30,0,.10),'Chest':(-.48,0,.16),'Neck':(-.34,0,0),'Head':(-.42,0,-.08),'UpperArm_L':(.28,0,.34),'ForeArm_L':(-.20,0,.16),'UpperArm_R':(.28,0,-.34),'ForeArm_R':(-.20,0,-.16),'Thigh_L':(.22,0,.10),'Shin_L':(-.26,0,0),'Thigh_R':(.12,0,-.10),'Shin_R':(-.18,0,0)}
action('CrowncapBrute_HeavyStagger',54,{1:n,8:{'Chest':(-.18,0,.10),'Head':(-.20,0,-.06)},18:stagger,34:{**stagger,'Chest':(-.36,0,.12),'Head':(-.34,0,-.05)},44:{'Chest':(-.12,0,.04),'Head':(-.14,0,0),'Thigh_L':(.08,0,0),'Thigh_R':(.06,0,0)},54:n})
death={'Pelvis':(.30,0,.62),'Spine1':(.46,0,.74),'Chest':(.62,0,.82),'Neck':(.34,0,.18),'Head':(.44,0,.22),'UpperArm_L':(.56,0,.44),'ForeArm_L':(-.34,0,.18),'UpperArm_R':(.62,0,-.38),'ForeArm_R':(-.42,0,-.16),'Thigh_L':(.40,0,.18),'Shin_L':(-.58,0,0),'Thigh_R':(.52,0,-.16),'Shin_R':(-.64,0,0)}
action('CrowncapBrute_Death',72,{1:n,16:{'Chest':(.12,0,.18),'Head':(-.12,0,-.10)},36:{'Pelvis':(.18,0,.32),'Chest':(.38,0,.48),'UpperArm_L':(.32,0,.28),'UpperArm_R':(.36,0,-.24)},54:death,72:death})
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'
arm['authored_actions']='CrowncapBrute_Idle,CrowncapBrute_Walk,CrowncapBrute_Turn,CrowncapBrute_Alert,CrowncapBrute_SweepLeft,CrowncapBrute_SweepRight,CrowncapBrute_HeavySlam,CrowncapBrute_Hit,CrowncapBrute_HeavyStagger,CrowncapBrute_Death'
arm['animation_readability_contract']='asymmetric-sweep-weight-transfer;slam-full-mass-anticipation;heavy-stagger-exposes-under-cap-gills'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Crowncap Brute r2: 10 weighted actions with asymmetric sweeps, slam anticipation, and gill-exposing stagger',flush=True)
