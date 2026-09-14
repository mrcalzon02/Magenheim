# Magenheim Changelog

## Unreleased — additive geode worldgen

- Reconciled repository metadata to a single authoritative `main` branch; the legacy `master` ref is no longer present.
- Added the one-time `GeodeWorldgenRegistrar` that consumes the validated/fingerprinted definition snapshot, performs read-only host observation, re-runs pure Add/Skip/Error planning, preflights all approved additions, and registers only new Magenheim-owned geode vegetation through Jötunn.
- Removed the redundant `GeodeWorldPrefabRegistrar`. Jötunn `ZoneManager.AddCustomVegetation` already registers the supplied prefab through `PrefabManager`, so pre-registering the world prefab risked a self-collision.
- Expanded host collision observation to include both ZoneManager vegetation and the broader PrefabManager namespace without mutating either.
- Hardened runtime area mapping against the Jötunn/Valheim combined-area naming difference by resolving `Heightmap.BiomeArea` from `Everything` or `Everywhere` at runtime instead of compiling against one spelling or casting enum integers.
- Added a centralized conservative Meadows geode placement profile as an implementation baseline; placement density/terrain settings remain non-user-configurable until moved into fingerprinted definition authority.
- Advanced runtime source/package identity to 0.0.12.
- Added `docs/validation/2026-09-14-additive-geode-vegetation-registration.md` documenting the registration contract, API reconciliation, and remaining runtime gates.
- Compilation and in-game generation remain unclaimed until executed in a current .NET/Valheim/Jötunn environment.

## Earlier unreleased foundation

- Reconciled divergent `main` and `master` histories while preserving both ancestries.
- Replaced a contaminated committed merge tree containing unresolved conflict markers with an explicitly resolved live tree.
- Retained the material additive worldgen area validator/planner and deterministic worldgen tests from the divergent work.
- Reconciled crystal tier authority to Rough -> Simple -> Crystal -> Advanced -> Master.
- Added project-level repository and compatibility instructions.
- Added a standalone combined pure-core test harness and `IsExternalInit` compatibility shim.
- Preserved the large pre-reconciliation design specification under `docs/archive/` for provenance and future recovery.
- Retargeted `Magenheim.Core` from `netstandard2.1` to `netstandard2.0` so the pure rules remain consumable by both the .NET 8 test harness and Jötunn's .NET Framework 4.6.2 runtime.
- Added `Magenheim.Runtime` targeting net462 with JötunnLib 2.30.0.
- Added BepInEx plugin identity `mrcalzon02.magenheim`, hard Jötunn dependency, and everyone-must-have network compatibility declaration.
- Added a thin runtime service composition boundary that consumes authoritative core services without duplicating gameplay rules.
- Added schema-versioned refinement/geode/worldgen compatibility definition models, strict validation, and deterministic SHA-256 fingerprinting.
- Added strict Newtonsoft.Json runtime loading that rejects unknown members, duplicate properties, missing required values, unsupported schemas, invalid enum/reference values, duplicate geode identities, invalid areas, and invalid weights.
- Added `default-data/foundation.json` as the shipped static content authority and configured it to copy with runtime build output.
- Added the initial Meadows/Earth definition with one guaranteed Earth crystal and independent 35% and 10% additional-crystal chances.
- Added deterministic geode-cracking and authority-gated geode-opening transaction planning.
- Added session-scoped exact-once geode operation admission and definition-authority synchronization source.
- Moved worldgen compatibility policy into the validated/fingerprinted definition snapshot and aligned area/identity validation with that policy.
- Restricted compatibility exclusions to Magenheim-owned registration/prefab namespaces.
- Added dedicated geode world-object identity derivation (`<item prefab>_World`) so natural worldgen never registers the temporary Stone-backed intact item directly.
- Added definition-driven Jötunn intact-geode item registration and dedicated mineable world-prefab source.
- Hardened spawn-area handling so negative numeric flags cannot be clamped into `All` through sign extension.
- Added textual area parsing for `Median`, `Edge`, `All`, and the runtime-facing `Everywhere` alias.
- Extended additive worldgen compatibility planning to detect prefab collisions as well as registration-key collisions.
- Added non-destructive compatibility controls for Magenheim-owned exclusions and optional case-insensitive identity comparison.
- Did not admit bundled runtime/vendor binaries, runtime logs/process files, unrelated third-party repair utilities, duplicate legacy engines, or stale build/runtime claims into the live source tree.
