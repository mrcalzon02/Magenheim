#!/usr/bin/env python3
"""Fail closed if the Underworld dungeon spawn path stops reaching native Valheim worldgen."""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
RUNTIME=ROOT/"src/Magenheim.Runtime"
CORE=ROOT/"src/Magenheim.Core"/"Underworld"
fail=[]

# Deep Fracture is the sole bespoke architecture lane.
deep=(RUNTIME/"UnderworldDeepFractureLocationRegistrar.cs").read_text()
for token,label in (
    ("UnderworldDungeonCatalog.DeepFracture","Deep Fracture catalog binding"),
    ("UnderworldTerrainRuntime.ToNativeBiome","Deep Fracture native biome mapping"),
    ("Quantity","Deep Fracture quantity"),
    ("MinDistanceFromSimilar","Deep Fracture same-family spacing"),
    ("ZoneManager.Instance.AddCustomLocation","Deep Fracture native/Jotunn admission"),
):
    if token not in deep:
        fail.append("Deep Fracture: missing "+label)

# The five ordinary families share one generic vanilla-donor runtime.
reuse=(CORE/"UnderworldVanillaDungeonReuseCatalog.cs").read_text()
registrar=(RUNTIME/"UnderworldVanillaDungeonRegistrar.cs").read_text()
for donor in ("DG_ForestCrypt","DG_SunkenCrypt","DG_DvergrTown","DG_Cave","DG_Hole"):
    if donor not in reuse:
        fail.append("ordinary reuse catalog: missing donor "+donor)
for token,label in (
    ("ActiveProfiles()","candidate/RuntimeReady active profile boundary"),
    ("CreateClonedLocation","owned donor entrance clone"),
    ("RegisterDungeonTheme","private room theme"),
    ("generator.m_themes = Room.Theme.None","vanilla room-theme isolation"),
    ("UnderworldTerrainRuntime.ToNativeBiome(profile.Biome)","native owning-biome mapping"),
    ("zone.m_quantity = definition.Quantity","catalog quantity binding"),
    ("zone.m_minDistanceFromSimilar","same-family spacing binding"),
):
    if token not in registrar:
        fail.append("ordinary reuse registrar: missing "+label)

candidate=(RUNTIME/"UnderworldVanillaDungeonCandidatePolicy.cs").read_text()
for token,label in (
    ('"EnablePlannedCandidateWorldgen",\n            false',"candidate disabled-by-default contract"),
    ("IsWorldgenAdmitted","single candidate worldgen admission predicate"),
    ("definition.Status == UnderworldDungeonStatus.RuntimeReady || IsCandidate(definition)",
     "RuntimeReady-or-explicit-candidate restriction"),
):
    if token not in candidate:
        fail.append("candidate policy: missing "+label)

bridge=(RUNTIME/"UnderworldWorldgenContentBridge.cs").read_text()
for token,label in (
    ("row.m_biome != expectedBiome","exact owning-biome mask enforcement"),
    ("is Planned but has","non-candidate Planned dungeon row rejection"),
    ("without candidate admission","candidate-scoped Planned rejection diagnostic"),
    ("exactly one is required","duplicate detached dungeon-row rejection"),
    ("missingDungeons","admitted row presence gate"),
    ("IsWorldgenAdmitted(dungeon)","candidate-aware catalog admission"),
):
    if token not in bridge:
        fail.append("worldgen bridge: missing "+label)

placement=(RUNTIME/"UnderworldDungeonPlacementRuntime.cs").read_text()
for token,label in (
    ("m_locationsGenerated","native location-generation completion boundary"),
    ("m_locationInstances.Values","actual generated position table"),
    ("GenerateLocationsTimeSliced","native missing-location recovery path"),
    ("SampleForDiagnostics","generated-position owning-biome audit"),
    ("instances.Length != dungeon.Quantity","target quantity enforcement"),
    ("MinDistanceFromSimilarMeters","same-family spacing enforcement"),
    ("ZNet.instance.IsServer()","server-authoritative reconciliation"),
    ("IsWorldgenAdmitted(dungeon)","candidate-aware native placement audit"),
    ("without candidate admission","non-candidate Planned placement rejection"),
):
    if token not in placement:
        fail.append("placement runtime: missing "+label)

host=(RUNTIME/"UnderworldNativeWorldHost.cs").read_text()
if "AddComponent<UnderworldDungeonPlacementRuntime>" not in host:
    fail.append("native world host: dungeon placement runtime is not attached")
if "UnderworldDungeonPlacementRuntime.Configure" not in host:
    fail.append("native world host: dungeon placement runtime is not configured")

terrain=(RUNTIME/"UnderworldTerrainRuntime.cs").read_text()
for token in (
    "FungalForestBiome = (Heightmap.Biome)1024",
    "BlackwaterDeepBiome = (Heightmap.Biome)2048",
    "SulfurousWastesBiome = (Heightmap.Biome)4096",
    "FrozenCavernsBiome = (Heightmap.Biome)8192",
    "FractureZonesBiome = (Heightmap.Biome)16384",
    "GreatDecayBiome = (Heightmap.Biome)32768",
    "GetBiomeSector",
):
    if token not in terrain:
        fail.append("terrain runtime: missing "+token)

cache=(RUNTIME/"UnderworldWorldGeneratorCacheIsolation.cs").read_text()
if "GetBiomeArea" not in cache or "TryGetBiomeArea" not in cache:
    fail.append("biome-area cache isolation no longer serves Underworld location placement")

dev=(RUNTIME/"UnderworldDevCommands.cs").read_text()
if 'case "dungeons"' not in dev or "UnderworldDungeonPlacementRuntime.LastReport" not in dev:
    fail.append("dev commands: actual dungeon placement report is unavailable")
if 'case "donors"' not in dev or "UnderworldVanillaDungeonDonorCensus.CaptureAll" not in dev:
    fail.append("dev commands: DDE live donor census is unavailable")

if fail:
    raise SystemExit("FAIL Underworld dungeon worldgen contract: "+"; ".join(fail))

print(
    "PASS Underworld dungeon worldgen contract: Deep Fracture remains bespoke; five ordinary "
    "families use private vanilla-donor clones; RuntimeReady plus at most one fingerprinted "
    "developer candidate route through exact native Underworld biome masks and placement audit.")
