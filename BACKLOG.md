# Magenheim Backlog

Priority is dependency order, with broken intended behavior repaired before new scope.

## P0 — repository/runtime foundation

- [x] Reconcile README claims against actual committed state.
- [x] Establish authoritative project state and design documents.
- [x] Restore pure-domain crystal tier/alignment/refinement foundation.
- [ ] Add BepInEx/Jötunn plugin bootstrap on `main`.
- [ ] Add config/data loading for refinement definitions with strict validation and safe fallback behavior.
- [ ] Register Crystal Shaping under permanent ID `magenheim.crystal_shaping`.
- [ ] Add deterministic unit/standalone tests for the pure-domain refinement engine.

## P1 — Meadows/Earth vertical slice

- [ ] Define Meadows geode data and Earth-only initial outcome table.
- [ ] Register intact Meadows geode item/prefab.
- [ ] Implement additive, non-destructive Meadows world placement with configurable spawn controls.
- [ ] Implement Geologist's Workstation registration and crafting recipe.
- [ ] Implement geode opening transaction with independent outcome rolls.
- [ ] Register Earth Rough/Simple/Crystal/Advanced/Master items.
- [ ] Bind valid refinement attempts to inventory transactions and Crystal Shaping XP.
- [ ] Implement multiplayer/server authority before enabling persistent gameplay mutation.

## P2 — adaptive sockets

- [ ] Define namespaced persistent socket metadata.
- [ ] Discover eligible equipment adaptively from item category and metadata.
- [ ] Add configurable include/exclude rules by item, prefab, category, and mod origin.
- [ ] Apply crystal effects additively without replacing external prefabs or recipes.
- [ ] Validate persistence, multiplayer authority, item transfer, death/drop, repair, upgrade, and mod-removal behavior.

## P3 — biome expansion

Add biome geodes and elemental weight tables only after the Meadows/Earth vertical slice is playable and validated. Biome expansion must be data-driven and must not require changes to the refinement engine.
