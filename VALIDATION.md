# Magenheim Validation Record

## 2026-09-13 history reconciliation

Observed pre-consolidation Git state:

- `main` head: `6282de796cf417edcd36bdc7f941e6e33f03a44f`;
- `master` head: `78502cbdb922d1d42e1a33cb8c8207698ec43a53`;
- merge base: `7cef7452f2ded492772d79e1ddd290081a6c3a68`;
- `main` was eleven commits ahead and three commits behind `master`;
- the `master` head commit contained literal unresolved merge markers in tracked project documents.

The contaminated tree was not accepted wholesale. Material divergent work was inspected by file and reconciled against current authority.

### Retained

- spawn-area flags and invalid-area policy;
- additive-only worldgen addition planner with immutable observed snapshots and duplicate diagnostics;
- deterministic worldgen planner tests;
- the large legacy design specification, archived for provenance rather than live authority.

### Not admitted to the live tree

- BepInEx/Jötunn runtime copies and binary dependencies;
- runtime process/log files and built plugin DLLs;
- unrelated third-party compatibility-repair utilities;
- stale packaging/build claims;
- duplicate legacy refinement/runtime implementations that would create parallel authorities.

## Domain reconciliation

The direct product progression is Rough -> Simple -> Crystal -> Advanced -> Master. Earlier repair text that renamed the third tier to `Refined` conflicted with the current product direction and was corrected throughout live source and authority documents.

Current refinement invariants:

- one tier per successful attempt;
- base failure 10%, 20%, 30%, 40%;
- skill reduction `BaseFailure * (1 - (Skill / 100) * MaximumFailureReduction)`;
- default maximum reduction 75%, allowed 50%-100%;
- workstation/upgrade gating;
- failure destroys the source and returns 1/2/3/5 matching shards;
- valid success/failure attempts award experience eligibility; invalid attempts do not;
- elemental alignment is preserved.

## Runtime bootstrap review — 2026-09-13

Observed dependency facts before implementation:

- current Jötunn package version selected for the runtime is `JotunnLib` 2.30.0;
- Jötunn 2.30.0 targets .NET Framework 4.6.2;
- Jötunn documentation recommends a hard `BepInDependency(Jotunn.Main.ModGuid)` and `NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)` for mods whose items/RPCs require consistent client/server presence;
- `Magenheim.Core` was retargeted to `netstandard2.0` so it can be consumed by the net462 runtime and net8.0 test harness.

Implemented runtime foundation:

- `src/Magenheim.Runtime/Magenheim.Runtime.csproj` targeting `net462` and referencing `JotunnLib` 2.30.0;
- BepInEx plugin identity `mrcalzon02.magenheim`, hard Jötunn dependency, and everyone-must-have/minor-version network compatibility declaration;
- runtime service composition that consumes the existing authoritative `CrystalRefinementService` rather than implementing a second rule engine;
- item registration, world generation, sockets, RPCs, inventory mutation, and persistent gameplay state remain disabled pending their validation gates.

## Worldgen compatibility hardening — 2026-09-13

Implementation commit: `22c4a6dd8a0400d8f53a888af7a1d50da422df6f`.

Static source review identified and repaired a flags failure mode: negative numeric values such as `-1` could previously be passed through `ClampKnownBits`, where sign extension made both known bits appear set and could silently normalize the invalid value to `All`. Negative numeric values now reject under Reject/Clamp policies and only become All under the explicit fallback policy.

Additional static compatibility checks added to the pure core and deterministic test harness:

- textual parsing accepts `Median`, `Edge`, `All`, and `Everywhere`;
- unknown textual tokens reject by default;
- clamp mode may preserve recognized textual areas while discarding unknown tokens;
- occupied prefab identities are detected in addition to occupied registration keys;
- collision checks are observation-only and do not mutate existing registrations;
- per-Magenheim registration-key and prefab exclusions produce Skip decisions rather than host mutation;
- optional case-insensitive identity matching can be enabled for interoperability;
- `AdditiveOnly = false` continues to be rejected rather than opening a destructive compatibility mode.

Current Jötunn documentation describes the runtime spawn enum default as `Heightmap.BiomeArea.Everywhere`, while generated vegetation data exposes Edge and Median area membership. The future runtime adapter therefore must map Magenheim's abstract All explicitly to the runtime Everywhere value rather than relying on integer coincidence.

## Strict definition pipeline — 2026-09-13

Source-level validation completed for the new definition authority introduced for runtime version `0.0.4`.

Observed implementation:

- `default-data/foundation.json` is the shipped static definition source and is copied into the runtime output under `default-data/foundation.json`;
- runtime JSON parsing uses Newtonsoft.Json with `MissingMemberHandling.Error`;
- duplicate JSON property names are rejected during `JObject` parsing;
- required schema fields use `Required.Always` rather than accepting missing default values;
- parsed refinement/geode records are passed into `MagenheimDefinitionValidator.ValidateAndFreeze` before `RuntimeServices` is constructed;
- startup failure is logged as fatal and rethrown; no hard-coded refinement fallback is retained in the runtime composition path;
- validated content receives a normalized deterministic SHA-256 fingerprint for later client/server authority comparison.

Pure-core definition validation statically enforces:

- exact schema version 1;
- exactly one Rough, Simple, Crystal, and Advanced source refinement transition and no Master source transition;
- existing `RefinementRule.Validate()` range, tier-order, station, and shard constraints;
- `magenheim.geode.` registration identity namespace and `Magenheim_` prefab namespace;
- supported terrestrial biome names only;
- conservative spawn-area admission through `SpawnAreaValidator`;
- exactly one guaranteed crystal in the current geode contract;
- finite second/third crystal chances in [0,1];
- at least one positive finite elemental weight with no duplicate element entries;
- duplicate geode ids and duplicate geode prefab identities rejected.

The shipped Meadows definition contains only Earth at weight 100.0 and uses one guaranteed result plus independent 0.35 and 0.10 additional-crystal probabilities. This is definition presence and static-schema evidence only; the actual random selection/cracking transaction is not yet implemented and is not claimed.

Static readback confirmed the runtime project, loader, plugin startup path, core definition validator, foundation JSON, backlog, project state, and README are all present on `main` after the changes. No generated DLL or vendor binary was committed.

## Repository/branch review

During the earlier compatibility pass, concurrent authorized work advanced `main` twice. Each advancement was detected before ref mutation and reconciled without force-pushing. A first prepared commit was intentionally not published after GitHub rejected it as non-fast-forward. The final compatibility implementation was rebuilt on top of the newer authoritative runtime-foundation history and then fast-forwarded normally.

`master` was subsequently fast-forwarded to the same implementation commit so the legacy default-branch pointer did not become a divergent development line. The available connector does not expose branch deletion or default-branch reassignment, so remote pruning beyond synchronization is deferred rather than falsely claimed.

## Compile/test boundary

The current execution environment exposes no `dotnet`, `csc`, or `mcs`. Therefore no compilation, package restore, deterministic test execution, Valheim startup, definition-loader execution, Jötunn registration, or fresh-world generation success is claimed.

Required next validation commands/environment:

`dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`

Then compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` from a development environment with .NET Framework 4.6.2 targeting support and current Valheim/Jötunn development dependencies available. Any resulting defect must be repaired at the authoritative source before server configuration overrides, skill registration, worldgen registration, or gameplay mutation is admitted.

Runtime startup, natural geode generation, repeated-load idempotence, multiplayer authority, save/load, and persistence remain unverified until directly observed.
