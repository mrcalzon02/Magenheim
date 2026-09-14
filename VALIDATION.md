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
- `Magenheim.Core` targets `netstandard2.0` so it can be consumed by the net462 runtime and net8.0 test harness.

Implemented runtime foundation:

- `src/Magenheim.Runtime/Magenheim.Runtime.csproj` targeting `net462` and referencing `JotunnLib` 2.30.0;
- BepInEx plugin identity `mrcalzon02.magenheim`, hard Jötunn dependency, and everyone-must-have/minor-version network compatibility declaration;
- runtime service composition consumes the existing authoritative `CrystalRefinementService` rather than implementing a second rule engine;
- item registration, world generation, sockets, RPCs, inventory mutation, and persistent gameplay state remain disabled pending their validation gates.

## Worldgen compatibility hardening — 2026-09-13

The pure core currently enforces:

- textual areas `Median`, `Edge`, `All`, and runtime-facing alias `Everywhere`;
- negative numeric areas reject under Reject/Clamp policy and only broaden under explicit fallback policy;
- occupied registration keys and prefab identities are detected without host mutation;
- per-Magenheim registration-key/prefab exclusions produce Skip decisions;
- optional case-insensitive identity matching is available for interoperability;
- `AdditiveOnly = false` is rejected rather than opening destructive compatibility behavior.

The future runtime adapter must map Magenheim's abstract All explicitly to the current Jötunn/Valheim Everywhere value rather than relying on enum integer coincidence.

## Strict definition and compatibility authority — 2026-09-13

Definition schema is now version 2. `default-data/foundation.json` requires a `worldgenCompatibility` object in addition to refinement and geode definitions.

Static validation performed for the compatibility-definition pass:

- compatibility policy is parsed before geode area parsing;
- configured `invalidAreaBehavior` now actually governs textual geode-area parsing;
- duplicate behavior, prefab collision detection, identity comparison, and Magenheim-only exclusion sets are stored in the validated snapshot;
- compatibility policy is included in the normalized SHA-256 definition fingerprint;
- changing identity comparison therefore changes the gameplay-authority fingerprint;
- registration exclusions outside the `magenheim.` namespace reject validation;
- prefab exclusions outside the `Magenheim_` namespace reject validation;
- duplicate or empty exclusion entries reject validation;
- `AdditiveOnly = false` rejects validation;
- old schema-1 documents are intentionally rejected instead of silently assuming non-fingerprinted compatibility defaults;
- the deterministic test harness contains explicit assertions for policy freezing, fingerprint variance, foreign exclusion rejection, and destructive-policy rejection.

The shipped schema-2 compatibility defaults are conservative:

- `invalidAreaBehavior`: `Reject`;
- `duplicateRegistrationBehavior`: `Skip`;
- `detectPrefabCollisions`: `true`;
- `identityComparison`: `Exact`;
- no registration-key exclusions;
- no prefab exclusions.

The Meadows definition remains Earth-only at weight 100.0, one guaranteed crystal, and independent 0.35 / 0.10 additional-crystal probabilities. Definition presence is not evidence that random cracking or natural world generation is implemented.

## Repository/branch review

`main` and `master` were observed at the same starting commit for this pass. `main` remains the only authoritative development branch; `master` is retained only as a synchronized compatibility/default-branch pointer because the available connector does not expose default-branch reassignment or branch deletion.

## Compile/test boundary

The current execution environment exposes no `dotnet`, `csc`, or `mcs`. Therefore no compilation, package restore, deterministic test execution, Valheim startup, definition-loader execution, Jötunn registration, or fresh-world generation success is claimed.

Required next validation commands/environment:

`dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`

Then compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` from a development environment with .NET Framework 4.6.2 targeting support and current Valheim/Jötunn development dependencies available. Any resulting defect must be repaired at the authoritative source before server overrides, skill registration, worldgen registration, or gameplay mutation is admitted.

Runtime startup, natural geode generation, repeated-load idempotence, multiplayer authority, save/load, and persistence remain unverified until directly observed.
