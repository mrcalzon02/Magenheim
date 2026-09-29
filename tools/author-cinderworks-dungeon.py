#!/usr/bin/env python3
"""Author the Sulfurous Wastes Cinderworks dungeon kit.

Sixteen large rooms plus one adaptive passage. Geometry communicates thermal routing: Safe/Warm
rooms retain raised bypasses and quench/recovery space; Hot/VentCycle/SlagChannel rooms expose
increasing furnace, vent and slag infrastructure. Runtime owns actual heat accumulation.
"""
import math,random,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
PREFIX="underworld-dungeon-sulfur-cinderworks-"
REVISION="cinderworks-dungeon-r1"
MIN_PARTS=28
MIN_TRIS=1700

SPECS={
"cinder-gate":((34,38,22),"Warm",.22,"gate"),
"slag-nave":((38,48,22),"Warm",.30,"nave"),
"furnace-gallery":((36,50,24),"Hot",.52,"gallery"),
"bellows-junction":((42,42,24),"Warm",.34,"junction"),
"chimney-shaft":((30,34,38),"VentCycle",.66,"shaft"),
"vent-choir":((40,44,24),"VentCycle",.78,"vents"),
"slag-runoff":((32,52,20),"SlagChannel",.82,"slag"),
"emberiron-foundry":((42,44,22),"Hot",.48,"foundry"),
"sulfur-kiln":((34,40,20),"Warm",.36,"kiln"),
"quench-vault":((38,40,22),"Safe",.08,"quench"),
"ashmite-conveyor":((34,46,18),"Warm",.32,"conveyor"),
"cinder-hound-yard":((42,46,20),"Hot",.56,"yard"),
"furnace-golem-crucible":((46,48,28),"SlagChannel",.74,"crucible"),
"broken-smeltery":((44,50,26),"Warm",.28,"smeltery"),
"pressure-lock":((28,38,18),"Safe",.10,"lock"),
"furnace-heart-antechamber":((50,52,30),"Hot",.64,"antechamber"),
"passage":((14,16,14),"Adaptive",.42,"passage"),
}

def mat(semantic):
 name="magenheim.cinderworks."+semantic
 m=bpy.data.materials.get(name)
 if m:return m
 m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=True
 m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value=(1,1,1,1)
 bind_underworld_material(bpy,m,name);return m

def finish(o,name,m,collision=False):
 o.name=name;o["game_node_path"]=name;o["game_collision"]=bool(collision);o["game_crystal"]="null"
 o.data.materials.clear();o.data.materials.append(m)
 if not o.data.uv_layers:o.data.uv_layers.new(name="CinderUV")
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode="EDIT");bpy.ops.mesh.select_all(action="SELECT")
 bpy.ops.uv.cube_project(cube_size=2.0);bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False)
 return o

def box(name,loc,size,m,collision=False,rot=(0,0,0),bevel=.12):
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

def shell(w,d,h,seed,stone):
 rng=random.Random(seed)
 for i in range(16):
  a=(i+.3)*math.tau/16
  rock(f"basalt-shell-{i}",(math.cos(a)*w*.48,math.sin(a)*d*.48,h*rng.uniform(.25,.43)),
       (w*.075+rng.uniform(.7,1.5),d*.07+rng.uniform(.7,1.6),h*rng.uniform(.24,.36)),stone,True,(rng.uniform(-.2,.2),rng.uniform(-.2,.2),a))
 for i in range(9):
  a=(i+.17)*math.tau/9
  rock(f"roof-mass-{i}",(math.cos(a)*w*.24,math.sin(a)*d*.24,h*rng.uniform(.78,.90)),
       (rng.uniform(2.5,4.5),rng.uniform(2.5,4.5),rng.uniform(1.8,3.0)),stone,True,(0,0,a))

def common_floor(w,d,route,stone,slag):
 # All rooms have a walkable structural datum. Hazard routes cut channels through it rather than
 # replacing the whole floor with unavoidable damage.
 box("main-floor",(0,0,-.45),(w*.44,d*.44,.45),stone,True,bevel=.16)
 if route in ("Hot","VentCycle","SlagChannel"):
  for side in (-1,1):
   box(f"safe-ledge-{side}",(side*w*.35,0,.35),(w*.07,d*.34,.35),stone,True,bevel=.10)
 if route=="SlagChannel":
  box("slag-channel-bed",(0,0,-.30),(w*.17,d*.39,.20),slag,True,bevel=.08)
  for side in (-1,1):box(f"slag-curb-{side}",(side*w*.20,0,.25),(w*.025,d*.39,.55),stone,True,bevel=.06)

def industrial(w,d,h,seed,iron,slag,charred):
 rng=random.Random(seed)
 for i,y in enumerate((-d*.31,-d*.10,d*.10,d*.31)):
  for side in (-1,1):
   z=rng.uniform(2.5,h*.32)
   box(f"furnace-pier-{side}-{i}",(side*w*.32,y,z*.5),(1.05,1.05,z*.5),slag,True,bevel=.10)
   torus(f"pier-band-{side}-{i}",(side*w*.32,y,z*.72),1.08,.10,iron,False,(math.pi/2,0,0))
 for i,y in enumerate((-d*.25,0,d*.25)):
  tube(f"overhead-main-{i}",(-w*.31,y,h*.45),(w*.31,y,h*.45),.26,iron,False)
  for side in (-1,1):tube(f"hanger-{i}-{side}",(side*w*.22,y,h*.45),(side*w*.22,y,h*.28),.08,charred,False)

def vent_array(w,d,h,count,iron,heat):
 for i in range(count):
  x=(-1 if i%2 else 1)*w*(.16+.035*(i%3));y=-d*.30+(i/(max(1,count-1)))*d*.60
  cyl(f"vent-stack-{i}",(x,y,h*.23),.55,h*.46,iron,True,verts=16)
  torus(f"vent-collar-{i}",(x,y,h*.43),.58,.09,heat,False)
  cone(f"vent-cap-{i}",(x,y,h*.50),.62,.28,.65,iron,False,verts=12)

def feature(kind,w,d,h,route,stone,slag,iron,heat,charred,sulfur):
 if kind=="gate":
  for side in (-1,1):box(f"gate-pillar-{side}",(side*w*.22,-d*.20,h*.22),(1.5,1.8,h*.22),slag,True)
  tube("gate-lintel",(-w*.25,-d*.20,h*.45),(w*.25,-d*.20,h*.45),.40,iron,True)
  for i in range(5):torus(f"gate-mark-{i}",(0,-d*.18,2.0+i*.7),w*(.06+i*.012),.06,heat,False,(math.pi/2,0,0))
 elif kind=="nave":
  for i,y in enumerate((-d*.30,-d*.15,0,d*.15,d*.30)):
   torus(f"nave-rib-{i}",(0,y,h*.38),w*.27,.28,slag,True,(math.pi/2,0,0))
 elif kind=="gallery":
  vent_array(w,d,h,5,iron,heat)
  for side in (-1,1):tube(f"gallery-bypass-{side}",(side*w*.34,-d*.34,1.3),(side*w*.34,d*.34,1.3),.18,iron,False)
 elif kind=="junction":
  for a in range(4):
   ang=a*math.pi/2;tube(f"bellows-arm-{a}",(0,0,2.0),(math.cos(ang)*w*.27,math.sin(ang)*d*.27,2.0),.35,iron,True)
   box(f"bellows-body-{a}",(math.cos(ang)*w*.19,math.sin(ang)*d*.19,1.7),(1.7,1.2,.65),charred,False,(0,0,ang),.12)
 elif kind=="shaft":
  for i,z in enumerate((2.0,6.0,10.0,14.0,h*.58,h*.75)):
   torus(f"chimney-ring-{i}",(0,0,z),w*.25,.32,slag,True)
  for side in (-1,1):tube(f"chimney-rail-{side}",(side*w*.18,0,1.0),(side*w*.18,0,h*.82),.16,iron,True)
  vent_array(w,d,h,4,iron,heat)
 elif kind=="vents":
  vent_array(w,d,h,9,iron,heat)
  for i,y in enumerate((-d*.25,0,d*.25)):box(f"timing-baffle-{i}",(0,y,1.0),(w*.18,.55,1.0),slag,True,bevel=.08)
 elif kind=="slag":
  for i,y in enumerate((-d*.30,-d*.10,d*.10,d*.30)):
   torus(f"slag-weir-{i}",(0,y,1.1),w*.18,.18,iron,True,(math.pi/2,0,0))
 elif kind=="foundry":
  for i,x in enumerate((-w*.22,0,w*.22)):
   box(f"foundry-anvil-{i}",(x,d*.10,.75),(2.0,1.5,.75),iron,True)
   for j in range(3):box(f"ingot-rack-{i}-{j}",(x,-d*.14+j*1.1,1.1),(.7,.35,.18),iron,False)
  vent_array(w,d,h,3,iron,heat)
 elif kind=="kiln":
  for i,x in enumerate((-w*.20,0,w*.20)):
   cyl(f"kiln-{i}",(x,0,h*.20),2.2,h*.40,slag,True,verts=18)
   torus(f"kiln-mouth-{i}",(x,-2.1,h*.15),.80,.16,sulfur,False,(math.pi/2,0,0))
 elif kind=="quench":
  box("quench-basin",(0,0,.40),(w*.24,d*.20,.40),stone,True)
  box("quench-walkway",(0,-d*.28,.55),(w*.30,d*.08,.55),stone,True)
  for i,x in enumerate((-w*.18,0,w*.18)):tube(f"cooling-pipe-{i}",(x,-d*.18,1.0),(x,d*.18,1.0),.20,iron,False)
 elif kind=="conveyor":
  for i,y in enumerate([(-d*.32)+j*d*.08 for j in range(9)]):
   cyl(f"roller-{i}",(0,y,.55),w*.20,.30,iron,True,(0,math.pi/2,0),12)
  for side in (-1,1):tube(f"conveyor-frame-{side}",(side*w*.22,-d*.35,.6),(side*w*.22,d*.35,.6),.18,iron,True)
 elif kind=="yard":
  for i in range(8):
   a=i*math.tau/8
   box(f"yard-post-{i}",(math.cos(a)*w*.28,math.sin(a)*d*.28,1.3),(.55,.55,1.3),slag,True,(0,0,a))
   torus(f"yard-ring-{i}",(math.cos(a)*w*.28,math.sin(a)*d*.28,2.2),.58,.08,iron,False)
 elif kind=="crucible":
  cyl("crucible-shell",(0,0,h*.23),w*.17,h*.34,slag,True,verts=20)
  torus("crucible-rim",(0,0,h*.40),w*.18,.35,iron,True)
  for i in range(6):
   a=i*math.tau/6;tube(f"crucible-brace-{i}",(math.cos(a)*w*.18,math.sin(a)*d*.18,.5),(math.cos(a)*w*.14,math.sin(a)*d*.14,h*.38),.28,iron,True)
 elif kind=="smeltery":
  for i in range(5):
   x=-w*.25+i*w*.125
   cyl(f"broken-stack-{i}",(x,d*.12,h*.15),1.4,h*(.20+.04*(i%2)),slag,True,rot=(.05*(i-2),0,0))
  for i in range(4):box(f"collapsed-beam-{i}",((-1 if i%2 else 1)*w*.18,-d*.12+i*d*.08,1.2),(3.5,.45,.35),iron,True,(0,.20*(-1 if i%2 else 1),.18*i),.08)
 elif kind=="lock":
  for side in (-1,1):box(f"lock-wall-{side}",(side*w*.34,0,h*.25),(w*.08,d*.38,h*.25),stone,True)
  for i,y in enumerate((-d*.20,0,d*.20)):box(f"pressure-door-{i}",(0,y,h*.18),(w*.24,.45,h*.18),iron,True)
  box("recovery-bench",(0,d*.30,.75),(w*.20,1.4,.75),charred,True)
 elif kind=="antechamber":
  for tier,(sx,sy,z) in enumerate(((.28,.24,.45),(.22,.18,1.15),(.15,.12,1.85))):
   box(f"heart-dais-{tier}",(0,0,z),(w*sx,d*sy,.45),slag,True)
  for i in range(10):
   a=i*math.tau/10;tube(f"heart-spoke-{i}",(math.cos(a)*w*.27,math.sin(a)*d*.27,.5),(math.cos(a)*w*.18,math.sin(a)*d*.18,h*.50),.26,iron,True)
   torus(f"heat-node-{i}",(math.cos(a)*w*.18,math.sin(a)*d*.18,h*.51),.28,.07,heat,False)
 elif kind=="passage":
  box("passage-floor",(0,0,-.35),(5.7,d*.46,.35),stone,True)
  for side in (-1,1):box(f"passage-ledge-{side}",(side*5.6,0,.35),(1.0,d*.46,.35),stone,True)
  for i,y in enumerate((-d*.34,-d*.12,d*.12,d*.34)):torus(f"passage-rib-{i}",(0,y,4.1),5.6,.24,slag,True,(math.pi/2,0,0))
  tube("passage-vent-main",(0,-d*.38,2.2),(0,d*.38,2.2),.24,iron,False)

def author(mid):
 suffix=mid.removeprefix(PREFIX)
 dims,route,pressure,kind=SPECS[suffix];w,d,h=dims
 bpy.ops.wm.read_factory_settings(use_empty=True)
 stone=mat("understone");slag=mat("slagstone");iron=mat("emberiron");heat=mat("ember-heat");charred=mat("charred-root");sulfur=mat("sulfur")
 seed=0xC1D3A5 ^ sum((i+1)*ord(c) for i,c in enumerate(mid))
 shell(w,d,h,seed,stone);common_floor(w,d,route,stone,slag);industrial(w,d,h,seed+17,iron,slag,charred)
 if route in ("Hot","VentCycle","SlagChannel"):vent_array(w,d,h,2 if route=="Hot" else 4,iron,heat)
 feature(kind,w,d,h,route,stone,slag,iron,heat,charred,sulfur)
 meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"];tris=0
 for o in meshes:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
 if len(meshes)<MIN_PARTS:raise RuntimeError(f"{mid}: detail regression {len(meshes)} < {MIN_PARTS}")
 if tris<MIN_TRIS:raise RuntimeError(f"{mid}: fidelity regression {tris} < {MIN_TRIS}")
 sc=bpy.context.scene;sc["model_id"]=mid;sc["underworld_authoring"]=REVISION;sc["runtime_lights"]="[]"
 sc["cinderworks_dimensions_m"]=",".join(str(x) for x in dims);sc["cinderworks_heat_route"]=route
 sc["cinderworks_thermal_pressure01"]=float(pressure);sc["cinderworks_damage_geometry"]="none-runtime-thermal-authority"
 bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(mid+".blend")),compress=True)
 print("AUTHORED",mid,route,"pressure",pressure,"parts",len(meshes),"tris",tris,flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
unknown=[x for x in requested if x.removeprefix(PREFIX) not in SPECS]
if unknown:raise SystemExit("Unknown Cinderworks model(s): "+", ".join(unknown))
for mid in requested:author(mid)
