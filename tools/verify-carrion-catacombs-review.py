#!/usr/bin/env python3
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"carrion-catacombs-renders";SHEETS=BASE/"sheets"
PREFIX="underworld-dungeon-great-decay-carrion-catacombs-";SUFFIXES=["ossuary-gate","processional-hall","sunken-reliquary","root-split-crossing","bone-chute","miasma-nave","carrion-sluice","amber-mortuary","bone-gravel-crypt","censer-court","rotling-warrens","spore-husk-cloister","graft-warden-hall","vanishing-archive","defiant-work-chapel","corpse-orchard-antechamber","passage"]
missing=[]
for suffix in SUFFIXES:
 for mode in ("context","top"):
  p=REND/f"{PREFIX+suffix}--{mode}.png"
  if not p.is_file() or p.stat().st_size<4096:missing.append(str(p.relative_to(ROOT)))
for page in range(1,4):
 p=SHEETS/f"carrion-catacombs-{page:02d}.png"
 if not p.is_file() or p.stat().st_size<8192:missing.append(str(p.relative_to(ROOT)))
if missing:raise SystemExit("FAIL Carrion Catacombs review: "+", ".join(missing))
print("PASS Carrion Catacombs visual review: 34 renders + 3 contact sheets")
