# Magenheim Implementation Plan

## 1. Repository and pure-domain authority

Keep one authoritative implementation on `main`. `Magenheim.Core` owns deterministic crystal, geode-outcome, refinement, socket-model, and compatibility rules without Unity/Valheim/Jötunn dependencies. A historical design may inform future work but cannot silently reintroduce duplicate engines or stale semantics.

## 2. Deterministic validation

Run the standalone .NET test harness first. Add boundary vectors for every pure rule before runtime adapters depend upon it. Compilation and deterministic tests are separate from in-game acceptance.

## 3. Runtime bootstrap

Add a thin BepInEx/Jötunn plugin entry point and explicit dependency/network declarations. Register the permanent `magenheim.crystal_shaping` skill and localization. Load validated data/config snapshots and fail clearly on unsupported schema or invalid gameplay definitions.

## 4. Additive world generation

Translate the validated pure `SpawnArea` and approved `WorldgenAdditionPlan` entries into current Jötunn/Valheim types. Explicitly map Magenheim `All` to the runtime `Heightmap.BiomeArea.Everywhere` concept; do not rely on enum integer coincidence. Snapshot existing registrations before and after registration. Add only Magenheim-owned entries. Repeated lifecycle events must not duplicate additions.

The compatibility pass must remain configurable without becoming destructive: reject or explicitly normalize invalid areas, detect occupied registration keys and prefabs, allow per-Magenheim-key/prefab exclusions, and optionally use case-insensitive identity matching for interoperability. Negative area integers must fail closed unless the operator explicitly selects fallback-to-All behavior.

## 5. Meadows/Earth geological loop

Create the Meadows geode prefab and intact geode item. Default opening yields one guaranteed crystal plus independent 35% and 10% rolls for second and third crystals. Select elemental alignment from the configured biome weighting for each produced crystal. Build the Geologist's Workstation and Earth crystal/shard items.

## 6. Server-authoritative transactions

Implement geode opening, refinement, shard handling, XP, inventory consumption, and item grants as atomic server-owned operations. Validate proximity, station requirements, source identity, configuration fingerprint, replay/reconnect safety, and full-inventory behavior before consuming input.

## 7. Adaptive sockets

Persist Magenheim-owned per-item socket metadata. Classify weapons, armor, shields, tools, and utility equipment from observable capabilities plus config overrides. Apply elemental bonuses through Magenheim's own effect layer without replacing foreign definitions.

## 8. Runtime validation ladder

Validate plugin startup, disposable-world worldgen, repeated world loads, save/load, drop/pickup, chest storage, player transfer, death, repair/upgrade, host/client, dedicated server, mismatch rejection, and mod-removal safety. Do not promote static validation to runtime acceptance.

## 9. Expansion

Only after the Earth vertical slice is playable and admitted should biome catalogs and later magic systems expand. Recover detailed feature intent from `docs/archive/MAGENHEIM_DESIGN_SPEC_0.1.0_PRE_RECONCILIATION.md`, reconciling each feature against current authority before implementation.
