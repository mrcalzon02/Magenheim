#!/usr/bin/env python3
"""Production acceptance gate for the Fungal Forest Shelf Lurker source asset."""
from pathlib import Path
import bpy
from mathutils import Vector
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
if len(meshes)<116 or tris<10000: raise RuntimeError(f'Shelf Lurker source fidelity regression: {len(meshes)} meshes / {tris} tris')
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
# Production shelf geometry must have a watertight, sculpted rim; a flattened
# icosphere or a mere painted scallop does not satisfy the silhouette gate.
from collections import Counter
from math import hypot
for index in range(1,5):
 plate=bpy.data.objects.get(f'ShelfLurker_DorsalShelf_{index}')
 if plate is None: raise RuntimeError(f'Missing dorsal shelf {index}')
 mesh=plate.data
 if len(mesh.vertices)!=1538 or len(mesh.polygons)!=1664:
  raise RuntimeError(f'{plate.name}: expected 12-ring sculpted shelf (1538 verts / 1664 faces)')
 if sum(len(p.vertices)-2 for p in mesh.polygons)!=3072:
  raise RuntimeError(f'{plate.name}: scalloped shelf triangle density regressed')
 edges=Counter()
 for face in mesh.polygons:
  ids=list(face.vertices)
  if face.area<=1e-9: raise RuntimeError(f'{plate.name}: degenerate shelf face')
  for j in range(len(ids)):
   edges[tuple(sorted((ids[j],ids[(j+1)%len(ids)])))]+=1
 if any(n!=2 for n in edges.values()):
  raise RuntimeError(f'{plate.name}: open or non-manifold shelf surface')
 rim=[mesh.vertices[2+7*128+j].co for j in range(128)]
 lip=[mesh.vertices[2+8*128+j].co for j in range(128)]
 extents=[hypot(p.x/plate.dimensions.x*2,p.y/plate.dimensions.y*2) for p in rim]
 # Normalized to the true bounds; 7+13 growth scallops must survive.
 if max(extents)-min(extents)<.08:
  raise RuntimeError(f'{plate.name}: lost visible scalloped outer silhouette')
 if any(rim[j].z-lip[j].z<=.010 for j in range(128)):
  raise RuntimeError(f'{plate.name}: physical underside lip collapsed')
 if not mesh.uv_layers.get('ShelfLurkerUV'):
  raise RuntimeError(f'{plate.name}: sculpted shelf missing UV')
if len([o for o in meshes if o.name.startswith('ShelfLurker_DorsalShelf_')])!=4: raise RuntimeError('Shelf Lurker requires four modeled dorsal shelf plates')
if len([o for o in meshes if o.name.endswith('_Grip')])!=6: raise RuntimeError('Shelf Lurker requires six modeled grip pads')
if len([o for o in meshes if o.name.startswith('ShelfLurker_SensoryFrond_')])!=5: raise RuntimeError('Shelf Lurker sensory crown geometry incomplete')
# All 18 gripping claws have two tapered, joined sections and their own
# outward-then-inward silhouette. Verify actual mesh endpoints, not just names.
talons=[o for o in meshes if '_Talon_' in o.name]
if len(talons)!=36: raise RuntimeError(f'Shelf Lurker requires 36 talon sections, found {len(talons)}')
def talon_endpoints(obj):
 zs=[v.co.z for v in obj.data.vertices]
 return (obj.matrix_world @ Vector((0,0,min(zs))),obj.matrix_world @ Vector((0,0,max(zs))))
for limb in limbs:
 pad=bpy.data.objects.get(f'ShelfLurker_{limb}_Grip')
 if pad is None: raise RuntimeError(f'Missing grip pad for {limb}')
 sign=-1 if limb.startswith('L') else 1
 tips=[]
 for digit in (1,2,3):
  base=bpy.data.objects.get(f'ShelfLurker_{limb}_Talon_{digit}_Base')
  hook=bpy.data.objects.get(f'ShelfLurker_{limb}_Talon_{digit}_Hook')
  if base is None or hook is None: raise RuntimeError(f'{limb}: incomplete three-claw anatomy')
  for obj in (base,hook):
   if obj.type!='MESH' or len(obj.data.polygons)<12 or any(p.area<=1e-8 for p in obj.data.polygons):
    raise RuntimeError(f'{obj.name}: invalid physical talon topology')
   if obj.parent!=arm or obj.vertex_groups.get(f'Leg_{limb}_Grip') is None:
    raise RuntimeError(f'{obj.name}: not bound to grip bone')
   if not any(m.type=='ARMATURE' and m.object==arm for m in obj.modifiers):
    raise RuntimeError(f'{obj.name}: missing armature modifier')
   if not obj.data.materials or obj.data.materials[0].name!='ShelfLurkerShelfArmor':
    raise RuntimeError(f'{obj.name}: wrong hard-talon material')
   if not obj.data.uv_layers.get('ShelfLurkerUV'):
    raise RuntimeError(f'{obj.name}: missing authored UV')
  start,knuckle=talon_endpoints(base)
  joint,point=talon_endpoints(hook)
  if (joint-knuckle).length>.02: raise RuntimeError(f'{limb} claw {digit}: sections detached')
  if sign*(knuckle.x-pad.location.x)<.22: raise RuntimeError(f'{limb} claw {digit}: lost outward silhouette')
  if sign*(point.x-knuckle.x)>-.09: raise RuntimeError(f'{limb} claw {digit}: no inward hook')
  if point.z>pad.location.z-.20: raise RuntimeError(f'{limb} claw {digit}: tip no longer grips below pad')
  tips.append(point.y)
 if not tips[0]<tips[1]<tips[2] or tips[2]-tips[0]<.30:
  raise RuntimeError(f'{limb}: three claws collapsed into one silhouette')

# Verify physical underside anatomy, rather than counting named meshes alone.
gills=[o for o in meshes if o.name.startswith('ShelfLurker_ShelfGill_')]
barbs=[o for o in meshes if o.name.startswith('ShelfLurker_MouthBarb_')]
if len(gills)!=24 or len(barbs)!=16:
 raise RuntimeError(f'Incomplete underside anatomy: {len(gills)} shelf gills / {len(barbs)} mouth sections')
for shelf_index in range(1,5):
 plate=bpy.data.objects.get(f'ShelfLurker_DorsalShelf_{shelf_index}')
 if plate is None: raise RuntimeError(f'Missing dorsal shelf {shelf_index}')
 bone='Body' if shelf_index<3 else 'Abdomen'
 for rib in range(1,7):
  o=bpy.data.objects.get(f'ShelfLurker_ShelfGill_{shelf_index}_{rib}')
  if o is None: raise RuntimeError(f'Shelf {shelf_index}: missing underside rib {rib}')
  if o.parent!=arm or o.vertex_groups.get(bone) is None: raise RuntimeError(f'{o.name}: incorrect shelf rig binding')
  if not o.data.materials or o.data.materials[0].name!='ShelfLurkerMouthGill': raise RuntimeError(f'{o.name}: incorrect lamella material')
  if not o.data.uv_layers.get('ShelfLurkerUV') or any(p.area<=1e-8 for p in o.data.polygons): raise RuntimeError(f'{o.name}: invalid gill mesh/UV')
  inner,outer=talon_endpoints(o)
  if max(inner.z,outer.z)>plate.location.z-.025 or min(inner.z,outer.z)<plate.location.z-.085: raise RuntimeError(f'{o.name}: gill is detached from sculpted shelf underside')
for digit in range(1,9):
 base=bpy.data.objects.get(f'ShelfLurker_MouthBarb_{digit}_Base')
 hook=bpy.data.objects.get(f'ShelfLurker_MouthBarb_{digit}_Hook')
 if base is None or hook is None: raise RuntimeError(f'Mouth barb {digit}: missing geometry')
 for o in (base,hook):
  if o.parent!=arm or o.vertex_groups.get('Head') is None: raise RuntimeError(f'{o.name}: incorrect Head binding')
  if not o.data.materials or o.data.materials[0].name!='ShelfLurkerMouthGill': raise RuntimeError(f'{o.name}: incorrect mouth material')
  if not o.data.uv_layers.get('ShelfLurkerUV') or any(p.area<=1e-8 for p in o.data.polygons): raise RuntimeError(f'{o.name}: invalid mouth mesh/UV')
 root,flare=talon_endpoints(base)
 joint,tip=talon_endpoints(hook)
 if (flare-joint).length>.02: raise RuntimeError(f'Mouth barb {digit}: sections disconnected')
 radial=lambda p: (p.x**2+(p.y-.38)**2)**.5
 if radial(flare)<radial(root)+.045 or radial(tip)>radial(flare)-.13:
  raise RuntimeError(f'Mouth barb {digit}: lost outward flare or inward funnel')
 if tip.z>1.02: raise RuntimeError(f'Mouth barb {digit}: downward strike silhouette lost')

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
print(f'PASS Shelf Lurker r2: {len(meshes)} meshes / {tris} tris / 8 actions / six-chain cling / 18 hooked talons / drop-pounce readable',flush=True)
