# Backlog

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
