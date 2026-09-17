#!/usr/bin/env python3
"""Author the Fungal Forest Sporeling production source model and HOST-SWARM-HEXAPOD rig.

This is the first Underworld creature benchmark: a 0.46m six-legged fungal scavenger with
an articulated mandible, deformable spore abdomen, cap armor, sensory fronds, explicit
animation/VFX sockets, authored armature hierarchy and material-separated anatomy.
"""
from pathlib import Path
from math import pi, sin, cos
import bpy

ROOT=Path(__file__).resolve().parents[1]
OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'underworld-creature-sporeling.blend'

def material(name,color,rough,emission=None):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    if emission and 'Emission Color' in bs.inputs:
        bs.inputs['Emission Color'].default_value=(*emission,1); bs.inputs['Emission Strength'].default_value=1.35
    return m

def bevel(o,w=.003):
    b=o.modifiers.new('SporelingEdge','BEVEL'); b.width=w; b.segments=2; b.limit_method='ANGLE'

def organic(name,loc,scale,mat,sub=2,seed=0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name
    o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for v in o.data.vertices:
        k=1+.045*sin(v.co.x*31+seed)+.035*cos(v.co.y*27-seed)+.025*sin(v.co.z*37+seed*.4); v.co*=k
    o.data.materials.append(mat); bevel(o); return o

def segment(name,a,b,r,mat):
    mid=tuple((a[i]+b[i])/2 for i in range(3)); dx,dy,dz=(b[i]-a[i] for i in range(3)); length=(dx*dx+dy*dy+dz*dz)**.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=r,depth=length,location=mid)
    o=bpy.context.object; o.name=name; o.data.materials.append(mat)
    # align local Z to segment direction
    from mathutils import Vector
    o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(Vector((dx,dy,dz)).normalized()); o.rotation_mode='XYZ'
    bevel(o,r*.18); return o

def cone(name,loc,r,depth,mat,rot=(0,0,0),verts=12):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r,radius2=r*.18,depth=depth,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.data.materials.append(mat); bevel(o,r*.12); return o

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
flesh=material('SporelingFlesh',(.29,.22,.32),.72)
chitin=material('SporelingCapChitin',(.18,.30,.31),.64)
gill=material('SporelingGill',(.25,.46,.43),.52,(.28,.82,.72))
sac=material('SporelingSporeSac',(.39,.30,.43),.58,(.52,.38,.67))
dark=material('SporelingJoint',(.075,.07,.085),.90)

# Body axis is +Y; ground plane z=0.
organic('Sporeling_Thorax',(0,0,.245),(.115,.145,.105),flesh,2,1)
organic('Sporeling_Abdomen',(0,-.145,.255),(.135,.165,.125),sac,2,2)
organic('Sporeling_Head',(0,.145,.245),(.105,.095,.085),flesh,2,3)

# Layered cap armor and underside gill lip are real silhouette geometry.
bpy.ops.mesh.primitive_uv_sphere_add(segments=28,ring_count=10,radius=1,location=(0,-.045,.345))
cap=bpy.context.object; cap.name='Sporeling_CapArmor'; cap.scale=(.185,.205,.045); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); cap.data.materials.append(chitin)
for v in cap.data.vertices:
    a=__import__('math').atan2(v.co.y,v.co.x); k=1+.055*sin(a*7)+.022*sin(a*13); v.co.x*=k; v.co.y*=k
bpy.ops.mesh.primitive_torus_add(major_radius=.142,minor_radius=.012,major_segments=28,minor_segments=6,location=(0,-.04,.314))
lip=bpy.context.object; lip.name='Sporeling_GillRim'; lip.scale.y=1.12; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); lip.data.materials.append(gill)

# Six legs: coxa -> femur -> tibia -> hooked tarsus, individually named for rig binding.
leg_points={}
for pair,y in enumerate((.105,0,-.105),1):
    for side in (-1,1):
        s='L' if side<0 else 'R'; hip=(side*.082,y,.245); knee=(side*(.155+.012*pair),y+(.025 if pair==1 else -.012*pair),.145); ankle=(side*(.205+.008*pair),y-.018,.045); toe=(side*(.225+.006*pair),y+.035,.012)
        leg_points[(s,pair)]=(hip,knee,ankle,toe)
        organic(f'Sporeling_{s}_Coxa{pair}',hip,(.031,.032,.030),dark,1,10+pair)
        segment(f'Sporeling_{s}_Femur{pair}',hip,knee,.022,flesh); segment(f'Sporeling_{s}_Tibia{pair}',knee,ankle,.017,dark); segment(f'Sporeling_{s}_Tarsus{pair}',ankle,toe,.010,chitin)

# Paired biting mandibles and sensory fronds.
for side in (-1,1):
    s='L' if side<0 else 'R'
    segment(f'Sporeling_Mandible_{s}',(side*.045,.205,.238),(side*.075,.265,.205),.018,chitin)
    segment(f'Sporeling_Frond_{s}',(side*.035,.185,.295),(side*.075,.265,.350),.009,gill)
    cone(f'Sporeling_FrondTip_{s}',(side*.082,.278,.36),.013,.045,gill,(pi/2,0,0),10)

# Spore vents around abdomen communicate the death-puff organ before it fires.
for i in range(6):
    a=2*pi*i/6; x=cos(a)*.095; z=.255+sin(a)*.075
    cone(f'Sporeling_SporeVent_{i+1}',(x,-.285,z),.016,.052,sac,(pi/2,0,0),10)

# Armature: explicit deform/host hierarchy. Mesh binding is a later weighting pass, but bone contract exists now.
bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_Sporeling_HOST_SWARM_HEXAPOD'; eb=arm.data.edit_bones
root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.12)
def bone(name,head,tail,parent=None):
    b=eb.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
thor=bone('Thorax',(0,0,.18),(0,0,.31),root); abd=bone('Abdomen',(0,-.02,.25),(0,-.23,.255),thor); head=bone('Head',(0,.05,.245),(0,.19,.245),thor)
jaw=bone('Jaw',(0,.15,.22),(0,.26,.205),head)
for (s,pair),(hip,knee,ankle,toe) in leg_points.items():
    c=bone(f'{s}_Coxa{pair}',hip,knee,thor); f=bone(f'{s}_Femur{pair}',knee,ankle,c); bone(f'{s}_Tarsus{pair}',ankle,toe,f)
for side in ('L','R'):
    sign=-1 if side=='L' else 1
    bone(f'Frond_{side}',(sign*.035,.18,.29),(sign*.082,.29,.36),head)
# Non-deforming technical bones.
bone('AttackOrigin',(0,.20,.22),(0,.31,.22),head); bone('SporeFX',(0,-.18,.28),(0,-.32,.28),abd); bone('HitCenter',(0,0,.20),(0,0,.30),root)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<40: raise RuntimeError(f'Sporeling detail regression: {len(meshes)} mesh parts')
if len(arm.data.bones)<27: raise RuntimeError(f'Sporeling rig regression: {len(arm.data.bones)} bones')
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-sporeling'; sc['magenheim_biome']='fungal-forest'; sc['magenheim_tier']='common'
sc['magenheim_host_rig']='HOST-SWARM-HEXAPOD'; sc['magenheim_length_m']=.50; sc['magenheim_height_m']=.40
sc['magenheim_texture_source_target']=1024; sc['magenheim_fidelity']='production-creature-r1'
sc['magenheim_animation_manifest']='idle,walk,scuttle,turn,alert,bite,hit,stagger,death-spore-puff,emerge'
sc['magenheim_socket_manifest']='AttackOrigin,SporeFX,HitCenter'
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True)
print(f'AUTHORED Sporeling production chassis: {len(meshes)} mesh parts / {len(arm.data.bones)} bones -> {OUT.name}',flush=True)
