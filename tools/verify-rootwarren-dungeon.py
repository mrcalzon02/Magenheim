#!/usr/bin/env python3
"""Non-destructive Blender gate for the authored Rootwarren room kit."""
import bpy
import math
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets/models/source"
PREFIX="underworld-dungeon-fungal-rootwarren-"
SPECS={
 "fracture-mouth":(28,32,18),"mycelial-gallery":(34,46,20),"glowcap-vault":(42,42,28),
 "spore-basin":(38,44,18),"root-bridge":(24,52,24),"sunken-nursery":(40,38,16),
 "tangle-junction":(38,38,22),"shelf-drop":(30,34,38),"amber-grotto":(32,36,20),
 "worldroot-hollow":(46,48,34),"crawler-nest":(34,36,16),"stalker-den":(36,42,20),
 "puffback-graze":(44,46,18),"buried-archway":(30,40,22),"root-squeeze":(22,38,14),
 "heartcap-sanctum":(48,50,30),"passage":(12,16,12),
}

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
unknown=[x for x in requested if x.removeprefix(PREFIX) not in SPECS]
if unknown: raise SystemExit("Unknown Rootwarren model(s): "+", ".join(unknown))

for model_id in requested:
 suffix=model_id.removeprefix(PREFIX); target=SPECS[suffix]
 path=SOURCE/(model_id+".blend")
 if not path.exists(): raise RuntimeError(f"Missing Rootwarren source: {path}")
 bpy.ops.wm.open_mainfile(filepath=str(path))
 scene=bpy.context.scene
 if scene.get("model_id")!=model_id: raise RuntimeError(f"{model_id}: scene model_id drifted")
 if scene.get("underworld_authoring")!="rootwarren-dungeon-r1":
  raise RuntimeError(f"{model_id}: wrong authoring revision")
 meshes=[o for o in scene.objects if o.type=="MESH"]
 if len(meshes)<4: raise RuntimeError(f"{model_id}: insufficient modeled parts ({len(meshes)})")
 colliders=[o for o in meshes if bool(o.get("game_collision"))]
 if len(colliders)<2: raise RuntimeError(f"{model_id}: needs at least two collidable structural parts")
 tris=0; xs=[]; ys=[]; zs=[]; surfaces=set()
 for obj in meshes:
  obj.data.calc_loop_triangles(); tris+=len(obj.data.loop_triangles)
  for v in obj.data.vertices:
   p=obj.matrix_world@v.co; xs.append(p.x);ys.append(p.y);zs.append(p.z)
  for mat in obj.data.materials:
   if mat and mat.get("surface"): surfaces.add(str(mat["surface"]))
 if tris<1200: raise RuntimeError(f"{model_id}: fidelity regression ({tris} triangles)")
 extent=(max(xs)-min(xs),max(ys)-min(ys),max(zs)-min(zs))
 if extent[0]<target[0]*.55 or extent[1]<target[1]*.55:
  raise RuntimeError(f"{model_id}: horizontal envelope too small {extent} for target {target}")
 if extent[0]>target[0]*1.35 or extent[1]>target[1]*1.35:
  raise RuntimeError(f"{model_id}: horizontal envelope exceeds room budget {extent} for target {target}")
 if extent[2]<target[2]*.28:
  raise RuntimeError(f"{model_id}: room silhouette too vertically flat {extent} for target {target}")
 required={"understone","worldroot"}
 if suffix=="passage": required={"understone","worldroot"}
 if not required.issubset(surfaces):
  raise RuntimeError(f"{model_id}: missing structural surfaces {required-surfaces}")
 if suffix!="passage" and not ({"cap-teal","cap-violet","gill","amber","mycelium"} & surfaces):
  raise RuntimeError(f"{model_id}: has no fungal/biological surface identity")
 print(f"VERIFIED {model_id} parts={len(meshes)} colliders={len(colliders)} "
       f"tris={tris} extent=({extent[0]:.1f},{extent[1]:.1f},{extent[2]:.1f}) surfaces={len(surfaces)}",
       flush=True)
