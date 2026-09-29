#!/usr/bin/env python3
"""Non-destructive Blender gate for Carrion Catacombs room sources."""
import sys
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets/models/source"
PREFIX="underworld-dungeon-great-decay-carrion-catacombs-"
REVISION="carrion-catacombs-dungeon-r1"
SPECS={
"ossuary-gate":((36,38,22),"PreservedRuin",.18,.04),
"processional-hall":((40,54,24),"PreservedRuin",.24,.06),
"sunken-reliquary":((42,46,24),"TaintedRuin",.34,.10),
"root-split-crossing":((42,42,24),"RootIngress",.48,.28),
"bone-chute":((30,36,42),"RootIngress",.52,.30),
"miasma-nave":((40,48,26),"BlackBloom",.62,.72),
"carrion-sluice":((32,52,20),"BlackBloom",.68,.78),
"amber-mortuary":((40,40,22),"TaintedRuin",.38,.12),
"bone-gravel-crypt":((38,42,20),"RootIngress",.46,.24),
"censer-court":((42,42,24),"Sanctuary",.08,.02),
"rotling-warrens":((36,40,18),"RootIngress",.50,.26),
"spore-husk-cloister":((40,44,22),"TaintedRuin",.42,.16),
"graft-warden-hall":((46,48,28),"RootIngress",.56,.34),
"vanishing-archive":((44,50,26),"PreservedRuin",.26,.08),
"defiant-work-chapel":((42,44,24),"Sanctuary",.10,.03),
"corpse-orchard-antechamber":((52,56,32),"BlackBloom",.72,.88),
"passage":((14,16,14),"Adaptive",.36,.18),
}
requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [PREFIX+k for k in SPECS]
for mid in requested:
    suffix=mid.removeprefix(PREFIX)
    if suffix not in SPECS:raise RuntimeError("Unknown Carrion source "+mid)
    target,route,contamination,bloom=SPECS[suffix]
    path=SOURCE/(mid+".blend")
    if not path.is_file():raise RuntimeError("Missing Carrion source "+str(path))
    bpy.ops.wm.open_mainfile(filepath=str(path))
    scene=bpy.context.scene
    if scene.get("model_id")!=mid:raise RuntimeError(mid+": model id drift")
    if scene.get("underworld_authoring")!=REVISION:raise RuntimeError(mid+": revision drift")
    if scene.get("carrion_route")!=route:raise RuntimeError(mid+": route drift")
    if abs(float(scene.get("carrion_contamination01",-9))-contamination)>.001:
        raise RuntimeError(mid+": contamination drift")
    if abs(float(scene.get("carrion_black_bloom_intensity01",-9))-bloom)>.001:
        raise RuntimeError(mid+": Black Bloom drift")
    if scene.get("carrion_atmosphere_geometry")!="none-runtime-atmosphere-authority":
        raise RuntimeError(mid+": fake atmosphere geometry contract drift")
    meshes=[o for o in scene.objects if o.type=="MESH"]
    if len(meshes)<30:raise RuntimeError(f"{mid}: insufficient detail {len(meshes)}")
    tris=0;xs=[];ys=[];keys=set();names=set()
    for obj in meshes:
        obj.data.calc_loop_triangles();tris+=len(obj.data.loop_triangles);names.add(obj.name)
        for vertex in obj.data.vertices:
            point=obj.matrix_world@vertex.co;xs.append(point.x);ys.append(point.y)
        for material in obj.data.materials:
            if material and material.get("magenheim_material_source_key"):
                keys.add(str(material.get("magenheim_material_source_key")))
    if tris<1700:raise RuntimeError(f"{mid}: insufficient triangles {tris}")
    extent=(max(xs)-min(xs),max(ys)-min(ys))
    if extent[0]<target[0]*.54 or extent[1]<target[1]*.54:
        raise RuntimeError(f"{mid}: envelope too small {extent}")
    if extent[0]>target[0]*1.50 or extent[1]>target[1]*1.50:
        raise RuntimeError(f"{mid}: envelope too large {extent}")
    if not {"understone","bone"}.issubset(keys):
        raise RuntimeError(mid+": missing ancient catacomb structural language")
    if not ({"rotwood","carrion-amber"}&keys):
        raise RuntimeError(mid+": missing Great Decay occupation/accent language")
    if route in ("RootIngress","BlackBloom") and not any(n.startswith("rotroot-rib-") for n in names):
        raise RuntimeError(mid+": organic occupation geometry missing")
    if route=="BlackBloom" and "decay-spore" not in keys:
        raise RuntimeError(mid+": Black Bloom route lost Decay Spore material language")
    if route in ("TaintedRuin","RootIngress","BlackBloom") and not any(n.startswith("stable-ledge-") for n in names):
        raise RuntimeError(mid+": contaminated route lost stable traversable ledge")
    print("VERIFIED",mid,route,"parts",len(meshes),"tris",tris,flush=True)
