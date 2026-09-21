#!/usr/bin/env python3
"""Author the Blackwater Deep Blackwater Lamprey production source model in Blender.

Art-only source authoring. Builds a flexible 0.92 m HOST-AQUATIC-FISH swimmer with
an actual circular oral disc, radial tooth geometry and a six-stage body chain for
attach/latch presentation. Valheim remains authoritative for movement and gameplay.
"""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-blackwater-lamprey.blend'
LENGTH=.92

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough=.7):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    return m
SKIN=mat('BlackwaterLamprey_DorsalHide',(.030,.043,.042),.78)
BELLY=mat('BlackwaterLamprey_VentralHide',(.090,.105,.095),.66)
ORAL=mat('BlackwaterLamprey_OralTissue',(.205,.080,.072),.55)
TOOTH=mat('BlackwaterLamprey_Tooth',(.54,.51,.39),.47)
FIN=mat('BlackwaterLamprey_FinTissue',(.042,.060,.057),.70)
parts={}

def uv(o):
    layer=o.data.uv_layers.new(name='BlackwaterLampreyUV')
    xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]; xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys); dx=max(xmax-xmin,.001); dy=max(ymax-ymin,.001)
    for p in o.data.polygons:
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co; layer.data[li].uv=((co.x-xmin)/dx,(co.y-ymin)/dy)

def ico(name,loc,scale,material,bone,sub=3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o); parts[name]=bone; return o

def torus(name,loc,major,minor,material,bone,rot=(math.pi/2,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=32,minor_segments=10,location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.data.materials.append(material); uv(o); parts[name]=bone; return o

def cone(name,loc,radius,depth,material,bone,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=radius,radius2=0,depth=depth,location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.data.materials.append(material); uv(o); parts[name]=bone; return o

def mesh(name,verts,faces,material,bone):
    me=bpy.data.meshes.new(name+'Mesh'); me.from_pydata(verts,[],faces); me.update(); o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o); me.materials.append(material); uv(o); parts[name]=bone; return o

# Overlapping anatomical masses preserve a sinuous silhouette while allowing each section to deform.
segments=[('HeadMass',(0,.335,.005),(.125,.155,.115),'Head',4),('ChestMass',(0,.205,.005),(.120,.155,.110),'Spine_1',4),('BodyMass_1',(0,.060,.004),(.110,.150,.102),'Spine_2',4),('BodyMass_2',(0,-.085,.003),(.100,.145,.094),'Spine_3',4),('BodyMass_3',(0,-.220,.002),(.086,.135,.082),'Spine_4',3),('TailMass_1',(0,-.340,.002),(.070,.120,.067),'Tail_1',3),('TailMass_2',(0,-.440,.002),(.050,.105,.050),'Tail_2',3)]
for name,loc,scale,bone,sub in segments: ico(name,loc,scale,SKIN,bone,sub)
ico('VentralChest',(0,.175,-.060),(.102,.205,.060),BELLY,'Spine_1',3)
# Circular sucker is physical silhouette geometry, not a painted mouth.
torus('OralDisc',(0,.478,.005),.100,.026,ORAL,'MouthRing')
ico('OralCavity',(0,.470,.005),(.078,.030,.078),ORAL,'MouthRing',3)
# Two radial tooth rings. Teeth point into the oral cavity and remain readable in latch closeups.
for ring,(count,radius,tooth_r) in enumerate([(16,.073,.009),(12,.046,.007)],1):
    for i in range(count):
        a=2*math.pi*i/count; x=math.cos(a)*radius; z=math.sin(a)*radius
        cone(f'OralTooth_R{ring}_{i+1}',(x,.493,z+.005),tooth_r,.040 if ring==1 else .032,TOOTH,'MouthRing',rot=(math.pi/2,0,0))
# Restrained fin silhouette; lamprey identity stays body-and-mouth led.
mesh('DorsalFin',[(-.025,-.12,.075),(.025,-.12,.075),(0,-.34,.060),(0,-.22,.145)],[(0,1,3),(1,2,3),(2,0,3)],FIN,'Spine_4')
mesh('CaudalFin',[(0,-.49,.00),(-.12,-.57,.075),(0,-.55,.00),(-.10,-.58,-.065),(.12,-.57,.075),(.10,-.58,-.065)],[(0,1,2),(2,3,0),(0,2,4),(2,0,5)],FIN,'Tail_2')

armdata=bpy.data.armatures.new('HOST-AQUATIC-FISH'); arm=bpy.data.objects.new('BlackwaterLampreyRig',armdata); bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,.05,-.02),(0,.05,.12)); s1=bone('Spine_1',(0,.31,0),(0,.18,0),root); s2=bone('Spine_2',(0,.18,0),(0,.04,0),s1); s3=bone('Spine_3',(0,.04,0),(0,-.10,0),s2); s4=bone('Spine_4',(0,-.10,0),(0,-.25,0),s3); t1=bone('Tail_1',(0,-.25,0),(0,-.38,0),s4); t2=bone('Tail_2',(0,-.38,0),(0,-.55,0),t1)
head=bone('Head',(0,.31,0),(0,.46,0),s1); mouth=bone('MouthRing',(0,.43,0),(0,.515,0),head)
# Jaw is retained for HOST-AQUATIC-FISH compatibility; on this jawless animal it controls oral-disc flare/compression.
jaw=bone('Jaw',(0,.43,-.025),(0,.515,-.025),mouth)
bone('Fin_L',(-.04,.12,0),(-.10,-.02,-.02),s2); bone('Fin_R',(.04,.12,0),(.10,-.02,-.02),s2)
bone('AttackOrigin',(0,.49,0),(0,.62,0),mouth); bone('LatchSocket',(0,.505,0),(0,.60,0),mouth); bone('MouthFX',(0,.48,0),(0,.58,0),mouth); bone('HitCenter',(0,.05,0),(0,.20,0),root); bone('TailTip',(0,-.48,0),(0,-.61,0),t2)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('BlackwaterLampreyArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if not .7 <= LENGTH <= 1.1: raise RuntimeError('Blackwater Lamprey length outside production band')
if len(meshes) < 35: raise RuntimeError(f'Lamprey anatomy regression: {len(meshes)} mesh parts')
if tris < 8000: raise RuntimeError(f'Lamprey source triangle floor regression: {tris}')
if sum(1 for o in meshes if o.name.startswith('OralTooth_')) != 28: raise RuntimeError('Lamprey radial tooth count regression')
for o in meshes:
    if not o.data.uv_layers.get('BlackwaterLampreyUV'): raise RuntimeError(f'Missing UVs: {o.name}')
for required in ['Jaw','MouthRing','Tail_1','Tail_2','AttackOrigin','LatchSocket','MouthFX','TailTip','HitCenter']:
    if required not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {required}')
arm['magenheim_asset']='blackwater-lamprey'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-AQUATIC-FISH'; arm['length_m']=LENGTH
arm['role']='blackwater-common-latch-predator'; arm['modeled_radial_teeth']=28; arm['oral_disc_geometry']=True
arm['animation_contract']='swim-idle,cruise,sprint,bank-left,bank-right,lunge,latch,attached-idle,detach,hit,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')
