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
- [x] Move worldgen compatibility policy into schema-versioned definition authority and include it in the deterministic fingerprint.
- [x] Make configured invalid-area behavior govern geode area parsing rather than hard-coding Reject before policy admission.
- [x] Restrict compatibility exclusions to Magenheim-owned registration/prefab namespaces so configuration cannot target foreign content.
- [x] Make definition duplicate detection and exclusion normalization obey the configured exact/case-insensitive identity comparer, preventing definitions that the runtime planner would later collapse into collisions.
- [x] Canonicalize case-insensitive compatibility exclusions in the definition fingerprint so semantically identical casing cannot cause false server/client authority mismatches; exact comparison retains casing as authority.
- [x] Flatten singleton Core Networking/Transactions and Runtime Networking source directories while preserving their namespaces and single authorities.
- [x] Reconcile GitHub repository metadata to authoritative `main`; the redundant legacy `master` ref is no longer present.
- [ ] Execute the standalone tests with a .NET 8 SDK and record the observed result.
- [ ] Compile `Magenheim.Core` with warnings as errors.
- [x] Add the thin BepInEx/Jötunn plugin bootstrap on `main`, with hard Jötunn dependency and everyone-must-have network declaration.
- [ ] Compile the runtime project against Jötunn 2.30.0 and a current Valheim development environment.
- [x] Add strict schema-validated static definition loading for refinement and geode definitions, including deterministic definition fingerprinting and hard failure on malformed/unknown content.
- [x] Add controlled balance overrides on top of the validated static definition snapshot; only existing rule/geode balance fields may change, elemental identity sets remain fixed, and the effective snapshot is revalidated and fingerprinted before use.
- [x] Extend the same validated override path to worldgen compatibility policy so server-side compatibility settings are revalidated and re-fingerprinted before use.
- [ ] Add deterministic tests for runtime JSON loader failure cases once a compilable runtime test environment is available.
- [x] Add deterministic schema/fingerprint authority comparison with fail-closed mutation admission.
- [x] Add deterministic pure-core tests for pending, matching, schema-mismatch, fingerprint-mismatch, malformed-local, malformed-remote, null, wrong-length, non-hex, and canonical definition-authority descriptors.
- [x] Register a two-way Jötunn definition-authority handshake: server sends effective schema/fingerprint during initial synchronization, client compares and acknowledges its own authority, server records per-peer admission.
- [x] Harden the authority acknowledgement so the client must echo the exact server descriptor it successfully parsed; malformed server authority now produces no acknowledgement and cannot yield server-side mutation admission.
- [x] Tighten pre-release network version enforcement to patch strictness when the authority RPC was introduced, preventing older 0.0.x clients without the current RPC contract from passing version admission.
- [x] Reset cached peer mutation admission at the start of every Jötunn initial-sync session so reconnects or reused peer IDs cannot inherit prior authorization.
- [x] Assign a strictly increasing server-local session generation at each initial sync and retire prior geode operation replay records for that routed peer id.
- [ ] Compile and execute the definition-authority synchronization path in a current Valheim/Jötunn environment and repair any API/serialization defect before runtime admission.
- [ ] Register Crystal Shaping under permanent ID `magenheim.crystal_shaping`.

## P1 — Meadows/Earth vertical slice

- [x] Define the initial Meadows geode data with one guaranteed Earth crystal and independent 35%/10% additional-crystal chances.
- [x] Add deterministic pure-core coverage for geode cracking: independent bonus rolls, chance boundaries, fixed RNG consumption, weighted element selection, Rough-tier output, and fail-closed malformed input/definitions.
- [x] Add a pure authority-gated geode-opening transaction planner that consumes exactly one source only when definition authority is compatible, the source exists, cracking succeeds, and output capacity is sufficient; rejected plans remain non-mutating.
- [x] Add session-scoped exact-once geode-opening admission keyed by peer/session generation/operation id, with duplicate-prepared, duplicate-applied, conflicting-replay, abort-and-retry, and reconnect-session handling.
- [x] Add definition-driven Jötunn geode item registration source using canonical prefab identities and a temporary vanilla Stone visual base; no recipe or inventory mutation is introduced.
- [ ] Validate intact Meadows geode item/prefab registration in a current Valheim/Jötunn runtime and replace the Stone placeholder only after the custom geode asset passes prefab/visual validation.
- [x] Carry authoritative geode biome metadata into `DesiredWorldgenAddition` and build worldgen plans directly from the validated definition snapshot.
- [x] Add an explicit Jötunn adapter boundary for validated biome/area mapping and read-only desired-host collision observation.
- [x] Bind the additive worldgen planner to a one-time Jötunn `CustomVegetation` registrar using the validated definition compatibility policy.
- [x] Explicitly resolve pure-core `SpawnArea.All` against runtime `Heightmap.BiomeArea` names `Everything`/`Everywhere` rather than compiling against a single version-specific spelling or casting enum integers.
- [ ] Move geode placement density/terrain limits into fingerprinted definition authority before exposing them as user-configurable placement settings.
- [ ] Snapshot pre/post worldgen registrations and prove repeated-load idempotence in a live Valheim/Jötunn environment.
- [x] Add source for a dedicated Magenheim-owned mineable geode world-object prefab derived as `<item prefab>_World`, with persistent network state and exactly one intact-geode destruction drop; never use the temporary item/Stone placeholder as vegetation.
- [x] Collapse world-prefab and vegetation registration into one additive path because Jötunn `AddCustomVegetation` registers the prefab itself; remove the superfluous separate prefab-registration path.
- [ ] Compile and validate the mineable geode world-object/vegetation path in current Valheim/Jötunn, including exactly-one intact drop, natural placement, host/client persistence, and proof vanilla `Rock_4` remains unchanged.
- [ ] Implement Geologist's Workstation registration and crafting recipe.
- [ ] Implement authoritative geode opening: one guaranteed crystal, independent 35% second-crystal roll, independent 10% third-crystal roll.
- [ ] Register Earth Rough/Simple/Crystal/Advanced/Master items and Earth Crystal Shards.
- [ ] Bind valid refinement attempts to atomic inventory transactions and Crystal Shaping XP; every server mutation must require successful definition-authority admission for the requesting peer.
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
