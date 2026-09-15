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
