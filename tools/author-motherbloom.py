#!/usr/bin/env python3
"""Author the Fungal Forest Motherbloom hero landmark.

Motherbloom is not a scaled Glowcap. It is an 18m district silhouette with a buttressed,
hollow lower body, asymmetric crown, layered luminous underside and secondary shelf growth.
The lower chamber is intentionally open and player-scale so later prefab work can make it a
usable shelter/build space without replacing the visual asset.

Run after Fungal Forest textures exist:
    tools/blender.ps1 author-motherbloom
"""
from pathlib import Path
from math import pi, sin, cos
import bpy

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'assets/models/source'; SOURCE.mkdir(parents=True,exist_ok=True)
TEX=ROOT/'assets/textures/underworld/fungal-forest'
OUT=SOURCE/'underworld-flora-motherbloom.blend'


def image(name):
    p=TEX/name
    if not p.is_file(): raise RuntimeError(f'Missing {p}; generate Fungal Forest textures first')
    im=bpy.data.images.get(name) or bpy.data.images.load(str(p),check_existing=True)
    if tuple(im.size)!=(512,512): raise RuntimeError(f'{name}: expected 512x512')
    return im


def mat(name,albedo,rough=.7,emission=None):
    m=bpy.data.materials.new(name); m.use_nodes=True; n=m.node_tree.nodes; l=m.node_tree.links; n.clear()
    out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); bs.inputs['Roughness'].default_value=rough; l.new(bs.outputs['BSDF'],out.inputs['Surface'])
    tex=n.new('ShaderNodeTexImage'); tex.name='MagenheimAlbedo'; tex.image=image(albedo); l.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if emission:
        em=n.new('ShaderNodeTexImage'); em.name='MagenheimEmissionMask'; em.image=image(emission); em.image.colorspace_settings.name='Non-Color'
        if 'Emission Color' in bs.inputs:
            l.new(tex.outputs['Color'],bs.inputs['Emission Color']); l.new(em.outputs['Color'],bs.inputs['Emission Strength'])
    return m


def unwrap(o,sphere=False):
    bpy.context.view_layer.objects.active=o; o.select_set(True); bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.sphere_project() if sphere else bpy.ops.uv.cylinder_project(direction='ALIGN_TO_OBJECT')
    bpy.ops.object.mode_set(mode='OBJECT'); o.select_set(False)


def bevel(o,w):
    b=o.modifiers.new('MotherbloomOrganicEdge','BEVEL'); b.width=w; b.segments=2; b.limit_method='ANGLE'

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
fibre=mat('MotherbloomFibre','fungal-fibre-albedo.png',.84); capmat=mat('MotherbloomCap','fungal-cap-albedo.png',.66); gill=mat('MotherbloomGills','fungal-gills-albedo.png',.48,'fungal-gills-emission.png')

# Eight massive buttresses leave broad walkable gaps and visually anchor the 18m organism.
for i in range(8):
    a=pi*2*i/8; x,y=cos(a)*2.35,sin(a)*2.35
    bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=1.35,radius2=.48,depth=6.2,location=(x,y,3.1))
    o=bpy.context.object; o.name=f'Motherbloom_Buttress_{i+1}'; o.rotation_euler[1]=.16; o.rotation_euler[2]=a; o.data.materials.append(fibre); bevel(o,.09); unwrap(o)

# Elevated trunk begins above the shelter opening: no collision-looking solid plug at ground level.
bpy.ops.mesh.primitive_cone_add(vertices=28,radius1=2.15,radius2=1.42,depth=10.8,location=(0,0,10.2))
tr=bpy.context.object; tr.name='Motherbloom_UpperTrunk'; tr.data.materials.append(fibre)
for v in tr.data.vertices:
    a=__import__('math').atan2(v.co.y,v.co.x); k=1+.065*sin(a*7+v.co.z*.9)+.025*sin(a*13-v.co.z*1.6); v.co.x*=k; v.co.y*=k
bevel(tr,.11); unwrap(tr)

# Asymmetric crown: overlapping lobes make the district silhouette readable from several angles.
for i,(x,y,r,sq) in enumerate(((0,0,6.6,1.0),(2.8,.8,4.4,.72),(-2.4,1.4,4.0,.78),(.5,-2.8,3.7,.68))):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=36,ring_count=14,radius=r,location=(x,y,17.0+i*.16))
    o=bpy.context.object; o.name=f'Motherbloom_Crown_{i+1}'; o.scale=(1,sq,.19); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(capmat); unwrap(o,True)

# Luminous underside discs are separated from crown skin so emission remains spatially controlled.
for i,(x,y,r,sq) in enumerate(((0,0,5.8,1.0),(2.8,.8,3.7,.72),(-2.4,1.4,3.35,.78),(.5,-2.8,3.05,.68))):
    bpy.ops.mesh.primitive_cone_add(vertices=40,radius1=r,radius2=.72,depth=.34,location=(x,y,16.42+i*.16))
    o=bpy.context.object; o.name=f'Motherbloom_GillLayer_{i+1}'; o.scale.y=sq; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(gill); unwrap(o)

# Secondary shelves break the trunk silhouette at player and mid-canopy heights.
for i in range(11):
    a=i*2.39996; z=4.8+(i%6)*1.55; rad=.75+(i%3)*.18; x,y=cos(a)*1.85,sin(a)*1.85
    bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=rad,radius2=.18,depth=.22,location=(x,y,z))
    o=bpy.context.object; o.name=f'Motherbloom_Shelf_{i+1}'; o.rotation_euler[2]=a; o.scale.y=.58; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(gill if i%4==0 else capmat); bevel(o,.025); unwrap(o)

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<20: raise RuntimeError(f'Motherbloom unexpectedly sparse: {len(meshes)} parts')
# Shelter contract is metadata for prefab/collider integration: art keeps a clear central footprint.
bpy.context.scene['magenheim_model_id']='underworld-flora-motherbloom'; bpy.context.scene['magenheim_family']='underworld_fungal_forest_hero'; bpy.context.scene['magenheim_landmark_height_m']=18.0; bpy.context.scene['magenheim_shelter_clear_radius_m']=1.55; bpy.context.scene['magenheim_shelter_clear_height_m']=3.2
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True)
print(f'AUTHORED underworld-flora-motherbloom: {len(meshes)} mesh parts, 18m hero landmark -> {OUT.name}',flush=True)
