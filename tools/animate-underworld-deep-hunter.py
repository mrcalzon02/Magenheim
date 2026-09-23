#!/usr/bin/env python3
"""Author Deep Hunter local-deformation production actions in Blender.

Art-only animation pass for the Blackwater apex predator. World translation remains
Valheim-owned; these actions author silhouette, propulsion, attack and recovery deformation.
"""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-deep-hunter.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='deep-hunter' or arm.get('host_rig')!='HOST-AQUATIC-FISH-APEX': raise RuntimeError('Wrong Deep Hunter source')

CHAIN=['Spine_1','Spine_2','Spine_3','Spine_4','Spine_5','Tail_1','Tail_2']
GAINS=[.16,.25,.38,.56,.78,1.05,1.38]

def clear():
    for p in arm.pose.bones:
        p.rotation_mode='XYZ'; p.rotation_euler=(0,0,0); p.location=(0,0,0); p.scale=(1,1,1)

def action(name,end,poses):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    a=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=a; clear()
    for frame,pose in poses.items():
        for bone,data in pose.items():
            p=arm.pose.bones.get(bone)
            if not p: raise RuntimeError(f'{name}: missing {bone}')
            if 'r' in data:
                p.rotation_euler=data['r']; p.keyframe_insert('rotation_euler',frame=frame,group=bone)
            if 's' in data:
                p.scale=data['s']; p.keyframe_insert('scale',frame=frame,group=bone)
    a.frame_start=1; a.frame_end=end

def R(x=0,y=0,z=0): return {'r':(x,y,z)}
def RS(r,s): return {'r':r,'s':s}

def wave(ph,amp=.20,pitch=.018,lag=.52):
    out={}
    for i,(b,g) in enumerate(zip(CHAIN,GAINS)):
        q=ph-i*lag
        out[b]=R(pitch*math.cos(q),0,amp*g*math.sin(q))
    out['Fin_L']=R(.05*math.sin(ph),-.09,.08*math.sin(ph+.7))
    out['Fin_R']=R(-.05*math.sin(ph),.09,-.08*math.sin(ph+.7))
    return out

def cycle(amp,end=48,lag=.52):
    return {1:wave(0,amp,lag=lag),end//4:wave(math.pi/2,amp,lag=lag),end//2:wave(math.pi,amp,lag=lag),3*end//4:wave(3*math.pi/2,amp,lag=lag),end:wave(2*math.pi,amp,lag=lag)}

action('DeepHunter_SwimIdle',64,cycle(.065,64,.60))
action('DeepHunter_Cruise',48,cycle(.22,48,.52))
# Sprint is deliberately faster and more caudal: a visibly different propulsion regime, not time-scaled cruise.
action('DeepHunter_Sprint',32,cycle(.34,32,.45))
action('DeepHunter_TurnLeft',36,{1:wave(0,.14),12:{**wave(1.3,.18),'Spine_1':R(.03,-.10,-.12),'Spine_2':R(.02,-.13,-.17),'Head':R(.02,-.18,-.20),'Fin_L':R(.24,-.32,-.18),'Fin_R':R(-.04,.04,.24)},24:{**wave(2.5,.16),'Spine_1':R(.03,-.14,-.18),'Spine_2':R(.02,-.18,-.23),'Head':R(.02,-.23,-.27)},36:wave(math.pi,.13)})
action('DeepHunter_TurnRight',36,{1:wave(0,.14),12:{**wave(1.3,.18),'Spine_1':R(.03,.10,.12),'Spine_2':R(.02,.13,.17),'Head':R(.02,.18,.20),'Fin_L':R(.04,-.04,-.24),'Fin_R':R(-.24,.32,.18)},24:{**wave(2.5,.16),'Spine_1':R(.03,.14,.18),'Spine_2':R(.02,.18,.23),'Head':R(.02,.23,.27)},36:wave(math.pi,.13)})
# Physical jaw strike: head recoil -> maximum gape -> snap-through -> recovery.
action('DeepHunter_Bite',34,{1:{},8:{'Head':R(-.10,0,0),'Jaw':R(.30,0,0),'Spine_1':R(-.04,0,0)},15:{'Head':R(-.16,0,0),'Jaw':R(.72,0,0),'Spine_1':R(-.08,0,-.05),'Fin_L':R(.14,-.18,0),'Fin_R':R(-.14,.18,0)},20:{'Head':R(.13,0,0),'Jaw':R(.06,0,0),'Spine_1':R(.10,0,.04),'Spine_2':R(.06,0,.05)},34:{}})
# Ram compresses the whole front half, braces fins, then drives a body-wave recoil after impact.
action('DeepHunter_Ram',52,{1:{},14:{'Head':R(-.09,0,0),'Spine_1':R(-.08,0,0),'Spine_2':R(-.06,0,0),'Fin_L':R(.24,-.28,0),'Fin_R':R(-.24,.28,0)},28:{'Head':R(-.17,0,0),'Spine_1':R(-.13,0,0),'Spine_2':R(-.10,0,0),'Spine_3':R(-.06,0,0),'Fin_L':R(.31,-.34,0),'Fin_R':R(-.31,.34,0)},34:{'Head':R(.18,0,.06),'Spine_1':R(.14,0,-.08),'Spine_2':R(.10,0,.11),'Spine_3':R(.07,0,-.13),'Spine_4':R(.04,0,.16),'Spine_5':R(.02,0,-.20),'Tail_1':R(0,0,.28),'Tail_2':R(0,0,-.38)},52:{}})
# Breach uses local pitch/fin/body deformation only; navigation owns the actual water exit trajectory.
action('DeepHunter_Breach',46,{1:wave(0,.16),12:{**wave(.9,.23),'Head':R(-.12,0,0),'Spine_1':R(-.10,0,0),'Fin_L':R(.34,-.24,0),'Fin_R':R(-.34,.24,0)},26:{'Head':R(-.34,0,0),'Spine_1':R(-.28,0,0),'Spine_2':R(-.22,0,.05),'Spine_3':R(-.15,0,-.08),'Spine_4':R(-.08,0,.12),'Tail_1':R(.06,0,-.22),'Tail_2':R(.12,0,.34),'Fin_L':R(.42,-.30,0),'Fin_R':R(-.42,.30,0)},46:{'Head':R(-.16,0,0),'Spine_1':R(-.12,0,0),'Tail_2':R(.04,0,.12)}})
action('DeepHunter_BreachRecover',42,{1:{'Head':R(-.16,0,0),'Spine_1':R(-.12,0,0),'Tail_2':R(.04,0,.12)},12:{'Head':R(.28,0,0),'Spine_1':R(.22,0,0),'Spine_2':R(.17,0,-.07),'Spine_3':R(.11,0,.10),'Spine_4':R(.06,0,-.14),'Tail_1':R(-.05,0,.22),'Tail_2':R(-.10,0,-.32),'Fin_L':R(-.22,-.18,0),'Fin_R':R(.22,.18,0)},26:wave(2.2,.18),42:wave(math.pi,.12)})
# Tail strike winds from shoulders to tail, then reverses through the same seven-stage chain.
action('DeepHunter_TailStrike',44,{1:{},12:{'Spine_1':R(0,0,.06),'Spine_2':R(0,0,.11),'Spine_3':R(0,0,.19),'Spine_4':R(0,0,.30),'Spine_5':R(0,0,.44),'Tail_1':R(0,0,.62),'Tail_2':R(0,0,.84)},22:{'Spine_1':R(0,0,-.08),'Spine_2':R(0,0,-.15),'Spine_3':R(0,0,-.26),'Spine_4':R(0,0,-.40),'Spine_5':R(0,0,-.58),'Tail_1':R(0,0,-.82),'Tail_2':R(0,0,-1.08)},32:{'Spine_3':R(0,0,.12),'Spine_4':R(0,0,.20),'Spine_5':R(0,0,.28),'Tail_1':R(0,0,.40),'Tail_2':R(0,0,.55)},44:{}})
action('DeepHunter_Hit',22,{1:{},6:{'Head':R(.08,.04,-.16),'Spine_1':R(.05,.03,-.13),'Spine_2':R(.03,0,-.09),'Tail_1':R(0,0,.14),'Tail_2':R(0,0,.22)},13:{'Head':R(-.04,0,.07),'Spine_2':R(-.02,0,.06),'Tail_2':R(0,0,-.12)},22:{}})
action('DeepHunter_Stagger',38,{1:{},10:{'Head':R(.18,.12,-.24),'Spine_1':R(.15,.08,-.20),'Spine_2':R(.12,.04,-.14),'Fin_L':R(.30,-.18,-.18),'Fin_R':R(-.18,.28,.12)},22:{'Head':R(-.10,-.06,.16),'Spine_1':R(-.08,-.04,.13),'Spine_3':R(.06,0,-.12),'Tail_1':R(.10,0,.22),'Tail_2':R(.14,0,.30)},38:{}})
action('DeepHunter_Death',88,{1:{},24:{'Head':R(.12,.08,.18),'Spine_2':R(.10,0,-.12),'Tail_1':R(.18,0,.24)},52:{'Head':R(.38,.24,.32),'Spine_1':R(.32,.18,.28),'Spine_2':R(.42,.14,-.32),'Spine_3':R(.52,.10,.38),'Spine_4':R(.64,.06,-.44),'Spine_5':R(.74,.03,.50),'Tail_1':R(.88,0,-.58),'Tail_2':R(1.02,0,.70),'Jaw':R(.38,0,0)},88:{'Head':R(.62,.34,.42),'Spine_1':R(.54,.28,.38),'Spine_2':R(.68,.20,-.44),'Spine_3':R(.82,.14,.50),'Spine_4':R(.96,.09,-.56),'Spine_5':R(1.08,.05,.62),'Tail_1':R(1.20,0,-.72),'Tail_2':R(1.30,0,.82),'Jaw':R(.52,0,0)}})

names=['DeepHunter_SwimIdle','DeepHunter_Cruise','DeepHunter_Sprint','DeepHunter_TurnLeft','DeepHunter_TurnRight','DeepHunter_Bite','DeepHunter_Ram','DeepHunter_Breach','DeepHunter_BreachRecover','DeepHunter_TailStrike','DeepHunter_Hit','DeepHunter_Stagger','DeepHunter_Death']
for name in names:
    a=bpy.data.actions.get(name)
    if not a: raise RuntimeError(f'Missing action {name}')
    for fc in getattr(a,'fcurves',[]): 
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'{name}: Root translation forbidden')
for name in ['DeepHunter_Cruise','DeepHunter_Sprint','DeepHunter_TailStrike']:
    a=bpy.data.actions[name]
    groups={g.name for g in a.groups}
    missing=[b for b in CHAIN if b not in groups]
    if missing: raise RuntimeError(f'{name}: missing seven-stage deformation {missing}')
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'; arm['authored_actions']=','.join(names)
arm['animation_readability_contract']='seven-stage-traveling-wave;increasing-caudal-amplitude;sprint-distinct-from-cruise;large-bank-turns;physical-jaw-bite;full-body-ram;local-deformation-breach-recovery;propagating-tail-strike;no-root-translation'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Deep Hunter r2: 13 production actions',flush=True)
