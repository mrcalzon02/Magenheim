#!/usr/bin/env python3
"""Author the Fungal Forest Crowncap Brute production source model in Blender."""
from pathlib import Path
import math, bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-crowncap-brute.blend'
HEIGHT=3.12

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes,bpy.data.curves,bpy.data.materials,bpy.data.armatures):
    pass

def mat(name,color,rough=.7,metal=0.0,emit=None):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough; bs.inputs['Metallic'].default_value=metal
    if emit:
        bs.inputs['Emission Color'].default_value=(*emit,1); bs.inputs['Emission Strength'].default_value=.45
    return m
HIDE=mat('Brute_RootHide',(0.17,.12,.08),.88); PLATE=mat('Brute_CrownPlate',(.28,.20,.09),.82); GILL=mat('Brute_GillTissue',(.36,.13,.09),.62,emit=(.55,.12,.045)); FLESH=mat('Brute_MycelialFlesh',(.38,.34,.23),.72)
parts={}
def uv(o):
    if o.type=='MESH':
        layer=o.data.uv_layers.new(name='CrowncapBruteUV')
        for p in o.data.polygons:
            for li in p.loop_indices:
                co=o.data.vertices[o.data.loops[li].vertex_index].co; layer.data[li].uv=((co.x*.19+.5)%1,(co.z*.19+.5)%1)
def ico(name,loc,scale,material,sub=3,bone=None):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o)
    if bone: parts[name]=bone
    return o
def seg(name,a,b,r,material,bone=None,verts=12):
    a,b=Vector(a),Vector(b); d=b-a; mid=(a+b)/2
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=mid); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y'); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o)
    if bone: parts[name]=bone
    return o

# Massive top-heavy fungal biped: broad pelvis/chest, short neck, deliberately oversized arms.
ico('Pelvis',(0,0,1.18),(.72,.48,.62),HIDE,4,'Pelvis'); ico('Torso',(0,0,1.83),(.82,.52,.82),HIDE,4,'Chest'); ico('UpperTorso',(0,.01,2.30),(.92,.55,.63),FLESH,4,'Chest'); ico('Head',(0,.10,2.67),(.48,.43,.38),FLESH,3,'Head')
# layered crown is silhouette geometry, not a painted cap
for i,(z,sx,sy) in enumerate([(2.77,1.12,.78),(2.91,.98,.70),(3.03,.79,.58),(3.12,.57,.43)]):
    ico(f'CrownPlate_{i+1}',(0,.02,z),(sx,sy,.14),PLATE,3,'Head')
# shoulder armor
for side,x in [('L',-.86),('R',.86)]:
    ico(f'ShoulderPlate_{side}',(x,0,2.31),(.55,.54,.30),PLATE,3,f'UpperArm_{side}')
    seg(f'UpperArmMesh_{side}',(x,0,2.20),(x*1.16,.02,1.55),.31,HIDE,f'UpperArm_{side}',16)
    seg(f'ForeArmMesh_{side}',(x*1.16,.02,1.55),(x*1.22,.18,.78),.37,HIDE,f'ForeArm_{side}',16)
    ico(f'Fist_{side}',(x*1.22,.22,.67),(.46,.42,.39),HIDE,3,f'Hand_{side}')
# load-bearing legs and broad feet
for side,x in [('L',-.43),('R',.43)]:
    seg(f'ThighMesh_{side}',(x,0,1.15),(x*.98,.03,.62),.34,HIDE,f'Thigh_{side}',16)
    seg(f'ShinMesh_{side}',(x*.98,.03,.62),(x*1.02,.12,.18),.30,HIDE,f'Shin_{side}',16)
    ico(f'Foot_{side}',(x*1.02,.30,.14),(.42,.62,.18),HIDE,3,f'Foot_{side}')
# exposed gill curtains beneath rear/lateral crown: readable vulnerability and effect source
for i,(x,y) in enumerate([(-.55,.30),(-.28,.40),(0,.43),(.28,.40),(.55,.30)]):
    seg(f'GillCurtain_{i+1}',(x,y,2.73),(x*1.08,y+.04,2.37),.075,GILL,'Head',10)
# chest mycelial cords provide secondary form
for i,x in enumerate([-.48,-.24,0,.24,.48]): seg(f'ChestCord_{i+1}',(x,.49,2.35),(x*.78,.53,1.45),.045,FLESH,'Chest',8)

# HOST-BIPED-MASS armature
armdata=bpy.data.armatures.new('HOST-BIPED-MASS'); arm=bpy.data.objects.new('CrowncapBruteRig',armdata); bpy.context.collection.objects.link(arm); bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,0,0),(0,0,.3)); pelvis=bone('Pelvis',(0,0,.72),(0,0,1.30),root); spine1=bone('Spine1',(0,0,1.30),(0,0,1.72),pelvis); chest=bone('Chest',(0,0,1.72),(0,0,2.35),spine1); neck=bone('Neck',(0,0,2.35),(0,.04,2.58),chest); head=bone('Head',(0,.04,2.58),(0,.08,2.94),neck); jaw=bone('Jaw',(0,.20,2.60),(0,.48,2.52),head)
for side,x in [('L',-.74),('R',.74)]:
    ua=bone(f'UpperArm_{side}',(x,0,2.28),(x*1.18,0,1.57),chest); fa=bone(f'ForeArm_{side}',(x*1.18,0,1.57),(x*1.24,.15,.80),ua); bone(f'Hand_{side}',(x*1.24,.15,.80),(x*1.24,.32,.54),fa)
    th=bone(f'Thigh_{side}',(x*.58,0,1.18),(x*.98,.03,.63),pelvis); sh=bone(f'Shin_{side}',(x*.98,.03,.63),(x*1.02,.12,.20),th); bone(f'Foot_{side}',(x*1.02,.12,.20),(x*1.02,.62,.12),sh)
bone('AttackOrigin',(0,.45,1.72),(0,1.05,1.68),chest); bone('HitCenter',(0,0,1.25),(0,0,1.82),root); bone('GillFX',(0,.38,2.62),(0,.62,2.50),head)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('CrowncapBruteArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']; tris=sum(len(o.data.loop_triangles) if (o.data.calc_loop_triangles() or True) else 0 for o in meshes)
if len(meshes)<30: raise RuntimeError(f'Crowncap Brute detail regression: {len(meshes)} mesh parts')
# Blender calc loop triangles populated above; enforce elite fidelity band floor.
tris=sum(len(o.data.loop_triangles) for o in meshes)
if tris<14000: raise RuntimeError(f'Crowncap Brute triangle floor regression: {tris}')
for o in meshes:
    if not o.data.uv_layers.get('CrowncapBruteUV'): raise RuntimeError(f'Missing UVs: {o.name}')
arm['magenheim_asset']='crowncap-brute'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-BIPED-MASS'; arm['height_m']=HEIGHT; arm['role']='elite-slow-armored-ground-control'; arm['crown_layers']=4; arm['exposed_gill_curtains']=5; arm['animation_contract']='idle,walk,turn,alert,sweep-left,sweep-right,heavy-slam,hit,heavy-stagger,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')
