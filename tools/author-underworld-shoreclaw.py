#!/usr/bin/env python3
"""Author the Blackwater Deep Shoreclaw production source model in Blender.

Art-only source generation: Valheim remains authoritative for locomotion, swimming,
AI, networking and persistence.
"""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-shoreclaw.blend'
LENGTH=1.48
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    return m
SHELL=mat('Shoreclaw_ShellArmor',(.10,.14,.13),.82); RIDGE=mat('Shoreclaw_RidgeScute',(.16,.19,.16),.88)
JOINT=mat('Shoreclaw_JointTissue',(.12,.09,.075),.57); BELLY=mat('Shoreclaw_Underside',(.20,.17,.13),.66)
CLAW=mat('Shoreclaw_ClawArmor',(.13,.17,.14),.76); EYE=mat('Shoreclaw_Eye',(.025,.03,.025),.34)
parts={}
def uv(o):
    layer=o.data.uv_layers.new(name='ShoreclawUV'); xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]
    xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys); dx=max(xmax-xmin,.001); dy=max(ymax-ymin,.001)
    for p in o.data.polygons:
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co; layer.data[li].uv=((co.x-xmin)/dx,(co.y-ymin)/dy)
def ico(name,loc,scale,material,bone,sub=3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o); parts[name]=bone; return o
def seg(name,a,b,r,material,bone,verts=12):
    a,b=Vector(a),Vector(b); d=b-a; bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(a+b)/2); o=bpy.context.object; o.name=name
    o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y'); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material); uv(o); parts[name]=bone; return o

# Low armored abdomen with four overlapping, independently rigged shell plates.
ico('Abdomen',(0,0,.24),(.62,.74,.25),JOINT,'Body',4); ico('Underside',(0,-.02,.10),(.54,.65,.13),BELLY,'Body',3)
for i,(y,s) in enumerate([(.43,(.55,.28,.13)),(.15,(.61,.31,.14)),(-.16,(.60,.31,.14)),(-.43,(.52,.27,.13))],1): ico(f'ShellPlate_{i}',(0,y,.39),s,SHELL,f'ShellPlate_{i}',3)
# Raised scutes are geometry, preserving armored silhouette at gameplay distance.
for i in range(16):
    row=i//4; col=i%4; x=(-.42+.28*col); y=.45-.29*row
    ico(f'Scute_{i+1}',(x,y,.52),(.075,.10,.055),RIDGE,f'ShellPlate_{row+1}',2)
# Eight three-stage legs, each ending in a broad amphibious paddle.
for side,s in [('L',-1),('R',1)]:
    for n,y in enumerate([.42,.16,-.13,-.40],1):
        prefix=f'Leg_{side}_{n}'; hip=(.45*s,y,.25); knee=(.78*s,y-.03,.10); ankle=(.96*s,y-.08,-.03); foot=(1.11*s,y-.14,-.04)
        seg(prefix+'_Upper',hip,knee,.075,JOINT,prefix+'_Upper',12); seg(prefix+'_Lower',knee,ankle,.060,JOINT,prefix+'_Lower',12)
        ico(prefix+'_Paddle',foot,(.18,.11,.035),SHELL,prefix+'_Foot',2)
# Asymmetric weapons: massive left crusher, narrow fast right cutter.
for side,s,scale in [('L',-1,1.0),('R',1,.68)]:
    base=(.38*s,.58,.30); elbow=(.72*s,.72,.31); tip=(1.02*s,.82,.32); pre=f'Claw_{side}'
    seg(pre+'_Arm',base,elbow,.105 if side=='L' else .075,JOINT,pre+'_Arm',14)
    ico(pre+'_Palm',tip,(.27*scale,.23*scale,.15*scale),CLAW,pre+'_Palm',3)
    for j,dy in enumerate([-.09,.09],1): seg(pre+f'_Finger_{j}',(tip[0],tip[1]+dy,tip[2]),(tip[0]+.28*s*scale,tip[1]+dy*.7,tip[2]),.055*scale,CLAW,pre+'_Palm',10)
# Stalked eyes and tactile feelers.
for side,s in [('L',-1),('R',1)]:
    seg(f'EyeStalk_{side}',(.18*s,.57,.42),(.24*s,.73,.51),.035,JOINT,'Body',10); ico(f'Eye_{side}',(.24*s,.74,.52),(.065,.065,.06),EYE,'Body',2)
    seg(f'Feeler_{side}',(.12*s,.67,.34),(.35*s,1.02,.28),.018,JOINT,'Body',8)

armdata=bpy.data.armatures.new('HOST-AMPHIB-ARMORED'); arm=bpy.data.objects.new('ShoreclawRig',armdata); bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,0,.05),(0,0,.25)); body=bone('Body',(0,-.38,.24),(0,.45,.24),root)
for i,y in enumerate([.43,.15,-.16,-.43],1): bone(f'ShellPlate_{i}',(0,y-.12,.38),(0,y+.12,.40),body)
for side,s in [('L',-1),('R',1)]:
    for n,y in enumerate([.42,.16,-.13,-.40],1):
        pre=f'Leg_{side}_{n}'; u=bone(pre+'_Upper',(.42*s,y,.25),(.77*s,y-.03,.10),body); l=bone(pre+'_Lower',(.77*s,y-.03,.10),(.96*s,y-.08,-.03),u); bone(pre+'_Foot',(.96*s,y-.08,-.03),(1.15*s,y-.14,-.04),l)
    pre=f'Claw_{side}'; a=bone(pre+'_Arm',(.30*s,.52,.30),(.72*s,.72,.31),body); bone(pre+'_Palm',(.72*s,.72,.31),(1.12*s,.83,.32),a)
bone('AttackOrigin',(0,.67,.28),(0,1.05,.28),body); bone('HitCenter',(0,-.05,.20),(0,.28,.20),root); bone('SwimCenter',(0,-.08,.08),(0,.25,.08),root); bone('ShellFX',(0,0,.46),(0,0,.70),body)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('ShoreclawArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if not 1.2<=LENGTH<=1.7: raise RuntimeError('Shoreclaw length outside production band')
if len(meshes)<55: raise RuntimeError(f'Shoreclaw anatomy regression: {len(meshes)} mesh parts')
if tris<11000: raise RuntimeError(f'Shoreclaw source triangle floor regression: {tris}')
if sum(1 for o in meshes if 'Paddle' in o.name)!=8: raise RuntimeError('Shoreclaw requires exactly eight paddles')
if sum(1 for o in meshes if o.name.startswith('ShellPlate_'))!=4: raise RuntimeError('Shoreclaw requires four shell plates')
for o in meshes:
    if not o.data.uv_layers.get('ShoreclawUV'): raise RuntimeError(f'Missing UVs: {o.name}')
arm['magenheim_asset']='shoreclaw'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-AMPHIB-ARMORED'; arm['length_m']=LENGTH
arm['role']='blackwater-coastline-controller'; arm['leg_chains']=8; arm['shell_plates']=4; arm['claw_asymmetry']='left-crusher/right-cutter'
arm['animation_contract']='idle,scuttle,turn-left,turn-right,crusher-attack,cutter-attack,guard,swim-idle,swim-forward,water-exit,water-entry,hit,stagger,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')
