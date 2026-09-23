#!/usr/bin/env python3
"""Verify Cinder Hound source anatomy and production animation fidelity in Blender.

Art acceptance only. Valheim remains authoritative for world-space locomotion,
navigation, AI, networking, persistence, damage and gameplay state.
"""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-cinder-hound.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing Cinder Hound model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=bpy.data.objects.get('RIG_CinderHound_HOST_QUADRUPED')
if not arm or arm.type!='ARMATURE': raise RuntimeError('Cinder Hound production armature missing')
if arm.get('host_family')!='HOST-QUADRUPED': raise RuntimeError(f"Wrong host family: {arm.get('host_family')!r}")
shoulder=float(arm.get('production_shoulder_target_m',0))
if not 1.1<=shoulder<=1.4: raise RuntimeError(f'Cinder Hound outside 1.1-1.4m shoulder band: {shoulder}')

LEGS=('FL','FR','HL','HR'); SEGS=('Upper','Lower','Hock','Foot')
LIMBS=[f'{tag}_{seg}' for tag in LEGS for seg in SEGS]
REQUIRED=['Root','Pelvis','Spine','Chest','Neck','Head','Jaw','Tail_1','Tail_2','Tail_3',*LIMBS,'AttackOrigin','MouthFX','RibVentFX','HitCenter','PackCallFX']
for name in REQUIRED:
    if name not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {name}')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<70: raise RuntimeError(f'Cinder Hound anatomy too sparse: {len(meshes)} meshes < 70')
polys=sum(len(o.data.polygons) for o in meshes)
if polys<12000: raise RuntimeError(f'Cinder Hound source fidelity too low: {polys} polygons < 12000')
if any(not o.data.uv_layers.get('CinderHoundUV') for o in meshes): raise RuntimeError('Cinder Hound mesh missing CinderHoundUV')
bound=sum(1 for o in meshes if any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers))
if bound!=len(meshes): raise RuntimeError(f'Unbound Cinder Hound anatomy: {bound}/{len(meshes)} meshes')
if sum(1 for o in meshes if o.name.startswith('CH_VentMouth_'))!=12: raise RuntimeError('Physical rib-vent count regression')
if sum(1 for o in meshes if o.name.startswith('CH_DorsalScute_'))!=5: raise RuntimeError('Broken dorsal scute count regression')
if sum(1 for o in meshes if o.name.startswith('CH_Tooth_'))!=16: raise RuntimeError('Modeled tooth count regression')
if bpy.data.objects.get('CH_ThroatVent') is None: raise RuntimeError('Physical throat vent missing')

EXPECTED={'Idle':48,'VentIdle':64,'Walk':32,'PackSprint':20,'TurnLeft':24,'TurnRight':24,'Bite':18,'Lunge':24,'PackCall':42,'Hit':16,'Stagger':28,'Death':54}
for suffix,end in EXPECTED.items():
    act=bpy.data.actions.get('CH_'+suffix)
    if not act: raise RuntimeError(f'Missing Cinder Hound action: {suffix}')
    if int(round(act.frame_end))!=end: raise RuntimeError(f'{act.name}: frame contract {act.frame_end} != {end}')

def fcurves(act):
    direct=getattr(act,'fcurves',None)
    if direct is not None: return list(direct)
    curves=[]
    for layer in getattr(act,'layers',[]):
        for strip in getattr(layer,'strips',[]):
            bag=getattr(strip,'channelbag',None)
            if bag: curves.extend(list(bag.fcurves))
            for bag in getattr(strip,'channelbags',[]): curves.extend(list(bag.fcurves))
    return curves

def values(act,bone,axis=None,channel='rotation_euler'):
    token=f'pose.bones["{bone}"].{channel}'; out=[]
    for fc in fcurves(act):
        if token in fc.data_path and (axis is None or fc.array_index==axis): out += [kp.co.y for kp in fc.keyframe_points]
    return out

def span(act,bone,axis=None):
    v=values(act,bone,axis); return max(v)-min(v) if v else 0.0

def peak(act,bone,axis=None): return max((abs(x) for x in values(act,bone,axis)),default=0.0)

for suffix in EXPECTED:
    for fc in fcurves(bpy.data.actions['CH_'+suffix]):
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'CH_{suffix}: Root translation forbidden')

walk=bpy.data.actions['CH_Walk']; sprint=bpy.data.actions['CH_PackSprint']
for bone in LIMBS:
    if span(walk,bone,0)<.08: raise RuntimeError(f'Walk under-articulates {bone}')
    if span(sprint,bone,0)<.16: raise RuntimeError(f'PackSprint under-articulates {bone}')
walk_total=sum(span(walk,b,0) for b in LIMBS); sprint_total=sum(span(sprint,b,0) for b in LIMBS)
if sprint_total < walk_total*1.65: raise RuntimeError('PackSprint is not materially larger than Walk')
# Diagonal quadruped phasing: FL/HR oppose FR/HL at the upper limb.
for act in (walk,sprint):
    a=(values(act,'FL_Upper',0),values(act,'HR_Upper',0)); b=(values(act,'FR_Upper',0),values(act,'HL_Upper',0))
    if any(not v for v in (*a,*b)): raise RuntimeError(f'{act.name}: incomplete diagonal gait curves')
    if a[0][0]*b[0][0]>=0 or a[1][0]*b[1][0]>=0: raise RuntimeError(f'{act.name}: diagonal opposition lost')
# Faster gait must demand materially greater segmented tail counterbalance.
if sum(span(sprint,b,2) for b in ('Tail_1','Tail_2','Tail_3')) < sum(span(walk,b,2) for b in ('Tail_1','Tail_2','Tail_3'))*1.65:
    raise RuntimeError('PackSprint tail counterbalance does not scale with gait')

vent=bpy.data.actions['CH_VentIdle']
if span(vent,'Chest',0)<.12: raise RuntimeError('VentIdle lacks chest heat-dump expansion')
if span(vent,'Jaw',0)<.35 or peak(vent,'Head',0)<.14: raise RuntimeError('VentIdle fails to present throat/mouth vent anatomy')

left=bpy.data.actions['CH_TurnLeft']; right=bpy.data.actions['CH_TurnRight']
if max(values(left,'Chest',2) or [0])<.20 or min(values(right,'Chest',2) or [0])>-.20: raise RuntimeError('Turns lack opposite chest commitment')
if max(values(left,'Head',2) or [0])<.16 or min(values(right,'Head',2) or [0])>-.16: raise RuntimeError('Turns lack opposite head commitment')
if peak(left,'FR_Upper',0)<.30 or peak(right,'FL_Upper',0)<.30: raise RuntimeError('Turns lack outside-leg reach')

bite=bpy.data.actions['CH_Bite']
if span(bite,'Jaw',0)<.65: raise RuntimeError('Bite jaw snap below threshold')
if span(bite,'Neck',0)<.35 or span(bite,'Chest',0)<.18: raise RuntimeError('Bite is floating-mouth motion instead of anterior-body attack')
lunge=bpy.data.actions['CH_Lunge']
for b in ('HL_Upper','HR_Upper','HL_Hock','HR_Hock','FL_Upper','FR_Upper','FL_Hock','FR_Hock'):
    if span(lunge,b,0)<.40: raise RuntimeError(f'Lunge lacks full digitigrade commitment at {b}')
if span(lunge,'Chest',0)<.45 or span(lunge,'Neck',0)<.28: raise RuntimeError('Lunge lacks whole-anterior-body compression/recoil')

call=bpy.data.actions['CH_PackCall']
if peak(call,'Neck',0)<.45 or span(call,'Jaw',0)<.40 or span(call,'Chest',0)<.15: raise RuntimeError('PackCall fails to present throat/vent anatomy')
hit=bpy.data.actions['CH_Hit']; stagger=bpy.data.actions['CH_Stagger']; death=bpy.data.actions['CH_Death']
if peak(hit,'Chest')>=peak(stagger,'Chest'): raise RuntimeError('Hit reaction must remain lighter than Stagger')
if peak(death,'Pelvis',2)<1.35 or peak(death,'Chest',2)<1.0: raise RuntimeError('Death lacks committed lateral collapse')
for b in ('FL_Upper','FR_Upper','HL_Upper','HR_Upper'):
    if peak(death,b,0)<.60: raise RuntimeError(f'Death lacks limb collapse at {b}')

fidelity=arm.get('animation_fidelity','')
for token in ('diagonal quadruped gait','compressed digitigrade pack sprint','segmented tail counterbalance','physical heat-vent idle','jaw-led bite','full-body in-place lunge','throat-presenting pack call'):
    if token not in fidelity: raise RuntimeError(f'Missing animation fidelity contract token: {token}')
if 'Root translation reserved for Valheim runtime' not in arm.get('root_motion_policy',''): raise RuntimeError('Root-motion ownership contract missing')
print(f'VERIFIED Cinder Hound production fidelity: shoulder={shoulder:.2f}m meshes={len(meshes)} polygons={polys} bound={bound} actions={len(EXPECTED)}; diagonal gait, distinct digitigrade pack sprint, scaled tail counterbalance, physical heat venting, mirrored turns, bite/lunge/call anatomy, reactions and collapse, no Root translation',flush=True)
