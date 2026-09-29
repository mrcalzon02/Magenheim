#!/usr/bin/env python3
"""Non-destructive Blender gate for Cinderworks room sources."""
import sys
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/"assets/models/source"
PREFIX="underworld-dungeon-sulfur-cinderworks-";REVISION="cinderworks-dungeon-r1"
SPECS={
"cinder-gate":((34,38,22),"Warm",.22),"slag-nave":((38,48,22),"Warm",.30),"furnace-gallery":((36,50,24),"Hot",.52),
"bellows-junction":((42,42,24),"Warm",.34),"chimney-shaft":((30,34,38),"VentCycle",.66),"vent-choir":((40,44,24),"VentCycle",.78),
"slag-runoff":((32,52,20),"SlagChannel",.82),"emberiron-foundry":((42,44,22),"Hot",.48),"sulfur-kiln":((34,40,20),"Warm",.36),
"quench-vault":((38,40,22),"Safe",.08),"ashmite-conveyor":((34,46,18),"Warm",.32),"cinder-hound-yard":((42,46,20),"Hot",.56),
"furnace-golem-crucible":((46,48,28),"SlagChannel",.74),"broken-smeltery":((44,50,26),"Warm",.28),
"pressure-lock":((28,38,18),"Safe",.10),"furnace-heart-antechamber":((50,52,30),"Hot",.64),"passage":((14,16,14),"Adaptive",.42)}
requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
for mid in requested:
 suffix=mid.removeprefix(PREFIX)
 if suffix not in SPECS:raise RuntimeError("Unknown Cinderworks source "+mid)
 target,route,pressure=SPECS[suffix];path=SOURCE/(mid+".blend")
 if not path.is_file():raise RuntimeError("Missing Cinderworks source "+str(path))
 bpy.ops.wm.open_mainfile(filepath=str(path));sc=bpy.context.scene
 if sc.get("model_id")!=mid:raise RuntimeError(mid+": model id drift")
 if sc.get("underworld_authoring")!=REVISION:raise RuntimeError(mid+": authoring revision drift")
 if sc.get("cinderworks_heat_route")!=route:raise RuntimeError(mid+": heat route drift")
 if abs(float(sc.get("cinderworks_thermal_pressure01",-9))-pressure)>.001:raise RuntimeError(mid+": thermal pressure drift")
 if sc.get("cinderworks_damage_geometry")!="none-runtime-thermal-authority":raise RuntimeError(mid+": decorative damage geometry contract drift")
 meshes=[o for o in sc.objects if o.type=="MESH"]
 if len(meshes)<28:raise RuntimeError(f"{mid}: insufficient detail {len(meshes)}")
 tris=0;xs=[];ys=[];keys=set()
 for o in meshes:
  o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
  for v in o.data.vertices:
   p=o.matrix_world@v.co;xs.append(p.x);ys.append(p.y)
  for m in o.data.materials:
   if m and m.get("magenheim_material_source_key"):keys.add(str(m.get("magenheim_material_source_key")))
 if tris<1700:raise RuntimeError(f"{mid}: insufficient triangles {tris}")
 extent=(max(xs)-min(xs),max(ys)-min(ys))
 if extent[0]<target[0]*.54 or extent[1]<target[1]*.54:raise RuntimeError(f"{mid}: envelope too small {extent}")
 if extent[0]>target[0]*1.48 or extent[1]>target[1]*1.48:raise RuntimeError(f"{mid}: envelope too large {extent}")
 if not {"understone","slagstone"}.issubset(keys):raise RuntimeError(mid+": missing structural material language")
 if not ({"emberiron","ember-heat","charred-root","sulfur-crust"} & keys):raise RuntimeError(mid+": missing Sulfur accent language")
 names={o.name for o in meshes}
 if route in ("Hot","VentCycle","SlagChannel") and not any(n.startswith("safe-ledge-") for n in names):
  raise RuntimeError(mid+": high-pressure room lost its non-boon bypass ledge")
 if route=="SlagChannel" and "slag-channel-bed" not in names:raise RuntimeError(mid+": slag route lost channel")
 print("VERIFIED",mid,route,"parts",len(meshes),"tris",tris,flush=True)
