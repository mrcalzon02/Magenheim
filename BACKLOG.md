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
