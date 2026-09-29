#!/usr/bin/env python3
"""Verify Rootwarren visual review coverage."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "dist/underworld-production-review"
RENDERS = BASE / "rootwarren-renders"
SHEETS = BASE / "sheets"
PREFIX = "underworld-dungeon-fungal-rootwarren-"
SUFFIXES = [
    "fracture-mouth","mycelial-gallery","glowcap-vault","spore-basin","root-bridge",
    "sunken-nursery","tangle-junction","shelf-drop","amber-grotto","worldroot-hollow",
    "crawler-nest","stalker-den","puffback-graze","buried-archway","root-squeeze",
    "heartcap-sanctum","passage",
]
missing = []
for suffix in SUFFIXES:
    model_id = PREFIX + suffix
    for mode in ("context","top"):
        path = RENDERS / f"{model_id}--{mode}.png"
        if not path.is_file() or path.stat().st_size < 4096:
            missing.append(str(path.relative_to(ROOT)))
for page in range(1,4):
    path = SHEETS / f"rootwarren-{page:02d}.png"
    if not path.is_file() or path.stat().st_size < 8192:
        missing.append(str(path.relative_to(ROOT)))
if missing:
    raise SystemExit("FAIL Rootwarren review: missing/undersized: " + ", ".join(missing))
print("PASS Rootwarren visual review: 34 renders + 3 contact sheets")
