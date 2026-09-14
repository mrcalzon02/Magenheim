# Magenheim

**Magic begins as geology.**

<<<<<<< HEAD
Magenheim is a Valheim expansion centered on biome geodes, elemental crystals, Crystal Shaping, risky refinement, equipment socketing, and additive world-generation integration.

## Repository state

The live repository was reconciled on 2026-09-13. The original initial commit contained only this README even though the earlier README text described source, tests, design files, and build outputs that were not actually present. This repository now treats GitHub contents as authoritative and does not claim unobserved builds, tests, or runtime validation.

The current implementation slice establishes the world-generation compatibility foundation before registering gameplay content. It adds:

- validated biome-area masks with explicit handling for `None`, unknown bits, `Median`, `Edge`, and `All`;
- additive-only planning that never edits or removes vanilla or foreign-mod registrations;
- deterministic duplicate detection by registration key;
- configurable conflict and invalid-area behavior with conservative defaults;
- immutable planning results and diagnostics so integration code can observe before mutating Jötunn state;
- a pure-core test harness that can run without Valheim or Jötunn assemblies.

## Compatibility rule

Magenheim owns only registrations whose keys begin with `magenheim.`. Existing vanilla or third-party entries are observations, not mutation targets. Compatibility logic may skip a Magenheim addition when another registration already occupies a key, but it does not rewrite, delete, disable, or reorder foreign content.

This matches Jötunn's current ZoneManager guidance: custom locations/vegetation are additions that should be registered once, while modification callbacks are for intentionally changing existing content. Magenheim does not use those callbacks to alter foreign worldgen.
=======
Magenheim is a Valheim mod built around biome geodes, elemental crystals, Crystal Shaping progression, increasingly risky crystal refinement, and later adaptive equipment socketing.

## Verified implementation state

The repository is currently at the restored foundation stage, not yet at a playable release.

Implemented on `main`:

- authoritative project-state and design records;
- canonical crystal tiers: Rough, Simple, Crystal, Advanced, Master;
- elemental-alignment domain model;
- definition-driven one-tier-at-a-time refinement validation;
- canonical default refinement success curve of 90%, 80%, 70%, and 60%;
- station and Crystal Shaping skill gates;
- preservation of crystal element through refinement;
- explicit success, preserved-failure, destroyed-failure, invalid-skill, invalid-station, invalid-roll, and no-rule outcomes;
- a dependency-free `Magenheim.Core` project targeting `netstandard2.1`.

Not yet implemented or verified:

- BepInEx/Jötunn runtime plugin bootstrap;
- Crystal Shaping registration in Valheim;
- geode prefabs or biome spawning;
- item registration and inventory mutation;
- multiplayer/RPC authority;
- persistent sockets or equipment effects;
- runtime configuration loading;
- in-game validation.

The first repository commit contained only this README while describing source, tests, build scripts, and runtime integration that were not actually present. The current `main` branch corrects that discrepancy by treating only committed files as implemented.

## Current vertical slice
>>>>>>> origin/main

The immediate playable target remains:

<<<<<<< HEAD
The code in `src/Magenheim.Core` is deliberately pure C# and contains no direct Jötunn or Unity dependency. `tests/Magenheim.Core.Tests` exercises area normalization and non-destructive addition planning. Runtime binding to `ZoneManager`, asset loading, gameplay transactions, networking, and actual Valheim world generation remain future work and must not be claimed as verified until observed in game.

## Next milestone

Bind the validated plan to a thin Jötunn adapter, register a Meadows geode as additive vegetation/location content using a namespaced key, then verify generation in a disposable world without changing vanilla or third-party registrations. See `MAGENHEIM_DESIGN_SPEC.md`, `IMPLEMENTATION_PLAN.md`, `BACKLOG.md`, and `VALIDATION.md`.
=======
Meadows geode -> Earth-aligned Rough crystal -> Geologist's Workstation -> risky refinement -> later socket/equipment integration.

See `PROJECT_STATE.md`, `docs/MAGENHEIM_DESIGN_SPEC.md`, `BACKLOG.md`, and `VALIDATION.md` for authoritative scope and next work.
>>>>>>> origin/main
