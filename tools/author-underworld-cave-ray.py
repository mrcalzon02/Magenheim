#!/usr/bin/env python3
"""Author the Blackwater Deep Cave Ray production source model in Blender."""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-cave-ray.blend'
SPAN=3.35

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough=.7,emit=None):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    if emit:
        bs.inputs['Emission Color'].default_value=(*emit,1); bs.inputs['Emission Strength'].default_value=.28
    return m
SKIN=mat('CaveRay_DorsalSkin',(.055,.075,.08),.74); BELLY=mat('CaveRay_VentralSkin',(.12,.16,.16),.61,emit=(.12,.24,.22)); FIN=mat('CaveRay_FinMembrane',(.075,.105,.11),.67); EYE=mat('CaveRay_SensoryTissue',(.16,.24,.22),.58,emit=(.10,.34,.29))
parts={}
def uv(o):
    layer=o.data.uv_layers.new(name='CaveRayUV')
    xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]
    xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys); dx=max(xmax-xmin,.001); dy=max(ymax-ymin,.001)
    for p in o.data.polygons:
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co; layer.data[li].uv=((co.x-xmin)/dx,(co.y-ymin)/dy)
def mesh(name,verts,faces,material,bone):
    me=bpy.data.meshes.new(name+'Mesh'); me.from_pydata(verts,[],faces); me.update(); o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o); me.materials.append(material); uv(o); parts[name]=bone; return o

def ico(name,loc,scale,material,bone,sub=3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o); parts[name]=bone; return o

def seg(name,a,b,r,material,bone,verts=12):
    a,b=Vector(a),Vector(b); d=b-a; bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(a+b)/2); o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y'); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o); parts[name]=bone; return o

# Flattened muscular central disc and cephalic lobe.
ico('Body',(0,0,0),(.72,1.05,.18),SKIN,'Body',4); ico('VentralBody',(0,.03,-.10),(.64,.92,.09),BELLY,'Body',3); ico('Head',(0,.78,.01),(.48,.48,.15),SKIN,'Head',3)
# Broad wings are segmented membrane geometry so deformation can travel root -> mid -> tip.
for side,s in [('L',-1),('R',1)]:
    xs=[0.38*s, .86*s, 1.30*s, 1.675*s]; ys=[.64,.42,.05,-.30]
    for i in range(3):
        x0,x1=xs[i],xs[i+1]; y0,y1=ys[i],ys[i+1]; bone=f'Wing_{side}_{i+1}'
        verts=[(x0,y0,.04),(x1,y1,.025),(x1,y1-.62,-.015),(x0,y0-.58,-.025),(x0,y0,.04),(x1,y1,.025)]
        faces=[(0,1,2,3),(4,5,1,0)]; mesh(f'WingMembrane_{side}_{i+1}',verts,faces,FIN,bone)
    # cephalic fin gives ray silhouette forward of wing root
    mesh(f'CephalicFin_{side}',[(.22*s,.82,.02),(.64*s,1.03,.01),(.52*s,.55,-.01),(.18*s,.48,-.02)],[(0,1,2,3)],FIN,'Head')
# Long tapering tail in five independently deformable pieces.
points=[(0,-.82,0),(0,-1.30,-.01),(0,-1.78,-.02),(0,-2.23,-.015),(0,-2.65,0),(0,-3.02,.015)]
for i in range(5): seg(f'TailSegment_{i+1}',points[i],points[i+1],.085-i*.011,SKIN,f'Tail_{i+1}',10)
# Restrained underside photophores: modeled sensory organs, not a glowing whole body.
for i,(x,y) in enumerate([(-.31,.45),(.31,.45),(-.42,.05),(.42,.05),(-.28,-.38),(.28,-.38)]): ico(f'Photophore_{i+1}',(x,y,-.185),(.055,.08,.025),EYE,'Body',2)

armdata=bpy.data.armatures.new('HOST-AQUATIC-RAY'); arm=bpy.data.objects.new('CaveRayRig',armdata); bpy.context.collection.objects.link(arm); bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,0,-.05),(0,0,.15)); body=bone('Body',(0,-.45,0),(0,.48,0),root); head=bone('Head',(0,.48,0),(0,1.08,0),body)
for side,s in [('L',-1),('R',1)]:
    p=body
    for i,(a,b) in enumerate([((.25*s,.35,0),(.72*s,.30,0)),((.72*s,.30,0),(1.20*s,.05,0)),((1.20*s,.05,0),(1.66*s,-.28,0))],1): p=bone(f'Wing_{side}_{i}',a,b,p)
p=body
for i in range(5): p=bone(f'Tail_{i+1}',points[i],points[i+1],p)
bone('AttackOrigin',(0,.76,0),(0,1.18,0),head); bone('HitCenter',(0,-.15,0),(0,.35,0),root); bone('VentralFX',(0,.10,-.12),(0,.10,-.35),body)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('CaveRayArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if SPAN<2.5 or SPAN>4.0: raise RuntimeError('Cave Ray span outside production band')
if len(meshes)<20: raise RuntimeError(f'Cave Ray anatomy regression: {len(meshes)} mesh parts')
if tris<7000: raise RuntimeError(f'Cave Ray source triangle floor regression: {tris}')
for o in meshes:
    if not o.data.uv_layers.get('CaveRayUV'): raise RuntimeError(f'Missing UVs: {o.name}')
arm['magenheim_asset']='cave-ray'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-AQUATIC-RAY'; arm['span_m']=SPAN; arm['role']='blackwater-ambient-swimmer'; arm['photophores']=6; arm['wing_deformation_chains']=6; arm['animation_contract']='glide,flap-impulse,bank-left,bank-right,dive,rise,flee,hit,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')