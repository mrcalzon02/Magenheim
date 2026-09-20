#!/usr/bin/env python3
"""Author the Fungal Forest Mycelial Stalker production source model and HOST-QUADRUPED rig."""
from pathlib import Path
from math import pi, atan2
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]; OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'underworld-creature-mycelial-stalker.blend'; TEX=ROOT/'assets/textures/underworld/creatures/mycelial-stalker'

def image(stem,kind):
 p=TEX/f'{stem}-{kind}.png'
 if not p.exists(): raise RuntimeError(f'Missing texture {p}; run generate-underworld-mycelial-stalker-textures.py first')
 im=bpy.data.images.load(str(p),check_existing=True)
 if kind in ('normal','roughness'): im.colorspace_settings.name='Non-Color'
 return im

def material(name,stem,emit=False):
 m=bpy.data.materials.new(name); m.use_nodes=True; n=m.node_tree.nodes; l=m.node_tree.links; n.clear(); out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); l.new(bs.outputs['BSDF'],out.inputs['Surface']); uv=n.new('ShaderNodeTexCoord')
 for kind,socket in (('albedo','Base Color'),('roughness','Roughness')):
  t=n.new('ShaderNodeTexImage'); t.image=image(stem,kind); l.new(uv.outputs['UV'],t.inputs['Vector']); l.new(t.outputs['Color'],bs.inputs[socket])
 t=n.new('ShaderNodeTexImage'); t.image=image(stem,'normal'); nm=n.new('ShaderNodeNormalMap'); l.new(uv.outputs['UV'],t.inputs['Vector']); l.new(t.outputs['Color'],nm.inputs['Color']); l.new(nm.outputs['Normal'],bs.inputs['Normal'])
 if emit:
  e=n.new('ShaderNodeTexImage'); e.image=image(stem,'emission'); l.new(uv.outputs['UV'],e.inputs['Vector']); l.new(e.outputs['Color'],bs.inputs['Emission Color']); bs.inputs['Emission Strength'].default_value=1.35
 return m

def uv(o):
 me=o.data; layer=me.uv_layers.get('MycelialStalkerUV') or me.uv_layers.new(name='MycelialStalkerUV'); zs=[v.co.z for v in me.vertices]; z0=min(zs); dz=max(max(zs)-z0,1e-6)
 for poly in me.polygons:
  for li in poly.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co; layer.data[li].uv=((atan2(co.y,co.x)/(2*pi)+.5)%1,(co.z-z0)/dz)
 return o

def organic(name,loc,scale,mat,sub=2):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(mat); return uv(o)
def seg(name,a,b,r,mat):
 d=Vector(b)-Vector(a); bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.data.materials.append(mat); return uv(o)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
hide=material('StalkerRootHide','root-hide'); cord=material('StalkerMycelium','mycelium'); shelf=material('StalkerShelfFungus','shelf-fungus'); sense=material('StalkerSensoryTissue','sensory-tissue',True)
parts={}
def keep(o,b): parts[o.name]=b; return o
# Long, low chest and raised pelvis create a stalking silhouette unlike wolf/boar donors.
keep(organic('Stalker_Chest',(0,.18,1.12),(.43,.70,.34),hide,3),'Spine'); keep(organic('Stalker_Pelvis',(0,-.62,1.22),(.38,.48,.32),hide,3),'Pelvis')
keep(seg('Stalker_Neck',(0,.58,1.18),(0,.88,1.34),.22,hide),'Neck'); keep(organic('Stalker_Head',(0,1.02,1.36),(.31,.42,.27),hide,3),'Head')
# Split opening jaw.
for side in (-1,1): keep(seg(f'Stalker_Jaw_{"L" if side<0 else "R"}',(side*.10,1.22,1.32),(side*.18,1.52,1.25),.075,hide),f'Jaw_{"L" if side<0 else "R"}')
# Four long rootlike limbs, deliberately high-elbowed and narrow-footed.
legs=((-.27,.43,1.10,-.46,.50,.55,-.40,.61,.08,'Front_L'),(.27,.43,1.10,.46,.50,.55,.40,.61,.08,'Front_R'),(-.27,-.62,1.17,-.50,-.70,.62,-.43,-.83,.08,'Rear_L'),(.27,-.62,1.17,.50,-.70,.62,.43,-.83,.08,'Rear_R'))
for hx,hy,hz,kx,ky,kz,fx,fy,fz,n in legs:
 parent='Spine' if n.startswith('Front') else 'Pelvis'; keep(seg(f'Stalker_{n}_Upper',(hx,hy,hz),(kx,ky,kz),.105,hide),f'{n}_Upper'); keep(seg(f'Stalker_{n}_Lower',(kx,ky,kz),(fx,fy,fz),.075,hide),f'{n}_Lower'); keep(organic(f'Stalker_{n}_Foot',(fx,fy,fz),(.12,.20,.055),hide,2),f'{n}_Foot')
# Visible mycelial cord anatomy crossing the torso.
for i,(a,b) in enumerate((((-.34,-.35,1.30),(.32,.38,1.34)),((.34,-.15,1.18),(-.30,.50,1.16)),((-.28,-.62,1.34),(.26,.02,1.43))),1): keep(seg(f'Stalker_MycelialCord_{i}',a,b,.026,cord),'Spine')
# Asymmetric shelf growth keeps the silhouette fungal and non-donor-like.
for i,(loc,scale) in enumerate((((-.34,-.20,1.48),(.38,.22,.055)),((.30,-.48,1.49),(.30,.18,.048)),((-.28,.25,1.46),(.25,.16,.045))),1): keep(organic(f'Stalker_Shelf_{i}',loc,scale,shelf,2),'Spine')
# Restrained sensory organs/fronds; only these may emit.
for side in (-1,1):
 s='L' if side<0 else 'R'; keep(organic(f'Stalker_SenseNode_{s}',(side*.18,1.28,1.46),(.055,.085,.045),sense,2),'Head'); keep(seg(f'Stalker_Frond_{s}',(side*.15,1.20,1.52),(side*.30,1.48,1.72),.022,cord),'Head')

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_MycelialStalker_HOST_QUADRUPED'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.3)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
spine=bone('Spine',(0,-.55,1.18),(0,.50,1.20),root); pelvis=bone('Pelvis',(0,-.78,1.18),(0,-.38,1.20),spine); neck=bone('Neck',(0,.48,1.20),(0,.87,1.34),spine); head=bone('Head',(0,.85,1.34),(0,1.28,1.36),neck)
for side in (-1,1): s='L' if side<0 else 'R'; bone(f'Jaw_{s}',(side*.10,1.20,1.32),(side*.18,1.52,1.25),head)
for hx,hy,hz,kx,ky,kz,fx,fy,fz,n in legs:
 p=spine if n.startswith('Front') else pelvis; up=bone(f'{n}_Upper',(hx,hy,hz),(kx,ky,kz),p); lo=bone(f'{n}_Lower',(kx,ky,kz),(fx,fy,fz),up); bone(f'{n}_Foot',(fx,fy,fz),(fx,fy+.16,fz),lo)
bone('AttackOrigin',(0,1.30,1.30),(0,1.65,1.28),head); bone('HitCenter',(0,.05,.9),(0,.05,1.3),root); bone('SenseFX',(0,1.25,1.48),(0,1.55,1.55),head); bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
 o=bpy.data.objects[name]; mod=o.modifiers.new('StalkerArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<30 or tris<7000: raise RuntimeError(f'Stalker source fidelity regression: {len(meshes)} meshes / {tris} tris')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-mycelial-stalker'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='uncommon'; sc['magenheim_host_rig']='HOST-QUADRUPED'; sc['magenheim_shoulder_height_m']=1.35; sc['magenheim_fidelity']='production-creature-r1'; sc['magenheim_uv_contract']='MycelialStalkerUV:explicit-object-local'; sc['magenheim_socket_manifest']='AttackOrigin,HitCenter,SenseFX'; sc['magenheim_material_identity']='root-hide+mycelium+shelf-fungus+localized-sensory-emission'; sc['magenheim_silhouette']='lean-low-chest+raised-pelvis+asymmetric-shelves+root-limbs'; sc['magenheim_animation_manifest']='conceal-idle,crouch,walk,pounce,failed-pounce-retreat,hit,stagger,death'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True); print(f'AUTHORED Mycelial Stalker r1: {len(meshes)} meshes / {len(arm.data.bones)} bones / {tris} tris -> {OUT.name}',flush=True)
