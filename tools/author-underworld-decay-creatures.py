#!/usr/bin/env python3
"""Author all seven Great Decay creature production bodies.

Each output is an individual rigid-segment-weighted source blend consumed by the shared Magenheim
creature runtime exporter. The designs deliberately break from the donor silhouettes while retaining
the donor object as the invisible gameplay/network chassis.
"""
from pathlib import Path
from math import pi, radians, sin, cos
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source'
OUT.mkdir(parents=True,exist_ok=True)

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.actions,bpy.data.meshes,bpy.data.materials,bpy.data.curves,bpy.data.armatures):
        for block in list(collection):
            try: collection.remove(block)
            except RuntimeError: pass

def mat(name,color,rough=.72,metal=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough; bs.inputs['Metallic'].default_value=metal
    return m

def organic(name,loc,scale,material,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material); return o

def seg(name,a,b,r,material,verts=12):
    d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2)
    o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'
    o.data.materials.append(material); return o

def cone(name,loc,r,d,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r,radius2=.002,depth=d,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.data.materials.append(material); return o

def box(name,loc,scale,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return o

def uv(o):
    layer=o.data.uv_layers.new(name='DecayUV')
    for poly in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(poly.normal[i]))
        for li in poly.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co
            q=((co.y,co.z),(co.x,co.z),(co.x,co.y))[axis]
            layer.data[li].uv=((q[0]*1.41)%1,(q[1]*1.41)%1)

def armature(name):
    bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
    arm=bpy.context.object; arm.name=name
    root=arm.data.edit_bones[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.18)
    return arm,root

def bone(arm,name,h,t,parent=None):
    b=arm.data.edit_bones.new(name); b.head=h; b.tail=t; b.parent=parent; return b

def finalize(arm,parts,model_id,actions,readability,min_meshes=12):
    bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if len(meshes)<min_meshes: raise RuntimeError(f'{model_id}: anatomical breakup too low {len(meshes)} < {min_meshes}')
    for o in meshes:
        if not o.data.uv_layers: uv(o)
        b=parts.get(o.name)
        if not b or b not in arm.data.bones: raise RuntimeError(f'{model_id}: no valid rigid binding for {o.name}: {b}')
        mod=o.modifiers.new('MagenheimRigidArmature','ARMATURE'); mod.object=arm
        vg=o.vertex_groups.new(name=b); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')
    arm.animation_data_create(); arm.animation_data.action=None
    sc=bpy.context.scene; sc.render.fps=24
    sc['magenheim_model_id']=model_id; sc['magenheim_skinning']='rigid-segment-weighted'; sc['magenheim_authored_actions']=','.join(actions)
    arm['authored_actions']=sc['magenheim_authored_actions']; arm['production_contract']='production-creature-r2'; arm['readability_contract']=readability

def reset(arm):
    for p in arm.pose.bones:
        p.rotation_mode='XYZ'; p.rotation_euler=(0,0,0); p.location=(0,0,0); p.scale=(1,1,1)

def action(arm,name):
    old=bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    a=bpy.data.actions.new(name); a.use_fake_user=True
    arm.animation_data_create(); arm.animation_data.action=a; reset(arm); return a

def key(arm,b,frame,rot=None,loc=None,scale=None):
    p=arm.pose.bones.get(b)
    if p is None: raise RuntimeError(f'{arm.name}: missing {b}')
    if rot is not None: p.rotation_euler=tuple(radians(v) for v in rot); p.keyframe_insert('rotation_euler',frame=frame,group=b)
    if loc is not None: p.location=loc; p.keyframe_insert('location',frame=frame,group=b)
    if scale is not None: p.scale=scale; p.keyframe_insert('scale',frame=frame,group=b)

def save(model_id):
    bpy.context.preferences.filepaths.save_version=0
    path=OUT/(model_id+'.blend'); bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
    print(f'AUTHORED {model_id}: {path}',flush=True)

def build_rotling():
    clear_scene(); parts={}
    flesh=mat('Rotling_Biomass',(.31,.30,.17),.78); rot=mat('Rotling_Rot',(.45,.43,.22),.70); bone_m=mat('Rotling_Bone',(.61,.57,.43),.76)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('ROT_Core',(0,0,.18),(.24,.28,.16),flesh,3),'Body'); keep(organic('ROT_Sac',(0,-.24,.19),(.22,.25,.18),rot,3),'Abdomen'); keep(organic('ROT_Maw',(0,.25,.14),(.16,.16,.10),flesh,2),'Head')
    legs={}
    for i,y in enumerate((.16,.01,-.16),1):
        for side in (-1,1):
            s='L' if side<0 else 'R'; tag=f'{s}{i}'; hip=(side*.15,y,.15); knee=(side*.31,y+.03,.09); foot=(side*.43,y+.08,.025); legs[tag]=(hip,knee,foot)
            keep(seg(f'ROT_{tag}_Upper',hip,knee,.034,flesh),f'{tag}_Upper'); keep(seg(f'ROT_{tag}_Lower',knee,foot,.025,bone_m),f'{tag}_Lower')
    for side in (-1,1): keep(cone(f'ROT_Fang_{"L" if side<0 else "R"}',(side*.045,.38,.12),.018,.10,bone_m,(pi/2,0,0)),'Head')
    arm,root=armature('RIG_Rotling_HOST_HEXAPOD'); body=bone(arm,'Body',(0,-.16,.15),(0,.18,.18),root); abd=bone(arm,'Abdomen',(0,-.12,.18),(0,-.43,.18),body); head=bone(arm,'Head',(0,.15,.14),(0,.37,.13),body)
    for tag,(hip,knee,foot) in legs.items(): u=bone(arm,f'{tag}_Upper',hip,knee,body if tag.endswith(('1','2')) else abd); bone(arm,f'{tag}_Lower',knee,foot,u)
    bone(arm,'AttackOrigin',(0,.32,.12),(0,.48,.12),head)
    actions=['Rotling_Idle','Rotling_Scuttle','Rotling_Latch','Rotling_Hit','Rotling_Death']; finalize(arm,parts,'underworld-creature-rotling',actions,'wet six-legged carrion biomass scavenger with exposed bone feet',12)
    action(arm,'Rotling_Idle')
    for f,v in ((1,0),(12,3),(24,0),(36,-3),(48,0)): key(arm,'Abdomen',f,scale=(1+v*.012,1+v*.018,1+v*.012))
    action(arm,'Rotling_Scuttle')
    for f in range(1,25,3):
        ph=2*pi*(f-1)/24
        for tag in legs:
            s=sin(ph+(pi if tag in ('R1','L2','R3') else 0)); key(arm,tag+'_Upper',f,(29*s,0,0)); key(arm,tag+'_Lower',f,(-23*s,0,0))
    action(arm,'Rotling_Latch')
    for f,p in ((1,0),(5,26),(9,-10),(14,18),(20,0)): key(arm,'Head',f,(p,0,0)); key(arm,'Body',f,(-p*.2,0,0))
    action(arm,'Rotling_Hit')
    for f,r in ((1,0),(4,18),(9,-6),(14,0)): key(arm,'Body',f,(0,0,r))
    action(arm,'Rotling_Death')
    for f,r,d in ((1,0,0),(10,22,-.02),(22,68,-.10),(36,90,-.17)): key(arm,'Body',f,(0,0,r),loc=(0,0,d))
    arm.animation_data.action=None; save('underworld-creature-rotling')

def build_rooted_plant(model_id,prefix,orchard=False):
    clear_scene(); parts={}
    flesh=mat(prefix+'_Flesh',(.36,.30,.18),.82); rot=mat(prefix+'_Rot',(.51,.48,.24),.73); bone_m=mat(prefix+'_Bone',(.65,.61,.45),.78); dark=mat(prefix+'_Maw',(.16,.12,.09),.55)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic(prefix+'_RootCore',(0,0,.25),(.42 if orchard else .28,.38 if orchard else .26,.24),flesh,3),'Root')
    root_tips=[]
    count=8 if orchard else 6
    for i in range(count):
        ang=2*pi*i/count; inner=(cos(ang)*.18,sin(ang)*.18,.16); outer=(cos(ang)*(1.05 if orchard else .72),sin(ang)*(1.05 if orchard else .72),.04); root_tips.append(outer)
        keep(seg(f'{prefix}_Root_{i+1}',inner,outer,.065 if orchard else .045,rot), 'Root')
    stalk_count=4 if orchard else 1
    stalk_bones=[]
    for i in range(stalk_count):
        x=(i-(stalk_count-1)/2)*.25 if orchard else 0; y=(-.08+(.11*(i%2))) if orchard else 0
        base=(x,y,.30); top=(x,y,1.28 if orchard else .98)
        name='Stalk' if not orchard else f'Stalk_{i+1}'; stalk_bones.append(name)
        keep(seg(f'{prefix}_{name}',base,top,.10 if orchard else .085,flesh),name)
        keep(organic(f'{prefix}_{name}_Bloom',top,(.24,.24,.18),rot,3),name)
        for p in range(5):
            ang=2*pi*p/5; petal=(top[0]+cos(ang)*.23,top[1]+sin(ang)*.23,top[2]+.03)
            keep(organic(f'{prefix}_{name}_Petal_{p+1}',petal,(.17,.09,.045),rot,2),name)
        keep(organic(f'{prefix}_{name}_Maw',(top[0],top[1],top[2]+.06),(.11,.11,.06),dark,2),name)
        for p in range(4): keep(cone(f'{prefix}_{name}_Tooth_{p+1}',(top[0]+(p-1.5)*.035,top[1]+.08,top[2]+.07),.012,.07,bone_m,(pi/2,0,0)),name)
    if orchard:
        for i in range(6):
            x=((i%3)-1)*.28; y=-.20-(i//3)*.18
            keep(organic(f'{prefix}_Pod_{i+1}',(x,y,.70+(i%2)*.10),(.12,.16,.20),flesh,2),stalk_bones[i%len(stalk_bones)])
    arm,root=armature('RIG_'+prefix+'_HOST_ROOTED'); rb=bone(arm,'Root',(0,0,.05),(0,0,.38),root)
    for i,name in enumerate(stalk_bones):
        x=(i-(len(stalk_bones)-1)/2)*.25 if orchard else 0; y=(-.08+(.11*(i%2))) if orchard else 0
        bone(arm,name,(x,y,.28),(x,y,1.38 if orchard else 1.08),rb)
    bone(arm,'AttackOrigin',(0,.08,.75),(0,.42,.75),rb)
    base='CorpseOrchard' if orchard else 'CarrionBloom'
    actions=[base+'_Idle',base+'_Pulse',base+'_Cast',base+'_Hit',base+'_Death']
    finalize(arm,parts,model_id,actions,'rooted carrion organism; physically radial roots and fleshy bloom mouths',18 if orchard else 10)
    action(arm,base+'_Idle')
    for f,w in ((1,0),(13,.5),(25,1),(37,.5),(49,0)):
        for name in stalk_bones: key(arm,name,f,(3*w,0,(2 if orchard else 0)*w),scale=(1+w*.025,1+w*.025,1+w*.045))
    action(arm,base+'_Pulse')
    for f,w in ((1,0),(8,.55),(16,1),(24,.55),(32,0)):
        key(arm,'Root',f,scale=(1+w*.08,1+w*.08,1+w*.05))
        for name in stalk_bones: key(arm,name,f,scale=(1+w*.06,1+w*.06,1+w*.11))
    action(arm,base+'_Cast')
    for f,w in ((1,0),(7,1),(13,-.55),(20,0)):
        for idx,name in enumerate(stalk_bones): key(arm,name,f,(-22*w,0,(idx-len(stalk_bones)/2)*8*w))
    action(arm,base+'_Hit')
    for f,r in ((1,0),(5,14),(10,-5),(16,0)): key(arm,'Root',f,(0,0,r))
    action(arm,base+'_Death')
    for f,w in ((1,0),(12,.25),(26,.70),(42,1)):
        for idx,name in enumerate(stalk_bones): key(arm,name,f,(72*w,0,(idx-len(stalk_bones)/2)*18*w))
        key(arm,'Root',f,scale=(1,1,1-.35*w))
    arm.animation_data.action=None; save(model_id)

def build_humanoid(model_id,prefix,warden=False):
    clear_scene(); parts={}
    flesh=mat(prefix+'_Flesh',(.32,.29,.20),.82); rot=mat(prefix+'_Graft',(.50,.47,.24),.72); bone_m=mat(prefix+'_Bone',(.67,.62,.48),.78); cloth=mat(prefix+'_Cloth',(.18,.17,.13),.90)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic(prefix+'_Pelvis',(0,0,.78),(.30,.22,.25),flesh,3),'Pelvis'); keep(organic(prefix+'_Torso',(0,.02,1.18),(.37,.28,.43),flesh,3),'Torso'); keep(organic(prefix+'_Head',(0,.07,1.65),(.23,.22,.25),flesh,2),'Head')
    keep(box(prefix+'_Rag',(0,-.05,.98),(.34,.12,.31),cloth),'Torso')
    for i in range(5): keep(organic(f'{prefix}_GraftNodule_{i+1}',(((i%2)*2-1)*(.21+i*.018),.17,1.15+i*.09),(.08,.07,.09),rot,2),'Torso')
    arms={}
    for side in (-1,1):
        s='L' if side<0 else 'R'; huge=warden and side>0
        shoulder=(side*.36,.02,1.38); elbow=(side*(.64 if huge else .56),.08,1.05); hand=(side*(.68 if huge else .59),.28,.74); arms[s]=(shoulder,elbow,hand)
        keep(seg(f'{prefix}_Upper_{s}',shoulder,elbow,.13 if huge else .085,flesh),f'Upper_{s}'); keep(seg(f'{prefix}_Lower_{s}',elbow,hand,.15 if huge else .075,flesh),f'Lower_{s}'); keep(organic(f'{prefix}_Hand_{s}',hand,(.18 if huge else .10,.16 if huge else .10,.15 if huge else .09),bone_m,2),f'Lower_{s}')
        if huge:
            for i in range(4): keep(cone(f'{prefix}_Knuckle_{s}_{i+1}',(hand[0]+(i-1.5)*.04,hand[1]+.13,hand[2]),.022,.11,bone_m,(pi/2,0,0)),f'Lower_{s}')
    legs={}
    for side in (-1,1):
        s='L' if side<0 else 'R'; hip=(side*.20,-.02,.75); knee=(side*.21,.04,.42); foot=(side*.21,.20,.08); legs[s]=(hip,knee,foot)
        keep(seg(f'{prefix}_UpperLeg_{s}',hip,knee,.10,flesh),f'UpperLeg_{s}'); keep(seg(f'{prefix}_LowerLeg_{s}',knee,foot,.085,flesh),f'LowerLeg_{s}'); keep(organic(f'{prefix}_Foot_{s}',foot,(.11,.18,.07),bone_m,2),f'LowerLeg_{s}')
    if not warden:
        for side in (-1,1): keep(cone(f'{prefix}_JawSpike_{"L" if side<0 else "R"}',(side*.10,.27,1.59),.020,.15,bone_m,(pi/2,0,side*.10)),'Head')
    else:
        keep(organic(prefix+'_ShoulderGraft',(.34,.02,1.48),(.26,.22,.30),rot,3),'Upper_R')
        for i in range(4): keep(cone(f'{prefix}_BackSpine_{i+1}',((i-1.5)*.09,-.18,1.30+i*.10),.025,.24,bone_m,(.25,0,0)),'Torso')
    arm,root=armature('RIG_'+prefix+'_HOST_BIPED'); pel=bone(arm,'Pelvis',(0,0,.58),(0,0,.90),root); torso=bone(arm,'Torso',(0,0,.88),(0,.04,1.47),pel); head=bone(arm,'Head',(0,.04,1.45),(0,.08,1.80),torso)
    for s,(shoulder,elbow,hand) in arms.items(): u=bone(arm,f'Upper_{s}',shoulder,elbow,torso); bone(arm,f'Lower_{s}',elbow,hand,u)
    for s,(hip,knee,foot) in legs.items(): u=bone(arm,f'UpperLeg_{s}',hip,knee,pel); bone(arm,f'LowerLeg_{s}',knee,foot,u)
    bone(arm,'AttackOrigin',(0,.10,1.10),(0,.40,1.10),torso)
    base='GraftWarden' if warden else 'SporeHusk'; actions=[base+'_Idle',base+'_Walk',base+'_Attack',base+'_GraftAttack',base+'_Hit',base+'_Death']
    finalize(arm,parts,model_id,actions,'asymmetric grafted carrion biped; donor humanoid body no longer visible',14)
    action(arm,base+'_Idle')
    for f,v in ((1,0),(13,2),(25,0),(37,-2),(48,0)): key(arm,'Torso',f,(v,0,0))
    action(arm,base+'_Walk')
    for f in range(1,33,4):
        ph=2*pi*(f-1)/32; s=sin(ph); key(arm,'UpperLeg_L',f,(26*s,0,0)); key(arm,'UpperLeg_R',f,(-26*s,0,0)); key(arm,'Upper_L',f,(-18*s,0,0)); key(arm,'Upper_R',f,(18*s,0,0))
    action(arm,base+'_Attack')
    for f,w in ((1,0),(7,1),(13,-.55),(20,0)): key(arm,'Upper_R',f,(0,0,58*w)); key(arm,'Torso',f,(0,0,-20*w))
    action(arm,base+'_GraftAttack')
    for f,w in ((1,0),(8,1),(15,-.70),(24,0)): key(arm,'Upper_L' if not warden else 'Upper_R',f,(52*w,0,0)); key(arm,'Torso',f,(-18*w,0,0))
    action(arm,base+'_Hit')
    for f,r in ((1,0),(5,15),(10,-6),(16,0)): key(arm,'Torso',f,(0,0,r))
    action(arm,base+'_Death')
    for f,r,d in ((1,0,0),(14,20,-.04),(30,58,-.20),(48,88,-.37)): key(arm,'Pelvis',f,(0,0,r),loc=(0,0,d)); key(arm,'Torso',f,(16 if r else 0,0,r*.65))
    arm.animation_data.action=None; save(model_id)

def build_marrow_creeper():
    clear_scene(); parts={}
    marrow=mat('MarrowCreeper_Marrow',(.58,.54,.40),.76); flesh=mat('MarrowCreeper_Flesh',(.30,.27,.18),.82); bone_m=mat('MarrowCreeper_Bone',(.73,.68,.53),.80)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('MC_Core',(0,0,.30),(.28,.38,.22),flesh,3),'Body'); keep(organic('MC_Head',(0,.36,.29),(.20,.22,.17),marrow,2),'Head'); keep(organic('MC_Abdomen',(0,-.43,.27),(.24,.38,.20),flesh,3),'Abdomen')
    for i,y in enumerate((-.26,-.10,.08,.24),1):
        keep(seg(f'MC_Rib_L_{i}',(-.05,y,.30),(-.30,y,.45),.025,bone_m),'Body'); keep(seg(f'MC_Rib_R_{i}',(.05,y,.30),(.30,y,.45),.025,bone_m),'Body')
    legs={}
    for i,y in enumerate((.22,-.05,-.34),1):
        for side in (-1,1):
            s='L' if side<0 else 'R'; tag=f'{s}{i}'; hip=(side*.24,y,.28); knee=(side*.48,y+.04,.16); foot=(side*.62,y+.13,.045); legs[tag]=(hip,knee,foot)
            keep(seg(f'MC_{tag}_Upper',hip,knee,.044,bone_m),f'{tag}_Upper'); keep(seg(f'MC_{tag}_Lower',knee,foot,.034,bone_m),f'{tag}_Lower')
    arm,root=armature('RIG_MarrowCreeper_HOST_HEXAPOD'); body=bone(arm,'Body',(0,-.14,.26),(0,.23,.31),root); abd=bone(arm,'Abdomen',(0,-.14,.27),(0,-.64,.26),body); head=bone(arm,'Head',(0,.20,.29),(0,.50,.28),body)
    for tag,(hip,knee,foot) in legs.items(): u=bone(arm,f'{tag}_Upper',hip,knee,body if tag.endswith(('1','2')) else abd); bone(arm,f'{tag}_Lower',knee,foot,u)
    bone(arm,'AttackOrigin',(0,.43,.27),(0,.68,.27),head)
    actions=['MarrowCreeper_Idle','MarrowCreeper_Scuttle','MarrowCreeper_Strike','MarrowCreeper_Guard','MarrowCreeper_Hit','MarrowCreeper_Death']; finalize(arm,parts,'underworld-creature-marrow-creeper',actions,'bone-rib supported low insect; explicit marrow cage silhouette',16)
    action(arm,'MarrowCreeper_Idle')
    for f,v in ((1,0),(13,2),(25,0),(37,-2),(48,0)): key(arm,'Body',f,(v,0,0))
    action(arm,'MarrowCreeper_Scuttle')
    for f in range(1,25,3):
        ph=2*pi*(f-1)/24
        for tag in legs:
            s=sin(ph+(pi if tag in ('R1','L2','R3') else 0)); key(arm,tag+'_Upper',f,(30*s,0,0)); key(arm,tag+'_Lower',f,(-22*s,0,0))
    action(arm,'MarrowCreeper_Strike')
    for f,p in ((1,0),(6,-14),(11,22),(18,0)): key(arm,'Head',f,(p,0,0)); key(arm,'Body',f,(-p*.35,0,0))
    action(arm,'MarrowCreeper_Guard')
    for f,p in ((1,0),(8,1),(24,1),(32,0)): key(arm,'Head',f,(22*p,0,0)); key(arm,'Body',f,(-10*p,0,0))
    action(arm,'MarrowCreeper_Hit')
    for f,r in ((1,0),(5,18),(10,-6),(16,0)): key(arm,'Body',f,(0,0,r))
    action(arm,'MarrowCreeper_Death')
    for f,r,d in ((1,0,0),(14,22,-.03),(28,65,-.15),(44,90,-.24)): key(arm,'Body',f,(0,0,r),loc=(0,0,d))
    arm.animation_data.action=None; save('underworld-creature-marrow-creeper')

def build_decay_hound():
    clear_scene(); parts={}
    hide=mat('DecayHound_Hide',(.27,.25,.18),.84); rot=mat('DecayHound_Rot',(.47,.44,.23),.72); bone_m=mat('DecayHound_Bone',(.65,.60,.46),.78)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('DH_Pelvis',(0,-.57,.62),(.29,.33,.28),hide,3),'Pelvis'); keep(organic('DH_Chest',(0,.03,.76),(.35,.45,.34),hide,3),'Chest'); keep(organic('DH_Neck',(0,.39,.80),(.23,.25,.23),hide,2),'Neck'); keep(organic('DH_Head',(0,.64,.81),(.24,.28,.22),hide,3),'Head')
    for i,(x,y,z) in enumerate(((-.18,.02,.95),(.16,.14,.98),(-.14,-.22,.87),(.13,-.42,.82)),1): keep(organic(f'DH_Tumor_{i}',(x,y,z),(.12,.11,.13),rot,2),'Chest' if i<3 else 'Pelvis')
    for side in (-1,1): keep(cone(f'DH_JawBone_{"L" if side<0 else "R"}',(side*.11,.86,.75),.025,.20,bone_m,(pi/2,0,side*.12)),'Head')
    legs={}; tail=[(0,-.74,.62),(0,-1.02,.56),(0,-1.28,.47)]
    for i in range(2): keep(seg(f'DH_Tail_{i+1}',tail[i],tail[i+1],.07-i*.018,hide),f'Tail_{i+1}')
    for side in (-1,1):
        for fore,y in ((True,.18),(False,-.51)):
            s='L' if side<0 else 'R'; tag=('F' if fore else 'H')+s; hip=(side*.27,y,.65); knee=(side*.31,y+.14,.37); hock=(side*.27,y-.04,.17); foot=(side*.25,y+.18,.055); legs[tag]=(hip,knee,hock,foot)
            keep(seg(f'DH_{tag}_Upper',hip,knee,.058,hide),f'{tag}_Upper'); keep(seg(f'DH_{tag}_Lower',knee,hock,.048,hide),f'{tag}_Lower'); keep(seg(f'DH_{tag}_Hock',hock,foot,.038,bone_m),f'{tag}_Hock')
    arm,root=armature('RIG_DecayHound_HOST_QUADRUPED'); pel=bone(arm,'Pelvis',(0,-.70,.55),(0,-.39,.69),root); chest=bone(arm,'Chest',(0,-.37,.69),(0,.18,.78),pel); neck=bone(arm,'Neck',(0,.18,.78),(0,.49,.82),chest); head=bone(arm,'Head',(0,.48,.82),(0,.80,.80),neck)
    for tag,(hip,knee,hock,foot) in legs.items(): parent=chest if tag.startswith('F') else pel; u=bone(arm,f'{tag}_Upper',hip,knee,parent); l=bone(arm,f'{tag}_Lower',knee,hock,u); bone(arm,f'{tag}_Hock',hock,foot,l)
    t1=bone(arm,'Tail_1',tail[0],tail[1],pel); bone(arm,'Tail_2',tail[1],tail[2],t1); bone(arm,'AttackOrigin',(0,.79,.78),(0,1.04,.78),head)
    actions=['DecayHound_Idle','DecayHound_Walk','DecayHound_Sprint','DecayHound_Bite','DecayHound_Hit','DecayHound_Death']; finalize(arm,parts,'underworld-creature-decay-hound',actions,'diseased carrion quadruped with visible graft nodules and exposed jaw bone',14)
    action(arm,'DecayHound_Idle')
    for f,v in ((1,0),(13,2),(25,0),(37,-2),(48,0)): key(arm,'Chest',f,(v,0,0)); key(arm,'Tail_2',f,(0,0,v*2))
    for name,end,amp in (('DecayHound_Walk',32,19),('DecayHound_Sprint',20,36)):
        action(arm,name)
        for f in range(1,end+1,4):
            ph=2*pi*(f-1)/max(1,end-1)
            for tag in legs:
                s=sin(ph+(pi if tag in ('FR','HL') else 0)); key(arm,tag+'_Upper',f,(amp*s,0,0)); key(arm,tag+'_Lower',f,(-amp*.68*s,0,0)); key(arm,tag+'_Hock',f,(amp*.82*s,0,0))
    action(arm,'DecayHound_Bite')
    for f,p in ((1,0),(5,-12),(9,18),(15,0)): key(arm,'Neck',f,(p,0,0)); key(arm,'Head',f,(p*.4,0,0))
    action(arm,'DecayHound_Hit')
    for f,r in ((1,0),(5,18),(10,-6),(16,0)): key(arm,'Chest',f,(0,0,r))
    action(arm,'DecayHound_Death')
    for f,r,d in ((1,0,0),(14,18,-.04),(30,58,-.20),(46,88,-.34)): key(arm,'Pelvis',f,(0,0,r),loc=(0,0,d)); key(arm,'Chest',f,(14 if r else 0,0,r*.68))
    arm.animation_data.action=None; save('underworld-creature-decay-hound')

build_rotling()
build_rooted_plant('underworld-creature-carrion-bloom','CB',False)
build_humanoid('underworld-creature-spore-husk','SH',False)
build_marrow_creeper()
build_decay_hound()
build_humanoid('underworld-creature-graft-warden','GW',True)
build_rooted_plant('underworld-creature-corpse-orchard','CO',True)

ids=[
 'underworld-creature-rotling','underworld-creature-carrion-bloom','underworld-creature-spore-husk',
 'underworld-creature-marrow-creeper','underworld-creature-decay-hound','underworld-creature-graft-warden',
 'underworld-creature-corpse-orchard'
]
missing=[model_id for model_id in ids if not (OUT/(model_id+'.blend')).is_file()]
if missing: raise RuntimeError('Great Decay creature outputs missing: '+', '.join(missing))
print('AUTHORED Great Decay creature family: '+', '.join(ids),flush=True)
