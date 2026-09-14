# Magenheim Project State

## Authority

`INSTRUCTIONS.md`, committed source on the live `main` branch, and directly observed repository/runtime evidence are authoritative. Backlog, validation records, archived design, scheduled prompts, and conversation are subordinate when they disagree with verified live state.

## Repository state — 2026-09-14

Repository: `mrcalzon02/Magenheim`.

GitHub now reports `main` as the default branch and branch enumeration reports only `main`. The legacy `master` compatibility pointer has been removed, so there is no remaining parallel development branch to synchronize or prune.

The additive geode vegetation implementation was committed to `main` as `e1b530804591004717f20d6c361b7d150421eb9a` (`Register geodes through additive Jotunn vegetation path`).

## Stable domain authority

Crystal tiers remain:

`Rough -> Simple -> Crystal -> Advanced -> Master`

Normal elemental alignments remain Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit. Ordinary refinement preserves alignment. Base failure remains 10/20/30/40 percent with Crystal Shaping reduction and workstation/upgrade progression. Valid failed attempts destroy the source and return 1/2/3/5 matching shards by tier.

## Definition and compatibility authority

Definition schema 2 owns refinement, geodes, and worldgen compatibility in one validated/fingerprinted snapshot. Compatibility settings may control invalid-area behavior, duplicate behavior, prefab-collision detection, exact/case-insensitive identity comparison, and exclusions limited to Magenheim-owned identities.

Worldgen remains structurally additive-only. Vanilla and foreign registrations are observations for collision planning; compatibility code cannot delete, rewrite, disable, reorder, or replace them.

Abstract spawn areas are Median, Edge, and All. Negative numeric flags fail closed under Reject/Clamp behavior, unknown bits are policy-governed, and textual configuration accepts `Everywhere` as an All alias. Runtime mapping does not cast enum integers.

## Additive geode worldgen path — runtime source 0.0.12

`GeodeWorldgenRegistrar` is now the single authoritative runtime mutation path for natural geode placement. The prior separate `GeodeWorldPrefabRegistrar` was removed because Jötunn `ZoneManager.AddCustomVegetation` already registers the supplied prefab through `PrefabManager`; pre-registering the same prefab created a self-collision risk.

The one-time registration flow is:

1. wait for Jötunn vanilla prefabs;
2. derive desired worldgen additions from the validated definition snapshot;
3. observe only desired host vegetation/prefab identities without mutation;
4. rebuild the pure `DefinitionWorldgenPlanner` against those observations and the effective compatibility policy;
5. abort before mutation for Error decisions and log/leave host data untouched for Skip decisions;
6. preflight all approved additions before the first registration;
7. clone/configure the dedicated `<item prefab>_World` object from `Rock_4`;
8. register that new Magenheim-owned object exactly once through `ZoneManager.AddCustomVegetation`;
9. unsubscribe after the one-time registration pass.

The dedicated world object is separate from the temporary Stone-backed intact inventory-item visual and is configured for persistent network state, destructibility, and exactly one intact-geode destruction drop. No code edits vanilla `Rock_4` itself.

Host observation now checks both ZoneManager vegetation and the broader PrefabManager namespace because a prefab occupied outside vegetation is still unsafe for `AddCustomVegetation` to claim. Even if configurable proactive prefab-collision reporting is disabled, the execution preflight still refuses replacement of an existing host prefab identity; configurability may reduce diagnostics but cannot weaken the non-destructive invariant.

## Area mapping repair

Current Jötunn source uses `Heightmap.BiomeArea.Everything` in `VegetationConfig`, while other Jötunn documentation/API surfaces refer to the combined value as `Everywhere`. `JotunnWorldgenAdapter` now resolves the runtime combined enum dynamically from `Everything` or `Everywhere`; Median and Edge use the same defined-enum check. Unsupported runtime values fail closed.

## Initial placement profile

The first Meadows/Earth source implementation centralizes a conservative placement profile: block checking enabled, no force placement, one-object groups, 35% maximum single-group chance per zone, altitude 1–1000, terrain-delta maximum 2 over radius 2, maximum tilt 35 degrees, scale 0.85–1.15, and ground offset -0.10.

These placement numbers are not yet exposed as free client configuration. Before density/terrain placement becomes user-configurable, it should move into the schema-versioned/fingerprinted definition authority so peers cannot silently disagree about generation behavior.

## Other runtime authority

Runtime target remains .NET Framework 4.6.2 with `JotunnLib` 2.30.0. BepInEx plugin GUID is `mrcalzon02.magenheim`; network compatibility is `EveryoneMustHaveMod` with patch strictness. Definition-authority synchronization and session-scoped geode-operation replay protection remain in source. Persistent socket/refinement/inventory mutation remains gated pending runtime validation and transaction binding.

## Validation boundary

This execution host still exposes no `dotnet`, `csc`, or `mcs`, so compilation and deterministic test execution are not claimed. The Jötunn API surface used by the new registrar was checked against current Jötunn source, including the `CustomVegetation(GameObject, bool, VegetationConfig)` constructor, `VegetationConfig` fields, and `ZoneManager.AddCustomVegetation` registering the prefab through `PrefabManager`.

Valheim startup, natural geode placement, repeated-world-load idempotence, multiplayer replication, destruction/drop behavior, and proof that the vanilla `Rock_4` runtime object remains unchanged are still runtime acceptance gates.

## Next exact action

In a current .NET/Valheim development environment:

1. run the pure-core test harness;
2. compile `Magenheim.Runtime` against Jötunn 2.30.0/current Valheim assemblies and repair any API defect at source;
3. launch a disposable world and verify exactly one Magenheim custom vegetation definition is registered per geode identity;
4. verify repeated world loads do not duplicate registrations;
5. verify an occupied foreign prefab is skipped/errored by policy without mutation;
6. mine a naturally generated Meadows geode and verify persistent network behavior plus exactly one intact-geode drop;
7. verify vanilla `Rock_4` remains unchanged;
8. then move placement density/terrain parameters into fingerprinted definition authority before exposing placement configurability.
