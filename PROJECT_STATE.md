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

## Current domain authority

Crystal tiers are:

`Rough -> Simple -> Crystal -> Advanced -> Master`

Normal elemental alignments are Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit.

Refinement uses base failure 10/20/30/40 percent. Crystal Shaping reduces that failure according to:

`effectiveFailure = BaseFailure * (1 - (Skill / 100) * MaximumFailureReduction)`

The default maximum reduction is 75%, configurable between 50% and 100%. Station/upgrade progression gates each refinement step. A valid failed attempt destroys the source crystal and returns 1/2/3/5 matching shards by tier. Valid success and failure attempts are experience-eligible; invalid attempts are non-mutating and award no experience.

## Worldgen compatibility foundation

`Magenheim.Core.Worldgen` validates Median/Edge/All area flags and plans only additions under `magenheim.` keys. Existing registrations are copied into read-only planning snapshots. Duplicate or occupied keys are skipped or errored according to policy without modifying foreign data. Destructive compatibility mode is rejected.

## Validation boundary

No compiler is available in the current execution environment. Compilation and runtime validation are therefore not claimed. Static source/Git reconciliation is the present admission level.

## Next exact action

Run `dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj` in a .NET 8 SDK environment, resolve any compile/test defect at the authoritative source, then add the thin BepInEx/Jötunn bootstrap without enabling persistent gameplay mutations until server-authoritative transactions exist.
