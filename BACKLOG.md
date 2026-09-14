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
- [x] Make definition duplicate detection and exclusion normalization obey the configured exact/case-insensitive identity comparer.
- [x] Canonicalize case-insensitive compatibility exclusions in the definition fingerprint while preserving exact-mode casing authority.
- [x] Reconcile the former `main`/`master` split and make `main` authoritative.
- [ ] Delete redundant `radiance-content`, `tmp-radiance-content`, and `__delete_me__` refs. All three point to `a8b6947c696e4da71e4837ff1b731ac53e98a387`, which is already contained in `main`; the current connector cannot delete branch refs.
- [x] Advance definition schema to 3 so geode placement behavior is validated, fingerprinted, and synchronized with the rest of gameplay/worldgen authority.
- [x] Add server-side BepInEx overrides for fingerprinted geode placement fields and route them through full definition revalidation before use.
- [x] Harden runtime `All` area mapping so `Everything` / `Everywhere` aliases are admitted only when semantically identical to distinct non-empty `Median | Edge` runtime components.
- [x] Execute the standalone tests with a .NET 8 SDK and record the observed result for the earlier admitted build.
- [x] Compile `Magenheim.Core` with warnings as errors for the earlier admitted build.
- [x] Add the thin BepInEx/Jötunn plugin bootstrap on `main`, with hard Jötunn dependency and everyone-must-have network declaration.
- [x] Compile the runtime project against Jötunn 2.30.0 and a current Valheim development environment for the earlier admitted build.
- [ ] Rebuild current 0.0.37 source and rerun the full deterministic suite after the accumulated socket/worldgen/content changes.
- [x] Add strict schema-validated static definition loading with deterministic definition fingerprinting and hard failure on malformed/unknown content.
- [x] Add controlled balance/compatibility/world-placement overrides on top of the validated static definition snapshot; effective authority is always revalidated and re-fingerprinted.
- [ ] Add deterministic tests for runtime JSON loader failure cases once a compilable runtime test environment is available.
- [x] Add deterministic schema/fingerprint authority comparison with fail-closed mutation admission.
- [x] Register a two-way Jötunn gameplay-authority handshake and reset cached admission per sync session.
- [x] Include socket eligibility/configuration policy in the synchronized gameplay fingerprint so peers with different socket rules cannot authorize persistent mutations.
- [x] Assign a strictly increasing server-local session generation and retire prior operation replay records for reused peers.
- [ ] Compile and execute the updated gameplay-authority synchronization path in a current Valheim/Jötunn environment and repair any API/serialization defect before runtime admission.
- [x] Register Crystal Shaping under permanent ID `magenheim.crystal_shaping`.
- [ ] Confirm the live console diagnostic `raiseskill magenheim.crystal_shaping 1`; the spaced display name is not a valid `raiseskill` argument.

## P1 — Meadows/Earth vertical slice

- [x] Define the initial Meadows geode data with one guaranteed Earth crystal and independent 35%/10% additional-crystal chances.
- [x] Add deterministic pure-core coverage for geode cracking and authority-gated geode-opening transaction planning.
- [x] Add session-scoped exact-once geode-opening admission keyed by peer/session generation/operation id.
- [x] Add definition-driven Jötunn geode item registration source using canonical prefab identities and a temporary vanilla Stone visual base.
- [ ] Validate intact Meadows geode item/prefab registration in a current Valheim/Jötunn runtime and replace the Stone placeholder only after the custom geode asset passes prefab/visual validation.
- [x] Carry authoritative geode biome metadata into `DesiredWorldgenAddition` and build worldgen plans directly from the validated definition snapshot.
- [x] Add an explicit Jötunn adapter boundary for validated biome/area mapping and read-only desired-host collision observation.
- [x] Bind the additive worldgen planner to a one-time Jötunn `CustomVegetation` registrar using the validated definition compatibility policy.
- [x] Resolve pure-core `SpawnArea.All` against runtime `Heightmap.BiomeArea` names `Everything`/`Everywhere` instead of casting enum integers.
- [x] Move geode placement density, terrain, scale, group, and offset controls into schema-3 fingerprinted definition authority.
- [x] Expose those placement controls through server-side BepInEx overrides that revalidate/re-fingerprint before registration.
- [ ] Snapshot pre/post worldgen registrations and prove repeated-load idempotence in a live Valheim/Jötunn environment.
- [x] Add a dedicated Magenheim-owned mineable geode world-object prefab derived as `<item prefab>_World`, with persistent network state and exactly one intact-geode destruction drop.
- [x] Collapse world-prefab and vegetation registration into one additive path because Jötunn `AddCustomVegetation` registers the prefab itself.
- [ ] Compile and validate the mineable geode world-object/vegetation path in current Valheim/Jötunn, including exactly-one intact drop, natural placement, host/client persistence, and proof vanilla `Rock_4` remains unchanged.
- [x] Implement Geologist's Workstation registration and building recipe (0.0.16; live world test confirms the station exists and opens its UI).
- [x] Record live-world test 1 confirming station UI access, direct Earth inventory-item spawning, and direct custom world-geode geometry.
- [x] Normalize Magenheim-owned cloned materials to opaque blend/depth state at the authoritative visual-loading boundary after the live world geode rendered translucent.
- [ ] Rebuild/install and confirm the spawned world geode is opaque under live Valheim lighting after the material-state repair.
- [ ] Repair the Geologist's Workstation iron-band geometry in the generator and checked-in generated mesh together; current bands intersect the tabletop and share visible planes.
- [ ] Revalidate the current authority-gated geode-opening/refinement workstation operation binding in live Valheim after the source advanced beyond the first-world-test baseline.
- [x] Register Earth Rough/Simple/Crystal/Advanced/Master items and Earth Crystal Shards.
- [ ] Verify a failed refinement transaction consumes exactly one source and returns exactly the configured matching shards in live runtime.
- [ ] Verify disposable-world generation, save/load, host/client, and dedicated-server behavior before production admission.

## P2 — adaptive sockets

- [x] Define namespaced persistent per-item socket metadata under `magenheim.sockets.v1` without changing shared item/prefab definitions.
- [x] Discover eligible equipment adaptively from runtime item category with a fail-safe Unknown result; unknown equipment requires explicit compatibility opt-in.
- [x] Add configurable include/exclude rules by shared item identity, prefab, category, and mod origin, with exact/case-insensitive identity semantics.
- [x] Enable Jötunn ModQuery before equipment classification so mod-origin compatibility rules can resolve non-Jötunn as well as Jötunn-added prefabs.
- [x] Map crystal bonuses by equipment category through Magenheim-owned runtime effect patches without replacing external prefabs or recipes.
- [x] Add an explicit Geologist's Workstation socket-management surface for local-host operations; mutations remain per-item metadata operations.
- [x] Preserve unknown foreign custom-data keys when writing or clearing Magenheim socket metadata.
- [x] Add authority/replay-gated socket add/install/extraction planners and local-host transactional rollback paths.
- [x] Fold gameplay-significant socket limits and compatibility rules into the multiplayer gameplay authority fingerprint.
- [x] Add deterministic source coverage for item-level compatibility, identity normalization, policy fingerprint drift, replay identity, metadata preservation, socket mutation, extraction, and effect calculation.
- [ ] Bind remote-client socket-management requests to the server-authoritative RPC/approval path; current workstation overlay admits mutation only on the local host.
- [ ] Validate persistence and behavior through save/load, drop/pickup, chest storage, repair, upgrade, player transfer, death, dedicated server, and mod removal.
- [ ] Live-test representative third-party equipment, including explicit item/prefab/mod-origin exclusions and unknown-category opt-in behavior.

## P3 — biome and magic expansion

Continue remaining elemental/magic content only when it does not outrank broken intended behavior or validation gates above. The archived detailed design remains recovery material, not evidence of implementation. Current source already contains substantial elemental staff work beyond the original Earth vertical-slice baseline; live source and validation records outrank this historical phase label.
