#!/usr/bin/env python3
"""Author all seven Frozen Caverns creature production bodies.

This is intentionally one biome production script rather than fourteen tiny author/animate tools.
Each creature is saved as its own normal source blend with a rigid-segment-weighted armature and
scene-level authored-action contract consumed by export-model-assets.py.
"""
from pathlib import Path
from math import pi, radians, sin, cos
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source'
OUT.mkdir(parents=True,exist_ok=True)

ICE=(.62,.80,.92)
DEEP_ICE=(.20,.34,.44)
PALE=(.72,.78,.80)
FLESH=(.38,.43,.44)
DARK=(.08,.12,.15)
FROST=(.82,.91,.98)
STONE=(.30,.38,.42)

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.actions,bpy.data.meshes,bpy.data.materials,bpy.data.curves,bpy.data.armatures):
        for block in list(collection):
            try: collection.remove(block)
            except RuntimeError: pass

def mat(name,color,rough=.72,metal=.0):
    m=bpy.data.materials.new(name)
    m.diffuse_color=(*color,1)
    m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=rough
    bs.inputs['Metallic'].default_value=metal
    return m

def organic(name,loc,scale,material,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    return o

def seg(name,a,b,radius,material,verts=12):
    d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=d.length,location=(Vector(a)+Vector(b))/2)
    o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'
    o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
    o.rotation_mode='XYZ'; o.data.materials.append(material)
    return o

def cone(name,loc,radius,depth,material,rot=(0,0,0),verts=12):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=radius,radius2=.002,depth=depth,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.data.materials.append(material); return o

def box(name,loc,scale,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material); return o

def uv(o,name='FrozenUV'):
    layer=o.data.uv_layers.new(name=name)
    for poly in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(poly.normal[i]))
        for li in poly.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co
            q=((co.y,co.z),(co.x,co.z),(co.x,co.y))[axis]
            layer.data[li].uv=((q[0]*1.37)%1,(q[1]*1.37)%1)

def armature(name):
    bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
    arm=bpy.context.object; arm.name=name
    root=arm.data.edit_bones[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.18)
    return arm,root

def bone(arm,name,head,tail,parent=None):
    b=arm.data.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b

def finalize_rig(arm,parts,model_id,actions,readability,min_meshes=12):
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.show_in_front=True
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if len(meshes)<min_meshes:
        raise RuntimeError(f'{model_id}: anatomical breakup too low: {len(meshes)} < {min_meshes}')
    for o in meshes:
        if not o.data.uv_layers:
            uv(o)
        target=parts.get(o.name)
        if not target or target not in arm.data.bones:
            raise RuntimeError(f'{model_id}: missing rigid bone binding for {o.name}: {target}')
        mod=o.modifiers.new('MagenheimRigidArmature','ARMATURE'); mod.object=arm
        vg=o.vertex_groups.new(name=target); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')

    arm.animation_data_create(); arm.animation_data.action=None
    sc=bpy.context.scene
    sc.render.fps=24
    sc['magenheim_model_id']=model_id
    sc['magenheim_skinning']='rigid-segment-weighted'
    sc['magenheim_authored_actions']=','.join(actions)
    arm['authored_actions']=sc['magenheim_authored_actions']
    arm['production_contract']='production-creature-r2'
    arm['readability_contract']=readability

def reset_pose(arm):
    for p in arm.pose.bones:
        p.rotation_mode='XYZ'; p.rotation_euler=(0,0,0); p.location=(0,0,0); p.scale=(1,1,1)

def begin_action(arm,name):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    a=bpy.data.actions.new(name); a.use_fake_user=True
    arm.animation_data_create(); arm.animation_data.action=a
    reset_pose(arm)
    return a

def key(arm,bone_name,frame,rot=None,loc=None,scale=None):
    p=arm.pose.bones.get(bone_name)
    if p is None: raise RuntimeError(f'{arm.name}: missing animation bone {bone_name}')
    if rot is not None:
        p.rotation_euler=tuple(radians(v) for v in rot)
        p.keyframe_insert('rotation_euler',frame=frame,group=bone_name)
    if loc is not None:
        p.location=loc; p.keyframe_insert('location',frame=frame,group=bone_name)
    if scale is not None:
        p.scale=scale; p.keyframe_insert('scale',frame=frame,group=bone_name)

def save(model_id):
    blend=OUT/(model_id+'.blend')
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
    print(f'AUTHORED {model_id}: {blend}',flush=True)

def build_rime_moth():
    clear_scene(); parts={}
    shell=mat('RimeMoth_FrostShell',(.48,.68,.78),.46)
    membrane=mat('RimeMoth_CrystalWing',(.72,.90,.97),.22,.08)
    dark=mat('RimeMoth_Joint',DARK,.62)
    eye=mat('RimeMoth_BlindEye',(.10,.18,.22),.35)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('RM_Thorax',(0,0,.34),(.22,.28,.18),shell,3),'Thorax')
    keep(organic('RM_Abdomen',(0,-.34,.32),(.17,.34,.14),shell,3),'Abdomen')
    keep(organic('RM_Head',(0,.25,.34),(.16,.15,.13),shell,2),'Head')
    for side in (-1,1):
        s='L' if side<0 else 'R'
        keep(cone(f'RM_Antenna_{s}',(side*.07,.43,.39),.018,.28,dark,(pi/2,0,side*.20)),f'Antenna_{s}')
        keep(organic(f'RM_Eye_{s}',(side*.09,.36,.37),(.025,.018,.022),eye,2),'Head')
        for pair,y in ((1,.06),(2,-.16)):
            bone_name=f'Wing_{s}{pair}'
            x=side*(.48 if pair==1 else .43)
            z=.40 if pair==1 else .31
            keep(box(f'RM_{bone_name}',(x,y,z),(.42,.18,.018),membrane,(0,side*.18,side*(.20 if pair==1 else -.12))),bone_name)
            keep(cone(f'RM_{bone_name}_Tip',(side*.88,y-.03,z),.028,.32,membrane,(0,pi/2,0)),bone_name)
    arm,root=armature('RIG_RimeMoth_HOST_AERIAL')
    thor=bone(arm,'Thorax',(0,-.08,.28),(0,.12,.38),root)
    abd=bone(arm,'Abdomen',(0,-.08,.32),(0,-.56,.30),thor)
    head=bone(arm,'Head',(0,.10,.34),(0,.38,.34),thor)
    for side in (-1,1):
        s='L' if side<0 else 'R'
        bone(arm,f'Antenna_{s}',(side*.04,.34,.38),(side*.08,.58,.41),head)
        for pair,y in ((1,.07),(2,-.12)):
            bone(arm,f'Wing_{s}{pair}',(side*.08,y,.35),(side*.88,y-.04,.36),thor)
    bone(arm,'AttackOrigin',(0,.32,.33),(0,.54,.33),head)
    actions=['RimeMoth_Hover','RimeMoth_Flight','RimeMoth_BankLeft','RimeMoth_BankRight','RimeMoth_Dive','RimeMoth_Hit','RimeMoth_Death']
    finalize_rig(arm,parts,'underworld-creature-rime-moth',actions,'four crystalline wings; compact frost moth; no bat silhouette',12)
    begin_action(arm,'RimeMoth_Hover')
    for f,v in ((1,0),(7,24),(13,0),(19,-24),(25,0)):
        for side in ('L','R'):
            sign=1 if side=='L' else -1
            key(arm,f'Wing_{side}1',f,(v*sign,0,8*sign))
            key(arm,f'Wing_{side}2',f,(-v*.72*sign,0,-6*sign))
        key(arm,'Thorax',f,(2*sin(f),0,0))
    begin_action(arm,'RimeMoth_Flight')
    for f in range(1,25,4):
        phase=2*pi*(f-1)/24
        flap=42*sin(phase)
        for side in ('L','R'):
            sign=1 if side=='L' else -1
            key(arm,f'Wing_{side}1',f,(flap*sign,0,12*sign))
            key(arm,f'Wing_{side}2',f,(-flap*.75*sign,0,-9*sign))
        key(arm,'Abdomen',f,(5*sin(phase*2),0,0))
    for name,sgn in (('RimeMoth_BankLeft',1),('RimeMoth_BankRight',-1)):
        begin_action(arm,name)
        for f,w in ((1,0),(7,1),(14,1),(20,0)):
            key(arm,'Thorax',f,(0,0,sgn*22*w)); key(arm,'Head',f,(0,0,sgn*12*w))
    begin_action(arm,'RimeMoth_Dive')
    for f,p in ((1,0),(6,18),(12,34),(18,12),(24,0)):
        key(arm,'Thorax',f,(p,0,0)); key(arm,'Head',f,(-p*.35,0,0))
    begin_action(arm,'RimeMoth_Hit')
    for f,r in ((1,0),(4,24),(9,-8),(14,0)): key(arm,'Thorax',f,(0,0,r))
    begin_action(arm,'RimeMoth_Death')
    for f,r,d in ((1,0,0),(10,26,-.03),(24,78,-.20),(40,92,-.38)):
        key(arm,'Thorax',f,(0,0,r),loc=(0,0,d)); key(arm,'Abdomen',f,(20 if r else 0,0,r*.3))
    arm.animation_data.action=None; save('underworld-creature-rime-moth')

def build_frost_tick():
    clear_scene(); parts={}
    shell=mat('FrostTick_IceCarapace',(.46,.66,.75),.50,.06)
    joint=mat('FrostTick_Joint',(.18,.25,.28),.65)
    crystal=mat('FrostTick_CrystalEdge',(.76,.91,.97),.24,.08)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('FT_Body',(0,0,.16),(.25,.31,.14),shell,3),'Body')
    keep(organic('FT_Head',(0,.29,.13),(.16,.15,.11),joint,2),'Head')
    for i,y in enumerate((.19,.06,-.08,-.21),1):
        for side in (-1,1):
            s='L' if side<0 else 'R'; hip=(side*.16,y,.14); knee=(side*.31,y+.03,.09); foot=(side*.43,y+.08,.025)
            keep(seg(f'FT_{s}{i}_Upper',hip,knee,.035,joint),f'{s}{i}_Upper')
            keep(seg(f'FT_{s}{i}_Lower',knee,foot,.026,shell),f'{s}{i}_Lower')
            keep(cone(f'FT_{s}{i}_Claw',foot,.018,.08,crystal,(pi/2,0,0)),f'{s}{i}_Lower')
    for side in (-1,1): keep(cone(f'FT_Fang_{"L" if side<0 else "R"}',(side*.05,.43,.11),.018,.12,crystal,(pi/2,0,0)),'Head')
    arm,root=armature('RIG_FrostTick_HOST_ARACHNID')
    body=bone(arm,'Body',(0,-.20,.13),(0,.20,.16),root)
    head=bone(arm,'Head',(0,.18,.13),(0,.42,.12),body)
    for i,y in enumerate((.19,.06,-.08,-.21),1):
        for side in (-1,1):
            s='L' if side<0 else 'R'; hip=(side*.13,y,.13); knee=(side*.31,y+.03,.08); foot=(side*.43,y+.08,.025)
            u=bone(arm,f'{s}{i}_Upper',hip,knee,body); bone(arm,f'{s}{i}_Lower',knee,foot,u)
    bone(arm,'AttackOrigin',(0,.35,.11),(0,.50,.11),head)
    actions=['FrostTick_Idle','FrostTick_Scuttle','FrostTick_Latch','FrostTick_Hit','FrostTick_Death']
    finalize_rig(arm,parts,'underworld-creature-frost-tick',actions,'low eight-legged ice tick with crystalline feeding claws',18)
    begin_action(arm,'FrostTick_Idle')
    for f,v in ((1,0),(13,2),(25,0),(37,-2),(48,0)): key(arm,'Body',f,(v,0,0))
    begin_action(arm,'FrostTick_Scuttle')
    for f in range(1,25,3):
        ph=2*pi*(f-1)/24
        for i in range(1,5):
            for s in ('L','R'):
                sign=sin(ph+(pi if ((s=='R')^(i%2==0)) else 0))
                key(arm,f'{s}{i}_Upper',f,(30*sign,0,0)); key(arm,f'{s}{i}_Lower',f,(-24*sign,0,0))
    begin_action(arm,'FrostTick_Latch')
    for f,p in ((1,0),(5,28),(9,-12),(14,18),(20,0)): key(arm,'Head',f,(p,0,0)); key(arm,'Body',f,(-p*.25,0,0))
    begin_action(arm,'FrostTick_Hit')
    for f,r in ((1,0),(4,16),(9,-6),(14,0)): key(arm,'Body',f,(0,0,r))
    begin_action(arm,'FrostTick_Death')
    for f,r,d in ((1,0,0),(10,25,-.02),(22,78,-.10),(36,92,-.16)): key(arm,'Body',f,(0,0,r),loc=(0,0,d))
    arm.animation_data.action=None; save('underworld-creature-frost-tick')

def build_quadruped(model_id,prefix,rig_name,armored=False,sensory=False):
    clear_scene(); parts={}
    hide=mat(prefix+'_Hide',(.35,.43,.46),.82)
    ice=mat(prefix+'_IceArmor',(.56,.76,.86),.42,.08)
    joint=mat(prefix+'_Joint',(.17,.23,.25),.66)
    crystal=mat(prefix+'_SensoryIce',(.75,.92,.98),.20,.10)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic(prefix+'_Pelvis',(0,-.58,.62),(.30,.34,.29),hide,3),'Pelvis')
    keep(organic(prefix+'_Chest',(0,.02,.77),(.36,.46,.35),hide,3),'Chest')
    keep(organic(prefix+'_Neck',(0,.40,.82),(.24,.27,.25),hide,2),'Neck')
    keep(organic(prefix+'_Head',(0,.65,.83),(.25,.30,.23),hide,3),'Head')
    keep(organic(prefix+'_Jaw',(0,.83,.75),(.20,.22,.09),joint,2),'Jaw')
    tail=[(0,-.77,.62),(0,-1.03,.58),(0,-1.28,.51)]
    for i in range(2): keep(seg(f'{prefix}_Tail_{i+1}',tail[i],tail[i+1],.075-i*.018,hide),f'Tail_{i+1}')
    legs={}
    for side in (-1,1):
        for fore,y in ((True,.18),(False,-.52)):
            s='L' if side<0 else 'R'; tag=('F' if fore else 'H')+s
            hip=(side*.27,y,.66); knee=(side*.31,y+.15,.38); hock=(side*.27,y-.03,.18); foot=(side*.25,y+.19,.055)
            legs[tag]=(hip,knee,hock,foot)
            keep(seg(f'{prefix}_{tag}_Upper',hip,knee,.060,hide),f'{tag}_Upper')
            keep(seg(f'{prefix}_{tag}_Lower',knee,hock,.050,hide),f'{tag}_Lower')
            keep(seg(f'{prefix}_{tag}_Hock',hock,foot,.040,joint),f'{tag}_Hock')
            keep(organic(f'{prefix}_{tag}_Foot',foot,(.08,.14,.045),ice,2),f'{tag}_Foot')
    if armored:
        for i,(y,z,w) in enumerate(((-.52,.87,.24),(-.25,.95,.30),(.02,1.02,.32),(.25,1.03,.28)),1):
            keep(organic(f'{prefix}_Armor_{i}',(0,y,z),(w,.16,.055),ice,2),'Pelvis' if i<2 else 'Chest')
    if sensory:
        for side in (-1,1):
            s='L' if side<0 else 'R'
            for i,angle in enumerate((-.35,0,.35),1):
                keep(cone(f'{prefix}_Sensor_{s}_{i}',(side*(.13+i*.025),.91,.91+i*.03),.022,.24,crystal,(angle,0,side*.22)), 'Head')
        keep(box(prefix+'_BlindMask',(0,.83,.88),(.22,.055,.07),ice),'Head')
    arm,root=armature(rig_name)
    pel=bone(arm,'Pelvis',(0,-.70,.55),(0,-.40,.69),root); chest=bone(arm,'Chest',(0,-.38,.69),(0,.18,.79),pel)
    neck=bone(arm,'Neck',(0,.18,.79),(0,.48,.84),chest); head=bone(arm,'Head',(0,.48,.84),(0,.80,.82),neck); jaw=bone(arm,'Jaw',(0,.71,.77),(0,.95,.73),head)
    for tag,(hip,knee,hock,foot) in legs.items():
        parent=chest if tag.startswith('F') else pel
        u=bone(arm,f'{tag}_Upper',hip,knee,parent); l=bone(arm,f'{tag}_Lower',knee,hock,u); h=bone(arm,f'{tag}_Hock',hock,foot,l); bone(arm,f'{tag}_Foot',foot,(foot[0],foot[1]+.18,foot[2]),h)
    t1=bone(arm,'Tail_1',tail[0],tail[1],pel); bone(arm,'Tail_2',tail[1],tail[2],t1)
    bone(arm,'AttackOrigin',(0,.82,.77),(0,1.08,.77),head)
    base=model_id.split('underworld-creature-')[1].replace('-',' ').title().replace(' ','')
    actions=[base+'_Idle',base+'_Walk',base+'_Sprint',base+'_Bite',base+'_Track',base+'_Hit',base+'_Death']
    finalize_rig(arm,parts,model_id,actions,'ice-adapted low quadruped; explicit non-wolf head/armor language',18)
    begin_action(arm,actions[0])
    for f,v in ((1,0),(13,2),(25,0),(37,-2),(48,0)): key(arm,'Chest',f,(v,0,0)); key(arm,'Tail_2',f,(0,0,v*2))
    for action_name,end,amp in ((actions[1],32,19),(actions[2],20,36)):
        begin_action(arm,action_name)
        for f in range(1,end+1,4):
            ph=2*pi*(f-1)/max(1,end-1)
            for tag in ('FL','FR','HL','HR'):
                s=sin(ph+(pi if tag in ('FR','HL') else 0))
                key(arm,tag+'_Upper',f,(amp*s,0,0)); key(arm,tag+'_Lower',f,(-amp*.68*s,0,0)); key(arm,tag+'_Hock',f,(amp*.84*s,0,0))
            key(arm,'Tail_1',f,(0,0,10*sin(ph))); key(arm,'Tail_2',f,(0,0,18*sin(ph-.4)))
    begin_action(arm,actions[3])
    for f,j,n in ((1,0,0),(5,32,-10),(9,-8,18),(14,6,7),(18,0,0)): key(arm,'Jaw',f,(j,0,0)); key(arm,'Neck',f,(n,0,0))
    begin_action(arm,actions[4])
    for f,n in ((1,0),(12,-14),(24,-22),(36,-10),(48,0)): key(arm,'Neck',f,(n,0,0)); key(arm,'Head',f,(n*.5,0,0))
    begin_action(arm,actions[5])
    for f,r in ((1,0),(5,18),(10,-7),(16,0)): key(arm,'Chest',f,(0,0,r))
    begin_action(arm,actions[6])
    for f,r,d in ((1,0,0),(14,18,-.04),(28,56,-.18),(44,88,-.30)): key(arm,'Pelvis',f,(0,0,r),loc=(0,0,d)); key(arm,'Chest',f,(12 if r else 0,0,r*.7))
    arm.animation_data.action=None; save(model_id)

def build_pale_burrower():
    clear_scene(); parts={}
    shell=mat('PaleBurrower_Carapace',(.67,.72,.73),.76)
    joint=mat('PaleBurrower_SoftJoint',(.23,.28,.28),.58)
    blade=mat('PaleBurrower_DigBlade',(.76,.86,.89),.40,.06)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('PB_Thorax',(0,0,.34),(.30,.39,.24),shell,3),'Thorax')
    keep(organic('PB_Abdomen',(0,-.43,.30),(.27,.38,.22),shell,3),'Abdomen')
    keep(organic('PB_Head',(0,.37,.31),(.23,.24,.19),shell,2),'Head')
    legs={}
    for pair,y in enumerate((.22,-.05,-.34),1):
        for side in (-1,1):
            s='L' if side<0 else 'R'; tag=f'{s}{pair}'
            hip=(side*.24,y,.31); knee=(side*.46,y+.04,.20); foot=(side*.61,y+.14,.055)
            legs[tag]=(hip,knee,foot)
            keep(seg(f'PB_{tag}_Upper',hip,knee,.055,joint),f'{tag}_Upper')
            keep(seg(f'PB_{tag}_Lower',knee,foot,.046,shell),f'{tag}_Lower')
            if pair==1:
                keep(box(f'PB_{tag}_Shovel',(side*.66,y+.22,.045),(.10,.18,.025),blade,(0,0,side*.10)),f'{tag}_Lower')
    for side in (-1,1): keep(cone(f'PB_Mandible_{"L" if side<0 else "R"}',(side*.08,.56,.27),.035,.22,blade,(pi/2,0,side*.12)),'Head')
    arm,root=armature('RIG_PaleBurrower_HOST_HEXAPOD')
    thor=bone(arm,'Thorax',(0,-.12,.29),(0,.25,.36),root); abd=bone(arm,'Abdomen',(0,-.12,.30),(0,-.65,.29),thor); head=bone(arm,'Head',(0,.21,.32),(0,.55,.30),thor)
    for tag,(hip,knee,foot) in legs.items():
        u=bone(arm,f'{tag}_Upper',hip,knee,thor if tag.endswith(('1','2')) else abd); bone(arm,f'{tag}_Lower',knee,foot,u)
    bone(arm,'AttackOrigin',(0,.48,.29),(0,.71,.29),head)
    actions=['PaleBurrower_Idle','PaleBurrower_Scuttle','PaleBurrower_Dig','PaleBurrower_Emerge','PaleBurrower_Bite','PaleBurrower_Hit','PaleBurrower_Death']
    finalize_rig(arm,parts,'underworld-creature-pale-burrower',actions,'pale shovel-limbed burrowing hexapod; broad subterranean forelimbs',14)
    begin_action(arm,'PaleBurrower_Idle')
    for f,v in ((1,0),(13,2),(25,0),(37,-2),(48,0)): key(arm,'Thorax',f,(v,0,0))
    begin_action(arm,'PaleBurrower_Scuttle')
    for f in range(1,25,3):
        ph=2*pi*(f-1)/24
        for tag in legs:
            opposed=tag in ('R1','L2','R3'); s=sin(ph+(pi if opposed else 0))
            key(arm,tag+'_Upper',f,(28*s,0,0)); key(arm,tag+'_Lower',f,(-21*s,0,0))
    begin_action(arm,'PaleBurrower_Dig')
    for f,w in ((1,0),(6,1),(12,-.6),(18,1),(26,0)):
        for tag in ('L1','R1'): key(arm,tag+'_Upper',f,(42*w,0,0)); key(arm,tag+'_Lower',f,(-58*w,0,0))
        key(arm,'Head',f,(18*abs(w),0,0))
    begin_action(arm,'PaleBurrower_Emerge')
    for f,p,d in ((1,28,-.30),(8,18,-.18),(16,6,-.06),(24,0,0)): key(arm,'Thorax',f,(p,0,0),loc=(0,0,d))
    begin_action(arm,'PaleBurrower_Bite')
    for f,p in ((1,0),(5,-12),(9,18),(15,0)): key(arm,'Head',f,(p,0,0))
    begin_action(arm,'PaleBurrower_Hit')
    for f,r in ((1,0),(5,18),(10,-6),(16,0)): key(arm,'Thorax',f,(0,0,r))
    begin_action(arm,'PaleBurrower_Death')
    for f,r,d in ((1,0,0),(14,22,-.03),(28,64,-.16),(44,90,-.24)): key(arm,'Thorax',f,(0,0,r),loc=(0,0,d))
    arm.animation_data.action=None; save('underworld-creature-pale-burrower')

def build_rimewing():
    clear_scene(); parts={}
    body=mat('Rimewing_Scale',(.38,.56,.68),.60)
    ice=mat('Rimewing_Crystal',(.70,.90,.98),.26,.08)
    joint=mat('Rimewing_WingMembrane',(.56,.76,.86),.38)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('RW_Chest',(0,0,.48),(.28,.37,.26),body,3),'Chest')
    keep(organic('RW_Neck',(0,.32,.54),(.16,.26,.17),body,2),'Neck')
    keep(organic('RW_Head',(0,.55,.57),(.19,.22,.17),body,2),'Head')
    keep(organic('RW_Hips',(0,-.36,.43),(.22,.27,.20),body,2),'Hips')
    tail=[(0,-.52,.43),(0,-.80,.37),(0,-1.08,.28),(0,-1.31,.18)]
    for i in range(3): keep(seg(f'RW_Tail_{i+1}',tail[i],tail[i+1],.075-i*.017,body),f'Tail_{i+1}')
    for side in (-1,1):
        s='L' if side<0 else 'R'
        keep(box(f'RW_Wing_{s}',(side*.52,.02,.53),(.50,.30,.025),joint,(0,side*.18,side*.16)),f'Wing_{s}')
        for i in range(3): keep(cone(f'RW_WingCrystal_{s}_{i}',(side*(.42+i*.20),-.05+i*.05,.60-i*.03),.030,.34,ice,(0,pi/2,0)),f'Wing_{s}')
        keep(cone(f'RW_Horn_{s}',(side*.08,.70,.69),.025,.24,ice,(.15,0,side*.12)),'Head')
    arm,root=armature('RIG_Rimewing_HOST_WYRM')
    chest=bone(arm,'Chest',(0,-.15,.42),(0,.23,.50),root); neck=bone(arm,'Neck',(0,.20,.50),(0,.48,.56),chest); head=bone(arm,'Head',(0,.45,.56),(0,.70,.57),neck); hips=bone(arm,'Hips',(0,-.15,.42),(0,-.52,.42),chest)
    t1=bone(arm,'Tail_1',tail[0],tail[1],hips); t2=bone(arm,'Tail_2',tail[1],tail[2],t1); bone(arm,'Tail_3',tail[2],tail[3],t2)
    for side in (-1,1):
        s='L' if side<0 else 'R'; bone(arm,f'Wing_{s}',(side*.09,.02,.50),(side*.95,-.04,.52),chest)
    bone(arm,'AttackOrigin',(0,.66,.56),(0,.94,.56),head)
    actions=['Rimewing_PerchIdle','Rimewing_Flight','Rimewing_Glide','Rimewing_FrostCast','Rimewing_Land','Rimewing_Hit','Rimewing_Death']
    finalize_rig(arm,parts,'underworld-creature-rimewing',actions,'small crystal-winged cavern wyrm with segmented tail and perch silhouette',12)
    begin_action(arm,'Rimewing_PerchIdle')
    for f,v in ((1,0),(13,3),(25,0),(37,-3),(48,0)): key(arm,'Chest',f,(v,0,0)); key(arm,'Tail_3',f,(0,0,v*2))
    begin_action(arm,'Rimewing_Flight')
    for f in range(1,25,4):
        ph=2*pi*(f-1)/24; flap=36*sin(ph)
        key(arm,'Wing_L',f,(flap,0,12)); key(arm,'Wing_R',f,(-flap,0,-12)); key(arm,'Tail_2',f,(0,0,12*sin(ph-.4)))
    begin_action(arm,'Rimewing_Glide')
    for f,w in ((1,0),(8,1),(20,1),(28,0)): key(arm,'Wing_L',f,(0,0,22*w)); key(arm,'Wing_R',f,(0,0,-22*w))
    begin_action(arm,'Rimewing_FrostCast')
    for f,p,j in ((1,0,0),(8,-16,0),(14,12,0),(20,2,0)): key(arm,'Neck',f,(p,0,0)); key(arm,'Head',f,(p*.5,0,0))
    begin_action(arm,'Rimewing_Land')
    for f,p in ((1,0),(8,14),(16,26),(24,0)): key(arm,'Chest',f,(p,0,0)); key(arm,'Wing_L',f,(18,0,10)); key(arm,'Wing_R',f,(-18,0,-10))
    begin_action(arm,'Rimewing_Hit')
    for f,r in ((1,0),(5,20),(10,-7),(16,0)): key(arm,'Chest',f,(0,0,r))
    begin_action(arm,'Rimewing_Death')
    for f,r,d in ((1,0,0),(12,28,-.05),(26,72,-.28),(42,96,-.55)): key(arm,'Chest',f,(0,0,r),loc=(0,0,d))
    arm.animation_data.action=None; save('underworld-creature-rimewing')

def build_cryolith():
    clear_scene(); parts={}
    stone=mat('Cryolith_Stone',(.27,.34,.38),.90)
    ice=mat('Cryolith_Ice',(.62,.83,.93),.30,.10)
    core=mat('Cryolith_Core',(.30,.62,.78),.24,.05)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('CG_Pelvis',(0,0,.68),(.40,.32,.34),stone,3),'Pelvis')
    keep(organic('CG_Torso',(0,.02,1.12),(.52,.39,.56),stone,3),'Torso')
    keep(organic('CG_Head',(0,.14,1.64),(.34,.30,.30),stone,3),'Head')
    keep(organic('CG_Core',(0,.34,1.14),(.18,.08,.22),core,2),'Torso')
    for i,(y,z,w) in enumerate(((-.14,1.34,.42),(.05,1.48,.45),(.22,1.56,.38)),1): keep(organic(f'CG_IcePlate_{i}',(0,y,z),(w,.10,.07),ice,2),'Torso')
    arms={}
    for side in (-1,1):
        s='L' if side<0 else 'R'; shoulder=(side*.48,.02,1.36); elbow=(side*.72,.02,1.03); hand=(side*.75,.25,.69)
        arms[s]=(shoulder,elbow,hand)
        keep(seg(f'CG_Upper_{s}',shoulder,elbow,.15,stone),f'Upper_{s}')
        keep(seg(f'CG_Lower_{s}',elbow,hand,.14,stone),f'Lower_{s}')
        keep(organic(f'CG_Fist_{s}',hand,(.20,.20,.18),ice,2),f'Lower_{s}')
    legs={}
    for side in (-1,1):
        s='L' if side<0 else 'R'; hip=(side*.27,-.02,.65); knee=(side*.30,.04,.34); foot=(side*.31,.25,.08)
        legs[s]=(hip,knee,foot)
        keep(seg(f'CG_UpperLeg_{s}',hip,knee,.17,stone),f'UpperLeg_{s}')
        keep(seg(f'CG_LowerLeg_{s}',knee,foot,.15,stone),f'LowerLeg_{s}')
        keep(box(f'CG_Foot_{s}',(side*.31,.31,.07),(.18,.27,.09),ice),f'LowerLeg_{s}')
    for side in (-1,1):
        keep(cone(f'CG_Horn_{"L" if side<0 else "R"}',(side*.17,.17,1.88),.045,.32,ice,(0,0,side*.18)),'Head')
    arm,root=armature('RIG_CryolithGuardian_HOST_CONSTRUCT')
    pel=bone(arm,'Pelvis',(0,-.05,.45),(0,.05,.82),root); torso=bone(arm,'Torso',(0,.02,.80),(0,.10,1.48),pel); head=bone(arm,'Head',(0,.10,1.43),(0,.18,1.80),torso)
    for s,(shoulder,elbow,hand) in arms.items():
        u=bone(arm,f'Upper_{s}',shoulder,elbow,torso); bone(arm,f'Lower_{s}',elbow,hand,u)
    for s,(hip,knee,foot) in legs.items():
        u=bone(arm,f'UpperLeg_{s}',hip,knee,pel); bone(arm,f'LowerLeg_{s}',knee,foot,u)
    bone(arm,'AttackOrigin',(0,.18,1.22),(0,.45,1.22),torso)
    actions=['CryolithGuardian_Idle','CryolithGuardian_Walk','CryolithGuardian_Slam','CryolithGuardian_Swipe','CryolithGuardian_Guard','CryolithGuardian_Hit','CryolithGuardian_Death']
    finalize_rig(arm,parts,'underworld-creature-cryolith-guardian',actions,'massive lithic ice construct with exposed blue core and crystalline fists',14)
    begin_action(arm,'CryolithGuardian_Idle')
    for f,v in ((1,0),(13,1.5),(25,0),(37,-1.5),(48,0)): key(arm,'Torso',f,(v,0,0))
    begin_action(arm,'CryolithGuardian_Walk')
    for f in range(1,33,4):
        ph=2*pi*(f-1)/32; s=sin(ph)
        key(arm,'UpperLeg_L',f,(22*s,0,0)); key(arm,'UpperLeg_R',f,(-22*s,0,0)); key(arm,'Upper_L',f,(-12*s,0,0)); key(arm,'Upper_R',f,(12*s,0,0))
    begin_action(arm,'CryolithGuardian_Slam')
    for f,w in ((1,0),(8,1),(15,-.75),(22,0)):
        key(arm,'Upper_L',f,(55*w,0,0)); key(arm,'Upper_R',f,(55*w,0,0)); key(arm,'Torso',f,(-18*w,0,0))
    begin_action(arm,'CryolithGuardian_Swipe')
    for f,w in ((1,0),(7,1),(13,-.65),(20,0)): key(arm,'Upper_R',f,(0,0,58*w)); key(arm,'Torso',f,(0,0,-24*w))
    begin_action(arm,'CryolithGuardian_Guard')
    for f,w in ((1,0),(8,1),(24,1),(32,0)): key(arm,'Upper_L',f,(32*w,0,-26*w)); key(arm,'Upper_R',f,(32*w,0,26*w))
    begin_action(arm,'CryolithGuardian_Hit')
    for f,r in ((1,0),(5,12),(10,-5),(16,0)): key(arm,'Torso',f,(0,0,r))
    begin_action(arm,'CryolithGuardian_Death')
    for f,r,d in ((1,0,0),(16,18,-.04),(34,58,-.22),(54,88,-.42)): key(arm,'Pelvis',f,(0,0,r),loc=(0,0,d)); key(arm,'Torso',f,(18 if r else 0,0,r*.65))
    arm.animation_data.action=None; save('underworld-creature-cryolith-guardian')

def validate_outputs():
    ids=[
        'underworld-creature-rime-moth',
        'underworld-creature-frost-tick',
        'underworld-creature-iceblind',
        'underworld-creature-pale-burrower',
        'underworld-creature-rimewing',
        'underworld-creature-glacier-stalker',
        'underworld-creature-cryolith-guardian',
    ]
    missing=[model_id for model_id in ids if not (OUT/(model_id+'.blend')).is_file()]
    if missing: raise RuntimeError('Frozen creature outputs missing: '+', '.join(missing))
    return ids

build_rime_moth()
build_frost_tick()
build_quadruped('underworld-creature-iceblind','IB','RIG_Iceblind_HOST_QUADRUPED',armored=False,sensory=True)
build_pale_burrower()
build_rimewing()
build_quadruped('underworld-creature-glacier-stalker','GS','RIG_GlacierStalker_HOST_QUADRUPED',armored=True,sensory=False)
build_cryolith()
ids=validate_outputs()
print('AUTHORED Frozen Caverns creature family: '+', '.join(ids),flush=True)
