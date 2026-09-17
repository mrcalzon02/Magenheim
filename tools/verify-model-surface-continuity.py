"""Reject retro-textured models whose maps are too coherent for fragmented UV islands.

The 0.0.52 live pass proved that texture presence and tonal range are insufficient quality
gates: smart-projected models can carry a perfectly valid image while sampling unrelated
large-scale features on neighbouring surface patches.  That reads as camouflage seams in game.

This gate works on exported runtime JSON so it checks what Unity receives.  Purpose-authored
families are exempt because their UV layouts were authored with their maps.  For other models,
a highly fragmented UV layout must use a texture whose local variation dominates broad-scale
variation.  The generic low-contrast grain repair satisfies that contract; reintroducing broad
coherent noise to packed islands fails before delivery.
"""
from __future__ import annotations

import json
import math
import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "assets/models/runtime"
TEXTURES = ROOT / "assets/models/textures"

# These families intentionally own coherent UV layouts and authored maps.
AUTHORED_PREFIXES = (
    "geode-sample", "earth-rough", "earth-simple", "earth-crystal", "earth-advanced",
    "earth-master", "deep-fracture-passage", "deep-fracture-traversal", "DF-",
)


def png_luma(path: Path):
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not PNG")
    pos = 8; width = height = ctype = None; raw = bytearray()
    while pos < len(data):
        n = struct.unpack(">I", data[pos:pos+4])[0]; typ = data[pos+4:pos+8]
        chunk = data[pos+8:pos+8+n]; pos += 12+n
        if typ == b"IHDR": width,height,depth,ctype,_,_,_ = struct.unpack(">IIBBBBB", chunk)
        elif typ == b"IDAT": raw.extend(chunk)
        elif typ == b"IEND": break
    if depth != 8 or ctype not in (0,2,6):
        raise ValueError("unsupported PNG format")
    channels = {0:1,2:3,6:4}[ctype]; stride = width*channels
    dec = zlib.decompress(bytes(raw)); rows=[]; prior=[0]*stride; p=0
    for _ in range(height):
        f=dec[p]; p+=1; scan=list(dec[p:p+stride]); p+=stride; recon=[]
        for i,x in enumerate(scan):
            a=recon[i-channels] if i>=channels else 0; b=prior[i]; c=prior[i-channels] if i>=channels else 0
            if f==1: x=(x+a)&255
            elif f==2: x=(x+b)&255
            elif f==3: x=(x+((a+b)//2))&255
            elif f==4:
                q=a+b-c; pa=abs(q-a); pb=abs(q-b); pc=abs(q-c); x=(x+(a if pa<=pb and pa<=pc else b if pb<=pc else c))&255
            elif f!=0: raise ValueError("unsupported PNG filter")
            recon.append(x)
        prior=recon
        rows.append([sum(recon[i:i+min(3,channels)])/min(3,channels) for i in range(0,stride,channels)])
    return rows


def broad_to_local_ratio(img):
    h=len(img); w=len(img[0]); local=[]; broad=[]
    for y in range(0,h,4):
        for x in range(0,w,4):
            if x+1<w: local.append(abs(img[y][x]-img[y][x+1]))
            if y+1<h: local.append(abs(img[y][x]-img[y+1][x]))
            if x+32<w: broad.append(abs(img[y][x]-img[y][x+32]))
            if y+32<h: broad.append(abs(img[y][x]-img[y+32][x]))
    l=sum(local)/max(1,len(local)); b=sum(broad)/max(1,len(broad))
    return b/max(0.25,l)


def uv_fragmentation(part):
    uvs=part.get("uv") or part.get("uvs") or []
    tris=part.get("triangles") or part.get("indices") or []
    if len(uvs)<6 or len(tris)<6: return 0.0
    # Exporters may flatten UV pairs; normalize both representations.
    pts=[tuple(uvs[i:i+2]) for i in range(0,len(uvs),2)] if isinstance(uvs[0],(int,float)) else [tuple(v[:2]) for v in uvs]
    edges=set(); boundary=0
    for i in range(0,len(tris)-2,3):
        tri=tris[i:i+3]
        for a,b in ((tri[0],tri[1]),(tri[1],tri[2]),(tri[2],tri[0])):
            if a>=len(pts) or b>=len(pts): continue
            pa,pb=pts[a],pts[b]; key=tuple(sorted(((round(pa[0],5),round(pa[1],5)),(round(pb[0],5),round(pb[1],5)))))
            if key in edges: edges.remove(key)
            else: edges.add(key)
    boundary=len(edges)
    return boundary/max(1,len(tris)//3)


def main():
    failures=[]; checked=0
    for model_path in sorted(RUNTIME.glob("*.model.json")):
        if model_path.stem.replace(".model","").startswith(AUTHORED_PREFIXES): continue
        payload=json.loads(model_path.read_text(encoding="utf-8"))
        parts=payload.get("parts",[])
        if not parts: continue
        frag=max((uv_fragmentation(p) for p in parts),default=0.0)
        if frag < 1.2: continue
        for part in parts:
            tex=part.get("texture") or part.get("albedo") or part.get("albedoTexture")
            if not tex: continue
            path=TEXTURES/Path(tex).name
            if not path.exists(): continue
            checked+=1
            try: ratio=broad_to_local_ratio(png_luma(path))
            except Exception as exc:
                failures.append(f"{model_path.name}: cannot inspect {path.name}: {exc}"); continue
            if ratio > 3.0:
                failures.append(f"{model_path.name}: fragmented UVs ({frag:.2f}) use broad-coherent {path.name} (broad/local {ratio:.2f})")
    if failures:
        print("MODEL SURFACE CONTINUITY: FAIL")
        for failure in failures: print(" - "+failure)
        return 1
    print(f"MODEL SURFACE CONTINUITY: PASS ({checked} fragmented-UV texture bindings inspected)")
    return 0

if __name__ == "__main__":
    sys.exit(main())
