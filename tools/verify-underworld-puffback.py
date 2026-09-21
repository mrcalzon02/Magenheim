#!/usr/bin/env python3
"""Production acceptance gate for the Fungal Forest Puffback source asset."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-puffback.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing {MODEL}; run Puffback author and animation passes first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-puffback': raise RuntimeError('Wrong Puffback model identity')
if sc.get('magenheim_host_rig')!='HOST-QUADRUPED': raise RuntimeError('Puffback host-rig regression')
if sc.get('magenheim_fidelity')!='production-creature-r2': raise RuntimeError('Puffback animation/fidelity pass not applied')
h=float(sc.get('magenheim_shoulder_height_m',0));
if not 1.6<=h<=1.9: raise RuntimeError(f'Puffback shoulder height outside design range: {h}')
meshes=[o for o in sc.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<30 or tris<10000: raise RuntimeError(f'Puffback source fidelity regression: {len(meshes)} meshes / {tris} tris')
for o in meshes:
 if not o.data.uv_layers.get('PuffbackUV'): raise RuntimeError(f'{o.name}: missing explicit PuffbackUV')
 if not any(m.type=='ARMATURE' for m in o.modifiers): raise RuntimeError(f'{o.name}: missing armature binding')
arm=[o for o in sc.objects if o.type=='ARMATURE']
if len(arm)!=1: raise RuntimeError(f'Expected one Puffback armature, found {len(arm)}')
arm=arm[0]
required={'Root','Pelvis','Spine_1','Spine_2','Neck','Head','Bladder_Main','AttackOrigin','HitCenter','SporeFX'}
for limb in ('Front_L','Front_R','Rear_L','Rear_R'): required|={f'{limb}_Upper',f'{limb}_Lower',f'{limb}_Foot'}
missing=required-{b.name for b in arm.data.bones}
if missing: raise RuntimeError('Missing Puffback bones: '+','.join(sorted(missing)))
if len([o for o in meshes if o.name.startswith('Puffback_Bladder_')])<4: raise RuntimeError('Puffback bladder silhouette geometry incomplete')
if len([o for o in meshes if o.name.startswith('Puffback_Vent_')])!=6: raise RuntimeError('Puffback requires six modeled discharge vents')
expected={'Puffback_GrazeRoot':78,'Puffback_Idle':72,'Puffback_Walk':36,'Puffback_WarningDisplay':48,'Puffback_Charge':30,'Puffback_DefensiveInflate':44,'Puffback_SporePuff':34,'Puffback_Hit':18,'Puffback_Stagger':32,'Puffback_Death':62}

def curves(a):
 legacy=getattr(a,'fcurves',None)
 if legacy is not None:return list(legacy)
 out=[]
 for layer in getattr(a,'layers',()):
  for strip in getattr(layer,'strips',()):
   for bag in getattr(strip,'channelbags',()):out.extend(bag.fcurves)
 return out
for name,end in expected.items():
 a=bpy.data.actions.get(name)
 if not a: raise RuntimeError(f'Missing Puffback action {name}')
 if int(round(a.frame_end))!=end: raise RuntimeError(f'{name}: expected frame end {end}, got {a.frame_end}')
 if not curves(a): raise RuntimeError(f'{name}: no readable curves')
walk=bpy.data.actions['Puffback_Walk']; walk_paths={fc.data_path for fc in curves(walk)}
for limb in ('Front_L','Front_R','Rear_L','Rear_R'):
 for joint in ('Upper','Lower','Foot'):
  token=f'pose.bones["{limb}_{joint}"]'
  if not any(token in p for p in walk_paths): raise RuntimeError(f'Puffback walk leaves {limb}_{joint} rigid')

def scale_values(action_name):
 vals=[]
 for fc in curves(bpy.data.actions[action_name]):
  if 'pose.bones["Bladder_Main"]' in fc.data_path and fc.data_path.endswith('scale'):
   vals.extend(k.co.y for k in fc.keyframe_points)
 return vals
inflate=scale_values('Puffback_DefensiveInflate'); puff=scale_values('Puffback_SporePuff')
if not inflate or max(inflate)<1.28 or min(inflate)>.90: raise RuntimeError('Defensive inflation lacks readable bladder compression/expansion')
if not puff or max(puff)<1.40 or min(puff)>.95: raise RuntimeError('Spore puff lacks pre-release expansion and discharge collapse')
warn=scale_values('Puffback_WarningDisplay')
if not warn or max(warn)<1.15: raise RuntimeError('Warning display does not visibly raise bladder profile')
if sc.get('magenheim_inflation_tell')!='compress->inflate-before-release;Bladder_Main scale keyed independently': raise RuntimeError('Missing Puffback inflation-tell contract')
print(f'PASS Puffback r2: {len(meshes)} meshes / {tris} tris / 10 actions / readable inflate->puff tell',flush=True)
