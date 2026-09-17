# Magenheim Backlog

Priority is dependency order. Broken intended behavior and repository divergence outrank new scope.

## P0.0 — Live play defects from the 0.0.52 session (TOP PRIORITY, raised 2026-09-16)

The first live acceptance pass against an installed build. These outrank P0.1 and everything
below. Items marked REGRESSION were introduced by the asset work in this session.

- [ ] **Geode still shows textureless, unsurfaced parts.** `geode-sample` reports five parts all carrying maps, so this is not a missing texture at the payload level. Needs a screenshot to identify which surface: candidates are `GeodeCore` seen through the crevices, the shaft interior, or a surface reading flat because its map has range but no legible structure under game lighting.
- [x] **Crystal Staff of Venom does not attack.** ROOT CAUSE FOUND, FIXED 0.0.54. Not an attack-binding defect at all: the Venom registrar never ran. `EarthStaffRegistrar` asked for `staff-earth-*.icon.png`, which has never existed in repository history, and `EarthAssets.Texture` resolves icons through `File.ReadAllBytes`. All eight staff registrars subscribe to `PrefabManager.OnVanillaPrefabsAvailable`, a multicast delegate, so the first handler to throw stops every handler after it. Bootstrap order is Fire, Frost, Storm, **Earth**, Venom, Radiance, Seidr, Spirit — the missing Earth icon removed 20 items across four families with no log line naming them. See `docs/validation/2026-09-17-staff-icon-registration-chain-repair.md`.
- [x] **Socket crystal descriptions must state per-slot effect.** DONE 0.0.53: A crystal's description needs to say what it does in a weapon, in armour and in a utility slot. Today the player cannot tell before committing the socket.
- [ ] **Crystal weapons and staves orient wrongly in the player's hand.** Almost certainly the authored up-axis: these are Y-up game-space sources, Blender is Z-up, and the exporter maps `(x, y, z)` to `(x, z, -y)`. The crystal tier models had exactly this defect and it was invisible to every gate. Check the attach transform against a vanilla weapon.
- [x] **Staves need icons derived from their current models.** DONE 0.0.54. `tools/render-staff-icons.py` renders all 32 icons from the same `.blend` the runtime mesh is exported from. Twelve never existed (Earth, Fire, Storm), three were corrupt, and seventeen predated the C1 rebuild. Fire and Storm set no icon at all and inherited the `StaffIceShards` donor icon; Radiance, Seidr and Spirit used a tinted generic `crystal` icon. All eight families now resolve `m_icons` from their own asset name.
- [ ] **All item descriptions must be mechanically informative and player-facing.** One clear statement of what the item does, in the player's language, not the implementation's.
- [x] **Staves must take damage and degrade like normal weapons and tools.** DONE 0.0.54. Staves clone `StaffIceShards`, which spends Eitr rather than durability, so the clones inherited none and never degraded. `Magenheim.Core/CrystalStaffDurability.cs` owns the pure tier -> service-life rule (Simple 150, +75 per tier) with fail-closed tier resolution; all eight registrars set `m_useDurability`, `m_maxDurability`, `m_durabilityPerLevel`, `m_durabilityDrain` and `m_useDurabilityDrain`. The field set was read off the installed `SharedData` by reflection, not assumed. Live wear/repair acceptance is still open.
- [x] **REGRESSION: crystal buildables are texturally broken.** FIXED 0.0.53. Introduced by the generic retro-texture pass. Models whose UVs were smart-projected carry packed islands, so a coherent noise map lands as discontinuous patchwork across island boundaries. Models with purpose-authored UVs (geode, crystal tiers, caverns, passages) are unaffected. Either make the retro map fine-grained and low-contrast so island seams cannot read, or revert the pass on the affected families.

### Raised 2026-09-17 — custom weapon scale and fidelity (user directive)

- [x] **Custom weapon scale does not match vanilla.** DONE 0.0.55. Measured from the committed runtime models, every weapon is oversized and the family is internally inconsistent. `crystal-weapon-knife` is 1.25m along its longest axis — longer than a vanilla sword — and `crystal-weapon-sword` is 2.09m, longer than a vanilla greatsword. Measured longest axis: knife 1.25, axe 1.56, mace 1.74, crossbow 1.76, sword 2.09, battleaxe 2.08, bow 2.46, spear 2.65, greatsword 2.78, atgeir 3.02. A held weapon is the most-looked-at asset in the game, and the player character is roughly 1.8m, so these read as props rather than weapons. Rescaled all ten authored sources by 0.75 about the world origin and re-exported through `export-model-assets.py`. The factor is the one observed in play: the staff family reads correctly at 1.80-2.96m and the sword was about a third longer than it needed to be. Sword is now 1.56m, knife 0.93m, atgeir 2.26m. `tools/verify-model-scale.py` gates the declared per-archetype target and the staff reference band, and is wired into `build.ps1`; proven to fail on the pre-rescale sword with a +0.52m drift.
- [ ] **Weapons are the lowest-detail family in the library.** Median 1,262 triangles against a library median of 2,348 and a staff median of 2,316 — roughly half the library standard, on the assets held closest to camera. This is the same defect shape as the crystal progression items under P0.1 (lowest-poly assets despite highest player contact) and needs the same treatment: re-author to the library standard with silhouette detail that survives at held distance.
- [x] **Weapon texturing is at the library floor, not vanilla's.** DONE 0.0.55. Every family samples at 256px albedo, so weapons are not behind the rest of Magenheim, but they are behind vanilla, whose weapon albedos carry materially more surface information at held distance. The real defect was worse than resolution: the retro pass assigned surface families essentially at random, so 76 of 79 weapon materials contradicted their own declared intent. A sword grip was textured as stone, its crystal as metal, and a greatsword's blackmetal as carapace, on five shared 256px maps. The family token is load-bearing twice over, because `GeneratedSurfaceTextures.Classify` also reads it to pick the runtime fallback surface. `tools/author-weapon-textures.py` now authors seven 512px families (leather, timber, blackmetal, silver, crystal, crystal-bright, prismatic) and binds each material by intent, rewriting the family token so the runtime classifier agrees. `tools/verify-weapon-materials.py` gates it.

### Raised 2026-09-17 — placeable collision audit

- [ ] **Crystal Banners (24 pieces) and the Crystal Sentinel have no owned collision.** Every other placeable family disables the donor's non-trigger colliders and installs fitted Magenheim boxes via a `ConfigureCollider(s)` pass. `CrystalBannerRegistrar` and `CrystalSentinelRegistrar` contain no collider code at all, so they clone `piece_banner01` and `piece_turret` and keep the *donor's* collider while `ModelAssets.Load` replaces the visible mesh. `hideOriginal` disables Renderers and LODGroups only — it does not touch colliders — so the piece collides with the donor's shape, not the model the player sees.
- [ ] **No placeable carries model-derived collision.** Only 25 of 281 models declare any `collider` part, and all 25 are Deep Fracture districts/passages, the Dark Throne and the geode. Every buildable depends entirely on hand-authored box constants in C#. That is a legitimate approach and matches vanilla, but nothing ties the constants to the models.
- [ ] **Gate placeable collision against model bounds.** The authored furniture boxes were checked this session against the committed model bounds and all ten pieces match on every axis, including base alignment — so this is prevention, not repair. The constants are hand-written and the model library has been rebuilt repeatedly; nothing would catch the next drift. `FurnitureRegistrar.ConfigureColliders` also has a `default:` branch that gives any unmatched model a generic 1m cube, which would ship silently.

### Raised 2026-09-17 by the staff registration investigation

- [x] **Gate the icon payload.** DONE 0.0.54 as `tools/verify-icon-assets.py`, wired into `build.ps1`. Walks every icon's PNG chunk stream verifying CRCs and decompressing the pixel data, requires every staff model in the catalog to have a matching icon, and resolves every literal `EarthAssets.Icon("x")` against disk. Proven to fail on the pre-repair tree with exactly the three real defects and nothing else.
- [x] **Declare binary assets to Git.** DONE 0.0.54. The repository had no `.gitattributes`, so nothing stopped Git applying text or EOL conversion to PNG, blend, glb, zip or dll payloads. Three committed staff icons carry bad IDAT CRCs or truncated chunks in the committed blobs, not merely in a worktree.
- [ ] **One content registrar throwing still removes every registrar after it.** The icon gate closes the trigger found this session, but not the class. All content registrars share `PrefabManager.OnVanillaPrefabsAvailable`, so any single failure silently deletes unrelated content and the player sees "this item does not work" rather than a named failure. Deciding the right failure semantics is a design call: per-registrar isolation would keep the rest of the mod alive but risks admitting a half-registered world, which the project's fail-closed doctrine otherwise rejects. Do not fix this by swallowing exceptions.
- [ ] **Audit the other asset-name contracts for the same defect shape.** `staff-earth-*` was requested for months and never existed, because nothing resolved asset names until build time. The icon gate now covers `EarthAssets.Icon`. Model, texture, mesh and prefab name lookups that resolve only at registration time have not been audited for the same class.

## P0.1 — Asset fidelity, live acceptance and geometry gating (TOP PRIORITY, raised 2026-09-16)

These outrank every item below, including P0.4 and P0.5 remnants and all new content.
Measurements are from the committed library at `924f5e7`; evidence and method are in
`docs/RENDERING_AND_CONTENT_DEFECTS.md`.

- [ ] **Run one disposable-world acceptance session.** Everything since 0.0.16 is recorded as "source repaired, live acceptance pending". A large number of defects were repaired from *inferred* behaviour rather than observed behaviour, and nothing currently distinguishes the correct inferences from the wrong ones. This is the cheapest highest-value action available and it gates honest claims on all the rest.
- [x] **Rebuild the geode.** DONE 2026-09-16. `geode-sample` is a single model shared by all eight biome geodes and it is actively broken, not merely dated: `stone-cavity` has 246 of 246 faces pointing inward and all five `interior-crystal-*` parts are 63-73% inverted, every one with negative signed volume. The reported "transparency" is back-face rendering; the atlas carries no alpha and the material is forced opaque. Rebuild the exterior as fractured Voronoi plates with deep crevices over a darker inner mass, and the exposed face as concentric agate banding plus a dense inward-pointing druzy field, authored greyscale. Preserve the existing runtime contract: `GeodeVisuals.Apply` tints any material named `magenheim.geode.interior-*` and lightens `interior-bright-*`, so biome shading needs no runtime change.
- [x] **Gate inverted faces across the whole model library.** DONE 2026-09-16 as `tools/verify-model-geometry.py`; it immediately caught inverted stalactites and columns in all twenty rebuilt districts. This defect class has now appeared three separate times: the copied prism/cylinder builders, the geode cavity, and the geode crystals. It produces no compile error, no exception and no log line, and is invisible until a player looks at the wrong side of a surface. Promote the ad-hoc check that found the geode into a permanent gate over all 281 models, in the manner of `tools/verify-deep-fracture-caverns.py`. Note that exported vertices are unwelded, so edge-pairing tests are uninformative; the face-normal-versus-centroid test is the one that holds.
- [x] **Close the texture gap.** DONE 2026-09-16: 281/281 models and 3,681/3,681 parts carry an albedo map, from 111/281 models and 36% of parts at `924f5e7`. A tonal-range gate in `verify-model-assets.py` keeps a map that carries no image from passing.
- [x] **Develop the crystal progression items.** DONE 2026-09-16. The items the entire mod is built around are the lowest-poly assets in the library: `earth-simple` is 36 triangles, `earth-rough`/`earth-crystal`/`earth-shards` are 108, and `earth-advanced`/`earth-master` are 180. The player handles these constantly across the whole refinement loop.
- [x] **Bring the Deep Fracture passages up to the districts.** DONE 2026-09-16. `deep-fracture-passage` is 60 triangles (five boxes) and `deep-fracture-traversal` is 172. They now connect districts of roughly 10,000 triangles each. The corridor is the first thing seen after a chamber, so the mismatch is immediately legible.
- [ ] **Compile before pushing.** Eighteen compile errors arrived on `main` in one batch and two more of the identical class followed hours later, after that class had already been repaired six times on the same branch. Running `build.ps1` before push catches all of it; nothing more elaborate is required. **It happened again:** on 2026-09-17 `main` at `5aa8ab0` carried eight unbuilt compile errors, including a seventh `netstandard2.0`/`net462` BCL-availability defect (`Math.Clamp`, `string.Contains(string, StringComparison)`). Repaired in `72b0652`. This item stays open until a push actually runs the gate.

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
- [x] Delete redundant remote refs. Four existed, not three: `radiance-content`, `tmp-radiance-content`, `__delete_me__` and `tmp-compat-work`. All four pointed to `fc84a519`, not the `a8b6947c` recorded by the earlier connector-only cycle. Containment in `origin/main` was re-verified immediately before deletion. `git ls-remote --heads origin` now returns `refs/heads/main` only.
- [x] Advance definition schema to 3 so geode placement behavior is validated, fingerprinted, and synchronized with the rest of gameplay/worldgen authority.
- [x] Add server-side BepInEx overrides for fingerprinted geode placement fields and route them through full definition revalidation before use.
- [x] Harden runtime `All` area mapping so `Everything` / `Everywhere` aliases are admitted only when semantically identical to distinct non-empty `Median | Edge` runtime components.
- [x] Execute the standalone tests with a .NET 8 SDK and record the observed result for the earlier admitted build.
- [x] Compile `Magenheim.Core` with warnings as errors for the earlier admitted build.
- [x] Add the thin BepInEx/Jötunn plugin bootstrap on `main`, with hard Jötunn dependency and everyone-must-have network declaration.
- [x] Compile the runtime project against Jötunn 2.30.0 and a current Valheim development environment for the earlier admitted build.
- [x] Rebuild accumulated source as 0.0.45; full core harness passes 13,974 assertions and runtime compiles without warnings/errors. See CLOSEOUT.md.
- [x] Add strict schema-validated static definition loading with deterministic definition fingerprinting and hard failure on malformed/unknown content.
- [x] Add controlled balance/compatibility/world-placement overrides on top of the validated static definition snapshot; effective authority is always revalidated and re-fingerprinted.
- [x] Test actual runtime JSON loader failure cases in the .NET harness, including Underworld required fields, duplicate/unknown properties and invalid references.
- [x] Add deterministic schema/fingerprint authority comparison with fail-closed mutation admission.
- [x] Register a two-way Jötunn gameplay-authority handshake and reset cached admission per sync session.
- [x] Include socket eligibility/configuration policy in the synchronized gameplay fingerprint so peers with different socket rules cannot authorize persistent mutations.
- [x] Assign a strictly increasing server-local session generation and retire prior operation replay records for reused peers.
- [x] Bind authority acknowledgement plus workshop/socket request-response-acknowledgement traffic to that server-issued session generation, reject stale-session traffic fail-closed, retire prior-generation client pending records, and preserve unapplied live queue entries. See `docs/validation/2026-09-15-session-generation-handshake-binding.md` and `docs/validation/2026-09-15-session-bound-mutation-rpcs.md`.
- [ ] Compile and execute the updated gameplay-authority synchronization path in a current Valheim/Jötunn environment and repair any API/serialization defect before runtime admission.
- [x] Register Crystal Shaping under permanent ID `magenheim.crystal_shaping`.
- [ ] Confirm the live console diagnostic `raiseskill magenheim.crystal_shaping 1`; the spaced display name is not a valid `raiseskill` argument.

## P0.5 — Valheim 1.0.12 compatibility repair

- [x] Reconcile origin/main's Broken Crown encounter-reset commits with the local 1.0.12 API pass without overwriting either intent.
- [x] Replace `Character.SetPos` and `Rigidbody.velocity`, both removed or superseded by Valheim 1.0.12, with the installed API.
- [x] Repair the `Inventory.Changed` binding: 1.0.12 declares `Changed(bool, bool)` and three call sites invoked it with zero arguments, throwing `TargetParameterCountException` in workshop rollback and local/remote socket mutation. Bound at the `RuntimeGameApi` boundary with a pinned signature.
- [x] Add `tools/verify-reflection-targets.ps1` so literal reflection bindings are resolved against installed assemblies at build time; proven to fail on the pre-repair 0.0.48 DLL.
- [x] Extend reflection verification to helper-wrapper bindings for `Aoe`, `Projectile`, `Destructible`, `ZNetView`, and `SE_Stats`, including exact type checks for required fields while preserving optional-field semantics.
- [x] Install 0.0.49 into the active Central Fuckery profile. Installed and SHA-256 verified after the user closed Valheim; launcher catalog entry and description verified by read-back.
- [x] Repair `install-local.ps1` checksum verification. Windows PowerShell emits a top-level JSON array as one pipeline item, so `@(... | ConvertFrom-Json)` produced a single nested entry and every package failed integrity. Semantics unchanged; empty manifests now rejected explicitly.
- [ ] Live-confirm that a failed workshop transaction rolls back without an unhandled exception and that socket install/extract refreshes the inventory.

## P0.4 — Rendering and content drift (raised from live play 2026-09-15)

Full evidence and root causes: `docs/RENDERING_AND_CONTENT_DEFECTS.md`. Source repair is complete for the screenshot defect set below; live Valheim visual/runtime acceptance remains open where recorded in that register and `TESTING.md`.

- [x] R1: replace the one-pixel `Texture2D.whiteTexture` material path with Magenheim-owned generated surface textures across the affected runtime visual families.
- [x] R2a: give `Magenheim.Core` correctly wound `Box`/`Cylinder`/`Prism` primitives plus deterministic closed-surface, signed-volume and outward-normal checks, with a regression test that rejects the exact winding shipped in eleven visual files.
- [x] R2b: migrate the original eleven affected visual files onto shared tested primitives and remove their duplicated private builders; additional affected Crystal Weapon and Obelisk Warden geometry was migrated to the same authority.
- [x] C1: give Fire, Storm, Earth, Radiance, Seidr and Spirit owned staff presentation/geometry and remove the unintended vanilla Eitr gameplay dependency from the elemental staff progression.
- [x] C2: strip inherited vanilla Beehive behavior/audio from Crystal Bed and Crystalline Ice Box clones.
- [x] C3: own piece snap points instead of inheriting or merely rescaling incompatible donor snap positions.
- [x] C4: give the Crystal Sentinel an owned icon/readable front and explicit forward emitter presentation.
- [x] C5: collapse the eight elemental Crystal Munition item identities into one `Magenheim_CrystalMunition` with multiple shard refining routes.
- [x] C6: rebuild the Crystal Enchanting Dais as a broad, low raised working platform rather than a stacked apex monument.
- [x] C7: replace the raw `OnGUI`/`GUILayout` socket window with an `InventoryGui`-hosted native Unity UI surface and remove the obsolete IMGUI skin patch.
- [x] C8: overwrite cloned furniture `Piece.m_name` and `m_description` with Magenheim-owned identity so donor names such as "Raven Throne" cannot leak into hover text.

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
- [x] Synchronize the repaired workstation strap generator with the shipped mesh, OBJ, icon and preview. Live visual acceptance remains open.
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
- [x] Bind remote-client socket-management requests to a server-authoritative, session-generation-bound request/response/ack RPC using existing socket/extraction replay guards; server reconstructs equipment classification from the registered prefab and owns extraction randomness.
- [ ] Compile and live-validate remote-client socket open/install/extraction, stale-state and stale-session rejection, replay behavior, policy mismatch rejection, and third-party descriptor spoof rejection in a current Valheim/Jötunn multiplayer environment.
- [ ] Validate persistence and behavior through save/load, drop/pickup, chest storage, repair, upgrade, player transfer, death, dedicated server, and mod removal.
- [ ] Live-test representative third-party equipment, including explicit item/prefab/mod-origin exclusions and unknown-category opt-in behavior.

## P3 — biome and magic expansion

Continue remaining elemental/magic content only when it does not outrank broken intended behavior or validation gates above. The archived detailed design remains recovery material, not evidence of implementation. Current source already contains substantial elemental staff work beyond the original Earth vertical-slice baseline; live source and validation records outrank this historical phase label.

## P4 — The Underworld expansion track

Durable authorities: `docs/UNDERWORLD_DESIGN.md` and `docs/UNDERWORLD_IMPLEMENTATION_PLAN.md`.

This is expansion scope and remains subordinate to broken intended behavior and unresolved runtime acceptance in P0-P2. Do not allow exciting new world content to hide failures in the existing Magenheim baseline.

- [x] Commit the durable Underworld design and heightfield cavern-illusion architecture.
- [x] Commit the detailed Underworld implementation program, including biome framework, creature ecology, six-boss progression, Deepstone Conclave and milestone gates.
- [x] U0: reconcile live definition, fingerprint, world-state, network and Nowhere King completion authorities; see docs/validation/2026-09-15-underworld-authority-integration.md. Runtime completion adapter remains a prerequisite to U2.
- [x] U1: compile/test the Core skeleton and embed six-boss progression plus A0 architecture in canonical schema-5 JSON/fingerprint authority; no runtime world registration.
- [ ] U2: prove the derived-world transition architecture in a disposable environment before biome production.
- [ ] U3: harden persistent world/map/multiplayer transition state.
- [ ] U4: prove Macro Basin + Wall Mass + inaccessible Roof Shelf + cavern-sky/fog illusion.
- [ ] U5-U7: implement generic biome, ecology, boss and Deepstone frameworks.
- [ ] U8-U9: complete Fungal Forest vertical slice and The First Bloom progression loop.
- [ ] U10-U11: complete Blackwater Deep and The Blackwater Maw.
- [ ] U12-U15: complete Sulfurous Wastes/Furnace Heart and Frozen Caverns/White Silence parallel progression branches.
- [ ] U16-U18: implement traversal framework, Fracture Zones and The Rift Titan.
- [ ] U19-U20: implement Great Decay and The Carrion Crown.
- [ ] U21: complete all six Deepstones and Deepbound Portal unlock using the authoritative transition service.
- [ ] U22-U25: expand civilizations, building, events/audio/music, configuration and compatibility.
- [ ] U26: execute expansion-scale validation across transition, generation, persistence, multiplayer, bosses, structures, building and performance.

## Closeout follow-through

Current unfinished asset/gameplay inventory is in `CLOSEOUT.md`. Execute the current TESTING.md matrix before enabling later world-generation scope or adding model-only gameplay. Historical unchecked runtime gates above remain open.
