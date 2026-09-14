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
