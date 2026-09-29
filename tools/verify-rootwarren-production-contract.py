#!/usr/bin/env python3
"""Fail-closed source/asset contract for the Fungal Rootwarren dungeon.

Valid states:
  * Planned + zero generated model families: source/runtime architecture exists, forge not run yet.
  * Planned + all 17 generated model families: forge output exists and awaits review/promotion.
  * RuntimeReady + all 17 generated model families: runtime admission may register.

Any partial asset family, manifest drift, missing runtime binding, or RuntimeReady-without-assets
is a release failure.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/Magenheim.Core/Underworld/UnderworldFungalRootwarrenCatalog.cs"
DUNGEONS = ROOT / "src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs"
AUTHOR = ROOT / "tools/author-rootwarren-dungeon.py"
REBUILD = ROOT / "tools/rebuild-rootwarren-dungeon.ps1"
VERIFY = ROOT / "tools/verify-rootwarren-dungeon.py"
VISUALS = ROOT / "src/Magenheim.Runtime/RootwarrenRoomVisuals.cs"
ROOMS = ROOT / "src/Magenheim.Runtime/RootwarrenRoomRegistrar.cs"
INTERIOR = ROOT / "src/Magenheim.Runtime/RootwarrenInteriorBinder.cs"
PASSAGES = ROOT / "src/Magenheim.Runtime/RootwarrenPassageAssembler.cs"
ENCOUNTERS = ROOT / "src/Magenheim.Runtime/RootwarrenEncounterRuntime.cs"
LOCATION = ROOT / "src/Magenheim.Runtime/RootwarrenLocationRegistrar.cs"
PLUGIN = ROOT / "src/Magenheim.Runtime/MagenheimPlugin.cs"
ENTRANCE = ROOT / "src/Magenheim.Runtime/RootwarrenEntranceVisuals.cs"
TRAVEL = ROOT / "src/Magenheim.Runtime/RootwarrenTravel.cs"
PROMOTE = ROOT / "tools/promote-rootwarren-runtime.py"

PREFIX = "underworld-dungeon-fungal-rootwarren-"
PASSAGE = "passage"

required_files = (
    CORE, DUNGEONS, AUTHOR, REBUILD, VERIFY, VISUALS, ROOMS,
    INTERIOR, PASSAGES, ENCOUNTERS, LOCATION, PLUGIN, ENTRANCE, TRAVEL, PROMOTE,
)
missing_files = [str(path.relative_to(ROOT)) for path in required_files if not path.is_file()]
if missing_files:
    raise SystemExit("FAIL Rootwarren contract: missing source files: " + ", ".join(missing_files))

core = CORE.read_text(encoding="utf-8")
author = AUTHOR.read_text(encoding="utf-8")
rebuild = REBUILD.read_text(encoding="utf-8")
verify = VERIFY.read_text(encoding="utf-8")
dungeons = DUNGEONS.read_text(encoding="utf-8")
visuals = VISUALS.read_text(encoding="utf-8")
rooms = ROOMS.read_text(encoding="utf-8")
interior = INTERIOR.read_text(encoding="utf-8")
passages = PASSAGES.read_text(encoding="utf-8")
encounters = ENCOUNTERS.read_text(encoding="utf-8")
location = LOCATION.read_text(encoding="utf-8")
plugin = PLUGIN.read_text(encoding="utf-8")
entrance = ENTRANCE.read_text(encoding="utf-8")
travel = TRAVEL.read_text(encoding="utf-8")

room_suffixes = re.findall(r'Room\("([a-z0-9-]+)"\s*,', core)
if len(room_suffixes) != 16 or len(set(room_suffixes)) != 16:
    raise SystemExit(
        f"FAIL Rootwarren contract: Core must own 16 unique room suffixes, found {len(room_suffixes)}")
expected = set(room_suffixes) | {PASSAGE}

author_block = author.split("SPECS = {", 1)[1].split("}", 1)[0]
author_suffixes = set(re.findall(r'^\s*"([a-z0-9-]+)"\s*:', author_block, re.MULTILINE))
if author_suffixes != expected:
    raise SystemExit(
        "FAIL Rootwarren contract: author SPECS drift: missing=" +
        ",".join(sorted(expected - author_suffixes)) + " extra=" +
        ",".join(sorted(author_suffixes - expected)))

rebuild_suffixes = set(re.findall(r"'([a-z0-9-]+)'", rebuild))
if not expected.issubset(rebuild_suffixes):
    raise SystemExit(
        "FAIL Rootwarren contract: rebuild script is missing: " +
        ",".join(sorted(expected - rebuild_suffixes)))

verify_specs = set(re.findall(r'"([a-z0-9-]+)"\s*:\s*\(', verify))
if verify_specs != expected:
    raise SystemExit(
        "FAIL Rootwarren contract: Blender verifier SPECS drift: missing=" +
        ",".join(sorted(expected - verify_specs)) + " extra=" +
        ",".join(sorted(verify_specs - expected)))

for suffix in expected:
    model_id = PREFIX + suffix
    if model_id not in author:
        raise SystemExit("FAIL Rootwarren contract: author lost model id " + model_id)

if f'PassageModelId = "{PREFIX}{PASSAGE}"' not in visuals:
    raise SystemExit("FAIL Rootwarren contract: runtime passage model identity drifted")

runtime_contracts = {
    "room registrar status gate": (
        "UnderworldDungeonCatalog.FungalForest.Status" in rooms and
        "UnderworldDungeonStatus.RuntimeReady" in rooms and
        "TryGetMissingPayloads" in rooms
    ),
    "location registrar status gate": (
        "UnderworldDungeonCatalog.FungalForest.Status" in location and
        "UnderworldDungeonStatus.RuntimeReady" in location and
        "TryGetMissingPayloads" in location
    ),
    "deterministic topology": (
        "UnderworldBiomeDungeonPlanner.Build" in interior and
        "UnderworldBiomeDungeonSpatialPlanner.Build" in interior
    ),
    "physical routed passages": (
        "spatial.Connections" in passages and
        "Waypoints" in passages
    ),
    "persistent encounters": (
        "RootwarrenEncounterAuthority" in encounters and
        "magenheim.rootwarren.cleared." in encounters
    ),
    "native resource pickups": (
        "UnderworldResourceCatalog.All" in encounters and
        "resource.PickupPrefab" in encounters
    ),
    "authored entrance lifecycle": (
        "RootwarrenEntranceVisuals.Build" in location and
        "RootwarrenTravel.AttachEntrancePortal" in interior and
        "RootwarrenTravel.Bind" in interior and
        "EntranceRoomId" in interior
    ),
    "same-instance return portal": (
        "RootwarrenTravelPortal" in travel and
        "TeleportTo" in travel and
        "Fungal Forest" in travel
    ),
    "server-authority population retry": (
        "TryPopulate()" in interior and
        "authority.HasAuthority" in interior and
        "_populationApplied" in interior
    ),
    "fungal interior environment": (
        "UnderworldWeatherRuntime.EnvironmentName" in interior and
        "UnderworldTerrainBiome.FungalForest" in interior
    ),
    "plugin room lifecycle": (
        "_rootwarrenRoomRegistrar=new RootwarrenRoomRegistrar" in plugin and
        "_rootwarrenRoomRegistrar?.Dispose()" in plugin
    ),
    "plugin location lifecycle": (
        "_rootwarrenLocationRegistrar=new RootwarrenLocationRegistrar" in plugin and
        "_rootwarrenLocationRegistrar?.Dispose()" in plugin
    ),
}
bad_runtime = [name for name, ok in runtime_contracts.items() if not ok]
if bad_runtime:
    raise SystemExit(
        "FAIL Rootwarren contract: runtime authority missing: " + ", ".join(bad_runtime))

planned = re.search(
    r'FungalForest\s*\{\s*get;\s*\}\s*=\s*Planned\(',
    dungeons,
    re.MULTILINE,
) is not None
ready = re.search(
    r'FungalForest\s*\{\s*get;\s*\}\s*=\s*Ready\(',
    dungeons,
    re.MULTILINE,
) is not None
if planned == ready:
    raise SystemExit(
        "FAIL Rootwarren contract: cannot determine exactly one Planned/RuntimeReady Fungal state")

def family(ext_dir: str, suffix: str):
    directory = ROOT / "assets/models" / ext_dir
    extension = ".model.json" if ext_dir == "runtime" else suffix
    return sorted(directory.glob(PREFIX + "*" + extension))

sources = family("source", ".blend")
glbs = family("glb", ".glb")
runtime = family("runtime", ".model.json")
counts = (len(sources), len(glbs), len(runtime))
if any(count not in (0, 17) for count in counts) or len(set(counts)) != 1:
    raise SystemExit(
        "FAIL Rootwarren contract: partial generated asset family " +
        f"source/glb/runtime={counts}; expected 0/0/0 or 17/17/17")

if counts[0] == 17:
    actual = {path.name.removeprefix(PREFIX).removesuffix(".blend") for path in sources}
    if actual != expected:
        raise SystemExit(
            "FAIL Rootwarren contract: generated source identities drift: missing=" +
            ",".join(sorted(expected - actual)) + " extra=" +
            ",".join(sorted(actual - expected)))

if ready and counts[0] != 17:
    raise SystemExit(
        "FAIL Rootwarren contract: RuntimeReady is forbidden until all 17 source/GLB/runtime payloads exist")

state = (
    "PLANNED / forge pending"
    if counts[0] == 0
    else "PLANNED / forged awaiting promotion"
    if planned
    else "RUNTIME READY"
)
print(
    "PASS Rootwarren production contract:",
    f"16 rooms + passage; assets source/glb/runtime={counts}; state={state}"
)
