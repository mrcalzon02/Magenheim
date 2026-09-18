#!/usr/bin/env python3
"""Author the high-fidelity Stone Guardian enemy source model.

The Guardian is the first creature-quality reference asset for the Deep Fracture roster:
a broad 3.4m stone chassis with articulated armor masses, readable joints, a protected
crystal core and asymmetric mineral growth. Geometry is separated by anatomical function
for later Unity/Valheim rigging instead of being fused into an unriggable statue.
"""
from pathlib import Path
from math import pi, sin, cos
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUTDIR=ROOT/'assets/models/source'; OUTDIR.mkdir(parents=True,exist_ok=True)
OUT=OUTDIR/'enemy-stone-guardian.blend'
CHARACTER_HEIGHT_M=3.4

def mat(name,color,rough,metal=0.0,emission=None):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough; bs.inputs['Metallic'].default_value=metal
    if emission and 'Emission Color' in bs.inputs:
        bs.inputs['Emission Color'].default_value=(*emission,1); bs.inputs['Emission Strength'].default_value=2.4
    return m

def bevel(o,w=.04):
    b=o.modifiers.new('GuardianEdge','BEVEL'); b.width=w; b.segments=2; b.limit_method='ANGLE'

def cube(name,loc,scale,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); bevel(o,min(scale)*.16); return o

def rock(name,loc,scale,material,seed=0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for v in o.data.vertices:
        k=1+.075*sin(v.co.x*3.7+seed)+.055*cos(v.co.z*4.3-seed*.7); v.co*=k
    o.data.materials.append(material); bevel(o,.025); return o

def crystal(name,loc,scale,material,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=6,radius1=scale[0],radius2=0,depth=scale[2],location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.scale.y=scale[1]/scale[0]; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); bevel(o,.018); return o

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
stone=mat('GuardianStone',(0.20,.22,.21),.92); fracture=mat('GuardianFracture',(0.075,.085,.08),.98); crystalmat=mat('GuardianCrystal',(.12,.42,.48),.28,.08,(.16,.72,.82))

# Anatomical masses are deliberately separate and named for rig/prefab binding.
rock('Guardian_Pelvis',(0,0,1.62),(.78,.52,.50),stone,1)
rock('Guardian_Chest',(0,0,2.35),(1.02,.61,.78),stone,2)
rock('Guardian_Head',(0,-.03,3.10),(.55,.48,.48),stone,3)
# Recessed luminous heart remains visible through a broken chest aperture.
crystal('Guardian_Core',(0,-.57,2.38),(.27,.22,.62),crystalmat,(pi/2,0,0))
for side in (-1,1):
    s='L' if side<0 else 'R'
    rock(f'Guardian_{s}_Shoulder',(side*1.03,0,2.55),(.48,.55,.52),stone,4+side)
    cube(f'Guardian_{s}_UpperArm',(side*1.22,0,1.98),(.30,.32,.58),stone,rot=(0,side*.10,side*.05))
    rock(f'Guardian_{s}_Elbow',(side*1.28,0,1.40),(.34,.34,.31),fracture,6+side)
    cube(f'Guardian_{s}_Forearm',(side*1.31,-.02,.98),(.34,.37,.48),stone,rot=(0,-side*.08,0))
    rock(f'Guardian_{s}_Hand',(side*1.32,-.10,.48),(.40,.46,.30),stone,8+side)
    cube(f'Guardian_{s}_Thigh',(side*.48,0,1.03),(.37,.40,.58),stone,rot=(0,side*.05,0))
    rock(f'Guardian_{s}_Knee',(side*.49,-.05,.49),(.38,.43,.31),fracture,10+side)
    cube(f'Guardian_{s}_Shin',(side*.50,.02,.02),(.35,.38,.48),stone)
    rock(f'Guardian_{s}_Foot',(side*.50,-.22,-.42),(.43,.66,.25),stone,12+side)

# Layered facial brow and jaw give a readable hostile expression without humanizing the chassis.
cube('Guardian_Brow', (0,-.43,3.20),(.48,.12,.13),fracture)
cube('Guardian_Jaw', (0,-.40,2.91),(.42,.16,.17),stone)
for side in (-1,1):
    crystal(f'Guardian_Eye_{"L" if side<0 else "R"}',(side*.19,-.505,3.15),(.065,.055,.16),crystalmat,(pi/2,0,0))

# Asymmetric crystal growth communicates the modular crystal-component system at silhouette range.
growth=((.72,.05,3.18,.18,.62,.18),(.91,.08,2.92,.14,.48,.30),(-.76,.10,2.76,.12,.40,-.22),(.38,.22,2.98,.10,.34,.08))
for i,(x,y,z,r,h,tilt) in enumerate(growth):
    crystal(f'Guardian_CrystalGrowth_{i+1}',(x,y,z),(r,r*.82,h),crystalmat,(0,tilt,0))

# Stone armor chips provide medium-frequency silhouette breakup; deterministic placement avoids noise soup.
for i in range(14):
    a=i*2.39996; z=.35+(i%7)*.39; radius=.78 if z>1.7 else .52
    rock(f'Guardian_ArmorChip_{i+1}',(cos(a)*radius,sin(a)*.48,z),(.13+(i%3)*.035,.10,.18+(i%2)*.04),stone,20+i)

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<35: raise RuntimeError(f'Stone Guardian fidelity regression: only {len(meshes)} mesh parts')

# The masses above are authored in a working frame whose soles hang below Z=0 and whose crown
# reaches ~4.28m, so the chassis contradicted the 3.4m character height this script declares.
# Normalise once, at the end: scale uniformly to the declared height and drop the soles onto
# Z=0. Doing it here keeps every authored proportion, material and part separation intact while
# making the declared contract literally true for the acceptance gate and for later rigging.
_pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
_lo_z=min(p[2] for p in _pts); _height=max(p[2] for p in _pts)-_lo_z
if _height<=0: raise RuntimeError('Stone Guardian chassis has no vertical extent')
_k=CHARACTER_HEIGHT_M/_height
for o in meshes:
    o.location=(o.location.x*_k,o.location.y*_k,(o.location.z-_lo_z)*_k)
    o.scale=(o.scale.x*_k,o.scale.y*_k,o.scale.z*_k)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
bpy.context.scene['magenheim_model_id']='enemy-stone-guardian'; bpy.context.scene['magenheim_creature_chassis']='StoneGuardian'
bpy.context.scene['magenheim_character_height_m']=CHARACTER_HEIGHT_M; bpy.context.scene['magenheim_rig_contract']='separate_head_pelvis_chest_limbs_joints_core'
bpy.context.scene['magenheim_fidelity']='hero-creature-r1'; bpy.context.scene['magenheim_crystal_growth_count']=len(growth)
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True)
print(f'AUTHORED enemy-stone-guardian: {len(meshes)} parts, rig-separated high-fidelity chassis -> {OUT.name}',flush=True)
