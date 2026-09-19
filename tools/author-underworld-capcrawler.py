#!/usr/bin/env python3
"""Author the Fungal Forest Capcrawler production source model and HOST-LOW-CRAWLER rig."""
from pathlib import Path
from math import pi, atan2
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]; OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'underworld-creature-capcrawler.blend'; TEX=ROOT/'assets/textures/underworld/creatures/capcrawler'

def image(stem,kind):
 p=TEX/f'{stem}-{kind}.png'
 if not p.exists(): raise RuntimeError(f'Missing texture {p}; run generate-underworld-capcrawler-textures.py first')
 im=bpy.data.images.load(str(p),check_existing=True)
 if kind in ('normal','roughness'): im.colorspace_settings.name='Non-Color'
 return im

def material(name,stem,emit=False):
 m=bpy.data.materials.new(name); m.use_nodes=True; n=m.node_tree.nodes; l=m.node_tree.links; n.clear(); out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); l.new(bs.outputs['BSDF'],out.inputs['Surface']); uv=n.new('ShaderNodeTexCoord')
 for kind,socket in (('albedo','Base Color'),('roughness','Roughness')):
  t=n.new('ShaderNodeTexImage'); t.image=image(stem,kind); l.new(uv.outputs['UV'],t.inputs['Vector']); l.new(t.outputs['Color'],bs.inputs[socket])
 t=n.new('ShaderNodeTexImage'); t.image=image(stem,'normal'); nm=n.new('ShaderNodeNormalMap'); l.new(uv.outputs['UV'],t.inputs['Vector']); l.new(t.outputs['Color'],nm.inputs['Color']); l.new(nm.outputs['Normal'],bs.inputs['Normal'])
 if emit:
  e=n.new('ShaderNodeTexImage'); e.image=image(stem,'emission'); l.new(uv.outputs['UV'],e.inputs['Vector'])
  if 'Emission Color' in bs.inputs: l.new(e.outputs['Color'],bs.inputs['Emission Color']); bs.inputs['Emission Strength'].default_value=1.8
 return m

def explicit_uv(o):
 me=o.data; layer=me.uv_layers.get('CapcrawlerUV') or me.uv_layers.new(name='CapcrawlerUV'); zs=[v.co.z for v in me.vertices]; z0=min(zs); dz=max(max(zs)-z0,1e-6)
 for poly in me.polygons:
  for li in poly.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co; layer.data[li].uv=((atan2(co.y,co.x)/(2*pi)+0.5)%1.0,(co.z-z0)/dz)
 if len(layer.data)!=len(me.loops): raise RuntimeError(f'UV loop mismatch on {o.name}')
 return o

def organic(name,loc,scale,mat,sub=2):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(mat); return explicit_uv(o)

def segment(name,a,b,r,mat):
 d=Vector(tuple(b[i]-a[i] for i in range(3))); mid=tuple((a[i]+b[i])/2 for i in range(3)); bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=d.length,location=mid); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(mat); return explicit_uv(o)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
car=material('CapcrawlerCarapace','cap-carapace'); flesh=material('CapcrawlerUnderside','underside-flesh'); plate=material('CapcrawlerLegPlate','leg-plate'); mand=material('CapcrawlerMandible','mandible'); gill=material('CapcrawlerGill','gill',True)
parts={}
def keep(o,b): parts[o.name]=b; return o
keep(organic('Capcrawler_Body',(0,0,.22),(.29,.38,.13),flesh,3),'Body')
keep(organic('Capcrawler_Carapace',(0,-.015,.31),(.36,.43,.105),car,3),'Body')
for side in (-1,1):
 s='L' if side<0 else 'R'
 for i,(y,z,sy) in enumerate(((-.24,.305,.105),(-.06,.326,.125),(.13,.320,.115)),1): keep(organic(f'Capcrawler_RimScute_{s}{i}',(side*.315,y,z),(.085,sy,.035),car,2),'Body')
keep(organic('Capcrawler_CrownPlate',(0,-.285,.345),(.205,.105,.042),car,2),'Body')
# Eight broad tarsal pads and short terminal claws make ground contact readable at combat distance.
# They are separate source pieces but inherit each distal leg bone, avoiding a second deformation chain.
for i,y in enumerate((-.24,-.08,.10,.26),1):
 for side in (-1,1):
  s='L' if side<0 else 'R'; hip=(side*.20,y,.22); knee=(side*.34,y+(i-2.5)*.018,.13); foot=(side*.43,y+(i-2.5)*.035,.035); toe=(side*.475,foot[1]+.008,.024)
  keep(organic(f'Capcrawler_{s}_Coxa{i}',hip,(.045,.05,.035),plate,2),f'{s}_Coxa{i}'); keep(segment(f'Capcrawler_{s}_Leg{i}A',hip,knee,.027,plate),f'{s}_Coxa{i}'); keep(segment(f'Capcrawler_{s}_Leg{i}B',knee,foot,.020,plate),f'{s}_Leg{i}')
  keep(organic(f'Capcrawler_{s}_FootPad{i}',foot,(.042,.052,.018),plate,2),f'{s}_Leg{i}'); keep(segment(f'Capcrawler_{s}_Claw{i}',foot,toe,.012,mand),f'{s}_Leg{i}')
for side in (-1,1):
 s='L' if side<0 else 'R'; a=(side*.08,.32,.22); b=(side*.13,.47,.17); keep(segment(f'Capcrawler_Mandible_{s}',a,b,.036,mand),f'Mandible_{s}')
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

def action(name,end,poses):
 act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
 for frame,bone_poses in poses.items():
  for bname,rot in bone_poses.items():
   pb=arm.pose.bones.get(bname)
   if not pb: raise RuntimeError(f'{name}: missing animation bone {bname}')
   pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler',frame=frame,group=bname)
 for fc in getattr(act,'fcurves',()):
  for kp in fc.keyframe_points: kp.interpolation='BEZIER'
 act.frame_start=1; act.frame_end=end; return act

def gait(amount):
 p={}
 for s,i,phase in (('L',1,1),('R',1,-1),('L',2,-1),('R',2,1),('L',3,1),('R',3,-1),('L',4,-1),('R',4,1)):
  p[f'{s}_Coxa{i}']=(0,0,phase*amount); p[f'{s}_Leg{i}']=(phase*amount*.32,0,0)
 return p
neutral={b:(0,0,0) for b in ('Body','Mandible_L','Mandible_R')}
action('Capcrawler_Idle',72,{1:neutral,18:{'Body':(.018,0,0),'Mandible_L':(0,0,.04),'Mandible_R':(0,0,-.04)},36:neutral,54:{'Body':(-.012,0,0)},72:neutral})
action('Capcrawler_Scuttle',24,{1:gait(.22),7:gait(-.22),13:gait(.22),19:gait(-.22),24:gait(.22)})
action('Capcrawler_Turn',30,{1:gait(.08),10:{'Body':(0,0,.14),**gait(-.12)},20:{'Body':(0,0,.28),**gait(.12)},30:neutral})
action('Capcrawler_AttackFront',24,{1:neutral,8:{'Body':(-.08,0,0),'Mandible_L':(0,0,.30),'Mandible_R':(0,0,-.30)},13:{'Body':(.12,0,0),'Mandible_L':(0,0,-.16),'Mandible_R':(0,0,.16)},24:neutral})
action('Capcrawler_AttackLeft',26,{1:neutral,9:{'Body':(-.05,0,-.16),'Mandible_L':(0,0,.38)},14:{'Body':(.08,0,.22),'Mandible_L':(0,0,-.22)},26:neutral})
action('Capcrawler_AttackRight',26,{1:neutral,9:{'Body':(-.05,0,.16),'Mandible_R':(0,0,-.38)},14:{'Body':(.08,0,-.22),'Mandible_R':(0,0,.22)},26:neutral})
action('Capcrawler_Guard',36,{1:neutral,12:{'Body':(-.10,0,0),'Mandible_L':(0,0,-.12),'Mandible_R':(0,0,.12)},28:{'Body':(-.10,0,0),'Mandible_L':(0,0,-.12),'Mandible_R':(0,0,.12)},36:neutral})
action('Capcrawler_Hit',16,{1:neutral,5:{'Body':(.10,0,.12)},16:neutral})
action('Capcrawler_Stagger',30,{1:neutral,8:{'Body':(.20,0,.25),'Mandible_L':(.10,0,.18),'Mandible_R':(.10,0,-.18)},18:{'Body':(-.08,0,-.10)},30:neutral})
action('Capcrawler_Death',54,{1:neutral,16:{'Body':(.12,0,.10)},30:{'Body':(.35,0,.55),'Mandible_L':(.25,0,.20),'Mandible_R':(.25,0,-.20)},54:{'Body':(.15,0,1.42),'Mandible_L':(.55,0,.35),'Mandible_R':(.55,0,-.35)}})
arm.animation_data.action=None
for name,bname in parts.items():
 o=bpy.data.objects[name]; mod=o.modifiers.new('CapcrawlerArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<53: raise RuntimeError(f'Capcrawler detail regression: {len(meshes)} mesh parts')
for o in meshes:
 uv=o.data.uv_layers.get('CapcrawlerUV')
 if not uv or not uv.data: raise RuntimeError(f'Missing explicit UVs: {o.name}')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-capcrawler'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='common'; sc['magenheim_host_rig']='HOST-LOW-CRAWLER'; sc['magenheim_length_m']=.90; sc['magenheim_width_m']=.98; sc['magenheim_fidelity']='production-creature-r5'; sc['magenheim_skinning']='rigid-segment-weighted'; sc['magenheim_uv_contract']='CapcrawlerUV:explicit-object-local'; sc['magenheim_socket_manifest']='AttackOrigin,HitCenter,GillFX'; sc['magenheim_authored_actions']='Capcrawler_Idle,Capcrawler_Scuttle,Capcrawler_Turn,Capcrawler_AttackLeft,Capcrawler_AttackRight,Capcrawler_AttackFront,Capcrawler_Guard,Capcrawler_Hit,Capcrawler_Stagger,Capcrawler_Death'; sc['magenheim_animation_manifest']='idle,scuttle,turn,attack-left,attack-right,attack-front,guard,hit,stagger,death'; sc['magenheim_carapace_language']='layered-fungal-rim-scutes+crown'; sc['magenheim_ground_contact']='8-tarsal-pads+8-terminal-claws'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True); print(f'AUTHORED Capcrawler r5: {len(meshes)} meshes / {len(arm.data.bones)} bones / articulated feet -> {OUT.name}',flush=True)
