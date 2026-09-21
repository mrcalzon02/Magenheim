#!/usr/bin/env python3
"""Author the Fungal Forest Puffback production source model and HOST-QUADRUPED rig."""
from pathlib import Path
from math import pi, atan2
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]; OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'underworld-creature-puffback.blend'

def mat(name,color,rough=.7):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True; bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough; return m

def uv(o):
 me=o.data; layer=me.uv_layers.get('PuffbackUV') or me.uv_layers.new(name='PuffbackUV'); zs=[v.co.z for v in me.vertices]; z0=min(zs); dz=max(max(zs)-z0,1e-6)
 for poly in me.polygons:
  for li in poly.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co; layer.data[li].uv=((atan2(co.y,co.x)/(2*pi)+.5)%1,(co.z-z0)/dz)
 return o

def organic(name,loc,scale,material,sub=2):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return uv(o)
def seg(name,a,b,r,material):
 d=Vector(b)-Vector(a); bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.data.materials.append(material); return uv(o)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
hide=mat('PuffbackRootHide',(.20,.16,.11),.86); bladder=mat('PuffbackSporeBladder',(.45,.36,.20),.62); plate=mat('PuffbackFungalPlate',(.31,.25,.15),.78); gill=mat('PuffbackVentGill',(.62,.47,.25),.55)
parts={}
def keep(o,b): parts[o.name]=b; return o
# Heavy, front-loaded neutral herbivore silhouette. The bladder is a modeled body mass, not painted detail.
keep(organic('Puffback_Chest',(0,.20,1.34),(.70,.92,.60),hide,3),'Spine_2'); keep(organic('Puffback_Pelvis',(0,-.70,1.28),(.62,.68,.56),hide,3),'Pelvis')
keep(seg('Puffback_Neck',(0,.78,1.42),(0,1.12,1.28),.34,hide),'Neck'); keep(organic('Puffback_Head',(0,1.30,1.18),(.48,.55,.38),hide,3),'Head')
# Broad rooting muzzle and two hornlike fungal brow plates make the head readable during charge.
keep(organic('Puffback_Muzzle',(0,1.70,1.04),(.46,.38,.25),hide,2),'Head')
for side in (-1,1):
 s='L' if side<0 else 'R'; keep(seg(f'Puffback_BrowPlate_{s}',(side*.24,1.36,1.40),(side*.43,1.57,1.48),.10,plate),'Head')
# Massive dorsal spore bladder plus secondary lobes; all are real silhouette geometry.
keep(organic('Puffback_Bladder_Main',(0,-.18,1.92),(.88,1.04,.72),bladder,3),'Bladder_Main')
for i,(loc,scale) in enumerate((((-.52,-.25,1.78),(.42,.55,.44)),((.50,-.34,1.80),(.40,.50,.42)),((0,-.78,1.72),(.52,.48,.40))),1): keep(organic(f'Puffback_Bladder_Lobe_{i}',loc,scale,bladder,2),'Bladder_Main')
# Layered cap plates protect the bladder base without turning it into an armored combat beast.
for i,(loc,scale) in enumerate((((-.48,.12,1.68),(.48,.34,.09)),((.47,.02,1.66),(.46,.32,.09)),((-.38,-.63,1.60),(.40,.30,.08)),((.38,-.68,1.58),(.38,.28,.08))),1): keep(organic(f'Puffback_BackPlate_{i}',loc,scale,plate,2),'Spine_2')
# Paired vent/gill banks are the physical source of the defensive spore puff.
for side in (-1,1):
 s='L' if side<0 else 'R'
 for i,y in enumerate((-.05,-.34,-.61),1): keep(organic(f'Puffback_Vent_{s}_{i}',(side*.67,y,1.48),(.09,.18,.13),gill,2),'Spine_2')
# Heavy articulated feet support the charge silhouette and prevent a scaled-wolf appearance.
legs=((-.48,.46,1.28,-.58,.48,.62,-.54,.58,.12,'Front_L'),(.48,.46,1.28,.58,.48,.62,.54,.58,.12,'Front_R'),(-.45,-.72,1.22,-.57,-.80,.60,-.53,-.88,.12,'Rear_L'),(.45,-.72,1.22,.57,-.80,.60,.53,-.88,.12,'Rear_R'))
for hx,hy,hz,kx,ky,kz,fx,fy,fz,n in legs:
 keep(seg(f'Puffback_{n}_Upper',(hx,hy,hz),(kx,ky,kz),.18,hide),f'{n}_Upper'); keep(seg(f'Puffback_{n}_Lower',(kx,ky,kz),(fx,fy,fz),.15,hide),f'{n}_Lower'); keep(organic(f'Puffback_{n}_Foot',(fx,fy,fz),(.25,.34,.10),hide,2),f'{n}_Foot')

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_Puffback_HOST_QUADRUPED'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.35)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
pelvis=bone('Pelvis',(0,-.85,1.22),(0,-.48,1.28),root); s1=bone('Spine_1',(0,-.52,1.28),(0,-.05,1.34),pelvis); s2=bone('Spine_2',(0,-.08,1.34),(0,.55,1.38),s1); neck=bone('Neck',(0,.52,1.38),(0,1.02,1.28),s2); head=bone('Head',(0,1.00,1.28),(0,1.55,1.16),neck); bladder_b=bone('Bladder_Main',(0,-.55,1.65),(0,.10,2.15),s1)
for hx,hy,hz,kx,ky,kz,fx,fy,fz,n in legs:
 p=s2 if n.startswith('Front') else pelvis; up=bone(f'{n}_Upper',(hx,hy,hz),(kx,ky,kz),p); lo=bone(f'{n}_Lower',(kx,ky,kz),(fx,fy,fz),up); bone(f'{n}_Foot',(fx,fy,fz),(fx,fy+.20,fz),lo)
bone('AttackOrigin',(0,1.45,1.12),(0,1.90,1.05),head); bone('HitCenter',(0,-.05,.95),(0,-.05,1.42),root); bone('SporeFX',(0,-.15,1.85),(0,-.15,2.30),bladder_b); bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
 o=bpy.data.objects[name]; mod=o.modifiers.new('PuffbackArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<30 or tris<10000: raise RuntimeError(f'Puffback source fidelity regression: {len(meshes)} meshes / {tris} tris')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-puffback'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='uncommon-neutral'; sc['magenheim_host_rig']='HOST-QUADRUPED'; sc['magenheim_shoulder_height_m']=1.72; sc['magenheim_fidelity']='production-creature-r1'; sc['magenheim_uv_contract']='PuffbackUV:explicit-object-local'; sc['magenheim_socket_manifest']='AttackOrigin,HitCenter,SporeFX'; sc['magenheim_material_identity']='root-hide+spore-bladder+fungal-plate+vent-gill'; sc['magenheim_silhouette']='front-heavy-herbivore+massive-dorsal-bladder+heavy-feet+broad-rooting-muzzle'; sc['magenheim_animation_manifest']='graze-root,idle,walk,warning-display,charge,defensive-inflate,spore-puff,hit,stagger,death'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True); print(f'AUTHORED Puffback r1: {len(meshes)} meshes / {len(arm.data.bones)} bones / {tris} tris -> {OUT.name}',flush=True)
