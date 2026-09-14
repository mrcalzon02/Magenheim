# Magenheim

**Magic begins as geology.**

Magenheim is a Valheim mod built around biome geodes, elemental crystals, the Crystal Shaping skill, risky refinement, and adaptive non-destructive equipment socketing.

## Verified implementation state

The repository is still a foundation, not a playable release. The authoritative branch is `main`.

Implemented in source:

- project authority, state, backlog, implementation, validation, and design records;
- a dependency-free `Magenheim.Core` domain project;
- canonical tiers Rough, Simple, Crystal, Advanced, and Master;
- normal alignments Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit;
- one-tier refinement rules with 10%, 20%, 30%, and 40% base failure;
- Crystal Shaping skill reduction, workstation/upgrade gates, destructive failure, matching shard returns, and element preservation;
- additive-only spawn-area validation and worldgen addition planning that snapshots foreign registrations rather than mutating them;
- a standalone deterministic test harness covering refinement and worldgen planning.

A detailed pre-reconciliation design record is preserved under `docs/archive/` for provenance and future feature recovery. The current `docs/MAGENHEIM_DESIGN_SPEC.md` controls when archived material conflicts with current authority.

## Validation boundary

The current execution environment has no .NET SDK/compiler, so this reconciliation performs source-level and Git-object verification only. The test harness is present but has not been executed in this environment. No Valheim, BepInEx, Jötunn, world-generation, multiplayer, or persistence runtime acceptance is claimed.

## Immediate playable target

Meadows geode -> intact geode -> Geologist's Workstation -> Earth Rough crystal -> Crystal Shaping refinement -> persistent adaptive equipment integration.

Runtime plugin bootstrap, asset/prefab registration, inventory transactions, server authority, sockets, and in-game validation remain open work. See `PROJECT_STATE.md`, `docs/MAGENHEIM_DESIGN_SPEC.md`, `BACKLOG.md`, `IMPLEMENTATION_PLAN.md`, and `VALIDATION.md`.
