# Definition-driven worldgen planning validation — 2026-09-14

## Target

Close the authority gap between validated geode definitions and the eventual Jötunn registrar without creating a second runtime worldgen model.

Starting `main`: `c087817e3c9d76175130977099c8bc55fe3cfe39`.

Implementation commit: `c0eaea435c381579719752fd49d861b8302e74fa` (`Carry authoritative biome into worldgen planning`).

## Defect found

`GeodeDefinition` owns the registration id, biome, prefab, and validated spawn area, but `DesiredWorldgenAddition` previously carried only registration id, prefab, and area. A future runtime registrar would therefore have been forced to reconstruct biome selection outside the authoritative definition snapshot. That would create a second worldgen authority and make compatibility configuration easier to apply inconsistently.

## Repair

- `DesiredWorldgenAddition` now carries an optional case-preserving `Biome` field.
- `WorldgenAdditionPlanner` rejects supplied empty/whitespace biome values rather than forwarding malformed placement data.
- New `DefinitionWorldgenPlanner` converts the validated `MagenheimDefinitionSet.Geodes` directly into worldgen additions, preserving id, prefab, biome, and normalized area.
- The planner always consumes `definitions.WorldgenCompatibility`; runtime callers no longer need to reconstruct or separately select the effective compatibility policy.
- Existing non-definition callers remain source-compatible because the original three positional `DesiredWorldgenAddition` parameters are unchanged and `Biome` is additive metadata.

## Deterministic source coverage added

`DefinitionWorldgenPlannerTests` is automatically executed by the existing net8 console test assembly through a module initializer and covers:

1. authoritative registration key preservation;
2. authoritative prefab preservation;
3. biome preservation (`Meadows`);
4. validated area preservation;
5. definition-owned exclusion policy propagation;
6. non-destructive observed prefab collision behavior;
7. case-insensitive identity policy propagation;
8. invalid-area fallback normalization remaining authoritative through planning.

## Current Jötunn evidence

Current Jötunn documentation states that custom vegetation uses `VegetationConfig` plus `CustomVegetation`, `VegetationConfig.BiomeArea` is a `Heightmap.BiomeArea`, and custom vegetation should be added only once while vanilla modifications repeat per world load. ZoneManager exposes `AddCustomVegetation`, and vegetation is appropriate for scattered rock/ore-style world objects.

This supports the existing design direction, but no Jötunn registration code is admitted by this validation record yet. The geode prefab/item is not registered, so adding a registrar that fabricates or clones a foreign prefab would violate the non-destructive/source-authority rules.

## Validation boundary

Static source/API review only. This execution host still has no .NET SDK/compiler and no Valheim runtime. No compilation, deterministic test pass, plugin startup, `CustomVegetation` registration, natural geode generation, or repeated-world-load idempotence result is claimed.

## Next exact action

1. In a .NET 8 / Valheim development environment, execute the full pure-core harness and compile the runtime project.
2. Register the intact Magenheim Meadows geode prefab/item under Magenheim ownership.
3. Add a thin Jötunn vegetation registrar that consumes only `DefinitionWorldgenPlanner` additions, explicitly maps `Meadows` and `SpawnArea` to current Valheim enums, and calls `ZoneManager.Instance.AddCustomVegetation` once per custom entry.
4. Snapshot observed registrations before planning and verify exclusion/collision behavior without modifying foreign vegetation.
5. Exercise repeated world loads and prove no duplicate Magenheim additions are created.
