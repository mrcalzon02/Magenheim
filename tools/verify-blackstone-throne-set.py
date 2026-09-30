#!/usr/bin/env python3
"""Admission gate for the authored Blackstone Throne module family and assembled Dark Throne."""
import json, math
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/"assets"/"models"
SOURCE=MODELS/"source"; GLB=MODELS/"glb"; RUNTIME=MODELS/"runtime"
IDS=(
 "blackstone-throne","blackstone-banner","blackstone-attendant-seat","blackstone-brazier",
 "blackstone-stair","blackstone-dais","blackstone-parapet","blackstone-bridge",
 "blackstone-arch","blackstone-cliff-edge","blackstone-floor-tile","blackstone-spire",
 "blackstone-pillar","dark-throne",
)
for model_id in IDS:
    for path in (SOURCE/(model_id+".blend"),GLB/(model_id+".glb"),RUNTIME/(model_id+".model.json")):
        if not path.is_file(): raise SystemExit(f"{model_id}: missing {path.relative_to(ROOT)}")
    doc=json.loads((RUNTIME/(model_id+".model.json")).read_text())
    if not doc.get("parts"): raise SystemExit(f"{model_id}: no runtime parts")
    names=[p["name"] for p in doc["parts"]]
    if len(names)!=len(set(names)): raise SystemExit(f"{model_id}: duplicate part names")
    for part in doc["parts"]:
        mat=part.get("material") or {}
        tex=mat.get("texture")
        if not tex or not (MODELS/"textures"/tex).is_file():
            raise SystemExit(f"{model_id}/{part['name']}: owned texture missing")
        if not all(math.isfinite(float(v)) for row in part["vertices"] for v in row):
            raise SystemExit(f"{model_id}/{part['name']}: non-finite geometry")

site=json.loads((RUNTIME/"dark-throne.model.json").read_text())
names=[p["name"] for p in site["parts"]]
requirements={
 "throne":sum(n.startswith("Throne_") for n in names)>=20,
 "dais":sum(n.startswith("Dais_") for n in names)>=5,
 "runes":sum(n.startswith("Rune_") for n in names)==8,
 "braziers":sum(n.startswith("Brazier_") and "_Flame_" not in n and "_Lip" not in n for n in names)>=8,
 "banners":sum(n.startswith("Banner_") for n in names)>=20,
 "seats":sum(n.startswith("Seat_") for n in names)>=12,
 "terrain":sum(n.startswith(("Floor_","Terrace_","Cliff_","Bridge_","Parapet_","Arch_","Pillar_","Spire_")) for n in names)>=90,
}
bad=[k for k,v in requirements.items() if not v]
if bad: raise SystemExit("dark-throne: missing assembled role coverage: "+", ".join(bad))
if len(site["parts"])<170: raise SystemExit(f"dark-throne: expected >=170 authored parts, found {len(site['parts'])}")
if len(site.get("lights") or [])<16: raise SystemExit("dark-throne: expected brazier + rune runtime lights")

# Preserve the encounter's fixed 52x60 m authority footprint.
verts=[v for p in site["parts"] for v in p["vertices"]]
xs=[v[0] for v in verts]; zs=[v[2] for v in verts]
if min(xs)<-27 or max(xs)>27 or min(zs)<-31 or max(zs)>31:
    raise SystemExit(f"dark-throne: geometry escaped encounter footprint x={min(xs):.2f}..{max(xs):.2f} z={min(zs):.2f}..{max(zs):.2f}")

art=ROOT/"assets"/"textures"/"underworld"/"blackstone"
required_art={
 "blackstone-basalt-albedo.png","blackstone-voidstone-albedo.png","blackstone-bronze-albedo.png",
 "blackstone-banner-sun-albedo.png","blackstone-sun-sigil.png","blackstone-ember-albedo.png",
}
missing=sorted(name for name in required_art if not (art/name).is_file())
if missing: raise SystemExit("Blackstone 2D source artwork missing: "+", ".join(missing))

print("VERIFIED Blackstone Throne set:",len(IDS),"model identities,",len(site["parts"]),"assembled site parts,",
      len(site.get("lights") or []),"runtime lights, modular throne/banner/seat/brazier/terrain coverage")
