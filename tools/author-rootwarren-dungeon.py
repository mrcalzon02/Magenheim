"""Author the Fungal Rootwarren dungeon kit as editable Blender sources.

Run:
  tools/blender.ps1 author-rootwarren-dungeon
  tools/blender.ps1 export-model-assets underworld-dungeon-fungal-rootwarren-...

This is asset authoring, not runtime procedural geometry. Sixteen large room-family sources match
UnderworldFungalRootwarrenCatalog plus one reusable passage. Rooms use the established Fungal Forest
surface vocabulary: understone, worldroot, mycelium, teal/violet caps, luminous gills and amber.
Blender Z is game up; all dimensions are metres.
"""
import bpy
import math
import random
import sys
from pathlib import Path
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from magenheim_blender_kit import (
    blob, lathe, merge, spec_painter, transformed, unwrap, uv_overlap)
from magenheim_flora_kit import Model, cap, gills, tube, bake_flora_atlas

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/models/source"
REVISION = "rootwarren-dungeon-r2"
PREFIX = "underworld-dungeon-fungal-rootwarren-"
Z = Vector((0.0, 0.0, 1.0))

SURFACES = {
    "understone": dict(
        low=(0.10, 0.12, 0.13), high=(0.30, 0.34, 0.35), scale=5,
        veins=((0.06, 0.075, 0.08), 0.025, 4.0),
        edge=((0.52, 0.58, 0.56), 0.7), occlusion=1.0, metallic=0.0, rough=0.94),
    "ruin": dict(
        low=(0.17, 0.18, 0.17), high=(0.39, 0.41, 0.37), scale=6,
        veins=((0.10, 0.11, 0.10), 0.018, 6.0),
        edge=((0.58, 0.62, 0.55), 0.65), occlusion=1.0, metallic=0.0, rough=0.91),
    "worldroot": dict(
        low=(0.10, 0.16, 0.10), high=(0.31, 0.42, 0.27), scale=3,
        stretch=(8.0, 8.0, 0.55), edge=((0.54, 0.68, 0.47), 0.45),
        occlusion=0.95, metallic=0.0, rough=0.86),
    "mycelium": dict(
        low=(0.23, 0.27, 0.22), high=(0.62, 0.66, 0.48), scale=9,
        veins=((0.80, 0.84, 0.65), 0.018, 8.0),
        edge=((0.76, 0.82, 0.66), 0.35), occlusion=0.72, metallic=0.0, rough=0.76),
    "cap-teal": dict(
        low=(0.035, 0.14, 0.17), high=(0.11, 0.37, 0.38), scale=5,
        veins=((0.33, 0.70, 0.65), 0.016, 4.0),
        edge=((0.52, 0.85, 0.77), 0.38), occlusion=0.82, metallic=0.0, rough=0.57),
    "cap-violet": dict(
        low=(0.13, 0.065, 0.22), high=(0.38, 0.20, 0.50), scale=5,
        veins=((0.70, 0.48, 0.86), 0.016, 4.0),
        edge=((0.82, 0.66, 0.95), 0.40), occlusion=0.82, metallic=0.0, rough=0.57),
    "gill": dict(
        low=(0.30, 0.78, 0.72), high=(0.72, 1.0, 0.92), scale=12,
        edge=((0.92, 1.0, 1.0), 0.25), occlusion=0.25, metallic=0.0, rough=0.48,
        emission=(0.07, 0.34, 0.30)),
    "amber": dict(
        low=(0.42, 0.21, 0.04), high=(0.92, 0.60, 0.18), scale=7,
        veins=((1.0, 0.82, 0.40), 0.025, 7.0),
        edge=((1.0, 0.86, 0.50), 0.35), occlusion=0.72, metallic=0.0, rough=0.60,
        emission=(0.13, 0.055, 0.006)),
}
paint = spec_painter(SURFACES, ao_distance=0.42, edge_radius=0.025)


def box_mesh(center, size):
    x, y, z = center
    hx, hy, hz = [value * 0.5 for value in size]
    verts = [
        Vector((x-hx, y-hy, z-hz)), Vector((x+hx, y-hy, z-hz)),
        Vector((x+hx, y+hy, z-hz)), Vector((x-hx, y+hy, z-hz)),
        Vector((x-hx, y-hy, z+hz)), Vector((x+hx, y-hy, z+hz)),
        Vector((x+hx, y+hy, z+hz)), Vector((x-hx, y+hy, z+hz)),
    ]
    faces = [
        (0,3,2,1), (4,5,6,7), (0,1,5,4),
        (1,2,6,5), (2,3,7,6), (3,0,4,7),
    ]
    return verts, faces


def rot(mesh, angle, axis="Z", origin=(0,0,0)):
    matrix = (
        Matrix.Translation(Vector(origin))
        @ Matrix.Rotation(angle, 4, axis)
        @ Matrix.Translation(-Vector(origin))
    )
    return transformed(mesh, matrix)


def cave_shell(m, width, depth, height, seed, floor_z=0.0):
    """Irregular cave envelope with eight broad approach gaps.

    Rock masses sit between the eight cardinal/diagonal approach directions rather than on them,
    so routed passages can enter from any grid direction without requiring runtime boolean cuts.
    """
    rng = random.Random(seed)
    shell = []
    for i in range(8):
        a = (i + .5) * math.tau / 8.0 + rng.uniform(-.055, .055)
        x = math.cos(a) * width * .47
        y = math.sin(a) * depth * .47
        rx = rng.uniform(2.2, 3.5) + width * .035
        ry = rng.uniform(2.2, 3.5) + depth * .035
        rz = max(2.8, height * rng.uniform(.24, .36))
        shell.append(blob(
            (x, y, floor_z + rz * .72),
            (rx, ry, rz),
            9, 7))

    # Broken ceiling masses close the chamber silhouette without creating a featureless flat roof.
    # The offset ring preserves a central high vault and leaves the luminous fungi readable.
    for i in range(10):
        a = (i + .35) * math.tau / 10.0 + rng.uniform(-.08, .08)
        x = math.cos(a) * width * rng.uniform(.16, .32)
        y = math.sin(a) * depth * rng.uniform(.16, .32)
        rx = rng.uniform(2.8, 4.8) + width * .025
        ry = rng.uniform(2.8, 4.8) + depth * .025
        rz = rng.uniform(1.7, 3.2)
        shell.append(blob(
            (x, y, floor_z + height * rng.uniform(.72, .86)),
            (rx, ry, rz),
            9, 6))

    m.part("cavern-shell", merge(*shell), "understone", collider=True, smooth=False)


def floor_and_boundary(m, width, depth, seed, broken=0.22):
    rng = random.Random(seed)
    floor = box_mesh((0, 0, -0.65), (width * 0.92, depth * 0.92, 1.3))
    m.part("understone-floor", floor, "understone", collider=True, smooth=False)
    rubble = []
    for i in range(12):
        a = math.tau * i / 12 + rng.uniform(-0.12, 0.12)
        edge_x = math.cos(a) * width * 0.46
        edge_y = math.sin(a) * depth * 0.46
        radius = rng.uniform(0.8, 2.2) * (1.0 + broken)
        rubble.append(blob((edge_x, edge_y, rng.uniform(-0.1, 0.6)),
                           (radius, radius * rng.uniform(.65, 1.1), radius * rng.uniform(.45, .85)),
                           8, 6))
    m.part("boundary-rubble", merge(*rubble), "understone", collider=True, smooth=False)
    cave_shell(m, width, depth, max(12.0, min(width, depth) * .62), seed + 101)


def ruin_bays(m, width, depth, height, seed, density=1.0):
    rng = random.Random(seed)
    solids = []
    count = max(5, int(9 * density))
    for i in range(count):
        side = -1 if i % 2 == 0 else 1
        x = side * width * rng.uniform(.32, .44)
        y = rng.uniform(-depth * .36, depth * .36)
        h = rng.uniform(height * .22, height * .62)
        w = rng.uniform(1.4, 2.8)
        d = rng.uniform(1.5, 3.0)
        block = box_mesh((x, y, h * .5), (w, d, h))
        block = rot(block, rng.uniform(-.18, .18), "Z", (x, y, 0))
        solids.append(block)
    m.part("ruined-masonry", merge(*solids), "ruin", collider=True, smooth=False)


def root_ribs(m, width, depth, height, seed, count=7, cross=False):
    rng = random.Random(seed)
    roots = []
    for i in range(count):
        t = (i + 1) / (count + 1)
        y = -depth * .38 + t * depth * .76
        left = Vector((-width * .48, y + rng.uniform(-1.2, 1.2), .15))
        right = Vector((width * .48, y + rng.uniform(-1.2, 1.2), .15))
        crown = Vector((rng.uniform(-width*.10, width*.10), y + rng.uniform(-2.0, 2.0),
                        height * rng.uniform(.58, .88)))
        roots.append(tube([left, left.lerp(crown,.48)+Z*1.2, crown,
                           right.lerp(crown,.48)+Z*.8, right],
                          [(0,.75),(0.18,.62),(.50,.48),(.82,.62),(1,.75)], 12, 24))
    if cross:
        for i in range(3):
            x = rng.uniform(-width*.26, width*.26)
            a = Vector((x, -depth*.46, .1))
            b = Vector((x+rng.uniform(-4,4), depth*.46, .1))
            c = (a+b)*.5 + Z * height * rng.uniform(.45,.68)
            roots.append(tube([a, a.lerp(c,.5)+Z*.5, c, b.lerp(c,.5)+Z*.4, b],
                              [(0,.55),(.2,.45),(.5,.38),(.8,.45),(1,.55)], 10, 20))
    m.part("worldroot-ribs", merge(*roots), "worldroot", collider=True)


def fungus_cluster(m, width, depth, height, seed, count=12, violet_ratio=.35, giant=False):
    rng = random.Random(seed)
    stalks, teal, violet, glow = [], [], [], []
    for i in range(count):
        x = rng.uniform(-width*.38, width*.38)
        y = rng.uniform(-depth*.38, depth*.38)
        h = rng.uniform(1.2, 3.5) * (2.1 if giant and i < 3 else 1.0)
        radius = rng.uniform(.45, 1.4) * (1.7 if giant and i < 3 else 1.0)
        lean = Vector((rng.uniform(-.5,.5), rng.uniform(-.5,.5), 0))
        top = Vector((x,y,h))
        stalks.append(tube([Vector((x,y,0)), Vector((x,y,h*.45))+lean*.35, top+lean],
                           [(0,.22),(.65,.16),(1,.20)], 10, 12))
        target = violet if rng.random() < violet_ratio else teal
        target.append(cap((top.x+lean.x, top.y+lean.y, top.z), radius, radius*.48, sides=20))
        glow.append(gills((top.x+lean.x, top.y+lean.y, top.z+.02),
                          radius*.18, radius*.90, radius*.16, 18, .015))
    m.part("fungal-stalks", merge(*stalks), "mycelium", collider=False)
    if teal: m.part("teal-caps", merge(*teal), "cap-teal", collider=False)
    if violet: m.part("violet-caps", merge(*violet), "cap-violet", collider=False)
    if glow: m.part("luminous-gills", merge(*glow), "gill", collider=False, smooth=False)


def amber_growth(m, width, depth, seed, count=10):
    rng = random.Random(seed)
    growths = []
    for _ in range(count):
        x = rng.uniform(-width*.32, width*.32)
        y = rng.uniform(-depth*.32, depth*.32)
        r = rng.uniform(.32, .95)
        growths.append(blob((x,y,r*.55), (r, r*.75, r*1.25), 9, 7))
    m.part("amber-growth", merge(*growths), "amber", collider=False)


def central_basin(m, radius, depth=.7):
    rings = []
    for i in range(18):
        a = math.tau*i/18
        x, y = math.cos(a)*radius, math.sin(a)*radius
        rings.append(blob((x,y,.15), (1.1, .75, .55), 7, 5))
    m.part("basin-rim", merge(*rings), "understone", collider=True, smooth=False)
    m.part("basin-bed", box_mesh((0,0,-depth), (radius*1.65, radius*1.65, .45)),
           "mycelium", collider=True, smooth=False)


def fractured_mouth(m, width, depth, height, seed):
    rng = random.Random(seed)
    floor_and_boundary(m,width,depth,seed,.35)
    jaws=[]
    for side in (-1,1):
        for i in range(4):
            x=side*(width*.28+i*1.15)
            y=-depth*.24+rng.uniform(-1.2,1.2)
            h=height*(.30+i*.10)
            rock=blob((x,y,h*.42),(1.8,2.3,h*.48),8,7)
            jaws.append(rot(rock, side*rng.uniform(.12,.30),"Y",(x,y,0)))
    m.part("fracture-jaws",merge(*jaws),"understone",collider=True,smooth=False)
    root_ribs(m,width,depth,height,seed+3,5)
    fungus_cluster(m,width,depth,height,seed+5,8)


def mycelial_gallery(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed)
    ruin_bays(m,w,d,h,seed+1,.8)
    root_ribs(m,w,d,h,seed+2,8)
    fungus_cluster(m,w,d,h,seed+3,12,.28)


def glowcap_vault(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.12)
    root_ribs(m,w,d,h,seed+2,9,True)
    fungus_cluster(m,w,d,h,seed+4,16,.20,True)
    amber_growth(m,w,d,seed+7,6)


def spore_basin(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.2)
    central_basin(m,min(w,d)*.18,1.0)
    root_ribs(m,w,d,h,seed+1,6)
    fungus_cluster(m,w,d,h,seed+2,18,.45)


def root_bridge(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.35)
    # Narrow elevated root causeway over a broken central floor.
    bridge=tube([Vector((0,-d*.44,2.0)),Vector((2,-d*.15,5.5)),
                 Vector((-1,d*.15,5.0)),Vector((0,d*.44,2.0))],
                [(0,1.7),(.33,1.35),(.67,1.35),(1,1.7)],14,30)
    m.part("root-bridge",bridge,"worldroot",collider=True)
    root_ribs(m,w,d,h,seed+2,5)
    fungus_cluster(m,w,d,h,seed+3,8)


def sunken_nursery(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.15)
    central_basin(m,min(w,d)*.22,.5)
    fungus_cluster(m,w,d,h,seed+1,22,.22)
    amber_growth(m,w,d,seed+2,12)


def tangle_junction(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.25)
    root_ribs(m,w,d,h,seed+1,10,True)
    ruin_bays(m,w,d,h,seed+2,.55)
    fungus_cluster(m,w,d,h,seed+3,10,.50)


def shelf_drop(m,w,d,h,seed):
    # Upper shelf plus lower landing establish genuine vertical navigation.
    cave_shell(m, w, d, h, seed + 90, floor_z=-9.0)
    m.part("upper-shelf",box_mesh((0,-d*.20,2.0),(w*.86,d*.38,1.4)),
           "understone",collider=True,smooth=False)
    m.part("lower-shelf",box_mesh((0,d*.25,-9.0),(w*.82,d*.32,1.4)),
           "understone",collider=True,smooth=False)
    roots=[]
    for i in range(5):
        x=-w*.25+i*w*.125
        roots.append(tube([Vector((x,-d*.04,1.8)),Vector((x+1.5,0,-1.5)),
                           Vector((x-.8,d*.10,-5.2)),Vector((x,d*.18,-8.4))],
                          [(0,.45),(.35,.36),(.7,.32),(1,.42)],10,20))
    m.part("descent-roots",merge(*roots),"worldroot",collider=True)
    root_ribs(m,w,d,h,seed+1,5)
    fungus_cluster(m,w,d,h,seed+2,9)


def amber_grotto(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.18)
    root_ribs(m,w,d,h,seed+1,6)
    amber_growth(m,w,d,seed+2,28)
    fungus_cluster(m,w,d,h,seed+3,8,.15)


def worldroot_hollow(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.12)
    # Giant hollow/root cathedral, leaving a navigable central void.
    arches=[]
    for i in range(10):
        a=math.tau*i/10
        foot=Vector((math.cos(a)*w*.38,math.sin(a)*d*.38,.1))
        crown=Vector((math.cos(a)*w*.10,math.sin(a)*d*.10,h*.82))
        arches.append(tube([foot,foot.lerp(crown,.45)+Z*2.5,crown],
                           [(0,1.05),(.45,.82),(1,.62)],14,22))
    m.part("worldroot-cathedral",merge(*arches),"worldroot",collider=True)
    fungus_cluster(m,w,d,h,seed+3,13,.30)


def crawler_nest(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.30)
    root_ribs(m,w,d,h,seed+1,7)
    central_basin(m,min(w,d)*.14,.35)
    fungus_cluster(m,w,d,h,seed+2,12,.55)
    amber_growth(m,w,d,seed+3,8)


def stalker_den(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.28)
    ruin_bays(m,w,d,h,seed+1,.75)
    root_ribs(m,w,d,h,seed+2,9,True)
    fungus_cluster(m,w,d,h,seed+3,7,.65)


def puffback_graze(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.10)
    root_ribs(m,w,d,h,seed+1,5)
    fungus_cluster(m,w,d,h,seed+2,18,.18)
    amber_growth(m,w,d,seed+3,4)


def buried_archway(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.24)
    ruins=[]
    for i in range(5):
        y=-d*.34+i*d*.17
        for side in (-1,1):
            ruins.append(box_mesh((side*w*.22,y,h*.20),(2.3,2.7,h*.40)))
        ruins.append(box_mesh((0,y,h*.42),(w*.42,2.6,1.5)))
    m.part("buried-arches",merge(*ruins),"ruin",collider=True,smooth=False)
    root_ribs(m,w,d,h,seed+1,7)
    fungus_cluster(m,w,d,h,seed+2,8,.25)


def root_squeeze(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.36)
    roots=[]
    for i in range(12):
        y=-d*.40+i*d*.073
        side=-1 if i%2==0 else 1
        start=Vector((side*w*.48,y,.1))
        target=Vector((-side*w*.08,y+random.Random(seed+i).uniform(-2,2),
                       h*random.Random(seed*3+i).uniform(.25,.65)))
        roots.append(tube([start,start.lerp(target,.55)+Z*.7,target],
                          [(0,.75),(.6,.56),(1,.42)],12,16))
    m.part("squeeze-roots",merge(*roots),"worldroot",collider=True)
    fungus_cluster(m,w,d,h,seed+1,7,.45)


def heartcap_sanctum(m,w,d,h,seed):
    floor_and_boundary(m,w,d,seed,.10)
    ruin_bays(m,w,d,h,seed+1,1.15)
    root_ribs(m,w,d,h,seed+2,10,True)
    fungus_cluster(m,w,d,h,seed+3,18,.30,True)
    # Central altar and heartcap crown.
    m.part("sanctum-dais",merge(
        box_mesh((0,0,.45),(10,8,.9)),
        box_mesh((0,0,1.2),(7,5, .8)),
        box_mesh((0,0,1.9),(4.5,3.5,.7))),
        "ruin",collider=True,smooth=False)
    top=Vector((0,0,7.8))
    m.part("heartcap-stalk",tube([Vector((0,0,2.2)),Vector((.4,-.3,5.2)),top],
                                 [(0,.75),(.6,.55),(1,.7)],14,18),
           "mycelium",collider=True)
    m.part("heartcap",cap((0,0,7.8),5.2,2.0,sides=28),"cap-violet",collider=False)
    m.part("heartcap-gills",gills((0,0,7.9),.8,4.8,.34,30,.025),"gill",collider=False,smooth=False)
    amber_growth(m,w,d,seed+4,10)


def passage(m,w,d,h,seed):
    m.part("passage-floor",box_mesh((0,0,-.35),(w*.72,d,.7)),
           "understone",collider=True,smooth=False)
    rng = random.Random(seed + 41)
    banks = []
    for y in (-d*.42, -d*.20, .0, d*.20, d*.42):
        for side in (-1, 1):
            banks.append(blob(
                (side*w*.46, y, h*.28),
                (rng.uniform(1.3,2.0), rng.uniform(1.7,2.6), h*rng.uniform(.28,.38)),
                8, 6))
    for y in (-d*.30, 0.0, d*.30):
        banks.append(blob(
            (rng.uniform(-1.2,1.2), y, h*.78),
            (w*.38, rng.uniform(2.0,3.2), rng.uniform(1.3,2.2)),
            8, 6))
    m.part("passage-shell", merge(*banks), "understone", collider=True, smooth=False)
    roots=[]
    for y in (-d*.38,-d*.12,d*.14,d*.40):
        roots.append(tube([Vector((-w*.46,y,.05)),Vector((0,y,h*.78)),Vector((w*.46,y,.05))],
                          [(0,.38),(.5,.30),(1,.38)],10,14))
    m.part("passage-ribs",merge(*roots),"worldroot",collider=True)
    fungus_cluster(m,w,d,h,seed,4,.4)


SPECS = {
    "fracture-mouth": ((28,32,18), fractured_mouth),
    "mycelial-gallery": ((34,46,20), mycelial_gallery),
    "glowcap-vault": ((42,42,28), glowcap_vault),
    "spore-basin": ((38,44,18), spore_basin),
    "root-bridge": ((24,52,24), root_bridge),
    "sunken-nursery": ((40,38,16), sunken_nursery),
    "tangle-junction": ((38,38,22), tangle_junction),
    "shelf-drop": ((30,34,38), shelf_drop),
    "amber-grotto": ((32,36,20), amber_grotto),
    "worldroot-hollow": ((46,48,34), worldroot_hollow),
    "crawler-nest": ((34,36,16), crawler_nest),
    "stalker-den": ((36,42,20), stalker_den),
    "puffback-graze": ((44,46,18), puffback_graze),
    "buried-archway": ((30,40,22), buried_archway),
    "root-squeeze": ((22,38,14), root_squeeze),
    "heartcap-sanctum": ((48,50,30), heartcap_sanctum),
    "passage": ((12,16,12), passage),
}
MODELS = {PREFIX + name: (dims,builder) for name,(dims,builder) in SPECS.items()}


def author(model_id):
    dims,builder=MODELS[model_id]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    m=Model(model_id,2048 if model_id != PREFIX+"passage" else 1024,SURFACES)
    seed=0x5F3759DF ^ sum((i+1)*ord(c) for i,c in enumerate(model_id))
    builder(m,*dims,seed)
    scene=bpy.context.scene
    scene["model_id"]=model_id
    scene["runtime_lights"]="[]"
    scene["surface_finish"]="2"
    scene["underworld_authoring"]=REVISION
    scene["rootwarren_dimensions_m"]=",".join(str(v) for v in dims)
    unwrap(m.parts)
    overlap,coverage=uv_overlap(m.parts,512)
    if overlap>0.012:
        raise RuntimeError(f"{model_id}: UV overlap {overlap:.2%}")
    bake_flora_atlas(m.parts,m.atlas,paint,size=m.atlas_size)
    triangles=0
    for obj in m.parts:
        obj.data.calc_loop_triangles()
        triangles += len(obj.data.loop_triangles)
    if triangles < 1200:
        raise RuntimeError(f"{model_id}: room fidelity regression, only {triangles} triangles")
    bpy.context.preferences.filepaths.save_version=0
    target=SOURCE/(model_id+".blend")
    bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
    print(f"AUTHORED {model_id} dims={dims} parts={len(m.parts)} tris={triangles} "
          f"atlas={m.atlas_size} coverage={coverage:.0%}",flush=True)


requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(MODELS)
unknown=[value for value in requested if value not in MODELS]
if unknown:
    raise SystemExit("Unknown Rootwarren model(s): "+", ".join(unknown))
for model_id in requested:
    author(model_id)
