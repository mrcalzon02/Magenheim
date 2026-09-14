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
- occupied Magenheim registration keys and prefab identities are collision-checked without changing observed host data;
- specific Magenheim registration keys or prefabs may be excluded through compatibility policy;
- identity matching is exact by default with optional case-insensitive comparison;
- destructive compatibility mode remains structurally rejected.

Definition schema 2 makes this compatibility policy part of the same validated gameplay snapshot as refinement and geodes. The fingerprint includes invalid-area behavior, duplicate-registration behavior, prefab collision detection, identity comparison, and Magenheim-only exclusion sets. Exclusions outside `magenheim.` registration keys or `Magenheim_` prefabs reject validation.

The runtime loader parses compatibility policy before geodes so configured invalid-area behavior governs geode area parsing. The eventual Jötunn adapter must still map pure `SpawnArea` values explicitly to current `Heightmap.BiomeArea` values rather than casting enum integers.

## Runtime bootstrap

A thin runtime project exists at `src/Magenheim.Runtime` and consumes the authoritative pure-core project rather than recreating refinement rules.

Runtime bootstrap properties:

- target framework: .NET Framework 4.6.2;
- Jötunn dependency: `JotunnLib` 2.30.0;
- BepInEx plugin GUID: `mrcalzon02.magenheim`;
- hard dependency on `Jotunn.Main.ModGuid`;
- Jötunn network compatibility: `EveryoneMustHaveMod` with `VersionStrictness.Patch`;
- runtime startup consumes the validated core definition snapshot;
- no world objects, items, sockets, inventory mutations, or persistent gameplay state are enabled by this bootstrap.

`Magenheim.Core` targets `netstandard2.0` so it can remain consumable by both the net462 runtime and the net8.0 standalone test harness.

## Strict definition pipeline

The runtime loads `default-data/foundation.json`, converts it into pure-core definition records, validates the complete snapshot, computes a deterministic SHA-256 fingerprint, and only then creates runtime services.

Definition schema is version 2. Version-1 files are intentionally rejected because they do not carry fingerprinted worldgen compatibility policy.

The shipped foundation snapshot defines the Meadows Earth vertical slice: one guaranteed Earth crystal plus independent 35% and 10% additional-crystal probabilities. Default worldgen compatibility remains conservative: Reject invalid areas, Skip occupied additions, detect prefab collisions, exact identity comparison, and no exclusions.

## Controlled runtime overrides

Runtime balance and compatibility settings use the validated BepInEx override path.

Permitted balance overrides remain limited to existing refinement/geode balance fields and cannot alter topology, ownership, prefab identity, guaranteed-crystal count, or elemental identity sets.

Worldgen compatibility overrides may change only the validated policy fields: invalid-area behavior, duplicate handling, prefab collision detection, identity comparison, and Magenheim-owned exclusions. They cannot disable additive-only behavior or target foreign identities. The override applier rebuilds the effective snapshot through `MagenheimDefinitionValidator.ValidateAndFreeze`, so invalid settings fail admission and the effective fingerprint changes when compatibility policy changes.

Plugin startup logs both baseline and effective fingerprints.

## Definition-authority synchronization — runtime 0.0.8

A two-way Jötunn `CustomRPC` definition-authority handshake is registered during initial synchronization. The server sends its effective schema and fingerprint before world admission. The client compares that authority against its own effective definition snapshot. The client then acknowledges by echoing the exact server descriptor it successfully parsed and by sending its own descriptor.

The server admits a peer to future Magenheim gameplay mutation only when both conditions hold:

1. the echoed server descriptor exactly matches the server's current authority;
2. the client's own schema/fingerprint exactly matches the server authority.

This closes a fail-closed defect in the earlier 0.0.7 protocol, where the client sent an acknowledgement even after failing to parse the server package and the server could authorize solely from the client's self-reported descriptor. In 0.0.8 malformed/unreadable server authority produces no acknowledgement, leaving server-side mutation admission false. Schema/fingerprint mismatch remains non-authorizing.

No inventory, socket, geode-opening, refinement, or other persistent gameplay transaction consumes the admission gate yet; those mutations remain disabled.

## Validation boundary

The active execution host still does not expose `dotnet`, `csc`, or `mcs`. Compilation and test execution therefore remain unclaimed. Jötunn 2.30.0 documentation was checked for the current `SynchronizationManager.AddInitialSynchronization(CustomRPC, Func<ZPackage>)` contract and `CustomRPC` coroutine receive model; the source shape remains consistent with those documented interfaces, but that is not a substitute for compilation or in-game execution.

## Next exact action

In a .NET 8 SDK / current Valheim development environment:

1. run `dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`;
2. compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` against Jötunn 2.30.0 and current Valheim dependencies;
3. execute the 0.0.8 authority synchronization on host/client and dedicated server, including malformed, schema-mismatch, fingerprint-mismatch, and matching-authority cases;
4. repair any compile/API/serialization defect at the authoritative source;
5. register Crystal Shaping under permanent ID `magenheim.crystal_shaping` only after the runtime compile gate passes;
6. then bind the pure worldgen plan to a thin Jötunn registrar using the effective compatibility policy, explicit `Heightmap.BiomeArea` mapping, and repeated-load idempotence checks.
