#!/usr/bin/env python3
"""Promote Rime Sepulcher only after all 17 forged model payloads exist."""
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
CATALOG=ROOT/"src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
PREFIX="underworld-dungeon-frozen-rime-sepulcher-"
EXPECTED=17

for directory,suffix in (("source",".blend"),("glb",".glb"),("runtime",".model.json")):
    files=sorted((ROOT/"assets/models"/directory).glob(PREFIX+"*"+suffix))
    if len(files)!=EXPECTED:
        raise SystemExit(
            f"Rime Sepulcher promotion requires {EXPECTED} {directory} payloads; found {len(files)}")

text=CATALOG.read_text()
if re.search(r'FrozenCaverns\s*\{\s*get;\s*\}\s*=\s*Ready\(',text,re.MULTILINE):
    print("PASS Rime Sepulcher catalog already RuntimeReady")
    raise SystemExit(0)

updated,count=re.subn(
    r'(public static UnderworldDungeonDefinition FrozenCaverns\s*\{\s*get;\s*\}\s*=\s*)Planned\(',
    r'\1Ready(',
    text,
    count=1,
    flags=re.MULTILINE)
if count!=1:
    raise SystemExit("Could not find exactly one Planned FrozenCaverns declaration")

CATALOG.write_text(updated)
print("PROMOTED Rime Sepulcher: Planned -> RuntimeReady after complete 17-model gate")
