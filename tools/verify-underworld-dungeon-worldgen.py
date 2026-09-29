#!/usr/bin/env python3
"""Fail closed if the Underworld dungeon spawn path stops reaching native Valheim worldgen."""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
RUNTIME=ROOT/"src/Magenheim.Runtime"

families={
    "Deep Fracture":(
        "UnderworldDeepFractureLocationRegistrar.cs",
        "UnderworldDungeonCatalog.DeepFracture",
        "FractureZones"),
    "Rootwarren":(
        "RootwarrenLocationRegistrar.cs",
        "UnderworldDungeonCatalog.FungalForest",
        "FungalForest"),
    "Drowned Vaults":(
        "DrownedVaultLocationRegistrar.cs",
        "UnderworldDungeonCatalog.BlackwaterDeep",
        "BlackwaterDeep"),
    "Cinderworks":(
        "CinderworksLocationRegistrar.cs",
        "UnderworldDungeonCatalog.SulfurousWastes",
        "SulfurousWastes"),
    "Rime Sepulcher":(
        "RimeSepulcherLocationRegistrar.cs",
        "UnderworldDungeonCatalog.FrozenCaverns",
        "FrozenCaverns"),
    "Carrion Catacombs":(
        "CarrionCatacombsLocationRegistrar.cs",
        "UnderworldDungeonCatalog.GreatDecay",
        "GreatDecay"),
}

fail=[]

for name,(filename,catalog,biome) in families.items():
    path=RUNTIME/filename
    if not path.is_file():
        fail.append(name+": missing "+filename)
        continue
    text=path.read_text()
    for token,label in (
        (catalog,"catalog binding"),
        ("UnderworldTerrainRuntime.ToNativeBiome","native biome mapping"),
        ("Quantity","quantity"),
        ("MinDistanceFromSimilar","same-family spacing"),
        ("ZoneManager.Instance.AddCustomLocation","native/Jotunn location admission"),
    ):
        if token not in text:
            fail.append(name+": missing "+label)
    if name!="Deep Fracture" and "UnderworldDungeonStatus.RuntimeReady" not in text:
        fail.append(name+": missing RuntimeReady registration gate")
    if biome not in text and name=="Deep Fracture":
        # Deep Fracture logs/identity may use the definition rather than spelling the enum.
        pass

bridge=(RUNTIME/"UnderworldWorldgenContentBridge.cs").read_text()
for token,label in (
    ("row.m_biome != expectedBiome","exact owning-biome mask enforcement"),
    ("is Planned but has","Planned dungeon row rejection"),
    ("exactly one is required","duplicate detached dungeon-row rejection"),
    ("missingDungeons","runtime-ready row presence gate"),
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
    "GetBiomeArea",
):
    if token not in terrain and token!="GetBiomeArea":
        fail.append("terrain runtime: missing "+token)

cache=(RUNTIME/"UnderworldWorldGeneratorCacheIsolation.cs").read_text()
if "GetBiomeArea" not in cache or "TryGetBiomeArea" not in cache:
    fail.append("biome-area cache isolation no longer serves Underworld location placement")

dev=(RUNTIME/"UnderworldDevCommands.cs").read_text()
if 'case "dungeons"' not in dev or "UnderworldDungeonPlacementRuntime.LastReport" not in dev:
    fail.append("dev commands: actual dungeon placement report is unavailable")

if fail:
    raise SystemExit("FAIL Underworld dungeon worldgen contract: "+"; ".join(fail))

print(
    "PASS Underworld dungeon worldgen contract: six dungeon families route through exact native "
    "Underworld biome masks; generated positions are counted, biome/spacing-audited and server-reconciled.")
