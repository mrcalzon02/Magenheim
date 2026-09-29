#!/usr/bin/env python3
"""Author the Great Decay Carrion Catacombs dungeon kit.

Sixteen large rooms plus one adaptive passage. Geometry deliberately transitions from recognizable
funerary construction into rot/root/bone occupation. Great Decay contamination remains runtime
atmosphere authority; the meshes never fake Black Bloom or create damage planes.
"""
import math,random,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
PREFIX="underworld-dungeon-great-decay-carrion-catacombs-"
REVISION="carrion-catacombs-dungeon-r1"
MIN_PARTS=30
MIN_TRIS=1700

SPECS={
"ossuary-gate":((36,38,22),"PreservedRuin",.18,.04,"gate"),
"processional-hall":((40,54,24),"PreservedRuin",.24,.06,"hall"),
"sunken-reliquary":((42,46,24),"TaintedRuin",.34,.10,"reliquary"),
"root-split-crossing":((42,42,24),"RootIngress",.48,.28,"crossing"),
"bone-chute":((30,36,42),"RootIngress",.52,.30,"chute"),
"miasma-nave":((40,48,26),"BlackBloom",.62,.72,"nave"),
"carrion-sluice":((32,52,20),"BlackBloom",.68,.78,"sluice"),
"amber-mortuary":((40,40,22),"TaintedRuin",.38,.12,"mortuary"),
"bone-gravel-crypt":((38,42,20),"RootIngress",.46,.24,"crypt"),
"censer-court":((42,42,24),"Sanctuary",.08,.02,"court"),
"rotling-warrens":((36,40,18),"RootIngress",.50,.26,"warrens"),
"spore-husk-cloister":((40,44,22),"TaintedRuin",.42,.16,"cloister"),
"graft-warden-hall":((46,48,28),"RootIngress",.56,.34,"warden"),
"vanishing-archive":((44,50,26),"PreservedRuin",.26,.08,"archive"),
"defiant-work-chapel":((42,44,24),"Sanctuary",.10,.03,"chapel"),
"corpse-orchard-antechamber":((52,56,32),"BlackBloom",.72,.88,"orchard"),
"passage":((14,16,14),"Adaptive",.36,.18,"passage"),
}

def mat(semantic):
    name="magenheim.carrion-catacombs."+semantic
    material=bpy.data.materials.get(name)
    if material:return material
    material=bpy.data.materials.new(name);material.use_nodes=True;material.use_backface_culling=True
    material.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value=(1,1,1,1)
    bind_underworld_material(bpy,material,name)
    return material

def finish(obj,name,material,collision=False):
    obj.name=name
    obj["game_node_path"]=name
    obj["game_collision"]=bool(collision)
    obj["game_crystal"]="null"
    obj.data.materials.clear();obj.data.materials.append(material)
    if not obj.data.uv_layers:obj.data.uv_layers.new(name="CarrionUV")
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT");bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=2.0)
    bpy.ops.object.mode_set(mode="OBJECT");obj.select_set(False)
    return obj

def box(name,loc,size,material,collision=False,rot=(0,0,0),bevel=.11):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot)
    obj=bpy.context.object;obj.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new("worked-edge","BEVEL");mod.width=min(bevel,min(size)*.22);mod.segments=2
    return finish(obj,name,material,collision)

def rock(name,loc,scale,material,collision=False,rot=(0,0,0)):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=loc)
    obj=bpy.context.object;obj.scale=scale;obj.rotation_euler=rot
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(obj,name,material,collision)

def cyl(name,loc,radius,depth,material,collision=False,rot=(0,0,0),verts=14):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=depth,location=loc,rotation=rot)
    return finish(bpy.context.object,name,material,collision)

def cone(name,loc,r1,r2,depth,material,collision=False,rot=(0,0,0),verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=depth,location=loc,rotation=rot)
    return finish(bpy.context.object,name,material,collision)

def torus(name,loc,major,minor,material,collision=False,rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=8,major_radius=major,minor_radius=minor,location=loc,rotation=rot)
    return finish(bpy.context.object,name,material,collision)

def tube(name,a,b,radius,material,collision=False,verts=12):
    a,b=Vector(a),Vector(b);delta=b-a
    if delta.length<1e-5:raise ValueError(name+" degenerate")
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=delta.length,location=(a+b)*.5)
    obj=bpy.context.object;obj.rotation_mode="QUATERNION"
    obj.rotation_quaternion=Vector((0,0,1)).rotation_difference(delta.normalized())
    return finish(obj,name,material,collision)

def shell(w,d,h,seed,stone,bone):
    rng=random.Random(seed)
    for i in range(16):
        angle=(i+.35)*math.tau/16
        material=stone if i%3 else bone
        rock(f"catacomb-shell-{i}",
             (math.cos(angle)*w*.48,math.sin(angle)*d*.48,h*rng.uniform(.24,.42)),
             (w*.075+rng.uniform(.7,1.5),d*.07+rng.uniform(.7,1.5),h*rng.uniform(.22,.34)),
             material,True,(rng.uniform(-.18,.18),rng.uniform(-.18,.18),angle))
    for i in range(8):
        angle=(i+.2)*math.tau/8
        cone(f"hanging-bone-{i}",
             (math.cos(angle)*w*.27,math.sin(angle)*d*.27,h*.78),
             .38,.05,h*.20,bone,False,(0,0,angle),8)

def floor(w,d,route,stone,bone,rotwood):
    box("catacomb-floor",(0,0,-.42),(w*.44,d*.44,.42),stone,True,bevel=.15)
    # Always preserve a stable walking edge through pressure rooms. Suppression changes pressure,
    # not whether the dungeon is physically traversable.
    if route in ("TaintedRuin","RootIngress","BlackBloom"):
        for side in (-1,1):
            box(f"stable-ledge-{side}",(side*w*.35,0,.26),(w*.065,d*.35,.26),stone,True,bevel=.08)
    if route in ("RootIngress","BlackBloom"):
        for i,y in enumerate((-d*.28,-d*.12,d*.04,d*.20)):
            side=-1 if i%2 else 1
            tube(f"floor-root-{i}",(side*w*.28,y,.20),(-side*w*.08,y+d*.08,1.7),
                 .35,rotwood,True,10)
    if route=="BlackBloom":
        for i,y in enumerate((-d*.30,-d*.10,d*.10,d*.30)):
            box(f"bone-ridge-{i}",(0,y,.34),(w*.15,.45,.34),bone,True,bevel=.05)

def funerary(w,d,h,seed,stone,bone,amber):
    rng=random.Random(seed)
    for side in (-1,1):
        for i,y in enumerate((-d*.30,-d*.10,d*.10,d*.30)):
            cyl(f"funerary-pier-{side}-{i}",(side*w*.31,y,h*.17),.78,h*.34,stone,True,verts=12)
            box(f"ossuary-plaque-{side}-{i}",(side*w*.26,y,1.45),(.22,.72,.52),bone,False,bevel=.04)
            if (i+side)%2==0:
                rock(f"amber-seal-{side}-{i}",(side*w*.25,y,2.45),(.34,.24,.45),amber,False)

def organic_takeover(w,d,h,route,seed,rotwood,spore,amber):
    level={"Sanctuary":1,"PreservedRuin":2,"TaintedRuin":4,"RootIngress":7,"BlackBloom":10,"Adaptive":4}[route]
    rng=random.Random(seed)
    for i in range(level):
        angle=(i*.77+.35)*math.tau/max(1,level)
        x=math.cos(angle)*w*rng.uniform(.15,.34);y=math.sin(angle)*d*rng.uniform(.14,.34)
        tube(f"rotroot-rib-{i}",(x,y,.25),(x*.45,y*.45,h*rng.uniform(.30,.62)),
             rng.uniform(.22,.48),rotwood,i%3==0,10)
        if route in ("TaintedRuin","RootIngress","BlackBloom") or i%2==0:
            rock(f"spore-growth-{i}",(x*.78,y*.78,rng.uniform(.5,2.8)),
                 (rng.uniform(.35,.8),rng.uniform(.35,.8),rng.uniform(.25,.75)),spore,False)
        if route=="BlackBloom" and i%2==0:
            rock(f"carrion-amber-growth-{i}",(x*.58,y*.58,rng.uniform(1.0,3.8)),
                 (rng.uniform(.25,.6),rng.uniform(.25,.6),rng.uniform(.45,1.0)),amber,False)

def feature(kind,w,d,h,stone,bone,rotwood,spore,amber):
    if kind=="gate":
        for side in (-1,1):
            box(f"gate-pillar-{side}",(side*w*.22,-d*.22,h*.19),(1.4,1.8,h*.19),stone,True)
        tube("gate-lintel",(-w*.24,-d*.22,h*.40),(w*.24,-d*.22,h*.40),.34,bone,True)
    elif kind=="hall":
        for side in (-1,1):
            for i,y in enumerate((-d*.32,-d*.16,0,d*.16,d*.32)):
                cyl(f"processional-column-{side}-{i}",(side*w*.28,y,h*.18),.72,h*.36,stone,True,verts=12)
        for i,y in enumerate((-d*.24,0,d*.24)):
            torus(f"processional-arch-{i}",(0,y,h*.42),w*.26,.22,bone,False,(math.pi/2,0,0))
    elif kind=="reliquary":
        for tier,(sx,sy,z) in enumerate(((.24,.20,.45),(.17,.14,1.05),(.10,.08,1.55))):
            box(f"reliquary-dais-{tier}",(0,0,z),(w*sx,d*sy,.45),stone,True)
        rock("reliquary-amber",(0,0,2.8),(1.2,.9,1.8),amber,False)
    elif kind=="crossing":
        for i in range(8):
            a=i*math.tau/8
            tube(f"cross-root-{i}",(math.cos(a)*w*.31,math.sin(a)*d*.31,.4),
                 (math.cos(a)*w*.10,math.sin(a)*d*.10,h*.48),.34,rotwood,True,10)
        box("crossing-stone-island",(0,0,.55),(w*.12,d*.12,.55),stone,True)
    elif kind=="chute":
        for i,z in enumerate((2,6,10,14,18,22,26,30)):
            torus(f"bone-chute-ring-{i}",(0,0,z),w*.24,.24,bone,True)
        for side in (-1,1):
            tube(f"chute-root-{side}",(side*w*.20,0,.5),(side*w*.12,0,h*.82),.28,rotwood,True)
    elif kind=="nave":
        for i in range(10):
            a=i*math.tau/10
            tube(f"nave-root-{i}",(math.cos(a)*w*.30,math.sin(a)*d*.30,.3),
                 (math.cos(a)*w*.12,math.sin(a)*d*.12,h*.54),.38,rotwood,True,10)
            rock(f"nave-spore-{i}",(math.cos(a)*w*.15,math.sin(a)*d*.15,2.0),
                 (.65,.65,.9),spore,False)
    elif kind=="sluice":
        for i,y in enumerate((-d*.32,-d*.20,-d*.08,d*.08,d*.20,d*.32)):
            box(f"sluice-rib-{i}",(0,y,.40),(w*.20,.42,.40),bone,True,bevel=.04)
            tube(f"sluice-root-{i}",(-w*.30,y,.4),(w*.18,y+d*.04,2.0),.30,rotwood,True,10)
    elif kind=="mortuary":
        for side in (-1,1):
            for row in range(4):
                y=-d*.28+row*d*.18
                box(f"mortuary-bier-{side}-{row}",(side*w*.27,y,.65),(1.2,1.8,.65),stone,True)
                rock(f"mortuary-amber-{side}-{row}",(side*w*.25,y,1.65),(.48,.35,.78),amber,False)
    elif kind=="crypt":
        for side in (-1,1):
            for row in range(4):
                y=-d*.28+row*d*.18
                box(f"bone-crypt-{side}-{row}",(side*w*.28,y,1.35),(1.0,1.9,1.35),stone,True)
                for tier in range(3):
                    box(f"bone-stack-{side}-{row}-{tier}",(side*w*.22,y,.55+tier*.75),(.38,1.35,.16),bone,False,bevel=.03)
    elif kind=="court":
        box("censer-court-dais",(0,0,.55),(w*.16,d*.16,.55),stone,True)
        for i in range(6):
            a=i*math.tau/6
            cyl(f"censer-court-pillar-{i}",(math.cos(a)*w*.27,math.sin(a)*d*.27,h*.18),.62,h*.36,stone,True,verts=12)
            rock(f"censer-amber-{i}",(math.cos(a)*w*.27,math.sin(a)*d*.27,h*.38),(.30,.30,.52),amber,False)
    elif kind=="warrens":
        for i in range(10):
            a=i*2.39996;r=w*(.10+.018*(i%3))
            tube(f"warren-root-{i}",(math.cos(a)*r,math.sin(a)*r,.2),
                 (math.cos(a)*r*.5,math.sin(a)*r*.5,3.0),.24,rotwood,True,9)
    elif kind=="cloister":
        for side in (-1,1):
            for i,y in enumerate((-d*.30,-d*.10,d*.10,d*.30)):
                box(f"cloister-cell-{side}-{i}",(side*w*.29,y,1.4),(1.0,1.25,1.4),stone,True)
                rock(f"cloister-spore-{side}-{i}",(side*w*.23,y,2.8),(.42,.42,.62),spore,False)
    elif kind=="warden":
        for i in range(8):
            a=i*math.tau/8
            box(f"warden-plinth-{i}",(math.cos(a)*w*.25,math.sin(a)*d*.25,.65),(1.4,1.4,.65),stone,True,(0,0,a))
            tube(f"warden-bone-spine-{i}",(math.cos(a)*w*.25,math.sin(a)*d*.25,1.2),
                 (math.cos(a)*w*.18,math.sin(a)*d*.18,4.8),.18,bone,False,9)
    elif kind=="archive":
        for side in (-1,1):
            for row in range(4):
                y=-d*.28+row*d*.18
                box(f"archive-stack-{side}-{row}",(side*w*.29,y,2.0),(1.1,2.2,2.0),rotwood,True)
                for tier in range(3):
                    box(f"archive-slate-{side}-{row}-{tier}",(side*w*.22,y,.8+tier*.9),(.38,1.5,.16),bone,False,bevel=.04)
    elif kind=="chapel":
        for tier,(sx,sy,z) in enumerate(((.26,.20,.45),(.18,.14,1.05),(.10,.08,1.55))):
            box(f"work-chapel-dais-{tier}",(0,0,z),(w*sx,d*sy,.45),stone,True)
        for side in (-1,1):
            box(f"work-bench-{side}",(side*w*.20,-d*.03,.55),(1.2,d*.22,.55),rotwood,True)
        torus("chapel-amber-halo",(0,d*.18,3.1),1.25,.11,amber,False,(math.pi/2,0,0))
    elif kind=="orchard":
        for i in range(12):
            a=i*math.tau/12
            x=math.cos(a)*w*.24;y=math.sin(a)*d*.24
            tube(f"orchard-trunk-{i}",(x,y,.2),(x*.52,y*.52,h*.52),.46,rotwood,True,10)
            rock(f"orchard-spore-crown-{i}",(x*.52,y*.52,h*.54),(1.0,1.0,.8),spore,False)
            if i%2==0:rock(f"orchard-amber-{i}",(x*.42,y*.42,h*.34),(.45,.45,.75),amber,False)
        box("orchard-burial-dais",(0,0,.75),(w*.16,d*.14,.75),bone,True)
    elif kind=="passage":
        box("passage-floor",(0,0,-.35),(5.7,d*.46,.35),stone,True)
        for side in (-1,1):
            box(f"passage-ledge-{side}",(side*5.6,0,.32),(1.0,d*.46,.32),stone,True)
        for i,y in enumerate((-d*.34,-d*.12,d*.12,d*.34)):
            torus(f"passage-bone-rib-{i}",(0,y,4.0),5.5,.20,bone,True,(math.pi/2,0,0))
        tube("passage-root-a",(-5.2,-d*.34,.4),(2.2,d*.28,3.2),.26,rotwood,False,10)

def author(mid):
    suffix=mid.removeprefix(PREFIX)
    dims,route,contamination,bloom,kind=SPECS[suffix]
    w,d,h=dims
    bpy.ops.wm.read_factory_settings(use_empty=True)
    stone=mat("understone");bone=mat("bone-gravel");rotwood=mat("rotwood")
    spore=mat("decay-spore");amber=mat("carrion-amber")
    seed=0xCA7710 ^ sum((i+1)*ord(c) for i,c in enumerate(mid))
    shell(w,d,h,seed,stone,bone)
    floor(w,d,route,stone,bone,rotwood)
    funerary(w,d,h,seed+11,stone,bone,amber)
    organic_takeover(w,d,h,route,seed+23,rotwood,spore,amber)
    feature(kind,w,d,h,stone,bone,rotwood,spore,amber)
    meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
    tris=0
    for obj in meshes:
        obj.data.calc_loop_triangles();tris+=len(obj.data.loop_triangles)
    if len(meshes)<MIN_PARTS:raise RuntimeError(f"{mid}: detail regression {len(meshes)} < {MIN_PARTS}")
    if tris<MIN_TRIS:raise RuntimeError(f"{mid}: fidelity regression {tris} < {MIN_TRIS}")
    scene=bpy.context.scene
    scene["model_id"]=mid
    scene["underworld_authoring"]=REVISION
    scene["runtime_lights"]="[]"
    scene["carrion_dimensions_m"]=",".join(str(x) for x in dims)
    scene["carrion_route"]=route
    scene["carrion_contamination01"]=float(contamination)
    scene["carrion_black_bloom_intensity01"]=float(bloom)
    scene["carrion_atmosphere_geometry"]="none-runtime-atmosphere-authority"
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(mid+".blend")),compress=True)
    print("AUTHORED",mid,route,"contamination",contamination,"bloom",bloom,
          "parts",len(meshes),"tris",tris,flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
unknown=[x for x in requested if x.removeprefix(PREFIX) not in SPECS]
if unknown:raise SystemExit("Unknown Carrion Catacombs model(s): "+", ".join(unknown))
for mid in requested:author(mid)
