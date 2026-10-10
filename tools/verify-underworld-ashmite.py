#!/usr/bin/env python3
"""Verify Ashmite source anatomy and production animation fidelity in Blender.

Art acceptance only. Valheim remains authoritative for world-space locomotion,
navigation, AI, networking, persistence, damage and gameplay state.
"""
from pathlib import Path
import bpy
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
import ashmite_scute_geometry as ashgeo
import ashmite_body_geometry as bodygeo
import ashmite_sulfur_ridge_geometry as ridgegeo
import ashmite_head_geometry as headgeo
import ashmite_mandible_geometry as mandgeo
import ashmite_scraper_geometry as scrapegeo
import ashmite_leg_geometry as leggeo
import ashmite_tarsal_geometry as tarsgeo
import ashmite_eye_geometry as eyegeo
import ashmite_joint_geometry as jointgeo

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-ashmite.blend'
if not MODEL.exists(): raise RuntimeError(f'Missing Ashmite model: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
arm=bpy.data.objects.get('RIG_Ashmite_HOST_SWARM_HEXAPOD')
if not arm or arm.type!='ARMATURE': raise RuntimeError('Ashmite production armature missing')
if arm.get('host_family')!='HOST-SWARM-HEXAPOD': raise RuntimeError(f"Wrong host family: {arm.get('host_family')!r}")
scale=float(arm.get('production_scale_m',0))
if not .25<=scale<=.45: raise RuntimeError(f'Ashmite outside 0.25-0.45m production band: {scale}')

LEGS=[f'{s}_{seg}{i}' for s in ('L','R') for i in range(1,4) for seg in ('Coxa','Femur','Tarsus')]
REQUIRED=['Root','Thorax','Abdomen','Head','Jaw','Mandible_L','Mandible_R',*LEGS,'AttackOrigin','ScavengeOrigin','HitCenter','SulfurFX']
for name in REQUIRED:
    if name not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {name}')

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<45: raise RuntimeError(f'Ashmite anatomy too sparse: {len(meshes)} meshes < 45')
polys=sum(len(o.data.polygons) for o in meshes)
if polys<6500: raise RuntimeError(f'Ashmite source fidelity too low: {polys} polygons < 6500')
if any(not o.data.uv_layers.get('AshmiteUV') for o in meshes): raise RuntimeError(f'Meshes missing AshmiteUV: {[o.name for o in meshes if not o.data.uv_layers.get("AshmiteUV")][:8]}')
bound=sum(1 for o in meshes if any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers))
if bound<len(meshes): raise RuntimeError(f'Unbound Ashmite anatomy: {bound}/{len(meshes)} meshes armature-bound')
for prefix,count in (('Ashmite_DorsalScute_',5),('Ashmite_SulfurRidge_',10),('Ashmite_L_Joint',3),('Ashmite_R_Joint',3)):
    found=sum(1 for o in meshes if o.name.startswith(prefix))
    if found!=count: raise RuntimeError(f'{prefix} physical anatomy regression: {found} != {count}')

# Six folded protected-joint membranes: fail on stale spheres, UVs or rig drift.
joint_vertices,joint_faces,joint_uvs=jointgeo.joint_mesh_data()
for pair in (1,2,3):
    for side,label in ((-1,'L'),(1,'R')):
        name=f'Ashmite_{label}_Joint{pair}'
        obj=bpy.data.objects.get(name)
        if not obj or obj.type!='MESH':
            raise RuntimeError(f'{name}: sculpted coxal membrane missing')
        if (obj.get('magenheim_ashmite_joint_contract')!=jointgeo.CONTRACT or
                obj.get('magenheim_ashmite_joint_side')!=side or
                obj.get('magenheim_ashmite_joint_pair')!=pair):
            raise RuntimeError(f'{name}: joint identity drift')
        if any(abs(obj.location[k]-jointgeo.anchor(side,pair)[k])>1e-6 for k in range(3)):
            raise RuntimeError(f'{name}: original hip anchor drift')
        if (not obj.data.materials or obj.data.materials[0].name!='Ashmite_ProtectedJoint' or
                not obj.vertex_groups.get(f'{label}_Coxa{pair}') or
                not any(m.type=='ARMATURE' and m.object==arm for m in obj.modifiers)):
            raise RuntimeError(f'{name}: protected joint material or Coxa binding drift')
        if len(obj.data.vertices)!=len(joint_vertices) or len(obj.data.polygons)!=len(joint_faces):
            raise RuntimeError(f'{name}: folded membrane topology drift')
        if any(any(abs(a-b)>2e-6 for a,b in zip(v.co,expected))
               for v,expected in zip(obj.data.vertices,joint_vertices)):
            raise RuntimeError(f'{name}: physical compression-fold drift')
        if any(tuple(p.vertices)!=tuple(f) for p,f in zip(obj.data.polygons,joint_faces)):
            raise RuntimeError(f'{name}: face winding drift')
        layer=obj.data.uv_layers.get('AshmiteUV')
        if not layer:
            raise RuntimeError(f'{name}: authored joint UV missing')
        for p,expected_uv in zip(obj.data.polygons,joint_uvs):
            if any(any(abs(a-b)>2e-5 for a,b in zip(layer.data[li].uv,uv))
                   for li,uv in zip(p.loop_indices,expected_uv)):
                raise RuntimeError(f'{name}: joint UV loop drift')

# Primary body envelopes: reject stale ico-spheres, topology, UV or rig drift.
for kind,name in (('thorax','Ashmite_Thorax'),('abdomen','Ashmite_Abdomen')):
    obj=bpy.data.objects.get(name)
    origin,_,bone=bodygeo.specification(kind)
    if not obj or obj.type!='MESH':
        raise RuntimeError(f'{name}: missing sculpted primary body')
    if (obj.get('magenheim_ashmite_body_contract')!=bodygeo.CONTRACT or
            obj.get('magenheim_ashmite_body_kind')!=kind):
        raise RuntimeError(f'{name}: primary body identity drift')
    if any(abs(obj.location[k]-origin[k])>1e-6 for k in range(3)):
        raise RuntimeError(f'{name}: original body anchor drift')
    if not obj.vertex_groups.get(bone) or not any(
            m.type=='ARMATURE' and m.object==arm for m in obj.modifiers):
        raise RuntimeError(f'{name}: original body rig binding drift')
    if not obj.data.materials or obj.data.materials[0].name!='Ashmite_HeatChitin':
        raise RuntimeError(f'{name}: body PBR material drift')
    verts,faces,uvs=bodygeo.body_mesh_data(kind)
    if len(obj.data.vertices)!=len(verts) or len(obj.data.polygons)!=len(faces):
        raise RuntimeError(f'{name}: body topology drift')
    if any(any(abs(a-b)>2e-6 for a,b in zip(actual.co,expected))
           for actual,expected in zip(obj.data.vertices,verts)):
        raise RuntimeError(f'{name}: body surface relief drift')
    if any(tuple(poly.vertices)!=tuple(face)
           for poly,face in zip(obj.data.polygons,faces)):
        raise RuntimeError(f'{name}: body face winding drift')
    layer=obj.data.uv_layers.get('AshmiteUV')
    if not layer:
        raise RuntimeError(f'{name}: missing authored body UV')
    for poly,expected_uvs in zip(obj.data.polygons,uvs):
        if any(any(abs(a-b)>2e-5 for a,b in zip(layer.data[li].uv,uv))
               for li,uv in zip(poly.loop_indices,expected_uvs)):
            raise RuntimeError(f'{name}: body UV loop drift')

# Sculpted shovel-shaped cephalic shield preserves existing eyes and feeding-tool rig.
head=bpy.data.objects.get('Ashmite_Head')
if not head or head.type!='MESH' or head.get('magenheim_ashmite_head_contract')!=headgeo.CONTRACT:
    raise RuntimeError('Ashmite sculpted cephalic shield missing')
if (len(head.data.vertices)!=headgeo.RINGS*headgeo.SIDES+2 or
        sum(len(p.vertices)-2 for p in head.data.polygons)!=2*headgeo.RINGS*headgeo.SIDES):
    raise RuntimeError('Ashmite cephalic shield topology drift')
if any(abs(head.location[k]-headgeo.ORIGIN[k])>1e-6 for k in range(3)):
    raise RuntimeError('Ashmite Head origin drift')
if not head.vertex_groups.get('Head') or not any(
        m.type=='ARMATURE' and m.object==arm for m in head.modifiers):
    raise RuntimeError('Ashmite Head bone binding drift')
if not head.data.materials or head.data.materials[0].name!='Ashmite_HeatChitin':
    raise RuntimeError('Ashmite cephalic shield PBR material drift')
head_uv=head.data.uv_layers.get('AshmiteUV')
if not head_uv or any(not 0<=c<=1 for item in head_uv.data for c in item.uv):
    raise RuntimeError('Ashmite cephalic shield authored UV drift')

# Paired sculpted eyes retain their original Head attachments and PBR materials.
# Fail closed on old generated sources: exact surface, winding and authored UVs.
for side,label in ((-1,'L'),(1,'R')):
    name=f'Ashmite_Eye_{label}'
    obj=bpy.data.objects.get(name)
    if not obj or obj.type!='MESH':
        raise RuntimeError(f'{name}: missing sculpted eye')
    if (obj.get('magenheim_ashmite_eye_contract')!=eyegeo.CONTRACT or
            obj.get('magenheim_ashmite_eye_side')!=side):
        raise RuntimeError(f'{name}: eye identity contract drift')
    origin=eyegeo.anchor(side)
    if any(abs(obj.location[k]-origin[k])>1e-6 for k in range(3)):
        raise RuntimeError(f'{name}: original eye position drift')
    if not obj.vertex_groups.get('Head') or not any(
            m.type=='ARMATURE' and m.object==arm for m in obj.modifiers):
        raise RuntimeError(f'{name}: Head bone attachment drift')
    if not obj.data.materials or obj.data.materials[0].name!='Ashmite_Eye':
        raise RuntimeError(f'{name}: eye PBR material drift')
    verts,faces,uvs=eyegeo.eye_mesh_data(side)
    if len(obj.data.vertices)!=len(verts) or len(obj.data.polygons)!=len(faces):
        raise RuntimeError(f'{name}: eye topology drift')
    if any(any(abs(a-b)>2e-6 for a,b in zip(actual.co,expected))
           for actual,expected in zip(obj.data.vertices,verts)):
        raise RuntimeError(f'{name}: sculpted eye surface relief drift')
    if any(tuple(poly.vertices)!=tuple(face)
           for poly,face in zip(obj.data.polygons,faces)):
        raise RuntimeError(f'{name}: eye face winding drift')
    layer=obj.data.uv_layers.get('AshmiteUV')
    if not layer:
        raise RuntimeError(f'{name}: missing authored eye UV')
    for poly,expected_uvs in zip(obj.data.polygons,uvs):
        if any(any(abs(a-b)>2e-5 for a,b in zip(layer.data[li].uv,uv))
               for li,uv in zip(poly.loop_indices,expected_uvs)):
            raise RuntimeError(f'{name}: authored eye UV drift')

# Five physical heat-scutella must retain their overlap silhouette, rig and
# three-island authored UVs; stale .blend assets must fail closed.
for i,y in enumerate((-.155,-.095,-.035,.030,.085),1):
    obj=bpy.data.objects.get(f'Ashmite_DorsalScute_{i}')
    if not obj or obj.type!='MESH':
        raise RuntimeError(f'Ashmite scute {i} missing')
    if (obj.get('magenheim_ashmite_scute_contract')!=ashgeo.CONTRACT or
            obj.get('magenheim_ashmite_scute_index')!=i or
            obj.get('magenheim_ashmite_scute_uv_contract')!=ashgeo.UV_CONTRACT):
        raise RuntimeError(f'Ashmite scute {i} geometry/UV contract drift')
    anchor=(0,y,.135-abs(y)*.10)
    if any(abs(obj.location[k]-anchor[k])>1e-6 for k in range(3)):
        raise RuntimeError(f'Ashmite scute {i} anchor drift')
    bone='Abdomen' if i<4 else 'Thorax'
    if not obj.vertex_groups.get(bone) or not any(
            m.type=='ARMATURE' and m.object==arm for m in obj.modifiers):
        raise RuntimeError(f'Ashmite scute {i} rig binding drift')
    if not obj.data.materials or obj.data.materials[0].name!='Ashmite_HeatChitin':
        raise RuntimeError(f'Ashmite scute {i} PBR material drift')
    rx=.092 if i<4 else .075
    verts,faces=ashgeo.scute_mesh_data(rx,.047,.018,i)
    if len(obj.data.vertices)!=len(verts) or len(obj.data.polygons)!=len(faces):
        raise RuntimeError(f'Ashmite scute {i} topology drift')
    if any(any(abs(a-b)>2e-6 for a,b in zip(actual.co,expected))
           for actual,expected in zip(obj.data.vertices,verts)):
        raise RuntimeError(f'Ashmite scute {i} physical overlap relief drift')
    if any(tuple(poly.vertices)!=tuple(face)
           for poly,face in zip(obj.data.polygons,faces)):
        raise RuntimeError(f'Ashmite scute {i} face winding drift')
    layer=obj.data.uv_layers.get('AshmiteUV')
    if not layer:
        raise RuntimeError(f'Ashmite scute {i} authored UV missing')
    expected_uvs=ashgeo.scute_face_uvs(verts,faces,rx,.047)
    for poly,uvs in zip(obj.data.polygons,expected_uvs):
        if any(any(abs(a-b)>2e-5 for a,b in zip(layer.data[li].uv,uv))
               for li,uv in zip(poly.loop_indices,uvs)):
            raise RuntimeError(f'Ashmite scute {i} UV island or seam drift')

# Ten sculpted sulfur accretions preserve the scute anchors, materials and rig.
for index in range(1,6):
    for side in (-1,1):
        label='L' if side<0 else 'R'
        name=f'Ashmite_SulfurRidge_{index}_{label}'
        obj=bpy.data.objects.get(name)
        if not obj or obj.type!='MESH':
            raise RuntimeError(f'{name}: missing sculpted sulfur ridge')
        if (obj.get('magenheim_ashmite_sulfur_ridge_contract')!=ridgegeo.CONTRACT or
                obj.get('magenheim_ashmite_sulfur_ridge_side')!=side or
                obj.get('magenheim_ashmite_sulfur_ridge_index')!=index):
            raise RuntimeError(f'{name}: sulfur ridge identity drift')
        anchor,_=ridgegeo.anchors(side,index)
        if any(abs(obj.location[k]-anchor[k])>1e-6 for k in range(3)):
            raise RuntimeError(f'{name}: original sulfur ridge anchor drift')
        if (len(obj.data.vertices)!=ridgegeo.RINGS*ridgegeo.SIDES+2 or
                sum(len(p.vertices)-2 for p in obj.data.polygons)!=2*ridgegeo.RINGS*ridgegeo.SIDES):
            raise RuntimeError(f'{name}: sculpted sulfur ridge topology drift')
        bone='Abdomen' if index<4 else 'Thorax'
        if not obj.vertex_groups.get(bone) or not any(
                m.type=='ARMATURE' and m.object==arm for m in obj.modifiers):
            raise RuntimeError(f'{name}: original bone binding drift')
        if not obj.data.materials or obj.data.materials[0].name!='Ashmite_SulfurCrust':
            raise RuntimeError(f'{name}: sulfur crust material drift')
        uv=obj.data.uv_layers.get('AshmiteUV')
        if not uv or any(not 0<=c<=1 for item in uv.data for c in item.uv):
            raise RuntimeError(f'{name}: authored sulfur ridge UV drift')

# Two sculpted feeding pincers retain existing sockets and bone assignments.
for side,label in ((-1,'L'),(1,'R')):
    obj=bpy.data.objects.get(f'Ashmite_Mandible_{label}')
    if not obj or obj.type!='MESH': raise RuntimeError(f'Ashmite mandible {label} missing')
    if obj.get('magenheim_ashmite_mandible_contract')!=mandgeo.CONTRACT:
        raise RuntimeError(f'Ashmite mandible {label} lacks sculpted geometry')
    if obj.get('magenheim_ashmite_mandible_side')!=side:
        raise RuntimeError(f'Ashmite mandible {label} side contract mismatch')
    if len(obj.data.vertices)!=mandgeo.RINGS*mandgeo.SIDES+2:
        raise RuntimeError(f'Ashmite mandible {label} vertex topology mismatch')
    tris=sum(len(p.vertices)-2 for p in obj.data.polygons)
    if tris!=2*mandgeo.RINGS*mandgeo.SIDES:
        raise RuntimeError(f'Ashmite mandible {label} triangle count mismatch: {tris}')
    root,_=mandgeo.anchors(side)
    if any(abs(obj.location[k]-root[k])>1e-6 for k in range(3)):
        raise RuntimeError(f'Ashmite mandible {label} root attachment drift')
    layer=obj.data.uv_layers.get('AshmiteUV')
    if not layer or any(not 0<=c<=1 for item in layer.data for c in item.uv):
        raise RuntimeError(f'Ashmite mandible {label} UVs missing or out of bounds')

# Ashmite's central vent scraper is an authored sulfurized mouth tool.
scraper=bpy.data.objects.get('Ashmite_VentScraper')
if not scraper or scraper.type!='MESH' or scraper.get('magenheim_ashmite_scraper_contract')!=scrapegeo.CONTRACT:
    raise RuntimeError('Ashmite vent scraper sculpted geometry missing')
if len(scraper.data.vertices)!=scrapegeo.RINGS*scrapegeo.SIDES+2 or sum(len(p.vertices)-2 for p in scraper.data.polygons)!=2*scrapegeo.RINGS*scrapegeo.SIDES:
    raise RuntimeError('Ashmite vent scraper topology drift')
if any(abs(scraper.location[k]-scrapegeo.ORIGIN[k])>1e-6 for k in range(3)):
    raise RuntimeError('Ashmite vent scraper Jaw anchor drift')
if not scraper.vertex_groups.get('Jaw') or not any(m.type=='ARMATURE' and m.object==arm for m in scraper.modifiers):
    raise RuntimeError('Ashmite vent scraper Jaw rig binding drift')
if not scraper.data.materials or scraper.data.materials[0].name!='Ashmite_SulfurCrust':
    raise RuntimeError('Ashmite vent scraper sulfur material drift')
uv=scraper.data.uv_layers.get('AshmiteUV')
if not uv or any(not 0<=c<=1 for item in uv.data for c in item.uv):
    raise RuntimeError('Ashmite vent scraper authored UV drift')

# Twelve sculpted heat-chitin femora and tibiae preserve the six-leg rig and joints.
for pair,y in enumerate((.065,0,-.070),1):
    for side in (-1,1):
        label='L' if side<0 else 'R'
        hip=(side*.068,y,.070)
        knee=(side*.125,y+(.020 if pair==1 else -.008),.048)
        ankle=(side*.165,y-.012,.018)
        for kind,start,bone in (('femur',hip,f'{label}_Coxa{pair}'),
                                ('tibia',knee,f'{label}_Femur{pair}')):
            name=f'Ashmite_{label}_{kind.capitalize()}{pair}'
            obj=bpy.data.objects.get(name)
            if not obj or obj.type!='MESH':
                raise RuntimeError(f'{name}: missing sculpted leg mesh')
            if (obj.get('magenheim_ashmite_leg_contract')!=leggeo.CONTRACT or
                    obj.get('magenheim_ashmite_leg_kind')!=kind):
                raise RuntimeError(f'{name}: sculpted leg identity drift')
            if (len(obj.data.vertices)!=leggeo.RINGS*leggeo.SIDES+2 or
                    sum(len(p.vertices)-2 for p in obj.data.polygons)!=2*leggeo.RINGS*leggeo.SIDES):
                raise RuntimeError(f'{name}: topology drift')
            if any(abs(obj.location[k]-start[k])>1e-6 for k in range(3)):
                raise RuntimeError(f'{name}: original joint anchor drift')
            if not obj.vertex_groups.get(bone) or not any(
                    m.type=='ARMATURE' and m.object==arm for m in obj.modifiers):
                raise RuntimeError(f'{name}: original bone binding drift')
            if not obj.data.materials or obj.data.materials[0].name!='Ashmite_HeatChitin':
                raise RuntimeError(f'{name}: heat-chitin material drift')
            uv=obj.data.uv_layers.get('AshmiteUV')
            if not uv or any(not 0<=c<=1 for item in uv.data for c in item.uv):
                raise RuntimeError(f'{name}: authored UV drift')

# The six protected tarsi and six backward-facing vent rasps are sculpted meshes.
# Preserve each Tarsus bone, original attachment, material, topology and authored UVs.
for pair in (1,2,3):
    for side in (-1,1):
        label='L' if side<0 else 'R'
        bone=f'{label}_Tarsus{pair}'
        for kind,part in (('tarsus','Tarsus'),('scraper','Scraper')):
            name=f'Ashmite_{label}_{part}{pair}'
            obj=bpy.data.objects.get(name)
            if not obj or obj.type!='MESH': raise RuntimeError(f'{name}: missing sculpted foot mesh')
            if (obj.get('magenheim_ashmite_tarsal_contract')!=tarsgeo.CONTRACT or
                    obj.get('magenheim_ashmite_tarsal_kind')!=kind or
                    obj.get('magenheim_ashmite_tarsal_pair')!=pair or
                    obj.get('magenheim_ashmite_tarsal_side')!=side):
                raise RuntimeError(f'{name}: sculpted foot identity drift')
            anchor,_=tarsgeo.anchors(side,pair,kind)
            if any(abs(obj.location[k]-anchor[k])>1e-6 for k in range(3)):
                raise RuntimeError(f'{name}: original attachment drift')
            if (len(obj.data.vertices)!=tarsgeo.RINGS[kind]*tarsgeo.SIDES+2 or
                    sum(len(p.vertices)-2 for p in obj.data.polygons)!=2*tarsgeo.RINGS[kind]*tarsgeo.SIDES):
                raise RuntimeError(f'{name}: sculpted foot topology drift')
            if not obj.vertex_groups.get(bone) or not any(
                    m.type=='ARMATURE' and m.object==arm for m in obj.modifiers):
                raise RuntimeError(f'{name}: Tarsus rig binding drift')
            expected_mat='Ashmite_ProtectedJoint' if kind=='tarsus' else 'Ashmite_Mouthpart'
            if not obj.data.materials or obj.data.materials[0].name!=expected_mat:
                raise RuntimeError(f'{name}: PBR material drift')
            layer=obj.data.uv_layers.get('AshmiteUV')
            if not layer or any(not 0<=c<=1 for item in layer.data for c in item.uv):
                raise RuntimeError(f'{name}: authored UV drift')

EXPECTED={'Idle':64,'Scavenge':52,'Walk':28,'Scuttle':16,'TurnLeft':22,'TurnRight':22,'Bite':18,'Hit':14,'Stagger':24,'Death':42}
for suffix,end in EXPECTED.items():
    act=bpy.data.actions.get('Ashmite_'+suffix)
    if not act: raise RuntimeError(f'Missing Ashmite action: {suffix}')
    if int(round(act.frame_end))!=end: raise RuntimeError(f'{act.name}: frame contract {act.frame_end} != {end}')

def fcurves(act):
    direct=getattr(act,'fcurves',None)
    if direct is not None: return list(direct)
    curves=[]
    for layer in getattr(act,'layers',[]):
        for strip in getattr(layer,'strips',[]):
            bag=getattr(strip,'channelbag',None)
            if bag is not None and hasattr(bag,"fcurves"): curves.extend(list(bag.fcurves))
            for bag in getattr(strip,'channelbags',[]): curves.extend(list(bag.fcurves))
    return curves

def values(act,bone,axis=None):
    token=f'pose.bones["{bone}"].rotation_euler'; out=[]
    for fc in fcurves(act):
        if token in fc.data_path and (axis is None or fc.array_index==axis): out += [kp.co.y for kp in fc.keyframe_points]
    return out

def span(act,bone,axis=None):
    v=values(act,bone,axis); return max(v)-min(v) if v else 0.0

def peak(act,bone,axis=None):
    return max((abs(x) for x in values(act,bone,axis)),default=0.0)

for suffix in EXPECTED:
    for fc in fcurves(bpy.data.actions['Ashmite_'+suffix]):
        if 'pose.bones["Root"].location' in fc.data_path: raise RuntimeError(f'Ashmite_{suffix}: Root translation forbidden')

walk=bpy.data.actions['Ashmite_Walk']; scuttle=bpy.data.actions['Ashmite_Scuttle']
# Every stage of every leg must contribute, and scuttle must materially exceed deliberate walking.
for bone in LEGS:
    if span(walk,bone)<.025: raise RuntimeError(f'Walk under-articulates {bone}')
    if span(scuttle,bone)<.050: raise RuntimeError(f'Scuttle under-articulates {bone}')
if sum(span(scuttle,b) for b in LEGS) < sum(span(walk,b) for b in LEGS)*1.65:
    raise RuntimeError('Scuttle is not materially larger than Walk')
# Alternating tripod phasing: L1/R2/L3 must oppose R1/L2/R3 at the coxa.
tripod_a=('L_Coxa1','R_Coxa2','L_Coxa3'); tripod_b=('R_Coxa1','L_Coxa2','R_Coxa3')
for act in (walk,scuttle):
    av=[values(act,b,2) for b in tripod_a]; bv=[values(act,b,2) for b in tripod_b]
    if any(not v for v in av+bv): raise RuntimeError(f'{act.name}: incomplete tripod coxa curves')
    if not all(a[0]*b[0] < 0 for a,b in zip(av,bv)): raise RuntimeError(f'{act.name}: opposing tripod phase lost')

scav=bpy.data.actions['Ashmite_Scavenge']
if peak(scav,'Head',0)<.30: raise RuntimeError('Scavenge lacks physical head-down vent commitment')
if span(scav,'Jaw',0)<.40: raise RuntimeError('Scavenge jaw scraping stroke too weak')
if span(scav,'Mandible_L',2)<.25 or span(scav,'Mandible_R',2)<.25: raise RuntimeError('Scavenge requires both working mandibles')

left=bpy.data.actions['Ashmite_TurnLeft']; right=bpy.data.actions['Ashmite_TurnRight']
if max(values(left,'Thorax',2) or [0])<.20 or min(values(right,'Thorax',2) or [0])>-.20: raise RuntimeError('Turns lack opposed thorax commitment')
if max(values(left,'Head',2) or [0])<.14 or min(values(right,'Head',2) or [0])>-.14: raise RuntimeError('Turns lack opposed head commitment')
if peak(left,'R_Coxa1',2)<.20 or peak(right,'L_Coxa1',2)<.20: raise RuntimeError('Turns lack asymmetric outer-leg reach')

bite=bpy.data.actions['Ashmite_Bite']
if span(bite,'Jaw',0)<.65: raise RuntimeError('Bite jaw snap below threshold')
if span(bite,'Mandible_L',2)<.35 or span(bite,'Mandible_R',2)<.35: raise RuntimeError('Bite lacks bilateral mandible closure')

hit=bpy.data.actions['Ashmite_Hit']; stagger=bpy.data.actions['Ashmite_Stagger']; death=bpy.data.actions['Ashmite_Death']
if peak(hit,'Thorax')>=peak(stagger,'Thorax'): raise RuntimeError('Hit reaction must remain lighter than Stagger')
if peak(stagger,'Thorax',2)<.25: raise RuntimeError('Stagger lacks readable lightweight displacement')
if peak(death,'Thorax',2)<1.0: raise RuntimeError('Death does not roll Ashmite onto armored shell')
for b in ('L_Coxa1','R_Coxa1','L_Coxa2','R_Coxa2','L_Coxa3','R_Coxa3'):
    if peak(death,b,2)<.50: raise RuntimeError(f'Death lacks arthropod leg collapse at {b}')

contract=arm.get('animation_readability_contract','')
for token in ('fast-alternating-tripod-scuttle','distinct-deliberate-walk','physical-vent-scraping-mouth-tools','bilateral-compact-turns','short-bite','lightweight-reactions','arthropod-collapse','no-root-translation'):
    if token not in contract: raise RuntimeError(f'Missing animation readability contract token: {token}')

print(f'VERIFIED Ashmite production fidelity: scale={scale:.3f}m meshes={len(meshes)} polygons={polys} bound={bound} actions={len(EXPECTED)}; six-leg staged gait, distinct scuttle, physical scavenging, opposed turns, bite, lightweight reactions, armored collapse, no Root translation',flush=True)
