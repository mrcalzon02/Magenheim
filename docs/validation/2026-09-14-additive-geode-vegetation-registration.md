# Additive Geode Vegetation Registration — 2026-09-14

## Scope

This pass binds the validated definition-driven worldgen planner to a single Jötunn vegetation registration path for Magenheim geodes. The change is intentionally limited to additive registration of new Magenheim-owned vegetation. It does not modify, remove, disable, reorder, or replace vanilla or foreign worldgen entries.

## Root-cause repairs

Two integration defects were identified during API reconciliation.

First, the previous `GeodeWorldPrefabRegistrar` explicitly called `PrefabManager.AddPrefab` for the dedicated world object. Current Jötunn `ZoneManager.AddCustomVegetation` also registers the supplied vegetation prefab through `PrefabManager`. Registering the prefab separately therefore risks making the later vegetation addition collide with Magenheim's own already-registered prefab. The two-step registration path has been collapsed into one authoritative `GeodeWorldgenRegistrar`: it creates/configures the world prefab, then hands that unregistered prefab directly to `ZoneManager.AddCustomVegetation`.

Second, Magenheim's runtime area adapter compiled directly against `Heightmap.BiomeArea.Everywhere`, while current Jötunn source uses `Heightmap.BiomeArea.Everything` in `VegetationConfig` and other documentation still refers to `Everywhere`. `JotunnWorldgenAdapter.MapArea` now resolves the combined runtime value dynamically from `Everything` or `Everywhere`, while Median and Edge are resolved through the same checked enum path. This removes a version-spelling compile/runtime dependency without weakening fail-closed behavior.

## Registration flow

1. Wait for Jötunn vanilla prefabs to become available.
2. Build the desired additions from the validated/fingerprinted Magenheim definition snapshot.
3. Observe only the host prefab/vegetation identities Magenheim intends to add.
4. Re-run the pure `DefinitionWorldgenPlanner` with those observations and the effective compatibility policy.
5. Abort before mutation if the plan contains an Error decision.
6. Log and leave host content untouched for Skip decisions.
7. Preflight all approved additions: intact item present, base rock present, no host prefab identity collision, biome and area mappings valid.
8. Clone/configure the Magenheim-owned world prefab and register it exactly once through `ZoneManager.AddCustomVegetation`.
9. Unsubscribe the registrar after the one-time Jötunn custom-vegetation registration pass.

The registrar also checks the broader `PrefabManager` namespace in addition to ZoneManager vegetation. This is necessary because Jötunn's vegetation addition registers the prefab itself, so a non-vegetation prefab collision is still an unsafe identity collision.

## Placement profile

The first Meadows/Earth vertical slice uses a conservative centralized runtime profile pending the next definition-schema placement pass: one-object groups, block checking enabled, no force placement, 35% maximum single-group chance per zone, altitude 1–1000, terrain delta <= 2 over radius 2, tilt <= 35 degrees, scale 0.85–1.15, and a -0.10 ground offset.

These values are intentionally centralized rather than exposed as unsynchronized ad-hoc client config. A later schema revision should move placement density/terrain values into the fingerprinted geode definition authority before they become user-configurable.

## Static verification

- repository default branch is `main`;
- branch enumeration reports only `main`;
- Jötunn `CustomVegetation(GameObject, bool, VegetationConfig)` constructor signature was checked against current source;
- Jötunn `ZoneManager.AddCustomVegetation` was checked and confirmed to call `PrefabManager.AddPrefab` internally;
- Jötunn `VegetationConfig` property names used by the registrar were checked against current source;
- Magenheim no longer needs a separate prefab-registration step for world geodes;
- the pure planner remains the only authority deciding Add/Skip/Error;
- host observations remain read-only.

## Runtime boundary

No compilation, package restore, Valheim startup, world creation, natural geode generation, host/client replication, or destruction/drop behavior was executed in this environment because no .NET compiler/runtime toolchain is available. Those checks remain required before runtime admission.
