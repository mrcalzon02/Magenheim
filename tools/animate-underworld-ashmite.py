#!/usr/bin/env python3
"""Author production animation actions for the Sulfurous Wastes Ashmite.

Run after generate-underworld-ashmite-textures.py and author-underworld-ashmite.py.
World translation remains Valheim-owned; no action keys Root.location.
"""
from pathlib import Path
from math import sin, pi
import bpy

ROOT=Path(__file__).resolve().parents[1]
BLEND=ROOT/'assets/models/source/underworld-creature-ashmite.blend'
if not BLEND.exists(): raise RuntimeError(f'Missing {BLEND}; run author-underworld-ashmite.py first')
bpy.ops.wm.open_mainfile(filepath=str(BLEND))
arm=bpy.data.objects.get('RIG_Ashmite_HOST_SWARM_HEXAPOD')
if not arm or arm.type!='ARMATURE': raise RuntimeError('Missing Ashmite production armature')

REQ_BONES=['Root','Thorax','Abdomen','Head','Jaw','Mandible_L','Mandible_R']+[f'{s}_{seg}{i}' for s in ('L','R') for i in range(1,4) for seg in ('Coxa','Femur','Tarsus')]
missing=[n for n in REQ_BONES if n not in arm.pose.bones]
if missing: raise RuntimeError('Missing Ashmite animation bones: '+', '.join(missing))

def action_fcurves(act):
    legacy=getattr(act,'fcurves',None)
    if legacy is not None: return list(legacy)
    curves=[]
    for layer in act.layers:
        for strip in layer.strips:
            for bag in getattr(strip,'channelbags',()): curves.extend(bag.fcurves)
    return curves

def action(name,end,poses,linear=False):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    act=bpy.data.actions.new(name); act.use_fake_user=True
    arm.animation_data_create(); arm.animation_data.action=act
    # Reset keyed bones before each authored pose so actions cannot inherit stale rotations.
    keyed=set(k for pose in poses.values() for k in pose)
    for frame,pose in sorted(poses.items()):
        for bname in keyed:
            pb=arm.pose.bones[bname]; pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0)
        for bname,rot in pose.items(): arm.pose.bones[bname].rotation_euler=rot
        for bname in keyed: arm.pose.bones[bname].keyframe_insert('rotation_euler',frame=frame,group=bname)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points: kp.interpolation='LINEAR' if linear else 'BEZIER'
    act.frame_start=1; act.frame_end=end
    return act

def gait(amount,body=0.0,lift=.42):
    """Alternating insect tripod: L1/R2/L3 opposes R1/L2/R3."""
    p={'Thorax':(0,body,0),'Abdomen':(-body*.35,0,0)}
    for s,i,phase in (('L',1,1),('R',1,-1),('L',2,-1),('R',2,1),('L',3,1),('R',3,-1)):
        p[f'{s}_Coxa{i}']=(0,phase*amount*.16,phase*amount)
        p[f'{s}_Femur{i}']=(phase*amount*lift,0,-phase*amount*.10)
        p[f'{s}_Tarsus{i}']=(-phase*amount*.30,0,phase*amount*.06)
    return p

def neutral():
    return {n:(0,0,0) for n in REQ_BONES if n!='Root'}

# Low armor barely breathes; mandibles continually test the air/ground.
action('Ashmite_Idle',64,{1:neutral(),16:{'Abdomen':(.025,0,0),'Mandible_L':(0,0,.055),'Mandible_R':(0,0,-.055)},32:neutral(),48:{'Head':(.018,0,0),'Mandible_L':(0,0,-.035),'Mandible_R':(0,0,.035)},64:neutral()})
# Deliberate walking preserves readable foot placement.
action('Ashmite_Walk',28,{1:gait(.16),8:gait(-.16),15:gait(.16),22:gait(-.16),28:gait(.16)})
# Scuttle is a materially faster, larger alternating tripod cycle with hard contact timing.
action('Ashmite_Scuttle',16,{1:gait(.31,.025,.52),5:gait(-.31,-.020,.52),9:gait(.31,.025,.52),13:gait(-.31,-.020,.52),16:gait(.31,.025,.52)},linear=True)
# Vent scavenging physically lowers the head and works jaw + both mandibles in repeated scraping strokes.
action('Ashmite_Scavenge',52,{1:neutral(),8:{'Head':(.30,0,0),'Jaw':(.24,0,0),'Mandible_L':(.08,0,.16),'Mandible_R':(.08,0,-.16)},16:{'Head':(.36,0,0),'Jaw':(-.18,0,0),'Mandible_L':(-.10,0,-.10),'Mandible_R':(-.10,0,.10)},24:{'Head':(.31,0,0),'Jaw':(.30,0,0),'Mandible_L':(.12,0,.18),'Mandible_R':(.12,0,-.18)},32:{'Head':(.38,0,0),'Jaw':(-.22,0,0),'Mandible_L':(-.12,0,-.12),'Mandible_R':(-.12,0,.12)},42:{'Head':(.25,0,0),'Jaw':(.12,0,0)},52:neutral()})
# Compact turns commit thorax/head and use asymmetric outer tripod reach.
left=gait(.12); left.update({'Thorax':(0,0,.22),'Head':(0,0,.16),'L_Coxa1':(0,-.03,-.08),'R_Coxa1':(0,.04,.24)})
right=gait(-.12); right.update({'Thorax':(0,0,-.22),'Head':(0,0,-.16),'R_Coxa1':(0,.03,.08),'L_Coxa1':(0,-.04,-.24)})
action('Ashmite_TurnLeft',22,{1:neutral(),7:left,15:left,22:neutral()})
action('Ashmite_TurnRight',22,{1:neutral(),7:right,15:right,22:neutral()})
# Short vicious bite: armored body stays low while mouth tools snap forward.
action('Ashmite_Bite',18,{1:neutral(),5:{'Head':(-.14,0,0),'Jaw':(.42,0,0),'Mandible_L':(0,0,.24),'Mandible_R':(0,0,-.24)},9:{'Head':(.20,0,0),'Jaw':(-.34,0,0),'Mandible_L':(.08,0,-.18),'Mandible_R':(.08,0,.18)},13:{'Jaw':(.08,0,0)},18:neutral()},linear=True)
action('Ashmite_Hit',14,{1:neutral(),4:{'Thorax':(.10,0,.16),'Head':(.16,0,.10),'Abdomen':(-.06,0,-.08)},8:{'Thorax':(-.04,0,-.06)},14:neutral()})
action('Ashmite_Stagger',24,{1:neutral(),6:{'Thorax':(.20,0,.28),'Head':(.26,0,.16),'Abdomen':(-.14,0,-.16)},13:{'Thorax':(-.10,0,-.14),'Head':(-.08,0,-.06)},24:neutral()})
# Small arthropod collapses and rolls onto its armor rather than receiving a mammal-like fall.
death={ 'Thorax':(.28,.10,1.18),'Head':(.42,0,.26),'Abdomen':(-.24,0,.34),'Jaw':(.20,0,0) }
for s in ('L','R'):
    for i in range(1,4): death[f'{s}_Coxa{i}']=(0,0,(.55 if s=='L' else -.55)); death[f'{s}_Femur{i}']=(.48,0,0); death[f'{s}_Tarsus{i}']=(-.36,0,0)
action('Ashmite_Death',42,{1:neutral(),10:{'Thorax':(.10,0,.35),'Head':(.18,0,.12)},24:death,42:death})

EXPECTED={'Ashmite_Idle':64,'Ashmite_Scavenge':52,'Ashmite_Walk':28,'Ashmite_Scuttle':16,'Ashmite_TurnLeft':22,'Ashmite_TurnRight':22,'Ashmite_Bite':18,'Ashmite_Hit':14,'Ashmite_Stagger':24,'Ashmite_Death':42}
for name,end in EXPECTED.items():
    act=bpy.data.actions.get(name)
    if not act or int(act.frame_end)!=end: raise RuntimeError(f'{name}: missing or incorrect frame contract')
    curves=action_fcurves(act)
    if not curves: raise RuntimeError(f'{name}: no animation curves')
    for fc in curves:
        if fc.data_path=='pose.bones["Root"].location': raise RuntimeError(f'{name}: Root translation forbidden')
# Scuttle must articulate every leg chain; scavenging must articulate all physical feeding tools.
scuttle_paths={fc.data_path for fc in action_fcurves(bpy.data.actions['Ashmite_Scuttle'])}
for s in ('L','R'):
    for i in range(1,4):
        for seg in ('Coxa','Femur','Tarsus'):
            path=f'pose.bones["{s}_{seg}{i}"].rotation_euler'
            if path not in scuttle_paths: raise RuntimeError(f'Ashmite_Scuttle missing {s}_{seg}{i}')
scav_paths={fc.data_path for fc in action_fcurves(bpy.data.actions['Ashmite_Scavenge'])}
for b in ('Head','Jaw','Mandible_L','Mandible_R'):
    if f'pose.bones["{b}"].rotation_euler' not in scav_paths: raise RuntimeError(f'Ashmite_Scavenge missing {b}')
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'
arm['authored_actions']=','.join(EXPECTED)
arm['animation_readability_contract']='fast-alternating-tripod-scuttle;distinct-deliberate-walk;physical-vent-scraping-mouth-tools;bilateral-compact-turns;short-bite;lightweight-reactions;arthropod-collapse;no-root-translation'
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
print(f'Authored {len(EXPECTED)} Ashmite production actions into {BLEND}')
