#!/usr/bin/env python3
"""Author the first Fungal Forest flora model family with mapped materials.

Run first:
    python tools/generate-fungal-forest-textures.py
Then:
    tools/blender.ps1 author-fungal-forest-flora

Creates Glowcap, Spirestalk and Shelfwood in five gameplay states. Geometry uses authored
cylindrical/spherical UV projection; smart_project is forbidden. Materials consume the
512px Fungal Forest source maps and the gill material uses the authored emission mask rather
than a constant glow.
"""
from math import pi, sin
from pathlib import Path
import bpy
import bmesh

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'assets/models/source'; SOURCE.mkdir(parents=True,exist_ok=True)
TEXTURES=ROOT/'assets/textures/underworld/fungal-forest'
SPECIES={'glowcap':dict(height=7.0,radius=.46,cap=2.25,shelves=False),'spirestalk':dict(height=12.0,radius=.38,cap=1.72,shelves=False),'shelfwood':dict(height=4.5,radius=.62,cap=1.48,shelves=True)}
STATES=('standing','cap','felled','stump','sapling')
MAPS={'FungalFibre':('fungal-fibre-albedo.png',None),'FungalCap':('fungal-cap-albedo.png',None),'BioluminescentGills':('fungal-gills-albedo.png','fungal-gills-emission.png')}


def reset():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)


def image(name):
    p=TEXTURES/name
    if not p.is_file(): raise RuntimeError(f'Missing Fungal Forest texture: {p}; run generate-fungal-forest-textures.py first')
    im=bpy.data.images.get(name) or bpy.data.images.load(str(p),check_existing=True)
    if tuple(im.size)!=(512,512): raise RuntimeError(f'{name}: expected 512x512, got {tuple(im.size)}')
    return im


def material(name,roughness=.72):
    albedo_name,emission_name=MAPS[name]
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_nodes=True
    nt=m.node_tree; nt.nodes.clear()
    out=nt.nodes.new('ShaderNodeOutputMaterial'); bsdf=nt.nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value=roughness; nt.links.new(bsdf.outputs['BSDF'],out.inputs['Surface'])
    tex=nt.nodes.new('ShaderNodeTexImage'); tex.name='MagenheimAlbedo'; tex.image=image(albedo_name); tex.interpolation='Linear'
    nt.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    if emission_name:
        em=nt.nodes.new('ShaderNodeTexImage'); em.name='MagenheimEmissionMask'; em.image=image(emission_name); em.image.colorspace_settings.name='Non-Color'
        if 'Emission Color' in bsdf.inputs:
            nt.links.new(tex.outputs['Color'],bsdf.inputs['Emission Color']); nt.links.new(em.outputs['Color'],bsdf.inputs['Emission Strength'])
            # Map is deliberately bright enough to drive the underside while preserving dark inter-gill tissue.
            em.outputs['Color'].default_value=(1,1,1,1)
    return m


def bevel(o,w=.035):
    mod=o.modifiers.new('MagenheimOrganicEdge','BEVEL'); mod.width=w; mod.segments=2; mod.limit_method='ANGLE'


def unwrap(o,sphere=False):
    bpy.context.view_layer.objects.active=o; o.select_set(True); bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.sphere_project() if sphere else bpy.ops.uv.cylinder_project(direction='ALIGN_TO_OBJECT')
    bpy.ops.object.mode_set(mode='OBJECT'); o.select_set(False)


def stalk(name,h,r,mat):
    bpy.ops.mesh.primitive_cone_add(vertices=18,radius1=r*1.08,radius2=r*.66,depth=h,location=(0,0,h/2)); o=bpy.context.object; o.name=name
    for v in o.data.vertices:
        import math
        a=math.atan2(v.co.y,v.co.x); k=1+.055*sin(a*6+v.co.z*1.7); v.co.x*=k; v.co.y*=k
    o.data.materials.append(mat); bevel(o,min(.055,r*.10)); unwrap(o); return o


def cap(name,r,t,z,cm,gm,squash=1):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=12,radius=r,location=(0,0,z)); u=bpy.context.object; u.name=name+'_Cap'; u.scale=(1,squash,t/r); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bm=bmesh.new(); bm.from_mesh(u.data); bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z < -t*.08],context='VERTS'); boundary=[e for e in bm.edges if len(e.link_faces)==1]
    if boundary: bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
    bm.to_mesh(u.data); bm.free(); u.data.materials.append(cm); unwrap(u,True)
    bpy.ops.mesh.primitive_cone_add(vertices=32,radius1=r*.90,radius2=r*.20,depth=t*.28,location=(0,0,z-t*.18)); g=bpy.context.object; g.name=name+'_Gills'; g.scale.y=squash; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); g.data.materials.append(gm); unwrap(g)


def shelf(name,r,cr,sm,cm,gm,state):
    if state in ('standing','sapling'):
        count=4 if state=='standing' else 2; scale=1 if state=='standing' else .38
        for i in range(count):
            z=(.55+i*.82)*scale; bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=cr*(.72-i*.07)*scale,radius2=cr*.18*scale,depth=.24*scale,location=(r*.18*scale,0,z)); o=bpy.context.object; o.name=f'{name}_Shelf_{i+1}'; o.scale.y=.58; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(cm if i%2==0 else gm); bevel(o,.025*scale); unwrap(o)
    else:
        length=1.4 if state=='felled' else .65; bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=.34,depth=length,location=(0,0,.34)); o=bpy.context.object; o.name=name+'_FibrousSection'; o.rotation_euler[1]=pi/2 if state=='felled' else 0; o.data.materials.append(sm); bevel(o,.03); unwrap(o)


def author(species,state,s):
    reset(); sm=material('FungalFibre',.82); cm=material('FungalCap',.68); gm=material('BioluminescentGills',.52); name=f'underworld-flora-{species}-{state}'; h=s['height']; r=s['radius']; cr=s['cap']
    if s['shelves']: shelf(name,r,cr,sm,cm,gm,state)
    elif state=='standing': stalk(name+'_Stalk',h,r,sm); cap(name,cr,cr*.32,h,cm,gm,.88 if species=='spirestalk' else 1)
    elif state=='cap': cap(name,cr,cr*.32,cr*.34,cm,gm,.88 if species=='spirestalk' else 1)
    elif state=='felled': o=stalk(name+'_Section',h*.46,r,sm); o.rotation_euler[1]=pi/2; o.location.z=r*1.2
    elif state=='stump': stalk(name+'_Stump',max(.55,h*.09),r*1.06,sm)
    else: sh=h*.18; stalk(name+'_YoungStalk',sh,r*.38,sm); cap(name,cr*.28,cr*.10,sh,cm,gm,.9 if species=='spirestalk' else 1)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if not meshes: raise RuntimeError(f'{name}: no geometry')
    for o in meshes:
        if not o.data.uv_layers: raise RuntimeError(f'{name}/{o.name}: missing UV')
    bpy.context.scene['magenheim_model_id']=name; bpy.context.scene['magenheim_family']='underworld_fungal_forest_flora'; bpy.context.scene['magenheim_species']=species; bpy.context.scene['magenheim_state']=state; bpy.context.preferences.filepaths.save_version=0
    p=SOURCE/f'{name}.blend'; bpy.ops.wm.save_as_mainfile(filepath=str(p),compress=True); print(f'AUTHORED {name}: mapped materials, {len(meshes)} mesh part(s)',flush=True)

for species,spec in SPECIES.items():
    for state in STATES: author(species,state,spec)
print('Authored 15 mapped Fungal Forest flora source models.',flush=True)
