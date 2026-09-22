#!/usr/bin/env python3
"""Author the Blackwater Deep Hunter production source model in Blender.

Art-only source authoring for the 6-9 m apex HOST-AQUATIC-FISH extension.
The creature uses a long deformable trunk/tail, physical attack anatomy and readable
fin control surfaces. Valheim remains authoritative for navigation/world movement.
"""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-deep-hunter.blend'
LENGTH=7.6
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough=.7):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    return m
DORSAL=mat('DeepHunter_DorsalHide',(.018,.026,.030),.78)
VENTRAL=mat('DeepHunter_VentralHide',(.105,.112,.100),.62)
FIN=mat('DeepHunter_FinTissue',(.025,.043,.047),.68)
MOUTH=mat('DeepHunter_OralTissue',(.145,.035,.032),.48)
TOOTH=mat('DeepHunter_Tooth',(.58,.55,.43),.44)
SCAR=mat('DeepHunter_ScarTissue',(.19,.085,.072),.64)
EYE=mat('DeepHunter_Eye',(.055,.075,.065),.25)
parts={}
def uv(o):
    layer=o.data.uv_layers.new(name='DeepHunterUV'); xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]
    xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys); dx=max(xmax-xmin,.001); dy=max(ymax-ymin,.001)
    for p in o.data.polygons:
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co; layer.data[li].uv=((co.x-xmin)/dx,(co.y-ymin)/dy)
def ico(name,loc,scale,material,bone,sub=3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o); parts[name]=bone; return o
def mesh(name,verts,faces,material,bone):
    me=bpy.data.meshes.new(name+'Mesh'); me.from_pydata(verts,[],faces); me.update(); o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o); me.materials.append(material); uv(o); parts[name]=bone; return o
def cone(name,loc,radius,depth,material,bone,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=radius,radius2=0,depth=depth,location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.data.materials.append(material); uv(o); parts[name]=bone; return o

# 7.6m apex silhouette: huge wedge head, muscular shoulders, tapering articulated trunk.
ico('Head',(0,2.35,.12),(1.05,1.18,.78),DORSAL,'Head',4)
ico('Shoulders',(0,1.15,.06),(.98,1.18,.76),DORSAL,'Spine_1',4)
ico('TrunkA',(0,.05,.03),(.86,1.05,.68),DORSAL,'Spine_2',4)
ico('TrunkB',(0,-.92,.02),(.72,.94,.57),DORSAL,'Spine_3',4)
ico('PeduncleA',(0,-1.78,.02),(.54,.78,.44),DORSAL,'Spine_4',3)
ico('PeduncleB',(0,-2.47,.02),(.40,.65,.34),DORSAL,'Spine_5',3)
ico('TailBase',(0,-3.02,.02),(.29,.52,.27),DORSAL,'Tail_1',3)
ico('VentralChest',(0,1.28,-.48),(.80,1.42,.32),VENTRAL,'Spine_1',3)
ico('VentralTrunk',(0,-.45,-.43),(.63,1.15,.27),VENTRAL,'Spine_3',3)
# A hinged predatory jaw and deep oral cavity are geometry, not a painted mouth line.
ico('LowerJaw',(0,2.78,-.38),(.82,.78,.20),MOUTH,'Jaw',3)
ico('OralCavity',(0,2.66,-.18),(.70,.65,.23),MOUTH,'Jaw',3)
# Large stabilizers and tall caudal blade keep the apex silhouette readable at combat distance.
for side,s in [('L',-1),('R',1)]:
    mesh(f'PectoralFin_{side}',[(.62*s,1.15,-.10),(1.72*s,.55,-.25),(1.28*s,-.48,-.20),(.55*s,.10,-.08)],[(0,1,2),(0,2,3)],FIN,f'Fin_{side}')
    ico(f'Eye_{side}',(.78*s,2.72,.32),(.105,.075,.105),EYE,'Head',2)
mesh('DorsalFin',[(0,.72,.63),(-.22,.35,.62),(0,-1.20,1.48),(.22,.35,.62)],[(0,1,2),(0,2,3)],FIN,'Spine_2')
mesh('CaudalFin',[(0,-3.32,.02),(-.12,-3.47,1.20),(0,-3.58,.10),(-.10,-3.46,-1.05),(.12,-3.47,1.20),(.10,-3.46,-1.05)],[(0,1,2),(0,2,3),(0,2,4),(0,5,2)],FIN,'Tail_2')
# Forty physical teeth create a coarse trap-jaw silhouette.
for side,s in [('L',-1),('R',1)]:
    for i in range(10):
        y=2.18+i*.105; x=(.31+.018*(i%3))*s; r=.035+.007*(i%2)
        cone(f'ToothUpper_{side}_{i+1}',(x,y,-.12),r,.22,TOOTH,'Head',rot=(math.pi,0,0))
        cone(f'ToothLower_{side}_{i+1}',(x*.96,y+.025,-.39),r*.92,.20,TOOTH,'Jaw')
# Old physical scar ridges distinguish the hero predator from a clean enlarged fish.
for i,(x,y,z,rz) in enumerate([(-.72,1.70,.38,-.35),(-.76,1.38,.30,-.20),(.67,.65,.42,.28),(.62,.30,.40,.40)],1):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(x,y,z),rotation=(0,0,rz)); o=bpy.context.object; o.name=f'ScarRidge_{i}'; o.scale=(.055,.48,.035); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(SCAR); uv(o); parts[o.name]='Spine_1' if y<1 else 'Head'

armdata=bpy.data.armatures.new('HOST-AQUATIC-FISH-APEX'); arm=bpy.data.objects.new('DeepHunterRig',armdata); bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,0,0),(0,0,.35)); s1=bone('Spine_1',(0,1.75,.05),(0,1.05,.05),root); s2=bone('Spine_2',(0,1.05,.05),(0,.25,.04),s1); s3=bone('Spine_3',(0,.25,.04),(0,-.65,.03),s2); s4=bone('Spine_4',(0,-.65,.03),(0,-1.45,.03),s3); s5=bone('Spine_5',(0,-1.45,.03),(0,-2.18,.02),s4); t1=bone('Tail_1',(0,-2.18,.02),(0,-2.82,.02),s5); t2=bone('Tail_2',(0,-2.82,.02),(0,-3.58,.02),t1)
head=bone('Head',(0,1.75,.08),(0,3.18,.08),s1); jaw=bone('Jaw',(0,2.10,-.24),(0,3.24,-.24),head)
for side,s in [('L',-1),('R',1)]: bone(f'Fin_{side}',(.55*s,1.10,-.05),(1.70*s,.40,-.20),s1)
bone('AttackOrigin',(0,2.68,-.18),(0,3.65,-.18),head); bone('BreachCenter',(0,.35,.08),(0,.35,.65),s2); bone('HitCenter',(0,.45,.02),(0,1.10,.02),root); bone('TailTip',(0,-2.95,.02),(0,-3.70,.02),t2); bone('MouthFX',(0,2.55,-.18),(0,3.45,-.18),head)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('DeepHunterArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if not 6.0 <= LENGTH <= 9.0: raise RuntimeError('Deep Hunter length outside production band')
if len(meshes)<58: raise RuntimeError(f'Deep Hunter anatomy regression: {len(meshes)} mesh parts')
if tris<30000: raise RuntimeError(f'Deep Hunter source triangle floor regression: {tris}')
if len([o for o in meshes if o.name.startswith('Tooth')])!=40: raise RuntimeError('Deep Hunter must retain exactly 40 modeled teeth')
for o in meshes:
    if not o.data.uv_layers.get('DeepHunterUV'): raise RuntimeError(f'Missing UVs: {o.name}')
for required in ['Spine_1','Spine_2','Spine_3','Spine_4','Spine_5','Tail_1','Tail_2','Head','Jaw','Fin_L','Fin_R','AttackOrigin','BreachCenter','HitCenter','TailTip','MouthFX']:
    if required not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {required}')
arm['magenheim_asset']='deep-hunter'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-AQUATIC-FISH-APEX'; arm['length_m']=LENGTH
arm['role']='blackwater-apex-hero-predator'; arm['modeled_teeth']=40; arm['body_tail_deformation_bones']=7
arm['animation_contract']='swim-idle,cruise,sprint,turn-left,turn-right,bite,ram,breach,breach-recover,tail-strike,hit,stagger,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')
