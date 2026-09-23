#!/usr/bin/env python3
"""Author the Sulfurous Wastes Ashmite production source model and HOST-SWARM-HEXAPOD rig."""
from pathlib import Path
from math import pi, sin, cos
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-ashmite.blend'
TEX=ROOT/'assets/textures/underworld/creatures/ashmite'
OUT.parent.mkdir(parents=True,exist_ok=True)

def image(stem,kind):
    p=TEX/f'{stem}-{kind}.png'
    if not p.exists(): raise RuntimeError(f'Missing {p}; run generate-underworld-ashmite-textures.py first')
    im=bpy.data.images.load(str(p),check_existing=True)
    if kind in ('normal','roughness'): im.colorspace_settings.name='Non-Color'
    return im

def material(name,stem):
    m=bpy.data.materials.new(name); m.use_nodes=True; n=m.node_tree.nodes; l=m.node_tree.links; n.clear()
    out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); l.new(bs.outputs['BSDF'],out.inputs['Surface']); uv=n.new('ShaderNodeTexCoord')
    a=n.new('ShaderNodeTexImage'); a.image=image(stem,'albedo'); l.new(uv.outputs['UV'],a.inputs['Vector']); l.new(a.outputs['Color'],bs.inputs['Base Color'])
    r=n.new('ShaderNodeTexImage'); r.image=image(stem,'roughness'); l.new(uv.outputs['UV'],r.inputs['Vector']); l.new(r.outputs['Color'],bs.inputs['Roughness'])
    t=n.new('ShaderNodeTexImage'); t.image=image(stem,'normal'); l.new(uv.outputs['UV'],t.inputs['Vector']); nm=n.new('ShaderNodeNormalMap'); l.new(t.outputs['Color'],nm.inputs['Color']); l.new(nm.outputs['Normal'],bs.inputs['Normal']); return m

def organic(name,loc,scale,mat,sub=2,seed=0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for v in o.data.vertices: v.co*=1+.035*sin(v.co.x*43+seed)+.025*cos(v.co.y*37-seed)
    o.data.materials.append(mat); return o

def segment(name,a,b,r,mat,verts=14):
    d=Vector(tuple(b[i]-a[i] for i in range(3))); mid=tuple((a[i]+b[i])/2 for i in range(3)); bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=mid)
    o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(mat); return o

def cone(name,loc,r,depth,mat,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=14,radius1=r,radius2=r*.12,depth=depth,location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.data.materials.append(mat); return o

def uv(o):
    u=o.data.uv_layers.new(name='AshmiteUV')
    for p in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(p.normal[i]))
        for li in p.loop_indices:
            c=o.data.vertices[o.data.loops[li].vertex_index].co; xy=((c.y,c.z),(c.x,c.z),(c.x,c.y))[axis]; u.data[li].uv=((xy[0]*3.7)%1,(xy[1]*3.7)%1)
    o.data.uv_layers.active=u

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
chitin=material('Ashmite_HeatChitin','heat-chitin'); sulfur=material('Ashmite_SulfurCrust','sulfur-crust'); joint=material('Ashmite_ProtectedJoint','protected-joint'); mouth=material('Ashmite_Mouthpart','mouthpart'); eye=material('Ashmite_Eye','eye')
parts={}
def keep(o,b): parts[o.name]=b; return o
# 0.36 m long, extremely low center of mass: not a mushroom-insect reskin.
keep(organic('Ashmite_Thorax',(0,0,.085),(.095,.105,.052),chitin,3,1),'Thorax'); keep(organic('Ashmite_Abdomen',(0,-.105,.080),(.105,.125,.060),chitin,3,2),'Abdomen'); keep(organic('Ashmite_Head',(0,.105,.070),(.080,.072,.048),chitin,3,3),'Head')
# Five physically raised dorsal armor scutes, with sulfur accretion concentrated on fracture ridges.
for i,y in enumerate((-.155,-.095,-.035,.030,.085),1):
    z=.135-abs(y)*.10; keep(organic(f'Ashmite_DorsalScute_{i}',(0,y,z),(.092 if i<4 else .075,.047,.018),chitin,2,20+i),'Abdomen' if i<4 else 'Thorax')
    for side in (-1,1): keep(segment(f'Ashmite_SulfurRidge_{i}_{"L" if side<0 else "R"}',(side*.020,y,z+.018),(side*.060,y-.010,z+.025),.006,sulfur,10),'Abdomen' if i<4 else 'Thorax')
# Six recessed, visibly soft coxal joints and rapid legs. Terminal claws are vent-scraping tools.
legpts={}
for pair,y in enumerate((.065,0,-.070),1):
    for side in (-1,1):
        s='L' if side<0 else 'R'; hip=(side*.068,y,.070); knee=(side*.125,y+(.020 if pair==1 else -.008),.048); ankle=(side*.165,y-.012,.018); toe=(side*.188,y+.020,.008); legpts[(s,pair)]=(hip,knee,ankle,toe)
        keep(organic(f'Ashmite_{s}_Joint{pair}',hip,(.020,.022,.017),joint,2,40+pair),f'{s}_Coxa{pair}'); keep(segment(f'Ashmite_{s}_Femur{pair}',hip,knee,.014,chitin),f'{s}_Coxa{pair}'); keep(segment(f'Ashmite_{s}_Tibia{pair}',knee,ankle,.011,chitin),f'{s}_Femur{pair}'); keep(segment(f'Ashmite_{s}_Tarsus{pair}',ankle,toe,.007,joint),f'{s}_Tarsus{pair}'); keep(cone(f'Ashmite_{s}_Scraper{pair}',(side*.198,toe[1]+.010,.007),.009,.030,mouth,(pi/2,0,0)),f'{s}_Tarsus{pair}')
# Robust paired mandibles plus central mineral scraper for vent/carrion scavenging.
for side in (-1,1):
    s='L' if side<0 else 'R'; keep(segment(f'Ashmite_Mandible_{s}',(side*.034,.142,.065),(side*.060,.195,.045),.014,mouth),f'Mandible_{s}')
keep(cone('Ashmite_VentScraper',(0,.185,.048),.018,.050,sulfur,(pi/2,0,0)),'Jaw')
for side in (-1,1):
    s='L' if side<0 else 'R'; keep(organic(f'Ashmite_Eye_{s}',(side*.047,.151,.085),(.013,.009,.012),eye,2,70),'Head')
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']: uv(o)
# Shared HOST-SWARM-HEXAPOD vocabulary retained for runtime compatibility.
bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_Ashmite_HOST_SWARM_HEXAPOD'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.07)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
thor=bone('Thorax',(0,0,.045),(0,0,.115),root); abd=bone('Abdomen',(0,-.015,.075),(0,-.17,.08),thor); head=bone('Head',(0,.03,.07),(0,.14,.07),thor); jaw=bone('Jaw',(0,.12,.055),(0,.205,.045),head)
for (s,p),(hip,knee,ankle,toe) in legpts.items(): c=bone(f'{s}_Coxa{p}',hip,knee,thor); f=bone(f'{s}_Femur{p}',knee,ankle,c); bone(f'{s}_Tarsus{p}',ankle,toe,f)
for side in ('L','R'):
    sg=-1 if side=='L' else 1; bone(f'Mandible_{side}',(sg*.025,.13,.06),(sg*.065,.205,.045),head)
bone('AttackOrigin',(0,.15,.05),(0,.235,.05),head); bone('ScavengeOrigin',(0,.17,.035),(0,.24,.025),jaw); bone('HitCenter',(0,0,.065),(0,0,.12),root); bone('SulfurFX',(0,-.06,.13),(0,-.06,.19),thor); bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
# Bind each rigid anatomical piece to its intended deformation bone.
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']:
    b=parts.get(o.name,'Thorax'); mod=o.modifiers.new('AshmiteArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=b); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')
# Production source gates.
meshes=[x for x in bpy.context.scene.objects if x.type=='MESH']; dims=[max((v.co.y for v in o.data.vertices),default=0)+o.location.y for o in meshes]; mins=[min((v.co.y for v in o.data.vertices),default=0)+o.location.y for o in meshes]; length=max(dims)-min(mins)
if not .25<=length<=.45: raise RuntimeError(f'Ashmite length outside 0.25-0.45m: {length:.3f}')
if len(meshes)<45: raise RuntimeError(f'Insufficient anatomical breakup: {len(meshes)} meshes')
if sum(len(o.data.polygons) for o in meshes)<6500: raise RuntimeError('Ashmite source detail below 6500 polygons')
for o in meshes:
    if not o.data.uv_layers.get('AshmiteUV'): raise RuntimeError(f'{o.name}: missing AshmiteUV')
arm['host_family']='HOST-SWARM-HEXAPOD'; arm['production_scale_m']=round(length,3); arm['material_language']='heat-cracked chitin / sulfur crust / wet protected joints / scorched mouthparts / no emission'; arm['readability_contract']='low armored vent scavenger; five raised scutes; six recessed joints; six rapid legs; physical vent scrapers'; arm['planned_actions']='Idle,Scavenge,Walk,Scuttle,TurnLeft,TurnRight,Bite,Hit,Stagger,Death'
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Authored {OUT}: {length:.3f}m, {len(meshes)} meshes, {sum(len(o.data.polygons) for o in meshes)} polygons')
