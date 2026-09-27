#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "Magenheim.Runtime"
CORE = ROOT / "src" / "Magenheim.Core" / "Underworld"

FORBIDDEN_FILES = (
    RUNTIME / "UnderworldInstanceLayer.cs",
    RUNTIME / "UnderworldInstanceLayerIsolation.cs",
    RUNTIME / "UnderworldInstanceChunkMaterializer.cs",
    RUNTIME / "UnderworldInstanceChunkStreamingRuntime.cs",
    RUNTIME / "UnderworldPlaceholderEcologyRuntime.cs",
    RUNTIME / "UnderworldStructureAdmissionController.cs",
)

FORBIDDEN_RUNTIME_TOKENS = (
    "EngineBaseY",
    "UnderworldInstanceLayer.",
    "IsUnderworldEnginePosition",
    "UnderworldInstanceChunkMaterializer",
    "UnderworldInstanceChunkStreamingRuntime",
    "UnderworldPlaceholderEcologyRuntime",
    "UnderworldStructureAdmissionController",
)

REQUIRED_FILES = (
    RUNTIME / "ValheimWorldInstanceRegistry.cs",
    RUNTIME / "ValheimWorldInstanceExecution.cs",
    RUNTIME / "UnderworldNativeWorldHost.cs",
    RUNTIME / "UnderworldZonePeerRouting.cs",
    RUNTIME / "UnderworldZdoPeerRouter.cs",
    RUNTIME / "UnderworldInstancePersistence.cs",
    RUNTIME / "UnderworldZoneInstanceState.cs",
    RUNTIME / "UnderworldPhysicsInstanceRouting.cs",
    RUNTIME / "UnderworldZdoRoutedRpcBridge.cs",
    RUNTIME / "UnderworldZonePeerRouting.cs",
    RUNTIME / "UnderworldSpawnQueryIsolation.cs",
    RUNTIME / "ValheimPathfindingInstanceState.cs",
    RUNTIME / "UnderworldStaticSceneRegistryIsolation.cs",
    RUNTIME / "UnderworldGateTransitRuntime.cs",
    RUNTIME / "UnderworldZoneSystemStartIsolation.cs",
    RUNTIME / "UnderworldZoneInstanceState.cs",
    RUNTIME / "UnderworldSpawnQueryIsolation.cs",
    RUNTIME / "UnderworldWorldGeneratorCacheIsolation.cs",
    CORE / "UnderworldInstanceContract.cs",
    CORE / "UnderworldWorldInstanceId.cs",
)

REQUIRED_SNIPPETS = {
    CORE / "UnderworldInstanceContract.cs": (
        "SingleSaveContainsMultipleWorldInstances = true",
        "NativeWorldServicesAreInstanceScoped = true",
        "PlayersMayOccupyDifferentInstancesConcurrently = true",
        "CoordinateOffsetDefinesInstanceIdentity = false",
        "VerticalEngineLayerDefinesInstanceIdentity = false",
        "WholeServerWorldSwapDefinesInstanceTransit = false",
        "SecondServerProcessDefinesInstanceTransit = false",
    ),
    CORE / "UnderworldWorldInstanceId.cs": (
        "Surface = new(0)",
        "Underworld = new(1)",
    ),
    RUNTIME / "ValheimWorldInstanceRegistry.cs": (
        "World World",
        "WorldGenerator WorldGenerator",
        "ZoneSystem ZoneSystem",
        "ZDOMan ZdoMan",
        "Scene Scene",
        "PhysicsScene PhysicsScene",
        "PathfindingState",
    ),
    RUNTIME / "UnderworldNativeWorldHost.cs": (
        "CreateSceneParameters(LocalPhysicsMode.Physics3D)",
        "UnderworldWorldInstanceId.Underworld",
        "ValheimWorldInstanceExecution.BindRegistry",
        '"m_locationsByHash"',
        'string.Equals(name, "m_globalKeys", StringComparison.Ordinal)',
        'string.Equals(name, "m_globalKeysEnums", StringComparison.Ordinal)',
        'string.Equals(name, "m_globalKeysValues", StringComparison.Ordinal)',
    ),
    RUNTIME / "UnderworldGateTransitRuntime.cs": (
        "WorldInstances.MovePlayer",
        "SceneManager.MoveGameObjectToScene",
        "TryMovePlayerToInstance",
        "CommitOldInstanceVisibility",
    ),
    RUNTIME / "UnderworldZoneSystemStartIsolation.cs": (
        "UnderworldZoneCatalogGuard.Validate(__instance);",
        "ValidateVegetation.Invoke(__instance, Array.Empty<object>());",
        "UnderworldZoneInstanceState.NotifyZoneReady(__instance);",
    ),
    RUNTIME / "UnderworldZoneInstanceState.cs": (
        "underworld.ZoneSystem.PrepareSave();",
        "underworld.ZoneSystem.SaveASync(writer);",
        "underworld.ZoneSystem.Load(reader, version);",
        "metadata.Persistent = true;",
        "SuppressSpatialIndex",
        "IncludeMetadataSaveClone",
    ),
    RUNTIME / "UnderworldSpawnQueryIsolation.cs": (
        "gameObject.scene.handle",
        "Player.GetAllPlayers",
        "BaseAI.BaseAIInstances",
    ),
    RUNTIME / "UnderworldWorldGeneratorCacheIsolation.cs": (
        '"s_cachedBiomeAreas"',
        '"s_cachedBiomes"',
        "CaptureSurfaceCaches",
        "RestoreSurfaceCaches",
    ),
    RUNTIME / "UnderworldInstancePersistence.cs": (
        "UnderworldZoneInstanceState.NotifyZdosLoaded();",
        "PreserveSharedSaveSnapshotDuringInstanceCleanup",
    ),
}

def fail(message: str) -> None:
    print(f"UNDERWORLD INSTANCE ARCHITECTURE ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)

for path in FORBIDDEN_FILES:
    if path.exists():
        fail(f"obsolete hosting file returned: {path.relative_to(ROOT)}")

for path in REQUIRED_FILES:
    if not path.is_file():
        fail(f"required instance file missing: {path.relative_to(ROOT)}")

runtime_sources = []
for path in RUNTIME.glob("*.cs"):
    text = path.read_text(encoding="utf-8-sig")
    runtime_sources.append((path, text))

for token in FORBIDDEN_RUNTIME_TOKENS:
    offenders = [str(path.relative_to(ROOT)) for path, text in runtime_sources if token in text]
    if offenders:
        fail(f"forbidden runtime token {token!r} found in {', '.join(offenders)}")

for path, snippets in REQUIRED_SNIPPETS.items():
    text = path.read_text(encoding="utf-8-sig")
    for snippet in snippets:
        if snippet not in text:
            fail(f"{path.relative_to(ROOT)} no longer proves required invariant {snippet!r}")

terrain = (RUNTIME / "UnderworldTerrainRuntime.cs").read_text(encoding="utf-8-sig")
sample_match = re.search(
    r"internal static UnderworldTerrainResult SampleInstanceTerrain\([^)]*\)\s*\{(?P<body>.*?)\n    \}",
    terrain,
    re.S,
)
if not sample_match:
    fail("could not locate SampleInstanceTerrain for worker-thread singleton audit")
sample_body = sample_match.group("body")
for forbidden in ("ZoneSystem.instance", "ZNet.instance", "Player.", "SceneManager."):
    if forbidden in sample_body:
        fail(f"worker-thread terrain sampler reads process/Unity singleton {forbidden!r}")

start_isolation = (RUNTIME / "UnderworldZoneSystemStartIsolation.cs").read_text(encoding="utf-8-sig")
if "SetupLocations.Invoke" in start_isolation or 'AccessTools.Method(typeof(ZoneSystem), "SetupLocations")' in start_isolation:
    fail("Underworld ZoneSystem Start reintroduced a second SetupLocations pass")

gate = (RUNTIME / "UnderworldGateTransitRuntime.cs").read_text(encoding="utf-8-sig")
return_match = re.search(
    r"internal static bool TryReturnTo\([^)]*\)\s*\{(?P<body>.*?)\n    \}",
    gate,
    re.S,
)
if not return_match:
    fail("could not locate TryReturnTo for instance-transfer audit")
return_body = return_match.group("body")
if "TryMovePlayerToInstance" not in return_body or ".TeleportTo(" in return_body:
    fail("developer Surface return bypasses atomic world-instance transfer")

plugin = (RUNTIME / "MagenheimPlugin.cs").read_text(encoding="utf-8-sig")
for patch in (
    "UnderworldZoneStateCapturePatch",
    "UnderworldZoneMetadataSectorPatch",
    "UnderworldZoneMetadataSaveClonePatch",
    "UnderworldZoneMetadataLoadFilterPatch",
    "UnderworldZoneMetadataShouldSendPatch",
    "UnderworldZoneSystemStartIsolationPatch",
    "UnderworldSpawnQueryScopePatch",
):
    if f"PatchAll(typeof({patch}))" not in plugin:
        fail(f"runtime bootstrap no longer installs {patch}")
if plugin.index("PatchAll(typeof(UnderworldZoneStateCapturePatch))") > plugin.index("PatchAll(typeof(UnderworldZdoPrepareSavePersistencePatch))"):
    fail("Underworld ZoneSystem metadata is captured after ZDO PrepareSave instead of before it")

plugin_version_match = re.search(r'PluginVersion\s*=\s*"([^"]+)"', plugin)
project = (RUNTIME / "Magenheim.Runtime.csproj").read_text(encoding="utf-8-sig")
project_version_match = re.search(r"<Version>([^<]+)</Version>", project)
if not plugin_version_match or not project_version_match:
    fail("could not resolve runtime/plugin version identity")
if plugin_version_match.group(1) != project_version_match.group(1):
    fail(
        f"runtime assembly version {project_version_match.group(1)} does not match "
        f"PluginVersion {plugin_version_match.group(1)}"
    )

instructions = (ROOT / "INSTRUCTIONS.md").read_text(encoding="utf-8-sig")
for statement in (
    "One parent save, one running Valheim server process, multiple live world instances.",
    "Instance identity is an explicit discriminator, never a coordinate transform.",
    "Surface and Underworld players must coexist concurrently in multiplayer.",
):
    if statement not in instructions:
        fail(f"authoritative INSTRUCTIONS.md lost invariant: {statement}")

print("Verified Underworld multi-world architecture: one save/process, explicit instance identity, concurrent multiplayer, no coordinate-layer host.")
