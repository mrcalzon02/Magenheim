#!/usr/bin/env python3
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"rime-sepulcher-renders";SHEETS=BASE/"sheets"
PREFIX="underworld-dungeon-frozen-rime-sepulcher-";SUFFIXES=["rime-mouth","long-glass-gallery","burial-colonnade","silent-crossing","icewell-shaft","whiteout-narthex","needle-pass","rimesilver-ossuary","clear-ice-lens-vault","still-air-crypt","frost-tick-niche","iceblind-hunt","cryolith-guard","frozen-archive","shelter-chapel","white-silence-antechamber","passage"]
missing=[]
for suffix in SUFFIXES:
 for mode in ("context","top"):
  p=REND/f"{PREFIX+suffix}--{mode}.png"
  if not p.is_file() or p.stat().st_size<4096:missing.append(str(p.relative_to(ROOT)))
for page in range(1,4):
 p=SHEETS/f"rime-sepulcher-{page:02d}.png"
 if not p.is_file() or p.stat().st_size<8192:missing.append(str(p.relative_to(ROOT)))
if missing:raise SystemExit("FAIL Rime Sepulcher review: "+", ".join(missing))
print("PASS Rime Sepulcher visual review: 34 renders + 3 contact sheets")
