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

The divergent histories were reconciled by two-parent commit `7859a27462d4b0c7a4748d3760065df599c7a521`, with the previous `main` and `master` heads as parents. `main` was advanced without force and `master` was brought onto the same ancestry.

`main` remains the authoritative development branch even though the repository's older GitHub default-branch metadata still names `master`. The available repository connector does not expose default-branch mutation or branch deletion. `master` is therefore treated only as a synchronized compatibility pointer and must not become an independent development line.

## Current domain authority

Crystal tiers are:

`Rough -> Simple -> Crystal -> Advanced -> Master`

Normal elemental alignments are Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit.

Refinement uses base failure 10/20/30/40 percent. Crystal Shaping reduces that failure according to:

`effectiveFailure = BaseFailure * (1 - (Skill / 100) * MaximumFailureReduction)`

The default maximum reduction is 75%, configurable between 50% and 100%. Station/upgrade progression gates each refinement step. A valid failed attempt destroys the source crystal and returns 1/2/3/5 matching shards by tier. Valid success and failure attempts are experience-eligible; invalid attempts are non-mutating and award no experience.

## Worldgen compatibility foundation

Current rules:

- abstract areas remain Median, Edge, and All;
- configuration also accepts `Everywhere` as the runtime-facing alias for All;
- negative numeric flag values cannot be clamped into All through sign extension;
- unknown positive bits reject by default, may retain recognized bits only under explicit clamp policy, or may fall back to All only under explicit fallback policy;
- occupied Magenheim registration keys and occupied prefab identities are collision-checked without changing observed host data;
- specific Magenheim registration keys or prefabs may be excluded through compatibility policy;
- identity matching is exact by default with an optional case-insensitive compatibility mode;
- destructive compatibility mode remains structurally rejected.

The eventual Jötunn adapter must map these abstract values explicitly to current `Heightmap.BiomeArea` values rather than casting enum integers.

## Runtime bootstrap

A thin runtime project exists at `src/Magenheim.Runtime` and consumes the authoritative pure-core project rather than recreating refinement rules.

Runtime bootstrap properties:

- target framework: .NET Framework 4.6.2;
- Jötunn dependency: `JotunnLib` 2.30.0;
- BepInEx plugin GUID: `mrcalzon02.magenheim`;
- hard dependency on `Jotunn.Main.ModGuid`;
- Jötunn network compatibility: `EveryoneMustHaveMod` with `VersionStrictness.Minor`;
- runtime startup consumes the validated core definition snapshot;
- no world objects, items, sockets, inventory mutations, RPCs, or persistent gameplay state are enabled by this bootstrap.

`Magenheim.Core` targets `netstandard2.0` so it can remain consumable by both the net462 runtime and the net8.0 standalone test harness.

## Strict definition pipeline

The runtime loads `default-data/foundation.json`, converts it into pure-core definition records, validates the complete snapshot, computes a deterministic SHA-256 fingerprint, and only then creates runtime services.

Definition admission rules include exact schema matching, strict JSON member handling, canonical four-step refinement topology, namespaced geode IDs/prefabs, supported biome validation, conservative spawn-area validation, fixed one-guaranteed-crystal geode contract, finite 0..1 additional-crystal probabilities, positive finite elemental weights, and duplicate identity rejection.

The shipped foundation snapshot defines the Meadows Earth vertical slice: one guaranteed Earth crystal plus independent 35% and 10% additional-crystal probabilities.

## Controlled balance overrides — 2026-09-13

Runtime version `0.0.5` adds a constrained BepInEx configuration layer over the validated baseline snapshot.

Permitted runtime balance overrides are limited to:

- refinement base failure chance;
- refinement minimum Crystal Shaping skill;
- refinement failure shard return count;
- geode second-crystal probability;
- geode third-crystal probability;
- relative weights of elemental identities already present in that geode definition.

The override layer cannot alter crystal tier topology, refinement source/destination identities, required stations, geode IDs, biome ownership, prefab identities, guaranteed-crystal count, or add/remove/replace elemental identities. Unknown refinement/geode override targets reject. Effective definitions are rebuilt through `MagenheimDefinitionValidator.ValidateAndFreeze`, so invalid configured values fail admission and the effective gameplay fingerprint is regenerated from normalized validated data.

Plugin startup now logs both baseline and effective fingerprints. Gameplay mutation remains disabled, so client/server fingerprint enforcement can be implemented before any divergent configuration is allowed to affect inventory, sockets, or world state.

## Validation boundary

The active execution host still does not expose `dotnet`, `csc`, or `mcs`. Compilation and test execution therefore remain unclaimed. Plugin startup, BepInEx config binding, definition override execution inside Valheim, and actual world generation have not been directly observed in Valheim.

## Next exact action

In a .NET 8 SDK / Valheim development environment:

1. run `dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`;
2. compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` against Jötunn 2.30.0 and current Valheim dependencies;
3. repair any compile/test defect at the authoritative source;
4. add deterministic tests covering definition-validation failures and override admission/rejection/fingerprint changes;
5. implement server-authoritative definition fingerprint synchronization/enforcement before gameplay mutation;
6. register Crystal Shaping under permanent ID `magenheim.crystal_shaping` only after the runtime project passes its compile gate;
7. then bind the pure worldgen plan to a thin Jötunn registrar with explicit `Heightmap.BiomeArea` mapping and repeated-load idempotence checks.
