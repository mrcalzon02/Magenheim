#!/usr/bin/env python3
"""Author Blackwater Lamprey locomotion and latch presentation; Valheim owns attachment gameplay."""
from pathlib import Path
import math, bpy
ROOT=Path(__file__).resolve().parents[1]; MODEL=ROOT/'assets/models/source/underworld-creature-blackwater-lamprey.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='blackwater-lamprey' or arm.get('host_rig')!='HOST-AQUATIC-FISH': raise RuntimeError('Wrong Lamprey source identity')
CHAIN=['Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2']; N={}
def wave(phase,amp): return {b:(0,0,amp*(.38+i*.11)*math.sin(phase-i*.68)) for i,b in enumerate(CHAIN)}
def action(name,end,poses):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    a=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=a
    for f,pose in poses.items():
        for b,r in pose.items():
            p=arm.pose.bones.get(b)
            if not p: raise RuntimeError(f'{name}: missing {b}')
            p.rotation_mode='XYZ'; p.rotation_euler=r; p.keyframe_insert('rotation_euler',frame=f,group=b)
    a.frame_start=1; a.frame_end=end
# Low station-keeping and progressively stronger traveling propulsion.
action('Lamprey_SwimIdle',72,{1:wave(0,.09),18:wave(math.pi/2,.09),36:wave(math.pi,.09),54:wave(3*math.pi/2,.09),72:wave(2*math.pi,.09)})
action('Lamprey_Cruise',48,{1:wave(0,.24),12:wave(math.pi/2,.24),24:wave(math.pi,.24),36:wave(3*math.pi/2,.24),48:wave(2*math.pi,.24)})
action('Lamprey_Sprint',24,{1:wave(0,.40),6:wave(math.pi/2,.40),12:wave(math.pi,.40),18:wave(3*math.pi/2,.40),24:wave(2*math.pi,.40)})
action('Lamprey_BankLeft',32,{1:N,12:{**wave(.8,.20),'Spine_1':(0,0,-.20),'Head':(0,0,-.16)},20:{**wave(1.6,.25),'Spine_1':(.03,-.06,-.34),'Head':(0,0,-.25)},32:N})
action('Lamprey_BankRight',32,{1:N,12:{**wave(-.8,.20),'Spine_1':(0,0,.20),'Head':(0,0,.16)},20:{**wave(-1.6,.25),'Spine_1':(.03,.06,.34),'Head':(0,0,.25)},32:N})
# Lunge coils then straightens. Latch uses oral-disc flare/compression only; no root translation is authored.
action('Lamprey_Lunge',28,{1:wave(0,.10),8:{**wave(.9,.34),'Head':(.08,0,0)},15:{**wave(2.0,.46),'Head':(.18,0,0),'MouthRing':(-.12,0,0),'Jaw':(-.18,0,0)},28:{**wave(math.pi,.12),'Head':(0,0,0)}})
action('Lamprey_Latch',22,{1:{'MouthRing':(0,0,0),'Jaw':(0,0,0)},7:{'MouthRing':(-.30,0,0),'Jaw':(-.38,0,0),'Head':(.10,0,0),**wave(.6,.12)},13:{'MouthRing':(.20,0,0),'Jaw':(.28,0,0),'Head':(.15,0,0),**wave(1.2,.08)},22:{'MouthRing':(.12,0,0),'Jaw':(.18,0,0),'Head':(.10,0,0),**wave(1.8,.05)}})
# Attached idle remains alive: sucker pulses while posterior body undulates at restrained amplitude.
action('Lamprey_AttachedIdle',48,{1:{'MouthRing':(.12,0,0),'Jaw':(.18,0,0),**wave(0,.045)},12:{'MouthRing':(.18,0,0),'Jaw':(.24,0,0),**wave(math.pi/2,.055)},24:{'MouthRing':(.10,0,0),'Jaw':(.16,0,0),**wave(math.pi,.045)},36:{'MouthRing':(.17,0,0),'Jaw':(.23,0,0),**wave(3*math.pi/2,.055)},48:{'MouthRing':(.12,0,0),'Jaw':(.18,0,0),**wave(2*math.pi,.045)}})
action('Lamprey_Detach',26,{1:{'MouthRing':(.12,0,0),'Jaw':(.18,0,0)},7:{'MouthRing':(-.36,0,0),'Jaw':(-.44,0,0),'Head':(-.08,0,0)},14:{'MouthRing':(-.10,0,0),'Jaw':(-.12,0,0),'Head':(-.18,0,0),**wave(.9,.22)},26:{'MouthRing':(0,0,0),'Jaw':(0,0,0),**wave(2.0,.12)}})
action('Lamprey_Hit',18,{1:N,6:{**wave(.9,.22),'Spine_1':(.08,.08,.25),'Head':(-.12,0,-.16)},18:N})
action('Lamprey_Death',64,{1:N,20:{**wave(.8,.10),'Spine_1':(.20,0,.18)},42:{**wave(1.5,.05),'Spine_1':(.48,0,.62),'Head':(.28,0,.20),'MouthRing':(-.20,0,0)},64:{'Spine_1':(.74,0,1.08),'Head':(.42,0,.30),'MouthRing':(-.30,0,0),'Jaw':(-.28,0,0)}})
arm.animation_data.action=None; arm['production_contract']='production-creature-r2'
arm['authored_actions']='Lamprey_SwimIdle,Lamprey_Cruise,Lamprey_Sprint,Lamprey_BankLeft,Lamprey_BankRight,Lamprey_Lunge,Lamprey_Latch,Lamprey_AttachedIdle,Lamprey_Detach,Lamprey_Hit,Lamprey_Death'
arm['animation_readability_contract']='traveling-six-stage-propulsion;oral-disc-latch-without-root-translation;living-attached-idle;detach-recovery'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Blackwater Lamprey r2: 11 actions; traveling propulsion and oral-disc latch lifecycle',flush=True)