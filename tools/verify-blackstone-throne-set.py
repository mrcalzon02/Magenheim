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
 "blackstone-pillar","blackstone-terrace","blackstone-wall-buttress",
 "blackstone-cathedral-wall","blackstone-gate","dark-throne",
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
        for slot in ("normalTexture","metallicGlossTexture"):
            owned=mat.get(slot)
            if not owned or Path(owned).name!=owned or not (MTEX/owned).is_file():
                raise SystemExit(f"{model_id}/{part['name']}: owned runtime PBR map missing for {slot}")
        if mat.get("name")=="blackstone.ember":
            owned=mat.get("emissionTexture")
            if not owned or Path(owned).name!=owned or not (MTEX/owned).is_file():
                raise SystemExit(f"{model_id}/{part['name']}: ember emission map missing")
    data=glb.read_bytes()
    if len(data)<20: raise SystemExit(f"{model_id}: truncated GLB")
    magic,version,length=struct.unpack_from("<III",data)
    if magic!=0x46546C67 or version!=2 or length!=len(data): raise SystemExit(f"{model_id}: invalid GLB container")

banner_doc=json.loads((RUNTIME/"blackstone-banner.model.json").read_text())
banner_names=[p["name"] for p in banner_doc["parts"]]
cloth=next((p for p in banner_doc["parts"] if p["name"]=="Banner_Cloth"),None)
if cloth is None: raise SystemExit("blackstone-banner: authored folded cloth missing")
if any("RaisedSigil" in n for n in banner_names): raise SystemExit("blackstone-banner: duplicate raised sigil returned")
if not {"Banner_Pole_L","Banner_Pole_R","Banner_Foot_L","Banner_Foot_R","Banner_Crossbar"}.issubset(set(banner_names)):
    raise SystemExit("blackstone-banner: twin-standard support hardware incomplete")
if len(cloth.get("vertices") or [])<100: raise SystemExit("blackstone-banner: cloth is still a rigid low-detail panel")
uv=cloth.get("uv") or []; us=[p[0] for p in uv]; vs=[p[1] for p in uv]
if not uv or min(us)<-1e-4 or max(us)>1.0001 or min(vs)<-1e-4 or max(vs)>1.0001:
    raise SystemExit("blackstone-banner: cloth UVs escape the single 0..1 heraldic field")
if max(us)-min(us)<.99 or max(vs)-min(vs)<.99: raise SystemExit("blackstone-banner: cloth does not consume the full heraldic texture")
zs=[p[2] for p in cloth["vertices"]]
if max(zs)-min(zs)<.12: raise SystemExit("blackstone-banner: cloth has no modeled fold depth")

throne_doc=json.loads((RUNTIME/"blackstone-throne.model.json").read_text()); throne_names=[p["name"] for p in throne_doc["parts"]]
for required in ("Throne_BackCore","Throne_BackInset","Throne_Apex","Throne_ArmCap_L","Throne_ArmCap_R"):
    if required not in throne_names: raise SystemExit("blackstone-throne: hero silhouette part missing: "+required)

brazier_doc=json.loads((RUNTIME/"blackstone-brazier.model.json").read_text()); brazier_names=[p["name"] for p in brazier_doc["parts"]]
for required in ("Brazier_Basin","Brazier_CoalBed","Brazier_Collar","Brazier_Flame_1"):
    if required not in brazier_names: raise SystemExit("blackstone-brazier: ceremonial vessel part missing: "+required)

site=json.loads((RUNTIME/"dark-throne.model.json").read_text())
names=[p["name"] for p in site["parts"]]
requirements={
 "throne":sum(n.startswith("Throne_") for n in names)>=20,
 "dais":sum(n.startswith("Dais_") for n in names)>=5,
 "runes":sum(n.startswith("Rune_") for n in names)==8,
 "braziers":len({n.split("_")[1] for n in names if n.startswith("Brazier_") and len(n.split("_"))>1})>=12,
 "banners":sum(n.startswith("Banner_") for n in names)>=20,
 "seats":sum(n.startswith("Seat_") for n in names)>=12,
 "terrain":sum(n.startswith(("LowerTerrace_","IntermediateTerrace_","UpperTerrace_","ThronePlatform_",
                            "SideLowerTerrace_","SideUpperTerrace_","Cliff_","Bridge_","GrandStair_",
                            "Arch_","Spire_","CathedralWall_","RearWall_","Buttress_","HighGalleryRail_","Gate_"))
               for n in names)>=180,
 "cathedral_walls":sum(n.startswith(("CathedralWall_","RearWall_")) for n in names)>=80,
 "stacked_stairs":sum(n.startswith("GrandStair_") for n in names)>=30,
}
bad=[k for k,v in requirements.items() if not v]
if bad: raise SystemExit("dark-throne: missing assembled role coverage: "+", ".join(bad))
if len(site["parts"])<360: raise SystemExit(f"dark-throne: expected >=360 authored parts, found {len(site['parts'])}")
if len(site.get("lights") or [])<16: raise SystemExit("dark-throne: expected brazier + rune runtime lights")
foundation=next((p for p in site["parts"] if p["name"]=="Arena_Foundation"),None)
if foundation is None: raise SystemExit("dark-throne: Arena_Foundation missing")
fx=[v[0] for v in foundation["vertices"]]; fz=[v[2] for v in foundation["vertices"]]
if min(fx)<-26.05 or max(fx)>26.05 or min(fz)<-30.05 or max(fz)>30.05:
    raise SystemExit(f"dark-throne: X/Z authority marker changed from 52x60m: x={min(fx):.2f}..{max(fx):.2f} z={min(fz):.2f}..{max(fz):.2f}")
if foundation.get("collider"):
    raise SystemExit("dark-throne: Arena_Foundation must remain a non-walkable deep authority marker, not a flat combat plate")
fy=[v[1] for v in foundation["vertices"]]
if max(fy)>-18.5:
    raise SystemExit(f"dark-throne: authority marker rose into the playable hall: y={min(fy):.2f}..{max(fy):.2f}")

def part_height(name):
    part=next((p for p in site["parts"] if p["name"]==name),None)
    if part is None: raise SystemExit("dark-throne: missing vertical level part "+name)
    values=[v[1] for v in part["vertices"]]
    return (min(values)+max(values))*.5

levels=[
 ("LowerTerrace_Deck",1.0,2.2),
 ("IntermediateTerrace_Deck",4.8,6.0),
 ("UpperTerrace_Deck",9.0,10.2),
 ("ThronePlatform_Deck",12.2,13.4),
]
previous=-1e9
for name,minimum,maximum in levels:
    y=part_height(name)
    if y<minimum or y>maximum: raise SystemExit(f"dark-throne: {name} height {y:.2f} outside intended tier {minimum:.2f}..{maximum:.2f}")
    if y<=previous+2.0: raise SystemExit("dark-throne: vertical tiers collapsed into a flat arena")
    previous=y

# Decorative cathedral architecture may exceed the leash in X/Z, but remains a bounded location.
verts=[v for p in site["parts"] for v in p["vertices"]]
xs=[v[0] for v in verts]; ys=[v[1] for v in verts]; zs=[v[2] for v in verts]
if min(xs)<-34.5 or max(xs)>34.5 or min(zs)<-34.0 or max(zs)>34.0:
    raise SystemExit(f"dark-throne: decorative structure escaped bounded site envelope x={min(xs):.2f}..{max(xs):.2f} z={min(zs):.2f}..{max(zs):.2f}")
if min(ys)>-19.0 or max(ys)<30.0 or max(ys)-min(ys)<49.0:
    raise SystemExit(f"dark-throne: reference-scale verticality missing y={min(ys):.2f}..{max(ys):.2f}")

art=ROOT/"assets"/"textures"/"underworld"/"blackstone"
required_art={
 "blackstone-basalt-albedo.png","blackstone-voidstone-albedo.png","blackstone-bronze-albedo.png",
 "blackstone-banner-sun-albedo.png","blackstone-sun-sigil.png","blackstone-ember-albedo.png","blackstone-flame-albedo.png",
}
missing=sorted(name for name in required_art if not (art/name).is_file())
if missing: raise SystemExit("Blackstone 2D source artwork missing: "+", ".join(missing))
pbr=ROOT/"assets"/"material-source"/"blackstone"
required_pbr={
 "blackstone-basalt-normal.png","blackstone-basalt-metallic-smoothness.png",
 "blackstone-voidstone-normal.png","blackstone-voidstone-metallic-smoothness.png",
 "blackstone-royal-bronze-normal.png","blackstone-royal-bronze-metallic-smoothness.png",
 "blackstone-banner-cloth-normal.png","blackstone-banner-cloth-metallic-smoothness.png",
 "blackstone-ember-normal.png","blackstone-ember-metallic-smoothness.png","blackstone-ember-emission.png",
 "blackstone-flame-normal.png","blackstone-flame-metallic-smoothness.png","blackstone-flame-emission.png",
}
missing=sorted(name for name in required_pbr if not (pbr/name).is_file())
if missing: raise SystemExit("Blackstone owned PBR source maps missing: "+", ".join(missing))

print("VERIFIED Blackstone Throne set:",len(IDS),"real Blender/GLB/runtime triplets,",len(site["parts"]),
      "assembled parts, four playable elevation tiers, cathedral wall/buttress envelope, abyss foundation, hero-prop usability gates, topology/winding/UV/albedo/PBR checks")
