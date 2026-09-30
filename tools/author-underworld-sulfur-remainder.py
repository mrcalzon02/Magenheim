#!/usr/bin/env python3
"""Author the remaining three Sulfurous Wastes creature bodies.

Outputs:
- Fume Wraith: smoke-wreathed spectral predator with physical tendril silhouette.
- Magma Leaper: squat volcanic ambush animal with oversized hindlimbs.
- Furnace Golem: plated industrial construct with furnace chest and vent stacks.

Each source uses the shared rigid-segment-weighted creature contract.
"""
from pathlib import Path
from math import pi, radians, sin
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

def mat(name,color,rough=.72,metal=0,alpha=1):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,alpha); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough; bs.inputs['Metallic'].default_value=metal; bs.inputs['Alpha'].default_value=alpha
    if alpha<1: m.surface_render_method='DITHERED'
    return m

def organic(name,loc,scale,material,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return o

def seg(name,a,b,r,material,verts=12):
    d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2)
    o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(material); return o

def cone(name,loc,r,d,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r,radius2=.002,depth=d,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.data.materials.append(material); return o

def box(name,loc,scale,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); return o

def uv(o):
    layer=o.data.uv_layers.new(name='SulfurRemainderUV')
    for poly in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(poly.normal[i]))
        for li in poly.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co
            q=((co.y,co.z),(co.x,co.z),(co.x,co.y))[axis]
            layer.data[li].uv=((q[0]*1.33)%1,(q[1]*1.33)%1)

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
    a=bpy.data.actions.new(name); a.use_fake_user=True; arm.animation_data_create(); arm.animation_data.action=a; reset(arm); return a

def key(arm,b,frame,rot=None,loc=None,scale=None):
    p=arm.pose.bones.get(b)
    if p is None: raise RuntimeError(f'{arm.name}: missing {b}')
    if rot is not None: p.rotation_euler=tuple(radians(v) for v in rot); p.keyframe_insert('rotation_euler',frame=frame,group=b)
    if loc is not None: p.location=loc; p.keyframe_insert('location',frame=frame,group=b)
    if scale is not None: p.scale=scale; p.keyframe_insert('scale',frame=frame,group=b)

def save(model_id):
    path=OUT/(model_id+'.blend'); bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True); print(f'AUTHORED {model_id}: {path}',flush=True)

def build_fume_wraith():
    clear_scene(); parts={}
    smoke=mat('FumeWraith_Smoke',(.20,.20,.18),.38,0,.76); ash=mat('FumeWraith_AshCore',(.13,.12,.10),.82); sulfur=mat('FumeWraith_SulfurTrace',(.52,.39,.16),.55)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('FW_Core',(0,0,1.10),(.28,.24,.46),ash,3),'Spine'); keep(organic('FW_Hood',(0,.08,1.55),(.31,.28,.33),smoke,3),'Head'); keep(organic('FW_FaceVoid',(0,.27,1.52),(.16,.08,.17),ash,2),'Head')
    tendrils=[]
    for i,(x,y,z) in enumerate(((-.20,-.04,.82),(.18,-.10,.78),(-.08,-.18,.64),(.10,-.22,.58)),1):
        end=(x*1.7,y-.26,z-.55); tendrils.append((f'Tendril_{i}',(x,y,z),end))
        keep(seg(f'FW_Tendril_{i}',(x,y,z),end,.055,smoke),f'Tendril_{i}')
        keep(organic(f'FW_TendrilPuff_{i}',end,(.09,.12,.13),smoke,2),f'Tendril_{i}')
    arms={}
    for side in (-1,1):
        s='L' if side<0 else 'R'; shoulder=(side*.24,.02,1.30); elbow=(side*.48,.10,1.08); hand=(side*.62,.27,.88); arms[s]=(shoulder,elbow,hand)
        keep(seg(f'FW_Upper_{s}',shoulder,elbow,.065,smoke),f'Upper_{s}'); keep(seg(f'FW_Lower_{s}',elbow,hand,.050,smoke),f'Lower_{s}')
        for i in range(3): keep(cone(f'FW_Claw_{s}_{i+1}',(hand[0]+(i-1)*.025,hand[1]+.08,hand[2]),.012,.10,sulfur,(pi/2,0,0)),f'Lower_{s}')
    for i in range(5): keep(organic(f'FW_FumeNode_{i+1}',(((i%2)*2-1)*(.18+i*.015),-.09+i*.05,1.05+i*.10),(.10,.12,.14),smoke,2),'Spine')
    arm,root=armature('RIG_FumeWraith_HOST_SPECTER'); spine=bone(arm,'Spine',(0,0,.55),(0,0,1.38),root); head=bone(arm,'Head',(0,0,1.34),(0,.10,1.78),spine)
    for s,(shoulder,elbow,hand) in arms.items(): u=bone(arm,f'Upper_{s}',shoulder,elbow,spine); bone(arm,f'Lower_{s}',elbow,hand,u)
    for name,start,end in tendrils: bone(arm,name,start,end,spine)
    bone(arm,'AttackOrigin',(0,.22,1.18),(0,.52,1.18),spine)
    actions=['FumeWraith_Drift','FumeWraith_Surge','FumeWraith_Strike','FumeWraith_Dissipate','FumeWraith_Hit','FumeWraith_Death']; finalize(arm,parts,'underworld-creature-fume-wraith',actions,'ash-core specter wrapped in physical smoke tendrils; no humanoid wraith cloth silhouette',14)
    action(arm,'FumeWraith_Drift')
    for f in range(1,49,6):
        ph=2*pi*(f-1)/48; key(arm,'Spine',f,(3*sin(ph),0,5*sin(ph*.5)))
        for i in range(1,5): key(arm,f'Tendril_{i}',f,(8*sin(ph+i),0,10*sin(ph*.7+i)))
    action(arm,'FumeWraith_Surge')
    for f,p,s in ((1,0,1),(8,-14,1.08),(16,18,.94),(24,0,1)): key(arm,'Spine',f,(p,0,0),scale=(s,s,s)); key(arm,'Head',f,(p*.45,0,0))
    action(arm,'FumeWraith_Strike')
    for f,w in ((1,0),(6,1),(12,-.55),(20,0)): key(arm,'Upper_R',f,(0,0,62*w)); key(arm,'Spine',f,(0,0,-22*w)); key(arm,'Lower_R',f,(28*w,0,0))
    action(arm,'FumeWraith_Dissipate')
    for f,s in ((1,1),(10,1.18),(22,.72),(36,.24)): key(arm,'Spine',f,scale=(s,s,s)); key(arm,'Head',f,scale=(s,s,s))
    action(arm,'FumeWraith_Hit')
    for f,r in ((1,0),(5,18),(10,-6),(16,0)): key(arm,'Spine',f,(0,0,r))
    action(arm,'FumeWraith_Death')
    for f,s,d in ((1,1,0),(12,1.18,-.02),(26,.70,-.16),(44,.18,-.45)): key(arm,'Spine',f,scale=(s,s,s),loc=(0,0,d)); key(arm,'Head',f,scale=(s,s,s))
    arm.animation_data.action=None; save('underworld-creature-fume-wraith')

def build_magma_leaper():
    clear_scene(); parts={}
    hide=mat('MagmaLeaper_Hide',(.19,.13,.10),.82); basalt=mat('MagmaLeaper_Basalt',(.12,.11,.10),.93); vent=mat('MagmaLeaper_Vent',(.58,.20,.07),.42); tooth=mat('MagmaLeaper_Tooth',(.55,.43,.28),.74)
    def keep(o,b): parts[o.name]=b; return o
    keep(organic('ML_Pelvis',(0,-.35,.56),(.36,.34,.31),hide,3),'Pelvis'); keep(organic('ML_Chest',(0,.08,.68),(.34,.38,.31),hide,3),'Chest'); keep(organic('ML_Head',(0,.38,.67),(.26,.28,.22),hide,3),'Head'); keep(organic('ML_Jaw',(0,.56,.59),(.23,.22,.09),hide,2),'Jaw')
    for i,(y,z,w) in enumerate(((-.37,.81,.27),(-.10,.88,.31),(.16,.90,.25)),1): keep(organic(f'ML_BackPlate_{i}',(0,y,z),(w,.15,.055),basalt,2),'Pelvis' if i==1 else 'Chest')
    keep(organic('ML_ThroatVent',(0,.31,.53),(.16,.18,.12),vent,2),'Jaw')
    legs={}
    # Oversized rear legs define silhouette and leap mechanics.
    for side in (-1,1):
        s='L' if side<0 else 'R'
        hip=(side*.28,-.36,.56); knee=(side*.44,-.12,.27); hock=(side*.42,.18,.12); foot=(side*.39,.42,.055); legs['H'+s]=(hip,knee,hock,foot)
        keep(seg(f'ML_H{s}_Upper',hip,knee,.105,hide),f'H{s}_Upper'); keep(seg(f'ML_H{s}_Lower',knee,hock,.082,hide),f'H{s}_Lower'); keep(seg(f'ML_H{s}_Hock',hock,foot,.060,basalt),f'H{s}_Hock'); keep(organic(f'ML_H{s}_Foot',foot,(.13,.21,.065),basalt,2),f'H{s}_Foot')
        hipf=(side*.25,.13,.66); kneef=(side*.38,.30,.45); footf=(side*.35,.48,.10); legs['F'+s]=(hipf,kneef,footf,footf)
        keep(seg(f'ML_F{s}_Upper',hipf,kneef,.060,hide),f'F{s}_Upper'); keep(seg(f'ML_F{s}_Lower',kneef,footf,.047,basalt),f'F{s}_Lower')
    for side in (-1,1): keep(cone(f'ML_Tusk_{"L" if side<0 else "R"}',(side*.10,.66,.58),.020,.15,tooth,(pi/2,0,side*.12)),'Jaw')
    arm,root=armature('RIG_MagmaLeaper_HOST_LEAPER'); pel=bone(arm,'Pelvis',(0,-.55,.47),(0,-.15,.61),root); chest=bone(arm,'Chest',(0,-.14,.61),(0,.23,.70),pel); head=bone(arm,'Head',(0,.20,.69),(0,.52,.66),chest); jaw=bone(arm,'Jaw',(0,.44,.60),(0,.70,.57),head)
    for tag,pts in legs.items():
        if tag.startswith('H'):
            hip,knee,hock,foot=pts; u=bone(arm,tag+'_Upper',hip,knee,pel); l=bone(arm,tag+'_Lower',knee,hock,u); h=bone(arm,tag+'_Hock',hock,foot,l); bone(arm,tag+'_Foot',foot,(foot[0],foot[1]+.18,foot[2]),h)
        else:
            hip,knee,foot,_=pts; u=bone(arm,tag+'_Upper',hip,knee,chest); bone(arm,tag+'_Lower',knee,foot,u)
    bone(arm,'AttackOrigin',(0,.55,.60),(0,.82,.60),head)
    actions=['MagmaLeaper_Idle','MagmaLeaper_Crouch','MagmaLeaper_Leap','MagmaLeaper_Land','MagmaLeaper_Bite','MagmaLeaper_Hit','MagmaLeaper_Death']; finalize(arm,parts,'underworld-creature-magma-leaper',actions,'squat volcanic ambush animal with massive folded hindlegs and exposed throat vent',14)
    action(arm,'MagmaLeaper_Idle')
    for f,v in ((1,0),(13,2),(25,0),(37,-2),(48,0)): key(arm,'Chest',f,(v,0,0)); key(arm,'Jaw',f,(max(0,v)*.5,0,0))
    action(arm,'MagmaLeaper_Crouch')
    for f,w in ((1,0),(8,.65),(16,1),(26,1),(34,0)):
        for s in ('L','R'): key(arm,'H'+s+'_Upper',f,(45*w,0,0)); key(arm,'H'+s+'_Lower',f,(-58*w,0,0)); key(arm,'H'+s+'_Hock',f,(38*w,0,0))
        key(arm,'Pelvis',f,(-18*w,0,0),loc=(0,0,-.12*w))
    action(arm,'MagmaLeaper_Leap')
    for f,w in ((1,0),(5,.55),(9,1),(15,.25),(22,0)):
        for s in ('L','R'): key(arm,'H'+s+'_Upper',f,(-42*w,0,0)); key(arm,'H'+s+'_Lower',f,(48*w,0,0)); key(arm,'H'+s+'_Hock',f,(-28*w,0,0))
        key(arm,'Chest',f,(18*w,0,0))
    action(arm,'MagmaLeaper_Land')
    for f,w in ((1,0),(5,1),(12,.45),(20,0)):
        for s in ('L','R'): key(arm,'H'+s+'_Upper',f,(38*w,0,0)); key(arm,'F'+s+'_Upper',f,(26*w,0,0))
        key(arm,'Chest',f,(-16*w,0,0))
    action(arm,'MagmaLeaper_Bite')
    for f,j,n in ((1,0,0),(5,34,-10),(9,-8,18),(14,6,6),(18,0,0)): key(arm,'Jaw',f,(j,0,0)); key(arm,'Head',f,(n,0,0))
    action(arm,'MagmaLeaper_Hit')
    for f,r in ((1,0),(5,18),(10,-6),(16,0)): key(arm,'Chest',f,(0,0,r))
    action(arm,'MagmaLeaper_Death')
    for f,r,d in ((1,0,0),(14,18,-.05),(30,58,-.22),(48,88,-.38)): key(arm,'Pelvis',f,(0,0,r),loc=(0,0,d)); key(arm,'Chest',f,(14 if r else 0,0,r*.68))
    arm.animation_data.action=None; save('underworld-creature-magma-leaper')

def build_furnace_golem():
    clear_scene(); parts={}
    iron=mat('FurnaceGolem_Iron',(.18,.17,.15),.74,.32); basalt=mat('FurnaceGolem_Basalt',(.16,.15,.13),.94); furnace=mat('FurnaceGolem_Furnace',(.50,.18,.06),.46,.06); slag=mat('FurnaceGolem_Slag',(.30,.23,.15),.82)
    def keep(o,b): parts[o.name]=b; return o
    keep(box('FG_Pelvis',(0,0,.76),(.42,.31,.30),basalt),'Pelvis'); keep(box('FG_Torso',(0,.02,1.30),(.54,.37,.55),iron),'Torso'); keep(box('FG_Head',(0,.08,1.86),(.32,.28,.28),iron),'Head')
    keep(box('FG_FurnaceMouth',(0,.40,1.28),(.24,.055,.24),furnace),'Torso')
    for i,x in enumerate((-.27,-.09,.09,.27),1): keep(box(f'FG_Grate_{i}',(x,.465,1.28),(.025,.035,.22),slag),'Torso')
    for side in (-1,1):
        s='L' if side<0 else 'R'
        keep(seg(f'FG_VentStack_{s}',(side*.25,-.18,1.67),(side*.25,-.22,2.13),.075,iron), 'Torso')
        keep(cone(f'FG_VentCap_{s}',(side*.25,-.22,2.18),.11,.18,slag,(0,0,0)),'Torso')
    arms={}
    for side in (-1,1):
        s='L' if side<0 else 'R'; shoulder=(side*.50,.02,1.52); elbow=(side*.74,.06,1.15); hand=(side*.76,.28,.80); arms[s]=(shoulder,elbow,hand)
        keep(seg(f'FG_Upper_{s}',shoulder,elbow,.16,iron),f'Upper_{s}'); keep(seg(f'FG_Lower_{s}',elbow,hand,.15,basalt),f'Lower_{s}'); keep(box(f'FG_Fist_{s}',hand,(.23,.22,.20),iron),f'Lower_{s}')
        for i in range(3): keep(cone(f'FG_FistSpike_{s}_{i+1}',(hand[0]+(i-1)*.06,hand[1]+.18,hand[2]),.025,.13,slag,(pi/2,0,0)),f'Lower_{s}')
    legs={}
    for side in (-1,1):
        s='L' if side<0 else 'R'; hip=(side*.28,-.02,.75); knee=(side*.31,.04,.39); foot=(side*.31,.28,.09); legs[s]=(hip,knee,foot)
        keep(seg(f'FG_UpperLeg_{s}',hip,knee,.18,iron),f'UpperLeg_{s}'); keep(seg(f'FG_LowerLeg_{s}',knee,foot,.16,basalt),f'LowerLeg_{s}'); keep(box(f'FG_Foot_{s}',(side*.31,.34,.075),(.20,.30,.10),iron),f'LowerLeg_{s}')
    for i,(x,y,z) in enumerate(((-.36,.02,1.48),(.36,.02,1.48),(-.28,-.10,1.07),(.28,-.10,1.07)),1): keep(box(f'FG_ArmorPlate_{i}',(x,y,z),(.15,.11,.26),iron),'Torso' if i<3 else 'Pelvis')
    arm,root=armature('RIG_FurnaceGolem_HOST_CONSTRUCT'); pel=bone(arm,'Pelvis',(0,-.05,.52),(0,.04,.92),root); torso=bone(arm,'Torso',(0,.02,.90),(0,.08,1.66),pel); head=bone(arm,'Head',(0,.08,1.62),(0,.10,2.04),torso)
    for s,(shoulder,elbow,hand) in arms.items(): u=bone(arm,f'Upper_{s}',shoulder,elbow,torso); bone(arm,f'Lower_{s}',elbow,hand,u)
    for s,(hip,knee,foot) in legs.items(): u=bone(arm,f'UpperLeg_{s}',hip,knee,pel); bone(arm,f'LowerLeg_{s}',knee,foot,u)
    bone(arm,'FurnaceCore',(0,.18,1.28),(0,.46,1.28),torso); bone(arm,'AttackOrigin',(0,.15,1.23),(0,.48,1.23),torso)
    actions=['FurnaceGolem_Idle','FurnaceGolem_Walk','FurnaceGolem_Slam','FurnaceGolem_Swipe','FurnaceGolem_Vent','FurnaceGolem_Guard','FurnaceGolem_Hit','FurnaceGolem_Death']; finalize(arm,parts,'underworld-creature-furnace-golem',actions,'industrial basalt/iron construct with physical furnace grate and twin exhaust stacks',16)
    action(arm,'FurnaceGolem_Idle')
    for f,w in ((1,0),(13,.5),(25,1),(37,.5),(49,0)): key(arm,'Torso',f,scale=(1+w*.012,1+w*.012,1+w*.018))
    action(arm,'FurnaceGolem_Walk')
    for f in range(1,33,4):
        ph=2*pi*(f-1)/32; s=sin(ph); key(arm,'UpperLeg_L',f,(22*s,0,0)); key(arm,'UpperLeg_R',f,(-22*s,0,0)); key(arm,'Upper_L',f,(-12*s,0,0)); key(arm,'Upper_R',f,(12*s,0,0))
    action(arm,'FurnaceGolem_Slam')
    for f,w in ((1,0),(8,1),(15,-.72),(23,0)): key(arm,'Upper_L',f,(55*w,0,0)); key(arm,'Upper_R',f,(55*w,0,0)); key(arm,'Torso',f,(-18*w,0,0))
    action(arm,'FurnaceGolem_Swipe')
    for f,w in ((1,0),(7,1),(13,-.58),(20,0)): key(arm,'Upper_R',f,(0,0,58*w)); key(arm,'Torso',f,(0,0,-22*w))
    action(arm,'FurnaceGolem_Vent')
    for f,w in ((1,0),(10,.55),(22,1),(34,.55),(44,0)): key(arm,'Torso',f,scale=(1+w*.035,1+w*.035,1+w*.055)); key(arm,'Head',f,(-8*w,0,0))
    action(arm,'FurnaceGolem_Guard')
    for f,w in ((1,0),(8,1),(25,1),(34,0)): key(arm,'Upper_L',f,(32*w,0,-28*w)); key(arm,'Upper_R',f,(32*w,0,28*w)); key(arm,'Torso',f,(-8*w,0,0))
    action(arm,'FurnaceGolem_Hit')
    for f,r in ((1,0),(5,12),(10,-5),(16,0)): key(arm,'Torso',f,(0,0,r))
    action(arm,'FurnaceGolem_Death')
    for f,r,d in ((1,0,0),(16,18,-.04),(34,58,-.23),(56,88,-.45)): key(arm,'Pelvis',f,(0,0,r),loc=(0,0,d)); key(arm,'Torso',f,(18 if r else 0,0,r*.68))
    arm.animation_data.action=None; save('underworld-creature-furnace-golem')

build_fume_wraith(); build_magma_leaper(); build_furnace_golem()
ids=['underworld-creature-fume-wraith','underworld-creature-magma-leaper','underworld-creature-furnace-golem']
missing=[model_id for model_id in ids if not (OUT/(model_id+'.blend')).is_file()]
if missing: raise RuntimeError('Sulfur creature outputs missing: '+', '.join(missing))
print('AUTHORED Sulfurous Wastes remainder: '+', '.join(ids),flush=True)
