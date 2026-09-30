#!/usr/bin/env python3
"""Resolve one exported Valheim prefab for each cinematic donor role."""
from __future__ import annotations
import argparse, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
ASSET_MANIFEST=ROOT/"assets/cinematics/peace-was-only-the-beginning/asset-manifest.json"

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--export-dir",type=Path,required=True)
    ap.add_argument("--output",type=Path,required=True)
    ns=ap.parse_args()
    spec=json.loads(ASSET_MANIFEST.read_text(encoding="utf-8"))
    runtime_manifest=ns.export_dir/"manifest.json"
    if not runtime_manifest.is_file():
        raise SystemExit("Runtime donor export manifest missing: "+str(runtime_manifest))
    runtime=json.loads(runtime_manifest.read_text(encoding="utf-8-sig"))
    exported={str(row["prefab"]).casefold():row for row in runtime.get("models",[]) if row.get("file")}
    resolved=[]
    missing=[]
    for group in spec["valheim_groups"]:
        selected=None
        for candidate in group["candidates"]:
            row=exported.get(candidate.casefold())
            if row:
                selected={"logical":group["logical"],"prefab":row["prefab"],"file":row["file"],
                          "meshes":row.get("meshes",0),"vertices":row.get("vertices",0),
                          "triangles":row.get("triangles",0),"required":bool(group.get("required"))}
                break
        if selected:
            source=ns.export_dir/selected["file"]
            if not source.is_file():
                raise SystemExit(f"Resolved donor file missing: {source}")
            resolved.append(selected)
        elif group.get("required"):
            missing.append(group["logical"]+" <= "+", ".join(group["candidates"]))
    if missing:
        raise SystemExit("Required Valheim cinematic donor group(s) unresolved: "+"; ".join(missing))
    if not resolved:
        raise SystemExit("No Valheim cinematic donors resolved.")
    ns.output.parent.mkdir(parents=True,exist_ok=True)
    payload={"schema":1,"source":"runtime-extracted Valheim prefabs; transient production artifact only",
             "asset_manifest":str(ASSET_MANIFEST.relative_to(ROOT)).replace("\\","/"),
             "resolved":resolved}
    ns.output.write_text(json.dumps(payload,indent=2)+"\n",encoding="utf-8")
    print("RESOLVED cinematic Valheim donors:",len(resolved),
          "required",sum(1 for r in resolved if r["required"]),
          "triangles",sum(int(r["triangles"]) for r in resolved))

if __name__=="__main__":
    main()
