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

require("MinimumLinearRoomScale = 1.5d" in reuse,
        "DDE-00 scale floor drifted below the 1.5x authority")
require("MinimumRoomCountMultiplier = 3.5d" in reuse,
        "DDE-00 room-count floor drifted below the 3.5x authority")
require("AllowMagenheimAuthoredRoomInjection: false" in reuse,
        "DDE-00 ordinary donor dungeons no longer prohibit bespoke room injection")
for donor in ("DG_ForestCrypt","DG_SunkenCrypt","DG_DvergrTown","DG_Cave","DG_Hole"):
    require(donor in reuse,"DDE-00 donor mapping disappeared: "+donor)

# Ordinary dungeons must remain fail-closed until their individual promotion gate is cleared.
for name in ("FungalForest","BlackwaterDeep","SulfurousWastes","FrozenCaverns","GreatDecay"):
    match=re.search(rf'public static UnderworldDungeonDefinition {name} .*?= Planned\(',catalog,re.DOTALL)
    require(match is not None,"DDE-00 ordinary dungeon is not Planned before per-family promotion: "+name)
require("DeepFracture" in catalog and "Status: UnderworldDungeonStatus.RuntimeReady" in catalog,
        "DDE-00 Deep Fracture lost RuntimeReady status")

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

# Planned candidate admission is a developer-only exception, disabled by default and peer-fingerprinted.
require('"EnablePlannedCandidateWorldgen",\n            false' in candidate,
        "DDE-00 candidate worldgen is not disabled by default")
require("CandidateDungeon" in candidate and '"fungal_forest"' in candidate,
        "DDE-00 Fungal-first candidate default is missing")
require("definition.Status == UnderworldDungeonStatus.RuntimeReady || IsCandidate(definition)" in candidate,
        "DDE-00 candidate policy no longer limits admission to RuntimeReady or one explicit candidate")
require("UnderworldVanillaDungeonCandidatePolicy.Fingerprint" in plugin,
        "DDE-00 candidate policy is not included in gameplay peer authority")
require('builder.Append("runtime-policy|")' in gameplay_fingerprint,
        "DDE-00 gameplay authority no longer fingerprints runtime candidate policy")
require("runtimePolicyFingerprint" in synchronizer,
        "DDE-00 definition authority synchronizer lost runtime candidate policy")
require("IsWorldgenAdmitted(dungeon)" in bridge and "without candidate admission" in bridge,
        "DDE-00 detached worldgen catalog does not preserve candidate-aware Planned rejection")
require("IsWorldgenAdmitted(dungeon)" in placement and "without candidate admission" in placement,
        "DDE-00 native placement audit does not preserve candidate-aware Planned rejection")
require("ReadyProfiles()" not in runtime,
        "DDE-00 generic registrar still contains the retired ReadyProfiles execution seam")

if fail:
    print("FAIL Deep Dungeon Expansion DDE-00 architecture freeze")
    for item in fail:
        print(" - "+item)
    raise SystemExit(1)

print("PASS Deep Dungeon Expansion DDE-00 architecture freeze")
print(" - Deep Fracture remains separate and runtime-ready")
print(" - five ordinary families remain Planned and vanilla-derived")
print(" - >=1.5x room scale and >=3.5x donor room-count floors are locked")
print(" - vanilla assets are cloned into private Magenheim identities")
print(" - retired bespoke ordinary dungeon bootstrap/production paths are inactive")
print(" - Planned candidate admission is disabled by default, single-family, and multiplayer-fingerprinted")
