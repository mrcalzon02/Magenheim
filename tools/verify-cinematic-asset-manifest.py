#!/usr/bin/env python3
"""Validate/query the asset-locked cinematic manifest.

Magenheim-owned .blend sources are committed. Valheim donor meshes are referenced by
prefab identity and may only exist as runtime-extracted CI/local artifacts.
"""
from __future__ import annotations
import argparse, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "assets/cinematics/peace-was-only-the-beginning/asset-manifest.json"
SOURCE = ROOT / "assets/models/source"

def load():
    data=json.loads(MANIFEST.read_text(encoding="utf-8"))
    assert data.get("schema")==1
    assert data.get("cinematic")=="peace-was-only-the-beginning"
    assert data.get("magenheim")
    assert data.get("valheim_groups")
    return data

def verify(data):
    missing=[]
    for entry in data["magenheim"]:
        model=entry["model"]
        path=SOURCE/(model+".blend")
        if entry.get("required") and not path.is_file():
            missing.append(str(path.relative_to(ROOT)))
    logical=[x["logical"] for x in data["valheim_groups"]]
    if len(logical)!=len(set(logical)):
        raise SystemExit("Duplicate Valheim logical donor identity.")
    for entry in data["valheim_groups"]:
        if not entry.get("candidates"):
            raise SystemExit("Valheim donor group has no candidates: "+entry["logical"])
    if missing:
        raise SystemExit("Missing required Magenheim cinematic source(s): "+", ".join(missing))
    print(f"VERIFIED cinematic asset manifest: {len(data['magenheim'])} Magenheim identities, "
          f"{len(data['valheim_groups'])} Valheim donor groups")

def prefab_list(data):
    seen=set()
    for entry in data["valheim_groups"]:
        for name in entry["candidates"]:
            key=name.casefold()
            if key not in seen:
                seen.add(key)
                print(name)

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--prefab-list",action="store_true")
    ns=ap.parse_args()
    data=load()
    verify(data)
    if ns.prefab_list:
        prefab_list(data)

if __name__=="__main__":
    main()
