# Magenheim Project State

## Authority

`INSTRUCTIONS.md`, committed source on the live `main` branch, and directly observed repository/runtime evidence are authoritative. Backlog, validation records, archived design, scheduled prompts, and conversation are subordinate when they disagree with verified live state.

## Current source state — 2026-09-14

Current source/package identity is **0.0.32**. The most recently live-tested/installed package remains **0.0.16**; do not conflate later committed source with live acceptance.

The repository has advanced substantially beyond the first live-world-test baseline. Current source includes:

- eight biome geode definitions and additive natural-geode worldgen infrastructure;
- eight five-tier elemental crystal families and shards;
- Crystal Shaping skill/refinement authority;
- Geologist's Workstation and three progression upgrades;
- authority/replay-aware workshop transaction infrastructure;
- per-item adaptive socket metadata, eligibility, effects, extraction, workstation UI, rollback planning, and synchronized gameplay-authority policy;
- Fire, Frost, Storm, Earth, Venom, Radiance, Seidr, and Spirit four-tier staff families with increasingly distinct runtime effect languages;
- original world-artifact geometry for later wardstone, passage, totem, keelstone, lighting, runecraft, ritual, spirit-fetish, Fate, and boss-resonance systems without falsely registering unfinished gameplay;
- a registered ten-piece geology/crystal furniture family under Hammer > Furniture;
- ten dedicated generated Hammer icons so the furniture no longer masquerades as unrelated vanilla clone-source pieces;
- patch-strict multiplayer gameplay-authority synchronization.

Verified source presence is not runtime acceptance. Current 0.0.32 source has not been rebuilt or run in this connector-only execution cycle.

## Geology/crystal furniture — 0.0.32 source

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

`FurnitureVisuals.cs` owns the original procedural geometry and reuses the established Magenheim material language: rugged stone, dark/fine timber, iron and bronze banding, faceted mineral cores, and restrained crystal emission. `FurnitureRegistrar.cs` owns additive Hammer registration, recoverable build costs, compatible vanilla source selection, wear variants, and model-specific replacement colliders. `FurnitureIcons.cs` generates one Magenheim-owned 128x128 icon per furniture identity so menu presentation does not reuse the source prefab icon.

Static validation is recorded in `docs/validation/2026-09-14-furniture-icons-static.md`. Runtime placement, inherited seating/bed/storage interaction, wear switching, collision, refund behavior, and multiplayer behavior remain explicitly unadmitted until a rebuilt disposable-world test.

## Live runtime evidence retained from 0.0.16

The first disposable-world test confirmed that the Geologist's Workstation exists and opens its UI, directly spawned Earth inventory items appear, and the dedicated Meadows Earth world-geode prefab uses custom Magenheim geometry.

That test also exposed translucent/oversized world-geode presentation and workstation iron-band surface fighting. Subsequent source repairs normalized Magenheim-owned material render state and reduced/rebased the world-geode visual/collider, but those repairs still require a rebuilt in-game retest before they are accepted as closed.

The permanent skill diagnostic remains `raiseskill magenheim.crystal_shaping 1`; the spaced display name is not a valid vanilla parser argument. Live confirmation remains open.

See `docs/validation/2026-09-14-live-world-test-1.md` and `TESTING.md` for the preserved test boundary.

## Stable domain authority

Crystal tiers remain `Rough -> Simple -> Crystal -> Advanced -> Master`. Normal elemental alignments remain Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, and Spirit. Ordinary refinement preserves alignment. Base refinement failure remains 10/20/30/40 percent with Crystal Shaping reduction and workstation/upgrade progression. Valid failed attempts destroy the source and return matching shards according to tier.

Furniture and later artifact models do not alter this gameplay authority. They are additive Magenheim-owned content and presentation layers.

## Worldgen and area compatibility

Definition schema 3 owns refinement, geodes, worldgen compatibility, and complete geode placement behavior in one validated/fingerprinted snapshot.

Worldgen remains additive-only. Magenheim does not rewrite, delete, disable, reorder, or patch foreign/vanilla registrations to obtain compatibility. Collision observation feeds pure Add/Skip/Error planning; approved world objects use Magenheim-owned identities.

Spawn-area handling remains fail-closed. Negative numeric masks cannot clamp into All through sign extension. Unknown bits are governed by validated policy. Text configuration accepts the Magenheim `All` abstraction plus runtime-facing aliases. At the Valheim boundary, the adapter resolves current `Heightmap.BiomeArea` names dynamically and can compose the defined Median/Edge flags when a named combined member is absent, avoiding integer-cast assumptions.

Geode placement density, altitude/depth, terrain delta, tilt, forest thresholds, scale, grouping, block checking, force placement, and offset are validated/fingerprinted authority and can be overridden only through revalidation before registration.

## Adaptive socket authority

Per-item socket persistence is namespaced under `magenheim.sockets.v1`. Socket writes operate on the individual `ItemDrop.ItemData.m_customData` dictionary and preserve unrelated foreign custom-data keys. Shared vanilla/foreign prefabs and shared item definitions are not the socket-state store.

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

The Geologist's Workstation source includes a dedicated socket-management overlay. Local-host operations can plan/open sockets, install crystals, and extract crystals using per-item state, authority/replay guards, inventory snapshots, rollback, and Crystal Shaping XP. Remote-client socket mutation remains intentionally unadmitted until its server request/approval RPC path is bound and validated.

Socket effects are computed from Magenheim-owned per-item metadata and applied through runtime effect patches by equipment category. They do not require replacing another mod's item prefab or recipe.

## Repository branch reconciliation

`main` remains the sole authoritative development branch by policy. Historical redundant refs (`radiance-content`, `tmp-radiance-content`, and `__delete_me__`) were previously proven to contain no unique material work relative to `main`; deletion remains repository cleanup when branch-ref deletion capability is available.

## Build and validation boundary

Earlier project revisions were successfully compiled and tested in the normal Valheim development environment, including a recorded zero-error/warning build and deterministic core run before later source expansion.

This connector-only execution cycle did not invoke the normal local .NET/Valheim build/runtime environment. For 0.0.32, remote source/read-back and static-source validation are therefore the strongest admissible claims from this cycle.

Current live gates include:

- rebuild 0.0.32 and rerun the full deterministic suite;
- plugin startup with the composite gameplay-authority fingerprint;
- verify all ten furniture entries appear under Hammer > Furniture with distinct Magenheim icons;
- place all ten furniture pieces and validate collision/wear/refund behavior plus representative inherited chair/throne, bed, and cabinet interactions;
- client/server rejection when socket policies differ;
- live item/prefab/mod-origin exclusion behavior on representative third-party equipment;
- socket metadata persistence through save/load, drop/pickup, storage, repair, upgrade, transfer, death, and dedicated-server flows;
- remote-client socket request/approval RPC;
- world-geode opacity/scale/collision retest;
- workstation iron-band geometry repair/retest;
- repeated-load worldgen idempotence and natural geode generation acceptance.

## Next dependency-valid action

1. rebuild/install current 0.0.32 in the normal Valheim development profile and execute the deterministic suite;
2. in a disposable world, validate the ten-piece furniture collection and its generated Hammer icons, then repair any source-prefab, interaction, collider, wear, or material defect at the authoritative source;
3. re-run the outstanding geode/workstation live visual gates in the same build;
4. continue remote-client socket authority and representative mod-compatibility validation after the build is clean.
