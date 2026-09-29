#!/usr/bin/env python3
"""Cheap preflight for the one-run Magenheim Blender production forge."""
import ast
import json
import py_compile
import re
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/"tools"))
from underworld_material_library import material_key
scope=json.loads((ROOT/"tools"/"underworld-production-scope.json").read_text())
manifest=json.loads((ROOT/"assets"/"generated.manifest.json").read_text())
entries={e["id"]:e for e in manifest["generators"]}
fail=[]

def require(ok,msg):
    if not ok: fail.append(msg)

require(len(scope["regenerate"])==len(set(scope["regenerate"])),"production generator order contains duplicates")
require("staff-model-exports" in scope["regenerate"] and "staff-icons" in scope["regenerate"],
        "all 32 elemental staff exports and icons must participate in the production run")
require("staff-icons" not in scope["verify_only"],"staff icons cannot remain verification-only in the weapon production run")
require(scope["verify_only"]==["crystal-tier-icons"],"only crystal-tier icons should remain verification-only in this production scope")
for gid in scope["regenerate"]+scope["verify_only"]:
    require(gid in entries,"manifest missing production authority: "+gid)
for path in (
 "tools/generate-underworld-material-textures.py","tools/underworld_material_library.py",
 "tools/verify-underworld-material-textures.py","tools/author-underworld-material-items.py",
 "tools/underworld-material-item-specs.json","tools/rebuild-underworld-material-items.ps1",
 "tools/render-underworld-resource-icons.py","tools/author-underworld-geothermal-vents.py",
 "tools/rebuild-underworld-geothermal-vents.ps1","tools/author-rootforged-placeables.py",
 "tools/author-underworld-stations.py","tools/author-underworld-tools.py",
 "tools/author-underworld-armour.py","tools/verify-underworld-armour.py",
 "tools/author-underworld-weapons.py","tools/author-crystal-weapons.py",
 "tools/rebuild-staff-production-assets.ps1","tools/rebuild-underworld-production.ps1",
 "tools/author-rootwarren-dungeon.py","tools/verify-rootwarren-dungeon.py",
 "tools/verify-rootwarren-production-contract.py","tools/record-rootwarren-generated-assets.py",
 "tools/promote-rootwarren-runtime.py","tools/rebuild-rootwarren-dungeon.ps1","tools/render-underworld-production-review.py",
 "tools/render-underworld-armour-articulation-review.py","tools/render-rootwarren-review.py",
 "tools/build-underworld-production-review-sheets.py","tools/build-rootwarren-review-sheets.py",
 "tools/verify-underworld-production-review.py","tools/verify-rootwarren-review.py",
 "tools/verify-underworld-production-assets.py",
 ".github/workflows/magenheim-production-forge.yml",
):
    require((ROOT/path).is_file(),"missing production file: "+path)

python_sources=(
 "tools/generate-underworld-material-textures.py","tools/underworld_material_library.py",
 "tools/verify-underworld-material-textures.py","tools/author-underworld-material-items.py",
 "tools/render-underworld-resource-icons.py","tools/author-underworld-geothermal-vents.py",
 "tools/author-rootforged-placeables.py",
 "tools/author-underworld-stations.py","tools/author-underworld-tools.py",
 "tools/author-underworld-armour.py","tools/verify-underworld-armour.py",
 "tools/author-underworld-weapons.py","tools/author-crystal-weapons.py",
 "tools/author-rootwarren-dungeon.py","tools/verify-rootwarren-dungeon.py",
 "tools/verify-rootwarren-production-contract.py","tools/record-rootwarren-generated-assets.py",
 "tools/promote-rootwarren-runtime.py","tools/render-underworld-production-review.py","tools/render-underworld-armour-articulation-review.py",
 "tools/render-rootwarren-review.py","tools/build-underworld-production-review-sheets.py","tools/build-rootwarren-review-sheets.py",
 "tools/verify-underworld-production-review.py","tools/verify-rootwarren-review.py","tools/verify-underworld-production-assets.py","tools/verify-generated-freshness.py",
 "tools/export-model-assets.py",
)
for source in python_sources:
    try: py_compile.compile(str(ROOT/source),doraise=True)
    except py_compile.PyCompileError as error: fail.append(source+" syntax error: "+str(error))

for path in (
 "tools/author-underworld-material-items.py","tools/author-rootforged-placeables.py","tools/author-underworld-stations.py",
 "tools/author-underworld-tools.py","tools/author-underworld-armour.py","tools/author-underworld-weapons.py",
):
    text=(ROOT/path).read_text()
    require("bind_underworld_material" in text,path+" is not bound to the shared material library")
    require(re.search(r"sys\.path\.insert\(\s*0\s*,\s*str\(Path\(__file__\)\.resolve\(\)\.parent\)\s*\)", text) is not None,
            path+" does not expose tools/ to Blender Python imports")
    require("bpy.ops.uv.smart_project" not in text,path+" uses forbidden smart_project UV generation")

generator_tree=ast.parse((ROOT/"tools"/"generate-underworld-material-textures.py").read_text())
material_specs=None
for node in generator_tree.body:
    if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=="SPECS" for t in node.targets):
        material_specs=ast.literal_eval(node.value);break
require(isinstance(material_specs,dict) and len(material_specs)==25,"Underworld material generator must own exactly 25 families")
material_binder=(ROOT/"tools"/"underworld_material_library.py").read_text()
require("metallic-smoothness" in material_binder and "ShaderNodeSeparateColor" in material_binder and 'bsdf.inputs["Metallic"]' in material_binder,
        "Shared Underworld Blender binder must apply the authored metallic channel during visual review")
literal_names=[]
for source in (
 "tools/author-underworld-geothermal-vents.py","tools/author-rootforged-placeables.py",
 "tools/author-underworld-stations.py","tools/author-underworld-tools.py"
):
    literal_names += re.findall(r'(?:material|mat)\(\s*["\']([^"\']+)["\']',(ROOT/source).read_text())
literal_names += [
 "armour.sporeweave.base","armour.sporeweave.structure","armour.sporeweave.accent",
 "armour.palewater.base","armour.palewater.structure","armour.palewater.accent",
 "armour.emberiron.base","armour.emberiron.structure","armour.emberiron.accent",
 "armour.rimeward.base","armour.rimeward.structure","armour.rimeward.accent",
 "armour.stoneanchor.base","armour.stoneanchor.structure","armour.stoneanchor.accent",
 "armour.defiant.base","armour.defiant.structure","armour.defiant.accent",
 "magenheim.underworld-weapon.preview.worldroot.timber",
 "magenheim.underworld-weapon.preview.spore-crystal.crystal",
 "magenheim.underworld-weapon.preview.flowstone",
 "magenheim.underworld-weapon.preview.blackwater-pearl",
 "magenheim.underworld-weapon.preview.pale-fibre",
 "magenheim.underworld-weapon.preview.emberiron",
 "magenheim.underworld-weapon.preview.furnace.heart",
 "magenheim.underworld-weapon.preview.charred-root",
 "magenheim.underworld-weapon.preview.rimesilver",
 "magenheim.underworld-weapon.preview.clear-ice",
 "magenheim.underworld-weapon.preview.rimewood",
 "magenheim.underworld-weapon.preview.titanbone",
 "magenheim.underworld-weapon.preview.forged-brace",
 "magenheim.underworld-weapon.preview.fracture-crystal",
 "magenheim.underworld-weapon.preview.shardstone",
 "magenheim.underworld-weapon.preview.rotwood",
 "magenheim.underworld-weapon.preview.bone",
 "magenheim.underworld-weapon.preview.carrion-amber",
]
material_item_specs=json.loads((ROOT/"tools"/"underworld-material-item-specs.json").read_text())
material_item_author=(ROOT/"tools"/"author-underworld-material-items.py").read_text()
weapon_author=(ROOT/"tools"/"author-underworld-weapons.py").read_text()
require("bind_underworld_material(bpy,m,semantic)" in material_item_author,
        "Material-item author must bind shared PBR by explicit semantic, not model-id-bearing material name")
require("semantic=None" in weapon_author and "bind_underworld_material(bpy,m,semantic or name)" in weapon_author,
        "Underworld weapon author must separate unique material names from shared PBR semantics")
require(len(material_item_specs)==32,"Underworld material-item catalog must own exactly 32 newly-authored models")
require(sum(1 for model_id in material_item_specs if model_id.startswith("underworld-resource-"))==14,
        "Material-item author must own exactly the fourteen previously missing raw resource models")
require(sum(1 for model_id in material_item_specs if model_id.startswith("underworld-refined-"))==18,
        "Material-item author must own all eighteen refinement models")
visuals=(ROOT/"src"/"Magenheim.Runtime"/"UnderworldResourceVisuals.cs").read_text()
visual_pairs=dict(re.findall(
    r'\["(Magenheim_Underworld_(?:Resource|Refined)_[^"]+)"\]\s*=\s*"([^"]+)"',visuals))
raw_catalog_text=(ROOT/"src"/"Magenheim.Core"/"Underworld"/"UnderworldResourceCatalog.cs").read_text()
fungal_refinement_text=(ROOT/"src"/"Magenheim.Core"/"Underworld"/"UnderworldFungalRefinementCatalog.cs").read_text()
biome_refinement_text=(ROOT/"src"/"Magenheim.Core"/"Underworld"/"UnderworldBiomeRefinementCatalog.cs").read_text()
raw_catalog=set(re.findall(
    r'new UnderworldResourceDefinition\(\s*"[^"]+"\s*,\s*"(Magenheim_Underworld_Resource_[^"]+)"',
    raw_catalog_text))
refined_catalog=set(re.findall(
    r'public const string \w+\s*=\s*"(Magenheim_Underworld_Refined_[^"]+)"',
    fungal_refinement_text+"\n"+biome_refinement_text))
mapped_raw={prefab for prefab in visual_pairs if prefab.startswith("Magenheim_Underworld_Resource_")}
mapped_refined={prefab for prefab in visual_pairs if prefab.startswith("Magenheim_Underworld_Refined_")}
require(len(raw_catalog)==22,"Raw Underworld authority must contain exactly 22 prefab identities")
require(len(refined_catalog)==18,"Refined Underworld authority must contain exactly 18 prefab identities")
require(mapped_raw==raw_catalog,
        "Runtime raw-material visual identities drifted from UnderworldResourceCatalog: missing="+
        repr(sorted(raw_catalog-mapped_raw))+" extra="+repr(sorted(mapped_raw-raw_catalog)))
require(mapped_refined==refined_catalog,
        "Runtime refined-material visual identities drifted from refinement catalogs: missing="+
        repr(sorted(refined_catalog-mapped_refined))+" extra="+repr(sorted(mapped_refined-refined_catalog)))
mapped_models=set(visual_pairs.values())
new_models=set(material_item_specs)
require(new_models <= mapped_models,
        "Every newly-authored material model must be consumed by the runtime visual map")
legacy_material_models=mapped_models-new_models
require(len(legacy_material_models)==8 and all(model.startswith("underworld-resource-") for model in legacy_material_models),
        "Exactly eight pre-existing Fungal/Blackwater raw material models must remain outside the 32-model author")
for model_id in legacy_material_models:
    require((ROOT/"assets"/"models"/"source"/(model_id+".blend")).is_file() and
            (ROOT/"assets"/"models"/"runtime"/(model_id+".model.json")).is_file(),
            "Legacy raw material visual lacks an admitted source/runtime model: "+model_id)
material_item_manifest=entries.get("underworld-material-item-models",{})
require(material_item_manifest.get("generator")=="tools/author-underworld-material-items.py",
        "Underworld material-item generator authority is missing")
require("underworld-resource-icons" in scope["regenerate"],
        "Owned raw/refined material icons must be regenerated in the one-run forge")
for primary,secondary,form in material_item_specs.values():
    for semantic in (primary,secondary):
        try:
            key=material_key(semantic)
            require(key in material_specs,semantic+" resolves to undeclared material family "+key)
        except Exception as error:
            fail.append(semantic+" material mapping failed: "+str(error))

for semantic in literal_names:
    try:
        key=material_key(semantic)
        require(key in material_specs,semantic+" resolves to undeclared material family "+key)
    except Exception as error:
        fail.append(semantic+" material mapping failed: "+str(error))

crystal=(ROOT/"tools"/"author-crystal-weapons.py").read_text()
require("bake_atlas(" in crystal and "unwrap(" in crystal,
        "crystal weapon authority lost purpose-authored unwrap/baked atlas pipeline")
uw_path=ROOT/"tools"/"author-underworld-weapons.py"
uw=uw_path.read_text()
require("bpy.ops.wm.open_mainfile" in uw and "derived_from" in uw,
        "Underworld weapon derivatives no longer preserve Crystal weapon source ancestry")
uw_tree=ast.parse(uw)
uw_specs=None
for node in uw_tree.body:
    if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=="SPECS" for t in node.targets):
        uw_specs=ast.literal_eval(node.value);break
require(isinstance(uw_specs,dict) and len(uw_specs)==12,
        "Underworld derivative author must own exactly 12 crystal-chassis weapon models")
uw_manifest=entries.get("underworld-weapon-models",{})
uw_outputs=uw_manifest.get("outputs",[])
require(sum(1 for out in uw_outputs if out.endswith(".model.json"))==12,
        "Underworld weapon manifest must own exactly 12 runtime derivative outputs")
require(all(any(("/"+model_id+".model.json")==out[-len(model_id)-12:] for out in uw_outputs)
            for model_id in (uw_specs or {})),
        "Every Underworld weapon SPECS identity must have a manifest runtime output")
root=(ROOT/"tools"/"author-rootforged-placeables.py").read_text()
require("DETAIL_REVISION=4" in root and "endgame-placeable-r4" in root and "DETAIL_FLOORS=" in root,
        "Rootforged endgame joinery revision 4 regression floor is absent")
stations=(ROOT/"tools"/"author-underworld-stations.py").read_text()
require("DETAIL_REVISION = 2" in stations and "endgame-station-r2" in stations and "DETAIL_FLOORS =" in stations,
        "Underworld station endgame detail revision 2 regression floor is absent")
arm=(ROOT/"tools"/"author-underworld-armour.py").read_text()
require("valheim-player-attach-skin" in arm and "BONE_ORDER=[" in arm,
        "Underworld armour source rig contract is absent")
rootwarren_author=(ROOT/"tools"/"author-rootwarren-dungeon.py").read_text()
rootwarren_rebuild=(ROOT/"tools"/"rebuild-rootwarren-dungeon.ps1").read_text()
production_rebuild=(ROOT/"tools"/"rebuild-underworld-production.ps1").read_text()
require("SPECS = {" in rootwarren_author and rootwarren_author.count('underworld-dungeon-fungal-rootwarren-') >= 1,
        "Rootwarren authoring authority is absent")
require("bake_flora_atlas" in rootwarren_author and "unwrap(" in rootwarren_author,
        "Rootwarren author lost authored UV/atlas production")
require("bpy.ops.uv.smart_project" not in rootwarren_author,
        "Rootwarren author must use the shared deterministic unwrap authority rather than inline smart_project")
require("verify-rootwarren-dungeon" in rootwarren_rebuild and "export-model-assets" in rootwarren_rebuild,
        "Rootwarren rebuild no longer verifies and exports its authored sources")
require('REVISION = "rootwarren-dungeon-r2"' in rootwarren_author,
        "Rootwarren authoring must remain on enclosed-cave revision r2")
require("cave_shell(" in rootwarren_author and '"cavern-shell"' in rootwarren_author and '"passage-shell"' in rootwarren_author,
        "Rootwarren author lost its collidable chamber/passage cave envelopes")
require("rebuild-rootwarren-dungeon.ps1" in production_rebuild and
        "record-rootwarren-generated-assets.py" in production_rebuild and
        "promote-rootwarren-runtime.py" in production_rebuild,
        "One-run Underworld production forge no longer includes Rootwarren generation, provenance, and gated admission")
require("render-rootwarren-review" in production_rebuild and
        "build-rootwarren-review-sheets.py" in production_rebuild and
        "verify-rootwarren-review.py" in production_rebuild,
        "Rootwarren production forge no longer emits/validates its dedicated visual acceptance plates")

wrapper=(ROOT/"tools"/"blender.ps1").read_text()
require("MAGENHEIM_BLENDER" in wrapper,"Blender wrapper cannot accept the Actions executable through environment")
workflow_path=ROOT/".github/workflows/magenheim-production-forge.yml"
if workflow_path.is_file():
    workflow=workflow_path.read_text()
    require("workflow_dispatch:" in workflow and "pull_request:" not in workflow and "\npush:" not in workflow,
            "production forge must remain manual-only")
    require("ref: main" in workflow,"production forge checkout must be pinned to authoritative main")
    require("blender-5.0.0" in workflow,"workflow does not pin Blender 5.0.0")
    require("BepInExPack_Valheim/5.4.2351/" in workflow,"workflow does not pin the current BepInExPack compile reference")
    require("runs-on: windows-2025" in workflow,"production forge must pin the Windows Server 2025 runner label")
    require("python-version: '3.12.10'" in workflow,"production forge must pin Python 3.12.10")
    require("dotnet-version: '8.0.425'" in workflow,"workflow does not pin the approved .NET 8 SDK")
    require("actions/checkout@11d5960a326750d5838078e36cf38b85af677262" in workflow,"checkout action is not commit-pinned")
    require("actions/setup-python@a26af69be951a213d495a4c3e4e4022e16d87065" in workflow,"setup-python action is not commit-pinned")
    require("actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9" in workflow,"setup-dotnet action is not commit-pinned")
    require("actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02" in workflow,"upload-artifact action is not commit-pinned")

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
