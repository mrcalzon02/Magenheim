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
)

FORBIDDEN_RUNTIME_TOKENS = (
    "EngineBaseY",
    "UnderworldInstanceLayer.",
    "IsUnderworldEnginePosition",
    "UnderworldInstanceChunkMaterializer",
    "UnderworldInstanceChunkStreamingRuntime",
    "UnderworldPlaceholderEcologyRuntime",
)

REQUIRED_FILES = (
    RUNTIME / "ValheimWorldInstanceRegistry.cs",
    RUNTIME / "ValheimWorldInstanceExecution.cs",
    RUNTIME / "UnderworldNativeWorldHost.cs",
    RUNTIME / "UnderworldZonePeerRouting.cs",
    RUNTIME / "UnderworldZdoPeerRouter.cs",
    RUNTIME / "UnderworldInstancePersistence.cs",
    RUNTIME / "UnderworldGateTransitRuntime.cs",
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
    ),
    RUNTIME / "UnderworldNativeWorldHost.cs": (
        "CreateSceneParameters(LocalPhysicsMode.Physics3D)",
        "UnderworldWorldInstanceId.Underworld",
        "ValheimWorldInstanceExecution.BindRegistry",
    ),
    RUNTIME / "UnderworldGateTransitRuntime.cs": (
        "WorldInstances.MovePlayer",
        "SceneManager.MoveGameObjectToScene",
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

instructions = (ROOT / "INSTRUCTIONS.md").read_text(encoding="utf-8-sig")
for statement in (
    "One parent save, one running Valheim server process, multiple live world instances.",
    "Instance identity is an explicit discriminator, never a coordinate transform.",
    "Surface and Underworld players must coexist concurrently in multiplayer.",
):
    if statement not in instructions:
        fail(f"authoritative INSTRUCTIONS.md lost invariant: {statement}")

print("Verified Underworld multi-world architecture: one save/process, explicit instance identity, concurrent multiplayer, no coordinate-layer host.")
