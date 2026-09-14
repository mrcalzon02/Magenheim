# Magenheim Project State

## Authority

This file records verified repository state. `INSTRUCTIONS.md`, live committed source on `main`, and directly observed Git state outrank stale branch documents or conversational claims.

## Reconciliation baseline — 2026-09-13

Before consolidation the repository had two divergent branches:

- `main`: `6282de796cf417edcd36bdc7f941e6e33f03a44f`
- `master`: `78502cbdb922d1d42e1a33cb8c8207698ec43a53`
- common ancestor: `7cef7452f2ded492772d79e1ddd290081a6c3a68`

`master` contained three commits not on `main`, while `main` contained eleven commits not on `master`. The head commit on `master` was a committed merge containing literal unresolved conflict markers in project documents. Its tree therefore was not safe to adopt as authority.

Material work from the divergent history was reviewed individually. The additive worldgen spawn-area validator, addition planner, and their deterministic tests were retained. The large pre-reconciliation design specification was preserved as an archive record rather than replacing current authority.

The live tree deliberately does not adopt bundled BepInEx/Jötunn runtime binaries, process/log files, unrelated third-party repair tooling, stale packaging claims, or duplicate legacy refinement/runtime implementations. Their history remains reachable through the merged ancestry for forensic recovery.

## Verified consolidation result

The divergent histories were reconciled by two-parent commit `7859a27462d4b0c7a4748d3760065df599c7a521`, with the previous `main` and `master` heads as parents. `main` was advanced without force, then `master` was fast-forwarded to the same commit. A direct branch comparison reported `identical`, ahead 0 / behind 0.

`main` remains the authoritative development branch even though the repository's older GitHub default-branch metadata still names `master`. Because the branch tips were synchronized at consolidation, that metadata no longer represented divergent content; future development remains on `main` and any retained `master` pointer must not become an independent work line.

## Current domain authority

Crystal tiers are:

`Rough -> Simple -> Crystal -> Advanced -> Master`

Normal elemental alignments are Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit.

Refinement uses base failure 10/20/30/40 percent. Crystal Shaping reduces that failure according to:

`effectiveFailure = BaseFailure * (1 - (Skill / 100) * MaximumFailureReduction)`

The default maximum reduction is 75%, configurable between 50% and 100%. Station/upgrade progression gates each refinement step. A valid failed attempt destroys the source crystal and returns 1/2/3/5 matching shards by tier. Valid success and failure attempts are experience-eligible; invalid attempts are non-mutating and award no experience.

## Worldgen compatibility foundation

`Magenheim.Core.Worldgen` validates Median/Edge/All area flags and plans only additions under `magenheim.` keys. Existing registrations are copied into read-only planning snapshots. Duplicate or occupied keys are skipped or errored according to policy without modifying foreign data. Destructive compatibility mode is rejected.

## Runtime bootstrap — 2026-09-13

A thin runtime project now exists at `src/Magenheim.Runtime` and consumes the authoritative pure-core project rather than recreating refinement rules.

Runtime bootstrap properties:

- target framework: .NET Framework 4.6.2;
- Jötunn dependency: `JotunnLib` 2.30.0;
- BepInEx plugin GUID: `mrcalzon02.magenheim`;
- hard dependency on `Jotunn.Main.ModGuid`;
- Jötunn network compatibility: `EveryoneMustHaveMod` with `VersionStrictness.Minor`;
- runtime startup builds `CrystalRefinementService` from the canonical core defaults;
- no world objects, items, sockets, inventory mutations, RPCs, or persistent gameplay state are enabled by this bootstrap.

During dependency review, `Magenheim.Core` was found to target `netstandard2.1`, which cannot be consumed by a .NET Framework runtime. It was retargeted to `netstandard2.0`, which is compatible with both the net462 runtime and the net8.0 standalone test harness. This is an architectural compatibility repair, not a gameplay change.

## Validation boundary

The active execution host still does not expose `dotnet`, `csc`, or `mcs`. Compilation and test execution therefore remain unclaimed. Runtime source was checked against current Jötunn documentation and package metadata, but plugin startup has not been observed inside Valheim.

## Next exact action

In a .NET 8 SDK / Valheim development environment:

1. run `dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`;
2. compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` against Jötunn 2.30.0 and current Valheim dependencies;
3. repair any compile defect at the authoritative source;
4. then implement strict configuration/data loading before Crystal Shaping registration or gameplay mutations.
