#!/usr/bin/env python3
"""Author the Frozen Caverns Rime Sepulcher dungeon kit.

Sixteen large rooms plus one adaptive passage. Static geometry communicates shelter, long sightlines,
ice-shear crossings and burial architecture. Whiteout/frost gameplay remains runtime atmosphere
authority; no fake fog or damage planes are authored into the room meshes.
"""
import math,random,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
PREFIX="underworld-dungeon-frozen-rime-sepulcher-"
REVISION="rime-sepulcher-dungeon-r1"
MIN_PARTS=28
MIN_TRIS=1700

SPECS={
"rime-mouth":((34,38,22),"ClearGallery",.24,.08,"mouth"),
"long-glass-gallery":((38,54,24),"ClearGallery",.30,.10,"gallery"),
"burial-colonnade":((42,48,24),"ClearGallery",.34,.12,"colonnade"),
"silent-crossing":((42,42,22),"FrostField",.44,.20,"crossing"),
"icewell-shaft":((30,34,40),"IceShear",.62,.26,"shaft"),
"whiteout-narthex":((34,44,20),"WhiteoutChoke",.58,.74,"narthex"),
"needle-pass":((28,52,22),"IceShear",.68,.34,"needles"),
"rimesilver-ossuary":((38,40,20),"FrostField",.48,.18,"ossuary"),
"clear-ice-lens-vault":((36,42,22),"ClearGallery",.32,.10,"lens"),
"still-air-crypt":((38,40,20),"Shelter",.06,.02,"crypt"),
"frost-tick-niche":((34,38,18),"FrostField",.46,.20,"niche"),
"iceblind-hunt":((42,46,22),"WhiteoutChoke",.56,.66,"hunt"),
"cryolith-guard":((46,48,28),"IceShear",.64,.28,"guard"),
"frozen-archive":((44,48,26),"ClearGallery",.28,.08,"archive"),
"shelter-chapel":((40,42,22),"Shelter",.08,.03,"chapel"),
"white-silence-antechamber":((50,54,30),"WhiteoutChoke",.62,.82,"antechamber"),
"passage":((14,16,14),"Adaptive",.36,.20,"passage"),
}

def mat(semantic):
 name="magenheim.rime-sepulcher."+semantic
 m=bpy.data.materials.get(name)
 if m:return m
 m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=True
 m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value=(1,1,1,1)
 bind_underworld_material(bpy,m,name);return m

def finish(o,name,m,collision=False):
 o.name=name;o["game_node_path"]=name;o["game_collision"]=bool(collision);o["game_crystal"]="null"
 o.data.materials.clear();o.data.materials.append(m)
 if not o.data.uv_layers:o.data.uv_layers.new(name="RimeUV")
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode="EDIT");bpy.ops.mesh.select_all(action="SELECT")
 bpy.ops.uv.cube_project(cube_size=2.0);bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False)
 return o
def box(name,loc,size,m,collision=False,rot=(0,0,0),bevel=.11):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot);o=bpy.context.object;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  md=o.modifiers.new("worked-edge","BEVEL");md.width=min(bevel,min(size)*.22);md.segments=2
 return finish(o,name,m,collision)
def rock(name,loc,scale,m,collision=False,rot=(0,0,0)):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=loc);o=bpy.context.object;o.scale=scale;o.rotation_euler=rot
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,m,collision)
def cyl(name,loc,radius,depth,m,collision=False,rot=(0,0,0),verts=14):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=depth,location=loc,rotation=rot);return finish(bpy.context.object,name,m,collision)
def cone(name,loc,r1,r2,depth,m,collision=False,rot=(0,0,0),verts=10):
 bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=depth,location=loc,rotation=rot);return finish(bpy.context.object,name,m,collision)
def torus(name,loc,major,minor,m,collision=False,rot=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=8,major_radius=major,minor_radius=minor,location=loc,rotation=rot);return finish(bpy.context.object,name,m,collision)
def tube(name,a,b,r,m,collision=False,verts=12):
 a,b=Vector(a),Vector(b);d=b-a
 if d.length<1e-5:raise ValueError(name+" degenerate")
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(a+b)*.5)
 o=bpy.context.object;o.rotation_mode="QUATERNION";o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
 return finish(o,name,m,collision)

def shell(w,d,h,seed,stone,ice):
 rng=random.Random(seed)
 for i in range(16):
  a=(i+.35)*math.tau/16
  rock(f"rime-shell-{i}",(math.cos(a)*w*.48,math.sin(a)*d*.48,h*rng.uniform(.24,.44)),
       (w*.075+rng.uniform(.7,1.5),d*.07+rng.uniform(.7,1.5),h*rng.uniform(.24,.36)),stone,True,(rng.uniform(-.2,.2),rng.uniform(-.2,.2),a))
 for i in range(8):
  a=(i+.2)*math.tau/8
  cone(f"ceiling-icicle-{i}",(math.cos(a)*w*.26,math.sin(a)*d*.26,h*.78),.45,.05,h*.24,ice,False,(0,0,a),8)

def floor(w,d,route,stone,ice):
 box("stone-floor",(0,0,-.42),(w*.44,d*.44,.42),stone,True,bevel=.15)
 # Risk routes leave a stable outer ledge; ice-shear geometry occupies only the middle.
 if route in ("FrostField","WhiteoutChoke","IceShear"):
  for side in (-1,1):box(f"shelter-ledge-{side}",(side*w*.35,0,.28),(w*.065,d*.34,.28),stone,True,bevel=.08)
 if route=="IceShear":
  for i in range(7):
   y=-d*.30+i*d*.10;side=-1 if i%2 else 1
   cone(f"shear-fin-{i}",(side*w*.10,y,1.9),1.1,.08,3.8,ice,True,(0,.20*side,0),7)

def funerary(w,d,h,seed,wood,silver,ice):
 rng=random.Random(seed)
 for side in (-1,1):
  for i,y in enumerate((-d*.30,-d*.10,d*.10,d*.30)):
   box(f"rime-pier-{side}-{i}",(side*w*.32,y,h*.18),(1.0,1.0,h*.18),wood,True,bevel=.10)
   torus(f"pier-silver-{side}-{i}",(side*w*.32,y,h*.30),1.03,.08,silver,False,(math.pi/2,0,0))
 for i,y in enumerate((-d*.25,0,d*.25)):
  tube(f"ice-rib-{i}",(-w*.30,y,h*.46),(w*.30,y,h*.46),.24,ice,False)

def feature(kind,w,d,h,route,stone,ice,silver,wood):
 if kind=="mouth":
  for side in (-1,1):box(f"mouth-pillar-{side}",(side*w*.22,-d*.22,h*.20),(1.4,1.6,h*.20),stone,True)
  tube("mouth-lintel",(-w*.24,-d*.22,h*.42),(w*.24,-d*.22,h*.42),.34,silver,True)
 elif kind=="gallery":
  # Keep centre sightline deliberately open.
  for side in (-1,1):
   for i,y in enumerate((-d*.32,-d*.16,0,d*.16,d*.32)):
    cone(f"gallery-fin-{side}-{i}",(side*w*.30,y,2.2),.65,.07,4.4,ice,True,(0,.08*side,0),8)
  for i,y in enumerate((-d*.25,0,d*.25)):torus(f"gallery-arch-{i}",(0,y,h*.42),w*.27,.22,silver,False,(math.pi/2,0,0))
 elif kind=="colonnade":
  for side in (-1,1):
   for i,y in enumerate((-d*.30,-d*.15,0,d*.15,d*.30)):
    cyl(f"burial-column-{side}-{i}",(side*w*.28,y,h*.18),.75,h*.36,stone,True,verts=12)
    box(f"burial-plaque-{side}-{i}",(side*w*.24,y,1.5),(.18,.62,.48),silver,False,bevel=.04)
 elif kind=="crossing":
  for i in range(8):
   a=i*math.tau/8
   cone(f"crossing-fin-{i}",(math.cos(a)*w*.22,math.sin(a)*d*.22,2.0),.8,.06,4.0,ice,True,(0,.15*math.cos(a),a),7)
  box("crossing-dais",(0,0,.55),(w*.13,d*.13,.55),stone,True)
 elif kind=="shaft":
  for i,z in enumerate((2,6,10,14,18,22,26)):
   torus(f"shaft-ring-{i}",(0,0,z),w*.25,.28,ice,True)
  for side in (-1,1):tube(f"shaft-rail-{side}",(side*w*.18,0,1),(side*w*.18,0,h*.80),.14,silver,True)
 elif kind=="narthex":
  for i,y in enumerate((-d*.30,-d*.15,0,d*.15,d*.30)):
   box(f"wind-baffle-{i}",((-.10 if i%2 else .10)*w,y,1.6),(w*.18,.55,1.6),ice,True,rot=(0,0,.08*(-1 if i%2 else 1)))
  for side in (-1,1):tube(f"guide-rail-{side}",(side*w*.32,-d*.34,1.1),(side*w*.32,d*.34,1.1),.12,silver,False)
 elif kind=="needles":
  for i in range(12):
   x=(-1 if i%2 else 1)*w*(.10+.025*(i%4));y=-d*.34+i*d*.055
   cone(f"needle-{i}",(x,y,2.4),.70,.045,4.8,ice,True,(0,.18*(-1 if i%2 else 1),0),7)
 elif kind=="ossuary":
  for side in (-1,1):
   for row in range(4):
    y=-d*.28+row*d*.18
    box(f"ossuary-shelf-{side}-{row}",(side*w*.28,y,1.5),(1.0,2.2,1.5),stone,True)
    for tier in range(3):box(f"rimesilver-casket-{side}-{row}-{tier}",(side*w*.22,y,.55+tier*.75),(.38,1.5,.18),silver,False,bevel=.04)
 elif kind=="lens":
  for i,x in enumerate((-w*.20,0,w*.20)):
   cyl(f"ice-lens-{i}",(x,0,2.1),1.35,.22,ice,False,(math.pi/2,0,0),20)
   torus(f"lens-frame-{i}",(x,0,2.1),1.38,.12,silver,True,(math.pi/2,0,0))
   tube(f"lens-stand-{i}",(x,0,.4),(x,0,1.3),.18,wood,True)
 elif kind=="crypt":
  box("still-air-chamber",(0,0,.75),(w*.24,d*.22,.75),stone,True)
  for side in (-1,1):box(f"crypt-bench-{side}",(side*w*.18,0,.55),(1.2,d*.16,.55),wood,True)
  for i,x in enumerate((-w*.12,0,w*.12)):torus(f"quiet-ring-{i}",(x,d*.18,1.8),.55,.07,silver,False,(math.pi/2,0,0))
 elif kind=="niche":
  for side in (-1,1):
   for i,y in enumerate((-d*.28,-d*.10,d*.10,d*.28)):
    box(f"tick-niche-{side}-{i}",(side*w*.30,y,1.2),(.9,1.1,1.2),stone,True)
    cone(f"niche-ice-{side}-{i}",(side*w*.24,y,2.7),.45,.04,2.2,ice,False,verts=7)
 elif kind=="hunt":
  for i in range(9):
   a=i*2.39996;r=w*(.10+.018*(i%3))
   cone(f"hunt-obscurer-{i}",(math.cos(a)*r,math.sin(a)*r,2.0),.65,.05,4.0,ice,True,(0,.14*math.sin(a),a),7)
  for side in (-1,1):tube(f"hunter-guide-{side}",(side*w*.32,-d*.34,1.0),(side*w*.32,d*.34,1.0),.13,silver,False)
 elif kind=="guard":
  for i in range(8):
   a=i*math.tau/8
   box(f"guardian-plinth-{i}",(math.cos(a)*w*.25,math.sin(a)*d*.25,.65),(1.4,1.4,.65),stone,True,(0,0,a))
   cone(f"guardian-spire-{i}",(math.cos(a)*w*.25,math.sin(a)*d*.25,3.0),.65,.06,4.7,ice,False,(0,.10,a),8)
 elif kind=="archive":
  for side in (-1,1):
   for row in range(4):
    y=-d*.28+row*d*.18
    box(f"archive-stack-{side}-{row}",(side*w*.29,y,2.0),(1.1,2.2,2.0),wood,True)
    for tier in range(3):box(f"archive-slate-{side}-{row}-{tier}",(side*w*.22,y,.8+tier*.9),(.38,1.5,.16),silver,False,bevel=.04)
 elif kind=="chapel":
  for tier,(sx,sy,z) in enumerate(((.25,.20,.45),(.18,.14,1.05),(.11,.09,1.55))):
   box(f"chapel-dais-{tier}",(0,0,z),(w*sx,d*sy,.45),stone,True)
  for side in (-1,1):box(f"chapel-bench-{side}",(side*w*.22,-d*.05,.55),(1.2,d*.24,.55),wood,True)
  torus("chapel-halo",(0,d*.18,3.1),1.3,.10,ice,False,(math.pi/2,0,0))
 elif kind=="antechamber":
  for i in range(10):
   a=i*math.tau/10
   tube(f"silence-rib-{i}",(math.cos(a)*w*.28,math.sin(a)*d*.28,.5),(math.cos(a)*w*.17,math.sin(a)*d*.17,h*.52),.24,silver,True)
   cone(f"silence-fin-{i}",(math.cos(a)*w*.17,math.sin(a)*d*.17,h*.56),.55,.05,3.5,ice,False,(0,.10,a),8)
  box("silence-dais",(0,0,.75),(w*.18,d*.16,.75),stone,True)
 elif kind=="passage":
  box("passage-floor",(0,0,-.35),(5.7,d*.46,.35),stone,True)
  for side in (-1,1):box(f"passage-ledge-{side}",(side*5.6,0,.32),(1.0,d*.46,.32),stone,True)
  for i,y in enumerate((-d*.34,-d*.12,d*.12,d*.34)):torus(f"passage-ice-rib-{i}",(0,y,4.0),5.5,.22,ice,True,(math.pi/2,0,0))

def author(mid):
 suffix=mid.removeprefix(PREFIX);dims,route,cold,whiteout,kind=SPECS[suffix];w,d,h=dims
 bpy.ops.wm.read_factory_settings(use_empty=True)
 stone=mat("understone");ice=mat("clear-ice");silver=mat("rimesilver");wood=mat("rimewood")
 seed=0x71CE51 ^ sum((i+1)*ord(c) for i,c in enumerate(mid))
 shell(w,d,h,seed,stone,ice);floor(w,d,route,stone,ice);funerary(w,d,h,seed+13,wood,silver,ice);feature(kind,w,d,h,route,stone,ice,silver,wood)
 meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"];tris=0
 for o in meshes:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
 if len(meshes)<MIN_PARTS:raise RuntimeError(f"{mid}: detail regression {len(meshes)} < {MIN_PARTS}")
 if tris<MIN_TRIS:raise RuntimeError(f"{mid}: fidelity regression {tris} < {MIN_TRIS}")
 sc=bpy.context.scene;sc["model_id"]=mid;sc["underworld_authoring"]=REVISION;sc["runtime_lights"]="[]"
 sc["rime_dimensions_m"]=",".join(str(x) for x in dims);sc["rime_route"]=route;sc["rime_cold_exposure01"]=float(cold);sc["rime_whiteout_intensity01"]=float(whiteout)
 sc["rime_weather_geometry"]="none-runtime-atmosphere-authority"
 bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(mid+".blend")),compress=True)
 print("AUTHORED",mid,route,"cold",cold,"whiteout",whiteout,"parts",len(meshes),"tris",tris,flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
unknown=[x for x in requested if x.removeprefix(PREFIX) not in SPECS]
if unknown:raise SystemExit("Unknown Rime Sepulcher model(s): "+", ".join(unknown))
for mid in requested:author(mid)
