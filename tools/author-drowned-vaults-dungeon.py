#!/usr/bin/env python3
"""Author the Blackwater Deep Drowned Vaults room kit.

Water is deliberately NOT authored as geometry. Runtime DrownedVaultWaterRuntime owns real native
Valheim WaterVolume instances at room-local Z=0 (Blender Z -> Unity Y). Wet floors therefore live
below zero; dry shelves, docks, air pockets and causeways live above it.
"""
import math,random,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
PREFIX="underworld-dungeon-blackwater-drowned-vaults-"
REVISION="drowned-vaults-dungeon-r1"
MIN_PARTS=24
MIN_TRIS=1500

SPECS={
"drowned-sinkhole":((34,38,22),"Mixed",2.5,"sinkhole"),
"tide-gallery":((36,48,20),"Mixed",1.8,"gallery"),
"dry-ledger":((30,42,18),"Dry",0.0,"archive"),
"collapsed-dock":((44,46,24),"Mixed",2.8,"dock"),
"siphon-hall":((34,50,20),"Flooded",5.5,"siphon"),
"bell-chamber":((38,40,26),"AirPocket",3.5,"bell"),
"split-cistern":((42,42,24),"Mixed",3.0,"cistern"),
"drowned-shaft":((28,32,34),"VerticalWater",12.0,"shaft"),
"pearl-vault":((34,38,20),"Flooded",4.5,"pearl"),
"high-water-archive":((40,44,28),"AirPocket",2.5,"higharchive"),
"lamprey-run":((30,46,18),"Flooded",4.0,"run"),
"deep-hunter-lair":((46,48,26),"Flooded",7.0,"lair"),
"sunken-quay":((44,52,20),"Mixed",2.0,"quay"),
"broken-causeway":((26,54,20),"Dry",0.0,"causeway"),
"undertow-sluice":((30,38,28),"VerticalWater",8.0,"sluice"),
"abyssal-sanctum":((50,52,30),"Mixed",4.0,"sanctum"),
"passage":((14,16,14),"Adaptive",3.0,"passage"),
}

def mat(semantic):
 name="magenheim.drowned-vaults."+semantic
 existing=bpy.data.materials.get(name)
 if existing:return existing
 m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=True
 bs=m.node_tree.nodes.get("Principled BSDF");bs.inputs["Base Color"].default_value=(1,1,1,1)
 bind_underworld_material(bpy,m,name);return m

def finish(o,name,material,collider=False):
 o.name=name;o["game_node_path"]=name;o["game_collision"]=bool(collider);o["game_crystal"]="null"
 o.data.materials.clear();o.data.materials.append(material)
 if not o.data.uv_layers:o.data.uv_layers.new(name="VaultUV")
 bpy.context.view_layer.objects.active=o;o.select_set(True)
 bpy.ops.object.mode_set(mode="EDIT");bpy.ops.mesh.select_all(action="SELECT");bpy.ops.uv.cube_project(cube_size=2.0)
 bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False)
 return o

def box(name,loc,size,material,collider=False,rot=(0,0,0),bevel=.10):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot);o=bpy.context.object;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel>0:
  mod=o.modifiers.new("worked-edge","BEVEL");mod.width=min(bevel,min(size)*.22);mod.segments=2
 return finish(o,name,material,collider)

def rock(name,loc,scale,material,collider=False,rot=(0,0,0)):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=loc);o=bpy.context.object
 o.scale=scale;o.rotation_euler=rot;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return finish(o,name,material,collider)

def cyl(name,loc,radius,depth,material,collider=False,rot=(0,0,0),verts=14):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=depth,location=loc,rotation=rot)
 return finish(bpy.context.object,name,material,collider)

def cone(name,loc,r1,r2,depth,material,collider=False,rot=(0,0,0),verts=10):
 bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=depth,location=loc,rotation=rot)
 return finish(bpy.context.object,name,material,collider)

def torus(name,loc,major,minor,material,collider=False,rot=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=8,major_radius=major,minor_radius=minor,location=loc,rotation=rot)
 return finish(bpy.context.object,name,material,collider)

def tube(name,a,b,radius,material,collider=False,verts=12):
 a,b=Vector(a),Vector(b);d=b-a
 if d.length<1e-5:raise ValueError(name+" degenerate tube")
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=d.length,location=(a+b)*.5)
 o=bpy.context.object;o.rotation_mode="QUATERNION";o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
 return finish(o,name,material,collider)

def cave_shell(w,d,h,seed,understone):
 rng=random.Random(seed)
 for i in range(16):
  a=(i+.5)*math.tau/16;x=math.cos(a)*w*.48;y=math.sin(a)*d*.48
  rock(f"cavern-shell-{i}",(x,y,h*rng.uniform(.24,.45)),(w*.08+rng.uniform(.6,1.4),d*.07+rng.uniform(.6,1.5),h*rng.uniform(.25,.38)),understone,True,(rng.uniform(-.2,.2),rng.uniform(-.2,.2),a))
 for i in range(10):
  a=(i+.27)*math.tau/10;x=math.cos(a)*w*rng.uniform(.12,.30);y=math.sin(a)*d*rng.uniform(.12,.30)
  rock(f"ceiling-mass-{i}",(x,y,h*rng.uniform(.76,.91)),(rng.uniform(2.4,4.3),rng.uniform(2.4,4.3),rng.uniform(1.8,3.0)),understone,True)

def base_floor(w,d,mode,depth,understone):
 if mode=="Dry":
  box("dry-floor",(0,0,-.55),(w*.44,d*.44,.55),understone,True,bevel=.18)
 else:
  box("flood-floor",(0,0,-depth-.55),(w*.44,d*.44,.55),understone,True,bevel=.18)
  # Permanent waterline geology; this is rock trim, not a rendered water plane.
  for i in range(12):
   a=i*math.tau/12
   rock(f"waterline-flowstone-{i}",(math.cos(a)*w*.40,math.sin(a)*d*.40,-.05),(1.2,.75,.55),mat("flowstone"),True,(0,0,a))

def dry_shelves(w,d,mode,flowstone):
 if mode in ("Mixed","AirPocket","VerticalWater"):
  for side in (-1,1):
   box(f"dry-shelf-{side}",(side*w*.34,0,.55),(w*.10,d*.34,.55),flowstone,True,bevel=.14)
 if mode=="AirPocket":
  box("air-pocket-dais",(0,d*.24,1.10),(w*.22,d*.10,1.10),flowstone,True,bevel=.16)

def ruins(w,d,h,seed,iron,flowstone):
 rng=random.Random(seed)
 for i in range(8):
  side=-1 if i%2==0 else 1;y=-d*.34+(i//2)*d*.22
  z=rng.uniform(2.0,h*.28)
  box(f"ruin-pier-{i}",(side*w*.30,y,z*.5),(1.1,1.1,z*.5),flowstone,True,(0,0,rng.uniform(-.08,.08)),.12)
  torus(f"pier-collar-{i}",(side*w*.30,y,z*.72),1.12,.10,iron,False,(math.pi/2,0,0))
 for row,y in enumerate((-d*.28,0,d*.28)):
  tube(f"vault-rib-{row}",(-w*.29,y,h*.42),(w*.29,y,h*.42),.30,iron,False)

def flowstone_growth(w,d,h,seed,flowstone):
 rng=random.Random(seed)
 for i in range(10):
  x=rng.uniform(-w*.38,w*.38);y=rng.uniform(-d*.38,d*.38)
  if abs(x)<w*.13 and abs(y)<d*.13:y+=d*.20
  length=rng.uniform(1.6,min(5.5,h*.28))
  cone(f"flowstone-drop-{i}",(x,y,h-length*.5),.35,.06,length,flowstone,False)
 for i in range(7):
  x=rng.uniform(-w*.35,w*.35);y=rng.uniform(-d*.35,d*.35)
  cone(f"flowstone-rise-{i}",(x,y,-.05+rng.uniform(.4,1.0)),.45,.08,rng.uniform(1.0,2.2),flowstone,False)

def pearl_cluster(w,d,seed,pearl):
 rng=random.Random(seed)
 for i in range(10):
  side=-1 if i%2 else 1;x=side*w*rng.uniform(.20,.38);y=rng.uniform(-d*.34,d*.34);z=rng.uniform(.45,2.6)
  rock(f"pearl-node-{i}",(x,y,z),(.22,.22,.22),pearl,False)

def fibre_rigging(w,d,h,seed,fibre):
 rng=random.Random(seed)
 for i in range(6):
  x=rng.uniform(-w*.28,w*.28)
  tube(f"pale-rigging-{i}",(x,-d*.34,h*.12),(x+rng.uniform(-2,2),d*.34,h*.28),.055,fibre,False,10)

def feature(kind,w,d,h,depth,understone,flowstone,pearl,fibre,iron,seed):
 if kind=="sinkhole":
  for i in range(6):
   r=w*(.30-i*.025);z=3.0-i*.75
   torus(f"sink-ring-{i}",(0,0,z),r,.35,flowstone,True)
  for i in range(5):box(f"descent-step-{i}",(0,-d*.24+i*1.8,2.6-i*.62),(4.5,1.4,.28),understone,True,.0 if False else (0,0,0),.10)
 elif kind in ("dock","quay"):
  for i in range(7):
   y=-d*.30+i*d*.10;box(f"dock-deck-{i}",(0,y,1.0),(w*.23,1.2,.18),flowstone,True,bevel=.06)
  for side in (-1,1):
   for i,y in enumerate((-d*.30,-d*.10,d*.10,d*.30)):
    cyl(f"dock-pile-{side}-{i}",(side*w*.19,y,-depth*.35),.30,max(2.2,depth+2.2),iron,True)
  fibre_rigging(w,d,h,seed+30,fibre)
 elif kind in ("archive","higharchive"):
  base=1.0 if kind=="higharchive" else .2
  for side in (-1,1):
   for row in range(4):
    y=-d*.28+row*d*.18
    box(f"archive-shelf-{side}-{row}",(side*w*.28,y,base+2.2),(1.0,2.4,2.2),flowstone,True,bevel=.10)
    for tier in range(3):box(f"archive-ledger-{side}-{row}-{tier}",(side*w*.20,y,base+.8+tier*1.0),(.45,1.65,.20),fibre,False,bevel=.04)
 elif kind=="siphon":
  for i,y in enumerate((-d*.30,-d*.10,d*.10,d*.30)):
   torus(f"siphon-ring-{i}",(0,y,h*.38),w*.22,.28,iron,True,(math.pi/2,0,0))
   tube(f"siphon-drop-{i}",(w*.22,y,h*.38),(w*.22,y,-depth*.70),.22,iron,True)
 elif kind=="bell":
  torus("bell-yoke",(0,0,h*.55),w*.18,.35,iron,True,(math.pi/2,0,0))
  cone("bell-body",(0,0,h*.36),w*.13,w*.23,h*.26,flowstone,True)
  cyl("bell-clapper",(0,0,h*.20),.22,h*.16,iron,False)
  for i in range(6):torus(f"bell-rune-{i}",(0,0,h*.29+i*.22),w*(.14+i*.01),.06,pearl,False)
 elif kind=="cistern":
  box("cistern-divider",(0,0,h*.18),(.55,d*.38,h*.18),flowstone,True,bevel=.10)
  for side in (-1,1):
   torus(f"cistern-valve-{side}",(side*w*.24,0,2.2),1.0,.13,iron,False,(math.pi/2,0,0))
   tube(f"cistern-channel-{side}",(side*w*.24,-d*.35,.4),(side*w*.24,d*.35,.4),.18,iron,False)
 elif kind=="shaft":
  for i,z in enumerate((-depth*.82,-depth*.45,-depth*.10,3.0,7.0,11.0)):
   torus(f"shaft-ring-{i}",(0,0,z),w*.26,.32,flowstone,True)
  for side in (-1,1):
   tube(f"shaft-chain-{side}",(side*w*.18,0,h*.60),(side*w*.18,0,-depth*.88),.10,iron,False)
 elif kind=="pearl":
  for side in (-1,1):
   box(f"pearl-rack-{side}",(side*w*.25,d*.10,1.6),(1.2,d*.22,1.6),flowstone,True,bevel=.12)
   for i in range(6):rock(f"vault-pearl-{side}-{i}",(side*w*.22,-d*.10+i*d*.04,1.0+(i%2)*.7),(.38,.38,.38),pearl,False)
 elif kind=="run":
  for i,y in enumerate((-d*.34,-d*.18,0,d*.18,d*.34)):
   torus(f"run-rib-{i}",(0,y,h*.34),w*.28,.24,flowstone,True,(math.pi/2,0,0))
 elif kind=="lair":
  for i in range(8):
   a=i*math.tau/8
   rock(f"lair-alcove-{i}",(math.cos(a)*w*.30,math.sin(a)*d*.30,2.0),(3.2,3.2,2.8),understone,True,(0,0,a))
  box("hunter-pit",(0,0,-depth*.82),(w*.18,d*.18,.55),flowstone,True,bevel=.18)
 elif kind=="causeway":
  for i in range(9):
   y=-d*.34+i*d*.085
   box(f"causeway-span-{i}",(0,y,.55),(w*.18,1.4,.55),flowstone,True,bevel=.12)
  for side in (-1,1):
   tube(f"causeway-rail-{side}",(side*w*.16,-d*.36,1.8),(side*w*.16,d*.36,1.8),.16,iron,False)
 elif kind=="sluice":
  for i,z in enumerate((-depth*.65,-depth*.30,1.5,6.0)):
   box(f"sluice-gate-{i}",(0,0,z),(w*.28,.45,.35),iron,True,bevel=.10)
   for side in (-1,1):tube(f"sluice-guide-{i}-{side}",(side*w*.27,-.6,z-2.2),(side*w*.27,-.6,z+2.2),.18,flowstone,True)
 elif kind=="sanctum":
  for tier,(sx,sy,z) in enumerate(((.28,.24,.45),(.22,.18,1.15),(.14,.12,1.80))):
   box(f"sanctum-dais-{tier}",(0,0,z),(w*sx,d*sy,.45),flowstone,True,bevel=.18)
  for i in range(10):
   a=i*math.tau/10
   tube(f"sanctum-spire-{i}",(math.cos(a)*w*.24,math.sin(a)*d*.24,.5),(math.cos(a)*w*.17,math.sin(a)*d*.17,h*.52),.24,iron,True)
   rock(f"sanctum-pearl-{i}",(math.cos(a)*w*.17,math.sin(a)*d*.17,h*.54),(.40,.40,.40),pearl,False)
 elif kind=="gallery":
  for side in (-1,1):
   tube(f"gallery-rail-{side}",(side*w*.24,-d*.36,1.7),(side*w*.24,d*.36,1.7),.15,iron,False)
 elif kind=="passage":
  box("adaptive-channel",(0,0,-3.55),(3.2,d*.46,.45),understone,True,bevel=.12)
  for side in (-1,1):box(f"adaptive-ledge-{side}",(side*4.3,0,.35),(1.1,d*.46,.35),flowstone,True,bevel=.10)
  for i,y in enumerate((-d*.36,-d*.12,d*.12,d*.36)):torus(f"passage-rib-{i}",(0,y,4.2),5.4,.24,flowstone,True,(math.pi/2,0,0))

def author(model_id):
 suffix=model_id.removeprefix(PREFIX)
 dims,mode,depth,kind=SPECS[suffix];w,d,h=dims
 bpy.ops.wm.read_factory_settings(use_empty=True)
 understone=mat("understone");flowstone=mat("flowstone");pearl=mat("blackwater-pearl")
 fibre=mat("pale-fibre");iron=mat("pearl-metal")
 seed=0xB1AC4A7E ^ sum((i+1)*ord(c) for i,c in enumerate(model_id))
 cave_shell(w,d,h,seed,understone)
 base_floor(w,d,mode if mode!="Adaptive" else "Mixed",depth,understone)
 dry_shelves(w,d,mode,flowstone)
 ruins(w,d,h,seed+11,iron,flowstone)
 flowstone_growth(w,d,h,seed+17,flowstone)
 pearl_cluster(w,d,seed+23,pearl)
 feature(kind,w,d,h,depth,understone,flowstone,pearl,fibre,iron,seed)
 if mode!="Dry":fibre_rigging(w,d,h,seed+47,fibre)
 meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
 tris=0
 for o in meshes:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
 if len(meshes)<MIN_PARTS:raise RuntimeError(f"{model_id}: detail regression {len(meshes)} < {MIN_PARTS}")
 if tris<MIN_TRIS:raise RuntimeError(f"{model_id}: fidelity regression {tris} < {MIN_TRIS} triangles")
 scene=bpy.context.scene;scene["model_id"]=model_id;scene["underworld_authoring"]=REVISION;scene["runtime_lights"]="[]"
 scene["drowned_vault_dimensions_m"]=",".join(str(x) for x in dims)
 scene["drowned_vault_route_mode"]=mode;scene["drowned_vault_water_depth_m"]=float(depth);scene["drowned_vault_waterline_z"]=0.0
 scene["drowned_vault_native_water"]="runtime-WaterVolume-only"
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(model_id+".blend")),compress=True)
 print("AUTHORED",model_id,mode,"depth",depth,"parts",len(meshes),"tris",tris,flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
unknown=[x for x in requested if x.removeprefix(PREFIX) not in SPECS]
if unknown:raise SystemExit("Unknown Drowned Vault model(s): "+", ".join(unknown))
for model_id in requested:author(model_id)
