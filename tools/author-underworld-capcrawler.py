#!/usr/bin/env python3
"""Author the Fungal Forest Capcrawler production source model and HOST-LOW-CRAWLER rig."""
from pathlib import Path
from math import pi, sin, cos
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]; OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'underworld-creature-capcrawler.blend'; TEX=ROOT/'assets/textures/underworld/creatures/capcrawler'

def image(stem,kind):
 p=TEX/f'{stem}-{kind}.png'
 if not p.exists(): raise RuntimeError(f'Missing texture {p}; run generate-underworld-capcrawler-textures.py first')
 im=bpy.data.images.load(str(p),check_existing=True)
 if kind in ('normal','roughness','emission'): im.colorspace_settings.name='Non-Color'
 return im

def material(name,stem,emit=False):
 m=bpy.data.materials.new(name); m.use_nodes=True; n=m.node_tree.nodes; l=m.node_tree.links; n.clear(); out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); l.new(bs.outputs['BSDF'],out.inputs['Surface']); uv=n.new('ShaderNodeTexCoord')
 for kind,socket in (('albedo','Base Color'),('roughness','Roughness')):
  t=n.new('ShaderNodeTexImage'); t.image=image(stem,kind); l.new(uv.outputs['Generated'],t.inputs['Vector']); l.new(t.outputs['Color'],bs.inputs[socket])
 t=n.new('ShaderNodeTexImage'); t.image=image(stem,'normal'); nm=n.new('ShaderNodeNormalMap'); l.new(uv.outputs['Generated'],t.inputs['Vector']); l.new(t.outputs['Color'],nm.inputs['Color']); l.new(nm.outputs['Normal'],bs.inputs['Normal'])
 if emit:
  e=n.new('ShaderNodeTexImage'); e.image=image(stem,'emission'); l.new(uv.outputs['Generated'],e.inputs['Vector']);
  if 'Emission Color' in bs.inputs: l.new(e.outputs['Color'],bs.inputs['Emission Color']); bs.inputs['Emission Strength'].default_value=1.8
 return m

def organic(name,loc,scale,mat,sub=2):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(mat); return o

def segment(name,a,b,r,mat):
 d=Vector(tuple(b[i]-a[i] for i in range(3))); mid=tuple((a[i]+b[i])/2 for i in range(3)); bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=d.length,location=mid); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(mat); return o

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
car=material('CapcrawlerCarapace','cap-carapace'); flesh=material('CapcrawlerUnderside','underside-flesh'); plate=material('CapcrawlerLegPlate','leg-plate'); mand=material('CapcrawlerMandible','mandible'); gill=material('CapcrawlerGill','gill',True)
parts={}
def keep(o,b): parts[o.name]=b; return o
keep(organic('Capcrawler_Body',(0,0,.22),(.29,.38,.13),flesh,3),'Body')
# Layered cap armor gives the creature its low, broad read at Valheim combat distance.
keep(organic('Capcrawler_Carapace',(0,-.015,.31),(.36,.43,.105),car,3),'Body')
for i,y in enumerate((-.24,-.08,.10,.26),1):
 for side in (-1,1):
  s='L' if side<0 else 'R'; hip=(side*.20,y,.22); knee=(side*.34,y+(i-2.5)*.018,.13); foot=(side*.43,y+(i-2.5)*.035,.035)
  keep(organic(f'Capcrawler_{s}_Coxa{i}',hip,(.045,.05,.035),plate,2),f'{s}_Coxa{i}'); keep(segment(f'Capcrawler_{s}_Leg{i}A',hip,knee,.027,plate),f'{s}_Coxa{i}'); keep(segment(f'Capcrawler_{s}_Leg{i}B',knee,foot,.020,plate),f'{s}_Leg{i}')
for side in (-1,1):
 s='L' if side<0 else 'R'; a=(side*.08,.32,.22); b=(side*.13,.47,.17); keep(segment(f'Capcrawler_Mandible_{s}',a,b,.036,mand),f'Mandible_{s}')
# Gill slits are modeled as readable luminous organs under the cap edge rather than painted decoration.
for i in range(5):
 x=(i-2)*.065; keep(organic(f'Capcrawler_Gill_{i+1}',(x,.285,.255),(.024,.055,.018),gill,2),'Body')

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_Capcrawler_HOST_LOW_CRAWLER'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.12)
def bone(name,h,t,p=None): b=eb.new(name); b.head=h; b.tail=t; b.parent=p; return b
body=bone('Body',(0,-.12,.16),(0,.18,.24),root)
for i,y in enumerate((-.24,-.08,.10,.26),1):
 for side in (-1,1):
  s='L' if side<0 else 'R'; hip=(side*.20,y,.22); knee=(side*.34,y+(i-2.5)*.018,.13); foot=(side*.43,y+(i-2.5)*.035,.035); c=bone(f'{s}_Coxa{i}',hip,knee,body); bone(f'{s}_Leg{i}',knee,foot,c)
for side in (-1,1):
 s='L' if side<0 else 'R'; bone(f'Mandible_{s}',(side*.08,.32,.22),(side*.13,.47,.17),body)
bone('AttackOrigin',(0,.34,.18),(0,.53,.18),body); bone('HitCenter',(0,0,.16),(0,0,.28),root); bone('GillFX',(0,.27,.24),(0,.39,.24),body); bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
 o=bpy.data.objects[name]; mod=o.modifiers.new('CapcrawlerArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<30: raise RuntimeError(f'Capcrawler detail regression: {len(meshes)} mesh parts')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-capcrawler'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='common'; sc['magenheim_host_rig']='HOST-LOW-CRAWLER'; sc['magenheim_length_m']=.90; sc['magenheim_width_m']=.86; sc['magenheim_fidelity']='production-creature-r1'; sc['magenheim_skinning']='rigid-segment-weighted'; sc['magenheim_socket_manifest']='AttackOrigin,HitCenter,GillFX'; sc['magenheim_animation_manifest']='idle,scuttle,turn,attack-left,attack-right,attack-front,guard,hit,stagger,death'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True); print(f'AUTHORED Capcrawler r1: {len(meshes)} meshes / {len(arm.data.bones)} bones -> {OUT.name}',flush=True)
