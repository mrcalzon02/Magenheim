#!/usr/bin/env python3
"""Production acceptance gate for the Fungal Forest Shelf Lurker source asset."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-shelf-lurker.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing {MODEL}; run Shelf Lurker author and animation passes first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-shelf-lurker': raise RuntimeError('Wrong Shelf Lurker model identity')
if sc.get('magenheim_host_rig')!='HOST-WALL-CLINGER': raise RuntimeError('Shelf Lurker host-rig regression')
if sc.get('magenheim_fidelity')!='production-creature-r2': raise RuntimeError('Shelf Lurker animation/fidelity pass not applied')
reach=float(sc.get('magenheim_reach_m',0))
if not 1.8<=reach<=2.4: raise RuntimeError(f'Shelf Lurker reach outside design range: {reach}')
meshes=[o for o in sc.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<40 or tris<10000: raise RuntimeError(f'Shelf Lurker source fidelity regression: {len(meshes)} meshes / {tris} tris')
for o in meshes:
 if not o.data.uv_layers.get('ShelfLurkerUV'): raise RuntimeError(f'{o.name}: missing explicit ShelfLurkerUV')
 if not any(m.type=='ARMATURE' for m in o.modifiers): raise RuntimeError(f'{o.name}: missing armature binding')
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one Shelf Lurker armature, found {len(arms)}')
arm=arms[0]; limbs=('L_1','L_2','L_3','R_1','R_2','R_3')
required={'Root','Body','Abdomen','Head','AttackOrigin','HitCenter','SenseFX'}
for limb in limbs: required|={f'Leg_{limb}_{j}' for j in ('Upper','Lower','Tarsus','Grip')}
missing=required-{b.name for b in arm.data.bones}
if missing: raise RuntimeError('Missing Shelf Lurker bones: '+','.join(sorted(missing)))
if len([o for o in meshes if o.name.startswith('ShelfLurker_DorsalShelf_')])!=4: raise RuntimeError('Shelf Lurker requires four modeled dorsal shelf plates')
if len([o for o in meshes if o.name.endswith('_Grip')])!=6: raise RuntimeError('Shelf Lurker requires six modeled grip pads')
if len([o for o in meshes if o.name.startswith('ShelfLurker_SensoryFrond_')])!=5: raise RuntimeError('Shelf Lurker sensory crown geometry incomplete')
expected={'ShelfLurker_ClingIdle':72,'ShelfLurker_LateralCrawl':42,'ShelfLurker_Reposition':52,'ShelfLurker_DropPounce':32,'ShelfLurker_Recover':38,'ShelfLurker_Attack':24,'ShelfLurker_Hit':18,'ShelfLurker_Death':58}

def curves(a):
 legacy=getattr(a,'fcurves',None)
 if legacy is not None:return list(legacy)
 out=[]
 for layer in getattr(a,'layers',()):
  for strip in getattr(layer,'strips',()):
   for bag in getattr(strip,'channelbags',()):out.extend(bag.fcurves)
 return out

def paths(name): return {fc.data_path for fc in curves(bpy.data.actions[name])}
for name,end in expected.items():
 a=bpy.data.actions.get(name)
 if not a: raise RuntimeError(f'Missing Shelf Lurker action {name}')
 if int(round(a.frame_end))!=end: raise RuntimeError(f'{name}: expected frame end {end}, got {a.frame_end}')
 if not curves(a): raise RuntimeError(f'{name}: no readable curves')
# Lateral crawl must articulate every joint in all six load-bearing chains.
crawl=paths('ShelfLurker_LateralCrawl')
for limb in limbs:
 for joint in ('Upper','Lower','Tarsus','Grip'):
  token=f'pose.bones["Leg_{limb}_{joint}"]'
  if not any(token in p for p in crawl): raise RuntimeError(f'Lateral crawl leaves {limb}_{joint} rigid')
# Reposition must independently release/replant every grip rather than rotate the creature as a single rigid mesh.
reposition=paths('ShelfLurker_Reposition')
for limb in limbs:
 token=f'pose.bones["Leg_{limb}_Grip"]'
 if not any(token in p for p in reposition): raise RuntimeError(f'Reposition never releases {limb} grip')
# Drop ambush must transform torso/head and all six upper/lower/tarsus chains.
drop=paths('ShelfLurker_DropPounce')
for core in ('Body','Abdomen','Head'):
 if not any(f'pose.bones["{core}"]' in p for p in drop): raise RuntimeError(f'Drop pounce does not articulate {core}')
for limb in limbs:
 for joint in ('Upper','Lower','Tarsus'):
  if not any(f'pose.bones["Leg_{limb}_{joint}"]' in p for p in drop): raise RuntimeError(f'Drop pounce leaves {limb}_{joint} rigid')
# Death must visibly break the six-point cling rather than freezing attached to the ceiling.
death=paths('ShelfLurker_Death')
for limb in limbs:
 if not any(f'pose.bones["Leg_{limb}_Grip"]' in p for p in death): raise RuntimeError(f'Death never releases {limb} grip')
contract='six-independent-grip-chains;sequential-release-replant;drop-pounce-without-whole-mesh-rotation'
if sc.get('magenheim_cling_contract')!=contract: raise RuntimeError('Missing Shelf Lurker cling/replant contract')
print(f'PASS Shelf Lurker r2: {len(meshes)} meshes / {tris} tris / 8 actions / six-chain cling and drop-pounce readable',flush=True)
