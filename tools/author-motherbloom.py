#!/usr/bin/env python3
"""Author the Fungal Forest Motherbloom hero landmark with close-range organic detail."""
from pathlib import Path
from math import pi, sin, cos, atan2
import bpy

ROOT=Path(__file__).resolve().parents[1]; SOURCE=ROOT/'assets/models/source'; SOURCE.mkdir(parents=True,exist_ok=True)
TEX=ROOT/'assets/textures/underworld/fungal-forest'; OUT=SOURCE/'underworld-flora-motherbloom.blend'

def image(name):
    p=TEX/name
    if not p.is_file(): raise RuntimeError(f'Missing {p}; generate Fungal Forest textures first')
    im=bpy.data.images.get(name) or bpy.data.images.load(str(p),check_existing=True)
    if tuple(im.size)!=(512,512): raise RuntimeError(f'{name}: expected 512x512')
    return im

def mat(name,albedo,rough=.7,emission=None):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_nodes=True; n=m.node_tree.nodes; l=m.node_tree.links; n.clear()
    out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); bs.inputs['Roughness'].default_value=rough; l.new(bs.outputs['BSDF'],out.inputs['Surface'])
    tex=n.new('ShaderNodeTexImage'); tex.name='MagenheimAlbedo'; tex.image=image(albedo); l.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if emission:
        em=n.new('ShaderNodeTexImage'); em.name='MagenheimEmissionMask'; em.image=image(emission); em.image.colorspace_settings.name='Non-Color'
        if 'Emission Color' in bs.inputs: l.new(tex.outputs['Color'],bs.inputs['Emission Color']); l.new(em.outputs['Color'],bs.inputs['Emission Strength'])
    return m

def unwrap(o,sphere=False):
    bpy.context.view_layer.objects.active=o; o.select_set(True); bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.sphere_project() if sphere else bpy.ops.uv.cylinder_project(direction='ALIGN_TO_OBJECT')
    bpy.ops.object.mode_set(mode='OBJECT'); o.select_set(False)

def bevel(o,w):
    b=o.modifiers.new('MotherbloomOrganicEdge','BEVEL'); b.width=w; b.segments=2; b.limit_method='ANGLE'

def organic_radial(o, lobes, strength, vertical=.7):
    for v in o.data.vertices:
        a=atan2(v.co.y,v.co.x); phase=v.co.z*vertical
        k=1+strength*sin(a*lobes+phase)+strength*.42*sin(a*(lobes+5)-phase*1.7)
        v.co.x*=k; v.co.y*=k

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
fibre=mat('MotherbloomFibre','fungal-fibre-albedo.png',.84); capmat=mat('MotherbloomCap','fungal-cap-albedo.png',.66); gill=mat('MotherbloomGills','fungal-gills-albedo.png',.48,'fungal-gills-emission.png')

# Buttresses receive longitudinal ridging and unequal radial lean so the base reads grown, not arrayed.
for i in range(8):
    a=pi*2*i/8; radial=2.28+.18*sin(i*2.17); x,y=cos(a)*radial,sin(a)*radial
    bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=1.38+(i%3)*.06,radius2=.46,depth=6.2+(i%2)*.28,location=(x,y,3.1))
    o=bpy.context.object; o.name=f'Motherbloom_Buttress_{i+1}'; o.rotation_euler[1]=.12+.035*(i%3); o.rotation_euler[2]=a; organic_radial(o,5+i%3,.075,1.15)
    o.data.materials.append(fibre); bevel(o,.075); unwrap(o)

bpy.ops.mesh.primitive_cone_add(vertices=32,radius1=2.15,radius2=1.42,depth=10.8,location=(0,0,10.2))
tr=bpy.context.object; tr.name='Motherbloom_UpperTrunk'; tr.data.materials.append(fibre); organic_radial(tr,7,.067,.9); bevel(tr,.10); unwrap(tr)

# Raised growth scars add readable close-range relief without fragmenting the primary trunk UV.
for i in range(9):
    a=i*2.39996+.31; z=6.0+(i%5)*1.55; rr=2.02-.075*(z-6)
    bpy.ops.mesh.primitive_torus_add(major_radius=.31+(i%3)*.055,minor_radius=.055,major_segments=14,minor_segments=6,location=(cos(a)*rr,sin(a)*rr,z),rotation=(pi/2,0,a))
    o=bpy.context.object; o.name=f'Motherbloom_GrowthScar_{i+1}'; o.scale.y=.58; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(fibre); unwrap(o)

crowns=((0,0,6.6,1.0,7),(2.8,.8,4.4,.72,6),(-2.4,1.4,4.0,.78,8),(.5,-2.8,3.7,.68,5))
for i,(x,y,r,sq,lobes) in enumerate(crowns):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=16,radius=r,location=(x,y,17.0+i*.16))
    o=bpy.context.object; o.name=f'Motherbloom_Crown_{i+1}'; o.scale=(1,sq,.19); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    # Scalloped perimeter and secondary waviness prevent a clean manufactured ellipse.
    for v in o.data.vertices:
        a=atan2(v.co.y/max(sq,.01),v.co.x); edge=max(0.0,1.0-abs(v.co.z)/(r*.19))
        k=1+edge*(.055*sin(a*lobes+i*.7)+.022*sin(a*(lobes*2+3)-i))
        v.co.x*=k; v.co.y*=k
    o.data.materials.append(capmat); unwrap(o,True)

for i,(x,y,r,sq,_) in enumerate(crowns):
    bpy.ops.mesh.primitive_cone_add(vertices=48,radius1=r*.88,radius2=.72,depth=.34,location=(x,y,16.42+i*.16))
    o=bpy.context.object; o.name=f'Motherbloom_GillLayer_{i+1}'; o.scale.y=sq; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(gill); unwrap(o)

# Hanging luminous tendrils create under-canopy depth and a visible spore curtain from player height.
for i in range(18):
    a=i*2.39996; ring=3.25+(i%4)*.52; top=15.95-(i%3)*.12; length=.75+(i%5)*.24
    x,y=cos(a)*ring,sin(a)*ring
    bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=.055,radius2=.16,depth=length,location=(x,y,top-length/2))
    o=bpy.context.object; o.name=f'Motherbloom_SporeTendril_{i+1}'; o.rotation_euler[0]=.08*sin(a*3); o.rotation_euler[1]=.08*cos(a*2); o.data.materials.append(gill); unwrap(o)

for i in range(11):
    a=i*2.39996; z=4.8+(i%6)*1.55; rad=.75+(i%3)*.18; x,y=cos(a)*1.85,sin(a)*1.85
    bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=rad,radius2=.18,depth=.22,location=(x,y,z))
    o=bpy.context.object; o.name=f'Motherbloom_Shelf_{i+1}'; o.rotation_euler[2]=a; o.scale.y=.58; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(gill if i%4==0 else capmat); bevel(o,.025); unwrap(o)

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)<45: raise RuntimeError(f'Motherbloom detail regression: {len(meshes)} parts')
bpy.context.scene['magenheim_model_id']='underworld-flora-motherbloom'; bpy.context.scene['magenheim_family']='underworld_fungal_forest_hero'
bpy.context.scene['magenheim_landmark_height_m']=18.0; bpy.context.scene['magenheim_shelter_clear_radius_m']=1.55; bpy.context.scene['magenheim_shelter_clear_height_m']=3.2
bpy.context.scene['magenheim_detail_revision']=2; bpy.context.scene['magenheim_spore_tendril_count']=18
bpy.context.preferences.filepaths.save_version=0; bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True)
print(f'AUTHORED underworld-flora-motherbloom detail-r2: {len(meshes)} mesh parts -> {OUT.name}',flush=True)
