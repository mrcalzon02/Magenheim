#!/usr/bin/env python3
"""Author Lantern Angler local-deformation production actions in Blender."""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-lantern-angler.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='lantern-angler' or arm.get('host_rig')!='HOST-AQUATIC-FISH': raise RuntimeError('Wrong Lantern Angler source')

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

def swim(ph,amp=.20):
    out={}
    chain=[('Spine_1',.20),('Spine_2',.35),('Spine_3',.55),('Spine_4',.80),('Tail_1',1.10),('Tail_2',1.40)]
    for i,(b,g) in enumerate(chain): out[b]=R(.015*math.cos(ph-i*.3),0,amp*g*math.sin(ph-i*.58))
    out['Fin_L']=R(.05*math.sin(ph),-.08,.07*math.sin(ph+.8)); out['Fin_R']=R(-.05*math.sin(ph),.08,-.07*math.sin(ph+.8))
    return out

def lure(ph,amp=.16):
    return {'Lure_1':R(.05*math.sin(ph),.03*math.cos(ph),amp*.4*math.sin(ph)),
            'Lure_2':R(.09*math.sin(ph-.4),.05*math.cos(ph-.3),amp*.7*math.sin(ph-.5)),
            'Lure_3':R(.13*math.sin(ph-.8),.07*math.cos(ph-.6),amp*math.sin(ph-.9))}

def cycle(amp,lamp):
    return {1:{**swim(0,amp),**lure(0,lamp)},10:{**swim(math.pi/2,amp),**lure(math.pi/2,lamp)},20:{**swim(math.pi,amp),**lure(math.pi,lamp)},30:{**swim(3*math.pi/2,amp),**lure(3*math.pi/2,lamp)},40:{**swim(2*math.pi,amp),**lure(2*math.pi,lamp)}}

action('LanternAngler_SwimIdle',40,cycle(.07,.16))
action('LanternAngler_Cruise',40,cycle(.23,.10))
action('LanternAngler_BankLeft',30,{1:swim(0,.15),12:{**swim(1.4,.17),'Spine_1':R(.03,-.14,-.16),'Head':R(.02,-.10,-.14),'Fin_L':R(.22,-.28,-.14),'Fin_R':R(-.03,.03,.18)},22:{**swim(2.5,.16),'Spine_1':R(.03,-.18,-.20),'Head':R(.02,-.14,-.18)},30:swim(math.pi,.14)})
action('LanternAngler_BankRight',30,{1:swim(0,.15),12:{**swim(1.4,.17),'Spine_1':R(.03,.14,.16),'Head':R(.02,.10,.14),'Fin_L':R(.03,-.03,-.18),'Fin_R':R(-.22,.28,.14)},22:{**swim(2.5,.16),'Spine_1':R(.03,.18,.20),'Head':R(.02,.14,.18)},30:swim(math.pi,.14)})
action('LanternAngler_LureIdle',80,{1:{**swim(0,.04),**lure(0,.24)},20:{**swim(.4,.04),**lure(math.pi/2,.24)},40:{**swim(.8,.04),**lure(math.pi,.24)},60:{**swim(1.2,.04),**lure(3*math.pi/2,.24)},80:{**swim(1.6,.04),**lure(2*math.pi,.24)}})
action('LanternAngler_LureTell',44,{1:lure(0,.12),12:{'Lure_1':R(-.18,.04,.05),'Lure_2':R(-.34,.08,-.12),'Lure_3':R(-.48,.10,-.24)},24:{'Lure_1':R(-.24,.05,.07),'Lure_2':R(-.44,.10,-.18),'Lure_3':R(-.60,.13,-.32)},32:{'Lure_1':R(.10,-.04,-.10),'Lure_2':R(.24,-.08,.18),'Lure_3':R(.38,-.10,.32)},44:lure(0,.12)})
action('LanternAngler_Bite',34,{1:{},9:{'Head':R(-.06,0,0),'Jaw':R(.42,0,0),'Throat':RS((.10,0,0),(1,1,1.14))},16:{'Head':R(-.12,0,0),'Jaw':R(.68,0,0),'Throat':RS((.18,0,0),(1.08,1.08,1.28))},21:{'Head':R(.08,0,0),'Jaw':R(.08,0,0),'Throat':RS((-.05,0,0),(.96,.96,.94))},34:{}})
action('LanternAngler_PressureTell',58,{1:{},14:{'Throat':RS((.05,0,0),(1.08,1.10,1.18)),'Jaw':R(.10,0,0)},30:{'Throat':RS((.12,0,0),(1.22,1.24,1.42)),'Jaw':R(.18,0,0),'Fin_L':R(.15,-.22,0),'Fin_R':R(-.15,.22,0)},48:{'Throat':RS((.17,0,0),(1.34,1.36,1.62)),'Jaw':R(.25,0,0),'Head':R(-.07,0,0)},58:{'Throat':RS((.18,0,0),(1.38,1.40,1.68)),'Jaw':R(.28,0,0)}})
action('LanternAngler_PressureRelease',30,{1:{'Throat':RS((.18,0,0),(1.38,1.40,1.68)),'Jaw':R(.28,0,0)},6:{'Throat':RS((-.12,0,0),(.82,.84,.72)),'Jaw':R(.52,0,0),'Head':R(.14,0,0),'Spine_1':R(.10,0,-.08)},14:{'Throat':RS((-.05,0,0),(.92,.94,.88)),'Jaw':R(.20,0,0)},30:{}})
action('LanternAngler_Hit',20,{1:{},6:{'Head':R(.08,.05,-.18),'Spine_1':R(.05,.03,-.16),'Lure_1':R(.18,.10,.20),'Lure_2':R(.24,.12,.28),'Lure_3':R(.30,.15,.34)},12:{'Head':R(-.03,0,.08),'Lure_2':R(-.12,0,-.16),'Lure_3':R(-.18,0,-.22)},20:{}})
action('LanternAngler_Death',72,{1:{},20:{'Head':R(.10,.05,.16),'Spine_2':R(.08,0,-.12),'Lure_1':R(.20,.06,.16)},44:{'Spine_1':R(.22,.18,.30),'Spine_2':R(.30,.14,-.24),'Spine_3':R(.38,.10,.28),'Spine_4':R(.46,.06,-.34),'Tail_1':R(.55,0,.42),'Tail_2':R(.66,0,-.52),'Jaw':R(.34,0,0)},72:{'Spine_1':R(.48,.30,.38),'Spine_2':R(.62,.22,-.34),'Spine_3':R(.78,.14,.42),'Spine_4':R(.92,.08,-.48),'Tail_1':R(1.02,0,.58),'Tail_2':R(1.12,0,-.68),'Jaw':R(.48,0,0),'Lure_3':R(1.02,.30,.70)}})

names=['LanternAngler_SwimIdle','LanternAngler_Cruise','LanternAngler_BankLeft','LanternAngler_BankRight','LanternAngler_LureIdle','LanternAngler_LureTell','LanternAngler_Bite','LanternAngler_PressureTell','LanternAngler_PressureRelease','LanternAngler_Hit','LanternAngler_Death']
for name in names:
    a=bpy.data.actions.get(name)
    if not a: raise RuntimeError(f'Missing action {name}')
    curves=getattr(a,'fcurves',[])
    for fc in curves:
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'{name}: Root translation forbidden')
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'; arm['authored_actions']=','.join(names)
arm['animation_readability_contract']='traveling-caudal-wave;three-stage-lure-lag;physical-lure-tell;jaw-throat-bite;throat-pressure-inflate-release;bank-fin-asymmetry;no-root-translation'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Lantern Angler r2: 11 production actions',flush=True)
