#!/usr/bin/env python3
"""Fail-closed source/asset/runtime contract for Blackwater Drowned Vaults."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
CORE=ROOT/"src/Magenheim.Core/Underworld/UnderworldBlackwaterDrownedVaultsCatalog.cs"
DUNGEONS=ROOT/"src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
AUTHOR=ROOT/"tools/author-drowned-vaults-dungeon.py";REBUILD=ROOT/"tools/rebuild-drowned-vaults-dungeon.ps1"
VERIFY=ROOT/"tools/verify-drowned-vaults-dungeon.py";VISUALS=ROOT/"src/Magenheim.Runtime/DrownedVaultRoomVisuals.cs"
ROOMS=ROOT/"src/Magenheim.Runtime/DrownedVaultRoomRegistrar.cs";INTERIOR=ROOT/"src/Magenheim.Runtime/DrownedVaultInteriorBinder.cs"
PASSAGES=ROOT/"src/Magenheim.Runtime/DrownedVaultPassageAssembler.cs";ENCOUNTERS=ROOT/"src/Magenheim.Runtime/DrownedVaultEncounterRuntime.cs"
LOCATION=ROOT/"src/Magenheim.Runtime/DrownedVaultLocationRegistrar.cs";PLUGIN=ROOT/"src/Magenheim.Runtime/MagenheimPlugin.cs"
ENTRANCE=ROOT/"src/Magenheim.Runtime/DrownedVaultEntranceVisuals.cs";TRAVEL=ROOT/"src/Magenheim.Runtime/DrownedVaultTravel.cs"
WATER=ROOT/"src/Magenheim.Runtime/DrownedVaultWaterRuntime.cs";PROMOTE=ROOT/"tools/promote-drowned-vaults-runtime.py"
RECORD=ROOT/"tools/record-drowned-vaults-generated-assets.py";PRODUCTION=ROOT/"tools/rebuild-underworld-production.ps1"
required=(CORE,DUNGEONS,AUTHOR,REBUILD,VERIFY,VISUALS,ROOMS,INTERIOR,PASSAGES,ENCOUNTERS,LOCATION,PLUGIN,ENTRANCE,TRAVEL,WATER,PROMOTE,RECORD)
missing=[str(p.relative_to(ROOT)) for p in required if not p.is_file()]
if missing:raise SystemExit("FAIL Drowned Vault contract missing: "+", ".join(missing))
core=CORE.read_text();dungeons=DUNGEONS.read_text();author=AUTHOR.read_text();verify=VERIFY.read_text();rebuild=REBUILD.read_text()
visuals=VISUALS.read_text();rooms=ROOMS.read_text();interior=INTERIOR.read_text();passages=PASSAGES.read_text()
encounters=ENCOUNTERS.read_text();location=LOCATION.read_text();plugin=PLUGIN.read_text();entrance=ENTRANCE.read_text();travel=TRAVEL.read_text();water=WATER.read_text()
room_suffixes=re.findall(r'Room\("([a-z0-9-]+)"\s*,',core)
if len(room_suffixes)!=16 or len(set(room_suffixes))!=16:raise SystemExit("FAIL Drowned Vault contract: Core must own 16 unique rooms")
expected=set(room_suffixes)|{"passage"}
block=author.split("SPECS={",1)[1].split("}",1)[0]
author_suffixes=set(re.findall(r'^"([a-z0-9-]+)"\s*:',block,re.MULTILINE))
if author_suffixes!=expected:raise SystemExit("FAIL Drowned Vault author SPECS drift")
verify_suffixes=set(re.findall(r'"([a-z0-9-]+)"\s*:\s*\(',verify))
if verify_suffixes!=expected:raise SystemExit("FAIL Drowned Vault verifier SPECS drift")
for suffix in expected:
 if PREFIX+suffix not in rebuild:raise SystemExit("FAIL Drowned Vault rebuild missing "+suffix)
runtime={
 "room status gate":"UnderworldDungeonCatalog.BlackwaterDeep.Status" in rooms and "RuntimeReady" in rooms and "TryGetMissingPayloads" in rooms,
 "location status gate":"UnderworldDungeonCatalog.BlackwaterDeep.Status" in location and "RuntimeReady" in location and "TryGetMissingPayloads" in location,
 "topology":"UnderworldBiomeDungeonPlanner.Build" in interior and "UnderworldBiomeDungeonSpatialPlanner.Build" in interior,
 "routed passages":"UnderworldDrownedVaultPassagePolicy.Resolve" in passages and "spatial.Connections" in passages,
 "native water":"WaterVolume" in water and "AttachPassage" in water and "ValidateDonor" in water,
 "persistent encounters":"DrownedVaultEncounterAuthority" in encounters and "magenheim.drownedvaults.cleared." in encounters,
 "Blackwater resources":"UnderworldTerrainBiome.BlackwaterDeep" in encounters and "resource.PickupPrefab" in encounters,
 "entrance travel":"DrownedVaultEntranceVisuals.Build" in location and "DrownedVaultTravel.Bind" in interior and "TeleportTo" in travel,
 "buried interior":"new Vector3(0f, -38f, -32f)" in entrance,
 "water-snapped location":"SnapToWater = true" in location,
 "room lifecycle":"_drownedVaultRoomRegistrar=new DrownedVaultRoomRegistrar" in plugin and "_drownedVaultRoomRegistrar?.Dispose()" in plugin,
 "location lifecycle":"_drownedVaultLocationRegistrar=new DrownedVaultLocationRegistrar" in plugin and "_drownedVaultLocationRegistrar?.Dispose()" in plugin,
 "pick persistence":"DrownedVaultPickablePersistencePatch" in encounters and "_harmony.PatchAll(typeof(DrownedVaultPickablePersistencePatch))" in plugin,
 "production promotion":"rebuild-drowned-vaults-dungeon.ps1" in PRODUCTION.read_text() and "promote-drowned-vaults-runtime.py" in PRODUCTION.read_text(),
}
bad=[k for k,v in runtime.items() if not v]
if bad:raise SystemExit("FAIL Drowned Vault runtime authority: "+", ".join(bad))
planned=re.search(r'BlackwaterDeep\s*\{\s*get;\s*\}\s*=\s*Planned\(',dungeons,re.MULTILINE) is not None
ready=re.search(r'BlackwaterDeep\s*\{\s*get;\s*\}\s*=\s*Ready\(',dungeons,re.MULTILINE) is not None
if planned==ready:raise SystemExit("FAIL Drowned Vault catalog state ambiguous")
def family(directory,suffix):
 return sorted((ROOT/"assets/models"/directory).glob(PREFIX+"*"+suffix))
counts=(len(family("source",".blend")),len(family("glb",".glb")),len(family("runtime",".model.json")))
if any(x not in (0,17) for x in counts) or len(set(counts))!=1:raise SystemExit(f"FAIL Drowned Vault partial family {counts}")
if ready and counts[0]!=17:raise SystemExit("FAIL Drowned Vault RuntimeReady without complete assets")
state="PLANNED / forge pending" if counts[0]==0 else "PLANNED / forged awaiting promotion" if planned else "RUNTIME READY"
print("PASS Drowned Vault production contract:",f"16 rooms + passage; assets={counts}; state={state}")
