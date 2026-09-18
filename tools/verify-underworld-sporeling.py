#!/usr/bin/env python3
"""Acceptance gate for the authored Fungal Forest Sporeling source asset."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]; MODEL=ROOT/'assets/models/source/underworld-creature-sporeling.blend'; TEX=ROOT/'assets/textures/underworld/creatures/sporeling'
REQ_BONES={'Root','Thorax','Abdomen','Head','Jaw','AttackOrigin','SporeFX','HitCenter'}
for side in ('L','R'):
    REQ_BONES.add(f'Frond_{side}')
    for i in range(1,4): REQ_BONES.update({f'{side}_Coxa{i}',f'{side}_Femur{i}',f'{side}_Tarsus{i}'})
REQ_TEX={f'{stem}-{kind}.png' for stem in ('flesh','cap-chitin','joint','gill','spore-sac') for kind in ('albedo','normal','roughness')}; REQ_TEX.update({'gill-emission.png','spore-sac-emission.png'})
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
if sc.get('magenheim_fidelity')!='production-creature-r4': raise RuntimeError(f"Expected production-creature-r4, got {sc.get('magenheim_fidelity')}")
if sc.get('magenheim_uv')!='authored-object-projection': raise RuntimeError('Explicit authored UV contract missing')
arms=[o for o in sc.objects if o.type=='ARMATURE']
if len(arms)!=1: raise RuntimeError(f'Expected one armature, found {len(arms)}')
arm=arms[0]; bones=set(arm.data.bones.keys()); missing=sorted(REQ_BONES-bones)
if missing: raise RuntimeError('Missing rig bones: '+', '.join(missing))
meshes=[o for o in sc.objects if o.type=='MESH']; triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<40 or triangles<4000: raise RuntimeError(f'Creature detail regression: meshes={len(meshes)} triangles={triangles}')
for o in meshes:
    uv=o.data.uv_layers.get('SporelingUV')
    if not uv or len(uv.data)!=len(o.data.loops): raise RuntimeError(f'{o.name}: authored UV layer missing/incomplete')
    coords=[tuple(x.uv) for x in uv.data]
    if not coords or max(u for u,v in coords)-min(u for u,v in coords)<.01 or max(v for u,v in coords)-min(v for u,v in coords)<.01: raise RuntimeError(f'{o.name}: degenerate UV coverage')
    mods=[m for m in o.modifiers if m.type=='ARMATURE' and m.object==arm]
    if len(mods)!=1: raise RuntimeError(f'{o.name}: expected exactly one Sporeling armature modifier')
    groups={g.name for g in o.vertex_groups}
    if not groups or groups-bones: raise RuntimeError(f'{o.name}: invalid skin groups {sorted(groups-bones)}')
materials={m.name:m for m in bpy.data.materials if m.name in REQ_MATERIALS}
if REQ_MATERIALS-set(materials): raise RuntimeError('Missing creature materials')
for name,m in materials.items():
    nodes=list(m.node_tree.nodes); images=[n for n in nodes if n.type=='TEX_IMAGE' and n.image]
    if len(images)<3 or not any(n.type=='NORMAL_MAP' for n in nodes): raise RuntimeError(f'{name}: incomplete PBR material')
    tc=[n for n in nodes if n.type=='TEX_COORD']
    if not tc: raise RuntimeError(f'{name}: UV coordinate source missing')
for name in ('SporelingGill','SporelingSporeSac'):
    if len([n for n in materials[name].node_tree.nodes if n.type=='TEX_IMAGE' and n.image])<4: raise RuntimeError(f'{name}: localized emission missing')
REQ_ACTIONS={'Sporeling_Idle':72,'Sporeling_Walk':32,'Sporeling_Scuttle':24,'Sporeling_Turn':24,'Sporeling_Alert':30,'Sporeling_Bite':22,'Sporeling_Hit':16,'Sporeling_Stagger':28,'Sporeling_DeathSporePuff':52,'Sporeling_Emerge':42}
def action_fcurves(act):
    """Blender 4.4 moved an action's F-Curves into slotted channelbags; 5.0 removed Action.fcurves."""
    legacy=getattr(act,'fcurves',None)
    if legacy is not None: return list(legacy)
    curves=[]
    for layer in act.layers:
        for strip in layer.strips:
            for bag in getattr(strip,'channelbags',()): curves.extend(bag.fcurves)
    return curves

# REQ_ACTIONS is a dict, and `dict - set` is a TypeError: this block had never run, because the
# fidelity check above always raised first on a stale .blend.
actions={a.name:a for a in bpy.data.actions}; missing_actions=set(REQ_ACTIONS)-set(actions)
if missing_actions: raise RuntimeError('Missing authored actions: '+', '.join(sorted(missing_actions)))
for name,end in REQ_ACTIONS.items():
    a=actions[name]
    if int(a.frame_start)!=1 or int(a.frame_end)!=end or not action_fcurves(a): raise RuntimeError(f'{name}: invalid authored action')
pts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]; span=[max(p[i] for p in pts)-min(p[i] for p in pts) for i in range(3)]
if max(span)>.80 or span[2]<.30: raise RuntimeError(f'Unexpected creature bounds: {span}')
print(f'VERIFIED Sporeling r4 source gate: meshes={len(meshes)} triangles={triangles} bones={len(bones)} actions={len(REQ_ACTIONS)} UV=SporelingUV textures={len(REQ_TEX)}',flush=True)
