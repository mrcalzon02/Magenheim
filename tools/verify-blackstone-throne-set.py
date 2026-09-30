#!/usr/bin/env python3
"""Surgical admission gate for Blackstone Throne source/GLB/runtime assets."""
import json,math,struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/"assets"/"models"
SOURCE=MODELS/"source"; GLB=MODELS/"glb"; RUNTIME=MODELS/"runtime"; MTEX=MODELS/"textures"
IDS=(
 "blackstone-throne","blackstone-banner","blackstone-attendant-seat","blackstone-brazier",
 "blackstone-stair","blackstone-dais","blackstone-parapet","blackstone-bridge",
 "blackstone-arch","blackstone-cliff-edge","blackstone-floor-tile","blackstone-spire",
 "blackstone-pillar","dark-throne",
)

def cross(a,b,c):
    u=[b[i]-a[i] for i in range(3)]; v=[c[i]-a[i] for i in range(3)]
    return [u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]

for model_id in IDS:
    src=SOURCE/(model_id+".blend"); glb=GLB/(model_id+".glb"); runtime=RUNTIME/(model_id+".model.json")
    for path in (src,glb,runtime):
        if not path.is_file(): raise SystemExit(f"{model_id}: missing {path.relative_to(ROOT)}")
    head=src.read_bytes()[:7]
    if not (head.startswith(b"BLENDER") or head.startswith(bytes.fromhex("28b52ffd"))):
        raise SystemExit(f"{model_id}: invalid Blender source header")
    doc=json.loads(runtime.read_text())
    if not doc.get("parts"): raise SystemExit(f"{model_id}: no runtime parts")
    names=[p["name"] for p in doc["parts"]]
    if len(names)!=len(set(names)): raise SystemExit(f"{model_id}: duplicate part names")
    for part in doc["parts"]:
        vertices=part.get("vertices") or []; normals=part.get("normals") or []; uv=part.get("uv") or []; indices=part.get("triangles") or []
        if not vertices or not indices or len(indices)%3 or len(vertices)!=len(normals) or len(vertices)!=len(uv):
            raise SystemExit(f"{model_id}/{part['name']}: incomplete topology")
        if any(i<0 or i>=len(vertices) for i in indices): raise SystemExit(f"{model_id}/{part['name']}: bad index")
        if not all(math.isfinite(float(v)) for row in (vertices,uv,normals) for vec in row for v in vec):
            raise SystemExit(f"{model_id}/{part['name']}: non-finite surface data")
        if any(abs(sum(float(x)*float(x) for x in n)-1)>0.02 for n in normals):
            raise SystemExit(f"{model_id}/{part['name']}: non-unit normal")
        for i in range(0,len(indices),3):
            face=indices[i:i+3]; a,b,c=[vertices[n] for n in face]; g=cross(a,b,c)
            gm=math.sqrt(sum(x*x for x in g))
            if gm<=1e-11: raise SystemExit(f"{model_id}/{part['name']}: degenerate triangle {i//3}")
            g=[x/gm for x in g]; authored=[sum(normals[n][axis] for n in face) for axis in range(3)]
            am=math.sqrt(sum(x*x for x in authored))
            if am>1e-8 and sum(g[j]*(authored[j]/am) for j in range(3)) < -0.05:
                raise SystemExit(f"{model_id}/{part['name']}: inverted winding at triangle {i//3}")
        mat=part.get("material") or {}; tex=mat.get("texture")
        if not tex or Path(tex).name!=tex or not (MTEX/tex).is_file():
            raise SystemExit(f"{model_id}/{part['name']}: owned texture missing")
        png=(MTEX/tex).read_bytes()
        if not png.startswith(b"\x89PNG\r\n\x1a\n") or len(png)<24:
            raise SystemExit(f"{model_id}/{part['name']}: invalid PNG {tex}")
        width,height=struct.unpack(">II",png[16:24])
        if width<256 or height<256: raise SystemExit(f"{model_id}/{part['name']}: texture below 256px floor")
    data=glb.read_bytes()
    if len(data)<20: raise SystemExit(f"{model_id}: truncated GLB")
    magic,version,length=struct.unpack_from("<III",data)
    if magic!=0x46546C67 or version!=2 or length!=len(data): raise SystemExit(f"{model_id}: invalid GLB container")

site=json.loads((RUNTIME/"dark-throne.model.json").read_text())
names=[p["name"] for p in site["parts"]]
requirements={
 "throne":sum(n.startswith("Throne_") for n in names)>=20,
 "dais":sum(n.startswith("Dais_") for n in names)>=5,
 "runes":sum(n.startswith("Rune_") for n in names)==8,
 "braziers":len({n.split("_")[1] for n in names if n.startswith("Brazier_") and len(n.split("_"))>1})>=8,
 "banners":sum(n.startswith("Banner_") for n in names)>=20,
 "seats":sum(n.startswith("Seat_") for n in names)>=12,
 "terrain":sum(n.startswith(("Floor_","Terrace_","Cliff_","Bridge_","Parapet_","Arch_","Pillar_","Spire_")) for n in names)>=90,
}
bad=[k for k,v in requirements.items() if not v]
if bad: raise SystemExit("dark-throne: missing assembled role coverage: "+", ".join(bad))
if len(site["parts"])<170: raise SystemExit(f"dark-throne: expected >=170 authored parts, found {len(site['parts'])}")
if len(site.get("lights") or [])<16: raise SystemExit("dark-throne: expected brazier + rune runtime lights")
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

print("VERIFIED Blackstone Throne set:",len(IDS),"real Blender/GLB/runtime triplets,",len(site["parts"]),
      "assembled parts, topology/winding/UV/texture checks, modular throne/banner/seat/brazier/terrain coverage")
