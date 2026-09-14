# Magenheim

**Magic begins as geology.**

Magenheim is a Valheim mod built around biome geodes, elemental crystals, the Crystal Shaping skill, risky refinement, and adaptive non-destructive equipment socketing.

## Verified implementation state

The repository is still a development foundation, not a validated playable release. The authoritative and only live branch is `main`.

Implemented in source:

- project authority, state, backlog, implementation, validation, and design records;
- dependency-free `Magenheim.Core` deterministic rules targeting `netstandard2.0`;
- canonical Rough -> Simple -> Crystal -> Advanced -> Master refinement progression;
- elemental identities Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit;
- schema-versioned/fingerprinted refinement, geode, and worldgen compatibility definitions;
- strict definition loading plus validated server configuration overrides;
- definition-authority synchronization and session-scoped geode-operation replay protection;
- deterministic geode cracking and authority-gated geode-opening transaction planning;
- definition-driven intact geode item registration and dedicated `<item prefab>_World` mineable prefab identities;
- additive-only worldgen planning with invalid-area validation, collision observation, configurable Magenheim-only exclusions, and exact/case-insensitive identity policies;
- a Jötunn runtime adapter that maps validated biomes/areas without depending on enum integer coincidence;
- a one-time additive `CustomVegetation` registration path for approved Magenheim geode world objects, with no foreign/vanilla worldgen mutation path.

Runtime source version `0.0.12` consumes `default-data/foundation.json` and the effective validated compatibility policy. Natural geode placement source is now wired through `DefinitionWorldgenPlanner` -> read-only host observation -> final Add/Skip/Error planning -> `ZoneManager.AddCustomVegetation` for approved Magenheim-owned additions only.

The separate world-prefab registration path was removed because Jötunn's `AddCustomVegetation` already registers its prefab. Keeping both would risk a Magenheim self-collision. The area adapter also tolerates the current Jötunn `BiomeArea` combined-value naming difference (`Everything` / `Everywhere`) by resolving the defined runtime enum explicitly.

## Meadows/Earth vertical slice

The current shipped foundation defines `magenheim.geode.meadows.earth`. It produces one guaranteed Rough Earth crystal with independent 35% and 10% chances for second and third crystals when opened. The dedicated natural world-object identity is derived from the intact item prefab as `Magenheim_Geode_Meadows_Earth_World`.

The temporary intact inventory item still uses a vanilla Stone-derived visual until the custom geode asset is runtime-validated. That placeholder is not registered directly as world vegetation.

## Validation boundary

The current execution environment has no .NET SDK/compiler. Source/API/Git reconciliation is therefore the present admission level. The deterministic test harness has not been executed here, and runtime package restore, compilation, Valheim startup, natural generation, repeated-load idempotence, multiplayer behavior, destruction/drop behavior, and persistence remain unverified.

A detailed pre-reconciliation design record is preserved under `docs/archive/`; current project instructions and `docs/MAGENHEIM_DESIGN_SPEC.md` control where archived material conflicts with live authority.

## Immediate target

Compile and run the deterministic suite, compile the runtime against current Valheim/Jötunn, then validate the Meadows geode in a disposable world: one additive vegetation registration, no duplication across world loads, no mutation of occupied foreign identities or vanilla `Rock_4`, persistent network behavior, and exactly one intact-geode destruction drop. Placement-density and terrain parameters should then move into fingerprinted definition authority before user-facing placement configurability expands.
