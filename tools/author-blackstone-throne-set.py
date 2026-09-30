#!/usr/bin/env python3
"""Author the Blackstone Throne modular structure set and the assembled Dark Throne boss site.

This is source-authoring, not runtime geometry generation. Every output is a real Blender source that
is exported through tools/export-model-assets.py into GLB + runtime model payloads.
"""
import json, math, sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
TEX=ROOT/"assets"/"textures"/"underworld"/"blackstone"
PBR=ROOT/"assets"/"material-source"/"blackstone"

IDS=(
 "blackstone-throne","blackstone-banner","blackstone-attendant-seat","blackstone-brazier",
 "blackstone-stair","blackstone-dais","blackstone-parapet","blackstone-bridge",
 "blackstone-arch","blackstone-cliff-edge","blackstone-floor-tile","blackstone-spire",
 "blackstone-pillar","blackstone-terrace","blackstone-wall-buttress",
 "blackstone-cathedral-wall","blackstone-gate","dark-throne",
)

def wloc(x,y,z): return (x,-z,y)
def wrot(rx,ry,rz): return (rx,rz,-ry)

def material(name,texture,pbr_key,metallic=0.0,rough=.72,emission=None,double_sided=False):
    path=TEX/texture
    if not path.is_file(): raise RuntimeError(f"Missing Blackstone source texture: {path}")
    mat=bpy.data.materials.new(name); mat.use_nodes=True; mat.use_backface_culling=not double_sided
    tree=mat.node_tree; bs=tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value=(1,1,1,1)
    bs.inputs["Metallic"].default_value=metallic; bs.inputs["Roughness"].default_value=rough
    image=bpy.data.images.load(str(path),check_existing=True)
    image.pack()
    node=tree.nodes.new("ShaderNodeTexImage"); node.name="BlackstoneSource"; node.image=image
    tree.links.new(node.outputs["Color"],bs.inputs["Base Color"])
    if emission and "Emission Color" in bs.inputs:
        bs.inputs["Emission Color"].default_value=(*emission,1)
        bs.inputs["Emission Strength"].default_value=2.0
    mat["magenheim_material_name"]=name
    mat["magenheim_material_source_key"]=pbr_key
    mat["magenheim_material_source_root"]="assets/material-source/blackstone"

    normal_path=PBR/(pbr_key+"-normal.png")
    packed_path=PBR/(pbr_key+"-metallic-smoothness.png")
    if not normal_path.is_file() or not packed_path.is_file():
        raise RuntimeError("Missing Blackstone PBR source for "+pbr_key)
    normal_image=bpy.data.images.load(str(normal_path),check_existing=True)
    normal_image.colorspace_settings.name="Non-Color"; normal_image.pack()
    normal_node=tree.nodes.new("ShaderNodeTexImage"); normal_node.name="Blackstone Normal"; normal_node.image=normal_image
    normal_map=tree.nodes.new("ShaderNodeNormalMap"); normal_map.name="Blackstone NormalMap"; normal_map.inputs["Strength"].default_value=.45
    tree.links.new(normal_node.outputs["Color"],normal_map.inputs["Color"])
    tree.links.new(normal_map.outputs["Normal"],bs.inputs["Normal"])

    packed_image=bpy.data.images.load(str(packed_path),check_existing=True)
    packed_image.colorspace_settings.name="Non-Color"; packed_image.pack()
    packed_node=tree.nodes.new("ShaderNodeTexImage"); packed_node.name="Blackstone MetallicSmoothness"; packed_node.image=packed_image
    separate=tree.nodes.new("ShaderNodeSeparateColor"); separate.name="Blackstone Packed Channels"
    tree.links.new(packed_node.outputs["Color"],separate.inputs["Color"])
    tree.links.new(separate.outputs["Red"],bs.inputs["Metallic"])
    invert=tree.nodes.new("ShaderNodeMath"); invert.name="Blackstone Smoothness To Roughness"; invert.operation="SUBTRACT"
    invert.inputs[0].default_value=1.0
    tree.links.new(packed_node.outputs["Alpha"],invert.inputs[1])
    tree.links.new(invert.outputs[0],bs.inputs["Roughness"])

    emission_path=PBR/(pbr_key+"-emission.png")
    if emission_path.is_file() and "Emission Color" in bs.inputs:
        emission_image=bpy.data.images.load(str(emission_path),check_existing=True); emission_image.pack()
        emission_node=tree.nodes.new("ShaderNodeTexImage"); emission_node.name="Blackstone Emission"; emission_node.image=emission_image
        tree.links.new(emission_node.outputs["Color"],bs.inputs["Emission Color"])
        if bs.inputs["Emission Strength"].default_value<=0:
            bs.inputs["Emission Strength"].default_value=.8
    return mat

def palette():
    return {
      "stone":material("blackstone.basalt","blackstone-basalt-albedo.png","blackstone-basalt",0.02,.78),
      "void":material("blackstone.voidstone","blackstone-voidstone-albedo.png","blackstone-voidstone",0.05,.66),
      "bronze":material("blackstone.royal-bronze","blackstone-bronze-albedo.png","blackstone-royal-bronze",.80,.32),
      "cloth":material("blackstone.banner-cloth","blackstone-banner-sun-albedo.png","blackstone-banner-cloth",0,.88,double_sided=True),
      "ember":material("blackstone.ember","blackstone-ember-albedo.png","blackstone-ember",.05,.28,(.55,.07,.01),double_sided=True),
      "flame":material("blackstone.flame","blackstone-flame-albedo.png","blackstone-flame",0,.20,(1.0,.20,.025),double_sided=True),
    }

def finish(o,name,mat,collision=True,bevel=.0,uv=.65):
    o.name=name; o["game_node_path"]=name; o["game_collision"]=bool(collision); o["game_crystal"]=json.dumps(None)
    o.data.materials.clear(); o.data.materials.append(mat)
    if not o.data.uv_layers: o.data.uv_layers.new(name="BlackstoneUV")
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=uv)
    bpy.ops.object.mode_set(mode="OBJECT"); o.select_set(False)
    if bevel>0:
        mod=o.modifiers.new("blackstone-bevel","BEVEL"); mod.width=bevel; mod.segments=2
    return o

def box(name,pos,size,mat,rot=(0,0,0),collision=True,bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1,location=wloc(*pos),rotation=wrot(*rot))
    o=bpy.context.object; o.scale=(size[0],size[2],size[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,collision,bevel,max(size)*.18+.3)

def cyl(name,pos,radius,height,mat,vertices=16,collision=True,bevel=.025):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=height,location=wloc(*pos))
    return finish(bpy.context.object,name,mat,collision,bevel,.55)

def cone(name,pos,r1,r2,height,mat,vertices=8,collision=True,rot=(0,0,0),bevel=.02):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=height,location=wloc(*pos),rotation=wrot(*rot))
    return finish(bpy.context.object,name,mat,collision,bevel,.5)

def torus(name,pos,major,minor,mat,collision=False):
    bpy.ops.mesh.primitive_torus_add(major_segments=24,minor_segments=8,major_radius=major,minor_radius=minor,location=wloc(*pos),rotation=(math.pi/2,0,0))
    return finish(bpy.context.object,name,mat,collision,.012,.5)

def rock(name,pos,scale,mat,collision=True,seed=0):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=wloc(*pos))
    o=bpy.context.object; o.scale=(scale[0],scale[2],scale[1]); o.rotation_euler=(.18*seed,.31*seed,.13*seed)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,collision,.03,.75)

def banner_cloth(name,top_center,width,height,mat,scale=1.0):
    """Weighted hanging standard with explicit one-tile UVs and modeled folds."""
    ox,top_y,oz=top_center
    cols=10; rows=12
    verts=[]; faces=[]; uvs=[]
    for row in range(rows+1):
        v=row/rows
        y=top_y-v*height*scale
        for col in range(cols+1):
            u=col/cols
            xn=u*2.0-1.0
            taper=1.0-.08*v
            x=ox+xn*(width*.5*scale)*taper
            hem=(.38*(abs(xn)**1.55)*scale) if row==rows else 0.0
            fold=(.105*math.sin(u*math.tau*3.0)+.028*math.sin(u*math.tau*6.0))*scale*(.30+.70*v)
            verts.append(wloc(x,y+hem,oz+fold))
            uvs.append((u,1.0-v))
    stride=cols+1
    for row in range(rows):
        for col in range(cols):
            a=row*stride+col; b=a+1; d=(row+1)*stride+col; c=d+1
            faces.extend(((a,d,c),(a,c,b)))
    mesh=bpy.data.meshes.new(name+"Mesh")
    mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    obj["game_node_path"]=name; obj["game_collision"]=False; obj["game_crystal"]=json.dumps(None)
    mesh.materials.append(mat)
    layer=mesh.uv_layers.new(name="BlackstoneUV")
    for poly in mesh.polygons:
        for loop_index in poly.loop_indices:
            vi=mesh.loops[loop_index].vertex_index
            layer.data[loop_index].uv=uvs[vi]
    return obj

def ray_boxes_floor(prefix,center,r1,r2,height,width,mat,count=16):
    cx,cy,cz=center
    for i in range(count):
        a=i*math.tau/count
        r=(r1+r2)/2
        x=cx+math.cos(a)*r; y=cy; z=cz+math.sin(a)*r
        box(f"{prefix}_Ray_{i+1}",(x,y,z),(width,height,r2-r1),mat,rot=(0,-a+math.pi/2,0),collision=False,bevel=.01)

def add_sun(prefix,center,mat,scale=1.0):
    # Royal sun is vertical in the X/Y plane with only a thin Z depth.
    cx,cy,cz=center
    torus(f"{prefix}_Ring",(cx,cy,cz),.64*scale,.075*scale,mat,False)
    cone(f"{prefix}_Disc",(cx,cy,cz),.43*scale,.43*scale,.06*scale,mat,24,False,rot=(math.pi/2,0,0),bevel=.008)
    for i in range(12):
        a=i*math.tau/12
        r=(.80+1.15)*.5*scale
        x=cx+math.cos(a)*r; y=cy+math.sin(a)*r
        box(f"{prefix}_Ray_{i+1}",(x,y,cz),(.07*scale,.35*scale,.06*scale),mat,
            rot=(0,0,a-math.pi/2),collision=False,bevel=.01)

def add_finial(prefix,pos,mat,scale=1.0):
    x,y,z=pos
    box(prefix+"_Base",(x,y,z),(0.34*scale,.18*scale,.34*scale),mat,collision=False,bevel=.02*scale)
    cone(prefix+"_Spike",(x,y+.42*scale,z),.18*scale,0,.66*scale,mat,6,False,bevel=.01)

def build_throne(m,prefix="Throne",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    box(prefix+"_Plinth",(ox,oy+.24*scale,oz),(5.5*scale,.48*scale,3.9*scale),m["void"],collision=True,bevel=.08*scale)
    box(prefix+"_Step",(ox,oy+.58*scale,oz-1.34*scale),(4.7*scale,.24*scale,1.05*scale),m["stone"],collision=True,bevel=.045*scale)
    box(prefix+"_SeatBase",(ox,oy+1.00*scale,oz-.05*scale),(2.75*scale,.34*scale,2.00*scale),m["void"],collision=True,bevel=.065*scale)
    box(prefix+"_Seat",(ox,oy+1.27*scale,oz-.10*scale),(2.35*scale,.28*scale,1.72*scale),m["stone"],collision=True,bevel=.10*scale)
    box(prefix+"_BackCore",(ox,oy+4.25*scale,oz+.82*scale),(2.18*scale,5.85*scale,.54*scale),m["void"],collision=True,bevel=.075*scale)
    box(prefix+"_BackInset",(ox,oy+4.10*scale,oz+.50*scale),(1.58*scale,4.70*scale,.18*scale),m["stone"],collision=False,bevel=.045*scale)
    for side in (-1,1):
        label="L" if side<0 else "R"
        box(f"{prefix}_Wing_{label}",(ox+side*1.48*scale,oy+4.10*scale,oz+.90*scale),(.52*scale,4.95*scale,.52*scale),m["stone"],collision=True,bevel=.045*scale)
        box(f"{prefix}_Pier_{label}",(ox+side*2.23*scale,oy+4.20*scale,oz+.78*scale),(.50*scale,6.65*scale,.62*scale),m["void"],collision=True,bevel=.045*scale)
        cone(f"{prefix}_Crown_{label}",(ox+side*2.23*scale,oy+8.05*scale,oz+.78*scale),.33*scale,0,1.38*scale,m["stone"],6,True,bevel=.022)
        box(f"{prefix}_Arm_{label}",(ox+side*1.48*scale,oy+1.86*scale,oz-.18*scale),(.38*scale,1.42*scale,2.15*scale),m["stone"],collision=True,bevel=.075*scale)
        box(f"{prefix}_ArmCap_{label}",(ox+side*1.48*scale,oy+2.60*scale,oz-.18*scale),(.48*scale,.12*scale,2.05*scale),m["bronze"],collision=False,bevel=.025*scale)
        box(f"{prefix}_FrontPost_{label}",(ox+side*1.48*scale,oy+1.05*scale,oz-1.00*scale),(.42*scale,1.65*scale,.42*scale),m["void"],collision=True,bevel=.045*scale)
        add_finial(f"{prefix}_FrontFinial_{label}",(ox+side*1.48*scale,oy+1.98*scale,oz-1.00*scale),m["bronze"],.44*scale)
    for i,xoff in enumerate((-1.18,-.59,0,.59,1.18)):
        h=(1.55+(2-abs(2-i))*.42)*scale
        base_y=oy+6.45*scale
        box(f"{prefix}_Spine_{i+1}",(ox+xoff*scale,base_y+h*.42,oz+1.18*scale),(.20*scale,h,.28*scale),m["stone"],collision=False,bevel=.02*scale)
        cone(f"{prefix}_SpineTip_{i+1}",(ox+xoff*scale,base_y+h+.28*scale,oz+1.18*scale),.15*scale,0,.62*scale,m["stone"],5,False,bevel=.008)
    cone(prefix+"_Apex",(ox,oy+8.55*scale,oz+1.18*scale),.42*scale,0,1.60*scale,m["void"],6,False,bevel=.015)
    add_sun(prefix+"_Sigil",(ox,oy+5.05*scale,oz+.49*scale),m["bronze"],.64*scale)

def build_banner(m,prefix="Banner",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    half=1.78*scale
    for side in (-1,1):
        label="L" if side<0 else "R"
        x=ox+side*half
        box(f"{prefix}_Foot_{label}",(x,oy+.18*scale,oz),(.72*scale,.36*scale,.72*scale),m["void"],collision=True,bevel=.055*scale)
        box(f"{prefix}_Pole_{label}",(x,oy+3.20*scale,oz),(.20*scale,6.25*scale,.20*scale),m["bronze"],collision=True,bevel=.025*scale)
        add_finial(f"{prefix}_Top_{label}",(x,oy+6.48*scale,oz),m["bronze"],.62*scale)
    box(prefix+"_Crossbar",(ox,oy+5.92*scale,oz),(3.95*scale,.18*scale,.24*scale),m["bronze"],collision=False,bevel=.025*scale)
    banner_cloth(prefix+"_Cloth",(ox,oy+5.72*scale,oz+.10*scale),3.28,5.02,m["cloth"],scale)

def build_seat(m,prefix="Seat",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    box(prefix+"_Base",(ox,oy+.22*scale,oz),(2.1*scale,.44*scale,2.2*scale),m["void"],collision=True,bevel=.06)
    box(prefix+"_Cushion",(ox,oy+.72*scale,oz-.12*scale),(1.55*scale,.35*scale,1.5*scale),m["stone"],collision=True,bevel=.10)
    box(prefix+"_Back",(ox,oy+2.35*scale,oz+.55*scale),(1.72*scale,3.1*scale,.42*scale),m["stone"],collision=True,bevel=.06)
    for side in (-1,1):
        box(f"{prefix}_Arm_{side}",(ox+side*.92*scale,oy+1.22*scale,oz-.08*scale),(.24*scale,1.35*scale,1.55*scale),m["bronze"],collision=False,bevel=.035)
        cone(f"{prefix}_Finial_{side}",(ox+side*.92*scale,oy+3.95*scale,oz+.55*scale),.17*scale,0,.70*scale,m["bronze"],6,False)
    add_sun(prefix+"_Sigil",(ox,oy+2.45*scale,oz+.30*scale),m["bronze"],.30*scale)

def build_brazier(m,prefix="Brazier",origin=(0,0,0),scale=1.0,flame=True):
    ox,oy,oz=origin
    cyl(prefix+"_Foot",(ox,oy+.14*scale,oz),.78*scale,.28*scale,m["void"],8,True,.045*scale)
    cyl(prefix+"_Plinth",(ox,oy+.43*scale,oz),.61*scale,.30*scale,m["stone"],8,True,.035*scale)
    cyl(prefix+"_Column",(ox,oy+1.28*scale,oz),.31*scale,1.38*scale,m["stone"],8,True,.035*scale)
    cyl(prefix+"_Collar",(ox,oy+1.91*scale,oz),.47*scale,.18*scale,m["bronze"],8,False,.025*scale)
    cyl(prefix+"_Basin",(ox,oy+2.20*scale,oz),.93*scale,.34*scale,m["bronze"],12,True,.045*scale)
    cyl(prefix+"_CoalBed",(ox,oy+2.39*scale,oz),.70*scale,.08*scale,m["ember"],12,False,.015*scale)
    for i,a in enumerate((0,math.pi/2,math.pi,math.pi*1.5)):
        x=ox+math.cos(a)*.72*scale; z=oz+math.sin(a)*.72*scale
        box(f"{prefix}_Brace_{i+1}",(x,oy+2.03*scale,z),(.13*scale,.46*scale,.34*scale),m["bronze"],rot=(0,-a,0),collision=False,bevel=.018*scale)
    if flame:
        for i,(dx,dz,h,r) in enumerate(((0,0,.82,.22),(.22,.05,.56,.14),(-.18,-.09,.64,.16))):
            cone(f"{prefix}_Flame_{i+1}",(ox+dx*scale,oy+(2.45+h*.46)*scale,oz+dz*scale),
                 r*scale,0,h*scale,m["flame"],7,False,rot=(0,0,(i*.31)),bevel=0)

def build_stair(m,prefix="Stair",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    steps=9
    for i in range(steps):
        y=oy+(i+.5)*.26*scale
        z=oz+(i-steps/2)*.72*scale
        box(f"{prefix}_Step_{i+1}",(ox,y,z),(7.5*scale,.26*scale,.78*scale),m["stone"],collision=True,bevel=.025)
    for side in (-1,1):
        box(f"{prefix}_Cheek_{side}",(ox+side*4.05*scale,oy+1.25*scale,oz),(.42*scale,2.6*scale,7.0*scale),m["void"],collision=True,bevel=.05)
        add_finial(f"{prefix}_FinialFront_{side}",(ox+side*4.05*scale,oy+2.7*scale,oz-3.05*scale),m["bronze"],.7*scale)

def build_dais(m,prefix="Dais",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    box(prefix+"_Lower",(ox,oy+.35*scale,oz),(16*scale,.70*scale,12*scale),m["stone"],collision=True,bevel=.08)
    box(prefix+"_Middle",(ox,oy+.82*scale,oz+1.2*scale),(12.8*scale,.48*scale,8.8*scale),m["void"],collision=True,bevel=.07)
    box(prefix+"_Upper",(ox,oy+1.22*scale,oz+2.4*scale),(9.2*scale,.34*scale,5.9*scale),m["stone"],collision=True,bevel=.06)
    for side in (-1,1):
        box(f"{prefix}_BronzeRail_{side}",(ox+side*6.6*scale,oy+1.05*scale,oz+.8*scale),(.16*scale,1.55*scale,8.7*scale),m["bronze"],collision=False,bevel=.025)

def build_parapet(m,prefix="Parapet",origin=(0,0,0),scale=1.0,length=7.5):
    ox,oy,oz=origin
    box(prefix+"_Foot",(ox,oy+.18*scale,oz),(length*scale,.36*scale,.56*scale),m["stone"],collision=True,bevel=.04)
    box(prefix+"_Rail",(ox,oy+1.35*scale,oz),(length*scale,.22*scale,.44*scale),m["bronze"],collision=True,bevel=.025)
    posts=max(3,int(length/1.5)+1)
    for i in range(posts):
        x=ox+(-length/2+i*(length/(posts-1)))*scale
        box(f"{prefix}_Post_{i+1}",(x,oy+.85*scale,oz),(.34*scale,1.55*scale,.48*scale),m["stone"],collision=True,bevel=.035)
        add_finial(f"{prefix}_Finial_{i+1}",(x,oy+1.7*scale,oz),m["bronze"],.48*scale)

def build_bridge(m,prefix="Bridge",origin=(0,0,0),scale=1.0,length=12.0):
    ox,oy,oz=origin
    box(prefix+"_Deck",(ox,oy,oz),(7.6*scale,.55*scale,length*scale),m["stone"],collision=True,bevel=.06)
    for side in (-1,1):
        build_parapet(m,f"{prefix}_Rail_{side}",(ox+side*3.65*scale,oy+.32*scale,oz),scale*.72,length/0.72)
    for zoff in (-length*.32,0,length*.32):
        for side in (-1,1):
            box(f"{prefix}_Support_{side}_{int((zoff+length)*10)}",(ox+side*2.75*scale,oy-2.0*scale,oz+zoff*scale),(.70*scale,3.5*scale,.70*scale),m["void"],collision=True,bevel=.05)

def build_arch(m,prefix="Arch",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    for side in (-1,1):
        box(f"{prefix}_Pier_{side}",(ox+side*2.55*scale,oy+2.3*scale,oz),(.82*scale,4.6*scale,1.05*scale),m["stone"],collision=True,bevel=.06)
        cone(f"{prefix}_PierTop_{side}",(ox+side*2.55*scale,oy+5.0*scale,oz),.46*scale,0,.90*scale,m["stone"],6,True)
    box(prefix+"_Lintel",(ox,oy+4.65*scale,oz),(5.9*scale,.72*scale,1.05*scale),m["void"],collision=True,bevel=.08)
    box(prefix+"_Crown",(ox,oy+5.35*scale,oz),(3.8*scale,.48*scale,.82*scale),m["bronze"],collision=False,bevel=.04)
    for side in (-1,1):
        box(f"{prefix}_InnerLeg_{side}",(ox+side*1.35*scale,oy+3.0*scale,oz),(.32*scale,2.6*scale,.62*scale),m["bronze"],collision=False,bevel=.025)

def build_cliff(m,prefix="Cliff",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    forms=((-2.5,-1.8,.0,1.8,3.2,2.0),(-.8,-2.4,.3,1.6,4.0,1.6),(1.0,-1.9,-.2,2.0,3.0,2.2),(2.5,-2.8,.4,1.3,4.8,1.5))
    for i,(x,y,z,sx,sy,sz) in enumerate(forms):
        rock(f"{prefix}_Rock_{i+1}",(ox+x*scale,oy+y*scale,oz+z*scale),(sx*scale,sy*scale,sz*scale),m["stone"],True,i+3)

def build_floor(m,prefix="Floor",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    box(prefix+"_Slab",(ox,oy,oz),(5.0*scale,.34*scale,5.0*scale),m["stone"],collision=True,bevel=.04)
    cyl(prefix+"_SunDisc",(ox,oy+.20*scale,oz),.56*scale,.055*scale,m["bronze"],24,False,.008)
    ray_boxes_floor(prefix+"_Sun",(ox,oy+.22*scale,oz),.78*scale,1.55*scale,.06*scale,.08*scale,m["bronze"],12)

def build_spire(m,prefix="Spire",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    box(prefix+"_Foot",(ox,oy+.22*scale,oz),(1.55*scale,.44*scale,1.55*scale),m["void"],collision=True,bevel=.06)
    box(prefix+"_Shaft",(ox,oy+2.9*scale,oz),(.72*scale,5.0*scale,.72*scale),m["stone"],collision=True,bevel=.04)
    cone(prefix+"_Crown",(ox,oy+6.0*scale,oz),.52*scale,0,1.35*scale,m["stone"],6,True)
    for side in (-1,1):
        box(f"{prefix}_Blade_{side}",(ox+side*.58*scale,oy+4.6*scale,oz),(.18*scale,3.2*scale,.38*scale),m["bronze"],collision=False,bevel=.018)

def build_pillar(m,prefix="Pillar",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    box(prefix+"_Base",(ox,oy+.28*scale,oz),(1.8*scale,.56*scale,1.8*scale),m["void"],collision=True,bevel=.06)
    box(prefix+"_Shaft",(ox,oy+2.6*scale,oz),(1.02*scale,4.1*scale,1.02*scale),m["stone"],collision=True,bevel=.05)
    box(prefix+"_Capital",(ox,oy+4.82*scale,oz),(1.75*scale,.48*scale,1.75*scale),m["bronze"],collision=True,bevel=.05)
    cone(prefix+"_Finial",(ox,oy+5.65*scale,oz),.42*scale,0,1.25*scale,m["stone"],6,True)


def build_terrace(m,prefix="Terrace",origin=(0,0,0),scale=1.0,width=18.0,depth=10.0):
    """Suspended combat/processional terrace with a readable structural load path."""
    ox,oy,oz=origin
    box(prefix+"_Deck",(ox,oy,oz),(width*scale,.72*scale,depth*scale),m["stone"],collision=True,bevel=.07)
    box(prefix+"_Underdeck",(ox,oy-.66*scale,oz),(width*.88*scale,.62*scale,depth*.88*scale),m["void"],collision=True,bevel=.06)
    for side in (-1,1):
        x=ox+side*(width*.5-.42)*scale
        build_arena_rail(m,f"{prefix}_SideRail_{side}",(x,oy+.48*scale,oz),max(3.5,depth-.8)*scale,"z")
    build_arena_rail(m,prefix+"_RearRail",(ox,oy+.48*scale,oz+(depth*.5-.42)*scale),max(4.0,width-1.0)*scale,"x")
    for side in (-1,1):
        for fore in (-1,1):
            x=ox+side*(width*.5-1.25)*scale
            z=oz+fore*(depth*.5-1.25)*scale
            box(f"{prefix}_Support_{side}_{fore}",(x,oy-3.0*scale,z),(1.05*scale,5.4*scale,1.05*scale),m["void"],collision=True,bevel=.05)
            cone(f"{prefix}_SupportCrown_{side}_{fore}",(x,oy-.08*scale,z),.58*scale,.30*scale,.80*scale,m["bronze"],6,False)

def build_buttress(m,prefix="Buttress",origin=(0,0,0),scale=1.0):
    """Tall cathedral buttress used to create the vertical wall rhythm in the reference hall."""
    ox,oy,oz=origin
    box(prefix+"_Foot",(ox,oy+.45*scale,oz),(3.0*scale,.90*scale,3.2*scale),m["void"],collision=True,bevel=.09)
    box(prefix+"_Lower",(ox,oy+4.6*scale,oz),(1.75*scale,7.8*scale,2.0*scale),m["stone"],collision=True,bevel=.07)
    box(prefix+"_Upper",(ox,oy+10.0*scale,oz),(1.15*scale,5.2*scale,1.45*scale),m["stone"],collision=True,bevel=.05)
    box(prefix+"_BronzeSpine",(ox,oy+8.8*scale,oz-.78*scale),(.20*scale,10.8*scale,.18*scale),m["bronze"],collision=False,bevel=.018)
    for side in (-1,1):
        box(f"{prefix}_Shoulder_{side}",(ox+side*1.15*scale,oy+6.9*scale,oz),(.72*scale,3.4*scale,1.25*scale),m["void"],collision=True,bevel=.05)
        cone(f"{prefix}_ShoulderCrown_{side}",(ox+side*1.15*scale,oy+9.0*scale,oz),.36*scale,0,1.25*scale,m["stone"],6,True)
    cone(prefix+"_Crown",(ox,oy+13.1*scale,oz),.72*scale,0,2.7*scale,m["stone"],6,True)

def build_cathedral_wall(m,prefix="CathedralWall",origin=(0,0,0),scale=1.0,length=12.0,axis="x"):
    """Massive wall panel; broad stone stays broad while ribs, fins and sigil carry the detail."""
    ox,oy,oz=origin
    if axis=="x":
        size=(length*scale,18.0*scale,1.15*scale)
        rib_positions=[(ox+(-length*.42+i*length*.21)*scale,oz) for i in range(5)]
        sun=(ox,oy+11.2*scale,oz-.72*scale)
    else:
        size=(1.15*scale,18.0*scale,length*scale)
        rib_positions=[(ox,oz+(-length*.42+i*length*.21)*scale) for i in range(5)]
        sun=(ox-.72*scale,oy+11.2*scale,oz)
    box(prefix+"_Wall",(ox,oy+9.0*scale,oz),size,m["void"],collision=True,bevel=.08)
    for i,(x,z) in enumerate(rib_positions):
        if axis=="x":
            box(f"{prefix}_Rib_{i+1}",(x,oy+9.4*scale,oz-.66*scale),(.34*scale,15.6*scale,.28*scale),m["stone"],collision=False,bevel=.025)
        else:
            box(f"{prefix}_Rib_{i+1}",(ox-.66*scale,oy+9.4*scale,z),(.28*scale,15.6*scale,.34*scale),m["stone"],collision=False,bevel=.025)
        cone(f"{prefix}_Pinnacle_{i+1}",(x,oy+18.2*scale,z),.24*scale,0,1.5*scale,m["stone"],6,False)
    add_sun(prefix+"_RoyalSun",sun,m["bronze"],.88*scale)

def build_gate(m,prefix="Gate",origin=(0,0,0),scale=1.0):
    """Monumental threshold framing the player entry into the throne complex."""
    ox,oy,oz=origin
    for side in (-1,1):
        box(f"{prefix}_Tower_{side}",(ox+side*4.8*scale,oy+5.3*scale,oz),(2.3*scale,10.6*scale,2.5*scale),m["stone"],collision=True,bevel=.08)
        build_buttress(m,f"{prefix}_Buttress_{side}",(ox+side*6.1*scale,oy,oz+.2*scale),.72*scale)
        cone(f"{prefix}_TowerCrown_{side}",(ox+side*4.8*scale,oy+11.2*scale,oz),.86*scale,0,2.4*scale,m["stone"],6,True)
    box(prefix+"_Lintel",(ox,oy+9.8*scale,oz),(8.8*scale,1.2*scale,2.0*scale),m["void"],collision=True,bevel=.08)
    box(prefix+"_BronzeLintel",(ox,oy+9.65*scale,oz-1.08*scale),(7.1*scale,.28*scale,.18*scale),m["bronze"],collision=False,bevel=.025)
    add_sun(prefix+"_RoyalSun",(ox,oy+11.4*scale,oz-1.18*scale),m["bronze"],1.05*scale)

def build_cross_bridge(m,prefix,origin,length=10.0,width=5.2):
    """Horizontal gallery bridge connecting central terraces to the high side walks."""
    ox,oy,oz=origin
    box(prefix+"_Deck",(ox,oy,oz),(length,.52,width),m["stone"],collision=True,bevel=.055)
    for side in (-1,1):
        build_arena_rail(m,f"{prefix}_Rail_{side}",(ox,oy+.35,oz+side*(width*.5-.30)),length-.5,"x")
    for side in (-1,1):
        box(f"{prefix}_Pier_{side}",(ox+side*(length*.36),oy-2.4,oz),(.78,4.6,.78),m["void"],collision=True,bevel=.05)

def build_arena_rail(m,prefix,origin,length=9.0,axis="z"):
    ox,oy,oz=origin
    if axis=="z":
        box(prefix+"_Foot",(ox,oy,oz),(.52,.34,length),m["stone"],collision=True,bevel=.035)
        box(prefix+"_Rail",(ox,oy+1.18,oz),(.34,.20,length),m["bronze"],collision=True,bevel=.022)
        ends=((ox,oz-length/2),(ox,oz+length/2))
    else:
        box(prefix+"_Foot",(ox,oy,oz),(length,.34,.52),m["stone"],collision=True,bevel=.035)
        box(prefix+"_Rail",(ox,oy+1.18,oz),(length,.20,.34),m["bronze"],collision=True,bevel=.022)
        ends=((ox-length/2,oz),(ox+length/2,oz))
    for i,(x,z) in enumerate(ends):
        box(f"{prefix}_Post_{i+1}",(x,oy+.72,z),(.42,1.45,.42),m["stone"],collision=True,bevel=.035)
        cone(f"{prefix}_Finial_{i+1}",(x,oy+1.68,z),.22,0,.58,m["bronze"],6,False)

def build_assembled(m):
    # The 52x60 X/Z authority remains the Nowhere King leash, NOT a giant walkable floor.
    # A non-walkable deep foundation records that footprint beneath the suspended throne complex.
    box("Arena_Foundation",(0,-19.5,0),(52,1.0,60),m["void"],collision=False,bevel=.08)

    # Lower threshold and approach: player entry over a bridge with open abyss on both sides.
    build_gate(m,"Gate_Approach",(0,-.1,-29.0),.82)
    build_bridge(m,"Bridge_LowerApproach",(0,.55,-25.0),.92,8.5)
    build_terrace(m,"LowerTerrace",(0,1.55,-18.2),1.0,26.0,9.5)

    # First climb to the intermediate battle/processional terrace.
    build_stair(m,"GrandStair_Lower",(0,1.75,-10.5),1.45)
    build_terrace(m,"IntermediateTerrace",(0,5.35,-2.1),1.0,32.0,13.0)

    # Second climb creates the strong vertical break visible in the reference composition.
    build_stair(m,"GrandStair_Upper",(0,5.55,6.8),1.58)
    build_terrace(m,"UpperTerrace",(0,9.55,14.2),1.0,36.0,15.0)

    # Throne platform is another level above the actual boss-combat terrace.
    build_stair(m,"GrandStair_Throne",(0,9.80,20.3),1.06)
    build_terrace(m,"ThronePlatform",(0,12.75,25.0),1.0,24.0,8.5)
    build_dais(m,"Dais",(0,13.25,24.6),.72)
    build_throne(m,"Throne",(0,14.20,26.0),.96)

    # Court furniture and standards sit on the throne level.
    build_seat(m,"Seat_West",(-6.6,13.35,23.8),.80)
    build_seat(m,"Seat_East",(6.6,13.35,23.8),.80)
    build_banner(m,"Banner_West",(-10.0,12.85,27.0),1.08)
    build_banner(m,"Banner_East",(10.0,12.85,27.0),1.08)

    # Ceremonial fire marks each elevation and stays individually addressable for phase changes.
    brazier_positions=[
        (-9.0,2.05,-18.0),(9.0,2.05,-18.0),
        (-13.0,5.85,-5.0),(13.0,5.85,-5.0),(-13.0,5.85,1.0),(13.0,5.85,1.0),
        (-15.0,10.05,10.2),(15.0,10.05,10.2),(-15.0,10.05,17.8),(15.0,10.05,17.8),
        (-9.0,13.25,24.0),(9.0,13.25,24.0),
    ]
    for i,pos in enumerate(brazier_positions):
        build_brazier(m,f"Brazier_{i+1}",pos,.78 if i<10 else .88,True)

    # Boss rune circuit belongs to the upper combat terrace in front of the throne climb.
    for i in range(8):
        angle=i*math.tau/8
        x=math.sin(angle)*8.0
        z=14.5+math.cos(angle)*4.1
        box(f"Rune_{i+1}",(x,9.98,z),(.34,.08,1.55),m["bronze"],rot=(0,-angle,0),collision=True,bevel=.018)

    # Stacked side galleries and bridge links make the hall a traversable vertical structure.
    for side in (-1,1):
        build_terrace(m,f"SideLowerTerrace_{side}",(side*21.2,3.45,-4.5),.72,8.0,19.0)
        build_terrace(m,f"SideUpperTerrace_{side}",(side*22.0,8.55,13.2),.72,7.6,19.5)
        build_cross_bridge(m,f"Bridge_Intermediate_{side}",(side*18.0,5.55,-1.5),7.5,4.4)
        build_cross_bridge(m,f"Bridge_Upper_{side}",(side*20.0,9.70,14.0),8.0,4.2)

    # Cathedral side walls and buttresses rise well above the encounter tiers.
    for side in (-1,1):
        x=side*28.0
        for row,z in enumerate((-18.0,-4.0,10.0,24.0)):
            build_cathedral_wall(m,f"CathedralWall_{side}_{row}",(x,1.0,z),.88,12.0,"z")
        for row,z in enumerate((-25.0,-13.0,-1.0,11.0,23.0)):
            build_buttress(m,f"Buttress_{side}_{row}",(side*27.2,.2,z),1.35 if row in (1,3) else 1.18)
        for row,z in enumerate((-17.0,-5.0,7.0,19.0)):
            build_arena_rail(m,f"HighGalleryRail_{side}_{row}",(side*25.4,14.4,z),10.5,"z")

    # Rear wall becomes the architectural crown behind the throne instead of empty sky.
    for col,x in enumerate((-18.0,-6.0,6.0,18.0)):
        build_cathedral_wall(m,f"RearWall_{col}",(x,5.0,30.0),.96,11.5,"x")
    for col,x in enumerate((-22.0,-14.5,-7.0,0,7.0,14.5,22.0)):
        build_spire(m,f"Spire_Rear_{col}",(x,12.0,28.5),2.10 if x else 2.80)

    # Deep understructure and cliff teeth keep the open volume beneath the platforms legible.
    for side in (-1,1):
        for row,z in enumerate((-17.0,-3.0,11.0,24.0)):
            build_arch(m,f"Arch_Under_{side}_{row}",(side*19.5,-5.5,z),1.05)
            build_cliff(m,f"Cliff_{side}_{row}",(side*25.0,-10.0,z),1.30)


BUILD={
 "blackstone-throne":lambda m:build_throne(m),
 "blackstone-banner":lambda m:build_banner(m),
 "blackstone-attendant-seat":lambda m:build_seat(m),
 "blackstone-brazier":lambda m:build_brazier(m),
 "blackstone-stair":lambda m:build_stair(m),
 "blackstone-dais":lambda m:build_dais(m),
 "blackstone-parapet":lambda m:build_parapet(m),
 "blackstone-bridge":lambda m:build_bridge(m),
 "blackstone-arch":lambda m:build_arch(m),
 "blackstone-cliff-edge":lambda m:build_cliff(m),
 "blackstone-floor-tile":lambda m:build_floor(m),
 "blackstone-spire":lambda m:build_spire(m),
 "blackstone-pillar":lambda m:build_pillar(m),
 "blackstone-terrace":lambda m:build_terrace(m),
 "blackstone-wall-buttress":lambda m:build_buttress(m),
 "blackstone-cathedral-wall":lambda m:build_cathedral_wall(m),
 "blackstone-gate":lambda m:build_gate(m),
 "dark-throne":build_assembled,
}

DETAIL_FLOOR={
 "blackstone-throne":20,"blackstone-banner":19,"blackstone-attendant-seat":8,"blackstone-brazier":10,
 "blackstone-stair":12,"blackstone-dais":5,"blackstone-parapet":10,"blackstone-bridge":20,
 "blackstone-arch":8,"blackstone-cliff-edge":4,"blackstone-floor-tile":8,"blackstone-spire":5,
 "blackstone-pillar":4,"blackstone-terrace":18,"blackstone-wall-buttress":8,
 "blackstone-cathedral-wall":14,"blackstone-gate":20,"dark-throne":360,
}

def author(model_id):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    m=palette(); BUILD[model_id](m)
    meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
    if len(meshes)<DETAIL_FLOOR[model_id]:
        raise RuntimeError(f"{model_id}: detail regression {len(meshes)} < {DETAIL_FLOOR[model_id]}")
    for o in meshes:
        if not o.data.uv_layers or not o.data.uv_layers.active.data:
            raise RuntimeError(f"{model_id}/{o.name}: Blackstone UV missing")
    scene=bpy.context.scene
    scene["model_id"]=model_id
    scene["magenheim_family"]="blackstone-throne-set"
    scene["magenheim_fidelity"]="blackstone-throne-r2-vertical-hall"
    if model_id=="dark-throne":
        lights=[]
        brazier_lights=[
            (-9.0,4.15,-18.0),(9.0,4.15,-18.0),
            (-13.0,7.95,-5.0),(13.0,7.95,-5.0),(-13.0,7.95,1.0),(13.0,7.95,1.0),
            (-15.0,12.15,10.2),(15.0,12.15,10.2),(-15.0,12.15,17.8),(15.0,12.15,17.8),
            (-9.0,15.65,24.0),(9.0,15.65,24.0),
        ]
        for i,(x,y,z) in enumerate(brazier_lights):
            lights.append({"path":f"Brazier_{i+1}","position":[x,y,z],"color":[1.0,.24,.045],"range":9.0,"intensity":2.25})
        for i in range(8):
            angle=i*math.tau/8
            lights.append({"path":f"Rune_{i+1}","position":[math.sin(angle)*8.0,10.18,14.5+math.cos(angle)*4.1],"color":[.32,.07,.48],"range":3.8,"intensity":.78})
        scene["runtime_lights"]=json.dumps(lights)
    elif model_id=="blackstone-brazier":
        scene["runtime_lights"]=json.dumps([{"path":"Brazier","position":[0,3.0,0],"color":[1.0,.24,.045],"range":6.0,"intensity":2.0}])
    bpy.context.preferences.filepaths.save_version=0
    target=SOURCE/(model_id+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
    print("AUTHORED",model_id,len(meshes),"parts",flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(IDS)
unknown=[x for x in requested if x not in IDS]
if unknown: raise SystemExit("Unknown Blackstone model(s): "+", ".join(unknown))
for model_id in requested: author(model_id)
