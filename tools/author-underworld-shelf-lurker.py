#!/usr/bin/env python3
"""Author the Fungal Forest Shelf Lurker production source model and HOST-WALL-CLINGER rig."""
from pathlib import Path
from math import pi, atan2, cos, sin
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
  loops=list(poly.loop_indices)
  coords=[me.vertices[me.loops[li].vertex_index].co for li in loops]
  angles=[(atan2(co.y,co.x)/(2*pi)+.5)%1 for co in coords]
  seam=max(angles)-min(angles)>.5
  for li,co,u in zip(loops,coords,angles):
   # Keep each face continuous across the angular texture seam.
   layer.data[li].uv=(u+1 if seam and u<.5 else u,(co.z-z0)/dz)
 return o

def organic(name,loc,scale,material,sub=2):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=max(sub,3),radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return uv(o)
def shelf_uv(obj,rx,ry):
 """Project cap growth bands radially rather than wrapping them around height.

 The authored shelf-armor maps use concentric rings about UV (0.5, 0.5).
 Normalizing each axis by its own cap radius keeps those rings centered on
 asymmetric shelves. Top and underside intentionally share the same growth
 coordinates; their material response is continuous around the overhang.
 """
 mesh=obj.data
 layer=mesh.uv_layers.get('ShelfLurkerUV') or mesh.uv_layers.new(name='ShelfLurkerUV')
 for poly in mesh.polygons:
  for li in poly.loop_indices:
   co=mesh.vertices[mesh.loops[li].vertex_index].co
   layer.data[li].uv=(.5+co.x/(2.24*rx),.5+co.y/(2.24*ry))
 return obj

def scalloped_shelf(name,loc,scale,material):
 """Closed, scalloped, growth-ring shelf rather than a flattened sphere.

 Twelve concentric top/rim/underside loops preserve the fungal silhouette at
 combat distance; 128 angular segments exceed the prior sphere's triangle
 density without relying on subdivision modifiers or painted-on overhangs.
 """
 profile=((.12,.56),(.22,.59),(.35,.63),(.49,.60),(.64,.51),(.78,.40),
          (.90,.24),(1.0,.08),(.99,-.20),(.86,-.45),(.61,-.49),(.30,-.42))
 segments=128
 rx,ry,rz=scale
 verts=[(0,0,rz*.55),(0,0,-rz*.40)]
 for rad,z in profile:
  for j in range(segments):
   a=2*pi*j/segments
   scallop=1+.065*cos(7*a+.30)+.025*cos(13*a-.45)
   wave=1+(scallop-1)*min(1,rad/.55)
   verts.append((rx*rad*wave*cos(a),ry*rad*wave*sin(a),
                 rz*z+.008*rad*cos(5*a)))
 faces=[]
 for j in range(segments):
  faces.append((0,2+j,2+(j+1)%segments))
 for k in range(len(profile)-1):
  inner=2+k*segments; outer=inner+segments
  for j in range(segments):
   nxt=(j+1)%segments
   faces.append((inner+j,outer+j,outer+nxt,inner+nxt))
 last=2+(len(profile)-1)*segments
 for j in range(segments):
  faces.append((1,last+(j+1)%segments,last+j))
 mesh=bpy.data.meshes.new(name+'_Mesh')
 mesh.from_pydata(verts,[],faces); mesh.update()
 obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
 obj.location=loc; mesh.materials.append(material)
 obj['magenheim_shelf_uv_radii']=(rx,ry)
 for poly in mesh.polygons: poly.use_smooth=True
 return shelf_uv(obj,rx,ry)


def taper(name,a,b,r0,r1,material):
 d=Vector(b)-Vector(a)
 if d.length<=1e-5 or not 0<r1<r0: raise RuntimeError(f'{name}: invalid tapered talon segment')
 bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r0,radius2=r1,depth=d.length,location=(Vector(a)+Vector(b))/2)
 o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
 o.data.materials.append(material); return uv(o)

def sensory_frond_path(start,end,bow,steps=8):
 """Eight curved intervals give the five sensory organs a visible, species-specific profile."""
 if steps<6: raise ValueError('Sensory frond needs at least six intervals')
 return [tuple(start[k]+(end[k]-start[k])*i/steps+bow[k]*sin(pi*i/steps)
               for k in range(3)) for i in range(steps+1)]

def sensory_frond(name,start,end,bow,material):
 """Watertight, tapering sensory antenna with an actual curved silhouette."""
 sides=12; steps=8
 centers=[Vector(p) for p in sensory_frond_path(start,end,bow,steps)]
 vertices=[]
 for i,center in enumerate(centers):
  tangent=(centers[min(i+1,steps)]-centers[max(i-1,0)]).normalized()
  reference=Vector((0,1,0)) if abs(tangent.y)<.9 else Vector((1,0,0))
  axis=tangent.cross(reference).normalized()
  other=tangent.cross(axis).normalized()
  radius=.047*(1-i/steps)**.9+.006
  for j in range(sides):
   a=2*pi*j/sides
   vertices.append(tuple(center+radius*(axis*cos(a)+other*sin(a))))
 faces=[]
 for i in range(steps):
  for j in range(sides):
   nxt=(j+1)%sides
   faces.append((i*sides+j,i*sides+nxt,(i+1)*sides+nxt,(i+1)*sides+j))
 faces.append(tuple(reversed(range(sides))))
 faces.append(tuple(steps*sides+j for j in range(sides)))
 mesh=bpy.data.meshes.new(name+'_Mesh')
 mesh.from_pydata(vertices,[],faces); mesh.update()
 obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
 mesh.materials.append(material)
 for poly in mesh.polygons: poly.use_smooth=len(poly.vertices)==4
 layer=mesh.uv_layers.new(name='ShelfLurkerUV')
 for poly in mesh.polygons:
  ring_ids=[mesh.loops[li].vertex_index%sides for li in poly.loop_indices]
  seam=0 in ring_ids and sides-1 in ring_ids
  for li in poly.loop_indices:
   idx=mesh.loops[li].vertex_index
   angular=idx%sides
   layer.data[li].uv=(1.0 if seam and angular==0 else angular/sides,(idx//sides)/steps)
 return obj

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
body=mat('ShelfLurkerRootFlesh',(.17,.14,.10),.88); shelf=mat('ShelfLurkerShelfArmor',(.32,.25,.16),.78); grip=mat('ShelfLurkerGripPad',(.25,.20,.15),.64); sense=mat('ShelfLurkerSensoryCrown',(.48,.38,.20),.58,(.22,.16,.06)); mouth=mat('ShelfLurkerMouthGill',(.38,.20,.15),.62)
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from shelf_lurker_gill_geometry import shelf_gill
from magenheim_creature_pbr import bind as bind_creature_pbr
from shelf_lurker_limb_geometry import limb_segment
bind_creature_pbr(bpy,bpy.data.materials['ShelfLurkerRootFlesh'],'shelf-lurker','root-flesh')
bind_creature_pbr(bpy,bpy.data.materials['ShelfLurkerShelfArmor'],'shelf-lurker','shelf-armor')
bind_creature_pbr(bpy,bpy.data.materials['ShelfLurkerGripPad'],'shelf-lurker','grip-pad')
bind_creature_pbr(bpy,bpy.data.materials['ShelfLurkerSensoryCrown'],'shelf-lurker','sensory-crown')
bind_creature_pbr(bpy,bpy.data.materials['ShelfLurkerMouthGill'],'shelf-lurker','mouth-gill')
parts={}
def keep(o,b): parts[o.name]=b; return o
# Flattened ceiling-hugging torso. Primary silhouette is lateral and radial, never quadruped-like.
# The two primary body masses use subdivision 4 so the production source genuinely clears its 10k triangle floor.
keep(organic('ShelfLurker_Torso',(0,0,1.55),(.78,.62,.22),body,4),'Body'); keep(organic('ShelfLurker_Abdomen',(0,-.62,1.58),(.58,.52,.20),body,4),'Abdomen')
# Layered shelf plates make the animal disappear against fungal overhangs when viewed from below/side.
for i,(loc,scale) in enumerate((((-.48,.10,1.72),(.55,.34,.07)),((.48,.02,1.71),(.52,.32,.07)),((-.36,-.52,1.72),(.44,.30,.06)),((.36,-.60,1.71),(.42,.28,.06))),1):
 shelf_bone='Body' if i<3 else 'Abdomen'
 keep(scalloped_shelf(f'ShelfLurker_DorsalShelf_{i}',loc,scale,shelf),shelf_bone)
 # Six physical underside lamellae per overhanging shelf: fungal growth,
 # rather than featureless armor disks, remains readable from below.
 for rib in range(6):
  angle=2*pi*rib/6
  direction=Vector((cos(angle),sin(angle),0))
  start=Vector((loc[0]+direction.x*.07,loc[1]+direction.y*.05,loc[2]-scale[2]*.42-.012))
  end=Vector((loc[0]+direction.x*scale[0]*.78,loc[1]+direction.y*scale[1]*.78,loc[2]-scale[2]*.48-.012))
  keep(shelf_gill(f'ShelfLurker_ShelfGill_{i}_{rib+1}',start,end,mouth),shelf_bone)
# Downward mouth is modeled geometry, ringed by four fleshy gill-lobes.
keep(organic('ShelfLurker_Mouth',(0,.38,1.31),(.30,.34,.10),mouth,2),'Head')
for i,(x,y) in enumerate(((-.23,.38),(.23,.38),(0,.17),(0,.60)),1): keep(organic(f'ShelfLurker_MouthLobe_{i}',(x,y,1.29),(.13,.18,.07),mouth,2),'Head')
# Eight hooked radial feeding barbs form a downward-facing funnel with a
# visibly open central aperture during the creature's drop ambush.
for digit in range(8):
 angle=2*pi*digit/8
 direction=Vector((cos(angle),sin(angle),0))
 root=Vector((direction.x*.23,.38+direction.y*.23,1.29))
 flare=Vector((direction.x*.30,.38+direction.y*.30,1.13))
 point=Vector((direction.x*.115,.38+direction.y*.115,.98))
 keep(taper(f'ShelfLurker_MouthBarb_{digit+1}_Base',root,flare,.036,.026,mouth),'Head')
 keep(taper(f'ShelfLurker_MouthBarb_{digit+1}_Hook',flare,point,.026,.004,mouth),'Head')
# Sensory crown points downward/forward while clinging; restrained glow only here.
for i,(x,y,z) in enumerate(((-.22,.58,1.52),(.22,.58,1.52),(-.30,.42,1.48),(.30,.42,1.48),(0,.70,1.50)),1):
 bow=(.065 if x>0 else -.065 if x<0 else 0,.105,.035)
 keep(sensory_frond(f'ShelfLurker_SensoryFrond_{i}',(x*.55,y-.18,z),(x,y,z-.34),bow,sense),'Head')
# Six long load-bearing limbs. Three per side distinguish it strongly from quadrupeds and provide wall/ceiling grip.
legs=[]
for side in (-1,1):
 s='L' if side<0 else 'R'
 for idx,(y0,y1) in enumerate(((.38,.58),(-.05,-.02),(-.48,-.62)),1):
  hip=(side*.52,y0,1.56); elbow=(side*1.02,y1,1.22); wrist=(side*1.34,y1+.08,.78); tip=(side*1.48,y1+.12,.54); n=f'{s}_{idx}'
  legs.append((n,hip,elbow,wrist,tip))
  keep(limb_segment(f'ShelfLurker_{n}_Upper',hip,elbow,.115,body),f'Leg_{n}_Upper'); keep(limb_segment(f'ShelfLurker_{n}_Lower',elbow,wrist,.085,body),f'Leg_{n}_Lower'); keep(limb_segment(f'ShelfLurker_{n}_Tarsus',wrist,tip,.060,grip),f'Leg_{n}_Tarsus'); keep(organic(f'ShelfLurker_{n}_Grip',tip,(.18,.24,.055),grip,2),f'Leg_{n}_Grip')
  # Three hard talons per pad project outward then return inward/downward,
  # preserving the six-limbed shelf-clinger silhouette under motion.
  for digit,offset in enumerate((-.15,0,.15),1):
   talon_root=Vector((tip[0]+side*.035,tip[1]+offset*.70,tip[2]-.02))
   knuckle=Vector((tip[0]+side*.27,tip[1]+offset,tip[2]-.11))
   hook=Vector((tip[0]+side*.14,tip[1]+offset*1.15,tip[2]-.25))
   keep(taper(f'ShelfLurker_{n}_Talon_{digit}_Base',talon_root,knuckle,.052,.035,shelf),f'Leg_{n}_Grip')
   keep(taper(f'ShelfLurker_{n}_Talon_{digit}_Hook',knuckle,hook,.035,.008,shelf),f'Leg_{n}_Grip')

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_ShelfLurker_HOST_WALL_CLINGER'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.35)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
body_b=bone('Body',(0,-.45,1.55),(0,.25,1.55),root); abdomen=bone('Abdomen',(0,-.38,1.55),(0,-.85,1.58),body_b); head=bone('Head',(0,.20,1.54),(0,.70,1.48),body_b)
for n,hip,elbow,wrist,tip in legs:
 up=bone(f'Leg_{n}_Upper',hip,elbow,body_b if n.endswith(('1','2')) else abdomen); lo=bone(f'Leg_{n}_Lower',elbow,wrist,up); tar=bone(f'Leg_{n}_Tarsus',wrist,tip,lo); bone(f'Leg_{n}_Grip',tip,(tip[0],tip[1]+.18,tip[2]),tar)
bone('AttackOrigin',(0,.35,1.30),(0,.35,.88),head); bone('HitCenter',(0,0,1.25),(0,0,1.65),root); bone('SenseFX',(0,.55,1.48),(0,.55,1.08),head); bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
 o=bpy.data.objects[name]; mod=o.modifiers.new('ShelfLurkerArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']; tris=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
if len(meshes)<116 or tris<10000: raise RuntimeError(f'Shelf Lurker source fidelity regression: {len(meshes)} meshes / {tris} tris')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-shelf-lurker'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='uncommon'; sc['magenheim_host_rig']='HOST-WALL-CLINGER'; sc['magenheim_reach_m']=2.25; sc['magenheim_fidelity']='production-creature-r1'; sc['magenheim_uv_contract']='ShelfLurkerUV:explicit-object-local'; sc['magenheim_socket_manifest']='AttackOrigin,HitCenter,SenseFX'; sc['magenheim_material_identity']='root-flesh+shelf-armor+grip-pad+sensory-crown+mouth-gill'; sc['magenheim_silhouette']='flattened-radial-body+six-long-gripping-limbs+downward-mouth+sensory-crown'; sc['magenheim_animation_manifest']='cling-idle,lateral-crawl,reposition,drop-pounce,recover,attack,hit,death'; sc['magenheim_skinning']='rigid-segment-weighted'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True); print(f'AUTHORED Shelf Lurker r1: {len(meshes)} meshes / {len(arm.data.bones)} bones / {tris} tris -> {OUT.name}',flush=True)
