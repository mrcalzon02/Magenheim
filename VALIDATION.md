<<<<<<< HEAD
# Validation record

## Passed locally

- `build.ps1`: compiled `Magenheim.dll` against the installed Valheim, BepInEx and Jötunn assemblies.
- Standalone core runner: **118 assertions passed**.
- Formula: all 20 tier/skill cells from the spec; 100% reduction at skill 100; exact probability boundary.
- Outcomes: all four target tiers, shard counts, same-element recovery and XP on failed attempts.
- Gates: inadequate station level, undefined element, master-tier refinement, invalid skills and random samples.
- Data: range errors, duplicate elements, duplicate JSON properties, missing/unknown fields, invalid progression, null numeric values and future schema rejection.
- Hash: object-key reordering and equivalent numeric spelling preserve it; balance changes alter it.
- Cracking: all four bonus combinations, exact probability boundaries, independent per-crystal element rolls, station gate and rejection before RNG use.
- Weighted sampling: zero-weight exclusion, interval boundaries, invalid RNG values, undefined references, duplicate entries and invalid/empty weight tables.
- 100,000 mixed-element test selections: **59,996 Earth / 40,004 Frost**, expected 60% / 40%. This fixture does not enable Frost in the shipped data.
- 100,000 Earth geode cracks: **58,543 single / 37,926 double / 3,531 triple** yields, expected 58.5% / 38% / 3.5%. Test tolerance: one percentage point for single/double and half a point for triple.
- Recombination: exact and excess supply, insufficient/negative supply, station gate, undefined element, configurable cost and one-batch output.
- Definition schema 2: geode bonus chances and XP affect the hash; schema 1 and unknown future schemas fail explicitly. This version refers to definition files, not persistent item metadata.

Default definition SHA-256:
`e12d347e1758609734abbf05adec804504c1a13551751acf4b3ec30657aa6189`

The test runner uses installed .NET because the Windows Framework loader rejects the game's Unity-built JSON assembly strong name. No system verification settings were changed. Compiler warning CS1701 is suppressed specifically for the game's netstandard 2.0/2.1 reference unification; all other warnings are errors.

## Pending (not claimed as passed)

- SDK solution build (SDK unavailable locally).
- BepInEx runtime startup, localization and skill persistence.
- Asset bundle, mineable geode, station and recipes.
- Inventory transactions, socket effects and staff combat.
- Save/load, drop/pickup, death and player-to-player transfer.
- Server-authoritative data synchronization, reconnect and replay handling.
- Dedicated-server and multiplayer test matrix from the design spec.

No acceptance milestone requiring in-game behavior is marked complete based on compilation alone.
=======
# Validation

## Repository reconciliation

Observed on 2026-09-13: the GitHub repository contained one branch (`master`) and one file (`README.md`). The README described implementation artifacts that were absent. This pass restores repository truth by adding the missing authority documents and a small, inspectable compatibility core. No prior build/test claim is carried forward as evidence.

## Pure-core acceptance checks

The worldgen compatibility slice is accepted only after these behaviors execute successfully:

- `None` biome area is rejected under conservative policy.
- unknown area bits are rejected under conservative policy.
- `ClampKnownBits` preserves only recognized bits and rejects a result of `None`.
- `FallbackToAll` broadens placement only when explicitly configured.
- valid Magenheim namespaced keys produce `Add` decisions.
- an occupied key produces `Skip` by default and leaves the observed registration unchanged.
- unowned/non-namespaced desired keys error.
- duplicate Magenheim desired keys cannot produce two additions.
- `AdditiveOnly = false` is rejected rather than enabling a destructive mode.

The source includes a console test harness for these checks. It is not marked verified until it has actually executed in an environment with .NET 8.

## Adapter acceptance checks

Before any Jötunn adapter is considered complete:

- compile against the exact installed Jötunn/Valheim references;
- confirm the current `BiomeArea` mapping by type/value rather than guessed integers;
- snapshot ZoneManager state before registration;
- execute approved Magenheim additions;
- snapshot afterward and prove no pre-existing registration changed;
- invoke the relevant world-load lifecycle more than once and prove Magenheim did not duplicate its additions;
- validate a disposable world with Median-only, Edge-only, and All placement definitions;
- intentionally inject an invalid/unknown area value and verify it fails closed with a diagnostic;
- intentionally occupy a Magenheim key before planning and verify the foreign entry survives unchanged.

## Runtime boundary

No Valheim runtime, Jötunn registration, generated world, multiplayer authority, or persistence behavior has been observed by this repository pass. Those remain open acceptance items.
>>>>>>> origin/master
