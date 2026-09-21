#!/usr/bin/env python3
"""Fail-fast production fidelity gate for the Blackwater Lamprey source and authored actions."""
from pathlib import Path
import bpy, math
ROOT=Path(__file__).resolve().parents[1]; MODEL=ROOT/'assets/models/source/underworld-creature-blackwater-lamprey.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='blackwater-lamprey': raise RuntimeError('Wrong asset identity')
if arm.get('host_rig')!='HOST-AQUATIC-FISH' or arm.get('production_contract')!='production-creature-r2': raise RuntimeError('Wrong production/host contract')
if not .7 <= float(arm.get('length_m',0)) <= 1.1: raise RuntimeError('Length outside 0.7-1.1m production band')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']; tris=0
for o in meshes:
    o.data.calc_loop_triangles(); tris+=len(o.data.loop_triangles)
    if not o.data.uv_layers.get('BlackwaterLampreyUV'): raise RuntimeError(f'Missing UV: {o.name}')
    if not any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers): raise RuntimeError(f'Missing armature binding: {o.name}')
if len(meshes)<35 or tris<8000: raise RuntimeError(f'Anatomy/fidelity regression meshes={len(meshes)} tris={tris}')
if sum(o.name.startswith('OralTooth_') for o in meshes)!=28: raise RuntimeError('Radial tooth geometry regression')
required_bones=['Head','Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2','MouthRing','Jaw','AttackOrigin','LatchSocket','MouthFX','HitCenter','TailTip']
for b in required_bones:
    if b not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {b}')
frames={'Lamprey_SwimIdle':72,'Lamprey_Cruise':48,'Lamprey_Sprint':24,'Lamprey_BankLeft':32,'Lamprey_BankRight':32,'Lamprey_Lunge':28,'Lamprey_Latch':22,'Lamprey_AttachedIdle':48,'Lamprey_Detach':26,'Lamprey_Hit':18,'Lamprey_Death':64}
for name,end in frames.items():
    a=bpy.data.actions.get(name)
    if not a or int(a.frame_end)!=end: raise RuntimeError(f'Missing/wrong action: {name}')
def values(action,bone):
    a=bpy.data.actions[action]; out=[]
    for fc in a.fcurves:
        if f'pose.bones["{bone}"]' in fc.data_path: out.extend(abs(k.co.y) for k in fc.keyframe_points)
    return out
def peak(action,bone):
    v=values(action,bone); return max(v) if v else 0
def keyed(action,bone): return bool(values(action,bone))
chain=['Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2']
for act in ['Lamprey_Cruise','Lamprey_Sprint']:
    if any(not keyed(act,b) for b in chain): raise RuntimeError(f'{act}: rigid chain segment')
    if peak(act,'Tail_2') <= peak(act,'Spine_1')*1.35: raise RuntimeError(f'{act}: propulsion does not build toward tail')
if peak('Lamprey_Sprint','Tail_2') <= peak('Lamprey_Cruise','Tail_2')*1.35: raise RuntimeError('Sprint lacks stronger caudal amplitude')
for act in ['Lamprey_Latch','Lamprey_AttachedIdle','Lamprey_Detach']:
    if not keyed(act,'MouthRing') or not keyed(act,'Jaw'): raise RuntimeError(f'{act}: oral disc does not articulate')
# Art contract deliberately forbids root translation as a fake latch implementation.
for act in ['Lamprey_Lunge','Lamprey_Latch','Lamprey_AttachedIdle','Lamprey_Detach']:
    a=bpy.data.actions[act]
    if any('pose.bones["Root"]' in fc.data_path and fc.data_path.endswith('location') for fc in a.fcurves): raise RuntimeError(f'{act}: fake root-translation attachment detected')
if peak('Lamprey_Latch','MouthRing')<.20 or peak('Lamprey_Latch','Jaw')<.25: raise RuntimeError('Latch sucker flare/compression too weak')
if any(not keyed('Lamprey_AttachedIdle',b) for b in chain): raise RuntimeError('Attached idle freezes posterior body')
if peak('Lamprey_Detach','MouthRing') <= peak('Lamprey_AttachedIdle','MouthRing')*1.5: raise RuntimeError('Detach lacks visible sucker release')
if peak('Lamprey_BankLeft','Spine_1')<.25 or peak('Lamprey_BankRight','Spine_1')<.25: raise RuntimeError('Bank body curvature too weak')
print(f'PASS Blackwater Lamprey production fidelity meshes={len(meshes)} tris={tris} actions={len(frames)}; latch lifecycle is articulated without root translation',flush=True)