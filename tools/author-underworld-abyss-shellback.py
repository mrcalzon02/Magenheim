#!/usr/bin/env python3
"""Author the Blackwater Deep Abyss Shellback production source model in Blender.

Art-only source generation. Reuses HOST-AMPHIB-ARMORED rig vocabulary while
keeping the elite Shellback anatomically distinct from Shoreclaw. Valheim owns
locomotion, swimming, AI, networking and persistence.
"""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-abyss-shellback.blend'
LENGTH=3.72
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name,color,rough):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    return m
SHELL=mat('Shellback_LaminateShell',(.075,.105,.095),.91)
EDGE=mat('Shellback_MineralEdge',(.16,.18,.15),.84)
JOINT=mat('Shellback_JointTissue',(.11,.075,.06),.54)
BELLY=mat('Shellback_VulnerableUnderside',(.24,.15,.105),.61)
CLAW=mat('Shellback_HeavyClaw',(.095,.12,.105),.79)
EYE=mat('Shellback_Eye',(.018,.022,.018),.30)
parts={}
def uv(o):
    layer=o.data.uv_layers.new(name='AbyssShellbackUV'); xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]
    xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys); dx=max(xmax-xmin,.001); dy=max(ymax-ymin,.001)
    for p in o.data.polygons:
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co; layer.data[li].uv=((co.x-xmin)/dx,(co.y-ymin)/dy)
def ico(name,loc,scale,material,bone,sub=3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o); parts[name]=bone; return o
def seg(name,a,b,r,material,bone,verts=14):
    a,b=Vector(a),Vector(b); d=b-a; bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(a+b)/2); o=bpy.context.object; o.name=name
    o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y'); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material); uv(o); parts[name]=bone; return o

# Broad deforming body and conspicuously exposed soft underside: this is the elite's vulnerability language.
ico('BodyMass',(0,-.08,.53),(1.23,1.52,.47),JOINT,'Body',4)
ico('VulnerableUnderside',(0,-.12,.18),(1.04,1.34,.24),BELLY,'Body',4)
for i,y in enumerate([1.00,.58,.12,-.38,-.83],1):
    scale=(1.05+(.10 if i in (2,3) else 0),.50,.19)
    ico(f'ShellPlate_{i}',(0,y,.82),scale,SHELL,f'ShellPlate_{i}',4)
    # Paired raised mineral rims preserve the laminated silhouette at gameplay distance.
    for s in (-1,1): ico(f'PlateRim_{i}_{s:+d}',(.86*s,y+.05,.96),(.20,.38,.095),EDGE,f'ShellPlate_{i}',2)
# Heavy dorsal keel blocks make Shellback read as a fortress, not a scaled-up Shoreclaw.
for i,y in enumerate([.86,.48,.08,-.34,-.72],1): ico(f'DorsalKeel_{i}',(0,y,1.09),(.24,.30,.16),EDGE,f'ShellPlate_{i}',2)

# Eight load-bearing amphibious legs: thicker and shorter than Shoreclaw, with broad digging/swim feet.
for side,s in [('L',-1),('R',1)]:
    for n,y in enumerate([.78,.30,-.22,-.70],1):
        pre=f'Leg_{side}_{n}'; hip=(.88*s,y,.48); knee=(1.40*s,y-.05,.25); ankle=(1.72*s,y-.12,.04); foot=(1.94*s,y-.18,.02)
        seg(pre+'_Upper',hip,knee,.145,JOINT,pre+'_Upper'); seg(pre+'_Lower',knee,ankle,.115,JOINT,pre+'_Lower')
        ico(pre+'_Paddle',foot,(.31,.20,.065),SHELL,pre+'_Foot',2)
        ico(pre+'_JointGap',hip,(.18,.17,.12),BELLY,pre+'_Upper',2)

# Paired heavy claws. Unlike Shoreclaw's crusher/cutter asymmetry, Shellback is a bilateral siege animal.
for side,s in [('L',-1),('R',1)]:
    pre=f'Claw_{side}'; base=(.68*s,1.02,.55); elbow=(1.26*s,1.34,.54); palm=(1.68*s,1.55,.55)
    seg(pre+'_Arm',base,elbow,.19,JOINT,pre+'_Arm',16); ico(pre+'_Palm',palm,(.43,.36,.25),CLAW,pre+'_Palm',3)
    for j,dy in enumerate([-.15,.15],1): seg(pre+f'_Finger_{j}',palm,(2.14*s,1.60+dy,.55),.105,CLAW,pre+'_Palm',12)
# Small recessed eyes emphasize armor mass.
for side,s in [('L',-1),('R',1)]: ico(f'Eye_{side}',(.34*s,1.28,.70),(.075,.065,.06),EYE,'Body',2)

armdata=bpy.data.armatures.new('HOST-AMPHIB-ARMORED'); arm=bpy.data.objects.new('AbyssShellbackRig',armdata); bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,-.2,.08),(0,-.2,.38)); body=bone('Body',(0,-.92,.48),(0,1.05,.50),root)
for i,y in enumerate([1.00,.58,.12,-.38,-.83],1): bone(f'ShellPlate_{i}',(0,y-.22,.78),(0,y+.22,.82),body)
for side,s in [('L',-1),('R',1)]:
    for n,y in enumerate([.78,.30,-.22,-.70],1):
        pre=f'Leg_{side}_{n}'; u=bone(pre+'_Upper',(.82*s,y,.48),(1.40*s,y-.05,.25),body); l=bone(pre+'_Lower',(1.40*s,y-.05,.25),(1.72*s,y-.12,.04),u); bone(pre+'_Foot',(1.72*s,y-.12,.04),(2.02*s,y-.18,.02),l)
    pre=f'Claw_{side}'; a=bone(pre+'_Arm',(.62*s,.98,.55),(1.26*s,1.34,.54),body); bone(pre+'_Palm',(1.26*s,1.34,.54),(1.86*s,1.58,.55),a)
bone('AttackOrigin',(0,1.18,.48),(0,1.80,.48),body); bone('HitCenter',(0,-.08,.42),(0,.36,.42),root)
bone('SwimCenter',(0,-.15,.14),(0,.35,.14),root); bone('ShellFX',(0,.02,.92),(0,.02,1.30),body); bone('UndersideFX',(0,.05,.06),(0,.05,.30),body)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('ShellbackArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if not 3.0<=LENGTH<=4.5: raise RuntimeError('Abyss Shellback length outside production band')
if len(meshes)<55: raise RuntimeError(f'Shellback anatomy regression: {len(meshes)} mesh parts')
if tris<18000: raise RuntimeError(f'Shellback source triangle floor regression: {tris}')
if sum(1 for o in meshes if 'Paddle' in o.name)!=8: raise RuntimeError('Shellback requires exactly eight amphibious feet')
if sum(1 for o in meshes if o.name.startswith('ShellPlate_'))!=5: raise RuntimeError('Shellback requires five independent layered shell plates')
for o in meshes:
    if not o.data.uv_layers.get('AbyssShellbackUV'): raise RuntimeError(f'Missing UVs: {o.name}')
arm['magenheim_asset']='abyss-shellback'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-AMPHIB-ARMORED'; arm['length_m']=LENGTH
arm['role']='blackwater-elite-fortress'; arm['leg_chains']=8; arm['shell_plates']=5; arm['vulnerability']='underside-and-leg-joints'; arm['claw_identity']='bilateral-heavy'
arm['animation_contract']='idle,heavy-walk,turn-left,turn-right,claw-left,claw-right,brace,guard,swim-idle,swim-forward,water-exit,water-entry,hit,stagger,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')
