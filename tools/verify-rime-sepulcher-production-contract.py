#!/usr/bin/env python3
"""Fail-closed source/asset/runtime contract for the Frozen Rime Sepulcher."""
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
CORE=ROOT/"src/Magenheim.Core/Underworld/UnderworldFrozenRimeSepulcherCatalog.cs"
POLICY=ROOT/"src/Magenheim.Core/Underworld/UnderworldRimeSepulcherExposurePolicy.cs"
DUNGEONS=ROOT/"src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
AUTHOR=ROOT/"tools/author-rime-sepulcher-dungeon.py"
REBUILD=ROOT/"tools/rebuild-rime-sepulcher-dungeon.ps1"
VERIFY=ROOT/"tools/verify-rime-sepulcher-dungeon.py"
VISUALS=ROOT/"src/Magenheim.Runtime/RimeSepulcherRoomVisuals.cs"
ROOMS=ROOT/"src/Magenheim.Runtime/RimeSepulcherRoomRegistrar.cs"
EXPOSURE=ROOT/"src/Magenheim.Runtime/RimeSepulcherExposureRuntime.cs"
PASSAGES=ROOT/"src/Magenheim.Runtime/RimeSepulcherPassageAssembler.cs"
INTERIOR=ROOT/"src/Magenheim.Runtime/RimeSepulcherInteriorBinder.cs"
ENCOUNTERS=ROOT/"src/Magenheim.Runtime/RimeSepulcherEncounterRuntime.cs"
LOCATION=ROOT/"src/Magenheim.Runtime/RimeSepulcherLocationRegistrar.cs"
ENTRANCE=ROOT/"src/Magenheim.Runtime/RimeSepulcherEntranceVisuals.cs"
TRAVEL=ROOT/"src/Magenheim.Runtime/RimeSepulcherTravel.cs"
PLUGIN=ROOT/"src/Magenheim.Runtime/MagenheimPlugin.cs"
PROMOTE=ROOT/"tools/promote-rime-sepulcher-runtime.py"
RECORD=ROOT/"tools/record-rime-sepulcher-generated-assets.py"
PRODUCTION=ROOT/"tools/rebuild-underworld-production.ps1"

PREFIX="underworld-dungeon-frozen-rime-sepulcher-"
required=(CORE,POLICY,DUNGEONS,AUTHOR,REBUILD,VERIFY,VISUALS,ROOMS,EXPOSURE,PASSAGES,
          INTERIOR,ENCOUNTERS,LOCATION,ENTRANCE,TRAVEL,PLUGIN,PROMOTE,RECORD)
missing=[str(p.relative_to(ROOT)) for p in required if not p.is_file()]
if missing:
    raise SystemExit("FAIL Rime Sepulcher contract missing: "+", ".join(missing))

core=CORE.read_text()
policy=POLICY.read_text()
dungeons=DUNGEONS.read_text()
author=AUTHOR.read_text()
verify=VERIFY.read_text()
rebuild=REBUILD.read_text()
visuals=VISUALS.read_text()
rooms=ROOMS.read_text()
exposure=EXPOSURE.read_text()
passages=PASSAGES.read_text()
interior=INTERIOR.read_text()
encounters=ENCOUNTERS.read_text()
location=LOCATION.read_text()
entrance=ENTRANCE.read_text()
travel=TRAVEL.read_text()
plugin=PLUGIN.read_text()
production=PRODUCTION.read_text()

suffixes=re.findall(r'Room\("([a-z0-9-]+)"\s*,',core)
if len(suffixes)!=16 or len(set(suffixes))!=16:
    raise SystemExit("FAIL Rime Sepulcher Core must own 16 unique rooms")
expected=set(suffixes)|{"passage"}

block=author.split("SPECS={",1)[1].split("}",1)[0]
author_suffixes=set(re.findall(r'^"([a-z0-9-]+)"\s*:',block,re.MULTILINE))
if author_suffixes!=expected:
    raise SystemExit("FAIL Rime Sepulcher author SPECS drift")
verify_suffixes=set(re.findall(r'"([a-z0-9-]+)"\s*:\s*\(',verify))
if verify_suffixes!=expected:
    raise SystemExit("FAIL Rime Sepulcher verifier SPECS drift")
for suffix in expected:
    if PREFIX+suffix not in rebuild:
        raise SystemExit("FAIL Rime Sepulcher rebuild missing "+suffix)

runtime={
    "room status gate":
        "UnderworldDungeonCatalog.FrozenCaverns.Status" in rooms and
        "RuntimeReady" in rooms and "TryGetMissingPayloads" in rooms,
    "location status gate":
        "UnderworldDungeonCatalog.FrozenCaverns.Status" in location and
        "RuntimeReady" in location and "TryGetMissingPayloads" in location,
    "deterministic topology":
        "UnderworldBiomeDungeonPlanner.Build" in interior and
        "UnderworldBiomeDungeonSpatialPlanner.Build" in interior,
    "route exposure":
        "UnderworldRimeSepulcherExposurePolicy.ResolvePassage" in passages and
        "RimeSepulcherExposureRuntime.AttachPassage" in passages and
        "UnderworldLocalWeatherOverride.Refresh" in exposure,
    "persistent encounters":
        "RimeSepulcherEncounterAuthority" in encounters and
        "magenheim.rimesepulcher.cleared." in encounters,
    "Frozen resources":
        "UnderworldTerrainBiome.FrozenCaverns" in encounters and
        "resource.PickupPrefab" in encounters,
    "room-role encounters":
        "UnderworldDungeonRoomRole.Hazard" in encounters and
        "UnderworldDungeonRoomRole.Encounter" in encounters and
        "frost-tick-niche" in encounters and "cryolith-guard" in encounters,
    "entrance travel":
        "RimeSepulcherEntranceVisuals.Build" in location and
        "RimeSepulcherTravel.Bind" in interior and
        "TeleportTo" in travel,
    "buried interior":
        "new Vector3(0f, -34f, -30f)" in entrance,
    "room lifecycle":
        "_rimeSepulcherRoomRegistrar=new RimeSepulcherRoomRegistrar" in plugin and
        "_rimeSepulcherRoomRegistrar?.Dispose()" in plugin,
    "location lifecycle":
        "_rimeSepulcherLocationRegistrar=new RimeSepulcherLocationRegistrar" in plugin and
        "_rimeSepulcherLocationRegistrar?.Dispose()" in plugin,
    "pick persistence":
        "RimeSepulcherPickablePersistencePatch" in encounters and
        "_harmony.PatchAll(typeof(RimeSepulcherPickablePersistencePatch))" in plugin,
    "production promotion":
        "rebuild-rime-sepulcher-dungeon.ps1" in production and
        "record-rime-sepulcher-generated-assets.py" in production and
        "promote-rime-sepulcher-runtime.py" in production,
}
bad=[name for name,ok in runtime.items() if not ok]
if bad:
    raise SystemExit("FAIL Rime Sepulcher runtime authority: "+", ".join(bad))

if "Whiteout" not in policy or "DeepFog" not in policy:
    raise SystemExit("FAIL Rime Sepulcher exposure policy lost Frozen weather authority")

planned=re.search(
    r'FrozenCaverns\s*\{\s*get;\s*\}\s*=\s*Planned\(',
    dungeons,re.MULTILINE) is not None
ready=re.search(
    r'FrozenCaverns\s*\{\s*get;\s*\}\s*=\s*Ready\(',
    dungeons,re.MULTILINE) is not None
if planned==ready:
    raise SystemExit("FAIL Rime Sepulcher catalog state ambiguous")

def family(directory,suffix):
    return sorted((ROOT/"assets/models"/directory).glob(PREFIX+"*"+suffix))

counts=(len(family("source",".blend")),
        len(family("glb",".glb")),
        len(family("runtime",".model.json")))
if any(count not in (0,17) for count in counts) or len(set(counts))!=1:
    raise SystemExit(f"FAIL Rime Sepulcher partial generated family {counts}")
if ready and counts[0]!=17:
    raise SystemExit("FAIL Rime Sepulcher RuntimeReady without complete assets")

state=("PLANNED / forge pending" if counts[0]==0 else
       "PLANNED / forged awaiting promotion" if planned else
       "RUNTIME READY")
print("PASS Rime Sepulcher production contract:",
      f"16 rooms + passage; assets={counts}; state={state}")
