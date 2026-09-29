#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"carrion-catacombs-renders";SHEETS=BASE/"sheets";SHEETS.mkdir(parents=True,exist_ok=True)
PREFIX="underworld-dungeon-great-decay-carrion-catacombs-";SUFFIXES=["ossuary-gate","processional-hall","sunken-reliquary","root-split-crossing","bone-chute","miasma-nave","carrion-sluice","amber-mortuary","bone-gravel-crypt","censer-court","rotling-warrens","spore-husk-cloister","graft-warden-hall","vanishing-archive","defiant-work-chapel","corpse-orchard-antechamber","passage"]
font=ImageFont.load_default();tw,th,label=320,240,34
for page,start in enumerate(range(0,len(SUFFIXES),6),1):
 batch=SUFFIXES[start:start+6];sheet=Image.new("RGB",(tw*2,len(batch)*(th+label)),(28,18,13));draw=ImageDraw.Draw(sheet)
 for row,suffix in enumerate(batch):
  y=row*(th+label);mid=PREFIX+suffix
  for col,mode in enumerate(("context","top")):
   p=REND/f"{mid}--{mode}.png"
   if not p.is_file():raise RuntimeError("Missing Carrion Catacombs render "+str(p))
   im=Image.open(p).convert("RGB");im.thumbnail((tw,th),Image.Resampling.LANCZOS);sheet.paste(im,(col*tw+(tw-im.width)//2,y+(th-im.height)//2))
  draw.text((8,y+th+8),suffix,fill=(234,219,190),font=font);draw.text((tw+8,y+th+8),"top-down",fill=(197,166,111),font=font)
 target=SHEETS/f"carrion-catacombs-{page:02d}.png";sheet.save(target);print("BUILT",target.name)
