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

## Static validation performed

- reviewed `main` and `master` branch heads and their common ancestor;
- compared both directions to identify unique material files;
- inspected the contaminated merge and confirmed tracked conflict-marker content;
- reviewed current refinement source against live design authority;
- reviewed worldgen validator/planner for additive-only behavior and no mutation of observed registrations;
- constructed one resolved consolidation tree rather than stacking another post-merge mutator;
- preserved rejected branch material through merge ancestry rather than deleting published history.

## Compile/test boundary

The current execution environment exposes no .NET SDK/compiler. Therefore no compilation or test-execution success is claimed. A deterministic console test project exists at `tests/Magenheim.Core.Tests` and is the next validation target.

Required next command in a .NET 8 SDK environment:

`dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`

Runtime startup, Jötunn registration, world generation, multiplayer authority, save/load, and persistence remain deferred until implemented and directly observed.
