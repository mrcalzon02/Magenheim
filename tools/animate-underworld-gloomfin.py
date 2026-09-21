#!/usr/bin/env python3
"""Author Gloomfin actions with traveling six-stage propulsion, active fins and jaw bite."""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-gloomfin.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-gloomfin.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='gloomfin' or arm.get('host_rig')!='HOST-AQUATIC-FISH': raise RuntimeError('Wrong Gloomfin model/host identity')

CHAIN=['Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2']

def action(name,end,poses):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
    for frame,pose in poses.items():
        for bone,rot in pose.items():
            pb=arm.pose.bones.get(bone)
            if not pb: raise RuntimeError(f'{name}: missing bone {bone}')
            pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bone)
    act.frame_start=1; act.frame_end=end

def wave(phase,amp):
    # Phase lag and increasing caudal amplitude create a traveling body wave, not rigid yaw.
    out={}
    for i,b in enumerate(CHAIN):
        a=amp*(.30+i*.14)*math.sin(phase-i*.72)
        out[b]=(0,0,a)
    return out

def fins(l,r): return {'Fin_L':(l*.34,l*.12,-l*.08),'Fin_R':(r*.34,-r*.12,r*.08)}
N={}
# Idle: low-amplitude station keeping with the full chain alive.
action('Gloomfin_SwimIdle',72,{1:{**wave(0,.10),**fins(.08,.08)},18:{**wave(math.pi/2,.10),**fins(-.05,.07)},36:{**wave(math.pi,.10),**fins(.06,-.05)},54:{**wave(3*math.pi/2,.10),**fins(-.06,-.06)},72:{**wave(2*math.pi,.10),**fins(.08,.08)}})
# Cruise: one broad propulsion cycle; every stage participates with increasing amplitude.
action('Gloomfin_Cruise',48,{1:{**wave(0,.24),**fins(.10,.10)},12:{**wave(math.pi/2,.24),**fins(-.08,.08)},24:{**wave(math.pi,.24),**fins(-.10,-.10)},36:{**wave(3*math.pi/2,.24),**fins(.08,-.08)},48:{**wave(2*math.pi,.24),**fins(.10,.10)}})
# Sprint: greater amplitude and twice the cadence of cruise.
action('Gloomfin_Sprint',24,{1:{**wave(0,.39),**fins(.16,.16)},6:{**wave(math.pi/2,.39),**fins(-.14,.14)},12:{**wave(math.pi,.39),**fins(-.16,-.16)},18:{**wave(3*math.pi/2,.39),**fins(.14,-.14)},24:{**wave(2*math.pi,.39),**fins(.16,.16)}})
# Banks bend the body into the turn and load the outside pectoral fin.
action('Gloomfin_BankLeft',32,{1:N,10:{**wave(.7,.16),'Spine_1':(0,0,-.18),**fins(-.16,.42)},20:{**wave(1.5,.20),'Spine_1':(.04,-.08,-.30),'Head':(0,0,-.18),**fins(-.24,.58)},32:N})
action('Gloomfin_BankRight',32,{1:N,10:{**wave(-.7,.16),'Spine_1':(0,0,.18),**fins(.42,-.16)},20:{**wave(-1.5,.20),'Spine_1':(.04,.08,.30),'Head':(0,0,.18),**fins(.58,-.24)},32:N})
# Bite is jaw-led: body compression and tail impulse support the strike, but do not replace jaw articulation.
action('Gloomfin_Bite',26,{1:{**wave(0,.10),'Jaw':(0,0,0)},7:{**wave(.8,.16),'Jaw':(-.38,0,0),'Head':(.08,0,0),**fins(-.08,-.08)},13:{**wave(1.7,.30),'Jaw':(-.62,0,0),'Head':(.15,0,0),**fins(.16,.16)},17:{**wave(2.4,.24),'Jaw':(-.08,0,0),'Head':(-.05,0,0)},26:{**wave(math.pi,.10),'Jaw':(0,0,0)}})
action('Gloomfin_Hit',18,{1:N,6:{**wave(.9,.18),'Spine_1':(.08,.10,.24),'Head':(-.10,0,-.18),**fins(-.18,.25)},18:N})
action('Gloomfin_Death',64,{1:N,18:{**wave(.8,.10),'Spine_1':(.18,0,.18),**fins(-.12,-.08)},40:{**wave(1.6,.06),'Spine_1':(.48,0,.62),'Head':(.24,0,.18),**fins(-.38,-.32),'Jaw':(-.18,0,0)},64:{'Spine_1':(.72,0,1.12),'Head':(.38,0,.28),'Fin_L':(-.58,0,0),'Fin_R':(-.52,0,0),'Jaw':(-.28,0,0)}})
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'
arm['authored_actions']='Gloomfin_SwimIdle,Gloomfin_Cruise,Gloomfin_Sprint,Gloomfin_BankLeft,Gloomfin_BankRight,Gloomfin_Bite,Gloomfin_Hit,Gloomfin_Death'
arm['animation_readability_contract']='traveling-six-stage-propulsion;sprint-amplitude-cadence;asymmetric-fin-banks;jaw-led-bite'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Gloomfin r2: 8 actions; traveling six-stage propulsion, active banking fins, jaw-led bite',flush=True)
