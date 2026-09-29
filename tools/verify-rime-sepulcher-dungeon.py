#!/usr/bin/env python3
"""Non-destructive Blender gate for Rime Sepulcher room sources."""
import sys
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/"assets/models/source"
PREFIX="underworld-dungeon-frozen-rime-sepulcher-";REVISION="rime-sepulcher-dungeon-r1"
SPECS={
"rime-mouth":((34,38,22),"ClearGallery",.24,.08),"long-glass-gallery":((38,54,24),"ClearGallery",.30,.10),
"burial-colonnade":((42,48,24),"ClearGallery",.34,.12),"silent-crossing":((42,42,22),"FrostField",.44,.20),
"icewell-shaft":((30,34,40),"IceShear",.62,.26),"whiteout-narthex":((34,44,20),"WhiteoutChoke",.58,.74),
"needle-pass":((28,52,22),"IceShear",.68,.34),"rimesilver-ossuary":((38,40,20),"FrostField",.48,.18),
"clear-ice-lens-vault":((36,42,22),"ClearGallery",.32,.10),"still-air-crypt":((38,40,20),"Shelter",.06,.02),
"frost-tick-niche":((34,38,18),"FrostField",.46,.20),"iceblind-hunt":((42,46,22),"WhiteoutChoke",.56,.66),
"cryolith-guard":((46,48,28),"IceShear",.64,.28),"frozen-archive":((44,48,26),"ClearGallery",.28,.08),
"shelter-chapel":((40,42,22),"Shelter",.08,.03),"white-silence-antechamber":((50,54,30),"WhiteoutChoke",.62,.82),
"passage":((14,16,14),"Adaptive",.36,.20)}
requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
for mid in requested:
 suffix=mid.removeprefix(PREFIX)
 if suffix not in SPECS:raise RuntimeError("Unknown Rime source "+mid)
 target,route,cold,whiteout=SPECS[suffix];path=SOURCE/(mid+".blend")
 if not path.is_file():raise RuntimeError("Missing Rime source "+str(path))
 bpy.ops.wm.open_mainfile(filepath=str(path));sc=bpy.context.scene
 if sc.get("model_id")!=mid:raise RuntimeError(mid+": model id drift")
 if sc.get("underworld_authoring")!=REVISION:raise RuntimeError(mid+": revision drift")
 if sc.get("rime_route")!=route:raise RuntimeError(mid+": route drift")
 if abs(float(sc.get("rime_cold_exposure01",-9))-cold)>.001:raise RuntimeError(mid+": cold exposure drift")
 if abs(float(sc.get("rime_whiteout_intensity01",-9))-whiteout)>.001:raise RuntimeError(mid+": whiteout intensity drift")
 if sc.get("rime_weather_geometry")!="none-runtime-atmosphere-authority":raise RuntimeError(mid+": fake weather geometry contract drift")
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
 if not {"understone","clear-ice"}.issubset(keys):raise RuntimeError(mid+": missing Frozen structural language")
 if not ({"rimesilver","rimewood"}&keys):raise RuntimeError(mid+": missing Frozen crafted accent language")
 names={o.name for o in meshes}
 if route in ("FrostField","WhiteoutChoke","IceShear") and not any(n.startswith("shelter-ledge-") for n in names):
  raise RuntimeError(mid+": pressure route lost its stable bypass ledge")
 if route=="IceShear" and not any(n.startswith("shear-fin-") for n in names):raise RuntimeError(mid+": ice-shear geometry missing")
 print("VERIFIED",mid,route,"parts",len(meshes),"tris",tris,flush=True)
