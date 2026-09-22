#!/usr/bin/env python3
"""Verify Shoreclaw source anatomy and amphibious animation fidelity in Blender.

This is an art acceptance gate only. It deliberately does not implement movement, swimming,
AI, attachment, networking, persistence, or any other gameplay authority owned by Valheim.
"""
from pathlib import Path
import math
import bpy

ROOT = Path(__file__).resolve().parents[1]
MODEL = ROOT / 'assets/models/source/underworld-creature-shoreclaw.blend'
if not MODEL.exists():
    raise RuntimeError(f'Missing Shoreclaw model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))

arm = next((o for o in bpy.context.scene.objects if o.type == 'ARMATURE'), None)
if not arm or arm.get('magenheim_asset') != 'shoreclaw':
    raise RuntimeError('Shoreclaw armature identity missing')
if arm.get('host_rig') != 'HOST-AMPHIB-ARMORED':
    raise RuntimeError(f"Wrong host rig: {arm.get('host_rig')!r}")

LEGS = [f'Leg_{s}_{n}' for s in ('L', 'R') for n in range(1, 5)]
SHELLS = [f'ShellPlate_{i}' for i in range(1, 5)]
REQUIRED_BONES = ['Root', 'Body', *SHELLS]
for leg in LEGS:
    REQUIRED_BONES += [leg + '_Upper', leg + '_Lower', leg + '_Foot']
REQUIRED_BONES += ['Claw_L_Arm', 'Claw_L_Palm', 'Claw_R_Arm', 'Claw_R_Palm']
for name in REQUIRED_BONES:
    if name not in arm.data.bones:
        raise RuntimeError(f'Missing production bone: {name}')

meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
if len(meshes) < 55:
    raise RuntimeError(f'Shoreclaw anatomy too sparse: {len(meshes)} meshes < 55')
triangles = sum(sum(max(0, len(p.vertices)-2) for p in o.data.polygons) for o in meshes)
if triangles < 11000:
    raise RuntimeError(f'Shoreclaw source fidelity too low: {triangles} triangles < 11000')
if any(not o.data.uv_layers for o in meshes):
    bad = [o.name for o in meshes if not o.data.uv_layers][:8]
    raise RuntimeError(f'Meshes missing UVs: {bad}')

# Bound meshes must actually be armature-driven; loose decorative geometry is not enough.
bound = 0
for o in meshes:
    if any(m.type == 'ARMATURE' and m.object == arm for m in o.modifiers):
        bound += 1
if bound < 55:
    raise RuntimeError(f'Insufficient armature-bound anatomy: {bound}/55 minimum')

required_actions = {
    'Shoreclaw_Idle':72, 'Shoreclaw_Scuttle':32,
    'Shoreclaw_TurnLeft':28, 'Shoreclaw_TurnRight':28,
    'Shoreclaw_CrusherAttack':38, 'Shoreclaw_CutterAttack':24,
    'Shoreclaw_Guard':42, 'Shoreclaw_SwimIdle':64,
    'Shoreclaw_SwimForward':40, 'Shoreclaw_WaterExit':48,
    'Shoreclaw_WaterEntry':44, 'Shoreclaw_Hit':18,
    'Shoreclaw_Stagger':34, 'Shoreclaw_Death':72,
}
for name, end in required_actions.items():
    act = bpy.data.actions.get(name)
    if not act:
        raise RuntimeError(f'Missing Shoreclaw action: {name}')
    if int(round(act.frame_end)) != end:
        raise RuntimeError(f'{name}: frame contract {act.frame_end} != {end}')

# Blender 4.4+ may store F-curves in slotted channelbags. Normalize both layouts.
def fcurves(act):
    direct = getattr(act, 'fcurves', None)
    if direct is not None:
        return list(direct)
    curves = []
    for layer in getattr(act, 'layers', []):
        for strip in getattr(layer, 'strips', []):
            bag = getattr(strip, 'channelbag', None)
            if bag:
                curves.extend(list(bag.fcurves))
            for bag in getattr(strip, 'channelbags', []):
                curves.extend(list(bag.fcurves))
    return curves

def bone_curves(act, bone):
    token = f'pose.bones["{bone}"]'
    return [fc for fc in fcurves(act) if token in fc.data_path]

def span(act, bone):
    vals=[]
    for fc in bone_curves(act,bone):
        vals += [kp.co.y for kp in fc.keyframe_points]
    return (max(vals)-min(vals)) if vals else 0.0

def foot_x_span(action_name, leg):
    act=bpy.data.actions[action_name]
    token=f'pose.bones["{leg}_Foot"].rotation_euler'
    vals=[]
    for fc in fcurves(act):
        if token in fc.data_path and fc.array_index == 0:
            vals += [kp.co.y for kp in fc.keyframe_points]
    return (max(vals)-min(vals)) if vals else 0.0

# No animation may steal world-space locomotion from Valheim.
for name in required_actions:
    act=bpy.data.actions[name]
    for fc in fcurves(act):
        if 'pose.bones["Root"].location' in fc.data_path:
            raise RuntimeError(f'{name}: Root translation is forbidden')

# All eight legs must contribute visibly to terrestrial locomotion.
scuttle=bpy.data.actions['Shoreclaw_Scuttle']
for leg in LEGS:
    for suffix in ('_Upper','_Lower','_Foot'):
        b=leg+suffix
        if span(scuttle,b) < 0.10:
            raise RuntimeError(f'Scuttle under-animates {b}: span={span(scuttle,b):.3f}')

# Swimming must be a different locomotion language: terminal paddles pitch far more than walking.
for leg in LEGS:
    land_span=foot_x_span('Shoreclaw_Scuttle',leg)
    swim_span=foot_x_span('Shoreclaw_SwimForward',leg)
    if swim_span < 0.70:
        raise RuntimeError(f'Swim paddle articulation too weak on {leg}: {swim_span:.3f}')
    if swim_span < land_span * 1.8:
        raise RuntimeError(f'{leg}: swim paddle motion not materially distinct from scuttle ({swim_span:.3f} vs {land_span:.3f})')

# Each principal armor plate must move independently in both locomotion modes.
for action_name in ('Shoreclaw_Scuttle','Shoreclaw_SwimForward'):
    act=bpy.data.actions[action_name]
    plate_spans=[]
    for shell in SHELLS:
        s=span(act,shell)
        if s < 0.025:
            raise RuntimeError(f'{action_name}: shell plate {shell} appears rigid ({s:.3f})')
        plate_spans.append(round(s,4))
    if len(set(plate_spans)) < 2:
        raise RuntimeError(f'{action_name}: shell plates move as a single rigid slab')

# Weapon asymmetry is functional as well as visual: crusher is slower/heavier; cutter is quicker.
crusher=bpy.data.actions['Shoreclaw_CrusherAttack']
cutter=bpy.data.actions['Shoreclaw_CutterAttack']
if span(crusher,'Claw_L_Arm') < 0.55 or span(crusher,'Claw_L_Palm') < 0.45:
    raise RuntimeError('Crusher attack lacks heavy left-claw articulation')
if span(cutter,'Claw_R_Arm') < 0.45 or span(cutter,'Claw_R_Palm') < 0.35:
    raise RuntimeError('Cutter attack lacks right-claw snap articulation')
if crusher.frame_end <= cutter.frame_end + 8:
    raise RuntimeError('Crusher/cutter timing is insufficiently asymmetric')

# Entry/exit must contain authored motion in every leg and shell, not merely a body tilt.
for action_name in ('Shoreclaw_WaterEntry','Shoreclaw_WaterExit'):
    act=bpy.data.actions[action_name]
    for leg in LEGS:
        if span(act,leg+'_Foot') < 0.20:
            raise RuntimeError(f'{action_name}: {leg} does not transition paddle/foot pose')
    for shell in SHELLS:
        if span(act,shell) < 0.02:
            raise RuntimeError(f'{action_name}: {shell} lacks transition response')

contract=arm.get('animation_readability_contract','')
for token in ('eight-leg-land-weight-transfer','distinct-paddle-swim-strokes','independent-shell-lag','asymmetric-claw-attacks','blended-water-transitions','no-root-translation'):
    if token not in contract:
        raise RuntimeError(f'Missing animation readability contract token: {token}')

print(f'VERIFIED Shoreclaw production fidelity: meshes={len(meshes)} triangles={triangles} bound={bound} actions={len(required_actions)}; eight-leg land/swim distinction, shell independence, claw asymmetry, transitions, no Root translation', flush=True)
