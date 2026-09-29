#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"rime-sepulcher-renders";SHEETS=BASE/"sheets";SHEETS.mkdir(parents=True,exist_ok=True)
PREFIX="underworld-dungeon-frozen-rime-sepulcher-";SUFFIXES=["rime-mouth","long-glass-gallery","burial-colonnade","silent-crossing","icewell-shaft","whiteout-narthex","needle-pass","rimesilver-ossuary","clear-ice-lens-vault","still-air-crypt","frost-tick-niche","iceblind-hunt","cryolith-guard","frozen-archive","shelter-chapel","white-silence-antechamber","passage"]
font=ImageFont.load_default();tw,th,label=320,240,34
for page,start in enumerate(range(0,len(SUFFIXES),6),1):
 batch=SUFFIXES[start:start+6];sheet=Image.new("RGB",(tw*2,len(batch)*(th+label)),(12,24,34));draw=ImageDraw.Draw(sheet)
 for row,suffix in enumerate(batch):
  y=row*(th+label);mid=PREFIX+suffix
  for col,mode in enumerate(("context","top")):
   p=REND/f"{mid}--{mode}.png"
   if not p.is_file():raise RuntimeError("Missing Rime Sepulcher render "+str(p))
   im=Image.open(p).convert("RGB");im.thumbnail((tw,th),Image.Resampling.LANCZOS);sheet.paste(im,(col*tw+(tw-im.width)//2,y+(th-im.height)//2))
  draw.text((8,y+th+8),suffix,fill=(225,242,252),font=font);draw.text((tw+8,y+th+8),"top-down",fill=(170,214,238),font=font)
 target=SHEETS/f"rime-sepulcher-{page:02d}.png";sheet.save(target);print("BUILT",target.name)
