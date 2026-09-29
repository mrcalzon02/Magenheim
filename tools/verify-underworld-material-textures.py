#!/usr/bin/env python3
"""Quantitative gate for shared Underworld source material families."""
from pathlib import Path
from statistics import mean,pstdev
from PIL import Image
import ast

ROOT=Path(__file__).resolve().parents[1]
def flat(im):
    reader=getattr(im,"get_flattened_data",None)
    return list(reader() if reader else im.getdata())

def edge_continuity(image):
    """Return seam/internal neighbor ratio; ~1 means wrapping is no harsher than normal texel detail."""
    im=image.convert("RGB")
    w,h=im.size
    px=im.load()
    def delta(a,b): return sum(abs(a[i]-b[i]) for i in range(3))/3
    vertical=sum(delta(px[0,y],px[w-1,y]) for y in range(h))/h
    horizontal=sum(delta(px[x,0],px[x,h-1]) for x in range(w))/w
    internal_v=sum(delta(px[x,y],px[x-1,y]) for x in range(1,w,17) for y in range(0,h,17))
    internal_v/=max(1,len(range(1,w,17))*len(range(0,h,17)))
    internal_h=sum(delta(px[x,y],px[x,y-1]) for x in range(0,w,17) for y in range(1,h,17))
    internal_h/=max(1,len(range(0,w,17))*len(range(1,h,17)))
    return vertical/max(internal_v,0.5),horizontal/max(internal_h,0.5)
DIR=ROOT/"assets"/"material-source"/"underworld"
generator_tree=ast.parse((ROOT/"tools"/"generate-underworld-material-textures.py").read_text())
specs=None
for node in generator_tree.body:
    if isinstance(node,ast.Assign) and any(isinstance(target,ast.Name) and target.id=="SPECS" for target in node.targets):
        specs=ast.literal_eval(node.value)
        break
if not isinstance(specs,dict) or not specs:
    raise SystemExit("Unable to read Underworld material SPECS without executing the generator")
albedos=sorted(DIR.glob("*-albedo.png"))
actual_keys={path.name[:-11] for path in albedos}
expected_keys=set(specs)
if actual_keys!=expected_keys:
    raise SystemExit("Material family mismatch: expected "+repr(sorted(expected_keys))+", got "+repr(sorted(actual_keys)))
for alb in albedos:
    key=alb.name[:-11]
    maps={suffix:DIR/f"{key}-{suffix}.png" for suffix in ("albedo","roughness","normal","metallic-smoothness")}
    for suffix,path in maps.items():
        if not path.is_file(): raise SystemExit(f"{key}: missing {suffix}")
        im=Image.open(path)
        if im.size!=(512,512): raise SystemExit(f"{path.name}: expected 512x512, got {im.size}")
        wrap_x,wrap_y=edge_continuity(im)
        if wrap_x>2.8 or wrap_y>2.8:
            raise SystemExit(f"{path.name}: repeat seam is harsher than ordinary texel detail (x={wrap_x:.2f}x y={wrap_y:.2f}x)")
    a=Image.open(maps["albedo"]).convert("L")
    p=a.resize((64,64),Image.Resampling.LANCZOS)
    vals=flat(p)
    luma=mean(vals)
    relative_range=(max(vals)-min(vals))/max(luma,24)
    if pstdev(vals)<1.0 or relative_range<0.05:
        raise SystemExit(f"{key}: albedo collapses at gameplay scale (relative range {relative_range:.1%}, sigma {pstdev(vals):.2f})")
    if min(vals)<3 or max(vals)>252: raise SystemExit(f"{key}: albedo clips excessively")
    r=Image.open(maps["roughness"]).convert("L").resize((64,64),Image.Resampling.LANCZOS)
    rv=flat(r)
    if pstdev(rv)<2.5 or not 45<=mean(rv)<=235: raise SystemExit(f"{key}: roughness lacks readable material variation")
    n=Image.open(maps["normal"]).convert("RGB").resize((64,64),Image.Resampling.LANCZOS)
    nv=flat(n)
    if mean(v[2] for v in nv)<150 or mean(abs(v[0]-128)+abs(v[1]-128) for v in nv)<1.8:
        raise SystemExit(f"{key}: normal relief is invalid or disappears at gameplay scale")
    mg=Image.open(maps["metallic-smoothness"]).convert("RGBA").resize((64,64),Image.Resampling.LANCZOS)
    smooth=[p[3] for p in flat(mg)]
    if pstdev(smooth)<2.5: raise SystemExit(f"{key}: metallic/smoothness map loses gloss variation at gameplay scale")
expected_emission={key for key,spec in specs.items() if spec[5] is not None}
actual_emission={p.name[:-13] for p in DIR.glob("*-emission.png")}
if actual_emission!=expected_emission:
    raise SystemExit("Emission family mismatch: expected "+repr(sorted(expected_emission))+", got "+repr(sorted(actual_emission)))
for em in DIR.glob("*-emission.png"):
    im=Image.open(em).convert("L").resize((64,64),Image.Resampling.LANCZOS);v=flat(im)
    coverage=sum(x>10 for x in v)/len(v)
    if not .005<=coverage<=.45: raise SystemExit(f"{em.name}: emission coverage {coverage:.1%} is not localized")
print(f"VERIFIED Underworld material source library: {len(albedos)} 512px albedo/roughness/normal families with 64px readability and {len(expected_emission)} localized-emission families.")
