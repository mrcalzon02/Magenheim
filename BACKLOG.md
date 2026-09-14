# Magenheim Backlog

Priority is dependency order. Broken intended behavior and repository divergence outrank new scope.

## P0 — repository and pure-core foundation

- [x] Reconcile false bootstrap claims against committed repository reality.
- [x] Establish authoritative instructions, project state, design, backlog, and validation records.
- [x] Reconcile divergent `main`/`master` histories without force-pushing or discarding ancestry.
- [x] Remove committed unresolved-merge text from the live authority by constructing an explicit resolved consolidation tree.
- [x] Preserve material legacy design work without making stale content authoritative.
- [x] Restore canonical crystal/refinement domain authority with Rough -> Simple -> Crystal -> Advanced -> Master.
- [x] Retain additive-only spawn-area validation and non-destructive worldgen planning from divergent work.
- [x] Add deterministic standalone tests for refinement and worldgen planning.
- [x] Add an `IsExternalInit` compatibility shim for record-based core models.
- [x] Align `Magenheim.Core` to `netstandard2.0` so it can be consumed by the .NET Framework 4.6.2 Valheim/Jötunn runtime while remaining consumable by the .NET 8 test harness.
- [x] Reject negative spawn-area integers from clamping so sign extension cannot silently become All.
- [x] Add configuration parsing for Median/Edge/All plus the Jötunn-facing `Everywhere` alias.
- [x] Detect prefab collisions as well as registration-key collisions without mutating observed data.
- [x] Add compatibility exclusions by Magenheim registration key/prefab and optional case-insensitive identity matching.
- [ ] Execute the standalone tests with a .NET 8 SDK and record the observed result.
- [ ] Compile `Magenheim.Core` with warnings as errors.
- [x] Add the thin BepInEx/Jötunn plugin bootstrap on `main`, with hard Jötunn dependency and everyone-must-have/minor-version network declaration.
- [ ] Compile the runtime project against Jötunn 2.30.0 and a current Valheim development environment.
- [x] Add strict schema-validated static definition loading for refinement and geode definitions, including deterministic definition fingerprinting and hard failure on malformed/unknown content.
- [ ] Add controlled server configuration overrides on top of the validated static definition snapshot; overrides must be revalidated and fingerprinted before use.
- [ ] Register Crystal Shaping under permanent ID `magenheim.crystal_shaping`.
- [ ] Add server-authoritative definition synchronization/fingerprint enforcement before gameplay mutations are enabled.

## P1 — Meadows/Earth vertical slice

- [x] Define the initial Meadows geode data with one guaranteed Earth crystal and independent 35%/10% additional-crystal chances.
- [ ] Register an intact Meadows geode item/prefab.
- [ ] Bind the additive worldgen planner to a thin Jötunn registrar.
- [ ] Explicitly map pure-core `SpawnArea.All` to current runtime `Heightmap.BiomeArea.Everywhere` rather than casting enum integers.
- [ ] Implement configurable non-destructive Meadows world placement.
- [ ] Snapshot pre/post worldgen registrations and prove repeated-load idempotence.
- [ ] Implement Geologist's Workstation registration and crafting recipe.
- [ ] Implement authoritative geode opening: one guaranteed crystal, independent 35% second-crystal roll, independent 10% third-crystal roll.
- [ ] Register Earth Rough/Simple/Crystal/Advanced/Master items and Earth Crystal Shards.
- [ ] Bind valid refinement attempts to atomic inventory transactions and Crystal Shaping XP.
- [ ] Verify a failed transaction consumes exactly one source and returns exactly the configured matching shards.
- [ ] Verify disposable-world generation, save/load, host/client, and dedicated-server behavior before production admission.

## P2 — adaptive sockets

- [ ] Define namespaced persistent socket metadata.
- [ ] Discover eligible equipment adaptively from item category/capabilities and metadata.
- [ ] Add configurable include/exclude rules by item, prefab, category, and mod origin.
- [ ] Map crystal bonuses by equipment category without replacing external prefabs or recipes.
- [ ] Preserve unknown foreign custom-data keys.
- [ ] Validate persistence, multiplayer authority, transfer, death/drop, repair, upgrade, and mod-removal behavior.

## P3 — biome and magic expansion

Add biome geodes, elemental weight tables, staves, runes, Galdr, Seidr, wards, travel, ship attunement, resonance, totems, spirits, Fate, and boss-resonance systems only after the Meadows/Earth vertical slice has passed its dependency and runtime gates. Use the archived detailed design as recovery material, not as evidence of implementation.
