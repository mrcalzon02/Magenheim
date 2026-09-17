#!/usr/bin/env python3
"""Non-destructive acceptance gate for the Motherbloom source hero asset.
Run after author-motherbloom.py inside Blender.
"""
from pathlib import Path
from math import hypot
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'assets/models/source/underworld-flora-motherbloom.blend'
EXPECTED={'magenheim_model_id':'underworld-flora-motherbloom','magenheim_family':'underworld_fungal_forest_hero'}

def fail(msg): raise RuntimeError('Motherbloom acceptance: '+msg)
if not SRC.is_file(): fail(f'missing source {SRC}')
bpy.ops.wm.open_mainfile(filepath=str(SRC))
scene=bpy.context.scene
for k,v in EXPECTED.items():
    if scene.get(k)!=v: fail(f'{k}={scene.get(k)!r}, expected {v!r}')
if abs(float(scene.get('magenheim_landmark_height_m',0))-18.0)>.01: fail('landmark metadata is not 18m')
clear_r=float(scene.get('magenheim_shelter_clear_radius_m',0)); clear_h=float(scene.get('magenheim_shelter_clear_height_m',0))
if clear_r<1.5 or clear_h<3.0: fail(f'shelter contract too small: r={clear_r:.2f}, h={clear_h:.2f}')
meshes=[o for o in scene.objects if o.type=='MESH']
if len(meshes)<20: fail(f'only {len(meshes)} mesh parts')
polys=sum(len(o.data.polygons) for o in meshes)
if polys<1500: fail(f'hero geometry too sparse: {polys} polygons')
for o in meshes:
    if not o.data.polygons: fail(f'{o.name}: empty mesh')
    if not o.data.uv_layers: fail(f'{o.name}: missing UVs')
    if not any(s.material for s in o.material_slots): fail(f'{o.name}: missing material')
# Actual world bounds must read as a landmark, while tolerating organic crown overshoot.
pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=[min(p[i] for p in pts) for i in range(3)]; hi=[max(p[i] for p in pts) for i in range(3)]
w,d,h=hi[0]-lo[0],hi[1]-lo[1],hi[2]-lo[2]
if not 16.0<=h<=20.5: fail(f'visual height {h:.2f}m outside 18m landmark envelope')
if max(w,d)<10.0: fail(f'crown silhouette too narrow: {w:.2f}x{d:.2f}m')
# The authored shelter must be physically clear, not metadata painted over a solid trunk.
for o in meshes:
    if not (o.name.startswith('Motherbloom_Buttress_') or o.name.startswith('Motherbloom_UpperTrunk')): continue
    for c in o.bound_box:
        p=o.matrix_world@Vector(c)
        if 0.25<p.z<clear_h and hypot(p.x,p.y)<clear_r*.82: fail(f'{o.name}: intrudes into central shelter clearance')
# Crown must be multi-lobed/asymmetric and gills independently modeled.
crowns=[o for o in meshes if o.name.startswith('Motherbloom_Crown_')]
gills=[o for o in meshes if o.name.startswith('Motherbloom_GillLayer_')]
butt=[o for o in meshes if o.name.startswith('Motherbloom_Buttress_')]
if len(crowns)<4 or len(gills)<4 or len(butt)<8: fail(f'hero composition incomplete: crowns={len(crowns)}, gills={len(gills)}, buttresses={len(butt)}')
centres={(round(o.location.x,1),round(o.location.y,1)) for o in crowns}
if len(centres)<4: fail('crown lobes collapsed into a symmetric stack')
# Material graph contract: gills need a real emission mask and all materials need mapped albedo.
for o in meshes:
    for slot in o.material_slots:
        m=slot.material
        if not m or not m.use_nodes: continue
        if not m.node_tree.nodes.get('MagenheimAlbedo'): fail(f'{m.name}: missing mapped albedo')
        if m.name=='MotherbloomGills' and not m.node_tree.nodes.get('MagenheimEmissionMask'): fail('gills missing emission mask')
print(f'VERIFIED Motherbloom: {len(meshes)} parts, {polys} polygons, {w:.2f}x{d:.2f}x{h:.2f}m; shelter r={clear_r:.2f}m h={clear_h:.2f}m')
