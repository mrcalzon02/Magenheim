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
if len([o for o in meshes if o.name.startswith('Stalker_MycelialCord_')])!=3:raise RuntimeError('Visible mycelial cord anatomy incomplete')
if len([o for o in meshes if o.name.startswith('Stalker_SenseNode_')])!=2:raise RuntimeError('Sensory-node anatomy incomplete')
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
