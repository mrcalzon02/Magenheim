#!/usr/bin/env python3
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"cinderworks-renders";SHEETS=BASE/"sheets"
PREFIX="underworld-dungeon-sulfur-cinderworks-";SUFFIXES=["cinder-gate","slag-nave","furnace-gallery","bellows-junction","chimney-shaft","vent-choir","slag-runoff","emberiron-foundry","sulfur-kiln","quench-vault","ashmite-conveyor","cinder-hound-yard","furnace-golem-crucible","broken-smeltery","pressure-lock","furnace-heart-antechamber","passage"]
missing=[]
for s in SUFFIXES:
 for mode in ("context","top"):
  p=REND/f"{PREFIX+s}--{mode}.png"
  if not p.is_file() or p.stat().st_size<4096:missing.append(str(p.relative_to(ROOT)))
for page in range(1,4):
 p=SHEETS/f"cinderworks-{page:02d}.png"
 if not p.is_file() or p.stat().st_size<8192:missing.append(str(p.relative_to(ROOT)))
if missing:raise SystemExit("FAIL Cinderworks review: "+", ".join(missing))
print("PASS Cinderworks visual review: 34 renders + 3 contact sheets")
