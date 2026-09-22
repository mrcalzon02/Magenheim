#!/usr/bin/env python3
"""Verify Abyss Shellback source anatomy and amphibious animation fidelity in Blender.

Art acceptance only: Valheim remains authoritative for world locomotion, swimming,
AI, networking, persistence, damage and gameplay state.
"""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-abyss-shellback.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing Abyss Shellback model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='abyss-shellback': raise RuntimeError('Abyss Shellback armature identity missing')
if arm.get('host_rig')!='HOST-AMPHIB-ARMORED': raise RuntimeError(f"Wrong host rig: {arm.get('host_rig')!r}")

LEGS=[f'Leg_{s}_{n}' for s in ('L','R') for n in range(1,5)]
SHELLS=[f'ShellPlate_{i}' for i in range(1,6)]
REQUIRED=['Root','Body',*SHELLS,'Claw_L_Arm','Claw_L_Palm','Claw_R_Arm','Claw_R_Palm','AttackOrigin','HitCenter','SwimCenter','ShellFX','UndersideFX']
for leg in LEGS: REQUIRED += [leg+'_Upper',leg+'_Lower',leg+'_Foot']
for name in REQUIRED:
    if name not in arm.data.bones: raise RuntimeError(f'Missing production bone: {name}')

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<55: raise RuntimeError(f'Abyss Shellback anatomy too sparse: {len(meshes)} meshes < 55')
triangles=sum(sum(max(0,len(p.vertices)-2) for p in o.data.polygons) for o in meshes)
if triangles<18000: raise RuntimeError(f'Abyss Shellback source fidelity too low: {triangles} triangles < 18000')
if any(not o.data.uv_layers for o in meshes): raise RuntimeError(f'Meshes missing UVs: {[o.name for o in meshes if not o.data.uv_layers][:8]}')
bound=sum(1 for o in meshes if any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers))
if bound<55: raise RuntimeError(f'Insufficient armature-bound anatomy: {bound}/55 minimum')

required_actions={'Idle':84,'HeavyWalk':48,'TurnLeft':36,'TurnRight':36,'ClawLeft':46,'ClawRight':46,'Brace':64,'Guard':64,'SwimIdle':72,'SwimForward':48,'WaterExit':58,'WaterEntry':54,'Hit':22,'Stagger':42,'Death':88}
for suffix,end in required_actions.items():
    name='AbyssShellback_'+suffix; act=bpy.data.actions.get(name)
    if not act: raise RuntimeError(f'Missing Abyss Shellback action: {name}')
    if int(round(act.frame_end))!=end: raise RuntimeError(f'{name}: frame contract {act.frame_end} != {end}')

def fcurves(act):
    direct=getattr(act,'fcurves',None)
    if direct is not None: return list(direct)
    curves=[]
    for layer in getattr(act,'layers',[]):
        for strip in getattr(layer,'strips',[]):
            bag=getattr(strip,'channelbag',None)
            if bag: curves.extend(list(bag.fcurves))
            for bag in getattr(strip,'channelbags',[]): curves.extend(list(bag.fcurves))
    return curves

def axis_values(act,bone,path='rotation_euler',axis=None):
    token=f'pose.bones["{bone}"].{path}'; vals=[]
    for fc in fcurves(act):
        if token in fc.data_path and (axis is None or fc.array_index==axis): vals += [kp.co.y for kp in fc.keyframe_points]
    return vals

def span(act,bone,path='rotation_euler',axis=None):
    vals=axis_values(act,bone,path,axis); return max(vals)-min(vals) if vals else 0.0

for suffix in required_actions:
    act=bpy.data.actions['AbyssShellback_'+suffix]
    for fc in fcurves(act):
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'{act.name}: Root translation forbidden')

walk=bpy.data.actions['AbyssShellback_HeavyWalk']; swim=bpy.data.actions['AbyssShellback_SwimForward']
for leg in LEGS:
    for suffix in ('_Upper','_Lower','_Foot'):
        if span(walk,leg+suffix)<0.10: raise RuntimeError(f'HeavyWalk under-animates {leg+suffix}')
    land=span(walk,leg+'_Foot',axis=0); water=span(swim,leg+'_Foot',axis=0)
    if water<0.85: raise RuntimeError(f'Swim paddle articulation too weak on {leg}: {water:.3f}')
    if water<land*2.5: raise RuntimeError(f'{leg}: swim stroke not materially distinct from heavy walk ({water:.3f} vs {land:.3f})')

for action_name in ('AbyssShellback_HeavyWalk','AbyssShellback_SwimForward'):
    act=bpy.data.actions[action_name]; signatures=[]
    for shell in SHELLS:
        s=span(act,shell)
        if s<0.025: raise RuntimeError(f'{action_name}: rigid shell plate {shell} ({s:.3f})')
        signatures.append(tuple(round(span(act,shell,axis=i),4) for i in range(3)))
    if len(set(signatures))<3: raise RuntimeError(f'{action_name}: five shell plates collapse toward rigid-slab motion')

brace=bpy.data.actions['AbyssShellback_Brace']
for shell in SHELLS:
    if span(brace,shell,axis=0)<0.10: raise RuntimeError(f'Brace does not materially settle {shell}')
center=span(brace,'ShellPlate_3',axis=0); edge=min(span(brace,'ShellPlate_1',axis=0),span(brace,'ShellPlate_5',axis=0))
if center<=edge: raise RuntimeError('Brace lacks center-weighted fortress interlock')

guard=bpy.data.actions['AbyssShellback_Guard']
for bone in ('Claw_L_Arm','Claw_L_Palm','Claw_R_Arm','Claw_R_Palm'):
    if span(guard,bone)<0.22: raise RuntimeError(f'Guard does not materially cover underside with {bone}')
for shell in SHELLS:
    if span(guard,shell,axis=0)<0.10: raise RuntimeError(f'Guard lacks shell closure on {shell}')

for side in ('Left','Right'):
    act=bpy.data.actions['AbyssShellback_Claw'+side]; prefix='Claw_L_' if side=='Left' else 'Claw_R_'
    if span(act,prefix+'Arm')<0.60 or span(act,prefix+'Palm')<0.50: raise RuntimeError(f'{side} siege-claw attack lacks heavy articulation')

for action_name in ('AbyssShellback_WaterEntry','AbyssShellback_WaterExit'):
    act=bpy.data.actions[action_name]
    for leg in LEGS:
        if span(act,leg+'_Foot')<0.35: raise RuntimeError(f'{action_name}: {leg} lacks land/water foot-paddle transition')
    for shell in SHELLS:
        if span(act,shell)<0.02: raise RuntimeError(f'{action_name}: {shell} lacks transition response')

contract=arm.get('animation_readability_contract','')
for token in ('eight-leg-heavy-weight-transfer','five-plate-independent-lag','bilateral-heavy-claws','brace-shell-interlock','guard-underside-cover','distinct-paddle-swim-strokes','blended-water-transitions','no-root-translation'):
    if token not in contract: raise RuntimeError(f'Missing animation readability contract token: {token}')

print(f'VERIFIED Abyss Shellback production fidelity: meshes={len(meshes)} triangles={triangles} bound={bound} actions={len(required_actions)}; heavy eight-leg gait, five-plate independence/interlock, guard coverage, bilateral claws, distinct swim/transition language, no Root translation',flush=True)
