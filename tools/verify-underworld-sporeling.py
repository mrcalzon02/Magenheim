#!/usr/bin/env python3
"""Acceptance gate for the authored Fungal Forest Sporeling source asset."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-sporeling.blend'
TEX=ROOT/'assets/textures/underworld/creatures/sporeling'
REQ_BONES={'Root','Thorax','Abdomen','Head','Jaw','AttackOrigin','SporeFX','HitCenter'}
for side in ('L','R'):
    REQ_BONES.add(f'Frond_{side}')
    for i in range(1,4): REQ_BONES.update({f'{side}_Coxa{i}',f'{side}_Femur{i}',f'{side}_Tarsus{i}'})
REQ_TEX={f'{stem}-{kind}.png' for stem in ('flesh','cap-chitin','joint','gill','spore-sac') for kind in ('albedo','normal','roughness')}
REQ_TEX.update({'gill-emission.png','spore-sac-emission.png'})
REQ_MATERIALS={'SporelingFlesh','SporelingCapChitin','SporelingJoint','SporelingGill','SporelingSporeSac'}

if not MODEL.exists(): raise RuntimeError(f'Missing authored model: {MODEL}')
missing_tex=sorted(p for p in REQ_TEX if not (TEX/p).exists())
if missing_tex: raise RuntimeError('Missing Sporeling textures: '+', '.join(missing_tex))
for p in sorted(TEX.glob('*.png')):
    im=bpy.data.images.load(str(p),check_existing=False)
    if tuple(im.size)!=(1024,1024): raise RuntimeError(f'{p.name}: expected 1024x1024, got {tuple(im.size)}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL)); sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-sporeling': raise RuntimeError('Wrong model identity')
if sc.get('magenheim_host_rig')!='HOST-SWARM-HEXAPOD': raise RuntimeError('Wrong host rig')
if sc.get('magenheim_fidelity')!='production-creature-r2': raise RuntimeError(f"Expected production-creature-r2, got {sc.get('magenheim_fidelity')}")
if sc.get('magenheim_skinning')!='rigid-segment-weighted': raise RuntimeError('Sporeling skinning contract is not explicit')
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one armature, found {len(arms)}')
arm=arms[0]; bones=set(arm.data.bones.keys()); missing=sorted(REQ_BONES-bones)
if missing: raise RuntimeError('Missing rig bones: '+', '.join(missing))
meshes=[o for o in sc.objects if o.type=='MESH']
if len(meshes)<40: raise RuntimeError(f'Creature detail regression: {len(meshes)} mesh parts')
triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if triangles<4000: raise RuntimeError(f'Runtime-detail floor missed: {triangles} source triangles (<4000)')
names={o.name for o in meshes}
for token in ('Thorax','Abdomen','Head','CapArmor','GillRim','Mandible_L','Mandible_R','SporeVent_1'):
    if not any(token in n for n in names): raise RuntimeError(f'Missing anatomical structure: {token}')
# Every modeled anatomical segment must be bound to the single host armature and carry a valid bone group.
for o in meshes:
    mods=[m for m in o.modifiers if m.type=='ARMATURE' and m.object==arm]
    if len(mods)!=1: raise RuntimeError(f'{o.name}: expected exactly one Sporeling armature modifier')
    groups={g.name for g in o.vertex_groups}
    if not groups: raise RuntimeError(f'{o.name}: unweighted anatomical mesh')
    unknown=groups-bones
    if unknown: raise RuntimeError(f'{o.name}: vertex groups do not resolve to bones: {sorted(unknown)}')
    if not any(v.groups for v in o.data.vertices): raise RuntimeError(f'{o.name}: contains no weighted vertices')
# Texture-backed materials must retain distinct material response and normal information.
materials={m.name:m for m in bpy.data.materials if m.name in REQ_MATERIALS}
missing_mats=REQ_MATERIALS-set(materials)
if missing_mats: raise RuntimeError('Missing creature materials: '+', '.join(sorted(missing_mats)))
for name,m in materials.items():
    if not m.use_nodes: raise RuntimeError(f'{name}: node material required')
    nodes=list(m.node_tree.nodes); images=[n for n in nodes if n.type=='TEX_IMAGE' and n.image]
    if len(images)<3: raise RuntimeError(f'{name}: requires albedo, normal and roughness texture nodes')
    if not any(n.type=='NORMAL_MAP' for n in nodes): raise RuntimeError(f'{name}: normal map node missing')
for name in ('SporelingGill','SporelingSporeSac'):
    if len([n for n in materials[name].node_tree.nodes if n.type=='TEX_IMAGE' and n.image])<4: raise RuntimeError(f'{name}: localized emission texture missing')
pts=[o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
mins=[min(p[i] for p in pts) for i in range(3)]; maxs=[max(p[i] for p in pts) for i in range(3)]; span=[maxs[i]-mins[i] for i in range(3)]
if max(span)>0.80 or span[2]<0.30: raise RuntimeError(f'Unexpected creature bounds: {span}')
manifest=set(str(sc.get('magenheim_animation_manifest','')).split(','))
for clip in ('idle','walk','scuttle','bite','death-spore-puff'):
    if clip not in manifest: raise RuntimeError(f'Animation contract missing {clip}')
print(f'VERIFIED Sporeling r2 source gate: meshes={len(meshes)} triangles={triangles} bones={len(bones)} bounds={tuple(round(x,3) for x in span)} textures={len(REQ_TEX)} materials={len(materials)}',flush=True)
