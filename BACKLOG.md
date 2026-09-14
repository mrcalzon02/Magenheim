<<<<<<< HEAD
# Backlog

<<<<<<< HEAD
## Immediate

- Run plugin in a disposable BepInEx/Jötunn profile and record version/startup/skill/localization evidence.
- Verify installed Valheim version and API compatibility; the spec's future 1.0-era targets are not assumed to exist locally.
- Build Unity asset project with workstation, Meadows geode, Earth crystal tiers, shards, upgrades and staff.
- Implement real bundle loading and prefab validation; missing required assets must fail before gameplay registration.
- Add authoritative data manifest/payload synchronization and reject mismatched snapshots before gameplay.

## Earth playable slice

- Geode world generation, valid pickaxe-only destruction, exact intact-geode drop.
- Wire the tested cracking planner (one guaranteed roll, independent 35% and 10% bonuses) to real geode inventory and server-generated randomness.
- Transactional inventory adapters for the tested refinement and recombination planners, with attempt XP, full-inventory handling and reconnect/replay protection. Count matching shards by element when invoking recombination.
- Workstation UI and four progression levels.
- Instance socket metadata, unknown-version preservation, classification and three Earth equipment paths.
- Four Earth staves with distinct spell behaviors.
- Save/load, transfer, duplication resistance and dedicated-server tests.

## Deferred

Remaining elements, runes, Galdr, rituals, wards, travel, ships, lighting, resonance, totems, spirits, Fate and boss artifacts follow the supplied roadmap after the Earth slice passes.
=======
## P0 - repository truth and compatibility safety
- [x] Reconcile live repository against README claims.
- [x] Replace false implementation/build claims with observed repository state.
- [x] Add authoritative design, implementation, backlog, and validation documents.
- [x] Add explicit biome-area validation and configurable invalid-area behavior.
- [x] Add immutable additive-only worldgen planning and collision diagnostics.
- [x] Add conservative compatibility defaults and Meadows geode config seed.
- [ ] Execute pure-core tests in a .NET 8 environment and record observed result.
- [ ] Compile the core library with warnings-as-errors.

## P1 - Jötunn binding
- [ ] Add a thin `JotunnWorldgenRegistrar` adapter.
- [ ] Observe ZoneManager registrations and translate them into read-only compatibility snapshots.
- [ ] Map validated `SpawnArea` to the current Valheim `Heightmap.BiomeArea` values.
- [ ] Prove idempotent custom registration across repeated world loads.
- [ ] Do not subscribe to vanilla-modification callbacks unless a separately scoped feature explicitly requires it.
- [ ] Add structured diagnostic logging for skipped conflicts and invalid definitions.

## P1 - Meadows vertical slice
- [ ] Create geode prefab/assets.
- [ ] Register Meadows Earth geode additively.
- [ ] Implement authoritative crack transaction.
- [ ] Register Rough Earth Crystal and shard items.
- [ ] Implement Geologist's Workstation and Crystal Shaping skill flow.

## P2 - adaptive compatibility
- [ ] Add capability-based item classification for weapon/armor/shield/tool/utility categories.
- [ ] Persist sockets in Magenheim-owned sidecar/item metadata without replacing foreign item definitions.
- [ ] Add opt-in public-API adapters for major equipment/content mods only where their APIs permit additive integration.
- [ ] Add config controls that can disable Magenheim integration per foreign owner/key without mutating that owner's registrations.
- [ ] Add compatibility report command/export showing observed owners, collisions, decisions, and active config.

## P2 - networking and persistence
- [ ] Gameplay-definition fingerprint.
- [ ] Initial synchronization before player world entry.
- [ ] Server-authoritative refinement/socket transactions.
- [ ] Persistence round-trip and removal-safety tests.
>>>>>>> origin/master
=======
# Magenheim Backlog

Priority is dependency order, with broken intended behavior repaired before new scope.

## P0 — repository/runtime foundation

- [x] Reconcile README claims against actual committed state.
- [x] Establish authoritative project state and design documents.
- [x] Restore pure-domain crystal tier/alignment/refinement foundation.
- [x] Correct canonical tier/element identity drift and authoritative skill-scaled destructive failure/shard semantics.
- [ ] Add deterministic standalone tests and a reproducible build path for the pure-domain refinement engine.
- [ ] Add BepInEx/Jötunn plugin bootstrap on `main`.
- [ ] Add config/data loading for refinement definitions with strict validation and explicit startup errors for unsupported schema/config values.
- [ ] Register Crystal Shaping under permanent ID `magenheim.crystal_shaping`.
- [ ] Add server-authoritative definition synchronization before gameplay mutations are enabled.

## P1 — Meadows/Earth vertical slice

- [ ] Define Meadows geode data and Earth-only initial outcome table.
- [ ] Register intact Meadows geode item/prefab.
- [ ] Implement additive, non-destructive Meadows world placement with configurable spawn controls.
- [ ] Implement Geologist's Workstation registration and crafting recipe.
- [ ] Implement geode opening transaction with independent outcome rolls.
- [ ] Register Earth Rough/Simple/Refined/Advanced/Master items and Earth Crystal Shards.
- [ ] Bind valid refinement attempts to atomic inventory transactions and Crystal Shaping XP.
- [ ] Verify failure consumes exactly one source and returns exactly the configured matching shards.
- [ ] Implement multiplayer/server authority before enabling persistent gameplay mutation.

## P2 — adaptive sockets

- [ ] Define namespaced persistent socket metadata.
- [ ] Discover eligible equipment adaptively from item category and metadata.
- [ ] Add configurable include/exclude rules by item, prefab, category, and mod origin.
- [ ] Apply crystal effects additively without replacing external prefabs or recipes.
- [ ] Validate persistence, multiplayer authority, item transfer, death/drop, repair, upgrade, and mod-removal behavior.

## P3 — biome expansion

Add biome geodes and elemental weight tables only after the Meadows/Earth vertical slice is playable and validated. Biome expansion must be data-driven and must not require changes to the refinement engine.
>>>>>>> origin/main
