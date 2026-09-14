# Magenheim

**Magic begins as geology.**

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

The immediate playable target remains:

Meadows geode -> Earth-aligned Rough crystal -> Geologist's Workstation -> risky refinement -> later socket/equipment integration.

See `PROJECT_STATE.md`, `docs/MAGENHEIM_DESIGN_SPEC.md`, `BACKLOG.md`, and `VALIDATION.md` for authoritative scope and next work.
