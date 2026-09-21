#!/usr/bin/env python3
"""Production acceptance gate for the Blackwater Deep Cave Ray source asset."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-cave-ray.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing {MODEL}; run Cave Ray author and animation passes first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); sc=bpy.context.scene
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one Cave Ray armature, found {len(arms)}')
arm=arms[0]
if arm.get('magenheim_asset')!='cave-ray' or arm.get('host_rig')!='HOST-AQUATIC-RAY': raise RuntimeError('Cave Ray identity/host-rig regression')
if arm.get('production_contract')!='production-creature-r2': raise RuntimeError('Cave Ray animation pass not applied')
span=float(arm.get('span_m',0))
if not 2.5<=span<=4.0: raise RuntimeError(f'Cave Ray span outside design range: {span}')
meshes=[o for o in sc.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<20 or tris<7000: raise RuntimeError(f'Cave Ray fidelity regression: {len(meshes)} meshes / {tris} tris')
for o in meshes:
 if not o.data.uv_layers.get('CaveRayUV'): raise RuntimeError(f'{o.name}: missing CaveRayUV')
 if not any(m.type=='ARMATURE' for m in o.modifiers): raise RuntimeError(f'{o.name}: missing armature binding')
required={'Root','Body','Head','AttackOrigin','HitCenter','VentralFX'}|{f'Wing_{s}_{i}' for s in ('L','R') for i in range(1,4)}|{f'Tail_{i}' for i in range(1,6)}
missing=required-{b.name for b in arm.data.bones}
if missing: raise RuntimeError('Missing Cave Ray bones: '+','.join(sorted(missing)))
if len([o for o in meshes if o.name.startswith('Photophore_')])!=6: raise RuntimeError('Cave Ray requires six modeled photophores')
expected={'CaveRay_Glide':72,'CaveRay_FlapImpulse':34,'CaveRay_BankLeft':42,'CaveRay_BankRight':42,'CaveRay_Dive':40,'CaveRay_Rise':40,'CaveRay_Flee':30,'CaveRay_Hit':18,'CaveRay_Death':72}
def curves(a):
 legacy=getattr(a,'fcurves',None)
 if legacy is not None:return list(legacy)
 out=[]
 for layer in getattr(a,'layers',()):
  for strip in getattr(layer,'strips',()):
   for bag in getattr(strip,'channelbags',()):out.extend(bag.fcurves)
 return out
def amp(name,bone):
 vals=[]; token=f'pose.bones["{bone}"]'
 for fc in curves(bpy.data.actions[name]):
  if token in fc.data_path: vals.extend(abs(k.co[1]) for k in fc.keyframe_points)
 return max(vals) if vals else 0
for name,end in expected.items():
 a=bpy.data.actions.get(name)
 if not a or int(round(a.frame_end))!=end or not curves(a): raise RuntimeError(f'{name}: missing/invalid action contract')
# Flap must increase deformation from wing root through mid to tip on both sides.
for side in ('L','R'):
 a=[amp('CaveRay_FlapImpulse',f'Wing_{side}_{i}') for i in range(1,4)]
 if not (a[0]>=.30 and a[0]<a[1]<a[2]): raise RuntimeError(f'Flap lacks progressive root-mid-tip deformation on {side}: {a}')
# Banks must be materially asymmetric and opposite in handedness.
for name,low,high in [('CaveRay_BankLeft','L','R'),('CaveRay_BankRight','R','L')]:
 lo=amp(name,f'Wing_{low}_3'); hi=amp(name,f'Wing_{high}_3')
 if hi-lo<.15 or amp(name,'Body')<.25: raise RuntimeError(f'{name}: insufficient asymmetric membrane/body bank')
# Tail must articulate as a chain in locomotion rather than remain a rigid appendage.
for name in ('CaveRay_Glide','CaveRay_FlapImpulse','CaveRay_Flee'):
 for i in range(1,6):
  if amp(name,f'Tail_{i}')<=0: raise RuntimeError(f'{name}: Tail_{i} is rigid')
if arm.get('animation_readability_contract')!='progressive-root-mid-tip-membrane;asymmetric-banks;independent-tail-chain': raise RuntimeError('Missing Cave Ray readability contract')
print(f'PASS Cave Ray r2: {len(meshes)} meshes / {tris} tris / 9 actions / progressive membrane, asymmetric banks, articulated tail',flush=True)
