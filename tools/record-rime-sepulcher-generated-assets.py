#!/usr/bin/env python3
"""Record a successfully forged Rime Sepulcher family into generated.manifest.json."""
from pathlib import Path
import hashlib
import json

ROOT=Path(__file__).resolve().parents[1]
MANIFEST=ROOT/"assets/generated.manifest.json"
PREFIX="underworld-dungeon-frozen-rime-sepulcher-"
GENERATOR_ID="rime-sepulcher-dungeon-models"
GENERATOR="tools/author-rime-sepulcher-dungeon.py"
INPUTS=[
    "src/Magenheim.Core/Underworld/UnderworldFrozenRimeSepulcherCatalog.cs",
    "src/Magenheim.Core/Underworld/UnderworldRimeSepulcherExposurePolicy.cs",
    "tools/underworld_material_library.py",
    "tools/verify-rime-sepulcher-dungeon.py",
    "tools/verify-rime-sepulcher-production-contract.py",
    "tools/rebuild-rime-sepulcher-dungeon.ps1",
    "tools/export-model-assets.py",
]
PATTERNS=[
    "assets/models/source/underworld-dungeon-frozen-rime-sepulcher-*.blend",
    "assets/models/glb/underworld-dungeon-frozen-rime-sepulcher-*.glb",
    "assets/models/runtime/underworld-dungeon-frozen-rime-sepulcher-*.model.json",
]

def sha(path):
    digest=hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda:handle.read(1<<20),b""):
            digest.update(block)
    return digest.hexdigest()

def input_hash():
    digest=hashlib.sha256()
    for name in INPUTS:
        path=ROOT/name
        if not path.is_file():
            raise SystemExit("Rime Sepulcher freshness input missing: "+name)
        digest.update(name.encode()+b"|"+sha(path).encode()+b"|")
    return digest.hexdigest()

def outputs():
    found_outputs=set()
    for pattern in PATTERNS:
        found=[path for path in ROOT.glob(pattern) if path.is_file()]
        if len(found)!=17:
            raise SystemExit(
                f"Rime Sepulcher forge must produce 17 files for {pattern!r}; found {len(found)}")
        found_outputs.update(path.relative_to(ROOT).as_posix() for path in found)
    if len(found_outputs)!=51:
        raise SystemExit(
            f"Rime Sepulcher freshness expects 51 outputs, found {len(found_outputs)}")
    return sorted(found_outputs)

def main():
    generator=ROOT/GENERATOR
    if not generator.is_file():
        raise SystemExit("Rime Sepulcher generator missing")
    out=outputs()
    data=json.loads(MANIFEST.read_text())
    entries=data["generators"]
    outset=set(out)
    for entry in entries:
        if entry.get("id")==GENERATOR_ID:
            continue
        collision=sorted(set((entry.get("output_sha256") or {}).keys())&outset)
        if collision:
            raise SystemExit(
                f"Rime Sepulcher output already belongs to {entry.get('id')}: {collision[0]}")
    replacement={
        "id":GENERATOR_ID,
        "generator":GENERATOR,
        "command":["pwsh","-NoProfile","-File","tools/rebuild-rime-sepulcher-dungeon.ps1"],
        "outputs":PATTERNS,
        "hash":"generator-only",
        "generator_sha256":sha(generator),
        "output_sha256":{name:None for name in out},
        "inputs":INPUTS,
        "inputs_sha256":input_hash(),
    }
    index=next((i for i,e in enumerate(entries) if e.get("id")==GENERATOR_ID),None)
    if index is None:
        entries.append(replacement)
    else:
        entries[index]=replacement
    MANIFEST.write_text(json.dumps(data,indent=2)+"\n")
    print("RECORDED Rime Sepulcher generated freshness:",len(out),"outputs")

if __name__=="__main__":
    main()
