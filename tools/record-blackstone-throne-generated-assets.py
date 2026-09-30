#!/usr/bin/env python3
"""Record generated-asset ownership for the Blackstone Throne family after a successful rebuild."""
from pathlib import Path
import hashlib,json

ROOT=Path(__file__).resolve().parents[1]
MANIFEST=ROOT/"assets"/"generated.manifest.json"

def sha(path):
    h=hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda:f.read(1<<20),b""): h.update(block)
    return h.hexdigest()

def outputs(patterns):
    found=set()
    for pattern in patterns:
        for path in ROOT.glob(pattern):
            if path.is_file(): found.add(path.relative_to(ROOT).as_posix())
    if not found: raise RuntimeError("Blackstone ownership recorder found no outputs")
    return sorted(found)

manifest=json.loads(MANIFEST.read_text())
entries=manifest["generators"]
by_id={e["id"]:e for e in entries}

texture_patterns=[
 "assets/textures/underworld/blackstone/blackstone-*.png",
 "assets/material-source/blackstone/blackstone-*.png",
]
texture_entry={
 "id":"blackstone-throne-textures",
 "generator":"tools/generate-blackstone-throne-textures.py",
 "command":["python","tools/generate-blackstone-throne-textures.py"],
 "outputs":texture_patterns,
 "generator_sha256":sha(ROOT/"tools"/"generate-blackstone-throne-textures.py"),
 "output_sha256":{},
 "hash":"bytes",
}
for name in outputs(texture_patterns): texture_entry["output_sha256"][name]=sha(ROOT/name)

ids=[
 "blackstone-throne","blackstone-banner","blackstone-attendant-seat","blackstone-brazier",
 "blackstone-stair","blackstone-dais","blackstone-parapet","blackstone-bridge",
 "blackstone-arch","blackstone-cliff-edge","blackstone-floor-tile","blackstone-spire",
 "blackstone-pillar","dark-throne",
]
model_patterns=[]
for model_id in ids:
    model_patterns += [
      f"assets/models/source/{model_id}.blend",
      f"assets/models/glb/{model_id}.glb",
      f"assets/models/runtime/{model_id}.model.json",
    ]
inputs=[
 "tools/generate-blackstone-throne-textures.py",
 "tools/export-model-assets.py",
 "tools/rebuild-blackstone-throne-set.ps1",
 "tools/verify-blackstone-throne-set.py",
]
digest=hashlib.sha256()
for name in inputs:
    digest.update(name.encode()+b"|"+sha(ROOT/name).encode()+b"|")
model_entry={
 "id":"blackstone-throne-set",
 "generator":"tools/author-blackstone-throne-set.py",
 "command":["pwsh","-NoProfile","-File","tools/rebuild-blackstone-throne-set.ps1"],
 "outputs":model_patterns,
 "generator_sha256":sha(ROOT/"tools"/"author-blackstone-throne-set.py"),
 "inputs":inputs,
 "inputs_sha256":digest.hexdigest(),
 "output_sha256":{name:None for name in outputs(model_patterns)},
 "hash":"generator-only",
}

for entry in (texture_entry,model_entry):
    old=by_id.get(entry["id"])
    if old is None: entries.append(entry)
    else:
        index=entries.index(old); entries[index]=entry

MANIFEST.write_text(json.dumps(manifest,indent=2)+"\n")
print("RECORDED Blackstone Throne generated ownership:",len(texture_entry["output_sha256"]),"2D assets and",len(model_entry["output_sha256"]),"model representations")
