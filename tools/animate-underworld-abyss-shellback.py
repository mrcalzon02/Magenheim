#!/usr/bin/env python3
"""Author Abyss Shellback amphibious production actions without replacing Valheim locomotion authority."""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-abyss-shellback.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}; run author-underworld-abyss-shellback.py first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='abyss-shellback' or arm.get('host_rig')!='HOST-AMPHIB-ARMORED': raise RuntimeError('Wrong Abyss Shellback model/host identity')

LEGS=[f'Leg_{s}_{n}' for s in ('L','R') for n in range(1,5)]
SHELLS=[f'ShellPlate_{i}' for i in range(1,6)]
N={}

def action(name,end,poses):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
    for pb in arm.pose.bones:
        pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0); pb.location=(0,0,0); pb.scale=(1,1,1)
    for frame,pose in poses.items():
        for bone,rot in pose.items():
            pb=arm.pose.bones.get(bone)
            if not pb: raise RuntimeError(f'{name}: missing bone {bone}')
            pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bone)
    act.frame_start=1; act.frame_end=end

def heavy_walk(phase,amp=.17):
    """Slow eight-leg weight transfer: short stride, deep planted stance, no world translation."""
    out={}
    for side in ('L','R'):
        sign=-1 if side=='L' else 1
        for n in range(1,5):
            p=phase+(n%2)*math.pi+(0 if side=='L' else math.pi)
            swing=math.sin(p); plant=max(0.0,-math.cos(p)); lift=max(0.0,math.cos(p))
            pre=f'Leg_{side}_{n}'
            out[pre+'_Upper']=(.07*lift,sign*.055*plant,amp*swing)
            out[pre+'_Lower']=(-.11*lift,sign*.035*plant,-.13*swing)
            out[pre+'_Foot']=(.10*lift,sign*.025*plant,-.08*swing)
    return out

def swim(phase,amp=.46):
    """Broad synchronized paddle strokes distinct from terrestrial load bearing."""
    out={}
    for side in ('L','R'):
        sign=-1 if side=='L' else 1
        for n in range(1,5):
            p=phase+(n-1)*.28+(0 if side=='L' else .16)
            stroke=math.sin(p); recover=math.cos(p); pre=f'Leg_{side}_{n}'
            out[pre+'_Upper']=(.08*recover,sign*.16*stroke,amp*stroke)
            out[pre+'_Lower']=(-.10*recover,sign*.10*stroke,-.31*stroke)
            out[pre+'_Foot']=(.62*stroke,sign*.08*recover,-.24*stroke)
    return out

def shell_lag(phase,amp=.045):
    return {b:(amp*math.sin(phase-i*.34),.018*math.sin(phase+i*.23),.014*math.cos(phase-i*.29)) for i,b in enumerate(SHELLS)}

def brace_shell(depth=.16):
    """Interlock five plates into a visibly lower fortress profile; center plates settle most."""
    return {b:(depth*(1.0-abs(i-2)*.11),0,(i-2)*.025) for i,b in enumerate(SHELLS)}

action('AbyssShellback_Idle',84,{1:{**shell_lag(0)},28:{**shell_lag(2.1),'Body':(.012,0,0)},56:{**shell_lag(4.2),'Body':(-.009,0,0)},84:{**shell_lag(2*math.pi)}})
action('AbyssShellback_HeavyWalk',48,{1:{**heavy_walk(0),**shell_lag(0,.055)},12:{**heavy_walk(math.pi/2),**shell_lag(math.pi/2,.055)},24:{**heavy_walk(math.pi),**shell_lag(math.pi,.055)},36:{**heavy_walk(3*math.pi/2),**shell_lag(3*math.pi/2,.055)},48:{**heavy_walk(2*math.pi),**shell_lag(2*math.pi,.055)}})
action('AbyssShellback_TurnLeft',36,{1:{**heavy_walk(0)},14:{**heavy_walk(1.3),'Body':(.025,0,-.13),'Claw_L_Arm':(0,0,-.09),'Claw_R_Arm':(0,0,.13),**shell_lag(1.3,.06)},26:{**heavy_walk(2.5),'Body':(.025,0,-.22),**shell_lag(2.5,.07)},36:{**heavy_walk(math.pi)}})
action('AbyssShellback_TurnRight',36,{1:{**heavy_walk(0)},14:{**heavy_walk(-1.3),'Body':(.025,0,.13),'Claw_L_Arm':(0,0,-.13),'Claw_R_Arm':(0,0,.09),**shell_lag(-1.3,.06)},26:{**heavy_walk(-2.5),'Body':(.025,0,.22),**shell_lag(-2.5,.07)},36:{**heavy_walk(-math.pi)}})
# Bilateral siege claws share mass, but attacks remain side-specific and readable.
action('AbyssShellback_ClawLeft',46,{1:N,14:{'Claw_L_Arm':(-.14,.10,-.30),'Claw_L_Palm':(.12,-.16,-.24),'Body':(.03,0,.07)},29:{'Claw_L_Arm':(.34,-.10,.48),'Claw_L_Palm':(-.26,.16,.40),'Body':(-.09,.02,-.14),**shell_lag(1.7,.08)},36:{'Claw_L_Arm':(.14,0,.18),'Claw_L_Palm':(-.10,.05,.14)},46:N})
action('AbyssShellback_ClawRight',46,{1:N,14:{'Claw_R_Arm':(-.14,-.10,.30),'Claw_R_Palm':(.12,.16,.24),'Body':(.03,0,-.07)},29:{'Claw_R_Arm':(.34,.10,-.48),'Claw_R_Palm':(-.26,-.16,-.40),'Body':(-.09,.02,.14),**shell_lag(1.7,.08)},36:{'Claw_R_Arm':(.14,0,-.18),'Claw_R_Palm':(-.10,-.05,-.14)},46:N})
# Brace lowers/interlocks armor; Guard adds bilateral claws over the exposed front/underside.
action('AbyssShellback_Brace',64,{1:N,18:{'Body':(.11,0,0),**brace_shell(.13),**heavy_walk(.4)},48:{'Body':(.15,0,0),**brace_shell(.19),**heavy_walk(.4)},64:N})
action('AbyssShellback_Guard',64,{1:N,18:{'Body':(.10,0,0),'Claw_L_Arm':(-.18,.16,-.27),'Claw_L_Palm':(.16,-.10,-.25),'Claw_R_Arm':(-.18,-.16,.27),'Claw_R_Palm':(.16,.10,.25),**brace_shell(.15)},48:{'Body':(.13,0,0),'Claw_L_Arm':(-.20,.18,-.30),'Claw_L_Palm':(.18,-.12,-.28),'Claw_R_Arm':(-.20,-.18,.30),'Claw_R_Palm':(.18,.12,.28),**brace_shell(.18)},64:N})
action('AbyssShellback_SwimIdle',72,{1:{**swim(0,.20),**shell_lag(0,.028)},18:{**swim(math.pi/2,.20),**shell_lag(math.pi/2,.028)},36:{**swim(math.pi,.20),**shell_lag(math.pi,.028)},54:{**swim(3*math.pi/2,.20),**shell_lag(3*math.pi/2,.028)},72:{**swim(2*math.pi,.20),**shell_lag(2*math.pi,.028)}})
action('AbyssShellback_SwimForward',48,{1:{**swim(0,.50),**shell_lag(0,.04)},12:{**swim(math.pi/2,.50),**shell_lag(math.pi/2,.04)},24:{**swim(math.pi,.50),**shell_lag(math.pi,.04)},36:{**swim(3*math.pi/2,.50),**shell_lag(3*math.pi/2,.04)},48:{**swim(2*math.pi,.50),**shell_lag(2*math.pi,.04)}})
action('AbyssShellback_WaterExit',58,{1:{**swim(0,.34),'Body':(-.09,0,0)},16:{**swim(1.1,.24),'Body':(-.04,0,0),**shell_lag(1.1,.04)},32:{**heavy_walk(.8),'Body':(.09,0,0),**shell_lag(2.1,.06)},48:{**heavy_walk(2.2),'Body':(.04,0,0),**shell_lag(3.5,.055)},58:{**heavy_walk(math.pi)}})
action('AbyssShellback_WaterEntry',54,{1:{**heavy_walk(0)},16:{**heavy_walk(1.0),'Body':(-.02,0,0),**shell_lag(1.0,.055)},30:{**swim(.9,.27),'Body':(-.08,0,0),**shell_lag(2.0,.04)},44:{**swim(2.1,.43),'Body':(-.11,0,0),**shell_lag(3.0,.03)},54:{**swim(math.pi,.46)}})
action('AbyssShellback_Hit',22,{1:N,7:{'Body':(.065,.08,-.12),'Claw_L_Arm':(.08,0,-.08),'Claw_R_Arm':(-.07,0,.09),**shell_lag(.8,.09)},22:N})
action('AbyssShellback_Stagger',42,{1:N,12:{'Body':(.12,-.06,.17),**heavy_walk(.7),**shell_lag(1.0,.12)},26:{'Body':(-.08,.04,-.14),**heavy_walk(2.1),**shell_lag(2.5,.10)},42:N})
action('AbyssShellback_Death',88,{1:N,24:{'Body':(.14,0,.16),**shell_lag(.8,.10)},52:{'Body':(.58,.10,.43),'Claw_L_Arm':(.28,0,-.20),'Claw_R_Arm':(-.26,0,.19),**shell_lag(2.0,.15),**heavy_walk(1.2)},88:{'Body':(1.02,.15,.64),'Claw_L_Arm':(.40,0,-.30),'Claw_R_Arm':(-.38,0,.28),**shell_lag(3.2,.18),**heavy_walk(2.7)}})

required=['Idle','HeavyWalk','TurnLeft','TurnRight','ClawLeft','ClawRight','Brace','Guard','SwimIdle','SwimForward','WaterExit','WaterEntry','Hit','Stagger','Death']
for suffix in required:
    if not bpy.data.actions.get('AbyssShellback_'+suffix): raise RuntimeError(f'Missing action: {suffix}')
for act in bpy.data.actions:
    if act.name.startswith('AbyssShellback_'):
        for fc in act.fcurves:
            if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'{act.name}: Root translation forbidden')
arm.animation_data.action=None
arm['production_contract']='production-creature-r2'
arm['authored_actions']=','.join('AbyssShellback_'+x for x in required)
arm['animation_readability_contract']='eight-leg-heavy-weight-transfer;five-plate-independent-lag;bilateral-heavy-claws;brace-shell-interlock;guard-underside-cover;distinct-paddle-swim-strokes;blended-water-transitions;no-root-translation'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(MODEL),compress=True)
print('AUTHORED Abyss Shellback r2: 15 actions; heavy land gait, five-plate fortress brace, bilateral claws, amphibious transitions',flush=True)
