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

IDS=(
 "blackstone-throne","blackstone-banner","blackstone-attendant-seat","blackstone-brazier",
 "blackstone-stair","blackstone-dais","blackstone-parapet","blackstone-bridge",
 "blackstone-arch","blackstone-cliff-edge","blackstone-floor-tile","blackstone-spire",
 "blackstone-pillar","dark-throne",
)

def wloc(x,y,z): return (x,-z,y)
def wrot(rx,ry,rz): return (rx,rz,-ry)

def material(name,texture,metallic=0.0,rough=.72,emission=None,double_sided=False):
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
    return mat

def palette():
    return {
      "stone":material("blackstone.basalt","blackstone-basalt-albedo.png",0.02,.78),
      "void":material("blackstone.voidstone","blackstone-voidstone-albedo.png",0.05,.66),
      "bronze":material("blackstone.royal-bronze","blackstone-bronze-albedo.png",.80,.32),
      "cloth":material("blackstone.banner-cloth","blackstone-banner-sun-albedo.png",0,.82,double_sided=True),
      "ember":material("blackstone.ember","blackstone-ember-albedo.png",.05,.22,(1.0,.18,.025),double_sided=True),
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
    box(prefix+"_Plinth",(ox,oy+.25*scale,oz),(5.8*scale,.5*scale,4.1*scale),m["void"],collision=True,bevel=.08*scale)
    box(prefix+"_Step",(ox,oy+.62*scale,oz-1.35*scale),(4.8*scale,.28*scale,1.1*scale),m["stone"],collision=True,bevel=.05*scale)
    box(prefix+"_Seat",(ox,oy+1.28*scale,oz+.10*scale),(2.6*scale,.44*scale,2.1*scale),m["stone"],collision=True,bevel=.08*scale)
    box(prefix+"_Back",(ox,oy+4.25*scale,oz+.95*scale),(3.1*scale,6.2*scale,.72*scale),m["void"],collision=True,bevel=.10*scale)
    for side in (-1,1):
        x=ox+side*1.85*scale
        box(f"{prefix}_Arm_{'L' if side<0 else 'R'}",(x,oy+2.0*scale,oz-.05*scale),(.68*scale,2.25*scale,2.6*scale),m["stone"],collision=True,bevel=.08*scale)
        box(f"{prefix}_Pier_{'L' if side<0 else 'R'}",(ox+side*2.45*scale,oy+4.35*scale,oz+.75*scale),(.56*scale,7.4*scale,.70*scale),m["stone"],collision=True,bevel=.05*scale)
        cone(f"{prefix}_Crown_{'L' if side<0 else 'R'}",(ox+side*2.45*scale,oy+8.25*scale,oz+.75*scale),.34*scale,0,1.45*scale,m["stone"],6,True,bevel=.025)
    for i,xoff in enumerate((-1.30,-.65,0,.65,1.30)):
        h=(2.0+abs(2-i)*.55)*scale
        box(f"{prefix}_Spine_{i+1}",(ox+xoff*scale,oy+6.4*scale+h*.25,oz+1.30*scale),(.23*scale,h,.32*scale),m["stone"],collision=False,bevel=.025*scale)
        cone(f"{prefix}_SpineTip_{i+1}",(ox+xoff*scale,oy+6.4*scale+h*.78,oz+1.30*scale),.16*scale,0,.58*scale,m["stone"],5,False,bevel=.01)
    add_sun(prefix+"_Sigil",(ox,oy+5.65*scale,oz+.54*scale),m["bronze"],.72*scale)

def build_banner(m,prefix="Banner",origin=(0,0,0),scale=1.0):
    ox,oy,oz=origin
    box(prefix+"_Pole",(ox,oy+3.1*scale,oz),(.16*scale,6.2*scale,.16*scale),m["bronze"],collision=True,bevel=.025)
    box(prefix+"_Crossbar",(ox,oy+5.75*scale,oz),(3.1*scale,.14*scale,.18*scale),m["bronze"],collision=False,bevel=.02)
    box(prefix+"_Cloth",(ox,oy+3.45*scale,oz+.08*scale),(2.55*scale,4.25*scale,.08*scale),m["cloth"],collision=False,bevel=.015)
    add_finial(prefix+"_Top",(ox,oy+6.35*scale,oz),m["bronze"],.72*scale)
    add_sun(prefix+"_RaisedSigil",(ox,oy+3.9*scale,oz+.18*scale),m["bronze"],.45*scale)

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
    box(prefix+"_Foot",(ox,oy+.15*scale,oz),(1.65*scale,.30*scale,1.65*scale),m["void"],collision=True,bevel=.07)
    box(prefix+"_Plinth",(ox,oy+.50*scale,oz),(1.28*scale,.42*scale,1.28*scale),m["stone"],collision=True,bevel=.05)
    box(prefix+"_Column",(ox,oy+1.35*scale,oz),(.72*scale,1.30*scale,.72*scale),m["stone"],collision=True,bevel=.05)
    box(prefix+"_Bowl",(ox,oy+2.18*scale,oz),(1.75*scale,.42*scale,1.75*scale),m["bronze"],collision=True,bevel=.07)
    for side in (-1,1):
        box(f"{prefix}_LipX_{side}",(ox+side*.94*scale,oy+2.33*scale,oz),(.14*scale,.27*scale,1.95*scale),m["bronze"],collision=False,bevel=.02)
        box(f"{prefix}_LipZ_{side}",(ox,oy+2.33*scale,oz+side*.94*scale),(1.95*scale,.27*scale,.14*scale),m["bronze"],collision=False,bevel=.02)
    if flame:
        for i,(dx,dz,h) in enumerate(((0,0,1.2),(.28,.08,.85),(-.22,-.12,.92),(.10,-.26,.72))):
            cone(f"{prefix}_Flame_{i+1}",(ox+dx*scale,oy+(2.62+h*.45)*scale,oz+dz*scale),(.28 if i==0 else .18)*scale,0,h*scale,m["ember"],7,False,rot=(0,0,(i*17)%31),bevel=0)

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
    # Solid legal combat plate: 52x60 m, matching DarkThroneArena.CreateDefault().
    box("Arena_Foundation",(0,-.65,0),(52,1.30,60),m["stone"],collision=True,bevel=.10)
    # Tile the visible combat surface so the huge plate does not read as one stretched texture.
    n=0
    for x in (-18,-12,-6,0,6,12,18):
        for z in (-19,-13,-7,-1,5,11):
            n+=1; box(f"Floor_Tile_{n}",(x,.12,z),(5.75,.24,5.75),m["stone"],collision=True,bevel=.035)
    # Formal approach and central dais.
    build_bridge(m,"Bridge_Approach",(0,.52,-23),1.0,12)
    build_stair(m,"Dais_Stair",(0,.55,10.8),1.15)
    build_dais(m,"Dais",(0,1.10,19.0),1.0)
    build_throne(m,"Throne",(0,2.1,24.0),1.18)
    # Attendant seats and standards.
    build_seat(m,"Seat_West",(-6.8,2.35,20.9),.82)
    build_seat(m,"Seat_East",(6.8,2.35,20.9),.82)
    build_banner(m,"Banner_West",(-10.2,1.5,26.6),1.10)
    build_banner(m,"Banner_East",(10.2,1.5,26.6),1.10)
    # Eight ceremonial braziers retained as the encounter's future extinguish/relight family.
    brazier_positions=[(-11,.8,-12),(11,.8,-12),(-19,.8,-1),(19,.8,-1),(-15,1.0,11),(15,1.0,11),(-8,2.0,18),(8,2.0,18)]
    for i,pos in enumerate(brazier_positions): build_brazier(m,f"Brazier_{i+1}",pos,.82,True)
    # Rune circuit remains grouped under Dais_ by DarkThroneVisuals and surrounds the King home.
    for i in range(8):
        a=i*math.tau/8
        x=math.sin(a)*7.4; z=22+math.cos(a)*3.2
        box(f"Rune_{i+1}",(x,2.34,z),(.32,.08,1.45),m["bronze"],rot=(0,-a,0),collision=True,bevel=.018)
    # Side processional terraces and strong parapet silhouette.
    for side in (-1,1):
        box(f"Terrace_{side}",(side*20,.20,3.0),(8.0,.55,34.0),m["void"],collision=True,bevel=.07)
        for row,z in enumerate((-14,-4,6,16)):
            build_pillar(m,f"Pillar_{side}_{row}",(side*21.5,.45,z),.72 if row%2 else .82)
        for row,z in enumerate((-10,4,14)):
            build_arch(m,f"Arch_{side}_{row}",(side*20,-3.15,z),.75)
    # Perimeter rails retain the Blackstone silhouette without exploding the assembled site into
    # hundreds of tiny GameObjects; the standalone parapet asset carries the full ornamental version.
    for side in (-1,1):
        for row,z in enumerate((-18,-6,6,18)):
            build_arena_rail(m,f"Parapet_X_{side}_{row}",(side*24.2,.55,z),9.0,"z")
    for side in (-1,1):
        for col,x in enumerate((-15,-5,5,15)):
            build_arena_rail(m,f"Parapet_Z_{side}_{col}",(x,.55,side*27.9),9.0,"x")
    # Rear skyline and broken abyss-edge geology.
    for i,x in enumerate((-18,-13,-8,8,13,18)):
        build_spire(m,f"Spire_Rear_{i+1}",(x,.3,27.0),.80+(.10*(i%3)))
    build_cliff(m,"Cliff_West",(-22,-1.0,24),1.0)
    build_cliff(m,"Cliff_East",(22,-1.0,24),1.0)

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
 "dark-throne":build_assembled,
}

DETAIL_FLOOR={
 "blackstone-throne":20,"blackstone-banner":20,"blackstone-attendant-seat":8,"blackstone-brazier":10,
 "blackstone-stair":12,"blackstone-dais":5,"blackstone-parapet":10,"blackstone-bridge":20,
 "blackstone-arch":8,"blackstone-cliff-edge":4,"blackstone-floor-tile":8,"blackstone-spire":5,
 "blackstone-pillar":4,"dark-throne":170,
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
    scene["magenheim_fidelity"]="blackstone-throne-r1"
    if model_id=="dark-throne":
        lights=[]
        for i,(x,y,z) in enumerate([(-11,3.4,-12),(11,3.4,-12),(-19,3.4,-1),(19,3.4,-1),(-15,3.6,11),(15,3.6,11),(-8,4.6,18),(8,4.6,18)]):
            lights.append({"path":f"Brazier_{i+1}","position":[x,y,z],"color":[1.0,.24,.045],"range":8.5,"intensity":2.2})
        for i in range(8):
            a=i*math.tau/8
            lights.append({"path":f"Rune_{i+1}","position":[math.sin(a)*7.4,2.5,22+math.cos(a)*3.2],"color":[.32,.07,.48],"range":3.4,"intensity":.7})
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
