# Geode Cracking Determinism Validation — 2026-09-14

## Scope

This bounded cycle closed the missing deterministic coverage around `Magenheim.Core.GeodeCrackingService`, which is the pure decision authority the later server-owned geode transaction must consume.

No Valheim inventory mutation, item grant, world object registration, or multiplayer transaction was enabled by this work.

## Covered invariants

The standalone core harness now covers:

- one guaranteed Rough crystal;
- a second crystal only when its independent roll is strictly below the configured second-crystal chance;
- a third crystal only when its independent roll is strictly below the configured third-crystal chance;
- exact chance-boundary behavior (`roll == chance` does not trigger a bonus);
- both bonus rolls succeeding produces exactly three crystals;
- each produced crystal consumes its own elemental roll;
- weighted element selection at cumulative boundaries;
- all generated crystals begin at `CrystalTier.Rough`;
- exactly three element rolls are required by the current schema even when only one crystal materializes, preserving fixed RNG-stream consumption;
- bonus rolls must be finite and in `[0,1)`;
- element rolls must be finite and in `[0,1)`;
- invalid requests emit no partial crystal results;
- definitions violating the one-guaranteed-crystal invariant fail closed;
- non-positive elemental weights fail closed.

The shipped Meadows/Earth definition remains one guaranteed Earth crystal with independent 0.35 and 0.10 bonus chances. The mixed-element test vector exists only to verify generic weighted-selection behavior and does not change the shipped Earth-only Meadows definition.

## Admission boundary

The test source was committed and statically reviewed against the current pure service. It has not been executed in this automation environment because the environment still lacks the required .NET SDK/compiler. Therefore no passing-test or compilation claim is made.

## Next dependency-valid action

In a .NET 8 SDK/current Valheim development environment:

1. execute `dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj` and repair any pure-core defect at its authoritative source;
2. compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` against Jötunn 2.30.0/current Valheim dependencies;
3. execute the definition-authority handshake on host/client and dedicated server;
4. only after the runtime compile/synchronization gate passes, register permanent Crystal Shaping ID `magenheim.crystal_shaping` and begin binding pure geode outcomes to an atomic server-owned inventory transaction.
