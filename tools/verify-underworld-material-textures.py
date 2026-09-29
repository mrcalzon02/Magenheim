#!/usr/bin/env python3
"""Quantitative gate for shared Underworld source material families."""
from pathlib import Path
from statistics import mean,pstdev
from PIL import Image
import importlib.util,sys

ROOT=Path(__file__).resolve().parents[1]
def flat(im):
    reader=getattr(im,"get_flattened_data",None)
    return list(reader() if reader else im.getdata())
DIR=ROOT/"assets"/"material-source"/"underworld"
spec=importlib.util.spec_from_file_location("gen",ROOT/"tools"/"generate-underworld-material-textures.py")
# Do not import generator (it would regenerate); read keys from helper's mapping expectations via filenames.
albedos=sorted(DIR.glob("*-albedo.png"))
if len(albedos)!=23: raise SystemExit(f"Expected 23 Underworld material families, found {len(albedos)}")
for alb in albedos:
    key=alb.name[:-11]
    maps={suffix:DIR/f"{key}-{suffix}.png" for suffix in ("albedo","roughness","normal")}
    for suffix,path in maps.items():
        if not path.is_file(): raise SystemExit(f"{key}: missing {suffix}")
        im=Image.open(path)
        if im.size!=(512,512): raise SystemExit(f"{path.name}: expected 512x512, got {im.size}")
    a=Image.open(maps["albedo"]).convert("L")
    p=a.resize((64,64),Image.Resampling.LANCZOS)
    vals=flat(p)
    if max(vals)-min(vals)<18 or pstdev(vals)<4.0: raise SystemExit(f"{key}: albedo collapses at gameplay scale")
    if min(vals)<3 or max(vals)>252: raise SystemExit(f"{key}: albedo clips excessively")
    r=Image.open(maps["roughness"]).convert("L").resize((64,64),Image.Resampling.LANCZOS)
    rv=flat(r)
    if pstdev(rv)<2.5 or not 45<=mean(rv)<=235: raise SystemExit(f"{key}: roughness lacks readable material variation")
    n=Image.open(maps["normal"]).convert("RGB").resize((64,64),Image.Resampling.LANCZOS)
    nv=flat(n)
    if mean(v[2] for v in nv)<150 or mean(abs(v[0]-128)+abs(v[1]-128) for v in nv)<1.8:
        raise SystemExit(f"{key}: normal relief is invalid or disappears at gameplay scale")
expected_emission={"glowcap","blackwater-pearl","ember-heat","clear-ice","fracture-crystal","carrion-amber"}
actual_emission={p.name[:-13] for p in DIR.glob("*-emission.png")}
if actual_emission!=expected_emission:
    raise SystemExit("Emission family mismatch: expected "+repr(sorted(expected_emission))+", got "+repr(sorted(actual_emission)))
for em in DIR.glob("*-emission.png"):
    im=Image.open(em).convert("L").resize((64,64),Image.Resampling.LANCZOS);v=flat(im)
    coverage=sum(x>10 for x in v)/len(v)
    if not .005<=coverage<=.45: raise SystemExit(f"{em.name}: emission coverage {coverage:.1%} is not localized")
print(f"VERIFIED Underworld material source library: {len(albedos)} 512px albedo/roughness/normal families with 64px readability and localized emission.")
