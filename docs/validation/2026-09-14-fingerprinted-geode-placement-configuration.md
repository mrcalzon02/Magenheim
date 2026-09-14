# Fingerprinted Geode Placement Configuration — 2026-09-14

## Scope

This pass removes the temporary hard-coded Meadows geode placement constants from the runtime registrar and moves placement behavior into the same validated/fingerprinted definition authority used for geode identity, biome, area, cracking rules, and worldgen compatibility.

## Schema 3

Definition schema 3 adds a required `placement` object to each geode definition. The authority now covers:

- block checking and force-placement behavior;
- minimum/maximum per-zone placement values;
- altitude and ocean-depth limits;
- terrain-delta limits and measurement radius;
- minimum/maximum tilt;
- forest-fractal enablement and thresholds;
- minimum/maximum scale;
- group-size limits and radius;
- ground offset.

The full placement record is included in the SHA-256 definition fingerprint. A server/client difference in any placement field therefore changes definition authority rather than silently generating different worlds.

## Validation

`GeodePlacementDefinition.Validate` rejects non-finite values, values that cannot be represented by Jötunn's float-facing runtime API, negative placement counts/chances, reversed numeric ranges, negative terrain-delta radius/group radius, tilt outside 0..90 degrees, non-positive scale, and invalid group sizes.

The validated snapshot carries the placement record with the geode definition. `GeodeWorldgenRegistrar` consumes that record directly when constructing `VegetationConfig`; it no longer owns density, terrain, scale, or group constants.

## Configurability

BepInEx now exposes placement overrides under `Worldgen.Placement.<geode id>`. Overrides cover every fingerprinted placement field. They are not applied directly to Jötunn. They are converted to a `GeodePlacementDefinition`, routed through `MagenheimDefinitionOverrideApplier`, and then revalidated/re-fingerprinted by `MagenheimDefinitionValidator.ValidateAndFreeze` before runtime services or worldgen registration consume them.

This preserves server-authoritative configurability without introducing a second runtime configuration authority.

## Deterministic source coverage

`GeodePlacementDefinitionTests` covers preservation of the Meadows baseline, fingerprint changes when placement changes, rejection of tilt beyond runtime bounds, rejection of values that cannot fit the runtime float API, and propagation of a placement override through the validated override path.

`DefinitionAuthorityTests` now derives its descriptor schema from `MagenheimDefinitionValidator.CurrentSchemaVersion` rather than embedding schema 2, preventing deterministic authority tests from becoming stale on future schema increments.

## Runtime boundary

No compiler is available in this execution host, so schema-3 compilation, JSON loading, BepInEx binding, package restore, Valheim startup, natural generation, and multiplayer authority synchronization have not been executed here. Runtime admission remains pending those checks.
