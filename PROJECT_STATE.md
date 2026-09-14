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

Implementation commit `22c4a6dd8a0400d8f53a888af7a1d50da422df6f` hardens the pure worldgen compatibility layer.

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

## Runtime bootstrap — 2026-09-13

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

## Strict definition pipeline — 2026-09-13

The runtime no longer constructs refinement rules from hard-coded defaults at startup. It loads `default-data/foundation.json`, converts the document into pure-core definition records, validates the complete snapshot, computes a deterministic SHA-256 fingerprint, and only then creates runtime services.

Definition admission rules now include:

- exact schema version match;
- unknown JSON members rejected;
- duplicate JSON properties rejected;
- required fields cannot be omitted or null;
- exactly four canonical one-tier refinement transitions must exist;
- geode ids must use `magenheim.geode.` and prefab names must use `Magenheim_`;
- biome references are restricted to Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, Ashlands, and DeepNorth;
- spawn areas must validate through the conservative pure-core area validator;
- geode output currently requires one guaranteed crystal plus finite 0..1 second/third crystal probabilities;
- elemental weights must be positive, finite, non-empty, and unique per element;
- duplicate geode ids and duplicate prefab identities reject the snapshot;
- invalid definitions fail plugin bootstrap rather than falling back silently to a second hard-coded ruleset.

The shipped foundation snapshot defines the Meadows Earth vertical slice: one guaranteed Earth crystal plus independent 35% and 10% additional-crystal probabilities. Its fingerprint is intentionally produced from normalized validated definitions so future server/client synchronization can compare gameplay authority independent of JSON ordering.

## Validation boundary

The active execution host still does not expose `dotnet`, `csc`, or `mcs`. Compilation and test execution therefore remain unclaimed. Runtime source and worldgen compatibility assumptions were checked against current Jötunn documentation, but plugin startup, static-definition loading inside Valheim, and actual world generation have not been observed inside Valheim.

## Next exact action

In a .NET 8 SDK / Valheim development environment:

1. run `dotnet run --project tests/Magenheim.Core.Tests/Magenheim.Core.Tests.csproj`;
2. compile `src/Magenheim.Runtime/Magenheim.Runtime.csproj` against Jötunn 2.30.0 and current Valheim dependencies;
3. repair any compile/test defect at the authoritative source;
4. add deterministic tests for definition validation/loading failure cases;
5. implement controlled server configuration overrides that revalidate and regenerate the definition fingerprint;
6. register Crystal Shaping under permanent ID `magenheim.crystal_shaping` only after the runtime project passes its compile gate;
7. then bind the pure worldgen plan to a thin Jötunn registrar with explicit `Heightmap.BiomeArea` mapping and repeated-load idempotence checks.
