#!/usr/bin/env python3
"""Verify Lantern Moth custom-body source; this gate intentionally rejects Bat-like stand-ins."""
from pathlib import Path
from PIL import Image
import bpy
ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-lantern-moth.blend'
TEX=ROOT/'assets/textures/underworld/creatures/lantern-moth'
if not MODEL.exists(): raise RuntimeError(f'Missing {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=bpy.data.objects.get('RIG_LanternMoth_HOST_INSECT_FLY')
if not arm or arm.get('host_family')!='HOST-INSECT-FLY': raise RuntimeError('Lantern Moth HOST-INSECT-FLY rig missing')
span=float(arm.get('production_span_m',0))
if not .45<=span<=.70: raise RuntimeError(f'Lantern Moth span invalid: {span}')
if 'never-bat-body' not in arm.get('silhouette_contract',''): raise RuntimeError('Bat-replacement silhouette contract missing')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
wings=[o for o in meshes if o.name.startswith('LM_ForeWing_') or o.name.startswith('LM_HindWing_')]
if len(wings)!=4: raise RuntimeError(f'Expected four physical wing surfaces, found {len(wings)}')
if sum(1 for o in meshes if o.name.startswith('LM_ThoraxTuft_'))<10: raise RuntimeError('Fuzzy thorax silhouette lost')
if sum(1 for o in meshes if o.name.startswith('LM_Antenna'))!=4: raise RuntimeError('Two articulated antenna chains required')
if sum(1 for o in meshes if o.name.startswith('LM_Leg'))!=12: raise RuntimeError('Six two-segment legs required')
if any(not o.data.uv_layers.get('LanternMothUV') for o in meshes): raise RuntimeError('Explicit LanternMothUV missing')
if any(not any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers) for o in meshes): raise RuntimeError('Unbound Lantern Moth anatomy')
for bone in ('Root','Thorax','Abdomen','Head','ForeWing_L','ForeWing_R','HindWing_L','HindWing_R','Antenna1_L','Antenna1_R','Antenna2_L','Antenna2_R','LightFX','HitCenter'):
    if bone not in arm.data.bones: raise RuntimeError(f'Missing bone/socket {bone}')
expected={'Hover':24,'Flight':16,'BankLeft':20,'BankRight':20,'Takeoff':28,'Land':32,'GroundIdle':56,'AlertFlee':20,'Hit':14,'Death':42}
for suffix,end in expected.items():
    act=bpy.data.actions.get('LanternMoth_'+suffix)
    if not act or int(round(act.frame_end))!=end: raise RuntimeError(f'Action contract failed: {suffix}')
for stem in ('thorax-fuzz','abdomen-chitin','wing-membrane','antenna','eye'):
    for kind in ('albedo','roughness','normal'):
        p=TEX/f'{stem}-{kind}.png'
        if not p.exists() or Image.open(p).size!=(1024,1024): raise RuntimeError(f'Invalid texture {p}')
em=TEX/'wing-membrane-emission.png'
if not em.exists() or Image.open(em).size!=(1024,1024): raise RuntimeError('Wing emission mask missing')
print(f'VERIFIED Lantern Moth custom-body source: {span:.3f}m span, four wings, fuzzy thorax, paired antennae, six legs, {len(expected)} actions; donor-Bat silhouette rejected')
