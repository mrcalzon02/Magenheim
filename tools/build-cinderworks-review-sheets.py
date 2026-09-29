#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"cinderworks-renders";SHEETS=BASE/"sheets";SHEETS.mkdir(parents=True,exist_ok=True)
PREFIX="underworld-dungeon-sulfur-cinderworks-";SUFFIXES=["cinder-gate","slag-nave","furnace-gallery","bellows-junction","chimney-shaft","vent-choir","slag-runoff","emberiron-foundry","sulfur-kiln","quench-vault","ashmite-conveyor","cinder-hound-yard","furnace-golem-crucible","broken-smeltery","pressure-lock","furnace-heart-antechamber","passage"]
font=ImageFont.load_default();tw,th,label=320,240,34
for page,start in enumerate(range(0,len(SUFFIXES),6),1):
 batch=SUFFIXES[start:start+6];sheet=Image.new("RGB",(tw*2,len(batch)*(th+label)),(30,16,10));draw=ImageDraw.Draw(sheet)
 for row,suffix in enumerate(batch):
  y=row*(th+label);mid=PREFIX+suffix
  for col,mode in enumerate(("context","top")):
   p=REND/f"{mid}--{mode}.png"
   if not p.is_file():raise RuntimeError("Missing Cinderworks render "+str(p))
   im=Image.open(p).convert("RGB");im.thumbnail((tw,th),Image.Resampling.LANCZOS);sheet.paste(im,(col*tw+(tw-im.width)//2,y+(th-im.height)//2))
  draw.text((8,y+th+8),suffix,fill=(242,224,205),font=font);draw.text((tw+8,y+th+8),"top-down",fill=(214,168,115),font=font)
 target=SHEETS/f"cinderworks-{page:02d}.png";sheet.save(target);print("BUILT",target.name)
