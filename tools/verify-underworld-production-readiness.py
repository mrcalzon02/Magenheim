#!/usr/bin/env python3
"""Cheap preflight for the one-run Magenheim Blender production forge."""
import json
import py_compile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
scope=json.loads((ROOT/"tools"/"underworld-production-scope.json").read_text())
manifest=json.loads((ROOT/"assets"/"generated.manifest.json").read_text())
entries={e["id"]:e for e in manifest["generators"]}
fail=[]

def require(ok,msg):
    if not ok: fail.append(msg)

require(len(scope["regenerate"])==len(set(scope["regenerate"])),"production generator order contains duplicates")
for gid in scope["regenerate"]+scope["verify_only"]:
    require(gid in entries,"manifest missing production authority: "+gid)
for path in (
 "tools/generate-underworld-material-textures.py","tools/underworld_material_library.py",
 "tools/verify-underworld-material-textures.py","tools/author-rootforged-placeables.py",
 "tools/author-underworld-stations.py","tools/author-underworld-tools.py",
 "tools/author-underworld-armour.py","tools/verify-underworld-armour.py",
 "tools/author-underworld-weapons.py","tools/author-crystal-weapons.py",
 "tools/rebuild-underworld-production.ps1","tools/render-underworld-production-review.py",
 "tools/build-underworld-production-review-sheets.py","tools/verify-underworld-production-review.py",
 ".github/workflows/magenheim-production-forge.yml",
):
    require((ROOT/path).is_file(),"missing production file: "+path)

python_sources=(
 "tools/generate-underworld-material-textures.py","tools/underworld_material_library.py",
 "tools/verify-underworld-material-textures.py","tools/author-rootforged-placeables.py",
 "tools/author-underworld-stations.py","tools/author-underworld-tools.py",
 "tools/author-underworld-armour.py","tools/verify-underworld-armour.py",
 "tools/author-underworld-weapons.py","tools/author-crystal-weapons.py",
 "tools/render-underworld-production-review.py","tools/build-underworld-production-review-sheets.py",
 "tools/verify-underworld-production-review.py","tools/verify-generated-freshness.py",
)
for source in python_sources:
    try: py_compile.compile(str(ROOT/source),doraise=True)
    except py_compile.PyCompileError as error: fail.append(source+" syntax error: "+str(error))

for path in (
 "tools/author-rootforged-placeables.py","tools/author-underworld-stations.py",
 "tools/author-underworld-tools.py","tools/author-underworld-armour.py","tools/author-underworld-weapons.py",
):
    text=(ROOT/path).read_text()
    require("bind_underworld_material" in text,path+" is not bound to the shared material library")
    require("sys.path.insert(0, str(Path(__file__).resolve().parent))" in text,path+" does not expose tools/ to Blender Python imports")
    require("bpy.ops.uv.smart_project" not in text,path+" uses forbidden smart_project UV generation")

crystal=(ROOT/"tools"/"author-crystal-weapons.py").read_text()
require("bake_atlas(" in crystal and "unwrap(" in crystal,
        "crystal weapon authority lost purpose-authored unwrap/baked atlas pipeline")
uw=(ROOT/"tools"/"author-underworld-weapons.py").read_text()
require("bpy.ops.wm.open_mainfile" in uw and "derived_from" in uw,
        "Underworld weapon derivatives no longer preserve Crystal weapon source ancestry")
root=(ROOT/"tools"/"author-rootforged-placeables.py").read_text()
require("DETAIL_REVISION=3" in root and "DETAIL_FLOORS=" in root,
        "Rootforged endgame detail regression floor is absent")
arm=(ROOT/"tools"/"author-underworld-armour.py").read_text()
require("valheim-player-attach-skin" in arm and "BONE_ORDER=[" in arm,
        "Underworld armour source rig contract is absent")
wrapper=(ROOT/"tools"/"blender.ps1").read_text()
require("MAGENHEIM_BLENDER" in wrapper,"Blender wrapper cannot accept the Actions executable through environment")
workflow_path=ROOT/".github/workflows/magenheim-production-forge.yml"
if workflow_path.is_file():
    workflow=workflow_path.read_text()
    require("workflow_dispatch:" in workflow and "pull_request:" not in workflow and "\npush:" not in workflow,
            "production forge must remain manual-only")
    require("blender-5.0.0" in workflow,"workflow does not pin Blender 5.0.0")

patterns={}
for gid in scope["regenerate"]:
    for out in entries[gid].get("outputs",[]):
        owner=patterns.get(out)
        require(owner is None or owner==gid,f"output pattern {out} is owned by both {owner} and {gid}")
        patterns[out]=gid

if fail:
    print("FAIL Magenheim production preflight")
    for item in fail: print(" - "+item)
    raise SystemExit(1)
print("PASS Magenheim production preflight:",len(scope["regenerate"]),"regenerated authorities;",
      len(scope["verify_only"]),"verification-only families; Blender",scope["blender_version"])
