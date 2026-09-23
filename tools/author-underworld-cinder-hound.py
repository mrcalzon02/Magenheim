#!/usr/bin/env python3
"""Author the Sulfurous Wastes Cinder Hound production source model and HOST-QUADRUPED rig.

This is geometry/rig source authoring. It deliberately does not invent runtime AI or root motion.
The hound must remain a lean heat-adapted pack predator, not a recolored wolf.
"""
from pathlib import Path
from math import pi
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-cinder-hound.blend'
OUT.parent.mkdir(parents=True,exist_ok=True)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough=.7,metal=0.0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough; bs.inputs['Metallic'].default_value=metal
    return m
hide=mat('CinderHound_ScorchedHide',(0.075,.055,.045),.82)
basalt=mat('CinderHound_BasaltScute',(.055,.05,.045),.9)
vent=mat('CinderHound_VentTissue',(.25,.075,.035),.42)
mouth=mat('CinderHound_Mouth',(.16,.045,.035),.48)
tooth=mat('CinderHound_Tooth',(.58,.49,.34),.64)
eye=mat('CinderHound_Eye',(.12,.055,.025),.28)
parts={}
def organic(name,loc,scale,material,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return o
def segment(name,a,b,r,material,verts=12):
    d=Vector(b)-Vector(a); bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(material); return o
def cone(name,loc,r,depth,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r,radius2=.001,depth=depth,location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.data.materials.append(material); return o
def keep(o,b): parts[o.name]=b; return o
def uv(o):
    u=o.data.uv_layers.new(name='CinderHoundUV')
    for p in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(p.normal[i]))
        for li in p.loop_indices:
            c=o.data.vertices[o.data.loops[li].vertex_index].co; xy=((c.y,c.z),(c.x,c.z),(c.x,c.y))[axis]; u.data[li].uv=((xy[0]*1.7)%1,(xy[1]*1.7)%1)

# 1.25m shoulder target. Deep chest, tucked abdomen and long limbs create heat-shedding silhouette.
keep(organic('CH_Chest',(0,.12,.92),(.34,.46,.43),hide,3),'Chest')
keep(organic('CH_Abdomen',(0,-.42,.88),(.25,.43,.29),hide,3),'Spine')
keep(organic('CH_Pelvis',(0,-.72,.86),(.30,.30,.30),hide,3),'Pelvis')
keep(organic('CH_Neck',(0,.47,1.08),(.24,.32,.31),hide,3),'Neck')
keep(organic('CH_Head',(0,.72,1.13),(.25,.34,.25),hide,3),'Head')
keep(organic('CH_Muzzle',(0,.99,1.07),(.18,.27,.15),hide,2),'Head')
keep(organic('CH_LowerJaw',(0,.99,1.00),(.16,.25,.08),mouth,2),'Jaw')
# Heat vents are physical anatomy: six paired rib chimneys, open toward the rear to read while sprinting.
for i,y in enumerate((.22,.08,-.07,-.22,-.37,-.51),1):
    for side in (-1,1):
        s='L' if side<0 else 'R'; x=side*(.27-(i-1)*.008)
        keep(segment(f'CH_VentRib_{s}_{i}',(x*.72,y,.96),(x,y,1.08),.035,basalt,10),'Chest' if i<4 else 'Spine')
        keep(organic(f'CH_VentMouth_{s}_{i}',(x,y-.025,1.095),(.052,.072,.035),vent,2),'Chest' if i<4 else 'Spine')
# Broken dorsal heat armor: separate plates preserve flexion and distinguish the animal from a wolf.
for i,(y,z,w) in enumerate(((.32,1.25,.18),(.10,1.29,.20),(-.13,1.25,.19),(-.36,1.18,.16),(-.60,1.10,.14)),1):
    keep(organic(f'CH_DorsalScute_{i}',(0,y,z),(w,.13,.055),basalt,2),'Chest' if i<3 else ('Spine' if i<5 else 'Pelvis'))
# Ears are short heat-scarred fins, not wolf triangles.
for side in (-1,1):
    s='L' if side<0 else 'R'; keep(cone(f'CH_Ear_{s}',(side*.17,.69,1.36),.09,.26,hide,(0,0,side*.25)),'Head')
    keep(organic(f'CH_Eye_{s}',(side*.17,.91,1.17),(.035,.025,.030),eye,2),'Head')
# Four long digitigrade limbs. Each has upper/lower/hock/foot geometry and exposed protected joint tissue.
leg={}
for side in (-1,1):
  for fore,y in ((True,.24),(False,-.61)):
    s='L' if side<0 else 'R'; tag=('F' if fore else 'H')+s
    hip=(side*.25,y,.91); knee=(side*.31,y+(.16 if fore else .10),.56); hock=(side*.27,y-(.05 if fore else .20),.24); foot=(side*.25,y+(.18 if fore else .13),.07)
    leg[tag]=(hip,knee,hock,foot)
    keep(organic(f'CH_{tag}_Joint',hip,(.075,.075,.065),vent,2),f'{tag}_Upper')
    keep(segment(f'CH_{tag}_Upper',hip,knee,.075,hide),f'{tag}_Upper'); keep(segment(f'CH_{tag}_Lower',knee,hock,.060,hide),f'{tag}_Lower'); keep(segment(f'CH_{tag}_Hock',hock,foot,.045,hide),f'{tag}_Hock')
    keep(organic(f'CH_{tag}_Foot',foot,(.095,.16,.055),basalt,2),f'{tag}_Foot')
    for d in (-1,0,1): keep(cone(f'CH_{tag}_Claw_{d+2}',(foot[0]+d*.04,foot[1]+.14,foot[2]-.01),.018,.10,tooth,(pi/2,0,0)),f'{tag}_Foot')
# Mouth has modeled teeth and a venting throat organ visible during bite/lunge telegraphs.
for row,z in ((1,1.075),(-1,1.005)):
    for i in range(8):
        x=(i-3.5)*.038; keep(cone(f'CH_Tooth_{"U" if row>0 else "L"}_{i+1}',(x,1.17,z),.014,.075,tooth,(pi/2,0,0)),'Head' if row>0 else 'Jaw')
keep(organic('CH_ThroatVent',(0,.79,.94),(.11,.16,.09),vent,2),'Jaw')
# Segmented counterbalance tail.
tailpts=[(0,-.88,.91),(0,-1.15,.88),(0,-1.42,.82),(0,-1.66,.73)]
for i in range(3): keep(segment(f'CH_Tail_{i+1}',tailpts[i],tailpts[i+1],.10-i*.022,hide),f'Tail_{i+1}')

for o in [x for x in bpy.context.scene.objects if x.type=='MESH']: uv(o)
# Dedicated quadruped host: code-driven world motion at Root, deforming skeleton beneath.
bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_CinderHound_HOST_QUADRUPED'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.22)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
pel=bone('Pelvis',(0,-.72,.72),(0,-.50,.88),root); spine=bone('Spine',(0,-.50,.88),(0,.02,.94),pel); chest=bone('Chest',(0,.02,.94),(0,.38,1.04),spine); neck=bone('Neck',(0,.38,1.04),(0,.62,1.12),chest); head=bone('Head',(0,.62,1.12),(0,.93,1.11),neck); jaw=bone('Jaw',(0,.83,1.04),(0,1.18,1.00),head)
for tag,(hip,knee,hock,foot) in leg.items():
    parent=chest if tag.startswith('F') else pel; u=bone(f'{tag}_Upper',hip,knee,parent); l=bone(f'{tag}_Lower',knee,hock,u); h=bone(f'{tag}_Hock',hock,foot,l); bone(f'{tag}_Foot',foot,(foot[0],foot[1]+.18,foot[2]),h)
t1=bone('Tail_1',tailpts[0],tailpts[1],pel); t2=bone('Tail_2',tailpts[1],tailpts[2],t1); bone('Tail_3',tailpts[2],tailpts[3],t2)
bone('AttackOrigin',(0,.91,1.06),(0,1.27,1.06),head); bone('MouthFX',(0,1.00,1.04),(0,1.22,1.04),head); bone('RibVentFX',(0,-.10,1.12),(0,-.10,1.35),chest); bone('HitCenter',(0,-.12,.90),(0,-.12,1.15),spine); bone('PackCallFX',(0,.72,1.22),(0,.72,1.42),head)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']:
    b=parts.get(o.name,'Chest'); mod=o.modifiers.new('CinderHoundArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=b); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')
meshes=[x for x in bpy.context.scene.objects if x.type=='MESH']; polys=sum(len(o.data.polygons) for o in meshes)
# Source fidelity gates: physical vent anatomy, articulated digitigrade limbs, sufficient breakup and UVs.
required=['Root','Pelvis','Spine','Chest','Neck','Head','Jaw','Tail_1','Tail_2','Tail_3','AttackOrigin','MouthFX','RibVentFX','HitCenter','PackCallFX']+[f'{tag}_{seg}' for tag in ('FL','FR','HL','HR') for seg in ('Upper','Lower','Hock','Foot')]
missing=[n for n in required if n not in arm.data.bones]
if missing: raise RuntimeError(f'Missing Cinder Hound bones: {missing}')
if len(meshes)<70: raise RuntimeError(f'Cinder Hound anatomical breakup too low: {len(meshes)} meshes < 70')
if polys<12000: raise RuntimeError(f'Cinder Hound source fidelity too low: {polys} polygons < 12000')
if sum(1 for o in meshes if o.name.startswith('CH_VentMouth_'))!=12: raise RuntimeError('Expected 12 physical rib vents')
if sum(1 for o in meshes if o.name.startswith('CH_Tooth_'))!=16: raise RuntimeError('Expected 16 modeled teeth')
if any(not o.data.uv_layers.get('CinderHoundUV') for o in meshes): raise RuntimeError('Cinder Hound mesh missing CinderHoundUV')
arm['host_family']='HOST-QUADRUPED'; arm['production_shoulder_target_m']=1.25; arm['material_language']='scorched hide / broken basalt scutes / wet protected joints / physical rib and throat vents / mineral teeth'; arm['readability_contract']='lean heat-adapted pack predator; deep chest and tucked waist; twelve rib vents; digitigrade sprint limbs; broken dorsal heat armor; venting mouth'; arm['planned_actions']='Idle,VentIdle,Walk,PackSprint,TurnLeft,TurnRight,Bite,Lunge,PackCall,Hit,Stagger,Death'; arm['root_motion_policy']='Root translation reserved for Valheim runtime; authored actions must remain in-place'
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Authored {OUT}: {len(meshes)} meshes, {polys} polygons, 12 rib vents, 16 teeth')