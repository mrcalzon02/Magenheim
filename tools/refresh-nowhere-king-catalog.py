#!/usr/bin/env python3
"""Refresh catalog metadata for only the two Nowhere King sword models."""
import csv
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/"assets"/"models"
IDS=("nowhere-king-sword-firmament","nowhere-king-sword-null-gate")

def sha(path: Path)->str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

catalog_path=MODELS/"catalog.json"
rows=json.loads(catalog_path.read_text(encoding="utf-8"))
by_id={row["id"]:row for row in rows}

for model_id in IDS:
    source=MODELS/"source"/f"{model_id}.blend"
    glb=MODELS/"glb"/f"{model_id}.glb"
    runtime=MODELS/"runtime"/f"{model_id}.model.json"
    for path in (source,glb,runtime):
        if not path.is_file():
            raise SystemExit(f"{model_id}: missing {path.relative_to(ROOT)}")
    doc=json.loads(runtime.read_text(encoding="utf-8"))
    parts=doc.get("parts",[])
    if not parts:
        raise SystemExit(f"{model_id}: runtime payload has no parts")
    materials={
        json.dumps(part.get("material",{}),sort_keys=True,separators=(",",":"))
        for part in parts
    }
    row=dict(
        id=model_id,
        parts=len(parts),
        triangles=sum(len(part.get("triangles",[]))//3 for part in parts),
        materials=len(materials),
        source=f"source/{model_id}.blend",
        glb=f"glb/{model_id}.glb",
        runtime=f"runtime/{model_id}.model.json",
        source_sha256=sha(source),
        runtime_sha256=sha(runtime),
        glb_sha256=sha(glb),
    )
    by_id[model_id]=row
    print("CATALOGED",model_id,row["parts"],row["triangles"],flush=True)

ordered=[by_id[key] for key in sorted(by_id)]
catalog_path.write_text(json.dumps(ordered,indent=2)+"\n",encoding="utf-8")
fields=["id","parts","triangles","materials","source","glb","runtime","source_sha256","runtime_sha256","glb_sha256"]
with (MODELS/"catalog.tsv").open("w",encoding="utf-8",newline="") as handle:
    writer=csv.DictWriter(handle,fieldnames=fields,delimiter="\t",lineterminator="\n")
    writer.writeheader()
    writer.writerows(ordered)
