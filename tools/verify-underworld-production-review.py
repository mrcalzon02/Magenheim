#!/usr/bin/env python3
"""Machine gate for completeness/readability of production review output."""
import json
from pathlib import Path
from statistics import pstdev
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/"dist"/"underworld-production-review";REND=BASE/"renders";SHEETS=BASE/"sheets"
index=json.loads((BASE/"index.json").read_text());fail=[]
for e in index:
    for cond in ("neutral","context"):
        p=REND/f"{e['id']}--{cond}.png"
        if not p.is_file():fail.append("missing "+p.name);continue
        im=Image.open(p).convert("L")
        if im.size!=(320,320):fail.append(p.name+" wrong dimensions");continue
        vals=list(im.resize((64,64)).getdata())
        if max(vals)-min(vals)<22 or pstdev(vals)<5:fail.append(p.name+" visually flat/blank")
groups={e["group"] for e in index}
for g in groups:
    if not (SHEETS/f"{g}.png").is_file():fail.append("missing contact sheet "+g)
if len(index)<60:fail.append("review index lost expected production families")
if fail:
    print("FAIL production review gate")
    for x in fail[:30]:print(" - "+x)
    raise SystemExit(1)
print("VERIFIED production review:",len(index),"models,",len(index)*2,"renders,",len(groups),"contact sheets")
