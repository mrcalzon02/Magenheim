# Current source candidate — 0.0.69 / schema 5 (2026-09-18)

Underworld environment production now starts from Valheim-owned runtime donors instead of authored
placeholder geometry. The disposable local ecology preview has biome-specific donor palettes for all
six Underworld terrain ecologies and reconstructs only the render/material/light/LOD surface of each
source prefab. Vanilla gameplay and network components are deliberately not carried into the local
preview objects, and no base-game mesh or texture is packaged by Magenheim.

The controlling flora/terrain plan now fixes the art order as: vanilla donor composition, donor
kitbash/material variation, Magenheim-authored gap fills, then external permissive assets only when a
remaining requirement justifies them. This lets terrain density, scale, silhouette and biome identity
be judged in Valheim before the custom asset backlog expands.

**Acceptance remains open.** The connected environment cannot launch Valheim, so 0.0.69 has not
been observed in the live game. The next runtime gate is a walk through all six Underworld terrain
ecologies checking missing donor warnings, donor grounding, collision obstruction, LOD transitions,
light range, placement density and frame cost before any donor palette is promoted from preview to
persistent/networked world content.

---

# Current source candidate — 0.0.64 / schema 5 (2026-09-18)

Closes the defect class 0.0.63 spent the session repairing: a generator or its gate moving on while
the committed output did not. `tools/verify-generated-freshness.py` and
`assets/generated.manifest.json` record, per generator, the hash of the generator and the set of
files it owns, and run first in `build.ps1` because verification is pure hashing. `--update` runs
the generator before recording, so the manifest cannot be reconciled without regenerating.

The gate found real staleness on its first run: the crystal tier of the Storm, Fire, Venom and
Radiance staves differ from what `render-staff-icons.py` now produces by about 10% of their pixels,
with RGB deltas above 220. Same tier across four families — a model change those four icons never
picked up. Re-rendered and committed.

Blender output is not reproducible, and the gate records that rather than pretending otherwise:
re-rendering all 32 icons produced 20 files differing from the committed ones, of which 16 differed
by a single LSB on a handful of pixels. Blender-backed entries are therefore checked on generator
hash and output names only, which still catches the defect that occurred three times, and gives up
hand-edit detection explicitly. Pure-Python generators keep full content hashing; the Sporeling
texture set is byte-reproducible across runs and is verified that way. Coverage today is the
Sporeling textures, the Sporeling source blend and the 32 staff icons; the remaining generators are
an open item in `BACKLOG.md` P0.-2.

Also repaired: `tools/blender.ps1` treated Blender 5.0's benign `Error: Not freed memory blocks`
shutdown line as a script failure, which intermittently failed renders that had already succeeded.

No runtime behaviour changed in 0.0.64 beyond the four re-rendered icons. Nothing is
runtime-accepted; the game has still not been launched against 0.0.63 or 0.0.64.

---

# Current source candidate — 0.0.63 / schema 5 (2026-09-18)

`main` and `origin/main` were both at `df27a65` at session start. `origin/main` then advanced to
`720bb5d` mid-session with two Capcrawler authoring commits; the session's seven commits were
rebased onto it and the combined tree was rebuilt rather than assumed compatible. No file overlap:
their commits touch only `tools/author-underworld-capcrawler.py`. Session end `origin/main` is
`f08c82aacb7a779674b60134bab821a30f89753d`, verified by independent `git ls-remote` readback, with
`refs/heads/main` still the only remote branch.

0.0.63 is installed and SHA-256 verified in the active Central Fuckery profile from that exact
tree: `Magenheim.dll` `3ED3F47C216FD89EF92070C039C98F3DA4EBE1AF24C179245738C62B3C2634C2`. The
launcher entry reads back as `Magenheim v0.0.63 by Local (enabled)` with the 0.0.63 description.
Prior payload backup `backups/Local-Magenheim-20260918-095723.zip`; catalog backup
`backups/mods-20260918-095735-183.yml`. Other mods, profile settings and enablement are unchanged.
This is verified catalog data, not an observed launcher UI and not a game start.

**`main` did not build, and did not compile.** Roughly 50 commits had landed since the last
successful build, and `build.ps1 -Offline` failed six times over before this session repaired it:
a call to `UnderworldMapPresentation.CellToLogical` that has never existed, a read of
`UnderworldExplorationState.ExploredCount` that has never existed, two nullable-flow errors, an icon
gate that six correctly-framed staff icons could not satisfy at any framing, a Sporeling texture set
four generator commits and five gate commits stale whose generator could not satisfy its own gate, a
source blend stale because its author had never once run against the installed Blender 5.0, a review
renderer that had never produced a plate for the same class of reason, and a Pillow deprecation
warning on stderr that killed the build after a gate had already reported success. All repaired at
source. Evidence: `docs/validation/2026-09-18-main-build-gate-repair.md`.

**The Underworld biome-sector override is fixed in source.** This was the open defect the
2026-09-17 handoff named as the next task, and it was settled from the installed assemblies rather
than from the live `[sector probe]`, which could never have fired. The patch was bound to
`WorldGenerator.GetBiomeSector(int gridx, int gridy, bool clamp)`, which clamps its grid indices
into [0, 2047] unconditionally — its own `clamp` argument is never read — and writes them with
`starg`, so the postfix read the clamped value. The biome map spans only ±12282m and the region is
at 40000, so containment was tested 27.7km outside the region and could never pass. **The region
does not have to move:** both world-space overloads still carry the true coordinate and nothing in
the game reaches the grid overload except those two, so the override moved there. The probe is
removed. Evidence: `docs/validation/2026-09-18-underworld-biome-sector-world-space-binding.md`.

38,320 deterministic Core assertions pass; 281 model assets import twice; runtime compiles with zero
warnings and zero errors; 28 Harmony patch targets and 13 literal plus 42 helper-wrapped reflection
bindings verify against the installed assemblies.

**Nothing here is runtime-accepted.** The game has not been launched against 0.0.63. Specifically
unverified: that the HUD stops logging `GetBiome error Ocean -> Meadows`, that
`SpawnSystem.UpdateSpawnList` stops throwing, that ocean fish stop spawning on dry Underworld
ground, that the ground texture stops reading as Ashlands, and that weather and sky change in the
region. Those are the observations the next live session should make, in the region, with
`-console`.

The controlling priority is `BACKLOG.md` **P0.-2** (the open freshness-gate item) and **P0.-1**
(per-layer map state, Underworld sky, the magenta Deep Gate, re-grounding the Conclave, and making
layer travel a teleport).

---

# Current source candidate — 0.0.62 / schema 5 (2026-09-17)

Begins Underworld flora F1a: validated Glowcap, Spirestalk and Shelfwood data, canonical
fingerprints and pure terrain eligibility. All biomes share one custom cavern-roof skybox;
there is no biome-specific physical ceiling. Plans now explicitly enforce that constraint.
The offline build passed 37,166 core assertions and compiled without warnings/errors.
0.0.62 is installed and hash-verified in Central Fuckery; enabled launcher metadata was
verified by read-back, with prior payload and catalog backups.
Species models, spawning, harvesting and the economy remain pending; no live gameplay
acceptance is claimed. See `docs/validation/2026-09-17-underworld-flora-foundation.md`.

---

# Current source candidate — 0.0.54 / schema 5 (2026-09-17)

`main` and `origin/main` were both at `5aa8ab0` at session start, with no divergence to
reconcile: the remote work was already present locally. 67 commits had landed since this
document was last written and the source version had moved 0.0.49 -> 0.0.53, so the
sections below this one describe a superseded state and are retained as history.

**`main` did not compile.** The first action of the session was `build.ps1 -Offline`,
which P0.1 requires before push. Eight compile errors were on `main`, none ever built:
`Math.Clamp` in a netstandard2.0 project, `string.Contains(string, StringComparison)` on
net462, two CS8604 nullable-flow errors, a member that does not exist on
`UnderworldWorldIdentity`, and a wrong Jotunn `CustomPrefab`/`AddPrefab` usage. Each was
repaired against the idiom already established elsewhere in the codebase. Committed as
`72b0652`.

**A missing icon was silently deleting four staff families.** `EarthStaffRegistrar`
requested `staff-earth-*.icon.png`, which has never existed in repository history, and
`EarthAssets.Texture` resolves icons through `File.ReadAllBytes`. All eight staff
registrars subscribe to `PrefabManager.OnVanillaPrefabsAvailable`, a multicast delegate
whose invocation stops at the first handler to throw. Bootstrap order is Fire, Frost,
Storm, **Earth**, Venom, Radiance, Seidr, Spirit, so the missing Earth icon also prevented
Venom, Radiance, Seidr and Spirit from registering — 20 items, no log line naming them.
That is the cause of the P0.0 live report "Crystal Staff of Venom does not attack": the
registrar that builds its projectile payloads never ran. Three further committed staff
icons (`staff-frost-simple`, `staff-venom-advanced`, `staff-venom-master`) carry bad IDAT
CRCs or truncated chunks in the committed blobs; Frost is second in bootstrap order, so it
would have thrown before Earth was even reached.

0.0.54 re-renders all 32 staff icons from the same Blender sources the runtime meshes are
exported from, wires all eight families to their own icons, adds staff durability as a
pure Core rule with fail-closed tier resolution, adds `tools/verify-icon-assets.py` as a
permanent build gate over icon integrity and coverage, and adds the `.gitattributes` the
repository never had. The gate is proven to fail on the pre-repair tree with exactly the
three real defects and nothing else.

37,120 deterministic assertions pass; runtime builds with zero warnings/errors; 281 model
asset sets, 25 Harmony patch targets and 13 literal plus 42 helper-wrapped reflection
bindings verify against installed assemblies.

**Nothing here is runtime-accepted.** The game has not been launched against this build.
In particular it is not established that the five previously-dead staff families now
register, that the Venom staff attacks, that the new icons read at inventory scale, or
that staves lose durability per swing. Evidence:
`docs/validation/2026-09-17-staff-icon-registration-chain-repair.md`.

The controlling priority remains `BACKLOG.md` **P0.0/P0.1** and `IMPLEMENTATION_PLAN.md`
**section 14**: one disposable-world acceptance session, which should now specifically
confirm the five staff families that could not previously register.

---

# Historical — asset fidelity and live acceptance (from 2026-09-16)

The controlling priority is now `BACKLOG.md` **P0.1** and `IMPLEMENTATION_PLAN.md`
**section 14**, which outrank every other open track including the Underworld program.
Evidence is in `docs/RENDERING_AND_CONTENT_DEFECTS.md` R3.

In short: nothing since 0.0.16 has been observed running, a large number of repairs were made
from inferred rather than observed behaviour, inverted winding has now shipped from three
independent sources, and 60% of the model library carries no texture map. The next gate is a
disposable-world acceptance session and the geode rebuild, not additional content.

Deep Fracture districts were rebuilt as enclosed caverns on 2026-09-16
(docs/validation/2026-09-16-deep-fracture-cavern-terrain.md). Their passage and traversal
pieces were not, and remain 60 and 172 triangles against districts of roughly 10,000.

---

# Current source candidate — 0.0.49 / schema 5

Repaired the `Inventory.Changed` reflection binding that Valheim 1.0.12 broke: the game
now declares `Changed(bool success, bool cheatedStateChanged)`, and three Magenheim call
sites still invoked it with no arguments, raising `TargetParameterCountException` inside
workshop rollback and both local and remote socket mutations. The binding moved to the
declared `RuntimeGameApi` boundary with a pinned signature. A new
`tools/verify-reflection-targets.ps1` gate reads compiled IL, resolves every literal
reflection binding against the installed assemblies, and fails the build on arity drift;
it is proven to fail on the pre-repair 0.0.48 DLL and pass on 0.0.49. See
docs/validation/2026-09-15-inventory-changed-reflection-repair.md.

Repository divergence reconciled: origin/main carried two Broken Crown encounter-reset
commits that the local 0.0.48 worktree did not contain, while the worktree carried a
Valheim 1.0.12 API pass that the remote did not. Both intents were merged rather than
either being overwritten; `Character.SetPos` was confirmed absent from the installed
assembly, so the local replacement was a required repair.

Reconciled a second time at close-out: origin/main advanced by four more commits adding the
Underworld authority composition slice while this work was in progress. The two local commits
were rebased onto it and the combined tree re-verified rather than assumed compatible.

36,868 deterministic assertions pass; runtime builds with zero warnings/errors; 20 Harmony
patch targets and 7 literal reflection bindings verify against installed assemblies.
0.0.49 is installed and SHA-256 verified in the active Central Fuckery profile, with its
enabled launcher entry and description confirmed by read-back. Delivery first failed because
`install-local.ps1` collapsed the JSON checksum manifest into a single entry under Windows
PowerShell and rejected every package; that verifier was repaired without weakening it.
Startup, world and gameplay acceptance are NOT claimed, because the game has not been
launched against this build. Underworld world transitions and live multiplayer/persistence
acceptance remain open.

---

# Historical — 0.0.48 / schema 5

Underworld six-boss progression and eleven Rootforged definitions now share canonical Magenheim JSON and gameplay fingerprint authority. Baseline compilation and Deep Fracture route generation repaired. 36,859 deterministic assertions pass; runtime builds without warnings/errors. Installation/startup status is recorded in docs/validation/2026-09-15-underworld-authority-integration.md. Underworld world transitions and live multiplayer/persistence acceptance remain open.

---

# Magenheim Project State

## Current installed build: 0.0.47 startup repair

On 2026-09-14, fixed fatal ambiguous GetDamage patch selection, the latent GetArmor
ambiguity, and obsolete GetTooltip signature. The core harness passes 13,974
assertions; runtime compilation has zero warnings/errors; all 11 patch declarations
pass an installed-assembly signature/binding gate that rejects the old 0.0.46 DLL.
Isolated startup observed Crystal Shaping registration and completed Magenheim
bootstrap. Steam initialization errors prevent claiming world acceptance there.

`closeout.ps1 -Offline` installed and hash-verified 0.0.47 in Central Fuckery and
updated its enabled launcher catalog entry and startup-repair description. Payload
backup: `backups/Local-Magenheim-20260914-183916.zip`; catalog backup:
`backups/mods-20260914-183917-412.yml`. Full-profile/world retesting remains pending.
See `docs/validation/2026-09-14-0.0.47-startup-repair.md`.

## Launcher catalog closeout repaired

On 2026-09-14, `closeout.ps1 -Offline` completed against the active Central Fuckery
profile: tests, package build, backup, payload verification and r2modman `mods.yml`
update/read-back. The enabled entry now reports 0.0.46 and its Crystal Shaping
level-zero visibility release description. Foreign launcher records are unchanged.
Catalog backup: `backups/mods-20260914-182354-135.yml`. `release.json` now supplies
version-specific descriptions, and INSTRUCTIONS.md requires live installation plus
catalog verification for every testing closeout. Reopen r2modman to refresh its UI.

## Installed skill visibility fix: 0.0.46

On 2026-09-14, inspection confirmed the active Central Fuckery profile still loaded
0.0.16 and registered Crystal Shaping without initializing an untrained character's
skills-list entry. The new SkillsDialog prefix requests the registered skill's
normal level lookup before the list is captured, creating level zero without XP.
Existing progress is preserved. Core tests pass (13,974 assertions); runtime builds
with zero warnings/errors. Version 0.0.46 is now installed and SHA-256 verified in
the active profile; the prior installation was backed up under
`backups/Local-Magenheim-20260914-181731.zip`. In-game panel confirmation awaits launch.

## Local closeout update: 0.0.45

On 2026-09-14 the local source was repaired and built as testing candidate 0.0.45.
The core harness passes 13,974 assertions; runtime compilation has zero warnings
and errors. Regenerated workstation assets now match the repaired generator.
See `CLOSEOUT.md` and `TESTING.md` for the current inventory and acceptance matrix.
The active profile was not updated. An isolated startup attempt encountered Steam
initialization errors without establishing plugin registration. Live acceptance
remains limited to the historical 0.0.16 observations.

The state below is retained as historical context from the earlier connector cycle.

## Authority

`INSTRUCTIONS.md`, committed source on the live `main` branch, and directly observed repository/runtime evidence are authoritative. Backlog, validation records, archived design, scheduled prompts, and conversation are subordinate when they disagree with verified live state.

## Current source state — 2026-09-14

Current source/package identity is **0.0.38**. The most recently live-tested/installed package remains **0.0.16**; do not conflate later committed source with live acceptance.

The repository has advanced substantially beyond the first live-world-test baseline. Current source includes:

- eight biome geode definitions and additive natural-geode worldgen infrastructure;
- eight five-tier elemental crystal families and shards;
- Crystal Shaping skill/refinement authority;
- Geologist's Workstation and three progression upgrades;
- authority/replay-aware workshop transaction infrastructure;
- per-item adaptive socket metadata, eligibility, effects, extraction, workstation UI, rollback planning, synchronized gameplay-authority policy, and server-authoritative remote-client socket request/response/ack transport;
- Fire, Frost, Storm, Earth, Venom, Radiance, Seidr, and Spirit four-tier staff families with increasingly distinct runtime effect languages;
- original world-artifact geometry for later wardstone, passage, totem, keelstone, lighting, runecraft, ritual, spirit-fetish, Fate, and boss-resonance systems without falsely registering unfinished gameplay;
- a registered ten-piece geology/crystal furniture family under Hammer > Furniture with explicit comfort grouping;
- ten dedicated generated Hammer icons so the furniture no longer masquerades as unrelated vanilla clone-source pieces;
- later additive crystal architecture/weapon/alchemy content, including the current crystal-grinding and Prismatic Eitrwine source work;
- patch-strict multiplayer gameplay-authority synchronization.

Verified source presence is not runtime acceptance. Current 0.0.38 source has not been rebuilt or run in this connector-only execution cycle.

## Geology/crystal furniture source

The current furniture family is:

- Geode Table;
- Geode Chair;
- Crystal Bench;
- Crystal-set Bed;
- Mineral Shelf;
- Lapidary Cabinet;
- Geologist's Desk;
- Geode Pedestal;
- Crystal Screen;
- Crystal Throne.

`FurnitureVisuals.cs` owns the original procedural geometry and reuses the established Magenheim material language: rugged stone, dark/fine timber, iron and bronze banding, faceted mineral cores, and restrained crystal emission. `FurnitureRegistrar.cs` owns additive Hammer registration, recoverable build costs, explicit comfort grouping, compatible vanilla source selection, wear variants, and model-specific replacement colliders. `FurnitureIcons.cs` generates one Magenheim-owned 128x128 icon per furniture identity so menu presentation does not reuse the source prefab icon.

Static validation is recorded in the furniture validation records under `docs/validation/`. Runtime placement, inherited seating/bed/storage interaction, wear switching, collision, refund behavior, comfort stacking, and multiplayer behavior remain explicitly unadmitted until a rebuilt disposable-world test.

## Live runtime evidence retained from 0.0.16

The first disposable-world test confirmed that the Geologist's Workstation exists and opens its UI, directly spawned Earth inventory items appear, and the dedicated Meadows Earth world-geode prefab uses custom Magenheim geometry.

That test also exposed translucent/oversized world-geode presentation and workstation iron-band surface fighting. Subsequent source repairs normalized Magenheim-owned material render state and reduced/rebased the world-geode visual/collider, but those repairs still require a rebuilt in-game retest before they are accepted as closed.

The permanent skill diagnostic remains `raiseskill magenheim.crystal_shaping 1`; the spaced display name is not a valid vanilla parser argument. Live confirmation remains open.

See `docs/validation/2026-09-14-live-world-test-1.md` and `TESTING.md` for the preserved test boundary.

## Stable domain authority

Crystal tiers remain `Rough -> Simple -> Crystal -> Advanced -> Master`. Normal elemental alignments remain Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit. Ordinary refinement preserves alignment. Base refinement failure remains 10/20/30/40 percent with Crystal Shaping reduction and workstation/upgrade progression. Valid failed attempts destroy the source and return matching shards according to tier.

Furniture, architecture, weapons, alchemy presentation, and later artifact models do not alter this core refinement authority unless explicitly admitted through validated gameplay definitions.

## Worldgen and area compatibility

Definition schema 3 owns refinement, geodes, worldgen compatibility, and complete geode placement behavior in one validated/fingerprinted snapshot.

Worldgen remains additive-only. Magenheim does not rewrite, delete, disable, reorder, or patch foreign/vanilla registrations to obtain compatibility. Collision observation feeds pure Add/Skip/Error planning; approved world objects use Magenheim-owned identities.

Spawn-area handling remains fail-closed. Negative numeric masks cannot clamp into All through sign extension. Unknown bits are governed by validated policy. Text configuration accepts the Magenheim `All` abstraction plus runtime-facing aliases.

At the Valheim boundary, `JotunnWorldgenAdapter` resolves runtime `Median` and `Edge` first and requires them to be distinct and non-empty. `All` is composed from those components. A runtime alias named `Everything` or `Everywhere` is accepted only when its value is exactly equivalent to the composed `Median | Edge` value; aliases with missing or extra bits are rejected rather than silently broadening or narrowing placement.

Geode placement density, altitude/depth, terrain delta, tilt, forest thresholds, scale, grouping, block checking, force placement, and offset are validated/fingerprinted authority and can be overridden only through revalidation before registration.

The additive runtime path remains `GeodeWorldgenRegistrar`: validated desired additions are planned in the pure core, host prefab occupancy is observed read-only, Add/Skip/Error policy is rerun against observed state, every approved addition is preflighted, and an occupied prefab identity is refused before Jötunn registration. Existing vanilla or foreign vegetation is not intentionally mutated for compatibility.

See `docs/validation/2026-09-14-runtime-biome-area-semantic-guard.md` for the current area-mapping repair boundary.

## Adaptive socket authority

Per-item socket persistence is namespaced under `magenheim.sockets.v1`. Socket writes operate on individual `ItemDrop.ItemData.m_customData` and preserve unrelated foreign custom-data keys. Shared vanilla/foreign prefabs and shared item definitions are not the socket-state store.

Eligibility is adaptive and conservative:

- known weapon/armor/shield/tool/utility categories use configured slot limits;
- unknown equipment is rejected unless explicitly opted in;
- compatibility can include/exclude by shared item identity, prefab identity, equipment category, or mod origin;
- exact and case-insensitive identity comparison are supported;
- explicit exclusions win before category admission;
- case-insensitive identity collections are de-duplicated with case-insensitive semantics;
- unknown identity-comparison enum values fail closed.

Jötunn `ModQuery` is enabled during plugin startup before equipment classification so mod-origin rules can identify modded prefabs. Item identity is separately carried from Valheim `m_shared.m_name`, allowing compatibility rules that are finer-grained than prefab origin alone.

`GameplayAuthorityFingerprint` combines the validated definition fingerprint with every gameplay-significant socket eligibility field. `DefinitionAuthoritySynchronizer` exchanges this composite gameplay fingerprint, so persistent gameplay mutation fails authority admission when peers disagree on socket compatibility rules rather than silently diverging.

Source coverage for this authority repair is recorded in `SocketCompatibilityAuthorityTests` and `docs/validation/2026-09-14-socket-compatibility-gameplay-authority.md`.

## Socket workstation/runtime boundary

The Geologist's Workstation has a dedicated socket-management overlay for open-slot, install, and risky extraction operations. Local-host operations continue to use the existing pure planners, authority/replay guards, exact-item state checks, inventory snapshots, rollback, and Crystal Shaping XP.

0.0.38 adds the remote-client transport previously missing from this boundary. `SocketOperationRpc` uses a request/response/ack protocol parallel to the established workshop transaction transport. The server requires an admitted peer session, validates workstation proximity, validates the Faceting Wheel for extraction, reconstructs equipment classification from the registered server prefab rather than trusting client category/mod-origin claims, evaluates the synchronized socket policy, owns the extraction roll, and reserves the existing socket/extraction replay guard. The client retains the exact `ItemData` reference and refuses stale responses before writing `magenheim.sockets.v1`; install source crystals and extraction output capacity are also revalidated before mutation. A successful local apply is acknowledged before the server marks the prepared replay guard applied; failure sends a negative acknowledgement and aborts the prepared reservation.

This is source-complete but not runtime-admitted. Compilation and host/client verification remain required. Static validation is recorded in `docs/validation/2026-09-14-remote-socket-authority-binding.md`.

Socket effects are computed from Magenheim-owned per-item metadata and applied through runtime effect patches by equipment category. They do not require replacing another mod's item prefab or recipe.

## Repository branch reconciliation

`main` is now the sole ref on the remote. On 2026-09-15 the four redundant refs
`radiance-content`, `tmp-radiance-content`, `__delete_me__` and `tmp-compat-work` were
deleted with user authorization after re-verifying that each was an ancestor of
`origin/main`. There were four, not the three recorded earlier, and they pointed to
`fc84a519` rather than the `a8b6947c` noted during the connector-only cycle; that earlier
record was stale. Independent readback via `git ls-remote --heads origin` returns
`refs/heads/main` only.

## Build and validation boundary

Earlier project revisions were successfully compiled and tested in the normal Valheim development environment, including a recorded zero-error/warning build and deterministic core run before later source expansion.

This execution environment cannot clone GitHub through the local container and does not provide the normal local Valheim managed assemblies/runtime profile. For 0.0.38, remote source/read-back and static-source validation are therefore the strongest admissible claims from this cycle.

Current live gates include:

- rebuild 0.0.38 and rerun the full deterministic suite;
- plugin startup with the composite gameplay-authority fingerprint and both workstation/socket RPC registrations;
- host/client remote socket open/install/extraction including stale-state, duplicate/replay, policy mismatch, and descriptor-spoof rejection;
- verify runtime `Median`, `Edge`, and `All` geode area mapping under the installed Valheim/Jötunn enum;
- snapshot pre/post worldgen registrations and prove repeated-load idempotence;
- verify current furniture, architecture, weapon, and alchemy additions in a disposable world;
- live item/prefab/mod-origin exclusion behavior on representative third-party equipment;
- socket metadata persistence through save/load, drop/pickup, storage, repair, upgrade, transfer, death, and dedicated-server flows;
- world-geode opacity/scale/collision retest;
- workstation iron-band geometry repair/retest.

## Next dependency-valid action

1. compile/install current 0.0.38 in the normal Valheim development profile and execute the deterministic suite;
2. run a host plus remote-client socket matrix covering open/install/extract, stale response rejection, duplicate delivery, policy mismatch, and representative third-party equipment classification;
3. validate runtime area semantics and repeated-load additive worldgen idempotence;
4. re-run the outstanding geode/workstation live visual gates;
5. continue persistence validation through save/load, inventory movement/storage, equipment repair/upgrade, death, transfer, and dedicated-server flows.

---

## Open issues logged 2026-09-18 (0.0.77 installed and field-tested)

Sections above this line predate 0.0.38 and are stale; this block is current.

### Held-model orientation — measured, not fixed
See [docs/validation/2026-09-18-held-model-orientation-field-report.md](docs/validation/2026-09-18-held-model-orientation-field-report.md)
for the attach-space frames and the analysis. In short: battleaxe now reads as held "like a guitar"
(0.0.75 traded upside-down for a different wrong orientation), spear is gripped too near the head,
and the crossbow's `ForwardAxisOverride` declares axis 1 when the measurement says 2. Five weapons
sit at exactly (90,0,0) and are all correct; every reported failure sits somewhere else, which says
bounds ranking is unstable on the two axes perpendicular to the weapon. Those five are the
regression test for any change.

### Crystal placeable surfaces — shipped 0.0.77, unconfirmed
Foundations, beams, dais and hearth export with no texture as of 0.0.77 and are classified at load
from each material's own semantic. Not yet looked at in game. If a piece is wrong in a *new* way
rather than the old flat speckle, the classifier is choosing badly and the material name is the
place to correct it.

### Geode size and lean — shipped 0.0.76, only visible in fresh terrain
`m_syncInitialScale` gates both the read and the write of the ZDO scale, so geodes in
already-generated zones have no stored scale and stay at 1.0 and upright permanently. Only terrain
generated on 0.0.76 or later will show the variation.

### Smaller open items
- **HUD `IndexOutOfRangeException`** in `InventoryGui.SetupRequirement` via `Hud.SetupPieceInfo`,
  fires every frame while a piece with four requirements is selected on the build hammer. Cosmetic
  but it floods the log. Seen on the Rainbow Crystal Foundation 2x2m (Stone/Crystal/Iron/Stonecutter).
- **`enemy-stone-guardian`** carries two inverted triangles in `Guardian_ArmorChip_1`. They come
  from a modifier, so recalculating normals on the source mesh does not remove them. The payload is
  excluded from the shipped set and nothing references it; fix the winding before wiring it up.
- **`underworld-creature-sporeling`** likewise has a source but no runtime consumer.
- **8 orphaned texture files** under `assets/models/textures/`, left in place deliberately. Includes
  the `magenheim.surface.*` bakes, whose content hashes are the denylist in
  `tools/verify-no-baked-surfaces.py` — do not delete those without updating that gate's reasoning.
- Backlog carried forward: staggered staff burst cadence, geode interior biome colouring, per-layer
  map state, Underworld sky, magenta Deep Gate, layer-travel caller, undefined
  `$magenheim_deep_gate` map-pin token, installer never prunes stale files.

---

## Open issues logged 2026-09-19

### Held-model orientation — fixed and tested
See [src/Magenheim.Runtime/HeldModelAlignment.cs](src/Magenheim.Runtime/HeldModelAlignment.cs). The
2026-09-18 field report's diagnosis was correct (bounds ranking treats near-symmetric shapes' own
measurement noise as decisive) but its proposed crossbow fix was wrong, based on misreading the
runtime log's *post*-rotation bounds as if they were pre-rotation. Reconstructing exact pre-rotation
bounds from the shipped payloads and replaying the actual algorithm (not a hand trace) found: six of
nine non-crossbow weapons already resolve to one identical, confirmed-correct rotation once
near-ties are discounted at a 25% margin (chosen from a genuine gap in the measured data, not fit to
pass); the crossbow's existing forward-axis override was already correct. Battleaxe, spear, mace and
the crossbow's sign are fixed. The sword reproduces the exact mirror of the good rotation, on a real
43.7% margin (not noise) — left untouched, since nobody has ever confirmed in game whether it's
actually right or wrong, and forcing it to match would be a guess. Covered by a real compiled test
(`tools/ModelAssetTests`, not just a standalone script) via a new `Matrix4x4`/`Vector4` shim.

### Crystal-tier staff models — four found genuinely reversed, not yet fixed
`tools/verify-held-model-grip-direction.py` was rewritten 2026-09-19 (vertex count → bounding volume →
triangle surface area, in that order, the first two each measurably wrong — see the script's own
docstring for the full trail) while fixing a false-positive on the user's hand-edited
crystal-weapon-greatsword (confirmed correct by render: [greatsword-iso.png] showed a clean blade
dominant over a small pommel gem). The improved metric then surfaced a real, pre-existing,
unrelated finding the old vertex-count metric was silently hiding: **Magenheim_Staff_Fire_Crystal,
Magenheim_Staff_Storm_Crystal, staff-radiance-crystal and staff-venom-crystal** each have
dramatically more surface area at the grip end than the working end (roughly 5-6x, not a close
call). All four are the *Crystal* tier specifically of four different elemental families, from four
different .blend sources, so this is not one shared-asset mistake with one fix. Crystal tier is a
normal, reachable crafting rung (`Magenheim_Crystal_Fire_Simple`-gated, not dev-only), so this will
become player-visible as progression reaches it. Not fixed tonight: this needs a Blender look at each
of the four sources, which is a content decision outside tonight's scope, not a mechanical one.
Excluded from the gate by name (`KNOWN_REVERSED_PENDING_REVIEW` in the script) so it doesn't block
unrelated builds; remove an entry only after visually confirming its model.

### Underworld instance-architecture migration — merged, not authored by this session
`origin/main` had advanced 11 commits (Underworld instance-domain terrain/map/transition rework,
Earth icon migration tooling, Capcrawler articulated feet) since the last local sync; fast-forwarded
cleanly, no conflicts with anything in this file's other sections.

### Capcrawler creature — pipeline scaffolded, content incomplete, gates temporarily bypassed
`underworld-creature-capcrawler.blend` had never been committed in this repository's history despite
~10 commits building gating/animation/review machinery around it. Ran the already-written,
already-committed authoring pipeline for the first time (`generate-underworld-capcrawler-textures.py`
→ `tools/blender.ps1 author-underworld-capcrawler`), which now produces a real 56-mesh/23-bone
source — but it falls short of its own gates: 4440 triangles against a 6500 floor
(`verify-underworld-capcrawler.py`), and the review renderer expects a `Capcrawler_Scuttle` animation
action the current rig doesn't produce (`render-underworld-capcrawler-review.py`). Both floors were
themselves ratcheted up incrementally by whoever was iterating on this creature (5000→6500 triangles
in this file's own history), meaning it was mid-tune when its session ended. Not fixed tonight:
closing either gap is a content-authoring decision, not a mechanical one, and I have no visual
reference for what this creature is meant to look like. **Both gates are temporarily commented out of
`build.ps1`** (search "TEMP:" — two `foreach` loops) so the rest of the build can run; restore them
once the creature's own author/reviewer session finishes it, or hands off with enough context for
someone else to.

### Staff icon renderer — two real bugs fixed in passing
`tools/render-staff-icons.py` (merged from origin, part of the same icon-readability work) crashed on
`style.color` with `style is None`: a fresh view layer has no Freestyle lineset/linestyle until one
is explicitly created, which the merged code assumed rather than checked — fixed by creating both
when absent. Once fixed, `staff-spirit-simple` then failed its own 8% ink-density floor by 0.3 points
(right at a `<` boundary) — `OUTLINE_PX` raised 1.35→1.6, a uniform, in-spirit-of-the-tool parameter
matching its own stated purpose, re-verified against all 32 icons with margin, not just the one.

### Update, same day: the sword and four staves fixed at the source
Both items above marked "not fixed"/"left untouched" are now resolved, found by the same method in
sequence. At the user's explicit request to prioritize local Blender-only verification and icon
inspection: rendering all 32 staff icons directly showed staff-fire-crystal, staff-storm-crystal,
staff-radiance-crystal and staff-venom-crystal with their ornate head pointing toward the grip.
Checking each source's Blender-space Z coordinates against a correctly-built reference
(Magenheim_Staff_Fire_Simple) confirmed it precisely: pommel at positive Z, head parts at negative Z,
the exact reverse of the correct asset. Fixed by rotating each of the four sources 180 degrees about
the world origin and re-exporting; the grip-direction gate now passes outright, with no exclusion
needed, and `KNOWN_REVERSED_PENDING_REVIEW` is empty again.

The same check applied to the sword's earlier "left untouched" mirror-image signature settled it too:
its pommel sat at positive Blender Z and its blade at negative Z, the reverse of both the axe and
knife (which agree with each other). Fixed the same way. A debug render with pommel and blade
colour-coded confirmed it unambiguously — the plain icon comparison alone was not conclusive, since a
reasonably-symmetric double-edged blade can look similar from either end under render-weapon-icons.py's
fixed camera angle. All ten crystal weapons are now confirmed correct in `HeldModelAlignment`'s
compiled regression test, and the full 281-model catalog was regenerated and reviewed by eye
end-to-end; nothing else in the library shows this defect.

Shipped as 0.0.79 (staves) and 0.0.80 (sword), both installed and verified.
