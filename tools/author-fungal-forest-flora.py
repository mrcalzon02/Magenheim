#!/usr/bin/env python3
"""Author the first Fungal Forest flora model family.

Run through the campaign wrapper:
    tools/blender.ps1 author-fungal-forest-flora

Creates source .blend files for Glowcap, Spirestalk and Shelfwood in five gameplay states:
standing, cap, felled section, stump and sapling.  These are deliberately authored as fungal
forms rather than recoloured Valheim trees: stalks are tapered/ridged, caps are radial solids
with visible gill undersides, and Shelfwood is a bracket-fungus cluster that reads against a
rock face.  Geometry is UV-unwrapped with cylindrical/spherical projection; smart_project is
never used because generated maps across packed islands caused the 0.0.52 patchwork defect.

This pass owns source geometry only. Runtime spawning, harvesting and economy remain separate
F1 slices. Export changed ids explicitly with export-model-assets.py.
"""
from math import pi, sin, cos
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
SOURCE.mkdir(parents=True, exist_ok=True)

SPECIES = {
    'glowcap': dict(height=7.0, radius=.46, cap=2.25, slender=.82, shelves=False),
    'spirestalk': dict(height=12.0, radius=.38, cap=1.72, slender=.62, shelves=False),
    'shelfwood': dict(height=4.5, radius=.62, cap=1.48, slender=.92, shelves=True),
}
STATES = ('standing', 'cap', 'felled', 'stump', 'sapling')


def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
        pass


def material(name, rgba, roughness=.72, emission=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = rgba
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Roughness'].default_value = roughness
    if emission:
        if 'Emission Color' in bsdf.inputs:
            bsdf.inputs['Emission Color'].default_value = emission
            bsdf.inputs['Emission Strength'].default_value = 1.6
    return m


def apply_bevel(obj, width=.035, segments=2):
    mod = obj.modifiers.new('MagenheimOrganicEdge', 'BEVEL')
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'


def unwrap(obj, mode='cylinder'):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    if mode == 'sphere':
        bpy.ops.uv.sphere_project()
    else:
        bpy.ops.uv.cylinder_project(direction='ALIGN_TO_OBJECT')
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)


def stalk(name, h, r, mat, z=0.0, lean=0.0):
    bpy.ops.mesh.primitive_cone_add(vertices=18, radius1=r * 1.08, radius2=r * .66,
                                    depth=h, location=(0, 0, z + h/2))
    obj = bpy.context.object
    obj.name = name
    # alternating scale gives a fibrous, non-tree trunk silhouette without noise displacement
    mesh = obj.data
    for v in mesh.vertices:
        ang = __import__('math').atan2(v.co.y, v.co.x)
        ridge = 1.0 + .055 * sin(ang * 6.0 + v.co.z * 1.7)
        v.co.x *= ridge
        v.co.y *= ridge
    obj.rotation_euler[1] = lean
    obj.data.materials.append(mat)
    apply_bevel(obj, min(.055, r*.10), 2)
    unwrap(obj)
    return obj


def cap(name, radius, thickness, z, cap_mat, gill_mat, squash=1.0):
    # Closed upper dome.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=12, radius=radius,
                                        location=(0, 0, z))
    upper = bpy.context.object
    upper.name = name + '_Cap'
    upper.scale = (1.0, squash, thickness/radius)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    # Remove lower half and cap it with a shallow gill cone; this keeps the visible silhouette
    # rounded while giving the underside a distinct material/read from player height.
    bm = bmesh.new(); bm.from_mesh(upper.data)
    remove = [v for v in bm.verts if v.co.z < -thickness*.08]
    bmesh.ops.delete(bm, geom=remove, context='VERTS')
    boundary = [e for e in bm.edges if len(e.link_faces) == 1]
    if boundary:
        bmesh.ops.holes_fill(bm, edges=boundary, sides=0)
    bm.to_mesh(upper.data); bm.free(); upper.data.update()
    upper.data.materials.append(cap_mat)
    unwrap(upper, 'sphere')

    bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=radius*.90, radius2=radius*.20,
                                    depth=thickness*.28, location=(0, 0, z-thickness*.18))
    gill = bpy.context.object
    gill.name = name + '_Gills'
    gill.scale.y = squash
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    gill.data.materials.append(gill_mat)
    unwrap(gill)
    return upper, gill


def shelf_cluster(name, h, r, cap_r, stalk_mat, cap_mat, gill_mat, state):
    parts=[]
    if state in ('standing','sapling'):
        count = 4 if state == 'standing' else 2
        scale = 1.0 if state == 'standing' else .38
        for i in range(count):
            z = (.55 + i*.82) * scale
            bpy.ops.mesh.primitive_cone_add(vertices=20, radius1=cap_r*(.72-i*.07)*scale,
                                            radius2=cap_r*(.18)*scale, depth=.24*scale,
                                            location=(r*.18*scale, 0, z))
            o=bpy.context.object; o.name=f'{name}_Shelf_{i+1}'
            o.scale.y=.58; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
            o.data.materials.append(cap_mat if i%2==0 else gill_mat); apply_bevel(o,.025*scale,2); unwrap(o)
            parts.append(o)
        return parts
    # Harvested Shelfwood is represented by dense fibrous slabs rather than a fake tree trunk.
    length = 1.4 if state == 'felled' else .65
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=.34, depth=length, location=(0,0,.34))
    o=bpy.context.object; o.name=name+'_FibrousSection'; o.rotation_euler[1]=pi/2 if state=='felled' else 0
    o.data.materials.append(stalk_mat); apply_bevel(o,.03,2); unwrap(o); return [o]


def author(species, state, s):
    reset()
    stem_mat = material('FungalFibre', (.23,.18,.15,1), .82)
    cap_mat = material('FungalCap', (.30,.22,.32,1), .68)
    gill_mat = material('BioluminescentGills', (.18,.34,.31,1), .52, (.28,.80,.66,1))
    name=f'underworld-flora-{species}-{state}'
    h=s['height']; r=s['radius']; cr=s['cap']
    if s['shelves']:
        shelf_cluster(name,h,r,cr,stem_mat,cap_mat,gill_mat,state)
    elif state == 'standing':
        stalk(name+'_Stalk',h,r,stem_mat,lean=.018 if species=='spirestalk' else 0)
        cap(name,cr,cr*.32,h,cap_mat,gill_mat,.88 if species=='spirestalk' else 1.0)
    elif state == 'cap':
        cap(name,cr,cr*.32,cr*.34,cap_mat,gill_mat,.88 if species=='spirestalk' else 1.0)
    elif state == 'felled':
        o=stalk(name+'_Section',h*.46,r,stem_mat,z=0)
        o.rotation_euler[1]=pi/2; o.location.z=r*1.2
    elif state == 'stump':
        stalk(name+'_Stump',max(.55,h*.09),r*1.06,stem_mat)
    elif state == 'sapling':
        sh=h*.18
        stalk(name+'_YoungStalk',sh,r*.38,stem_mat)
        cap(name,cr*.28,cr*.10,sh,cap_mat,gill_mat,.9 if species=='spirestalk' else 1.0)

    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if not meshes: raise RuntimeError(f'{name}: no geometry authored')
    for o in meshes:
        if not o.data.uv_layers: raise RuntimeError(f'{name}/{o.name}: missing UV layer')
    bpy.context.scene['magenheim_model_id']=name
    bpy.context.scene['magenheim_family']='underworld_fungal_forest_flora'
    bpy.context.scene['magenheim_species']=species
    bpy.context.scene['magenheim_state']=state
    bpy.context.preferences.filepaths.save_version=0
    path=SOURCE/f'{name}.blend'
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)
    print(f'AUTHORED {name}: {len(meshes)} mesh part(s) -> {path.name}', flush=True)


for species,spec in SPECIES.items():
    for state in STATES:
        author(species,state,spec)
print('Authored 15 Fungal Forest flora source models.', flush=True)
