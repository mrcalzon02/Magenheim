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
- `Magenheim.Core` was targeting `netstandard2.1`, which is not consumable from .NET Framework.

Repair and implementation performed:

- retargeted `Magenheim.Core` from `netstandard2.1` to `netstandard2.0`;
- preserved the .NET 8 standalone test project as a consumer of the same core;
- added `src/Magenheim.Runtime/Magenheim.Runtime.csproj` targeting `net462` and referencing `JotunnLib` 2.30.0;
- added BepInEx plugin identity `mrcalzon02.magenheim`, hard Jötunn dependency, and everyone-must-have/minor-version network compatibility declaration;
- added a runtime service boundary that creates the existing authoritative `CrystalRefinementService` from canonical core defaults instead of implementing a second rule engine;
- deliberately left item registration, world generation, sockets, RPCs, inventory mutation, and persistent gameplay state disabled.

Static checks performed:

- net462 -> netstandard2.0 compatibility was confirmed against current Microsoft framework compatibility guidance;
- plugin attribute forms were checked against current BepInEx/Jötunn documentation;
- current Jötunn package target/version metadata was checked before pinning 2.30.0;
- no runtime code duplicates refinement equations or canonical tier definitions;
- no foreign prefab/worldgen mutation was introduced;
- no GitHub Actions or bundled runtime binaries were added.

## Compile/test boundary

The current execution environment exposes no .NET SDK/compiler. Therefore no compilation, package restore, deterministic test execution, or Valheim startup success is claimed.

Required next validation commands/environment:

`dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`

Then compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` from a development environment with .NET Framework 4.6.2 targeting support and the current Valheim/Jötunn development dependencies available. Any resulting defect must be repaired at the authoritative source before config loading or skill registration is admitted.

Runtime startup, Jötunn registration, world generation, multiplayer authority, save/load, and persistence remain unverified until directly observed.
