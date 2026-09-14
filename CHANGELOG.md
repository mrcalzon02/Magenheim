# Magenheim Changelog

## Unreleased — repository consolidation, runtime foundation, compatibility hardening, and definition authority

- Reconciled divergent `main` and `master` histories while preserving both ancestries.
- Replaced a contaminated committed merge tree containing unresolved conflict markers with an explicitly resolved live tree.
- Retained the material additive worldgen area validator/planner and deterministic worldgen tests from the divergent work.
- Reconciled crystal tier authority to Rough -> Simple -> Crystal -> Advanced -> Master.
- Added project-level repository and compatibility instructions.
- Added a standalone combined pure-core test harness and `IsExternalInit` compatibility shim.
- Preserved the large pre-reconciliation design specification under `docs/archive/` for provenance and future recovery.
- Retargeted `Magenheim.Core` from `netstandard2.1` to `netstandard2.0` so the pure rules remain consumable by both the .NET 8 test harness and Jötunn's .NET Framework 4.6.2 runtime.
- Added `Magenheim.Runtime` targeting net462 with JötunnLib 2.30.0.
- Added BepInEx plugin identity `mrcalzon02.magenheim`, hard Jötunn dependency, and everyone-must-have/minor-version network compatibility declaration.
- Added a thin runtime service composition boundary that consumes the authoritative core refinement service without duplicating gameplay rules.
- Added schema-versioned refinement/geode definition models and strict pure-core validation.
- Added normalized SHA-256 definition fingerprinting for future multiplayer definition synchronization.
- Added strict Newtonsoft.Json runtime loading that rejects unknown members, duplicate properties, missing required values, unsupported schemas, invalid enum/reference values, duplicate geode identities, invalid areas, and invalid weights.
- Added `default-data/foundation.json` as the shipped static content authority and configured it to copy with the runtime build output.
- Moved runtime refinement construction from hard-coded defaults to the validated definition snapshot; invalid definitions now fail startup instead of silently selecting a fallback ruleset.
- Added the initial Meadows/Earth definition with one guaranteed Earth crystal and independent 35% and 10% additional-crystal chances.
- Advanced the runtime foundation version to `0.0.4` for the definition-pipeline behavior change.
- Kept world objects, items, sockets, RPCs, inventory mutation, and persistent gameplay changes disabled pending validation and server-authoritative transaction work.
- Hardened spawn-area handling so negative numeric flags cannot be clamped into `All` through sign extension.
- Added textual area parsing for `Median`, `Edge`, `All`, and the runtime-facing `Everywhere` alias, with explicit reject/clamp/fallback policy.
- Extended additive worldgen compatibility planning to detect prefab collisions as well as registration-key collisions.
- Added non-destructive compatibility controls for per-Magenheim key/prefab exclusions and optional case-insensitive identity comparison.
- Kept collision/exclusion behavior observation-only: existing vanilla or foreign registrations are never rewritten, disabled, removed, or reordered.
- Did not admit bundled runtime/vendor binaries, runtime logs/process files, unrelated third-party repair utilities, duplicate legacy engines, or stale build/runtime claims into the live source tree.
- Compilation and Valheim runtime validation remain unclaimed because the current execution environment does not provide the required compiler/runtime test environment.
