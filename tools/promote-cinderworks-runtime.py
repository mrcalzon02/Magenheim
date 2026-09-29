#!/usr/bin/env python3
"""Promote Cinderworks only after all 17 forged model payloads exist."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
CATALOG=ROOT/"src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
PREFIX="underworld-dungeon-sulfur-cinderworks-";EXPECTED=17
for directory,suffix in (("source",".blend"),("glb",".glb"),("runtime",".model.json")):
 files=sorted((ROOT/"assets/models"/directory).glob(PREFIX+"*"+suffix))
 if len(files)!=EXPECTED:raise SystemExit(f"Cinderworks promotion requires {EXPECTED} {directory} payloads; found {len(files)}")
text=CATALOG.read_text()
if re.search(r'SulfurousWastes\s*\{\s*get;\s*\}\s*=\s*Ready\(',text,re.MULTILINE):
 print("PASS Cinderworks catalog already RuntimeReady");raise SystemExit(0)
updated,count=re.subn(
 r'(public static UnderworldDungeonDefinition SulfurousWastes\s*\{\s*get;\s*\}\s*=\s*)Planned\(',
 r'\1Ready(',text,count=1,flags=re.MULTILINE)
if count!=1:raise SystemExit("Could not find exactly one Planned SulfurousWastes declaration")
CATALOG.write_text(updated)
print("PROMOTED Cinderworks: Planned -> RuntimeReady after complete 17-model gate")
