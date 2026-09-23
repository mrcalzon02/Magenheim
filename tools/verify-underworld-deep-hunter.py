#!/usr/bin/env python3
"""Verify Deep Hunter source anatomy and apex-predator animation fidelity in Blender.

Art acceptance only. Valheim remains authoritative for world-space locomotion,
navigation, AI, networking, persistence, damage and gameplay state.
"""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-deep-hunter.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing Deep Hunter model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='deep-hunter': raise RuntimeError('Deep Hunter armature identity missing')
if arm.get('host_rig')!='HOST-AQUATIC-FISH-APEX': raise RuntimeError(f"Wrong host rig: {arm.get('host_rig')!r}")
if not 6.0 <= float(arm.get('length_m',0)) <= 9.0: raise RuntimeError('Deep Hunter outside 6-9 m apex production band')

CHAIN=['Spine_1','Spine_2','Spine_3','Spine_4','Spine_5','Tail_1','Tail_2']
REQUIRED=['Root',*CHAIN,'Head','Jaw','Fin_L','Fin_R','AttackOrigin','BreachCenter','HitCenter','TailTip','MouthFX']
for name in REQUIRED:
    if name not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {name}')
if int(arm.get('body_tail_deformation_bones',0))!=7: raise RuntimeError('Deep Hunter must retain seven body/tail deformation stages')
if int(arm.get('modeled_teeth',0))!=40: raise RuntimeError('Deep Hunter modeled-teeth contract != 40')

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<58: raise RuntimeError(f'Deep Hunter anatomy too sparse: {len(meshes)} meshes < 58')
triangles=sum(sum(max(0,len(p.vertices)-2) for p in o.data.polygons) for o in meshes)
if triangles<30000: raise RuntimeError(f'Deep Hunter source fidelity too low: {triangles} triangles < 30000')
teeth=[o for o in meshes if o.name.startswith('Tooth')]
if len(teeth)!=40: raise RuntimeError(f'Deep Hunter physical tooth count regression: {len(teeth)} != 40')
if any(not o.data.uv_layers.get('DeepHunterUV') for o in meshes): raise RuntimeError(f'Meshes missing DeepHunterUV: {[o.name for o in meshes if not o.data.uv_layers.get("DeepHunterUV")][:8]}')
bound=sum(1 for o in meshes if any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers))
if bound<len(meshes): raise RuntimeError(f'Unbound Deep Hunter anatomy: {bound}/{len(meshes)} meshes armature-bound')

required_actions={'SwimIdle':64,'Cruise':48,'Sprint':32,'TurnLeft':36,'TurnRight':36,'Bite':34,'Ram':52,'Breach':46,'BreachRecover':42,'TailStrike':44,'Hit':22,'Stagger':38,'Death':88}
for suffix,end in required_actions.items():
    name='DeepHunter_'+suffix; act=bpy.data.actions.get(name)
    if not act: raise RuntimeError(f'Missing Deep Hunter action: {name}')
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

def values(act,bone,path='rotation_euler',axis=None):
    token=f'pose.bones["{bone}"].{path}'; out=[]
    for fc in fcurves(act):
        if token in fc.data_path and (axis is None or fc.array_index==axis): out += [kp.co.y for kp in fc.keyframe_points]
    return out

def span(act,bone,path='rotation_euler',axis=None):
    v=values(act,bone,path,axis); return max(v)-min(v) if v else 0.0

def peak(act,bone,path='rotation_euler',axis=None):
    v=values(act,bone,path,axis); return max((abs(x) for x in v),default=0.0)

for suffix in required_actions:
    act=bpy.data.actions['DeepHunter_'+suffix]
    for fc in fcurves(act):
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'{act.name}: Root translation forbidden')

# Propulsion must be a seven-stage traveling wave with strongly increasing caudal authority.
for suffix in ('Cruise','Sprint'):
    act=bpy.data.actions['DeepHunter_'+suffix]
    z=[span(act,b,axis=2) for b in CHAIN]
    if any(v<0.045 for v in z): raise RuntimeError(f'{act.name}: rigid/under-driven deformation stage: {z}')
    if z[-1] < z[0]*5.0: raise RuntimeError(f'{act.name}: insufficient caudal amplitude growth: {z[0]:.3f}->{z[-1]:.3f}')
    if sum(1 for a,b in zip(z,z[1:]) if b>a) < 5: raise RuntimeError(f'{act.name}: deformation does not progressively strengthen caudally: {z}')
cruise=bpy.data.actions['DeepHunter_Cruise']; sprint=bpy.data.actions['DeepHunter_Sprint']
if span(sprint,'Tail_2',axis=2) < span(cruise,'Tail_2',axis=2)*1.35: raise RuntimeError('Sprint propulsion insufficiently distinct from Cruise')

# Large-bank turns must commit head/body in opposite directions and use asymmetric fins.
left=bpy.data.actions['DeepHunter_TurnLeft']; right=bpy.data.actions['DeepHunter_TurnRight']
lz=values(left,'Head',axis=2); rz=values(right,'Head',axis=2)
if not lz or min(lz)>-0.20: raise RuntimeError('TurnLeft lacks committed negative head bank')
if not rz or max(rz)<0.20: raise RuntimeError('TurnRight lacks committed positive head bank')
for act in (left,right):
    if peak(act,'Fin_L')<0.20 or peak(act,'Fin_R')<0.20: raise RuntimeError(f'{act.name}: both stabilizers must materially articulate')

bite=bpy.data.actions['DeepHunter_Bite']
if peak(bite,'Jaw',axis=0)<0.65: raise RuntimeError('Bite jaw gape below physical apex-predator threshold')
if peak(bite,'Head',axis=0)<0.12 or peak(bite,'Spine_1')<0.08: raise RuntimeError('Bite lacks head/front-body strike commitment')

# Ram recoil must propagate through the entire chain; a head-only shove is unacceptable.
ram=bpy.data.actions['DeepHunter_Ram']
for bone in CHAIN:
    if peak(ram,bone)<0.035: raise RuntimeError(f'Ram recoil fails to reach {bone}')
if peak(ram,'Tail_2',axis=2)<0.35: raise RuntimeError('Ram lacks terminal caudal recoil')

# Tail strike must visibly wind and reverse through every intermediate deformation stage.
tail=bpy.data.actions['DeepHunter_TailStrike']
for bone in CHAIN:
    v=values(tail,bone,axis=2)
    if not v or min(v)>=0 or max(v)<=0: raise RuntimeError(f'TailStrike does not reverse through {bone}')
if peak(tail,'Tail_2',axis=2)<1.0: raise RuntimeError('TailStrike terminal whip below hero-predator silhouette threshold')

# Breach pair must deform multiple body stages and fins; trajectory itself remains gameplay-owned.
for suffix in ('Breach','BreachRecover'):
    act=bpy.data.actions['DeepHunter_'+suffix]
    active=sum(1 for b in CHAIN if peak(act,b)>=0.10)
    if active<5: raise RuntimeError(f'{act.name}: whole-body breach curvature too weak ({active}/7 stages >= 0.10 rad)')
    if peak(act,'Fin_L')<0.20 or peak(act,'Fin_R')<0.20: raise RuntimeError(f'{act.name}: fins do not materially respond to breach')

contract=arm.get('animation_readability_contract','')
for token in ('seven-stage-traveling-wave','increasing-caudal-amplitude','sprint-distinct-from-cruise','large-bank-turns','physical-jaw-bite','full-body-ram','local-deformation-breach-recovery','propagating-tail-strike','no-root-translation'):
    if token not in contract: raise RuntimeError(f'Missing animation readability contract token: {token}')

print(f'VERIFIED Deep Hunter production fidelity: meshes={len(meshes)} triangles={triangles} bound={bound} teeth={len(teeth)} actions={len(required_actions)}; seven-stage propulsion, distinct sprint, opposed banking, physical bite, full-chain ram/tail strike, breach deformation, no Root translation',flush=True)
