# Magenheim Changelog

## Unreleased — fingerprinted geode placement authority

- Advanced the definition schema to version 3 and made complete geode vegetation placement behavior part of the validated/fingerprinted authority snapshot.
- Added pure `GeodePlacementDefinition` validation for per-zone values, altitude/ocean-depth limits, terrain delta/radius, tilt, forest thresholds, scale, group sizing/radius, ground offset, and runtime-float representability.
- Added required schema-3 `placement` data to the Meadows/Earth foundation definition.
- Added server-side BepInEx placement overrides under `Worldgen.Placement.<geode id>` and routed every override through the existing pure definition override/validation/fingerprint pipeline before worldgen registration.
- Removed the runtime registrar's temporary density/terrain/scale/group constants; `GeodeWorldgenRegistrar` now consumes the validated geode placement record directly.
- Added deterministic source coverage proving placement survives validation, changes the fingerprint, rejects invalid tilt/unrepresentable runtime values, and flows through the validated server override path.
- Updated definition-authority tests to use the current schema constant instead of embedding schema 2.
- Advanced runtime source/package identity to 0.0.13.
- Runtime compilation and in-game schema-3/worldgen behavior remain unclaimed pending execution in a current .NET/Valheim/Jötunn environment.

## Unreleased — additive geode worldgen

- Reconciled repository metadata to a single authoritative `main` branch; the legacy `master` ref is no longer present.
- Added the one-time `GeodeWorldgenRegistrar` that consumes the validated/fingerprinted definition snapshot, performs read-only host observation, re-runs pure Add/Skip/Error planning, preflights all approved additions, and registers only new Magenheim-owned geode vegetation through Jötunn.
- Removed the redundant `GeodeWorldPrefabRegistrar`. Jötunn `ZoneManager.AddCustomVegetation` already registers the supplied prefab through `PrefabManager`, so pre-registering the world prefab risked a self-collision.
- Expanded host collision observation to include both ZoneManager vegetation and the broader PrefabManager namespace without mutating either.
- Hardened runtime area mapping against the Jötunn/Valheim combined-area naming difference by resolving `Heightmap.BiomeArea` from `Everything` or `Everywhere` at runtime instead of compiling against one spelling or casting enum integers.
- Added `docs/validation/2026-09-14-additive-geode-vegetation-registration.md` documenting the registration contract, API reconciliation, and remaining runtime gates.
- Compilation and in-game generation remain unclaimed until executed in a current .NET/Valheim/Jötunn environment.

## Earlier unreleased foundation

- Reconciled divergent repository histories without discarding material work or using force-pushes.
- Restored canonical crystal/refinement authority and established project instructions, state, backlog, design, and validation records.
- Added the pure core, net462 Jötunn runtime bootstrap, strict definition loading, deterministic SHA-256 definition fingerprinting, and validated balance/compatibility overrides.
- Added Meadows/Earth geode definitions, deterministic cracking, authority-gated opening transaction planning, definition-authority synchronization, and session-scoped replay protection.
- Added additive-only worldgen area validation/planning, configurable collision behavior, Magenheim-owned exclusions, and dedicated `<item prefab>_World` geode identities.
- Added definition-driven intact geode item registration and dedicated mineable world-object source while keeping temporary Stone-derived visuals separate from natural worldgen identity.
- Hardened negative/unknown spawn-area handling and exact/case-insensitive identity semantics.
- Did not admit bundled runtime/vendor binaries, runtime logs/process files, unrelated third-party repair utilities, duplicate legacy engines, or stale runtime claims into live source.
