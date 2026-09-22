#!/usr/bin/env python3
"""Author the Blackwater Deep Lantern Angler production source model in Blender.

Art-only source authoring for the 1.8-2.4 m HOST-AQUATIC-FISH uncommon predator.
The lure, expanding jaw/throat and light organ are physical production geometry.
Valheim remains authoritative for swimming/navigation/gameplay movement.
"""
from pathlib import Path
import math
import bpy

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-lantern-angler.blend'
LENGTH=2.12
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough=.7,emission=None,strength=0.0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    if emission:
        if 'Emission Color' in bs.inputs: bs.inputs['Emission Color'].default_value=(*emission,1); bs.inputs['Emission Strength'].default_value=strength
        elif 'Emission' in bs.inputs: bs.inputs['Emission'].default_value=(*emission,1)
    return m
SKIN=mat('LanternAngler_DorsalHide',(.025,.037,.041),.82); BELLY=mat('LanternAngler_VentralHide',(.075,.085,.078),.68)
FIN=mat('LanternAngler_FinTissue',(.035,.055,.058),.72); MOUTH=mat('LanternAngler_OralTissue',(.16,.045,.040),.55)
TOOTH=mat('LanternAngler_Tooth',(.54,.51,.39),.50); LURE=mat('LanternAngler_LureTissue',(.055,.085,.075),.61)
LIGHT=mat('LanternAngler_LightOrgan',(.16,.31,.27),.38,(.20,.78,.62),3.2)
parts={}
def uv(o):
    layer=o.data.uv_layers.new(name='LanternAnglerUV'); xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]
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

# Deep-bodied ambush silhouette with oversized head and narrow propulsion tail.
ico('Body',(0,-.08,.02),(.42,.70,.40),SKIN,'Spine_2',4); ico('VentralBody',(0,.00,-.19),(.35,.57,.22),BELLY,'Spine_2',3)
ico('Head',(0,.56,.04),(.46,.48,.40),SKIN,'Head',4); ico('LowerJaw',(0,.76,-.20),(.36,.35,.12),MOUTH,'Jaw',3)
ico('ThroatSac',(0,.47,-.30),(.30,.36,.18),MOUTH,'Throat',3); ico('TailMuscle',(0,-.67,.03),(.25,.37,.23),SKIN,'Spine_4',3)
# Expandable throat folds remain separate geometry so the pressure/ranged tell can visibly inflate.
for i,y in enumerate([.31,.43,.55],1): ico(f'ThroatFold_{i}',(0,y,-.315),(.28,.075,.10),MOUTH,'Throat',2)
# Ragged fins preserve silhouette at Valheim camera distance.
mesh('DorsalFin',[(-.18,.12,.31),(.18,.12,.31),(.13,-.42,.31),(0,-.53,.70),(-.15,-.25,.55)],[(0,1,4),(1,2,3),(1,3,4)],FIN,'Spine_3')
for side,s in [('L',-1),('R',1)]:
    mesh(f'PectoralFin_{side}',[(.28*s,.28,-.02),(.72*s,.02,-.10),(.43*s,-.30,-.07),(.24*s,-.18,-.03)],[(0,1,2),(0,2,3)],FIN,f'Fin_{side}')
mesh('CaudalFin',[(0,-.92,.04),(-.42,-1.08,.33),(0,-1.02,.04),(-.36,-1.12,-.25),(.42,-1.08,.33),(.36,-1.12,-.25)],[(0,1,2),(2,3,0),(0,2,4),(2,5,0)],FIN,'Tail_2')
# Illicium/lure is articulated geometry, not a glowing decal.
ico('LureBase',(0,.34,.38),(.07,.09,.07),LURE,'Lure_1',2); ico('LureStalkA',(0,.39,.56),(.045,.07,.22),LURE,'Lure_1',2)
ico('LureStalkB',(0,.50,.78),(.04,.08,.20),LURE,'Lure_2',2); ico('LureStalkTip',(0,.65,.91),(.035,.10,.16),LURE,'Lure_3',2)
ico('LureLightOrgan',(0,.76,.92),(.13,.16,.12),LIGHT,'Lure_3',3)
# Teeth are physical and irregular enough to read as a trap mouth, not a painted saw edge.
for side,s in [('L',-1),('R',1)]:
    for i in range(5):
        y=.57+i*.075; r=.020+(i%2)*.006
        cone(f'ToothUpper_{side}_{i+1}',(.13*s,y,-.075),r,.105,TOOTH,'Head',rot=(math.pi,0,0))
        cone(f'ToothLower_{side}_{i+1}',(.12*s,y+.018,-.205),r*.92,.095,TOOTH,'Jaw')

armdata=bpy.data.armatures.new('HOST-AQUATIC-FISH'); arm=bpy.data.objects.new('LanternAnglerRig',armdata); bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,0,-.03),(0,0,.15)); s1=bone('Spine_1',(0,.38,.04),(0,.18,.04),root); s2=bone('Spine_2',(0,.18,.04),(0,-.18,.04),s1); s3=bone('Spine_3',(0,-.18,.04),(0,-.48,.04),s2); s4=bone('Spine_4',(0,-.48,.04),(0,-.72,.04),s3); t1=bone('Tail_1',(0,-.72,.04),(0,-.92,.04),s4); t2=bone('Tail_2',(0,-.92,.04),(0,-1.13,.04),t1)
head=bone('Head',(0,.18,.04),(0,.83,.04),s1); jaw=bone('Jaw',(0,.47,-.13),(0,.92,-.13),head); throat=bone('Throat',(0,.28,-.20),(0,.68,-.28),head)
for side,s in [('L',-1),('R',1)]: bone(f'Fin_{side}',(.24*s,.22,0),(.70*s,-.02,-.08),s1)
l1=bone('Lure_1',(0,.32,.35),(0,.39,.59),head); l2=bone('Lure_2',(0,.39,.59),(0,.51,.80),l1); l3=bone('Lure_3',(0,.51,.80),(0,.77,.93),l2)
bone('AttackOrigin',(0,.66,-.09),(0,1.02,-.09),head); bone('MouthFX',(0,.62,-.10),(0,.94,-.10),head); bone('PressureFX',(0,.45,-.28),(0,.66,-.31),throat); bone('LureFX',(0,.70,.91),(0,.87,.94),l3); bone('HitCenter',(0,-.05,0),(0,.24,0),root); bone('TailTip',(0,-.92,.04),(0,-1.20,.04),t2)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('LanternAnglerArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if not 1.8 <= LENGTH <= 2.4: raise RuntimeError('Lantern Angler length outside production band')
if len(meshes) < 35: raise RuntimeError(f'Lantern Angler anatomy regression: {len(meshes)} mesh parts')
if tris < 12000: raise RuntimeError(f'Lantern Angler source triangle floor regression: {tris}')
if len([o for o in meshes if o.name.startswith('Tooth')]) != 20: raise RuntimeError('Lantern Angler must retain exactly 20 modeled teeth')
for o in meshes:
    if not o.data.uv_layers.get('LanternAnglerUV'): raise RuntimeError(f'Missing UVs: {o.name}')
for required in ['Jaw','Throat','Lure_1','Lure_2','Lure_3','Fin_L','Fin_R','Tail_1','Tail_2','AttackOrigin','MouthFX','PressureFX','LureFX','TailTip','HitCenter']:
    if required not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {required}')
arm['magenheim_asset']='lantern-angler'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-AQUATIC-FISH'; arm['length_m']=LENGTH
arm['role']='blackwater-uncommon-lure-pressure-predator'; arm['modeled_teeth']=20; arm['articulated_lure_segments']=3; arm['expanding_throat']=True
arm['animation_contract']='swim-idle,cruise,bank-left,bank-right,lure-idle,lure-tell,bite,pressure-tell,pressure-release,hit,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')
