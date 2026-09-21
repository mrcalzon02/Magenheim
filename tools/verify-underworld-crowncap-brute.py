#!/usr/bin/env python3
"""Production acceptance gate for the Fungal Forest Crowncap Brute source asset."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-crowncap-brute.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing {MODEL}; run Crowncap Brute author and animation passes first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); sc=bpy.context.scene
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one Crowncap Brute armature, found {len(arms)}')
arm=arms[0]
if arm.get('magenheim_asset')!='crowncap-brute': raise RuntimeError('Wrong Crowncap Brute model identity')
if arm.get('host_rig')!='HOST-BIPED-MASS': raise RuntimeError('Crowncap Brute host-rig regression')
if arm.get('production_contract')!='production-creature-r2': raise RuntimeError('Crowncap Brute animation/fidelity pass not applied')
height=float(arm.get('height_m',0))
if not 2.8<=height<=3.4: raise RuntimeError(f'Crowncap Brute height outside design range: {height}')
meshes=[o for o in sc.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<30 or tris<14000: raise RuntimeError(f'Crowncap Brute source fidelity regression: {len(meshes)} meshes / {tris} tris')
for o in meshes:
 if not o.data.uv_layers.get('CrowncapBruteUV'): raise RuntimeError(f'{o.name}: missing explicit CrowncapBruteUV')
 if not any(m.type=='ARMATURE' for m in o.modifiers): raise RuntimeError(f'{o.name}: missing armature binding')
required={'Root','Pelvis','Spine1','Chest','Neck','Head','Jaw','AttackOrigin','HitCenter','GillFX'}
for side in ('L','R'): required|={f'{j}_{side}' for j in ('UpperArm','ForeArm','Hand','Thigh','Shin','Foot')}
missing=required-{b.name for b in arm.data.bones}
if missing: raise RuntimeError('Missing Crowncap Brute bones: '+','.join(sorted(missing)))
if len([o for o in meshes if o.name.startswith('CrownPlate_')])!=4: raise RuntimeError('Crowncap Brute requires four modeled crown layers')
if len([o for o in meshes if o.name.startswith('GillCurtain_')])!=5: raise RuntimeError('Crowncap Brute requires five exposed gill curtains')
if len([o for o in meshes if o.name.startswith('ShoulderPlate_')])!=2: raise RuntimeError('Crowncap Brute shoulder armor geometry incomplete')
expected={'CrowncapBrute_Idle':72,'CrowncapBrute_Walk':48,'CrowncapBrute_Turn':34,'CrowncapBrute_Alert':40,'CrowncapBrute_SweepLeft':38,'CrowncapBrute_SweepRight':38,'CrowncapBrute_HeavySlam':46,'CrowncapBrute_Hit':18,'CrowncapBrute_HeavyStagger':54,'CrowncapBrute_Death':72}

def curves(a):
 legacy=getattr(a,'fcurves',None)
 if legacy is not None:return list(legacy)
 out=[]
 for layer in getattr(a,'layers',()):
  for strip in getattr(layer,'strips',()):
   for bag in getattr(strip,'channelbags',()):out.extend(bag.fcurves)
 return out

def paths(name): return {fc.data_path for fc in curves(bpy.data.actions[name])}
def keyed_abs(name,bone):
 vals=[]
 token=f'pose.bones["{bone}"]'
 for fc in curves(bpy.data.actions[name]):
  if token in fc.data_path:
   vals.extend(abs(k.co[1]) for k in fc.keyframe_points)
 return max(vals) if vals else 0.0
for name,end in expected.items():
 a=bpy.data.actions.get(name)
 if not a: raise RuntimeError(f'Missing Crowncap Brute action {name}')
 if int(round(a.frame_end))!=end: raise RuntimeError(f'{name}: expected frame end {end}, got {a.frame_end}')
 if not curves(a): raise RuntimeError(f'{name}: no readable curves')
# Walk must remain a planted full-body mass gait, not sliding legs beneath a rigid torso.
walk=paths('CrowncapBrute_Walk')
for bone in ('Pelvis','Chest','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R','UpperArm_L','UpperArm_R'):
 if not any(f'pose.bones["{bone}"]' in p for p in walk): raise RuntimeError(f'Walk leaves {bone} rigid')
# Sweeps must use attacking arm plus pelvis/chest and opposite-side brace leg.
for side,brace in (('Left','R'),('Right','L')):
 name=f'CrowncapBrute_Sweep{side}'; p=paths(name); atk='L' if side=='Left' else 'R'
 for bone in ('Pelvis','Spine1','Chest',f'UpperArm_{atk}',f'ForeArm_{atk}',f'Thigh_{brace}',f'Shin_{brace}'):
  if not any(f'pose.bones["{bone}"]' in x for x in p): raise RuntimeError(f'{name} lacks weighted participation from {bone}')
 if keyed_abs(name,f'UpperArm_{atk}')<0.45 or keyed_abs(name,'Chest')<0.30: raise RuntimeError(f'{name} sweep amplitude too weak for elite ground control')
# Slam needs a readable whole-mass anticipation and both massive arm chains.
slam=paths('CrowncapBrute_HeavySlam')
for bone in ('Pelvis','Spine1','Chest','Head','UpperArm_L','ForeArm_L','UpperArm_R','ForeArm_R','Thigh_L','Shin_L','Thigh_R','Shin_R'):
 if not any(f'pose.bones["{bone}"]' in p for p in slam): raise RuntimeError(f'Heavy slam leaves {bone} out of anticipation/impact')
if keyed_abs('CrowncapBrute_HeavySlam','Chest')<0.45 or keyed_abs('CrowncapBrute_HeavySlam','ForeArm_L')<0.75 or keyed_abs('CrowncapBrute_HeavySlam','ForeArm_R')<0.75: raise RuntimeError('Heavy slam lacks production-scale compression/impact amplitude')
# Heavy stagger must peel crown/head/chest backward enough to expose the under-cap gill curtains.
stagger=paths('CrowncapBrute_HeavyStagger')
for bone in ('Spine1','Chest','Neck','Head','UpperArm_L','UpperArm_R'):
 if not any(f'pose.bones["{bone}"]' in p for p in stagger): raise RuntimeError(f'Heavy stagger does not articulate {bone}')
if keyed_abs('CrowncapBrute_HeavyStagger','Chest')<0.40 or keyed_abs('CrowncapBrute_HeavyStagger','Head')<0.35 or keyed_abs('CrowncapBrute_HeavyStagger','Neck')<0.28: raise RuntimeError('Heavy stagger insufficient to expose under-cap gills')
contract='asymmetric-sweep-weight-transfer;slam-full-mass-anticipation;heavy-stagger-exposes-under-cap-gills'
if arm.get('animation_readability_contract')!=contract: raise RuntimeError('Missing Crowncap Brute animation readability contract')
print(f'PASS Crowncap Brute r2: {len(meshes)} meshes / {tris} tris / 10 actions / weighted sweeps, slam anticipation, gill-exposing stagger',flush=True)
