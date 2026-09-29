#!/usr/bin/env python3
"""Record a successfully forged Drowned Vault family into generated.manifest.json."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[1]
MANIFEST=ROOT/"assets/generated.manifest.json"
PREFIX="underworld-dungeon-blackwater-drowned-vaults-"
GENERATOR_ID="drowned-vaults-dungeon-models"
GENERATOR="tools/author-drowned-vaults-dungeon.py"
INPUTS=[
 "src/Magenheim.Core/Underworld/UnderworldBlackwaterDrownedVaultsCatalog.cs",
 "src/Magenheim.Core/Underworld/UnderworldDrownedVaultPassagePolicy.cs",
 "tools/underworld_material_library.py","tools/verify-drowned-vaults-dungeon.py",
 "tools/verify-drowned-vaults-production-contract.py","tools/rebuild-drowned-vaults-dungeon.ps1",
 "tools/export-model-assets.py",
]
PATTERNS=[
 "assets/models/source/underworld-dungeon-blackwater-drowned-vaults-*.blend",
 "assets/models/glb/underworld-dungeon-blackwater-drowned-vaults-*.glb",
 "assets/models/runtime/underworld-dungeon-blackwater-drowned-vaults-*.model.json",
]
def sha(path):
 d=hashlib.sha256()
 with path.open("rb") as h:
  for block in iter(lambda:h.read(1<<20),b""):d.update(block)
 return d.hexdigest()
def inputs_hash():
 d=hashlib.sha256()
 for name in INPUTS:
  p=ROOT/name
  if not p.is_file():raise SystemExit("Drowned Vault freshness input missing: "+name)
  d.update(name.encode()+b"|"+sha(p).encode()+b"|")
 return d.hexdigest()
def outputs():
 out=set()
 for pattern in PATTERNS:
  found=[p for p in ROOT.glob(pattern) if p.is_file()]
  if len(found)!=17:raise SystemExit(f"Drowned Vault forge must produce 17 files for {pattern!r}; found {len(found)}")
  out.update(p.relative_to(ROOT).as_posix() for p in found)
 if len(out)!=51:raise SystemExit(f"Drowned Vault freshness expects 51 outputs, found {len(out)}")
 return sorted(out)
def main():
 generator=ROOT/GENERATOR
 if not generator.is_file():raise SystemExit("Drowned Vault generator missing")
 out=outputs();data=json.loads(MANIFEST.read_text());entries=data["generators"];outset=set(out)
 for entry in entries:
  if entry.get("id")==GENERATOR_ID:continue
  collision=sorted(set((entry.get("output_sha256") or {}).keys())&outset)
  if collision:raise SystemExit(f"Drowned Vault output already belongs to {entry.get('id')}: {collision[0]}")
 replacement={"id":GENERATOR_ID,"generator":GENERATOR,
  "command":["pwsh","-NoProfile","-File","tools/rebuild-drowned-vaults-dungeon.ps1"],
  "outputs":PATTERNS,"hash":"generator-only","generator_sha256":sha(generator),
  "output_sha256":{name:None for name in out},"inputs":INPUTS,"inputs_sha256":inputs_hash()}
 idx=next((i for i,e in enumerate(entries) if e.get("id")==GENERATOR_ID),None)
 if idx is None:entries.append(replacement)
 else:entries[idx]=replacement
 MANIFEST.write_text(json.dumps(data,indent=2)+"\n")
 print("RECORDED Drowned Vault generated freshness:",len(out),"outputs")
if __name__=="__main__":main()
