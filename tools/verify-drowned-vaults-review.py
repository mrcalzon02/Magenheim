#!/usr/bin/env python3
"""Verify Drowned Vault visual review coverage."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/"dist/underworld-production-review";REND=BASE/"drowned-vault-renders";SHEETS=BASE/"sheets"
PREFIX="underworld-dungeon-blackwater-drowned-vaults-"
SUFFIXES=["drowned-sinkhole","tide-gallery","dry-ledger","collapsed-dock","siphon-hall","bell-chamber","split-cistern","drowned-shaft","pearl-vault","high-water-archive","lamprey-run","deep-hunter-lair","sunken-quay","broken-causeway","undertow-sluice","abyssal-sanctum","passage"]
missing=[]
for suffix in SUFFIXES:
 for mode in ("context","top"):
  p=REND/f"{PREFIX+suffix}--{mode}.png"
  if not p.is_file() or p.stat().st_size<4096:missing.append(str(p.relative_to(ROOT)))
for page in range(1,4):
 p=SHEETS/f"drowned-vaults-{page:02d}.png"
 if not p.is_file() or p.stat().st_size<8192:missing.append(str(p.relative_to(ROOT)))
if missing:raise SystemExit("FAIL Drowned Vault review: "+", ".join(missing))
print("PASS Drowned Vault visual review: 34 renders + 3 contact sheets")
