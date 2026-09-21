#!/usr/bin/env python3
"""Production gate for Gloomfin model, rig and animation readability."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-gloomfin.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing production model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if not arm or arm.get('magenheim_asset')!='gloomfin': raise RuntimeError('Not a Gloomfin production source')
if arm.get('host_rig')!='HOST-AQUATIC-FISH': raise RuntimeError('Gloomfin must preserve HOST-AQUATIC-FISH')
length=float(arm.get('length_m',0));
if not 1.0 <= length <= 1.5: raise RuntimeError(f'Gloomfin scale regression: {length}m')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if len(meshes)<25 or tris<8000: raise RuntimeError(f'Gloomfin source fidelity regression meshes={len(meshes)} tris={tris}')
for o in meshes:
    if not o.data.uv_layers.get('GloomfinUV'): raise RuntimeError(f'Missing GloomfinUV: {o.name}')
    if not any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers): raise RuntimeError(f'Missing armature binding: {o.name}')
for name in ['Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2','Head','Jaw','Fin_L','Fin_R','AttackOrigin','MouthFX','HitCenter','TailTip']:
    if name not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {name}')
for name in ['DorsalFin_1','DorsalFin_2','DorsalFin_3','LowerJaw','CaudalFin']:
    if name not in bpy.data.objects: raise RuntimeError(f'Missing silhouette anatomy: {name}')
teeth=[o for o in meshes if o.name.startswith('Tooth')]
if len(teeth)!=16: raise RuntimeError(f'Expected 16 modeled teeth, found {len(teeth)}')

expected={'Gloomfin_SwimIdle':72,'Gloomfin_Cruise':48,'Gloomfin_Sprint':24,'Gloomfin_BankLeft':32,'Gloomfin_BankRight':32,'Gloomfin_Bite':26,'Gloomfin_Hit':18,'Gloomfin_Death':64}
for name,end in expected.items():
    a=bpy.data.actions.get(name)
    if not a: raise RuntimeError(f'Missing action: {name}')
    if int(a.frame_start)!=1 or int(a.frame_end)!=end: raise RuntimeError(f'{name}: frame contract regression {a.frame_start}-{a.frame_end}')

CHAIN=['Spine_1','Spine_2','Spine_3','Spine_4','Tail_1','Tail_2']
def curves(action,bone,index=None):
    found=[]
    for fc in action.fcurves:
        if f'pose.bones["{bone}"]' in fc.data_path and (index is None or fc.array_index==index): found.append(fc)
    return found
def peak(action,bone,index=2):
    cs=curves(action,bone,index)
    if not cs: return 0.0
    return max(abs(k.co[1]) for fc in cs for k in fc.keyframe_points)

cruise=bpy.data.actions['Gloomfin_Cruise']; sprint=bpy.data.actions['Gloomfin_Sprint']
for b in CHAIN:
    if peak(cruise,b)<.015: raise RuntimeError(f'Cruise rigid-chain regression: {b} lacks propulsion')
    if peak(sprint,b)<=peak(cruise,b)*1.20: raise RuntimeError(f'Sprint amplitude regression: {b} sprint={peak(sprint,b):.3f} cruise={peak(cruise,b):.3f}')
# Traveling wave must grow materially toward the caudal end instead of yawing as one block.
if peak(cruise,'Tail_2') <= peak(cruise,'Spine_1')*1.8: raise RuntimeError('Cruise lacks increasing caudal amplitude')
if sprint.frame_end >= cruise.frame_end*.75: raise RuntimeError('Sprint cadence is not materially faster than cruise')

left=bpy.data.actions['Gloomfin_BankLeft']; right=bpy.data.actions['Gloomfin_BankRight']
for a,label in [(left,'left'),(right,'right')]:
    if peak(a,'Fin_L',0)<.08 or peak(a,'Fin_R',0)<.08: raise RuntimeError(f'{label} bank lacks paired fin participation')
    if peak(a,'Spine_1',2)<.12 or peak(a,'Head',2)<.10: raise RuntimeError(f'{label} bank lacks body curvature')
# Banks must load opposite fins differently, not reuse a symmetric pose.
if abs(peak(left,'Fin_L',0)-peak(left,'Fin_R',0))<.10 or abs(peak(right,'Fin_L',0)-peak(right,'Fin_R',0))<.10: raise RuntimeError('Bank fin loading collapsed to symmetry')

bite=bpy.data.actions['Gloomfin_Bite']
if peak(bite,'Jaw',0)<.45: raise RuntimeError('Bite is not visibly jaw-led')
if peak(bite,'Tail_2',2)<.12 or peak(bite,'Head',0)<.10: raise RuntimeError('Bite lacks supporting body impulse')
if arm.get('animation_readability_contract')!='traveling-six-stage-propulsion;sprint-amplitude-cadence;asymmetric-fin-banks;jaw-led-bite': raise RuntimeError('Animation readability contract missing/regressed')
print(f'PASS Gloomfin production r2: length={length:.2f}m meshes={len(meshes)} tris={tris}; 8 actions; traveling propulsion, sprint cadence, asymmetric banks and jaw bite gated',flush=True)
