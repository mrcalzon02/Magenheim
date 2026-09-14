# Magenheim Changelog

## Unreleased — lifecycle-safe worldgen observation

- Removed the `ZoneManager.GetZoneVegetation` lookup from the `OnVanillaPrefabsAvailable` collision-observation path after current Jötunn source review showed that method dereferences `ZoneSystem.instance` when no custom vegetation match exists.
- Collision observation now uses `PrefabManager.GetPrefab`, the same prefab namespace Jötunn `AddCustomVegetation` must claim through `PrefabManager.AddPrefab`.
- Preserved the preflight refusal of occupied prefab identities, so removing the premature ZoneSystem lookup does not weaken non-destructive behavior.
- Advanced runtime source/package identity to 0.0.14 under patch-strict network compatibility.
- Added a dedicated lifecycle-safety validation record; runtime menu/world-transition behavior remains unverified until exercised in Valheim.

## Unreleased — fingerprinted geode placement authority

- Advanced the definition schema to version 3 and made complete geode vegetation placement behavior part of the validated/fingerprinted authority snapshot.
- Added pure `GeodePlacementDefinition` validation for per-zone values, altitude/ocean-depth limits, terrain delta/radius, tilt, forest thresholds, scale, group sizing/radius, ground offset, and runtime-float representability.
- Added required schema-3 `placement` data to the Meadows/Earth foundation definition.
- Added server-side BepInEx placement overrides under `Worldgen.Placement.<geode id>` and routed every override through the pure definition override/validation/fingerprint pipeline before worldgen registration.
- Removed runtime-owned placement constants; `GeodeWorldgenRegistrar` now consumes the validated geode placement record directly.
- Added deterministic placement-authority source coverage and updated authority tests to use the current schema constant.
- Runtime compilation and in-game schema-3/worldgen behavior remain unclaimed pending execution in a current .NET/Valheim/Jötunn environment.

## Unreleased — additive geode worldgen

- Reconciled repository metadata to a single authoritative `main` branch; the legacy `master` ref is no longer present.
- Added the one-time `GeodeWorldgenRegistrar` that consumes validated/fingerprinted definitions, performs read-only host observation, re-runs pure Add/Skip/Error planning, preflights approved additions, and registers only new Magenheim-owned geode vegetation through Jötunn.
- Removed the redundant `GeodeWorldPrefabRegistrar`; Jötunn `ZoneManager.AddCustomVegetation` already registers the supplied prefab through `PrefabManager`.
- Expanded host collision protection to the broader PrefabManager namespace without mutating host content.
- Hardened runtime area mapping by resolving `Heightmap.BiomeArea` from `Everything` or `Everywhere` instead of compiling against one spelling or casting enum integers.
- Added validation records for the additive registration contract and remaining runtime gates.

## Earlier unreleased foundation

- Reconciled divergent repository histories without discarding material work or using force-pushes.
- Restored canonical crystal/refinement authority and established project instructions, state, backlog, design, and validation records.
- Added the pure core, net462 Jötunn runtime bootstrap, strict definition loading, deterministic SHA-256 definition fingerprinting, and validated balance/compatibility overrides.
- Added Meadows/Earth geode definitions, deterministic cracking, authority-gated opening transaction planning, definition-authority synchronization, and session-scoped replay protection.
- Added additive-only worldgen area validation/planning, configurable collision behavior, Magenheim-owned exclusions, and dedicated `<item prefab>_World` geode identities.
- Added definition-driven intact geode item registration and dedicated mineable world-object source while keeping temporary Stone-derived visuals separate from natural worldgen identity.
- Hardened negative/unknown spawn-area handling and exact/case-insensitive identity semantics.
- Did not admit bundled runtime/vendor binaries, runtime logs/process files, unrelated third-party repair utilities, duplicate legacy engines, or stale runtime claims into live source.
