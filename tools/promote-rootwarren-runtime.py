#!/usr/bin/env python3
"""Promote the Fungal Rootwarren from Planned to RuntimeReady after forged payload admission.

This script is intentionally narrow and idempotent. It refuses partial/missing asset families and
changes exactly one catalog constructor call. It never creates assets and never promotes any other
dungeon family.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
PREFIX = "underworld-dungeon-fungal-rootwarren-"
EXPECTED = 17


def require_complete(directory: str, suffix: str) -> None:
    root = ROOT / "assets/models" / directory
    files = sorted(root.glob(PREFIX + "*" + suffix))
    if len(files) != EXPECTED:
        raise SystemExit(
            f"Rootwarren promotion requires {EXPECTED} {directory} payloads; found {len(files)}")
    names = [path.name for path in files]
    if len(set(names)) != EXPECTED:
        raise SystemExit(f"Rootwarren promotion found duplicate {directory} payload identities")


require_complete("source", ".blend")
require_complete("glb", ".glb")
require_complete("runtime", ".model.json")

text = CATALOG.read_text(encoding="utf-8")
planned_pattern = (
    r'(public static UnderworldDungeonDefinition FungalForest\s*\{\s*get;\s*\}\s*=\s*)'
    r'Planned\('
)
ready_pattern = (
    r'public static UnderworldDungeonDefinition FungalForest\s*\{\s*get;\s*\}\s*=\s*'
    r'Ready\('
)

if re.search(ready_pattern, text, re.MULTILINE):
    print("PASS Rootwarren catalog is already RuntimeReady")
    raise SystemExit(0)

updated, count = re.subn(
    planned_pattern,
    r'\1Ready(',
    text,
    count=1,
    flags=re.MULTILINE,
)
if count != 1:
    raise SystemExit(
        "Rootwarren promotion could not find exactly one Planned FungalForest catalog declaration")

CATALOG.write_text(updated, encoding="utf-8")
print("PROMOTED Rootwarren catalog: Planned -> RuntimeReady after complete 17-model asset gate")
