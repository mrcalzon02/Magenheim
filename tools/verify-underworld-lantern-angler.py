#!/usr/bin/env python3
"""Verify Lantern Angler source anatomy and animation/deformation fidelity in Blender.

Art acceptance only. Valheim remains authoritative for swimming, navigation, AI,
networking, persistence and world-space movement.
"""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-lantern-angler.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing Lantern Angler model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='lantern-angler': raise RuntimeError('Lantern Angler armature identity missing')
if arm.get('host_rig')!='HOST-AQUATIC-FISH': raise RuntimeError(f"Wrong host rig: {arm.get('host_rig')!r}")
if not 1.8 <= float(arm.get('length_m',0)) <= 2.4: raise RuntimeError('Lantern Angler scale contract violated')

required_bones=['Root','Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2','Head','Jaw','Throat','Fin_L','Fin_R','Lure_1','Lure_2','Lure_3','AttackOrigin','MouthFX','PressureFX','LureFX','HitCenter','TailTip']
for b in required_bones:
    if b not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {b}')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<35: raise RuntimeError(f'Anatomy too sparse: {len(meshes)} meshes < 35')
tris=sum(sum(max(0,len(p.vertices)-2) for p in o.data.polygons) for o in meshes)
if tris<12000: raise RuntimeError(f'Source fidelity too low: {tris} triangles < 12000')
teeth=[o for o in meshes if o.name.startswith('Tooth')]
if len(teeth)!=20: raise RuntimeError(f'Expected 20 physical teeth, found {len(teeth)}')
if any(not o.data.uv_layers.get('LanternAnglerUV') for o in meshes): raise RuntimeError('One or more meshes lack LanternAnglerUV')
bound=sum(any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers) for o in meshes)
if bound<len(meshes): raise RuntimeError(f'Loose/unbound anatomy detected: {bound}/{len(meshes)} meshes armature-bound')

required_actions={'LanternAngler_SwimIdle':40,'LanternAngler_Cruise':40,'LanternAngler_BankLeft':30,'LanternAngler_BankRight':30,'LanternAngler_LureIdle':80,'LanternAngler_LureTell':44,'LanternAngler_Bite':34,'LanternAngler_PressureTell':58,'LanternAngler_PressureRelease':30,'LanternAngler_Hit':20,'LanternAngler_Death':72}
for name,end in required_actions.items():
    a=bpy.data.actions.get(name)
    if not a: raise RuntimeError(f'Missing Lantern Angler action: {name}')
    if int(round(a.frame_end))!=end: raise RuntimeError(f'{name}: frame contract {a.frame_end} != {end}')

def fcurves(a):
    direct=getattr(a,'fcurves',None)
    if direct is not None: return list(direct)
    out=[]
    for layer in getattr(a,'layers',[]):
        for strip in getattr(layer,'strips',[]):
            bag=getattr(strip,'channelbag',None)
            if bag: out.extend(list(bag.fcurves))
            for bag in getattr(strip,'channelbags',[]): out.extend(list(bag.fcurves))
    return out

def values(action,bone,prop=None,index=None):
    token=f'pose.bones["{bone}"]'
    out=[]
    for fc in fcurves(bpy.data.actions[action]):
        if token not in fc.data_path: continue
        if prop and not fc.data_path.endswith('.'+prop): continue
        if index is not None and fc.array_index!=index: continue
        out.extend(k.co.y for k in fc.keyframe_points)
    return out

def span(action,bone,prop=None,index=None):
    v=values(action,bone,prop,index); return max(v)-min(v) if v else 0.0

def vmax(action,bone,prop,index):
    v=values(action,bone,prop,index); return max(v) if v else None

def vmin(action,bone,prop,index):
    v=values(action,bone,prop,index); return min(v) if v else None

# Never steal world locomotion from Valheim.
for name in required_actions:
    for fc in fcurves(bpy.data.actions[name]):
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'{name}: Root translation forbidden')

# Cruise must be a traveling caudal wave whose lateral amplitude grows toward the tail.
chain=['Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2']
caudal=[span('LanternAngler_Cruise',b,'rotation_euler',2) for b in chain]
if min(caudal)<0.07: raise RuntimeError(f'Cruise propulsion under-animates body chain: {caudal}')
for a,b in zip(caudal,caudal[1:]):
    if b <= a*1.12: raise RuntimeError(f'Cruise lacks increasing caudal amplitude: {caudal}')
if caudal[-1] < caudal[0]*4.5: raise RuntimeError(f'Tail propulsion insufficiently distinct from head: {caudal}')

# Lure must behave as a flexible three-stage organ, and the tell must exceed ambient drift.
idle=[span('LanternAngler_LureIdle',f'Lure_{i}','rotation_euler') for i in range(1,4)]
tell=[span('LanternAngler_LureTell',f'Lure_{i}','rotation_euler') for i in range(1,4)]
if not (idle[1]>idle[0]*1.20 and idle[2]>idle[1]*1.15): raise RuntimeError(f'Lure idle reads as rigid stalk: {idle}')
if tell[2] < idle[2]*1.65 or tell[2] < 0.75: raise RuntimeError(f'Lure tell insufficiently distinct from idle: idle={idle} tell={tell}')
if max(tell)-min(tell)<0.20: raise RuntimeError(f'Lure tell segments collapse into rigid motion: {tell}')

# Bite must physically articulate both jaw and throat rather than rely on VFX.
if span('LanternAngler_Bite','Jaw','rotation_euler',0)<0.55: raise RuntimeError('Bite jaw opening too weak')
if (vmax('LanternAngler_Bite','Throat','scale',2) or 0)<1.24: raise RuntimeError('Bite lacks visible throat expansion')

# Pressure tell/release must have a large silhouette change and a fast compression overshoot.
tell_z=vmax('LanternAngler_PressureTell','Throat','scale',2) or 0
release_min=vmin('LanternAngler_PressureRelease','Throat','scale',2)
if tell_z<1.60: raise RuntimeError(f'Pressure tell throat inflation too weak: {tell_z:.3f}')
if release_min is None or release_min>0.78: raise RuntimeError(f'Pressure release lacks compression overshoot: {release_min}')
if tell_z-release_min<0.80: raise RuntimeError('Pressure inflate/release silhouette contrast too weak')

# Banking must visibly use opposed pectoral fins and opposite body signs.
for name in ('LanternAngler_BankLeft','LanternAngler_BankRight'):
    if span(name,'Fin_L','rotation_euler')<0.30 or span(name,'Fin_R','rotation_euler')<0.25: raise RuntimeError(f'{name}: pectoral banking under-authored')
left=values('LanternAngler_BankLeft','Spine_1','rotation_euler',2); right=values('LanternAngler_BankRight','Spine_1','rotation_euler',2)
if not left or not right or min(left)>=0 or max(right)<=0: raise RuntimeError('Bank actions do not establish opposite body-direction signs')

contract=arm.get('animation_readability_contract','')
for token in ('traveling-caudal-wave','three-stage-lure-lag','physical-lure-tell','jaw-throat-bite','throat-pressure-inflate-release','bank-fin-asymmetry','no-root-translation'):
    if token not in contract: raise RuntimeError(f'Missing animation readability contract token: {token}')
print(f'VERIFIED Lantern Angler production fidelity: meshes={len(meshes)} triangles={tris} bound={bound} actions={len(required_actions)}; caudal wave={caudal}; lure idle={idle}; lure tell={tell}; pressure={tell_z:.2f}->{release_min:.2f}; no Root translation',flush=True)
