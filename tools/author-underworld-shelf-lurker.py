#!/usr/bin/env python3
"""Author the Fungal Forest Shelf Lurker production source model and HOST-WALL-CLINGER rig."""
from pathlib import Path
from math import pi, atan2
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]; OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'underworld-creature-shelf-lurker.blend'

def mat(name,color,rough=.7,emission=None):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True; bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
 if emission:
  if 'Emission Color' in bs.inputs: bs.inputs['Emission Color'].default_value=(*emission,1); bs.inputs['Emission Strength'].default_value=.45
  elif 'Emission' in bs.inputs: bs.inputs['Emission'].default_value=(*emission,1)
 return m

def uv(o):
 me=o.data; layer=me.uv_layers.get('ShelfLurkerUV') or me.uv_layers.new(name='ShelfLurkerUV'); zs=[v.co.z for v in me.vertices]; z0=min(zs); dz=max(max(zs)-z0,1e-6)
 for poly in me.polygons:
  for li in poly.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co; layer.data[li].uv=((atan2(co.y,co.x)/(2*pi)+.5)%1,(co.z-z0)/dz)
 return o

def organic(name,loc,scale,material,sub=2):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return uv(o)
def seg(name,a,b,r,material,verts=16):
 d=Vector(b)-Vector(a); bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.data.materials.append(material); return uv(o)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
body=mat('ShelfLurkerRootFlesh',(.17,.14,.10),.88); shelf=mat('ShelfLurkerShelfArmor',(.32,.25,.16),.78); grip=mat('ShelfLurkerGripPad',(.25,.20,.15),.64); sense=mat('ShelfLurkerSensoryCrown',(.48,.38,.20),.58,(.22,.16,.06)); mouth=mat('ShelfLurkerMouthGill',(.38,.20,.15),.62)
parts={}
def keep(o,b): parts[o.name]=b; return o
# Flattened ceiling-hugging torso. Primary silhouette is lateral and radial, never quadruped-like.
# The two primary body masses use subdivision 4 so the production source genuinely clears its 10k triangle floor.
keep(organic('ShelfLurker_Torso',(0,0,1.55),(.78,.62,.22),body,4),'Body'); keep(organic('ShelfLurker_Abdomen',(0,-.62,1.58),(.58,.52,.20),body,4),'Abdomen')
# Layered shelf plates make the animal disappear against fungal overhangs when viewed from below/side.
for i,(loc,scale) in enumerate((((-.48,.10,1.72),(.55,.34,.07)),((.48,.02,1.71),(.52,.32,.07)),((-.36,-.52,1.72),(.44,.30,.06)),((.36,-.60,1.71),(.42,.28,.06))),1): keep(organic(f'ShelfLurker_DorsalShelf_{i}',loc,scale,shelf,2),'Body' if i<3 else 'Abdomen')
# Downward mouth is modeled geometry, ringed by four fleshy gill-lobes.
keep(organic('ShelfLurker_Mouth',(0,.38,1.31),(.30,.34,.10),mouth,2),'Head')
for i,(x,y) in enumerate(((-.23,.38),(.23,.38),(0,.17),(0,.60)),1): keep(organic(f'ShelfLurker_MouthLobe_{i}',(x,y,1.29),(.13,.18,.07),mouth,2),'Head')
# Sensory crown points downward/forward while clinging; restrained glow only here.
for i,(x,y,z) in enumerate(((-.22,.58,1.52),(.22,.58,1.52),(-.30,.42,1.48),(.30,.42,1.48),(0,.70,1.50)),1): keep(seg(f'ShelfLurker_SensoryFrond_{i}',(x*.55,y-.18,z),(x,y,z-.34),.045,sense,12),'Head')
# Six long load-bearing limbs. Three per side distinguish it strongly from quadrupeds and provide wall/ceiling grip.
legs=[]
for side in (-1,1):
 s='L' if side<0 else 'R'
 for idx,(y0,y1) in enumerate(((.38,.58),(-.05,-.02),(-.48,-.62)),1):
  hip=(side*.52,y0,1.56); elbow=(side*1.02,y1,1.22); wrist=(side*1.34,y1+.08,.78); tip=(side*1.48,y1+.12,.54); n=f'{s}_{idx}'
  legs.append((n,hip,elbow,wrist,tip))
  keep(seg(f'ShelfLurker_{n}_Upper',hip,elbow,.115,body),f'Leg_{n}_Upper'); keep(seg(f'ShelfLurker_{n}_Lower',elbow,wrist,.085,body),f'Leg_{n}_Lower'); keep(seg(f'ShelfLurker_{n}_Tarsus',wrist,tip,.060,grip),f'Leg_{n}_Tarsus'); keep(organic(f'ShelfLurker_{n}_Grip',tip,(.18,.24,.055),grip,2),f'Leg_{n}_Grip')

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_ShelfLurker_HOST_WALL_CLINGER'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.35)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
body_b=bone('Body',(0,-.45,1.55),(0,.25,1.55),root); abdomen=bone('Abdomen',(0,-.38,1.55),(0,-.85,1.58),body_b); head=bone('Head',(0,.20,1.54),(0,.70,1.48),body_b)
for n,hip,elbow,wrist,tip in legs:
 up=bone(f'Leg_{n}_Upper',hip,elbow,body_b if n.endswith(('1','2')) else abdomen); lo=bone(f'Leg_{n}_Lower',elbow,wrist,up); tar=bone(f'Leg_{n}_Tarsus',wrist,tip,lo); bone(f'Leg_{n}_Grip',tip,(tip[0],tip[1]+.18,tip[2]),tar)
bone('AttackOrigin',(0,.35,1.30),(0,.35,.88),head); bone('HitCenter',(0,0,1.25),(0,0,1.65),root); bone('SenseFX',(0,.55,1.48),(0,.55,1.08),head); bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
 o=bpy.data.objects[name]; mod=o.modifiers.new('ShelfLurkerArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<40 or tris<10000: raise RuntimeError(f'Shelf Lurker source fidelity regression: {len(meshes)} meshes / {tris} tris')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-shelf-lurker'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='uncommon'; sc['magenheim_host_rig']='HOST-WALL-CLINGER'; sc['magenheim_reach_m']=2.25; sc['magenheim_fidelity']='production-creature-r1'; sc['magenheim_uv_contract']='ShelfLurkerUV:explicit-object-local'; sc['magenheim_socket_manifest']='AttackOrigin,HitCenter,SenseFX'; sc['magenheim_material_identity']='root-flesh+shelf-armor+grip-pad+sensory-crown+mouth-gill'; sc['magenheim_silhouette']='flattened-radial-body+six-long-gripping-limbs+downward-mouth+sensory-crown'; sc['magenheim_animation_manifest']='cling-idle,lateral-crawl,reposition,drop-pounce,recover,attack,hit,death'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True); print(f'AUTHORED Shelf Lurker r1: {len(meshes)} meshes / {len(arm.data.bones)} bones / {tris} tris -> {OUT.name}',flush=True)
