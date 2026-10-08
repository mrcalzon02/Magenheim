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

# Verify the sculpted six-chain limb silhouette and raised dorsal sclerites.
for limb in limbs:
 for segment in ('Upper','Lower','Tarsus'):
  name=f'ShelfLurker_{limb}_{segment}'
  o=bpy.data.objects.get(name)
  if o is None: raise RuntimeError(f'Missing sculpted limb: {name}')
  mesh=o.data
  if len(mesh.vertices)!=208 or len(mesh.polygons)!=194 or sum(len(p.vertices)-2 for p in mesh.polygons)!=412:
   raise RuntimeError(f'{name}: expected 13x16 curved cuticle')
  if o.get('magenheim_limb_contract')!='curved-sclerite-13x16':
   raise RuntimeError(f'{name}: lost authored limb anatomy')
  if o.parent!=arm or o.vertex_groups.get(f'Leg_{limb}_{segment}') is None:
   raise RuntimeError(f'{name}: broken existing rig attachment')
  if not mesh.uv_layers.get('ShelfLurkerUV') or not mesh.materials:
   raise RuntimeError(f'{name}: missing PBR material/UV')
  edges=Counter()
  for poly in mesh.polygons:
   if poly.area<=1e-9: raise RuntimeError(f'{name}: degenerate cuticle')
   ids=list(poly.vertices)
   for j in range(len(ids)):
    edges[tuple(sorted((ids[j],ids[(j+1)%len(ids)])))]+=1
  if any(n!=2 for n in edges.values()): raise RuntimeError(f'{name}: open cuticle topology')
  centers=[sum((mesh.vertices[ring*16+j].co for j in range(16)),Vector())/16 for ring in (0,6,12)]
  bone=arm.data.bones[f'Leg_{limb}_{segment}']
  if (centers[0]-bone.head_local).length>.001 or (centers[2]-bone.tail_local).length>.001:
   raise RuntimeError(f'{name}: sculpted limb misses original bone endpoints')
  if (centers[1]-(centers[0]+centers[2])*.5).length<.025:
   raise RuntimeError(f'{name}: straight cylinder silhouette regression')
  for ring in (4,8):
   center=sum((mesh.vertices[ring*16+j].co for j in range(16)),Vector())/16
   if (mesh.vertices[ring*16+4].co-center).z<=.02:
    raise RuntimeError(f'{name}: armored sclerites face downward rather than dorsally')
   dorsal=(mesh.vertices[ring*16+4].co-center).length
   ventral=(mesh.vertices[ring*16+12].co-center).length
   if dorsal<=ventral*1.10:
    raise RuntimeError(f'{name}: lost physical dorsal sclerite relief')

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
 radii=plate.get('magenheim_shelf_uv_radii')
 if not radii or len(radii)!=2: raise RuntimeError(f'{plate.name}: missing authored shelf UV scale')
 rx,ry=map(float,radii)
 if rx<=0 or ry<=0: raise RuntimeError(f'{plate.name}: invalid shelf UV radii')
 if not (1.9*rx<=plate.dimensions.x<=2.25*rx and 1.9*ry<=plate.dimensions.y<=2.25*ry):
  raise RuntimeError(f'{plate.name}: cap dimensions diverge from authored UV scale')
 layer=mesh.uv_layers.get('ShelfLurkerUV')
 if not layer: raise RuntimeError(f'{plate.name}: sculpted shelf missing UV')
 # Concentric PBR growth bands must follow the cap radius, not object height
 # or angular cylindrical mapping (which turns them into unrelated stripes).
 for poly in mesh.polygons:
  for li in poly.loop_indices:
   co=mesh.vertices[mesh.loops[li].vertex_index].co
   u,v=layer.data[li].uv
   if abs(u-(.5+co.x/(2.24*rx)))>1e-5 or abs(v-(.5+co.y/(2.24*ry)))>1e-5:
    raise RuntimeError(f'{plate.name}: fungal growth texture UVs lost radial alignment')
   if not (0<=u<=1 and 0<=v<=1):
    raise RuntimeError(f'{plate.name}: scalloped lip exceeds texture atlas')
if len([o for o in meshes if o.name.startswith('ShelfLurker_DorsalShelf_')])!=4: raise RuntimeError('Shelf Lurker requires four modeled dorsal shelf plates')
if len([o for o in meshes if o.name.endswith('_Grip')])!=6: raise RuntimeError('Shelf Lurker requires six modeled grip pads')
if len([o for o in meshes if o.name.startswith('ShelfLurker_SensoryFrond_')])!=5: raise RuntimeError('Shelf Lurker sensory crown geometry incomplete')
# A five-cylinder crown passes an object-count check but is not a production
# sensory silhouette. Require physically curved, tapered, closed fronds.
for i in range(1,6):
 frond=bpy.data.objects.get(f'ShelfLurker_SensoryFrond_{i}')
 if frond is None: raise RuntimeError(f'Missing sensory frond {i}')
 mesh=frond.data
 if len(mesh.vertices)!=108 or len(mesh.polygons)!=98:
  raise RuntimeError(f'{frond.name}: expected 9x12 curved antenna rings with closed ends')
 if sum(len(p.vertices)-2 for p in mesh.polygons)!=212:
  raise RuntimeError(f'{frond.name}: sensory frond triangle density regressed')
 if frond.parent!=arm or frond.vertex_groups.get('Head') is None:
  raise RuntimeError(f'{frond.name}: sensory organ not bound to Head')
 if not any(m.type=='ARMATURE' and m.object==arm for m in frond.modifiers):
  raise RuntimeError(f'{frond.name}: missing armature modifier')
 if not mesh.materials or mesh.materials[0].name!='ShelfLurkerSensoryCrown':
  raise RuntimeError(f'{frond.name}: wrong sensory material')
 if not mesh.uv_layers.get('ShelfLurkerUV') or any(p.area<=1e-8 for p in mesh.polygons):
  raise RuntimeError(f'{frond.name}: missing UV or degenerate frond geometry')
 uv_layer=mesh.uv_layers['ShelfLurkerUV']
 cap_count=0
 for poly in mesh.polygons:
  coords=[tuple(uv_layer.data[li].uv) for li in poly.loop_indices]
  if any(not (0<=u<=1 and 0<=v<=1) for u,v in coords):
   raise RuntimeError(f'{frond.name}: sensory crown UVs exceed atlas')
  if len(poly.vertices)>4:
   cap_count+=1
   area=abs(sum(coords[j][0]*coords[(j+1)%len(coords)][1]-
                coords[(j+1)%len(coords)][0]*coords[j][1] for j in range(len(coords)))/2)
   if area<.59: raise RuntimeError(f'{frond.name}: sensory crown end-cap UV island collapsed')
  else:
   if max(u for u,v in coords)-min(u for u,v in coords)>.09:
    raise RuntimeError(f'{frond.name}: sensory crown side UV seam discontinuity')
 if cap_count!=2: raise RuntimeError(f'{frond.name}: expected two mapped sensory crown end caps')
 if frond.get('magenheim_sensory_frame')!='species-x-continuous':
  raise RuntimeError(f'{frond.name}: lost stable sensory frame contract')
 centers=[]; radii=[]; axes=[]
 for ring in range(9):
  points=[mesh.vertices[ring*12+j].co for j in range(12)]
  center=sum(points,Vector())/12
  centers.append(center)
  radii.append(sum((p-center).length for p in points)/12)
  axes.append((points[0]-center).normalized())
 if any(a.dot(b)<.866 for a,b in zip(axes,axes[1:])):
  raise RuntimeError(f'{frond.name}: abrupt sensory ring rotation twists texture and silhouette')
 if radii[-1]>=radii[0]*.25 or any(radii[j+1]>=radii[j] for j in range(8)):
  raise RuntimeError(f'{frond.name}: sensory tip no longer tapers')
 if (centers[4]-(centers[0]+centers[8])*.5).length<.07:
  raise RuntimeError(f'{frond.name}: straight cylinder replaced curved crown')
 if centers[8].z>centers[0].z-.30:
  raise RuntimeError(f'{frond.name}: lost downward sensory reach')

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
  mesh=o.data
  if len(mesh.vertices)!=204 or len(mesh.polygons)!=194 or sum(len(p.vertices)-2 for p in mesh.polygons)!=404:
   raise RuntimeError(f'{o.name}: expected 17x12 scalloped fungal lamella')
  if o.get('magenheim_gill_contract')!='swept-scalloped-lamella-17x12':
   raise RuntimeError(f'{o.name}: missing swept lamella contract')
  if not mesh.uv_layers.get('ShelfLurkerUV') or any(p.area<=1e-8 for p in mesh.polygons):
   raise RuntimeError(f'{o.name}: invalid gill mesh/UV')
  edges=Counter()
  for face in mesh.polygons:
   ids=list(face.vertices)
   for j in range(len(ids)):
    edges[tuple(sorted((ids[j],ids[(j+1)%len(ids)])))]+=1
  if any(n!=2 for n in edges.values()): raise RuntimeError(f'{o.name}: non-manifold gill')
  centers=[sum((mesh.vertices[ring*12+j].co for j in range(12)),Vector())/12 for ring in range(17)]
  if (centers[-1]-centers[0]).length<.15 or centers[8].z>=min(centers[0].z,centers[-1].z)-.022:
   raise RuntimeError(f'{o.name}: swept lamella silhouette collapsed')
  def half_width(ring):
   p=mesh.vertices[ring*12].co; c=centers[ring]
   return hypot(p.x-c.x,p.y-c.y)
  if half_width(12)-half_width(4)<.003:
   raise RuntimeError(f'{o.name}: scalloped gill margins collapsed')
  for ring in (4,8,12):
   dorsal=mesh.vertices[ring*12+3].co.z-centers[ring].z
   ventral=centers[ring].z-mesh.vertices[ring*12+9].co.z
   if dorsal-ventral<.0006:
    raise RuntimeError(f'{o.name}: gill dorsal midrib collapsed')
  heights=[v.co.z for v in mesh.vertices]
  if max(heights)>plate.location.z-.025 or min(heights)<plate.location.z-.11:
   raise RuntimeError(f'{o.name}: lamella detached from shelf underside')
# Swept eight-hook feeding anatomy: reject cones, disconnected joints and
# inward-collapsed funnels. Cone-specific world transform checks do not apply.
for digit in range(1,9):
 base=bpy.data.objects.get(f'ShelfLurker_MouthBarb_{digit}_Base')
 hook=bpy.data.objects.get(f'ShelfLurker_MouthBarb_{digit}_Hook')
 if base is None or hook is None: raise RuntimeError(f'Mouth barb {digit}: missing anatomy')
 for o in (base,hook):
  mesh=o.data
  if o.parent!=arm or o.vertex_groups.get('Head') is None:
   raise RuntimeError(f'{o.name}: incorrect Head binding')
  if not any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers):
   raise RuntimeError(f'{o.name}: missing armature modifier')
  if not mesh.materials or mesh.materials[0].name!='ShelfLurkerMouthGill':
   raise RuntimeError(f'{o.name}: incorrect mouth material')
  if o.get('magenheim_mouth_contract')!='swept-mouth-barb-9x12':
   raise RuntimeError(f'{o.name}: lost curved mouth anatomy')
  if len(mesh.vertices)!=108 or len(mesh.polygons)!=98 or sum(len(p.vertices)-2 for p in mesh.polygons)!=212:
   raise RuntimeError(f'{o.name}: wrong swept-barb topology')
  if not mesh.uv_layers.get('ShelfLurkerUV') or any(p.area<=1e-9 for p in mesh.polygons):
   raise RuntimeError(f'{o.name}: invalid mouth mesh/UV')
  edges=Counter()
  for poly in mesh.polygons:
   ids=list(poly.vertices)
   for k in range(len(ids)):
    edges[tuple(sorted((ids[k],ids[(k+1)%len(ids)])))]+=1
  if any(n!=2 for n in edges.values()): raise RuntimeError(f'{o.name}: nonmanifold mouth barb')
  # Guard against sudden 90-degree frame switches in the swept mouth.
  axes=[]
  for ring in range(9):
   points=[mesh.vertices[ring*12+j].co for j in range(12)]
   mid=sum(points,Vector())/12
   axes.append((points[0]-mid).normalized())
  if any(a.dot(b)<.866 for a,b in zip(axes,axes[1:])):
   raise RuntimeError(f'{o.name}: mouth section twists across adjacent rings')
 def center(obj,ring):
  return sum((obj.data.vertices[ring*12+j].co for j in range(12)),Vector())/12
 root,flare=center(base,0),center(base,8)
 joint,tip=center(hook,0),center(hook,8)
 if (flare-joint).length>.001: raise RuntimeError(f'Mouth barb {digit}: separated flare joint')
 if (center(base,4)-(root+flare)*.5).length<.018 or (center(hook,4)-(joint+tip)*.5).length<.018:
  raise RuntimeError(f'Mouth barb {digit}: lost curved hook silhouette')
 radial=lambda p: (p.x**2+(p.y-.38)**2)**.5
 if radial(flare)<radial(root)+.045 or radial(tip)>radial(flare)-.13 or radial(tip)<.10:
  raise RuntimeError(f'Mouth barb {digit}: lost outward flare or inward open funnel')
 if not root.z>flare.z>tip.z or tip.z>1.02:
  raise RuntimeError(f'Mouth barb {digit}: lost downward feeding reach')

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
print(f'PASS Shelf Lurker r2: {len(meshes)} meshes / {tris} tris / 8 actions / six-chain cling / 18 hooked talons / 24 scalloped lamellae / drop-pounce readable',flush=True)
