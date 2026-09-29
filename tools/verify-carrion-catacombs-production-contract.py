#!/usr/bin/env python3
"""Fail-closed source/asset/runtime contract for Great Decay Carrion Catacombs."""
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
CORE=ROOT/"src/Magenheim.Core/Underworld/UnderworldGreatDecayCarrionCatacombsCatalog.cs"
POLICY=ROOT/"src/Magenheim.Core/Underworld/UnderworldCarrionCatacombsContaminationPolicy.cs"
DUNGEONS=ROOT/"src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
AUTHOR=ROOT/"tools/author-carrion-catacombs-dungeon.py"
REBUILD=ROOT/"tools/rebuild-carrion-catacombs-dungeon.ps1"
VERIFY=ROOT/"tools/verify-carrion-catacombs-dungeon.py"
VISUALS=ROOT/"src/Magenheim.Runtime/CarrionCatacombsRoomVisuals.cs"
ROOMS=ROOT/"src/Magenheim.Runtime/CarrionCatacombsRoomRegistrar.cs"
CONTAMINATION=ROOT/"src/Magenheim.Runtime/CarrionCatacombsContaminationRuntime.cs"
PASSAGES=ROOT/"src/Magenheim.Runtime/CarrionCatacombsPassageAssembler.cs"
INTERIOR=ROOT/"src/Magenheim.Runtime/CarrionCatacombsInteriorBinder.cs"
ENCOUNTERS=ROOT/"src/Magenheim.Runtime/CarrionCatacombsEncounterRuntime.cs"
LOCATION=ROOT/"src/Magenheim.Runtime/CarrionCatacombsLocationRegistrar.cs"
ENTRANCE=ROOT/"src/Magenheim.Runtime/CarrionCatacombsEntranceVisuals.cs"
TRAVEL=ROOT/"src/Magenheim.Runtime/CarrionCatacombsTravel.cs"
MITIGATION=ROOT/"src/Magenheim.Runtime/UnderworldWeatherMitigationRuntime.cs"
PLUGIN=ROOT/"src/Magenheim.Runtime/MagenheimPlugin.cs"
PROMOTE=ROOT/"tools/promote-carrion-catacombs-runtime.py"
RECORD=ROOT/"tools/record-carrion-catacombs-generated-assets.py"
PRODUCTION=ROOT/"tools/rebuild-underworld-production.ps1"

PREFIX="underworld-dungeon-great-decay-carrion-catacombs-"
required=(CORE,POLICY,DUNGEONS,AUTHOR,REBUILD,VERIFY,VISUALS,ROOMS,CONTAMINATION,PASSAGES,
          INTERIOR,ENCOUNTERS,LOCATION,ENTRANCE,TRAVEL,MITIGATION,PLUGIN,PROMOTE,RECORD)
missing=[str(p.relative_to(ROOT)) for p in required if not p.is_file()]
if missing:
    raise SystemExit("FAIL Carrion Catacombs contract missing: "+", ".join(missing))

core=CORE.read_text();policy=POLICY.read_text();dungeons=DUNGEONS.read_text()
author=AUTHOR.read_text();verify=VERIFY.read_text();rebuild=REBUILD.read_text()
visuals=VISUALS.read_text();rooms=ROOMS.read_text();contamination=CONTAMINATION.read_text()
passages=PASSAGES.read_text();interior=INTERIOR.read_text();encounters=ENCOUNTERS.read_text()
location=LOCATION.read_text();entrance=ENTRANCE.read_text();travel=TRAVEL.read_text()
mitigation=MITIGATION.read_text();plugin=PLUGIN.read_text();production=PRODUCTION.read_text()

suffixes=re.findall(r'Room\("([a-z0-9-]+)"\s*,',core)
if len(suffixes)!=16 or len(set(suffixes))!=16:
    raise SystemExit("FAIL Carrion Catacombs Core must own 16 unique rooms")
expected=set(suffixes)|{"passage"}

block=author.split("SPECS={",1)[1].split("}",1)[0]
if set(re.findall(r'^"([a-z0-9-]+)"\s*:',block,re.MULTILINE))!=expected:
    raise SystemExit("FAIL Carrion Catacombs author SPECS drift")
if set(re.findall(r'"([a-z0-9-]+)"\s*:\s*\(',verify))!=expected:
    raise SystemExit("FAIL Carrion Catacombs verifier SPECS drift")
for suffix in expected:
    if PREFIX+suffix not in rebuild:
        raise SystemExit("FAIL Carrion Catacombs rebuild missing "+suffix)

runtime={
    "room status gate":
        "UnderworldDungeonCatalog.GreatDecay.Status" in rooms and
        "RuntimeReady" in rooms and "TryGetMissingPayloads" in rooms,
    "location status gate":
        "UnderworldDungeonCatalog.GreatDecay.Status" in location and
        "RuntimeReady" in location and "TryGetMissingPayloads" in location,
    "deterministic topology":
        "UnderworldBiomeDungeonPlanner.Build" in interior and
        "UnderworldBiomeDungeonSpatialPlanner.Build" in interior,
    "contamination authority":
        "UnderworldCarrionCatacombsContaminationPolicy.ResolvePassage" in passages and
        "CarrionCatacombsContaminationRuntime.AttachPassage" in passages and
        "UnderworldLocalWeatherOverride.Refresh" in contamination,
    "Defiant progression":
        "MatchingDeepBoonResistance" in mitigation and
        '"defiant_flesh"' in mitigation and
        "DefiantCenserSuppression" in mitigation and
        "HasActiveCenserNearby" in mitigation,
    "persistent encounters":
        "CarrionCatacombsEncounterAuthority" in encounters and
        "magenheim.carrioncatacombs.cleared." in encounters,
    "Great Decay resources":
        "UnderworldTerrainBiome.GreatDecay" in encounters and
        "resource.PickupPrefab" in encounters,
    "room-role encounters":
        "rotling-warrens" in encounters and
        "graft-warden-hall" in encounters and
        "corpse-orchard-antechamber" in encounters and
        "UnderworldDungeonRoomRole.Hazard" in encounters,
    "entrance travel":
        "CarrionCatacombsEntranceVisuals.Build" in location and
        "CarrionCatacombsTravel.Bind" in interior and
        "TeleportTo" in travel,
    "buried interior":
        "new Vector3(0f,-38f,-32f)" in entrance or
        "new Vector3(0f, -38f, -32f)" in entrance,
    "room lifecycle":
        "_carrionCatacombsRoomRegistrar=new CarrionCatacombsRoomRegistrar" in plugin and
        "_carrionCatacombsRoomRegistrar?.Dispose()" in plugin,
    "location lifecycle":
        "_carrionCatacombsLocationRegistrar=new CarrionCatacombsLocationRegistrar" in plugin and
        "_carrionCatacombsLocationRegistrar?.Dispose()" in plugin,
    "pick persistence":
        "CarrionCatacombsPickablePersistencePatch" in encounters and
        "_harmony.PatchAll(typeof(CarrionCatacombsPickablePersistencePatch))" in plugin,
    "production promotion":
        "rebuild-carrion-catacombs-dungeon.ps1" in production and
        "record-carrion-catacombs-generated-assets.py" in production and
        "promote-carrion-catacombs-runtime.py" in production,
}
bad=[name for name,ok in runtime.items() if not ok]
if bad:
    raise SystemExit("FAIL Carrion Catacombs runtime authority: "+", ".join(bad))

if "BlackBloom" not in policy or "UnderworldAtmosphereEvent.None" not in policy:
    raise SystemExit("FAIL Carrion Catacombs contamination policy lost Great Decay atmosphere authority")

planned=re.search(
    r'GreatDecay\s*\{\s*get;\s*\}\s*=\s*Planned\(',
    dungeons,re.MULTILINE) is not None
ready=re.search(
    r'GreatDecay\s*\{\s*get;\s*\}\s*=\s*Ready\(',
    dungeons,re.MULTILINE) is not None
if planned==ready:
    raise SystemExit("FAIL Carrion Catacombs catalog state ambiguous")

def family(directory,suffix):
    return sorted((ROOT/"assets/models"/directory).glob(PREFIX+"*"+suffix))

counts=(len(family("source",".blend")),
        len(family("glb",".glb")),
        len(family("runtime",".model.json")))
if any(count not in (0,17) for count in counts) or len(set(counts))!=1:
    raise SystemExit(f"FAIL Carrion Catacombs partial generated family {counts}")
if ready and counts[0]!=17:
    raise SystemExit("FAIL Carrion Catacombs RuntimeReady without complete assets")

state=("PLANNED / forge pending" if counts[0]==0 else
       "PLANNED / forged awaiting promotion" if planned else
       "RUNTIME READY")
print("PASS Carrion Catacombs production contract:",
      f"16 rooms + passage; assets={counts}; state={state}")
