#!/usr/bin/env python3
"""Author Shoreclaw amphibious actions without replacing Valheim locomotion authority."""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-shoreclaw.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-shoreclaw.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='shoreclaw' or arm.get('host_rig')!='HOST-AMPHIB-ARMORED': raise RuntimeError('Wrong Shoreclaw model/host identity')

LEGS=[f'Leg_{s}_{n}' for s in ('L','R') for n in range(1,5)]
SHELLS=[f'ShellPlate_{i}' for i in range(1,5)]

def action(name,end,poses):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
    # Clear pose state so sparse action definitions cannot inherit transforms from the previous action.
    for pb in arm.pose.bones:
        pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0); pb.location=(0,0,0); pb.scale=(1,1,1)
    for frame,pose in poses.items():
        for bone,rot in pose.items():
            pb=arm.pose.bones.get(bone)
            if not pb: raise RuntimeError(f'{name}: missing bone {bone}')
            pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bone)
    act.frame_start=1; act.frame_end=end

def land(step):
    """Alternating diagonal weight transfer; all eight legs participate."""
    out={}
    for side in ('L','R'):
        sign=-1 if side=='L' else 1
        for n in range(1,5):
            phase=step + (n%2)*math.pi + (0 if side=='L' else math.pi)
            swing=math.sin(phase); lift=max(0.0,math.cos(phase))
            pre=f'Leg_{side}_{n}'
            out[pre+'_Upper']=(.10*lift, sign*.12*swing, .24*swing)
            out[pre+'_Lower']=(-.18*lift, sign*.08*swing, -.20*swing)
            out[pre+'_Foot']=(.16*lift, sign*.05*swing, -.13*swing)
    return out

def swim(phase,amp=.48):
    """Broad paddle power/recovery strokes, intentionally unlike terrestrial scuttle."""
    out={}
    for side in ('L','R'):
        sign=-1 if side=='L' else 1
        for n in range(1,5):
            p=phase+(n-1)*.34+(0 if side=='L' else .20)
            stroke=math.sin(p); recover=math.cos(p)
            pre=f'Leg_{side}_{n}'
            out[pre+'_Upper']=(.10*recover, sign*.20*stroke, amp*stroke)
            out[pre+'_Lower']=(-.12*recover, sign*.12*stroke, -.34*stroke)
            # Terminal paddle pitches broadside on power stroke and feathers on recovery.
            out[pre+'_Foot']=(.58*stroke, sign*.10*recover, -.28*stroke)
    return out

def shell(phase,amp=.055):
    # Plate phase lag keeps rigid armor visually independent from the deforming abdomen.
    return {b:(amp*math.sin(phase-i*.42), .025*math.sin(phase+i*.31), .018*math.cos(phase-i*.27)) for i,b in enumerate(SHELLS)}

N={}
action('Shoreclaw_Idle',72,{1:{**shell(0)},24:{**shell(2.1),'Body':(.018,0,0)},48:{**shell(4.2),'Body':(-.012,0,0)},72:{**shell(2*math.pi)}})
action('Shoreclaw_Scuttle',32,{1:{**land(0),**shell(0,.07)},8:{**land(math.pi/2),**shell(math.pi/2,.07)},16:{**land(math.pi),**shell(math.pi,.07)},24:{**land(3*math.pi/2),**shell(3*math.pi/2,.07)},32:{**land(2*math.pi),**shell(2*math.pi,.07)}})
# Turning preserves planted-leg asymmetry rather than rotating Root through world space.
action('Shoreclaw_TurnLeft',28,{1:{**land(0)},10:{**land(1.2),'Body':(.02,0,-.16),'Claw_L_Arm':(0,0,-.10),'Claw_R_Arm':(0,0,.18),**shell(1.2,.07)},18:{**land(2.4),'Body':(.02,0,-.25),**shell(2.4,.08)},28:{**land(math.pi)}})
action('Shoreclaw_TurnRight',28,{1:{**land(0)},10:{**land(-1.2),'Body':(.02,0,.16),'Claw_L_Arm':(0,0,-.18),'Claw_R_Arm':(0,0,.10),**shell(-1.2,.07)},18:{**land(-2.4),'Body':(.02,0,.25),**shell(-2.4,.08)},28:{**land(-math.pi)}})
# Crusher: slow, heavy left-side wind-up and body commitment.
action('Shoreclaw_CrusherAttack',38,{1:N,10:{'Claw_L_Arm':(-.12,.10,-.34),'Claw_L_Palm':(.08,-.18,-.22),'Body':(.03,0,.08)},22:{'Claw_L_Arm':(.28,-.12,.52),'Claw_L_Palm':(-.22,.20,.44),'Body':(-.08,.03,-.16),**shell(1.6,.09)},28:{'Claw_L_Arm':(.12,0,.22),'Claw_L_Palm':(-.10,.08,.18)},38:N})
# Cutter: shorter right-side snap with less body displacement and faster recovery.
action('Shoreclaw_CutterAttack',24,{1:N,5:{'Claw_R_Arm':(-.06,-.05,.22),'Claw_R_Palm':(.04,.10,.18)},10:{'Claw_R_Arm':(.16,.08,-.46),'Claw_R_Palm':(-.14,-.12,-.38),'Body':(-.025,0,.06)},15:{'Claw_R_Arm':(.04,0,-.16),'Claw_R_Palm':(-.04,0,-.12)},24:N})
action('Shoreclaw_Guard',42,{1:N,12:{'Claw_L_Arm':(-.16,.18,-.28),'Claw_L_Palm':(.18,-.12,-.32),'Claw_R_Arm':(-.12,-.12,.24),'Claw_R_Palm':(.14,.10,.28),'Body':(.06,0,0),**shell(.8,.10)},32:{'Claw_L_Arm':(-.16,.18,-.28),'Claw_L_Palm':(.18,-.12,-.32),'Claw_R_Arm':(-.12,-.12,.24),'Claw_R_Palm':(.14,.10,.28),'Body':(.05,0,0),**shell(2.4,.10)},42:N})
action('Shoreclaw_SwimIdle',64,{1:{**swim(0,.22),**shell(0,.035)},16:{**swim(math.pi/2,.22),**shell(math.pi/2,.035)},32:{**swim(math.pi,.22),**shell(math.pi,.035)},48:{**swim(3*math.pi/2,.22),**shell(3*math.pi/2,.035)},64:{**swim(2*math.pi,.22),**shell(2*math.pi,.035)}})
action('Shoreclaw_SwimForward',40,{1:{**swim(0,.52),**shell(0,.05)},10:{**swim(math.pi/2,.52),**shell(math.pi/2,.05)},20:{**swim(math.pi,.52),**shell(math.pi,.05)},30:{**swim(3*math.pi/2,.52),**shell(3*math.pi/2,.05)},40:{**swim(2*math.pi,.52),**shell(2*math.pi,.05)}})
# Transition poses blend both locomotion languages. Root is never translated; Valheim owns movement.
action('Shoreclaw_WaterExit',48,{1:{**swim(0,.34),'Body':(-.10,0,0)},14:{**swim(1.2,.24),'Body':(-.04,0,0),**shell(1.2,.06)},28:{**land(.7),'Body':(.08,0,0),**shell(2.2,.08)},40:{**land(2.1),'Body':(.03,0,0),**shell(3.4,.07)},48:{**land(math.pi)}})
action('Shoreclaw_WaterEntry',44,{1:{**land(0)},12:{**land(1.0),'Body':(-.02,0,0),**shell(1.0,.07)},24:{**swim(.8,.26),'Body':(-.08,0,0),**shell(2.0,.05)},34:{**swim(2.0,.42),'Body':(-.12,0,0),**shell(3.0,.04)},44:{**swim(math.pi,.46)}})
action('Shoreclaw_Hit',18,{1:N,6:{'Body':(.08,.10,-.18),'Claw_L_Arm':(.10,0,-.12),'Claw_R_Arm':(-.08,0,.14),**shell(.8,.12)},18:N})
action('Shoreclaw_Stagger',34,{1:N,10:{'Body':(.16,-.08,.24),**land(.8),**shell(1.0,.16)},20:{'Body':(-.10,.05,-.18),**land(2.2),**shell(2.4,.13)},34:N})
action('Shoreclaw_Death',72,{1:N,18:{'Body':(.18,0,.22),**shell(.7,.12)},42:{'Body':(.72,.12,.55),'Claw_L_Arm':(.32,0,-.24),'Claw_R_Arm':(-.28,0,.20),**shell(2.0,.18),**land(1.3)},72:{'Body':(1.18,.18,.72),'Claw_L_Arm':(.48,0,-.34),'Claw_R_Arm':(-.42,0,.28),**shell(3.1,.20),**land(2.8)}})

# Root translation is intentionally absent from every action.
for act in bpy.data.actions:
    if act.name.startswith('Shoreclaw_'):
        for fc in act.fcurves:
            if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'{act.name}: Root translation forbidden')
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'
arm['authored_actions']='Shoreclaw_Idle,Shoreclaw_Scuttle,Shoreclaw_TurnLeft,Shoreclaw_TurnRight,Shoreclaw_CrusherAttack,Shoreclaw_CutterAttack,Shoreclaw_Guard,Shoreclaw_SwimIdle,Shoreclaw_SwimForward,Shoreclaw_WaterExit,Shoreclaw_WaterEntry,Shoreclaw_Hit,Shoreclaw_Stagger,Shoreclaw_Death'
arm['animation_readability_contract']='eight-leg-land-weight-transfer;distinct-paddle-swim-strokes;independent-shell-lag;asymmetric-claw-attacks;blended-water-transitions;no-root-translation'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Shoreclaw r2: 14 amphibious actions; eight-leg scuttle/swim, shell lag, asymmetric claws, blended water transitions',flush=True)
