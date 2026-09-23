#!/usr/bin/env python3
"""Author the Sulfurous Wastes Basalt Crawler production source model.

The Crawler is a low defensive lithic animal, not a scaled insect proxy. World motion
remains Valheim-owned; this source authors physical armor, protected face and rig only.
"""
from pathlib import Path
from math import pi
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-basalt-crawler.blend'
OUT.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,c,r=.7):
    m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF'); b.inputs['Base Color'].default_value=(*c,1); b.inputs['Roughness'].default_value=r
    return m
basalt=mat('BasaltCrawler_Armor',(0.045,.042,.038),.93)
edge=mat('BasaltCrawler_FractureEdge',(.105,.075,.052),.82)
joint=mat('BasaltCrawler_ProtectedTissue',(.16,.07,.045),.46)
mouth=mat('BasaltCrawler_Mouth',(.13,.045,.035),.52)
eye=mat('BasaltCrawler_Eye',(.10,.045,.025),.30)
parts={}
def organic(n,loc,scale,ma,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=n; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(ma); return o
def seg(n,a,b,r,ma,v=10):
    d=Vector(b)-Vector(a); bpy.ops.mesh.primitive_cylinder_add(vertices=v,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2); o=bpy.context.object; o.name=n; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(ma); return o
def cone(n,loc,r,d,ma,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=r,radius2=.002,depth=d,location=loc,rotation=rot); o=bpy.context.object; o.name=n; o.data.materials.append(ma); return o
def keep(o,b): parts[o.name]=b; return o
def uv(o):
    u=o.data.uv_layers.new(name='BasaltCrawlerUV')
    for p in o.data.polygons:
        ax=max(range(3),key=lambda i:abs(p.normal[i]))
        for li in p.loop_indices:
            c=o.data.vertices[o.data.loops[li].vertex_index].co; q=((c.y,c.z),(c.x,c.z),(c.x,c.y))[ax]; u.data[li].uv=((q[0]*1.35)%1,(q[1]*1.35)%1)

# 1.30m long, broad and very low. The face retracts beneath the forward armor shelf.
keep(organic('BC_Core',(0,0,.39),(.48,.56,.25),joint,3),'Body')
keep(organic('BC_Abdomen',(0,-.43,.37),(.39,.40,.21),joint,3),'Abdomen')
keep(organic('BC_Face',(0,.47,.34),(.27,.25,.15),mouth,3),'Head')
# Seven overlapping transverse armor slabs produce a stone-shingle silhouette and real guard surface.
plates=[(.45,.56,.34),(.28,.59,.37),(.10,.62,.39),(-.09,.61,.40),(-.28,.57,.38),(-.46,.50,.34),(-.61,.40,.28)]
for i,(y,w,z) in enumerate(plates,1):
    p=keep(organic(f'BC_Plate_{i}',(0,y,z+.14),(w,.22,.105),basalt,2),'Body' if i<5 else 'Abdomen')
    # broken raised keel makes plate overlap readable from gameplay camera
    keep(seg(f'BC_Keel_{i}',(-w*.42,y,z+.24),(w*.42,y,z+.24),.028,edge,8),'Body' if i<5 else 'Abdomen')
# Forward brow is physically massive: guard pose lowers this over the face.
keep(organic('BC_BrowShield',(0,.62,.52),(.42,.24,.14),basalt,3),'Brow')
for s in (-1,1):
    side='L' if s<0 else 'R'
    keep(organic(f'BC_Eye_{side}',(s*.18,.60,.37),(.035,.025,.028),eye,2),'Head')
    keep(cone(f'BC_Mandible_{side}',(s*.15,.73,.31),.055,.28,edge,(pi/2,0,s*.18)),'Mandible_'+side)
# Six load-bearing legs, each three deforming stages; front pair is heavier for ram/guard bracing.
legs={}
for pair,y in enumerate((.31,-.04,-.39),1):
  for s in (-1,1):
    side='L' if s<0 else 'R'; tag=f'{side}{pair}'; heavy=1.22 if pair==1 else 1.0
    hip=(s*.34,y,.36); knee=(s*.57,y+(.07 if pair==1 else 0),.27); foot=(s*.68,y+.12,.08)
    legs[tag]=(hip,knee,foot)
    keep(organic(f'BC_{tag}_Joint',hip,(.075,.08,.065),joint,2),f'{tag}_Upper')
    keep(seg(f'BC_{tag}_Upper',hip,knee,.065*heavy,basalt),f'{tag}_Upper')
    keep(seg(f'BC_{tag}_Lower',knee,foot,.052*heavy,basalt),f'{tag}_Lower')
    keep(organic(f'BC_{tag}_Foot',foot,(.10*heavy,.14,.055),edge,2),f'{tag}_Foot')
    for d in (-1,0,1): keep(cone(f'BC_{tag}_Toe_{d+2}',(foot[0]+s*.025,foot[1]+.09+d*.035,.055),.016,.085,edge,(pi/2,0,0)),f'{tag}_Foot')
# Frontal battering bosses prevent the ram from reading as a headbutting beetle.
for s in (-1,1): keep(cone(f'BC_RamBoss_{"L" if s<0 else "R"}',(s*.27,.73,.48),.07,.32,basalt,(pi/2,0,s*.10)),'Brow')
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']: uv(o)

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_BasaltCrawler_HOST_LOW_CRAWLER'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.16)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
body=bone('Body',(0,-.30,.34),(0,.28,.40),root); abdomen=bone('Abdomen',(0,-.30,.34),(0,-.68,.34),body); head=bone('Head',(0,.28,.39),(0,.60,.36),body); brow=bone('Brow',(0,.40,.48),(0,.72,.50),head)
for tag,(hip,knee,foot) in legs.items():
    u=bone(f'{tag}_Upper',hip,knee,body if tag.endswith(('1','2')) else abdomen); l=bone(f'{tag}_Lower',knee,foot,u); bone(f'{tag}_Foot',foot,(foot[0],foot[1]+.14,foot[2]),l)
ml=bone('Mandible_L',(-.12,.55,.31),(-.18,.79,.29),head); mr=bone('Mandible_R',(.12,.55,.31),(.18,.79,.29),head)
bone('AttackOrigin',(0,.60,.36),(0,.88,.36),head); bone('RamOrigin',(0,.56,.49),(0,.91,.49),brow); bone('HitCenter',(0,-.04,.40),(0,-.04,.64),body)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']:
    b=parts.get(o.name,'Body'); mod=o.modifiers.new('BasaltCrawlerArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=b); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')
meshes=[x for x in bpy.context.scene.objects if x.type=='MESH']; polys=sum(len(o.data.polygons) for o in meshes)
required=['Root','Body','Abdomen','Head','Brow','Mandible_L','Mandible_R','AttackOrigin','RamOrigin','HitCenter']+[f'{s}{i}_{q}' for s in ('L','R') for i in (1,2,3) for q in ('Upper','Lower','Foot')]
missing=[n for n in required if n not in arm.data.bones]
if missing: raise RuntimeError(f'Missing Basalt Crawler bones: {missing}')
if len(meshes)<55: raise RuntimeError(f'Basalt Crawler breakup too low: {len(meshes)} meshes < 55')
if polys<8000: raise RuntimeError(f'Basalt Crawler source fidelity too low: {polys} polygons < 8000')
if sum(1 for o in meshes if o.name.startswith('BC_Plate_'))!=7: raise RuntimeError('Expected seven physical overlapping armor plates')
if any(not o.data.uv_layers.get('BasaltCrawlerUV') for o in meshes): raise RuntimeError('Basalt Crawler mesh missing BasaltCrawlerUV')
arm['host_family']='HOST-LOW-CRAWLER'; arm['production_length_target_m']=1.30
arm['material_language']='dry fractured basalt plates / warmer fracture edges / wet protected joints and recessed face'
arm['readability_contract']='very low broad crawler; seven overlapping stone slabs; face protected below articulated brow; six bracing legs; paired frontal ram bosses'
arm['planned_actions']='Idle,Scuttle,TurnLeft,TurnRight,AttackLeft,AttackRight,FrontStrike,Guard,Ram,Hit,Stagger,Death'
arm['root_motion_policy']='Root translation reserved for Valheim runtime; authored actions remain in-place'
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Authored {OUT}: {len(meshes)} meshes, {polys} polygons, 7 plates, 6 articulated legs')
