#!/usr/bin/env python3
"""Author three owned Sulfurous-Wastes geothermal vents for worldgen and station siting.

The vents are persistent geological features, not recoloured donor rocks. Each variant has explicit
UVs, solid slag/iron collision, an open emissive throat and enough secondary structure to read from
both exploration distance and Furnace-Heart placement distance.
"""
import json,math,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
IDS=("underworld-geothermal-vent-crown","underworld-geothermal-vent-split","underworld-geothermal-vent-rootbound")
DETAIL_FLOOR=34

def material(name,colour,metal=0.0,rough=.72,emission=None):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=True
    bs=m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value=(*colour,1);bs.inputs["Metallic"].default_value=metal;bs.inputs["Roughness"].default_value=rough
    if emission and "Emission Color" in bs.inputs:
        bs.inputs["Emission Color"].default_value=(*emission,1);bs.inputs["Emission Strength"].default_value=1.35
    bind_underworld_material(bpy,m,name);return m

def finish(o,name,mat,collision):
    o.name=name;o["game_node_path"]=name;o["game_collision"]=collision;o["game_crystal"]=json.dumps(None)
    o.data.materials.clear();o.data.materials.append(mat)
    if o.data.uv_layers.get("VentUV") is None:o.data.uv_layers.new(name="VentUV")
    o.data.uv_layers.active_index=o.data.uv_layers.find("VentUV")
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode="EDIT");bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=.42);bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False)
    return o

def rock(name,loc,scale,mat,rot=(0,0,0),collision=True):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=loc)
    o=bpy.context.object;o.scale=scale;o.rotation_euler=rot;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,collision)

def cone(name,loc,r1,r2,depth,mat,rot=(0,0,0),vertices=18,collision=True):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=depth,location=loc,rotation=rot)
    o=bpy.context.object
    mod=o.modifiers.new("weathered rim","BEVEL");mod.width=.04;mod.segments=2
    return finish(o,name,mat,collision)

def tube(name,a,b,r,mat,vertices=12,collision=True):
    av=Vector(a);bv=Vector(b);d=bv-av
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length,location=(av+bv)*.5)
    o=bpy.context.object;o.rotation_mode="QUATERNION";o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
    return finish(o,name,mat,collision)

def torus(name,loc,major,minor,mat,rot=(0,0,0),collision=True):
    bpy.ops.mesh.primitive_torus_add(major_segments=24,minor_segments=8,major_radius=major,minor_radius=minor,location=loc,rotation=rot)
    return finish(bpy.context.object,name,mat,collision)

def throat(name,loc,radius,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=18,ring_count=10,radius=radius,location=loc)
    return finish(bpy.context.object,name,mat,False)

def palette():
    return {
      "slag":material("underworld.geothermal-vent.slagstone",(.18,.14,.12),0,.82),
      "iron":material("underworld.geothermal-vent.emberiron",(.27,.15,.09),.84,.30),
      "root":material("underworld.geothermal-vent.charred-root",(.14,.065,.03),0,.88),
      "heat":material("underworld.geothermal-vent.heat",(.90,.22,.025),.03,.18,(.92,.16,.01)),
    }

def common_base(m,seed):
    for i in range(14):
        a=(i/14)*math.tau+.17*seed
        r=1.05+.30*math.sin(i*2.17+seed)
        z=.16+.05*(i%3)
        rock(f"slag-foot-{i}",(math.cos(a)*r,math.sin(a)*r,z),(.50,.34,.24),m["slag"],(0,.18*math.sin(a),a),True)
    for i in range(8):
        a=(i/8)*math.tau+.31
        tube(f"iron-vein-{i}",(math.cos(a)*.55,math.sin(a)*.55,.28),(math.cos(a)*1.22,math.sin(a)*1.22,.34+.11*(i%2)),.035,m["iron"],8,False)

def build_crown(m):
    common_base(m,1)
    cone("main-chimney",(0,0,.92),.88,.54,1.48,m["slag"],vertices=22)
    torus("emberiron-rim",(0,0,1.62),.60,.075,m["iron"])
    throat("live-throat",(0,0,1.54),.43,m["heat"])
    for i in range(12):
        a=i*math.tau/12
        tube(f"crown-spire-{i}",(math.cos(a)*.70,math.sin(a)*.70,1.18),(math.cos(a)*(.93+.08*(i%2)),math.sin(a)*(.93+.08*(i%2)),1.88+.12*(i%3)),.07,m["slag"],10,True)
    for i in range(6):
        a=i*math.tau/6+.22
        throat(f"heat-fissure-{i}",(math.cos(a)*.62,math.sin(a)*.62,.72+.06*(i%2)),.10,m["heat"])

def build_split(m):
    common_base(m,2)
    for side in (-1,1):
        cone(f"split-stack-{side}",(side*.48,0,1.04),.66,.34,1.82,m["slag"],(0,side*.10,side*.06),20,True)
        torus(f"stack-band-{side}",(side*.48,0,1.72),.39,.055,m["iron"],collision=False)
        throat(f"stack-throat-{side}",(side*.48,0,1.78),.29,m["heat"])
    for i in range(7):
        x=-.72+i*.24
        tube(f"bridge-rib-{i}",(x,-.48,.55),(x,.48,.82+.08*math.sin(i)),.045,m["iron"],8,False)
    for i in range(8):
        a=i*math.tau/8+.18
        rock(f"split-shard-{i}",(math.cos(a)*1.16,math.sin(a)*1.16,.57),(.24,.18,.52),m["slag"],(a,.15,a*.3),True)

def build_rootbound(m):
    common_base(m,3)
    cone("broken-vent",(0,0,.86),.92,.46,1.34,m["slag"],(.05,-.12,.04),22,True)
    throat("rootbound-throat",(0,0,1.42),.40,m["heat"])
    torus("forged-collar",(0,0,1.43),.52,.065,m["iron"],collision=False)
    for i in range(9):
        a=i*math.tau/9+.14
        start=(math.cos(a)*1.34,math.sin(a)*1.34,.30)
        mid=(math.cos(a+.18)*.78,math.sin(a+.18)*.78,.80+.10*(i%3))
        end=(math.cos(a+.35)*.42,math.sin(a+.35)*.42,1.50+.08*(i%2))
        tube(f"charred-root-a-{i}",start,mid,.08,m["root"],10,True)
        tube(f"charred-root-b-{i}",mid,end,.06,m["root"],10,False)
    for i in range(6):
        a=i*math.tau/6
        throat(f"root-ember-{i}",(math.cos(a)*.46,math.sin(a)*.46,.82+.08*(i%2)),.085,m["heat"])

BUILD={"underworld-geothermal-vent-crown":build_crown,"underworld-geothermal-vent-split":build_split,"underworld-geothermal-vent-rootbound":build_rootbound}

def author(model_id):
    bpy.ops.wm.read_factory_settings(use_empty=True);m=palette();BUILD[model_id](m)
    meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
    if len(meshes)<DETAIL_FLOOR:raise RuntimeError(f"{model_id}: detail regression {len(meshes)} < {DETAIL_FLOOR}")
    for o in meshes:
        uv=o.data.uv_layers.get("VentUV")
        if uv is None or not uv.data:raise RuntimeError(f"{model_id}/{o.name}: VentUV missing")
    scene=bpy.context.scene;scene["model_id"]=model_id;scene["magenheim_family"]="underworld_geothermal_vent";scene["magenheim_fidelity"]="geothermal-vent-r1"
    scene["runtime_lights"]=json.dumps([{"path":"geothermal-glow","position":[0,1.25,0],"color":[1.0,.20,.035],"range":5.5,"intensity":2.2}])
    bpy.context.preferences.filepaths.save_version=0
    target=SOURCE/(model_id+".blend");bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
    print("AUTHORED",model_id,len(meshes),"parts",flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(IDS)
unknown=[x for x in requested if x not in IDS]
if unknown:raise SystemExit("Unknown geothermal vent model(s): "+", ".join(unknown))
for model_id in requested:author(model_id)
