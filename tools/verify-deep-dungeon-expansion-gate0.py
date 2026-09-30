#!/usr/bin/env python3
"""Fail-closed architecture freeze for Deep Dungeon Expansion Gate 0."""
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
fail=[]

def require(ok,msg):
    if not ok:
        fail.append(msg)

reuse=(ROOT/"src/Magenheim.Core/Underworld/UnderworldVanillaDungeonReuseCatalog.cs").read_text()
catalog=(ROOT/"src/Magenheim.Core/Underworld/UnderworldDungeonCatalog.cs").read_text()
runtime=(ROOT/"src/Magenheim.Runtime/UnderworldVanillaDungeonRegistrar.cs").read_text()
plugin=(ROOT/"src/Magenheim.Runtime/MagenheimPlugin.cs").read_text()
production=(ROOT/"tools/rebuild-underworld-production.ps1").read_text()
readiness=(ROOT/"tools/verify-underworld-production-readiness.py").read_text()
deep_fracture=(ROOT/"src/Magenheim.Runtime/DeepFractureLocationRegistrar.cs").read_text()
candidate=(ROOT/"src/Magenheim.Runtime/UnderworldVanillaDungeonCandidatePolicy.cs").read_text()
bridge=(ROOT/"src/Magenheim.Runtime/UnderworldWorldgenContentBridge.cs").read_text()
placement=(ROOT/"src/Magenheim.Runtime/UnderworldDungeonPlacementRuntime.cs").read_text()
synchronizer=(ROOT/"src/Magenheim.Runtime/DefinitionAuthoritySynchronizer.cs").read_text()
gameplay_fingerprint=(ROOT/"src/Magenheim.Core/Socketing/GameplayAuthorityFingerprint.cs").read_text()
plan=(ROOT/"docs/DEEP_DUNGEON_EXPANSION.md").read_text()

for token in (
    "# Deep Dungeon Expansion",
    "## DDE-00 — Architecture freeze",
    "## DDE-13 — All-family runtime admission",
    "## DDE-14 — Final regression against Deep Fracture",
    "1.5x linear room scale",
    "3.5x donor room-count target",
    "DG_ForestCrypt",
    "DG_SunkenCrypt",
    "DG_DvergrTown",
    "DG_Cave",
    "DG_Hole",
    "Deep Fracture remains fully Magenheim-owned architecture",
):
    require(token in plan,"DDE-00 durable plan authority lost required contract text: "+token)

require("MinimumLinearRoomScale = 1.5d" in reuse,
        "DDE-00 scale floor drifted below the 1.5x authority")
require("MinimumRoomCountMultiplier = 3.5d" in reuse,
        "DDE-00 room-count floor drifted below the 3.5x authority")
require("AllowMagenheimAuthoredRoomInjection: false" in reuse,
        "DDE-00 ordinary donor dungeons no longer prohibit bespoke room injection")
for donor in ("DG_ForestCrypt","DG_SunkenCrypt","DG_DvergrTown","DG_Cave","DG_Hole"):
    require(donor in reuse,"DDE-00 donor mapping disappeared: "+donor)

# All five ordinary expanded-vanilla families are production-admitted together.
for name in ("FungalForest","BlackwaterDeep","SulfurousWastes","FrozenCaverns","GreatDecay"):
    match=re.search(rf'public static UnderworldDungeonDefinition {name} .*?= Ready\(',catalog,re.DOTALL)
    require(match is not None,"DDE-00 ordinary dungeon is not RuntimeReady: "+name)
require("DeepFracture" in catalog and "Status: UnderworldDungeonStatus.RuntimeReady" in catalog,
        "DDE-00 Deep Fracture lost RuntimeReady status")
require("All.Any(value => value.Status != UnderworldDungeonStatus.RuntimeReady)" in catalog,
        "DDE-00 catalog no longer enforces all-six runtime admission")

# Vanilla assets are cloned into private identities; they are never modified in place.
for token in (
    "UnityEngine.Object.Instantiate(source)",
    "new CustomRoom(",
    "CreateClonedLocation(",
    "CreateClonedPrefab(",
    "generator.m_themes = Room.Theme.None",
):
    require(token in runtime,"DDE-00 vanilla clone/isolation seam missing: "+token)

legacy_registrars=(
    "RootwarrenRoomRegistrar","RootwarrenLocationRegistrar",
    "DrownedVaultRoomRegistrar","DrownedVaultLocationRegistrar",
    "CinderworksRoomRegistrar","CinderworksLocationRegistrar",
    "RimeSepulcherRoomRegistrar","RimeSepulcherLocationRegistrar",
    "CarrionCatacombsRoomRegistrar","CarrionCatacombsLocationRegistrar",
)
require("UnderworldVanillaDungeonRegistrar" in plugin,
        "DDE-00 generic vanilla-reuse registrar is not bootstrapped")
for token in legacy_registrars:
    require(token not in plugin,"DDE-00 legacy ordinary registrar is still bootstrapped: "+token)

retired_tools=(
    "rebuild-rootwarren-dungeon.ps1","rebuild-drowned-vaults-dungeon.ps1",
    "rebuild-cinderworks-dungeon.ps1","rebuild-rime-sepulcher-dungeon.ps1",
    "rebuild-carrion-catacombs-dungeon.ps1",
    "promote-rootwarren-runtime.py","promote-drowned-vaults-runtime.py",
    "promote-cinderworks-runtime.py","promote-rime-sepulcher-runtime.py",
    "promote-carrion-catacombs-runtime.py",
)
for token in retired_tools:
    require(token not in production,
            "DDE-00 retired bespoke ordinary dungeon tooling is active in production: "+token)

require("ordinary vanilla-reuse dungeons must not be rebuilt/promoted" in readiness,
        "DDE-00 production-readiness verifier no longer protects the vanilla-reuse boundary")
require("DeepFracture" in deep_fracture,
        "DDE-00 Deep Fracture runtime architecture source is missing")

# One-at-a-time candidate admission is retired. Legacy config keys are inert compatibility only.
require("Compatibility shell for the retired one-at-a-time DDE candidate switch" in candidate,
        "DDE-00 retired candidate compatibility shell is missing")
require("internal static bool Enabled => false" in candidate,
        "DDE-00 retired candidate switch can still become active")
require("definition.Status == UnderworldDungeonStatus.RuntimeReady" in candidate,
        "DDE-00 worldgen admission is not RuntimeReady-only")
require("UnderworldVanillaDungeonCandidatePolicy.Fingerprint" in plugin,
        "DDE-00 retired runtime-policy fingerprint is not included in gameplay peer authority")
require('builder.Append("runtime-policy|")' in gameplay_fingerprint,
        "DDE-00 gameplay authority no longer fingerprints runtime policy")
require("runtimePolicyFingerprint" in synchronizer,
        "DDE-00 definition authority synchronizer lost runtime policy")
require("UnderworldDungeonCatalog.All.Count" in bridge and
        "WorldgenAdmittedDungeonCount" not in bridge,
        "DDE-00 detached worldgen bridge does not require the full dungeon catalog")
require("without candidate admission" not in placement and
        "IsWorldgenAdmitted(dungeon)" not in placement,
        "DDE-00 native placement audit still contains one-at-a-time candidate gating")
require("active.Length != UnderworldVanillaDungeonReuseCatalog.All.Count" in runtime,
        "DDE-00 registrar does not require all five ordinary donor families together")

if fail:
    print("FAIL Deep Dungeon Expansion DDE-00 architecture freeze")
    for item in fail:
        print(" - "+item)
    raise SystemExit(1)

print("PASS Deep Dungeon Expansion DDE-00 architecture freeze")
print(" - Deep Fracture remains separate and runtime-ready")
print(" - five ordinary expanded-vanilla families are RuntimeReady together")
print(" - >=1.5x room scale and >=3.5x donor room-count floors are locked")
print(" - vanilla assets are cloned into private Magenheim identities")
print(" - retired bespoke ordinary dungeon bootstrap/production paths are inactive")
print(" - one-at-a-time candidate admission is retired; legacy config is inert")
