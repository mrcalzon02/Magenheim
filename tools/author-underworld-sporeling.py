#!/usr/bin/env python3
"""Author the Fungal Forest Sporeling production source model and HOST-SWARM-HEXAPOD rig."""
from pathlib import Path
from math import pi, sin, cos
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'underworld-creature-sporeling.blend'
TEX=ROOT/'assets/textures/underworld/creatures/sporeling'

def image(stem,kind):
    p=TEX/f'{stem}-{kind}.png'
    if not p.exists(): raise RuntimeError(f'Missing texture {p}; run generate-underworld-sporeling-textures.py first')
    im=bpy.data.images.load(str(p),check_existing=True)
    if kind in ('normal','roughness','emission'): im.colorspace_settings.name='Non-Color'
    return im

def material(name,stem,rough_default,emission=False):
    m=bpy.data.materials.new(name); m.use_nodes=True; nt=m.node_tree; nt.nodes.clear()
    out=nt.nodes.new('ShaderNodeOutputMaterial'); bs=nt.nodes.new('ShaderNodeBsdfPrincipled'); nt.links.new(bs.outputs['BSDF'],out.inputs['Surface'])
    uv=nt.nodes.new('ShaderNodeTexCoord')
    alb=nt.nodes.new('ShaderNodeTexImage'); alb.image=image(stem,'albedo'); nt.links.new(uv.outputs['UV'],alb.inputs['Vector']); nt.links.new(alb.outputs['Color'],bs.inputs['Base Color'])
    rough=nt.nodes.new('ShaderNodeTexImage'); rough.image=image(stem,'roughness'); nt.links.new(uv.outputs['UV'],rough.inputs['Vector']); nt.links.new(rough.outputs['Color'],bs.inputs['Roughness']); bs.inputs['Roughness'].default_value=rough_default
    normal=nt.nodes.new('ShaderNodeTexImage'); normal.image=image(stem,'normal'); nm=nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(uv.outputs['UV'],normal.inputs['Vector']); nt.links.new(normal.outputs['Color'],nm.inputs['Color']); nt.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
    if emission:
        em=nt.nodes.new('ShaderNodeTexImage'); em.image=image(stem,'emission'); nt.links.new(uv.outputs['UV'],em.inputs['Vector'])
        if 'Emission Color' in bs.inputs: nt.links.new(alb.outputs['Color'],bs.inputs['Emission Color']); nt.links.new(em.outputs['Color'],bs.inputs['Emission Strength'])
    return m

def bevel(o,w=.003):
    b=o.modifiers.new('SporelingEdge','BEVEL'); b.width=w; b.segments=2; b.limit_method='ANGLE'

def organic(name,loc,scale,mat,sub=2,seed=0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for v in o.data.vertices:
        k=1+.045*sin(v.co.x*31+seed)+.035*cos(v.co.y*27-seed)+.025*sin(v.co.z*37+seed*.4); v.co*=k
    o.data.materials.append(mat); bevel(o); return o

def segment(name,a,b,r,mat):
    mid=tuple((a[i]+b[i])/2 for i in range(3)); d=Vector(tuple(b[i]-a[i] for i in range(3))); bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=d.length,location=mid)
    o=bpy.context.object; o.name=name; o.data.materials.append(mat); o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; bevel(o,r*.18); return o

def cone(name,loc,r,depth,mat,rot=(0,0,0),verts=16):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r,radius2=r*.18,depth=depth,location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.data.materials.append(mat); bevel(o,r*.12); return o

def authored_uv(o):
    """Deterministic object-space triplanar-style unwrap written into a real UV layer."""
    uv=o.data.uv_layers.get('SporelingUV') or o.data.uv_layers.new(name='SporelingUV')
    for poly in o.data.polygons:
        n=poly.normal
        axis=max(range(3),key=lambda i:abs(n[i]))
        for li in poly.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co
            if axis==0: u,v=co.y,co.z
            elif axis==1: u,v=co.x,co.z
            else: u,v=co.x,co.y
            uv.data[li].uv=((u*2.75)%1.0,(v*2.75)%1.0)
    o.data.uv_layers.active=uv

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
flesh=material('SporelingFlesh','flesh',.72); chitin=material('SporelingCapChitin','cap-chitin',.64); gill=material('SporelingGill','gill',.52,True); sac=material('SporelingSporeSac','spore-sac',.58,True); dark=material('SporelingJoint','joint',.90)
parts={}
def keep(o,bone): parts[o.name]=bone; return o
keep(organic('Sporeling_Thorax',(0,0,.245),(.115,.145,.105),flesh,3,1),'Thorax'); keep(organic('Sporeling_Abdomen',(0,-.145,.255),(.135,.165,.125),sac,3,2),'Abdomen'); keep(organic('Sporeling_Head',(0,.145,.245),(.105,.095,.085),flesh,3,3),'Head')
bpy.ops.mesh.primitive_uv_sphere_add(segments=28,ring_count=16,radius=1,location=(0,-.045,.345)); cap=bpy.context.object; cap.name='Sporeling_CapArmor'; cap.scale=(.185,.205,.045); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); cap.data.materials.append(chitin); keep(cap,'Thorax')
for v in cap.data.vertices:
    a=__import__('math').atan2(v.co.y,v.co.x); k=1+.055*sin(a*7)+.022*sin(a*13); v.co.x*=k; v.co.y*=k
bpy.ops.mesh.primitive_torus_add(major_radius=.142,minor_radius=.012,major_segments=28,minor_segments=8,location=(0,-.04,.314)); lip=bpy.context.object; lip.name='Sporeling_GillRim'; lip.scale.y=1.12; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); lip.data.materials.append(gill); keep(lip,'Thorax')
leg_points={}
for pair,y in enumerate((.105,0,-.105),1):
    for side in (-1,1):
        s='L' if side<0 else 'R'; hip=(side*.082,y,.245); knee=(side*(.155+.012*pair),y+(.025 if pair==1 else -.012*pair),.145); ankle=(side*(.205+.008*pair),y-.018,.045); toe=(side*(.225+.006*pair),y+.035,.012); leg_points[(s,pair)]=(hip,knee,ankle,toe)
        keep(organic(f'Sporeling_{s}_Coxa{pair}',hip,(.031,.032,.030),dark,2,10+pair),f'{s}_Coxa{pair}'); keep(segment(f'Sporeling_{s}_Femur{pair}',hip,knee,.022,flesh),f'{s}_Coxa{pair}'); keep(segment(f'Sporeling_{s}_Tibia{pair}',knee,ankle,.017,dark),f'{s}_Femur{pair}'); keep(segment(f'Sporeling_{s}_Tarsus{pair}',ankle,toe,.010,chitin),f'{s}_Tarsus{pair}')
for side in (-1,1):
    s='L' if side<0 else 'R'; keep(segment(f'Sporeling_Mandible_{s}',(side*.045,.205,.238),(side*.075,.265,.205),.018,chitin),'Jaw'); keep(segment(f'Sporeling_Frond_{s}',(side*.035,.185,.295),(side*.075,.265,.350),.009,gill),f'Frond_{s}'); keep(cone(f'Sporeling_FrondTip_{s}',(side*.082,.278,.36),.013,.045,gill,(pi/2,0,0),10),f'Frond_{s}')
for i in range(6):
    a=2*pi*i/6; keep(cone(f'Sporeling_SporeVent_{i+1}',(cos(a)*.095,-.285,.255+sin(a)*.075),.016,.052,sac,(pi/2,0,0),10),'Abdomen')
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']: authored_uv(o)

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_Sporeling_HOST_SWARM_HEXAPOD'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.12)
def bone(name,head,tail,parent=None): b=eb.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
thor=bone('Thorax',(0,0,.18),(0,0,.31),root); abd=bone('Abdomen',(0,-.02,.25),(0,-.23,.255),thor); head=bone('Head',(0,.05,.245),(0,.19,.245),thor); bone('Jaw',(0,.15,.22),(0,.26,.205),head)
for (s,pair),(hip,knee,ankle,toe) in leg_points.items(): c=bone(f'{s}_Coxa{pair}',hip,knee,thor); f=bone(f'{s}_Femur{pair}',knee,ankle,c); bone(f'{s}_Tarsus{pair}',ankle,toe,f)
for side in ('L','R'): sign=-1 if side=='L' else 1; bone(f'Frond_{side}',(sign*.035,.18,.29),(sign*.082,.29,.36),head)
bone('AttackOrigin',(0,.20,.22),(0,.31,.22),head); bone('SporeFX',(0,-.18,.28),(0,-.32,.28),abd); bone('HitCenter',(0,0,.20),(0,0,.30),root); bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True

def action_fcurves(act):
    """Blender 4.4 moved an action's F-Curves into slotted channelbags; 5.0 removed Action.fcurves.

    Reading the legacy property raised AttributeError on Blender 5.0.0, so every run of this author
    since the animation actions were added had failed and the committed .blend went stale behind two
    author commits without anything noticing. Prefer the legacy collection when it exists so the
    script still runs on older Blender.
    """
    legacy=getattr(act,'fcurves',None)
    if legacy is not None: return list(legacy)
    curves=[]
    for layer in act.layers:
        for strip in layer.strips:
            for bag in getattr(strip,'channelbags',()): curves.extend(bag.fcurves)
    return curves

def action(name,frames,poses):
    act=bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action=act
    for frame,bone_poses in poses.items():
        for bname,rotation in bone_poses.items():
            pb=arm.pose.bones.get(bname)
            if not pb: raise RuntimeError(f'{name}: missing animation bone {bname}')
            pb.rotation_mode='XYZ'; pb.rotation_euler=rotation; pb.keyframe_insert('rotation_euler',frame=frame,group=bname)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points: kp.interpolation='BEZIER'
    # Each action loses its only user when the next one is assigned to the rig, and the last loses
    # it when the rig is cleared below. Blender does not write zero-user datablocks, so without a
    # fake user all ten actions were authored and then dropped on save.
    act.use_fake_user=True
    act.frame_start=frames[0]; act.frame_end=frames[1]; return act

def gait_pose(amount):
    p={}
    for s,pair,phase in (('L',1,1),('R',1,-1),('L',2,-1),('R',2,1),('L',3,1),('R',3,-1)):
        p[f'{s}_Coxa{pair}']=(0,0,phase*amount); p[f'{s}_Femur{pair}']=(phase*amount*.45,0,0); p[f'{s}_Tarsus{pair}']=(-phase*amount*.25,0,0)
    return p
neutral={b:(0,0,0) for b in ('Thorax','Abdomen','Head','Jaw','Frond_L','Frond_R')}
action('Sporeling_Idle',(1,72),{1:neutral,18:{'Abdomen':(.025,0,0),'Frond_L':(.05,0,.03),'Frond_R':(.05,0,-.03)},36:neutral,54:{'Abdomen':(-.018,0,0),'Frond_L':(-.035,0,-.02),'Frond_R':(-.035,0,.02)},72:neutral})
action('Sporeling_Walk',(1,32),{1:gait_pose(.18),9:gait_pose(-.18),17:gait_pose(.18),25:gait_pose(-.18),32:gait_pose(.18)})
action('Sporeling_Scuttle',(1,24),{1:gait_pose(.28),7:gait_pose(-.28),13:gait_pose(.28),19:gait_pose(-.28),24:gait_pose(.28)})
action('Sporeling_Turn',(1,24),{1:gait_pose(.12),8:{'Thorax':(0,0,.16),'Head':(0,0,.10)},16:{'Thorax':(0,0,-.10),'Head':(0,0,-.06)},24:gait_pose(.12)})
action('Sporeling_Alert',(1,30),{1:neutral,12:{'Head':(-.18,0,0),'Frond_L':(-.28,0,.16),'Frond_R':(-.28,0,-.16),'Abdomen':(.06,0,0)},30:neutral})
action('Sporeling_Bite',(1,22),{1:neutral,7:{'Head':(-.16,0,0),'Jaw':(.42,0,0)},12:{'Head':(.12,0,0),'Jaw':(-.30,0,0)},16:{'Jaw':(.10,0,0)},22:neutral})
action('Sporeling_Hit',(1,16),{1:neutral,5:{'Thorax':(.12,0,.12),'Head':(.18,0,.08)},16:neutral})
action('Sporeling_Stagger',(1,28),{1:neutral,8:{'Thorax':(.22,0,.22),'Abdomen':(-.16,0,-.10),'Head':(.28,0,.12)},18:{'Thorax':(-.08,0,-.08)},28:neutral})
action('Sporeling_DeathSporePuff',(1,52),{1:neutral,16:{'Abdomen':(-.20,0,0),'Head':(.15,0,0)},27:{'Abdomen':(.32,0,0),'Thorax':(.10,0,.10),'Frond_L':(.35,0,.30),'Frond_R':(.35,0,-.30)},38:{'Thorax':(0,0,.65),'Head':(.45,0,.20)},52:{'Thorax':(0,0,1.25),'Head':(.70,0,.35),'Abdomen':(-.30,0,.25)}})
action('Sporeling_Emerge',(1,42),{1:{'Thorax':(.30,0,0),'Head':(.22,0,0),'Abdomen':(-.15,0,0)},18:{'Thorax':(-.08,0,0),'Head':(-.12,0,0)},30:{'Thorax':(.04,0,0)},42:neutral})
arm.animation_data.action=None
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('SporelingArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<40: raise RuntimeError(f'Sporeling detail regression: {len(meshes)} mesh parts')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-sporeling'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='common'; sc['magenheim_host_rig']='HOST-SWARM-HEXAPOD'; sc['magenheim_length_m']=.50; sc['magenheim_height_m']=.40; sc['magenheim_texture_source_target']=1024; sc['magenheim_fidelity']='production-creature-r4'; sc['magenheim_skinning']='rigid-segment-weighted'; sc['magenheim_uv']='authored-object-projection'; sc['magenheim_authored_actions']='Sporeling_Idle,Sporeling_Walk,Sporeling_Scuttle,Sporeling_Turn,Sporeling_Alert,Sporeling_Bite,Sporeling_Hit,Sporeling_Stagger,Sporeling_DeathSporePuff,Sporeling_Emerge'; sc['magenheim_animation_manifest']='idle,walk,scuttle,turn,alert,bite,hit,stagger,death-spore-puff,emerge'; sc['magenheim_socket_manifest']='AttackOrigin,SporeFX,HitCenter'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True); print(f'AUTHORED Sporeling r4: {len(meshes)} mesh parts / {len(arm.data.bones)} bones / UV-textured rigid skin / 10 actions -> {OUT.name}',flush=True)
