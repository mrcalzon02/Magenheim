#!/usr/bin/env python3
"""Author the Sulfurous Wastes Vent Spitter production source model.

The Spitter is a front-heavy geothermal pressure animal, not a Cinder Hound variant.
Its ranged tell is physical anatomy: a deformable throat reservoir and articulated mouth.
World motion and projectile gameplay remain Valheim-owned.
"""
from pathlib import Path
from math import pi
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-vent-spitter.blend'
OUT.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough=.7):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF'); b.inputs['Base Color'].default_value=(*color,1); b.inputs['Roughness'].default_value=rough
    return m
hide=mat('VentSpitter_HeatHide',(.095,.070,.052),.78)
plate=mat('VentSpitter_MineralPlate',(.075,.068,.060),.91)
sacmat=mat('VentSpitter_PressureSac',(.30,.105,.050),.38)
throatmat=mat('VentSpitter_Throat',(.23,.075,.040),.44)
mouthmat=mat('VentSpitter_Mouth',(.16,.040,.030),.46)
tooth=mat('VentSpitter_Tooth',(.56,.47,.31),.66)
eye=mat('VentSpitter_Eye',(.11,.045,.020),.27)
parts={}
def organic(n,loc,scale,ma,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=n; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(ma); return o
def seg(n,a,b,r,ma,v=12):
    d=Vector(b)-Vector(a); bpy.ops.mesh.primitive_cylinder_add(vertices=v,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2); o=bpy.context.object; o.name=n; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(ma); return o
def cone(n,loc,r,d,ma,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r,radius2=.002,depth=d,location=loc,rotation=rot); o=bpy.context.object; o.name=n; o.data.materials.append(ma); return o
def keep(o,b): parts[o.name]=b; return o
def uv(o):
    u=o.data.uv_layers.new(name='VentSpitterUV')
    for p in o.data.polygons:
        ax=max(range(3),key=lambda i:abs(p.normal[i]))
        for li in p.loop_indices:
            c=o.data.vertices[o.data.loops[li].vertex_index].co; q=((c.y,c.z),(c.x,c.z),(c.x,c.y))[ax]; u.data[li].uv=((q[0]*1.55)%1,(q[1]*1.55)%1)

# 1.50m production length. Squat, front-heavy silhouette distinguishes it from the Cinder Hound.
keep(organic('VS_Pelvis',(0,-.55,.66),(.38,.38,.32),hide,3),'Pelvis')
keep(organic('VS_Belly',(0,-.12,.65),(.43,.50,.37),hide,3),'Spine')
keep(organic('VS_Chest',(0,.34,.76),(.48,.45,.45),hide,3),'Chest')
keep(organic('VS_Neck',(0,.63,.82),(.34,.30,.32),hide,3),'Neck')
keep(organic('VS_Head',(0,.86,.87),(.35,.34,.29),hide,3),'Head')
keep(organic('VS_Muzzle',(0,1.10,.82),(.27,.30,.19),hide,3),'Head')
keep(organic('VS_LowerJaw',(0,1.10,.73),(.25,.29,.095),mouthmat,2),'Jaw')

# Three-lobed reservoir is exposed beneath chest/throat. All lobes deform with Sac control.
for i,(x,y,z,sx) in enumerate(((-.17,.47,.55,.18),(0,.55,.51,.23),(.17,.47,.55,.18)),1):
    keep(organic(f'VS_PressureSac_{i}',(x,y,z),(sx,.24,.19),sacmat,3),'Sac')
# Ringed throat physically connects reservoir to mouth and provides readable compression/inflation.
for i,y in enumerate((.63,.72,.81,.90),1):
    keep(organic(f'VS_ThroatRing_{i}',(0,y,.67+i*.018),(.24-i*.018,.105,.085),throatmat,2),'Throat')
keep(seg('VS_ThroatPipe',(0,.58,.57),(0,.96,.73),.105,throatmat,12),'Throat')

# Dorsal mineral baffles shield the pressure body without turning it into another plated crawler.
for i,(y,z,w) in enumerate(((-.48,.91,.25),(-.24,.98,.28),(.02,1.03,.30),(.27,1.08,.27),(.49,1.07,.22)),1):
    keep(organic(f'VS_DorsalBaffle_{i}',(0,y,z),(w,.14,.055),plate,2),'Chest' if i>3 else ('Spine' if i>1 else 'Pelvis'))
# Paired lateral relief vents make the pressure system readable from either gameplay-camera side.
for side in (-1,1):
    s='L' if side<0 else 'R'
    for i,y in enumerate((.20,.37,.52),1):
        keep(seg(f'VS_ReliefVent_{s}_{i}',(side*.31,y,.78),(side*.45,y,.84),.038,plate,10),'Chest')
        keep(organic(f'VS_ReliefMouth_{s}_{i}',(side*.46,y,.85),(.060,.070,.040),throatmat,2),'Chest')
    keep(organic(f'VS_Eye_{s}',(side*.23,1.03,.93),(.040,.028,.034),eye,2),'Head')

# Four splayed load-bearing limbs: heavy forequarters brace recoil; hind legs drive repositioning.
legs={}
for side in (-1,1):
    for fore,y in ((True,.32),(False,-.48)):
        s='L' if side<0 else 'R'; tag=('F' if fore else 'H')+s
        hip=(side*(.38 if fore else .30),y,.70); knee=(side*(.58 if fore else .48),y+(.12 if fore else -.05),.42); hock=(side*.54,y+(.24 if fore else .12),.18); foot=(side*.52,y+(.38 if fore else .29),.07)
        legs[tag]=(hip,knee,hock,foot)
        keep(organic(f'VS_{tag}_Joint',hip,(.09,.09,.075),throatmat,2),f'{tag}_Upper')
        keep(seg(f'VS_{tag}_Upper',hip,knee,.085 if fore else .073,hide),f'{tag}_Upper')
        keep(seg(f'VS_{tag}_Lower',knee,hock,.067 if fore else .058,hide),f'{tag}_Lower')
        keep(seg(f'VS_{tag}_Hock',hock,foot,.050,hide),f'{tag}_Hock')
        keep(organic(f'VS_{tag}_Foot',foot,(.12,.19,.06),plate,2),f'{tag}_Foot')
        for d in (-1,0,1): keep(cone(f'VS_{tag}_Claw_{d+2}',(foot[0]+d*.045,foot[1]+.16,.045),.017,.095,tooth,(pi/2,0,0)),f'{tag}_Foot')

# Broad pressure mouth: modeled teeth plus paired lip vanes around a clear projectile origin.
for row,z in (('U',.84),('L',.745)):
    for i in range(8):
        x=(i-3.5)*.052; keep(cone(f'VS_Tooth_{row}_{i+1}',(x,1.30,z),.015,.078,tooth,(pi/2,0,0)),'Head' if row=='U' else 'Jaw')
for side in (-1,1): keep(organic(f'VS_LipVane_{"L" if side<0 else "R"}',(side*.24,1.18,.80),(.055,.16,.12),throatmat,2),'Jaw')
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']: uv(o)

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_VentSpitter_HOST_QUADRUPED_EXT'; eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,0); root.tail=(0,0,.18)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
pel=bone('Pelvis',(0,-.58,.57),(0,-.39,.68),root); spine=bone('Spine',(0,-.39,.68),(0,.08,.72),pel); chest=bone('Chest',(0,.08,.72),(0,.44,.79),spine); neck=bone('Neck',(0,.44,.79),(0,.70,.85),chest); head=bone('Head',(0,.70,.85),(0,1.02,.85),neck); jaw=bone('Jaw',(0,.93,.77),(0,1.31,.73),head)
sac=bone('Sac',(0,.37,.52),(0,.61,.54),chest); throat=bone('Throat',(0,.58,.61),(0,.96,.72),sac)
for tag,(hip,knee,hock,foot) in legs.items():
    parent=chest if tag.startswith('F') else pel; u=bone(f'{tag}_Upper',hip,knee,parent); l=bone(f'{tag}_Lower',knee,hock,u); h=bone(f'{tag}_Hock',hock,foot,l); bone(f'{tag}_Foot',foot,(foot[0],foot[1]+.18,foot[2]),h)
bone('AttackOrigin',(0,1.12,.80),(0,1.46,.80),head); bone('PressureFX',(0,.50,.53),(0,.50,.78),sac); bone('MouthFX',(0,1.17,.80),(0,1.43,.80),head); bone('HitCenter',(0,.02,.70),(0,.02,.98),spine)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']:
    b=parts.get(o.name,'Chest'); mod=o.modifiers.new('VentSpitterArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=b); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')
meshes=[x for x in bpy.context.scene.objects if x.type=='MESH']; polys=sum(len(o.data.polygons) for o in meshes)
required=['Root','Pelvis','Spine','Chest','Neck','Head','Jaw','Sac','Throat','AttackOrigin','PressureFX','MouthFX','HitCenter']+[f'{tag}_{q}' for tag in ('FL','FR','HL','HR') for q in ('Upper','Lower','Hock','Foot')]
missing=[n for n in required if n not in arm.data.bones]
if missing: raise RuntimeError(f'Missing Vent Spitter bones: {missing}')
if len(meshes)<65: raise RuntimeError(f'Vent Spitter anatomical breakup too low: {len(meshes)} meshes < 65')
if polys<10000: raise RuntimeError(f'Vent Spitter source fidelity too low: {polys} polygons < 10000')
if sum(1 for o in meshes if o.name.startswith('VS_PressureSac_'))!=3: raise RuntimeError('Expected three physical pressure-sac lobes')
if sum(1 for o in meshes if o.name.startswith('VS_ReliefMouth_'))!=6: raise RuntimeError('Expected six physical relief vents')
if sum(1 for o in meshes if o.name.startswith('VS_Tooth_'))!=16: raise RuntimeError('Expected sixteen modeled teeth')
if any(not o.data.uv_layers.get('VentSpitterUV') for o in meshes): raise RuntimeError('Vent Spitter mesh missing VentSpitterUV')
arm['host_family']='HOST-QUADRUPED extension'; arm['production_length_target_m']=1.50
arm['material_language']='heat-cured hide / dry mineral baffles / wet pressure sac and throat / mineral teeth'
arm['readability_contract']='squat front-heavy geothermal animal; exposed three-lobed pressure reservoir; ringed throat; broad pressure mouth; heavy recoil-bracing forelegs'
arm['planned_actions']='Idle,PressureIdle,Walk,Scuttle,TurnLeft,TurnRight,Charge,Spit,Recoil,Bite,Hit,Stagger,Death'
arm['charge_contract']='Charge must visibly inflate Sac and Throat before Spit; ranged tell cannot be particle-only'
arm['root_motion_policy']='Root translation reserved for Valheim runtime; authored actions remain in-place'
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Authored {OUT}: {len(meshes)} meshes, {polys} polygons, 3 pressure lobes, 6 relief vents, 16 teeth')
