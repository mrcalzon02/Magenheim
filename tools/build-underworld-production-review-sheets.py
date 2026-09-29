#!/usr/bin/env python3
"""Build labelled family contact sheets from production review renders."""
import json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/"dist"/"underworld-production-review";REND=BASE/"renders";SHEETS=BASE/"sheets"
SHEETS.mkdir(parents=True,exist_ok=True)
index=json.loads((BASE/"index.json").read_text());groups={}
for e in index:groups.setdefault(e["group"],[]).append(e["id"])
font=ImageFont.load_default();thumb=220;label_h=34;pair_w=thumb*2;cols=3
for group,ids in sorted(groups.items()):
    rows=math.ceil(len(ids)/cols)
    sheet=Image.new("RGB",(cols*pair_w,rows*(thumb+label_h)),(24,27,32))
    draw=ImageDraw.Draw(sheet)
    for n,mid in enumerate(ids):
        x=(n%cols)*pair_w;y=(n//cols)*(thumb+label_h)
        for k,cond in enumerate(("neutral","context")):
            im=Image.open(REND/f"{mid}--{cond}.png").convert("RGB")
            im.thumbnail((thumb,thumb),Image.Resampling.LANCZOS)
            sheet.paste(im,(x+k*thumb+(thumb-im.width)//2,y+(thumb-im.height)//2))
        label=mid if len(mid)<=58 else mid[:55]+"..."
        draw.text((x+6,y+thumb+8),label,fill=(225,232,240),font=font)
    out=SHEETS/f"{group}.png";sheet.save(out);print("BUILT",out.name,flush=True)
print("BUILT",len(groups),"production review contact sheets",flush=True)

articulation=BASE/"armour-articulation"
articulation_ids=[e["id"] for e in index if e["id"].startswith("underworld-armor-")]
if len(articulation_ids)!=24:
    raise RuntimeError(f"Expected 24 armour entries for articulation sheet, found {len(articulation_ids)}")
cols=4;rows=math.ceil(len(articulation_ids)/cols)
sheet=Image.new("RGB",(cols*thumb,rows*(thumb+label_h)),(24,27,32));draw=ImageDraw.Draw(sheet)
for n,mid in enumerate(sorted(articulation_ids)):
    path=articulation/(mid+".png")
    if not path.is_file(): raise RuntimeError("Missing articulation review "+str(path))
    im=Image.open(path).convert("RGB");im.thumbnail((thumb,thumb),Image.Resampling.LANCZOS)
    x=(n%cols)*thumb;y=(n//cols)*(thumb+label_h)
    sheet.paste(im,(x+(thumb-im.width)//2,y+(thumb-im.height)//2))
    label=mid.replace("underworld-armor-","")
    draw.text((x+6,y+thumb+8),label,fill=(225,232,240),font=font)
out=SHEETS/"armour-articulation.png";sheet.save(out);print("BUILT",out.name,flush=True)
