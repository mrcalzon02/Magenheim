#!/usr/bin/env python3
"""Acceptance gate for the authored Fungal Forest Mycelial Stalker source asset."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]; MODEL=ROOT/'assets/models/source/underworld-creature-mycelial-stalker.blend'
REQ_MATS={'StalkerRootHide','StalkerMycelium','StalkerShelfFungus','StalkerSensoryTissue'}
REQ_BONES={'Root','Spine','Pelvis','Neck','Head','Jaw_L','Jaw_R','AttackOrigin','HitCenter','SenseFX'}
for n in ('Front_L','Front_R','Rear_L','Rear_R'):REQ_BONES.update({f'{n}_Upper',f'{n}_Lower',f'{n}_Foot'})
REQ_ACTIONS={'MycelialStalker_ConcealIdle':84,'MycelialStalker_Crouch':36,'MycelialStalker_Walk':32,'MycelialStalker_Pounce':34,'MycelialStalker_FailedPounceRetreat':42,'MycelialStalker_Hit':18,'MycelialStalker_Stagger':32,'MycelialStalker_Death':58}
if not MODEL.exists():raise RuntimeError(f'Missing model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL));sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-mycelial-stalker' or sc.get('magenheim_host_rig')!='HOST-QUADRUPED':raise RuntimeError('Wrong model/host identity')
if sc.get('magenheim_fidelity')!='production-creature-r2':raise RuntimeError('Expected production-creature-r2 animation pass')
if not (1.2<=float(sc.get('magenheim_shoulder_height_m',0))<=1.5):raise RuntimeError('Shoulder-height contract broken')
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1:raise RuntimeError(f'Expected one armature, found {len(arms)}')
arm=arms[0];bones=set(arm.data.bones.keys());missing=REQ_BONES-bones
if missing:raise RuntimeError('Missing bones: '+', '.join(sorted(missing)))
meshes=[o for o in sc.objects if o.type=='MESH'];tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<30 or tris<7000:raise RuntimeError(f'Fidelity regression: meshes={len(meshes)} tris={tris}')
for o in meshes:
 uv=o.data.uv_layers.get('MycelialStalkerUV')
 if not uv or len(uv.data)!=len(o.data.loops):raise RuntimeError(f'{o.name}: explicit UV contract broken')
 if len([m for m in o.modifiers if m.type=='ARMATURE' and m.object==arm])!=1:raise RuntimeError(f'{o.name}: armature binding broken')
 if not o.vertex_groups or not any(v.groups for v in o.data.vertices):raise RuntimeError(f'{o.name}: unweighted')
if len([o for o in meshes if o.name.startswith('Stalker_Shelf_')])!=3:raise RuntimeError('Asymmetric shelf silhouette incomplete')
if sc.get('magenheim_shelf_anatomy')!='three-rimmed-brackets+24-ventral-ribs':raise RuntimeError('Fungal shelf anatomy contract missing')
for i in (1,2,3):
 shelf_obj=bpy.data.objects.get(f'Stalker_Shelf_{i}')
 lip=bpy.data.objects.get(f'Stalker_ShelfLip_{i}')
 if not shelf_obj or not lip or lip.type!='MESH' or lip.vertex_groups.get('Spine') is None:
  raise RuntimeError(f'Shelf {i}: missing rim or incorrect rig binding')
 if not lip.data.materials or lip.data.materials[0].name!='StalkerShelfFungus':
  raise RuntimeError(f'Shelf {i}: incorrect rim material')
 if lip.dimensions.x < shelf_obj.dimensions.x*.88 or lip.dimensions.y < shelf_obj.dimensions.y*.88:
  raise RuntimeError(f'Shelf {i}: rim no longer extends around fungal bracket')
 for rib in range(8):
  o=bpy.data.objects.get(f'Stalker_ShelfGill_{i}_{rib}')
  if not o or o.type!='MESH' or o.vertex_groups.get('Spine') is None:
   raise RuntimeError(f'Shelf {i}: missing or unbound underside gill {rib}')
  if not o.data.materials or o.data.materials[0].name!='StalkerMycelium':
   raise RuntimeError(f'Shelf {i}: gill {rib} has incorrect material')
  if o.location.z>=shelf_obj.location.z:
   raise RuntimeError(f'Shelf {i}: gill {rib} no longer lies beneath the cap')
if len([o for o in meshes if o.name.startswith('Stalker_MycelialCord_')])!=3:raise RuntimeError('Visible mycelial cord anatomy incomplete')
if sc.get('magenheim_root_anatomy')!='four-tapered-limbs+four-knee-burls+eight-hooked-claws':
 raise RuntimeError('Root anatomy contract missing')
for leg in ('Front_L','Front_R','Rear_L','Rear_R'):
 for suffix,bone,mat in (('Upper',f'{leg}_Upper','StalkerRootHide'),('Lower',f'{leg}_Lower','StalkerRootHide'),('KneeBurl',f'{leg}_Lower','StalkerRootHide')):
  o=bpy.data.objects.get(f'Stalker_{leg}_{suffix}')
  if not o or o.type!='MESH' or o.vertex_groups.get(bone) is None or not o.data.materials or o.data.materials[0].name!=mat:
   raise RuntimeError(f'{leg}: missing or incorrectly bound {suffix}')
 for toe in (-1,1):
  t=bpy.data.objects.get(f'Stalker_{leg}_Toe{toe}')
  c=bpy.data.objects.get(f'Stalker_{leg}_RootClaw{toe}')
  for o in (t,c):
   if not o or o.type!='MESH' or o.vertex_groups.get(f'{leg}_Foot') is None:
    raise RuntimeError(f'{leg}: missing or unbound root claw {toe}')
   if not o.data.materials or o.data.materials[0].name!='StalkerMycelium':
    raise RuntimeError(f'{leg}: root claw material mismatch')
  if c.location.y<=t.location.y or c.location.z>=t.location.z:
   raise RuntimeError(f'{leg}: hooked claw lost its forward/downward silhouette')
if len([o for o in meshes if o.name.startswith('Stalker_SenseNode_')])!=2:raise RuntimeError('Sensory-node anatomy incomplete')
# Physical sensory crown and gripping teeth must survive the authoring pass.
if sc.get('magenheim_sensory_anatomy')!='four-curved-fronds+eight-fork-tips':raise RuntimeError('Sensory silhouette contract missing')
if sc.get('magenheim_jaw_detail')!='six-jaw-bound-gripping-teeth':raise RuntimeError('Physical jaw-detail contract missing')
fronds=[o for o in meshes if o.name.startswith('Stalker_Frond_')]
teeth=[o for o in meshes if o.name.startswith('Stalker_JawTooth_')]
if len(fronds)!=16 or len(teeth)!=6:raise RuntimeError(f'Anatomical silhouette regression: fronds={len(fronds)} teeth={len(teeth)}')
for side in ('L','R'):
 for branch in (1,2):
  for suffix in ('Base','Tip','Fork1','Fork2'):
   name=f'Stalker_Frond_{side}{branch}_{suffix}'
   o=bpy.data.objects.get(name)
   if not o or o.vertex_groups.get('Head') is None or not o.data.materials or o.data.materials[0].name!='StalkerMycelium':
    raise RuntimeError(f'{name}: missing, unbound or incorrect sensory anatomy')
 for i in (1,2,3):
  name=f'Stalker_JawTooth_{side}{i}'
  o=bpy.data.objects.get(name)
  if not o or o.vertex_groups.get(f'Jaw_{side}') is None or not o.data.materials or o.data.materials[0].name!='StalkerRootHide':
   raise RuntimeError(f'{name}: missing, unbound or incorrect gripping tooth')
def world_extreme(o,axis):
 from mathutils import Vector
 return max((o.matrix_world @ Vector(c))[axis] for c in o.bound_box)
if max(world_extreme(o,2) for o in fronds)<1.79 or max(world_extreme(o,1) for o in fronds)<1.50:
 raise RuntimeError('Sensory crown no longer breaks the dorsal/frontal silhouette')
mats={m.name:m for m in bpy.data.materials if m.name in REQ_MATS}
if set(mats)!=REQ_MATS:raise RuntimeError('Material family incomplete')
for name,m in mats.items():
 imgs=[n for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
 if len(imgs)<3 or not any(n.type=='NORMAL_MAP' for n in m.node_tree.nodes):raise RuntimeError(f'{name}: incomplete PBR nodes')
 if any(link.from_node.type=='TEX_COORD' and link.from_socket.name=='Generated' for link in m.node_tree.links):raise RuntimeError(f'{name}: Generated coordinates forbidden')
if len([n for n in mats['StalkerSensoryTissue'].node_tree.nodes if n.type=='TEX_IMAGE' and n.image])<4:raise RuntimeError('Localized sensory emission missing')
for name in REQ_MATS-{'StalkerSensoryTissue'}:
 if len([n for n in mats[name].node_tree.nodes if n.type=='TEX_IMAGE' and n.image])>3:raise RuntimeError(f'{name}: unexpected emission/extra texture path')
def curves(act):
 legacy=getattr(act,'fcurves',None)
 if legacy is not None:return list(legacy)
 out=[]
 for layer in getattr(act,'layers',()):
  for strip in getattr(layer,'strips',()):
   for bag in getattr(strip,'channelbags',()):out.extend(bag.fcurves)
 return out
actions={a.name:a for a in bpy.data.actions};missing=set(REQ_ACTIONS)-set(actions)
if missing:raise RuntimeError('Missing actions: '+', '.join(sorted(missing)))
for name,end in REQ_ACTIONS.items():
 a=actions[name];cs=curves(a)
 if int(a.frame_start)!=1 or int(a.frame_end)!=end:raise RuntimeError(f'{name}: wrong frame range')
 if not cs:raise RuntimeError(f'{name}: no animation curves')
for name in ('MycelialStalker_Walk','MycelialStalker_FailedPounceRetreat'):
 paths={fc.data_path for fc in curves(actions[name])}
 for leg in ('Front_L','Front_R','Rear_L','Rear_R'):
  for joint in ('Upper','Lower','Foot'):
   if not any(f'pose.bones["{leg}_{joint}"]' in p for p in paths):raise RuntimeError(f'{name}: {leg}_{joint} lacks locomotion articulation')
for name in ('MycelialStalker_Pounce','MycelialStalker_Death'):
 if len(curves(actions[name]))<9:raise RuntimeError(f'{name}: insufficient full-body articulation')
print(f'VERIFIED Mycelial Stalker r2: meshes={len(meshes)} tris={tris} bones={len(bones)} materials={len(mats)} actions={len(REQ_ACTIONS)}',flush=True)
