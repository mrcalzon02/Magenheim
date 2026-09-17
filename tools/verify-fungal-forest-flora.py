#!/usr/bin/env python3
"""Acceptance gate for authored Fungal Forest flora source assets.

Run after texture generation and Blender authoring:
    tools/blender.ps1 verify-fungal-forest-flora

Checks all 15 Glowcap/Spirestalk/Shelfwood source models for geometry, UVs, expected mapped
materials, 512px source images, and species/state scale sanity. This gate does not alter assets.
"""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'assets/models/source'; TEX=ROOT/'assets/textures/underworld/fungal-forest'
SPECIES={'glowcap':7.0,'spirestalk':12.0,'shelfwood':4.5}; STATES=('standing','cap','felled','stump','sapling')
MAPS={'FungalFibre':'fungal-fibre-albedo.png','FungalCap':'fungal-cap-albedo.png','BioluminescentGills':'fungal-gills-albedo.png'}; EMISSION='fungal-gills-emission.png'

def fail(msg): raise RuntimeError(msg)

def verify_textures():
    for filename in (*MAPS.values(),EMISSION):
        p=TEX/filename
        if not p.is_file(): fail(f'missing texture {p}')
        im=bpy.data.images.load(str(p),check_existing=True)
        if tuple(im.size)!=(512,512): fail(f'{filename}: expected 512x512, got {tuple(im.size)}')

def material_contract(model_id,mat):
    if mat.name not in MAPS: fail(f'{model_id}: unexpected material {mat.name}')
    if not mat.use_nodes: fail(f'{model_id}/{mat.name}: nodes disabled')
    albedo=mat.node_tree.nodes.get('MagenheimAlbedo')
    if not albedo or not albedo.image or Path(albedo.image.filepath).name!=MAPS[mat.name]: fail(f'{model_id}/{mat.name}: wrong or missing albedo map')
    if mat.name=='BioluminescentGills':
        em=mat.node_tree.nodes.get('MagenheimEmissionMask')
        if not em or not em.image or Path(em.image.filepath).name!=EMISSION: fail(f'{model_id}: gills missing authored emission mask')

def dimensions(objects):
    pts=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
    return tuple(max(p[i] for p in pts)-min(p[i] for p in pts) for i in range(3))

def verify_model(species,state):
    model_id=f'underworld-flora-{species}-{state}'; p=SOURCE/f'{model_id}.blend'
    if not p.is_file(): fail(f'{model_id}: missing source blend')
    bpy.ops.wm.open_mainfile(filepath=str(p)); objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if not objects: fail(f'{model_id}: no mesh geometry')
    polygons=sum(len(o.data.polygons) for o in objects)
    if polygons<12: fail(f'{model_id}: implausibly low geometry ({polygons} polygons)')
    used=set()
    for o in objects:
        if not o.data.uv_layers: fail(f'{model_id}/{o.name}: no UV layer')
        if not o.data.polygons: fail(f'{model_id}/{o.name}: empty mesh')
        for slot in o.material_slots:
            if slot.material: used.add(slot.material.name); material_contract(model_id,slot.material)
    if not used: fail(f'{model_id}: no authored material')
    dx,dy,dz=dimensions(objects); longest=max(dx,dy,dz); expected=SPECIES[species]
    if longest<=.08: fail(f'{model_id}: degenerate bounds')
    if state=='standing' and (dz<expected*.70 or dz>expected*1.35): fail(f'{model_id}: standing height {dz:.2f} outside species envelope')
    if state=='standing' and species!='shelfwood' and 'BioluminescentGills' not in used: fail(f'{model_id}: no luminous gill material')
    if state=='sapling' and longest>=expected*.60: fail(f'{model_id}: sapling not visually subordinate ({longest:.2f}m)')
    print(f'VERIFIED {model_id}: {len(objects)} parts, {polygons} polygons, {dx:.2f}x{dy:.2f}x{dz:.2f}m')

verify_textures()
for species in SPECIES:
    for state in STATES: verify_model(species,state)
print('Verified 15 Fungal Forest flora source assets and mapped texture contracts.')
