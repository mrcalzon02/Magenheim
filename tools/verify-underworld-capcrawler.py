#!/usr/bin/env python3
"""Acceptance gate for the authored Fungal Forest Capcrawler source asset."""
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-capcrawler.blend'
REQ_MATS={'CapcrawlerCarapace','CapcrawlerUnderside','CapcrawlerLegPlate','CapcrawlerMandible','CapcrawlerGill'}
REQ_BONES={'Root','Body','Mandible_L','Mandible_R','AttackOrigin','HitCenter','GillFX'}
for s in ('L','R'):
 for i in range(1,5): REQ_BONES.update({f'{s}_Coxa{i}',f'{s}_Leg{i}'})
REQ_ACTIONS={'Capcrawler_Idle':72,'Capcrawler_Scuttle':24,'Capcrawler_Turn':30,'Capcrawler_AttackFront':24,'Capcrawler_AttackLeft':26,'Capcrawler_AttackRight':26,'Capcrawler_Guard':36,'Capcrawler_Hit':16,'Capcrawler_Stagger':30,'Capcrawler_Death':54}
if not MODEL.exists(): raise RuntimeError(f'Missing model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-capcrawler': raise RuntimeError('Wrong model identity')
if sc.get('magenheim_host_rig')!='HOST-LOW-CRAWLER': raise RuntimeError('Wrong host rig')
if sc.get('magenheim_fidelity')!='production-creature-r3': raise RuntimeError('Expected production-creature-r3')
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one armature, found {len(arms)}')
arm=arms[0]; bones=set(arm.data.bones.keys()); missing=REQ_BONES-bones
if missing: raise RuntimeError('Missing bones: '+', '.join(sorted(missing)))
meshes=[o for o in sc.objects if o.type=='MESH']
if len(meshes)<30: raise RuntimeError(f'Detail regression: {len(meshes)} meshes')
triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if triangles<4500: raise RuntimeError(f'Source detail floor missed: {triangles} triangles')
for o in meshes:
 uv=o.data.uv_layers.get('CapcrawlerUV')
 if not uv or len(uv.data)!=len(o.data.loops): raise RuntimeError(f'{o.name}: explicit UV contract broken')
 mods=[m for m in o.modifiers if m.type=='ARMATURE' and m.object==arm]
 if len(mods)!=1: raise RuntimeError(f'{o.name}: armature binding broken')
 if not o.vertex_groups or not any(v.groups for v in o.data.vertices): raise RuntimeError(f'{o.name}: unweighted')
# Silhouette/anatomy gate: preserve the low fungal shield identity instead of allowing
# later edits to collapse this into a generic oval arthropod.
def world_bounds(o):
 pts=[o.matrix_world @ Vector(c) for c in o.bound_box]
 return tuple(min(p[i] for p in pts) for i in range(3)),tuple(max(p[i] for p in pts) for i in range(3))
def span(o):
 lo,hi=world_bounds(o); return tuple(hi[i]-lo[i] for i in range(3))
body=bpy.data.objects.get('Capcrawler_Body'); cap=bpy.data.objects.get('Capcrawler_Carapace')
if not body or not cap: raise RuntimeError('Body/carapace anatomy missing')
bw,bl,bh=span(body); cw,cl,ch=span(cap)
if cw < bw*1.15 or cl < bl*1.05: raise RuntimeError(f'Carapace no longer overhangs body: cap={cw:.3f}x{cl:.3f}, body={bw:.3f}x{bl:.3f}')
if ch > cw*.42: raise RuntimeError(f'Carapace too domed for low-crawler silhouette: {ch/cw:.2f}')
gills=[o for o in meshes if o.name.startswith('Capcrawler_Gill_')]
mandibles=[o for o in meshes if o.name.startswith('Capcrawler_Mandible_')]
feet=[o for o in meshes if o.name.startswith('Capcrawler_') and o.name.endswith('B')]
if len(gills)!=5 or len(mandibles)!=2 or len(feet)!=8: raise RuntimeError(f'Anatomy regression: gills={len(gills)} mandibles={len(mandibles)} distal_legs={len(feet)}')
foot_z=[world_bounds(o)[0][2] for o in feet]
if max(foot_z)-min(foot_z)>.025 or min(foot_z)<-.015 or max(foot_z)>.055: raise RuntimeError(f'Ground-contact regression: distal leg minima {min(foot_z):.3f}..{max(foot_z):.3f}m')
all_lo=[world_bounds(o)[0] for o in meshes]; all_hi=[world_bounds(o)[1] for o in meshes]
extent=(max(p[0] for p in all_hi)-min(p[0] for p in all_lo),max(p[1] for p in all_hi)-min(p[1] for p in all_lo),max(p[2] for p in all_hi)-min(p[2] for p in all_lo))
if not (.80<=extent[0]<=1.00 and .80<=extent[1]<=1.10 and extent[2]<=.55): raise RuntimeError(f'Creature silhouette outside authored envelope: {extent}')
mats={m.name:m for m in bpy.data.materials if m.name in REQ_MATS}
if set(mats)!=REQ_MATS: raise RuntimeError('Material family incomplete')
for name,m in mats.items():
 imgs=[n for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
 if len(imgs)<3 or not any(n.type=='NORMAL_MAP' for n in m.node_tree.nodes): raise RuntimeError(f'{name}: incomplete PBR nodes')
 if any(link.from_node.type=='TEX_COORD' and link.from_socket.name=='Generated' for link in m.node_tree.links): raise RuntimeError(f'{name}: Generated coordinates forbidden')
if len([n for n in mats['CapcrawlerGill'].node_tree.nodes if n.type=='TEX_IMAGE' and n.image])<4: raise RuntimeError('Gill emission missing')
actions={a.name:a for a in bpy.data.actions}; missing=set(REQ_ACTIONS)-set(actions)
if missing: raise RuntimeError('Missing actions: '+', '.join(sorted(missing)))
for name,end in REQ_ACTIONS.items():
 a=actions[name]
 if int(a.frame_start)!=1 or int(a.frame_end)!=end: raise RuntimeError(f'{name}: wrong frame range')
 if not a.fcurves: raise RuntimeError(f'{name}: no animation curves')
for name in ('Capcrawler_Scuttle','Capcrawler_AttackFront','Capcrawler_Death'):
 if len(actions[name].fcurves)<6: raise RuntimeError(f'{name}: insufficient articulation')
print(f'VERIFIED Capcrawler r3 source gate: meshes={len(meshes)} triangles={triangles} bones={len(bones)} materials={len(mats)} actions={len(REQ_ACTIONS)} extent={extent}',flush=True)
