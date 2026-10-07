#!/usr/bin/env python3
"""Verify Lantern Moth custom-body source; this gate intentionally rejects Bat-like stand-ins."""
from pathlib import Path
import struct
import bpy
ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-lantern-moth.blend'
TEX=ROOT/'assets/textures/underworld/creatures/lantern-moth'
def png_size(path):
    data=path.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n' or data[12:16] != b'IHDR':
        raise RuntimeError(f'Invalid PNG header: {path}')
    return struct.unpack('>II', data[16:24])
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
veins=[o for o in meshes if o.name.startswith('LM_WingVein_')]
if len(veins)!=24: raise RuntimeError(f'Expected 24 physical wing vein relief segments, found {len(veins)}')
for wing in wings:
    mesh=wing.data
    if len(mesh.vertices)!=12 or len(mesh.polygons)!=14:
        raise RuntimeError(f'{wing.name}: closed two-sided membrane topology lost')
    if any(abs(mesh.vertices[i].co.z-mesh.vertices[i+6].co.z-.003)>1e-5 for i in range(6)):
        raise RuntimeError(f'{wing.name}: 3 mm membrane rim lost')
    if sum(p.normal.z>.5 for p in mesh.polygons)!=4 or sum(p.normal.z<-.5 for p in mesh.polygons)!=4:
        raise RuntimeError(f'{wing.name}: dorsal/ventral winding is not outward')
    edges={}
    for poly in mesh.polygons:
        indices=list(poly.vertices)
        for i,a in enumerate(indices):
            key=tuple(sorted((a,indices[(i+1)%len(indices)])))
            edges[key]=edges.get(key,0)+1
        if poly.area<=1e-8: raise RuntimeError(f'{wing.name}: degenerate membrane/rim polygon')
    if any(count!=2 for count in edges.values()):
        raise RuntimeError(f'{wing.name}: wing membrane has open or nonmanifold edges')
    if any(not (0<=uv.uv.x<=1 and 0<=uv.uv.y<=1) for uv in mesh.uv_layers['LanternMothUV'].data):
        raise RuntimeError(f'{wing.name}: membrane/rim UV escaped authored texture')
for side in ('L','R'):
    for kind in ('Fore','Hind'):
        group=[o for o in veins if o.name.startswith(f'LM_WingVein_{side}_{kind}_')]
        if len(group)!=6: raise RuntimeError(f'{side} {kind} wing relief incomplete')
        bone=f'{kind}Wing_{side}'
        for part in group:
            vg=part.vertex_groups.get(bone)
            if vg is None or any(not any(g.group==vg.index and g.weight>=.999 for g in v.groups) for v in part.data.vertices):
                raise RuntimeError(f'{part.name}: vein relief is not rigidly bound to its wing bone')
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
        if not p.exists() or png_size(p)!=(1024,1024): raise RuntimeError(f'Invalid texture {p}')
em=TEX/'wing-membrane-emission.png'
if not em.exists() or png_size(em)!=(1024,1024): raise RuntimeError('Wing emission mask missing')
print(f'VERIFIED Lantern Moth custom-body source: {span:.3f}m span, four closed two-sided wings, 24 raised vein segments, fuzzy thorax, paired antennae, six legs, {len(expected)} actions; donor-Bat silhouette rejected')
