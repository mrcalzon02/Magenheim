#!/usr/bin/env python3
"""Non-destructive Blender gate for Blackwater Drowned Vault room sources."""
import sys
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets/models/source"
PREFIX="underworld-dungeon-blackwater-drowned-vaults-"
REVISION="drowned-vaults-dungeon-r1"
SPECS={
"drowned-sinkhole":((34,38,22),"Mixed",2.5),"tide-gallery":((36,48,20),"Mixed",1.8),
"dry-ledger":((30,42,18),"Dry",0.0),"collapsed-dock":((44,46,24),"Mixed",2.8),
"siphon-hall":((34,50,20),"Flooded",5.5),"bell-chamber":((38,40,26),"AirPocket",3.5),
"split-cistern":((42,42,24),"Mixed",3.0),"drowned-shaft":((28,32,34),"VerticalWater",12.0),
"pearl-vault":((34,38,20),"Flooded",4.5),"high-water-archive":((40,44,28),"AirPocket",2.5),
"lamprey-run":((30,46,18),"Flooded",4.0),"deep-hunter-lair":((46,48,26),"Flooded",7.0),
"sunken-quay":((44,52,20),"Mixed",2.0),"broken-causeway":((26,54,20),"Dry",0.0),
"undertow-sluice":((30,38,28),"VerticalWater",8.0),"abyssal-sanctum":((50,52,30),"Mixed",4.0),
"passage":((14,16,14),"Adaptive",3.0),
}
requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
for model_id in requested:
 suffix=model_id.removeprefix(PREFIX)
 if suffix not in SPECS:raise RuntimeError("Unknown Drowned Vault source "+model_id)
 target,mode,depth=SPECS[suffix];path=SOURCE/(model_id+".blend")
 if not path.is_file():raise RuntimeError("Missing Drowned Vault source "+str(path))
 bpy.ops.wm.open_mainfile(filepath=str(path));scene=bpy.context.scene
 if scene.get("model_id")!=model_id:raise RuntimeError(model_id+": model_id drift")
 if scene.get("underworld_authoring")!=REVISION:raise RuntimeError(model_id+": wrong authoring revision")
 if scene.get("drowned_vault_route_mode")!=mode:raise RuntimeError(model_id+": route-mode metadata drift")
 if abs(float(scene.get("drowned_vault_water_depth_m",-999))-depth)>.001:raise RuntimeError(model_id+": water depth metadata drift")
 if abs(float(scene.get("drowned_vault_waterline_z",-999)))>.001:raise RuntimeError(model_id+": waterline must stay at Z=0")
 if scene.get("drowned_vault_native_water")!="runtime-WaterVolume-only":raise RuntimeError(model_id+": native-water contract missing")
 meshes=[o for o in scene.objects if o.type=="MESH"]
 if len(meshes)<24:raise RuntimeError(f"{model_id}: insufficient detail parts {len(meshes)}")
 colliders=[o for o in meshes if bool(o.get("game_collision"))]
 if len(colliders)<8:raise RuntimeError(f"{model_id}: insufficient collidable structure {len(colliders)}")
 tris=0;xs=[];ys=[];zs=[];keys=set()
 for o in meshes:
  if "water-surface" in o.name.lower() or "waterplane" in o.name.lower():
   raise RuntimeError(model_id+": authored decorative water mesh is forbidden")
  o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
  for v in o.data.vertices:
   p=o.matrix_world@v.co;xs.append(p.x);ys.append(p.y);zs.append(p.z)
  for m in o.data.materials:
   if m:
    key=m.get("magenheim_material_source_key")
    if key:keys.add(str(key))
 if tris<1500:raise RuntimeError(f"{model_id}: fidelity regression {tris} triangles")
 extent=(max(xs)-min(xs),max(ys)-min(ys),max(zs)-min(zs))
 if extent[0]<target[0]*.55 or extent[1]<target[1]*.55:raise RuntimeError(f"{model_id}: horizontal envelope too small {extent}")
 if extent[0]>target[0]*1.45 or extent[1]>target[1]*1.45:raise RuntimeError(f"{model_id}: horizontal envelope exceeds budget {extent}")
 if not {"understone","flowstone"}.issubset(keys):raise RuntimeError(model_id+": missing Blackwater structural PBR language")
 if not ({"blackwater-pearl","pale-fibre","pearl-metal"} & keys):raise RuntimeError(model_id+": missing Blackwater accent identity")
 names={o.name for o in meshes}
 if mode=="Dry":
  if "dry-floor" not in names or "flood-floor" in names:raise RuntimeError(model_id+": dry-floor contract broken")
 else:
  if "flood-floor" not in names:raise RuntimeError(model_id+": wet room lacks structural flooded floor")
  floor=next(o for o in meshes if o.name=="flood-floor")
  floor_top=max((floor.matrix_world@v.co).z for v in floor.data.vertices)
  if floor_top>-(depth*.70):raise RuntimeError(f"{model_id}: flooded floor {floor_top:.2f} does not preserve depth {depth:.2f}")
 print("VERIFIED",model_id,mode,"parts",len(meshes),"colliders",len(colliders),"tris",tris,"extent",tuple(round(x,1) for x in extent),flush=True)
