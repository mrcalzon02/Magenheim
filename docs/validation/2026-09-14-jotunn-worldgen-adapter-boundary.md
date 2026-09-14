# Jötunn Worldgen Adapter Boundary — 2026-09-14

## Intent

Close the runtime translation gap between validated Magenheim worldgen intent and Valheim/Jötunn without prematurely registering vegetation or introducing a second worldgen authority.

## Defect examined

The pure core already validates biome names and the abstract spawn areas `Median`, `Edge`, and `All`, but the runtime had no explicit translation boundary. A future registrar could therefore be tempted to cast enum integers or reconstruct biome/area policy ad hoc. That would make area generation dependent on implementation coincidence rather than authoritative definitions.

The compatibility planner also accepted observed host registrations, but there was no runtime read-only observation helper for checking whether a desired prefab identity already exists in `ZoneManager` before planning an addition.

## Implemented boundary

`src/Magenheim.Runtime/JotunnWorldgenAdapter.cs` now provides two deliberately non-mutating responsibilities:

1. explicit mapping of validated Magenheim biome names to the current `Heightmap.Biome` enum by defined runtime name, including the `Ashlands` / `AshLands` compatibility alias;
2. explicit mapping of `SpawnArea.Median`, `SpawnArea.Edge`, and `SpawnArea.All` to `Heightmap.BiomeArea.Median`, `Edge`, and `Everywhere` respectively.

Unsupported or unnormalized values throw instead of falling through to an integer cast.

The same adapter can observe only the vegetation prefab identities Magenheim intends to add through `ZoneManager.Instance.GetZoneVegetation`. Existing host entries are returned as `ObservedWorldgenRegistration` records for the pure planner. The observation path does not call any Jötunn add/remove/modify API and does not mutate the returned host vegetation object.

## Compatibility properties

- additive-only policy remains owned by `Magenheim.Core`;
- runtime observation is scoped to desired Magenheim prefab names rather than sweeping and rewriting host tables;
- existing vegetation is represented to the planner as `host-existing-vegetation` with no invented Magenheim registration key;
- registration-key collision authority remains definition/internal-planner owned;
- prefab collision authority can now consume directly observed host presence;
- no vanilla or foreign vegetation is removed, modified, disabled, reordered, or cloned by this slice.

## Current Jötunn contract review

Current Jötunn documentation states that vegetation is used for rocks and ore-like singular world objects, that `BiomeArea` distinguishes biome middle/edge placement, and that custom vegetation must be added only once. `VegetationConfig.BiomeArea` defaults to `Everywhere`, while Magenheim deliberately maps its own `All` abstraction to that runtime enum member instead of relying on numeric equivalence.

## Validation boundary

Static source/API review only. This execution host does not provide `dotnet`, `csc`, or `mcs`; therefore compile success, API binding, Valheim startup, and world-generation behavior are not claimed.

The adapter does **not** register the current geode item prefab as vegetation. The presently registered geode item uses a temporary vanilla `Stone` item visual and is not yet an authoritative mineable world-object prefab. World placement remains blocked behind a proper Magenheim world geode prefab or another explicitly validated world-object source.

## Next exact action

In a current Valheim/Jötunn development environment, compile the runtime adapter and verify the current `Heightmap.Biome` names. Then create the Magenheim-owned mineable geode world prefab, feed desired definitions through `DefinitionWorldgenPlanner` plus `JotunnWorldgenAdapter.ObserveDesiredVegetation`, and register only approved additions once through `ZoneManager.AddCustomVegetation`.
