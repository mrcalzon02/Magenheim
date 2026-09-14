# Validation — Definition-Driven Geode Item Registration

Date: 2026-09-14

## Reconciled baseline

Authoritative `main` already contained validated schema-2 geode authority, Meadows/Earth geode definition `magenheim.geode.meadows.earth`, prefab identity `Magenheim_Geode_Meadows_Earth`, deterministic cracking, authority-gated transaction planning, and session-scoped exact-once operation admission. Runtime inventory mutation and world spawning remained disabled.

## Implemented slice

Runtime 0.0.10 adds `GeodeItemRegistrar` as a thin Jötunn adapter over the validated `MagenheimDefinitionSet`.

The registrar:

- subscribes to `PrefabManager.OnVanillaPrefabsAvailable` before cloning a vanilla resource;
- derives every geode prefab identity from validated definition authority instead of maintaining a second runtime list;
- creates each geode item through Jötunn `CustomItem` and registers it through `ItemManager`;
- rejects a prefab identity already registered as a Jötunn custom item rather than silently replacing it;
- configures the geode as a material resource with stack size 20, weight 2, and no coin value;
- unsubscribes after the one registration pass and also exposes disposal from plugin shutdown;
- creates no recipe and performs no cracking, inventory consumption, world placement, socket mutation, or refinement mutation.

The initial visual source is deliberately the vanilla `Stone` prefab. This is only a bootstrap visual so the canonical Magenheim prefab can exist before a custom asset bundle is produced and runtime-tested. The asset target is recorded in `docs/assets/MEADOWS_EARTH_GEODE_ASSET_SPEC.md`.

## Compatibility invariants

The adapter does not replace or edit the vanilla Stone prefab. Jötunn performs a clone into the Magenheim-owned prefab name. Existing foreign custom items are not modified; a conflicting Magenheim prefab identity fails registration visibly.

Worldgen compatibility exclusions are not used to suppress item registration. A geode may be excluded from spawning while its item prefab remains available for existing saves, inventories, commands, or later acquisition paths.

## Source/API review

Current Jötunn documentation was checked during this cycle. Its item tutorial and `CustomItem` API continue to document cloning an existing vanilla prefab by name and registering through `ItemManager`, and recommend `PrefabManager.OnVanillaPrefabsAvailable` when cloning instantiated vanilla assets.

## Validation boundary

This environment still does not provide the required .NET/Valheim runtime toolchain. Therefore this record claims source implementation and API-shape review only. It does not claim successful compilation, Jötunn registration, ObjectDB admission, item spawning, networking, save/load behavior, or visual correctness in game.

## Next dependency-valid action

In a current Valheim/Jötunn development environment:

1. run the complete `Magenheim.Core` test harness;
2. compile `Magenheim.Runtime` and resolve any Jötunn/Valheim API defect in `GeodeItemRegistrar` at source;
3. launch a disposable world and confirm `Magenheim_Geode_Meadows_Earth` registers exactly once and can be spawned without altering vanilla Stone;
4. verify host/client and dedicated-server ObjectDB consistency under patch-strict network admission;
5. once the runtime compile/admission gate passes, register Crystal Shaping under `magenheim.crystal_shaping` and bind the additive worldgen planner to Jötunn with explicit `Heightmap.BiomeArea` mapping;
6. replace the Stone placeholder only after the custom Meadows Earth geode mesh/texture asset passes visual and prefab validation.
