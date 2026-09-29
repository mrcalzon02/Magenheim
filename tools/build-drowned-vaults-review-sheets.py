#!/usr/bin/env python3
"""Build Drowned Vault context/top-down review sheets."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"drowned-vault-renders";SHEETS=BASE/"sheets";SHEETS.mkdir(parents=True,exist_ok=True)
PREFIX="underworld-dungeon-blackwater-drowned-vaults-"
SUFFIXES=["drowned-sinkhole","tide-gallery","dry-ledger","collapsed-dock","siphon-hall","bell-chamber","split-cistern","drowned-shaft","pearl-vault","high-water-archive","lamprey-run","deep-hunter-lair","sunken-quay","broken-causeway","undertow-sluice","abyssal-sanctum","passage"]
font=ImageFont.load_default();tw,th,label=320,240,34
for page,start in enumerate(range(0,len(SUFFIXES),6),1):
 batch=SUFFIXES[start:start+6];sheet=Image.new("RGB",(tw*2,len(batch)*(th+label)),(12,22,30));draw=ImageDraw.Draw(sheet)
 for row,suffix in enumerate(batch):
  y=row*(th+label);mid=PREFIX+suffix
  for col,mode in enumerate(("context","top")):
   p=REND/f"{mid}--{mode}.png"
   if not p.is_file():raise RuntimeError("Missing Drowned Vault render "+str(p))
   im=Image.open(p).convert("RGB");im.thumbnail((tw,th),Image.Resampling.LANCZOS);sheet.paste(im,(col*tw+(tw-im.width)//2,y+(th-im.height)//2))
  draw.text((8,y+th+8),suffix,fill=(220,236,242),font=font);draw.text((tw+8,y+th+8),"top-down",fill=(150,195,210),font=font)
 target=SHEETS/f"drowned-vaults-{page:02d}.png";sheet.save(target);print("BUILT",target.name)
