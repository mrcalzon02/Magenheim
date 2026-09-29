#!/usr/bin/env python3
"""Fail-closed source/asset/runtime contract for Sulfur Cinderworks."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
CORE=ROOT/"src/Magenheim.Core/Underworld/UnderworldSulfurCinderworksCatalog.cs"
POLICY=ROOT/"src/Magenheim.Core/Underworld/UnderworldCinderworksThermalRoutePolicy.cs"
DUNGEONS=ROOT/"src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
AUTHOR=ROOT/"tools/author-cinderworks-dungeon.py";REBUILD=ROOT/"tools/rebuild-cinderworks-dungeon.ps1";VERIFY=ROOT/"tools/verify-cinderworks-dungeon.py"
VISUALS=ROOT/"src/Magenheim.Runtime/CinderworksRoomVisuals.cs";ROOMS=ROOT/"src/Magenheim.Runtime/CinderworksRoomRegistrar.cs"
THERMAL=ROOT/"src/Magenheim.Runtime/CinderworksThermalRuntime.cs";PASSAGES=ROOT/"src/Magenheim.Runtime/CinderworksPassageAssembler.cs"
INTERIOR=ROOT/"src/Magenheim.Runtime/CinderworksInteriorBinder.cs";ENCOUNTERS=ROOT/"src/Magenheim.Runtime/CinderworksEncounterRuntime.cs"
LOCATION=ROOT/"src/Magenheim.Runtime/CinderworksLocationRegistrar.cs";ENTRANCE=ROOT/"src/Magenheim.Runtime/CinderworksEntranceVisuals.cs"
TRAVEL=ROOT/"src/Magenheim.Runtime/CinderworksTravel.cs";PLUGIN=ROOT/"src/Magenheim.Runtime/MagenheimPlugin.cs"
PROMOTE=ROOT/"tools/promote-cinderworks-runtime.py";RECORD=ROOT/"tools/record-cinderworks-generated-assets.py"
PRODUCTION=ROOT/"tools/rebuild-underworld-production.ps1"
required=(CORE,POLICY,DUNGEONS,AUTHOR,REBUILD,VERIFY,VISUALS,ROOMS,THERMAL,PASSAGES,INTERIOR,ENCOUNTERS,LOCATION,ENTRANCE,TRAVEL,PLUGIN,PROMOTE,RECORD)
missing=[str(p.relative_to(ROOT)) for p in required if not p.is_file()]
if missing:raise SystemExit("FAIL Cinderworks contract missing: "+", ".join(missing))
core=CORE.read_text();policy=POLICY.read_text();dungeons=DUNGEONS.read_text();author=AUTHOR.read_text();verify=VERIFY.read_text();rebuild=REBUILD.read_text()
visuals=VISUALS.read_text();rooms=ROOMS.read_text();thermal=THERMAL.read_text();passages=PASSAGES.read_text();interior=INTERIOR.read_text()
encounters=ENCOUNTERS.read_text();location=LOCATION.read_text();entrance=ENTRANCE.read_text();travel=TRAVEL.read_text();plugin=PLUGIN.read_text()
suffixes=re.findall(r'Room\("([a-z0-9-]+)"\s*,',core)
if len(suffixes)!=16 or len(set(suffixes))!=16:raise SystemExit("FAIL Cinderworks Core must own 16 unique rooms")
expected=set(suffixes)|{"passage"}
block=author.split("SPECS={",1)[1].split("}",1)[0]
if set(re.findall(r'^"([a-z0-9-]+)"\s*:',block,re.MULTILINE))!=expected:raise SystemExit("FAIL Cinderworks author SPECS drift")
if set(re.findall(r'"([a-z0-9-]+)"\s*:\s*\(',verify))!=expected:raise SystemExit("FAIL Cinderworks verifier SPECS drift")
for suffix in expected:
 if PREFIX+suffix not in rebuild:raise SystemExit("FAIL Cinderworks rebuild missing "+suffix)
runtime={
 "room status gate":"UnderworldDungeonCatalog.SulfurousWastes.Status" in rooms and "RuntimeReady" in rooms and "TryGetMissingPayloads" in rooms,
 "location status gate":"UnderworldDungeonCatalog.SulfurousWastes.Status" in location and "RuntimeReady" in location and "TryGetMissingPayloads" in location,
 "topology":"UnderworldBiomeDungeonPlanner.Build" in interior and "UnderworldBiomeDungeonSpatialPlanner.Build" in interior,
 "thermal authority":"UnderworldGeothermalHazardVolume" in thermal and "UnderworldCinderworksThermalRoutePolicy" in thermal,
 "Furnace Blood path":"UnderworldGeothermalHazard.VentField" in policy and "UnderworldGeothermalHazard.LavaChannel" in policy,
 "passage heat":"ResolvePassage" in passages and "AttachPassage" in passages,
 "persistent encounters":"CinderworksEncounterAuthority" in encounters and "magenheim.cinderworks.cleared." in encounters,
 "Sulfur resources":"UnderworldTerrainBiome.SulfurousWastes" in encounters and "resource.PickupPrefab" in encounters,
 "entrance travel":"CinderworksEntranceVisuals.Build" in location and "CinderworksTravel.Bind" in interior and "TeleportTo" in travel,
 "buried interior":"new Vector3(0f, -36f, -30f)" in entrance,
 "room lifecycle":"_cinderworksRoomRegistrar=new CinderworksRoomRegistrar" in plugin and "_cinderworksRoomRegistrar?.Dispose()" in plugin,
 "location lifecycle":"_cinderworksLocationRegistrar=new CinderworksLocationRegistrar" in plugin and "_cinderworksLocationRegistrar?.Dispose()" in plugin,
 "pick persistence":"CinderworksPickablePersistencePatch" in encounters and "_harmony.PatchAll(typeof(CinderworksPickablePersistencePatch))" in plugin,
 "production promotion":"rebuild-cinderworks-dungeon.ps1" in PRODUCTION.read_text() and "promote-cinderworks-runtime.py" in PRODUCTION.read_text(),
}
bad=[k for k,v in runtime.items() if not v]
if bad:raise SystemExit("FAIL Cinderworks runtime authority: "+", ".join(bad))
planned=re.search(r'SulfurousWastes\s*\{\s*get;\s*\}\s*=\s*Planned\(',dungeons,re.MULTILINE) is not None
ready=re.search(r'SulfurousWastes\s*\{\s*get;\s*\}\s*=\s*Ready\(',dungeons,re.MULTILINE) is not None
if planned==ready:raise SystemExit("FAIL Cinderworks catalog state ambiguous")
def family(directory,suffix):return sorted((ROOT/"assets/models"/directory).glob(PREFIX+"*"+suffix))
counts=(len(family("source",".blend")),len(family("glb",".glb")),len(family("runtime",".model.json")))
if any(x not in (0,17) for x in counts) or len(set(counts))!=1:raise SystemExit(f"FAIL Cinderworks partial family {counts}")
if ready and counts[0]!=17:raise SystemExit("FAIL Cinderworks RuntimeReady without complete assets")
state="PLANNED / forge pending" if counts[0]==0 else "PLANNED / forged awaiting promotion" if planned else "RUNTIME READY"
print("PASS Cinderworks production contract:",f"16 rooms + passage; assets={counts}; state={state}")
