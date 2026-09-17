#!/usr/bin/env python3
"""Acceptance gate for the authored Stone Guardian creature source."""
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'assets/models/source/enemy-stone-guardian.blend'
EXPECTED={'magenheim_model_id':'enemy-stone-guardian','magenheim_creature_chassis':'StoneGuardian','magenheim_rig_contract':'separate_head_pelvis_chest_limbs_joints_core'}
def fail(message): raise RuntimeError('Stone Guardian acceptance: '+message)
if not SRC.is_file(): fail(f'missing source {SRC}')
bpy.ops.wm.open_mainfile(filepath=str(SRC)); scene=bpy.context.scene
for key,value in EXPECTED.items():
    if scene.get(key)!=value: fail(f'{key}={scene.get(key)!r}, expected {value!r}')
if abs(float(scene.get('magenheim_character_height_m',0))-3.4)>.01: fail('character-height metadata is not 3.4m')
if int(scene.get('magenheim_crystal_growth_count',0))<4: fail('crystal-growth metadata lost asymmetric growth contract')
meshes=[o for o in scene.objects if o.type=='MESH']
if len(meshes)<35: fail(f'only {len(meshes)} mesh parts')
required={'Guardian_Pelvis','Guardian_Chest','Guardian_Head','Guardian_Core','Guardian_L_Shoulder','Guardian_R_Shoulder','Guardian_L_UpperArm','Guardian_R_UpperArm','Guardian_L_Elbow','Guardian_R_Elbow','Guardian_L_Forearm','Guardian_R_Forearm','Guardian_L_Thigh','Guardian_R_Thigh','Guardian_L_Knee','Guardian_R_Knee','Guardian_L_Shin','Guardian_R_Shin','Guardian_L_Foot','Guardian_R_Foot'}
missing=sorted(required-{o.name for o in meshes})
if missing: fail('missing rig-separable anatomy: '+', '.join(missing))
for o in meshes:
    if not o.data.polygons: fail(f'{o.name}: empty mesh')
    if not any(slot.material for slot in o.material_slots): fail(f'{o.name}: missing material')
    if min(o.dimensions)<=0: fail(f'{o.name}: collapsed dimension')
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=[min(p[i] for p in pts) for i in range(3)]; hi=[max(p[i] for p in pts) for i in range(3)]
width,depth,height=hi[0]-lo[0],hi[1]-lo[1],hi[2]-lo[2]
if not 3.1<=height<=4.2: fail(f'visual height {height:.2f}m outside Guardian envelope')
if width<2.4: fail(f'silhouette too narrow for heavy Guardian role: {width:.2f}m')
core=scene.objects.get('Guardian_Core')
if core is None or core.location.z<2.0: fail('protected crystal core is not chest-readable')
if scene.objects.get('Guardian_Eye_L') is None or scene.objects.get('Guardian_Eye_R') is None: fail('paired eye crystals missing')
growth=[o for o in meshes if o.name.startswith('Guardian_CrystalGrowth_')]; chips=[o for o in meshes if o.name.startswith('Guardian_ArmorChip_')]
if len(growth)<4: fail(f'only {len(growth)} crystal growth pieces')
if len(chips)<12: fail(f'only {len(chips)} armor breakup pieces')
if len({(round(o.location.x,2),round(o.location.z,2)) for o in growth})<4: fail('crystal growth collapsed into repetitive placement')
materials={slot.material.name for o in meshes for slot in o.material_slots if slot.material}
for expected in ('GuardianStone','GuardianFracture','GuardianCrystal'):
    if expected not in materials: fail(f'missing authored material {expected}')
crystal=bpy.data.materials.get('GuardianCrystal')
if crystal is None or not crystal.use_nodes: fail('crystal material lost node graph')
bsdf=crystal.node_tree.nodes.get('Principled BSDF')
if bsdf is None: fail('crystal material missing Principled BSDF')
if 'Emission Strength' in bsdf.inputs and bsdf.inputs['Emission Strength'].default_value<1.0: fail('crystal emission too weak for combat readability')
print(f'VERIFIED Stone Guardian: {len(meshes)} parts, {width:.2f}x{depth:.2f}x{height:.2f}m, {len(growth)} crystal growths, {len(chips)} armor chips, rig-separated anatomy',flush=True)
