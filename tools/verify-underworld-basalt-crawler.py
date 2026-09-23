#!/usr/bin/env python3
"""Verify Basalt Crawler source anatomy and production animation fidelity in Blender.

Art acceptance only. Valheim remains authoritative for world-space locomotion,
navigation, AI, networking, persistence, damage and gameplay state.
"""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-basalt-crawler.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing Basalt Crawler model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=bpy.data.objects.get('RIG_BasaltCrawler_HOST_LOW_CRAWLER')
if not arm or arm.type!='ARMATURE': raise RuntimeError('Basalt Crawler production armature missing')
if arm.get('host_family')!='HOST-LOW-CRAWLER': raise RuntimeError(f"Wrong host family: {arm.get('host_family')!r}")
length=float(arm.get('production_length_target_m',0))
if not 1.0<=length<=1.5: raise RuntimeError(f'Basalt Crawler outside 1.0-1.5m body band: {length}')

LEGS=tuple(f'{s}{i}' for s in ('L','R') for i in (1,2,3)); SEGS=('Upper','Lower','Foot')
LIMBS=[f'{tag}_{seg}' for tag in LEGS for seg in SEGS]
REQUIRED=['Root','Body','Abdomen','Head','Brow','Mandible_L','Mandible_R',*LIMBS,'AttackOrigin','RamOrigin','HitCenter']
for name in REQUIRED:
    if name not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {name}')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<55: raise RuntimeError(f'Basalt Crawler anatomy too sparse: {len(meshes)} meshes < 55')
polys=sum(len(o.data.polygons) for o in meshes)
if polys<8000: raise RuntimeError(f'Basalt Crawler source fidelity too low: {polys} polygons < 8000')
if any(not o.data.uv_layers.get('BasaltCrawlerUV') for o in meshes): raise RuntimeError('Basalt Crawler mesh missing BasaltCrawlerUV')
bound=sum(1 for o in meshes if any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers))
if bound!=len(meshes): raise RuntimeError(f'Unbound Basalt Crawler anatomy: {bound}/{len(meshes)} meshes')
if sum(1 for o in meshes if o.name.startswith('BC_Plate_'))!=7: raise RuntimeError('Physical armor-plate count regression')
if sum(1 for o in meshes if o.name.startswith('BC_Keel_'))!=7: raise RuntimeError('Armor fracture-keel count regression')
if sum(1 for o in meshes if o.name.startswith('BC_RamBoss_'))!=2: raise RuntimeError('Paired physical ram bosses missing')
if sum(1 for o in meshes if '_Toe_' in o.name)!=18: raise RuntimeError('Modeled toe count regression')
if bpy.data.objects.get('BC_BrowShield') is None: raise RuntimeError('Physical articulated brow shield missing')

EXPECTED={'Idle':48,'Scuttle':28,'TurnLeft':24,'TurnRight':24,'AttackLeft':22,'AttackRight':22,'FrontStrike':20,'Guard':32,'Ram':30,'Hit':16,'Stagger':30,'Death':56}
for suffix,end in EXPECTED.items():
    act=bpy.data.actions.get('BC_'+suffix)
    if not act: raise RuntimeError(f'Missing Basalt Crawler action: {suffix}')
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
    for fc in fcurves(bpy.data.actions['BC_'+suffix]):
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'BC_{suffix}: Root translation forbidden')

# Scuttle must use every stage of every leg, with the authored opposed tripod groups.
sc=bpy.data.actions['BC_Scuttle']
for bone in LIMBS:
    if span(sc,bone,0)<.12: raise RuntimeError(f'Scuttle under-articulates {bone}')
tripod_a=('L1_Upper','R2_Upper','L3_Upper'); tripod_b=('R1_Upper','L2_Upper','R3_Upper')
for a,b in zip(tripod_a,tripod_b):
    av=values(sc,a,0); bv=values(sc,b,0)
    if not av or not bv: raise RuntimeError('Scuttle tripod curves incomplete')
    # Compare first non-neutral authored sample; groups must carry opposite signs.
    pair=next(((x,y) for x,y in zip(av,bv) if abs(x)>.02 and abs(y)>.02),None)
    if pair is None or pair[0]*pair[1]>=0: raise RuntimeError(f'Scuttle tripod opposition lost at {a}/{b}')

# Heavy pivots must mirror body/head/brow commitment and reach with the outside front leg.
left=bpy.data.actions['BC_TurnLeft']; right=bpy.data.actions['BC_TurnRight']
if max(values(left,'Body',2) or [0])<.24 or min(values(right,'Body',2) or [0])>-.24: raise RuntimeError('Turns lack opposite shell commitment')
if max(values(left,'Head',2) or [0])<.17 or min(values(right,'Head',2) or [0])>-.17: raise RuntimeError('Turns lack opposite head commitment')
if peak(left,'R1_Upper',0)<.40 or peak(right,'L1_Upper',0)<.40: raise RuntimeError('Turns lack outside-front-leg reach')

# Guard is a physical shutter: brow closes while both front legs form a brace.
guard=bpy.data.actions['BC_Guard']
if span(guard,'Brow',0)<.38: raise RuntimeError('Guard does not materially close articulated brow')
for b in ('L1_Upper','R1_Upper','L1_Lower','R1_Lower','L1_Foot','R1_Foot'):
    if span(guard,b,0)<.20: raise RuntimeError(f'Guard lacks frontal brace at {b}')
if span(guard,'Head',0)<.14 or span(guard,'Body',0)<.07: raise RuntimeError('Guard is a floating brow instead of whole-anterior-body defense')

# Ram must use rear compression, front impact bracing and the brow/body, while staying in place.
ram=bpy.data.actions['BC_Ram']
for b in ('L3_Upper','R3_Upper','L3_Lower','R3_Lower'):
    if span(ram,b,0)<.55: raise RuntimeError(f'Ram lacks rear compression/extension at {b}')
for b in ('L1_Upper','R1_Upper','L1_Lower','R1_Lower','L1_Foot','R1_Foot'):
    if span(ram,b,0)<.20: raise RuntimeError(f'Ram lacks frontal impact brace at {b}')
if span(ram,'Brow',0)<.40 or span(ram,'Body',0)<.40: raise RuntimeError('Ram lacks brow/shell commitment')

# Lateral strikes must mirror; frontal strike must expose then protect the recessed face.
al=bpy.data.actions['BC_AttackLeft']; ar=bpy.data.actions['BC_AttackRight']; front=bpy.data.actions['BC_FrontStrike']
if min(values(al,'Body',2) or [0])>-.25 or max(values(ar,'Body',2) or [0])<.25: raise RuntimeError('Lateral attacks are not mirrored whole-shell strikes')
if peak(al,'L1_Upper',0)<.25 or peak(ar,'R1_Upper',0)<.25: raise RuntimeError('Lateral attacks lack same-side front-leg commitment')
if span(front,'Brow',0)<.28 or span(front,'Head',0)<.30: raise RuntimeError('FrontStrike fails to cycle brow/head exposure')
if span(front,'Mandible_L',2)<.60 or span(front,'Mandible_R',2)<.60: raise RuntimeError('FrontStrike lacks bilateral mandible snap')

hit=bpy.data.actions['BC_Hit']; stagger=bpy.data.actions['BC_Stagger']; death=bpy.data.actions['BC_Death']
if peak(hit,'Body')>=peak(stagger,'Body'): raise RuntimeError('Hit reaction must remain lighter than Stagger')
if peak(death,'Body',2)<1.40 or peak(death,'Abdomen',2)<.95: raise RuntimeError('Death lacks committed lithic flank collapse')
for b in LIMBS:
    if peak(death,b,0)<.30: raise RuntimeError(f'Death lacks progressive leg fold at {b}')

fidelity=arm.get('animation_fidelity','')
for token in ('alternating-tripod low scuttle','heavy mirrored pivots','whole-body lateral/frontal strikes','physical brow shutter guard','braced in-place ram','lithic flank collapse'):
    if token not in fidelity: raise RuntimeError(f'Missing animation fidelity contract token: {token}')
if 'Root translation reserved for Valheim runtime' not in arm.get('root_motion_policy',''): raise RuntimeError('Root-motion ownership contract missing')
print(f'VERIFIED Basalt Crawler production fidelity: length={length:.2f}m meshes={len(meshes)} polygons={polys} bound={bound} actions={len(EXPECTED)}; opposed tripod scuttle, mirrored heavy pivots/strikes, physical brow guard, braced ram, reaction hierarchy and lithic collapse, no Root translation',flush=True)
