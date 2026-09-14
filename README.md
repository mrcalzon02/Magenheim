# Magenheim

**Magic begins as geology.**

Magenheim is a Valheim mod built around biome geodes, elemental crystals, the Crystal Shaping skill, risky refinement, and adaptive non-destructive equipment socketing.

## Verified implementation state

The repository is still a development foundation, not a runtime-validated playable release. The authoritative and only live branch is `main`.

Implemented in source:

- dependency-free `Magenheim.Core` deterministic rules targeting `netstandard2.0`;
- canonical Rough -> Simple -> Crystal -> Advanced -> Master refinement progression;
- schema-versioned/fingerprinted refinement, geode, compatibility, and geode-placement definitions;
- strict static definition loading plus validated server configuration overrides;
- definition-authority synchronization and session-scoped geode-operation replay protection;
- deterministic geode cracking and authority-gated geode-opening transaction planning;
- definition-driven intact geode item registration and dedicated `<item prefab>_World` mineable prefab identities;
- additive-only worldgen planning with invalid-area validation, collision observation, Magenheim-only exclusions, and exact/case-insensitive identity policy;
- a Jötunn runtime adapter that resolves validated biomes/areas without enum-integer assumptions;
- a one-time additive `CustomVegetation` path for approved Magenheim geode world objects, with no foreign/vanilla mutation path;
- schema-3 server-configurable placement density, terrain, scale, grouping, forest, and offset controls that are revalidated and included in multiplayer definition authority before registration.

Runtime source version `0.0.13` consumes `default-data/foundation.json` schema 3. Natural geode placement flows through validated definitions -> read-only host observation -> pure Add/Skip/Error planning -> preflight -> `ZoneManager.AddCustomVegetation` for approved Magenheim-owned additions only.

The old separate world-prefab registration path was removed because Jötunn's vegetation API already registers the supplied prefab. The area adapter also resolves the combined runtime `BiomeArea` value from `Everything` or `Everywhere`, avoiding a version-specific spelling dependency.

## Meadows/Earth vertical slice

The shipped foundation defines `magenheim.geode.meadows.earth`. Opening produces one guaranteed Rough Earth crystal with independent 35% and 10% chances for second and third crystals.

The natural world object is `Magenheim_Geode_Meadows_Earth_World`; the intact inventory item remains `Magenheim_Geode_Meadows_Earth`. The temporary inventory visual still derives from vanilla Stone until the custom geode asset is runtime-validated. The temporary item visual is never registered directly as world vegetation.

The initial fingerprinted Meadows placement profile uses block checking, no force placement, a 35% maximum single-group chance per zone, altitude 1–1000, terrain delta <=2 over radius 2, tilt <=35 degrees, scale 0.85–1.15, one-object groups, and ground offset -0.10. Server overrides for these and the other Jötunn placement fields are admitted only after pure validation and fingerprint regeneration.

## Validation boundary

This execution environment has no .NET SDK/compiler. Source/API/Git reconciliation is therefore the present admission level. The deterministic test harness source has expanded with placement-authority coverage but has not been executed here. Runtime package restore, compilation, Valheim startup, schema-3 loading, natural generation, repeated-load idempotence, multiplayer behavior, destruction/drop behavior, and persistence remain unverified.

## Immediate target

Compile and run the deterministic suite, compile the runtime against current Valheim/Jötunn, then validate the Meadows geode in a disposable world: one additive vegetation registration, no duplicate registration across world loads, placement overrides synchronized by definition fingerprint, no mutation of occupied foreign identities or vanilla `Rock_4`, persistent network behavior, and exactly one intact-geode destruction drop. After that, continue the playable loop with Crystal Shaping registration, the Geologist's Workstation, Earth crystal/shard items, and authority-gated atomic inventory transactions.
