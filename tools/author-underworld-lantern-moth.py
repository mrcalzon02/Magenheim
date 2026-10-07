#!/usr/bin/env python3
"""Author the Fungal Forest Lantern Moth production source and HOST-INSECT-FLY rig.

This deliberately replaces the Bat silhouette instead of tinting it. Valheim may remain the
future locomotion/network chassis, but visible anatomy is four-wing moth geometry.
"""
from pathlib import Path
from math import pi, sin, cos
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/models/source/underworld-creature-lantern-moth.blend'
TEX=ROOT/'assets/textures/underworld/creatures/lantern-moth'
OUT.parent.mkdir(parents=True,exist_ok=True)

def image(stem,kind):
    p=TEX/f'{stem}-{kind}.png'
    if not p.exists(): raise RuntimeError(f'Missing {p}; run generate-underworld-lantern-moth-textures.py first')
    im=bpy.data.images.load(str(p),check_existing=True)
    if kind in ('normal','roughness','emission'): im.colorspace_settings.name='Non-Color'
    return im

def material(name,stem,emission=False):
    m=bpy.data.materials.new(name); m.use_nodes=True
    n=m.node_tree.nodes; l=m.node_tree.links; n.clear()
    out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); l.new(bs.outputs['BSDF'],out.inputs['Surface'])
    uv=n.new('ShaderNodeTexCoord')
    a=n.new('ShaderNodeTexImage'); a.image=image(stem,'albedo'); l.new(uv.outputs['UV'],a.inputs['Vector']); l.new(a.outputs['Color'],bs.inputs['Base Color'])
    r=n.new('ShaderNodeTexImage'); r.image=image(stem,'roughness'); l.new(uv.outputs['UV'],r.inputs['Vector']); l.new(r.outputs['Color'],bs.inputs['Roughness'])
    t=n.new('ShaderNodeTexImage'); t.image=image(stem,'normal'); l.new(uv.outputs['UV'],t.inputs['Vector']); nm=n.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value=.55; l.new(t.outputs['Color'],nm.inputs['Color']); l.new(nm.outputs['Normal'],bs.inputs['Normal'])
    if emission:
        e=n.new('ShaderNodeTexImage'); e.image=image(stem,'emission'); eout=bs.inputs.get('Emission Color') or bs.inputs.get('Emission')
        estr=bs.inputs.get('Emission Strength')
        if eout is not None: l.new(e.outputs['Color'],eout)
        if estr is not None: estr.default_value=1.6
    return m

def organic(name,loc,scale,mat,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(mat); return o

def segment(name,a,b,r,mat,verts=10):
    a,b=Vector(a),Vector(b); d=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(a+b)*.5)
    o=bpy.context.object; o.name=name; o.rotation_mode='QUATERNION'; o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized()); o.rotation_mode='XYZ'; o.data.materials.append(mat); return o

def wing(name,side,fore,mat,vein_mat):
    """Closed, thin four-wing anatomy with deliberate two-sided UVs and raised veins.

    The donor Bat has no usable moth-wing silhouette. Keep each membrane on its own
    wing bone, with readable vein relief rather than texture-only structural lines.
    """
    s=-1 if side=='L' else 1
    if fore:
        pts=[(.045*s,.055,.105),(.16*s,.12,.12),(.31*s,.095,.105),(.285*s,-.015,.095),(.15*s,-.045,.09),(.06*s,.005,.10)]
    else:
        pts=[(.04*s,-.005,.095),(.135*s,-.045,.105),(.24*s,-.11,.095),(.19*s,-.18,.08),(.09*s,-.14,.078),(.045*s,-.06,.09)]
    top=[Vector((x,y,z+.0015)) for x,y,z in pts]
    bottom=[Vector((x,y,z-.0015)) for x,y,z in pts]
    verts=top+bottom
    triangles=[(0,1,5),(1,4,5),(1,2,4),(2,3,4)]
    # Mirroring the left/right coordinates reverses winding. Both dorsal surfaces
    # must face upward; the ventral surfaces must face downward in exported glTF.
    upper=triangles if s==-1 else [tuple(reversed(t)) for t in triangles]
    lower=[tuple(reversed(tuple(i+6 for i in t))) for t in upper]
    rim=[]
    for i in range(6):
        j=(i+1)%6
        rim.append((i,i+6,j+6,j) if s==-1 else (i,j,j+6,i+6))
    mesh=bpy.data.meshes.new(name+'Mesh')
    mesh.from_pydata(verts,[],upper+lower+rim)
    mesh.update()
    o=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(o)
    o.data.materials.append(mat)
    uv=o.data.uv_layers.new(name='LanternMothUV')
    perimeter=[0.0]
    for i in range(6):
        perimeter.append(perimeter[-1]+(Vector(pts[(i+1)%6])-Vector(pts[i])).length)
    for pi,poly in enumerate(o.data.polygons):
        for li in poly.loop_indices:
            index=o.data.loops[li].vertex_index
            v=o.data.vertices[index].co
            if pi<8:
                uv.data[li].uv=(.5+v.x*1.45,.5+v.y*2.1)
            else:
                # Give the actual 3 mm rim a separate narrow UV strip; projecting
                # both rim levels to the same XY would collapse every side-wall UV.
                edge=pi-8
                u=perimeter[edge if index%6==edge else edge+1]/perimeter[-1]
                uv.data[li].uv=(u,.98 if index<6 else .95)
    veins=[]
    kind='Fore' if fore else 'Hind'
    for i,(a,b) in enumerate(((0,1),(0,2),(0,4))):
        for face,offset in (('Dorsal',.0023),('Ventral',-.0023)):
            start=Vector(pts[a]); finish=Vector(pts[b])
            start.z+=offset; finish.z+=offset
            veins.append(segment(f'LM_WingVein_{side}_{kind}_{i}_{face}',start,finish,.0015,vein_mat,6))
    return o,veins

def planar_uv(o,scale=3.0):
    uv=o.data.uv_layers.new(name='LanternMothUV')
    for p in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(p.normal[i]))
        for li in p.loop_indices:
            c=o.data.vertices[o.data.loops[li].vertex_index].co; xy=((c.y,c.z),(c.x,c.z),(c.x,c.y))[axis]
            uv.data[li].uv=((xy[0]*scale)%1,(xy[1]*scale)%1)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
thorax=material('LanternMoth_ThoraxFuzz','thorax-fuzz')
abdomen=material('LanternMoth_AbdomenChitin','abdomen-chitin')
wingmat=material('LanternMoth_WingLight','wing-membrane',True)
ant=material('LanternMoth_Antenna','antenna')
eye=material('LanternMoth_Eye','eye')

parts={}
def keep(o,bone):
    parts[o.name]=bone
    if not o.data.uv_layers: planar_uv(o)
    else: o.data.uv_layers.active.name='LanternMothUV'
    return o

keep(organic('LM_Thorax',(0,0,.105),(.075,.105,.07),thorax,3),'Thorax')
keep(organic('LM_Abdomen',(0,-.12,.095),(.055,.14,.052),abdomen,3),'Abdomen')
keep(organic('LM_Head',(0,.105,.105),(.052,.05,.05),thorax,2),'Head')
for side in ('L','R'):
    s=-1 if side=='L' else 1
    keep(organic(f'LM_Eye_{side}',(s*.037,.14,.12),(.012,.009,.011),eye,2),'Head')
for side in ('L','R'):
    for fore in (True,False):
        bone=f'{"Fore" if fore else "Hind"}Wing_{side}'
        membrane,veins=wing(f'LM_{"Fore" if fore else "Hind"}Wing_{side}',side,fore,wingmat,ant)
        keep(membrane,bone)
        for vein in veins: keep(vein,bone)
for i in range(12):
    a=i*pi*2/12; base=(cos(a)*.055,sin(a)*.07,.145+sin(a*2)*.006); tip=(cos(a)*.085,sin(a)*.105,.17+sin(a*2)*.008)
    keep(segment(f'LM_ThoraxTuft_{i:02d}',base,tip,.006,thorax,8),'Thorax')
for side in ('L','R'):
    s=-1 if side=='L' else 1
    p0=(s*.025,.142,.13); p1=(s*.05,.19,.15); p2=(s*.075,.235,.145)
    keep(segment(f'LM_AntennaBase_{side}',p0,p1,.0045,ant,8),f'Antenna1_{side}')
    keep(segment(f'LM_AntennaTip_{side}',p1,p2,.003,ant,8),f'Antenna2_{side}')
for pair,y in enumerate((.045,-.015,-.075),1):
    for side in ('L','R'):
        s=-1 if side=='L' else 1
        a=(s*.045,y,.075); b=(s*.085,y-.02,.04); c=(s*.105,y-.04,.012)
        keep(segment(f'LM_LegUpper_{side}{pair}',a,b,.0045,ant,8),'Thorax')
        keep(segment(f'LM_LegLower_{side}{pair}',b,c,.0032,ant,8),'Thorax')

bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0)); arm=bpy.context.object; arm.name='RIG_LanternMoth_HOST_INSECT_FLY'
eb=arm.data.edit_bones; root=eb[0]; root.name='Root'; root.head=(0,0,.03); root.tail=(0,0,.09)
def bone(n,h,t,p=None): b=eb.new(n); b.head=h; b.tail=t; b.parent=p; return b
th=bone('Thorax',(0,0,.07),(0,0,.15),root)
ab=bone('Abdomen',(0,-.02,.10),(0,-.22,.09),th)
hd=bone('Head',(0,.035,.105),(0,.16,.105),th)
for side in ('L','R'):
    s=-1 if side=='L' else 1
    bone(f'ForeWing_{side}',(s*.045,.045,.105),(s*.25,.09,.105),th)
    bone(f'HindWing_{side}',(s*.04,-.015,.095),(s*.19,-.12,.09),th)
    a1=bone(f'Antenna1_{side}',(s*.02,.14,.13),(s*.05,.19,.15),hd)
    bone(f'Antenna2_{side}',(s*.05,.19,.15),(s*.08,.24,.145),a1)
bone('AttackOrigin',(0,.14,.10),(0,.22,.10),hd); bone('LightFX',(0,0,.13),(0,0,.19),th); bone('HitCenter',(0,-.02,.09),(0,-.02,.15),root)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True

for o in [x for x in bpy.context.scene.objects if x.type=='MESH']:
    b=parts.get(o.name,'Thorax'); mod=o.modifiers.new('LanternMothArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=b); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
span=max(v.co.x+o.location.x for o in meshes for v in o.data.vertices)-min(v.co.x+o.location.x for o in meshes for v in o.data.vertices)
if not .45<=span<=.70: raise RuntimeError(f'Lantern Moth wingspan outside 0.45-0.70m: {span:.3f}')
if sum(1 for o in meshes if o.name.startswith('LM_ForeWing_') or o.name.startswith('LM_HindWing_'))!=4: raise RuntimeError('Lantern Moth must have four physical wing surfaces')
if any(not o.data.uv_layers.get('LanternMothUV') for o in meshes): raise RuntimeError('Lantern Moth mesh missing explicit LanternMothUV')
arm['host_family']='HOST-INSECT-FLY'; arm['production_span_m']=round(span,3); arm['magenheim_model_id']='underworld-creature-lantern-moth'
arm['material_language']='fungal-charcoal-chitin+soft-thorax+pale-membrane+localized-wing-light'
arm['silhouette_contract']='four-independent-wings+fuzzy-thorax+paired-antennae+six-legs;never-bat-body'
arm['planned_actions']='Hover,Flight,BankLeft,BankRight,Land,GroundIdle,Takeoff,AlertFlee,Hit,Death'
sc=bpy.context.scene; sc['magenheim_model_id']='underworld-creature-lantern-moth'; sc['magenheim_skinning']='rigid-segment-weighted'
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT))
print(f'AUTHORED Lantern Moth source: span={span:.3f}m meshes={len(meshes)} -> {OUT}')
