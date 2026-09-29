# 2026-09-29 — Underworld foundational station authoring begins

## Intent

Replace the temporary "one tinted vanilla workbench" economy seam with the six-station crafting
ladder already required by `UNDERWORLD_FLORA_TERRAIN_PLAN.md`. The station set is a progression
system and an art family, not six recoloured donors.

## Source authority added

`UnderworldStationCatalog` now owns the six canonical station prefabs, unique model identities,
biome ownership, build costs, approximate collider envelopes, prior-station construction gate and
the intended environmental siting rule:

1. Mycelial Bench — Fungal Forest — open-ground foothold.
2. Tidal Basin — Blackwater Deep — shoreline.
3. Furnace Heart Forge — Sulfurous Wastes — geothermal vent.
4. Silence Table — Frozen Caverns — quiet ground.
5. Anchor Forge — Fracture Zones — stable ground.
6. Crown Reliquary — Great Decay — Deepstone-attuned endgame.

Every station after the first requires the immediately preceding Underworld station as its Hammer
construction station. This closes the easiest progression bypass: arriving with late-biome raw
materials is not sufficient to skip the crafting ladder.

Weapon station constants now alias this station catalog instead of maintaining a parallel set of
string identities.

## Runtime seam

The existing `UnderworldMycelialBenchRegistrar` bootstrap slot now registers all six stations in
catalog order. Native Valheim donors provide only proven `CraftingStation`, `Piece`, persistence and
network behavior. When an exported `underworld-station-*` payload is installed, `ModelAssets` hides
the donor renderers and loads the owned model. A bounded replacement collider is fitted to the
station definition rather than leaving the invisible donor's collision shape active.

Until the local Blender rebuild is run, the donor renderer remains as a deliberately obvious
fallback. That fallback is not final art and is not a completion claim.

## Art direction encoded in the authoring tool

`tools/author-underworld-stations.py` authors six independent silhouettes rather than recolours:

- Mycelial Bench: grown Worldroot frame, Understone feet, fungal task pods and fibre lashings.
- Tidal Basin: broad Flowstone water bowl, pearl-working arms, salt racks and Pale Fibre ties.
- Furnace Heart Forge: massive slag hearth, Emberiron cage, chimney/vent stack and contained heat core.
- Silence Table: low Rimewood precision table, Clear-Ice optical array and Rimesilver instrument arms.
- Anchor Forge: Shardstone anvil mass, Titanbone braces, fracture anchors and heavy suspended tooling.
- Crown Reliquary: Rotwood/bone altar, Carrion-Amber reliquary cage and elevated crown assembly.

The station family is now `endgame-station-r2` with per-station regression floors of roughly
52–58 mesh parts. The added geometry is functional rather than indiscriminate ornament: joinery,
drawers, spools and sockets on the Mycelial Bench; drains/calipers on the Tidal Basin; bellows,
tuyeres, gauges and quench hardware on the Furnace Heart; damping feet, vice and micrometer hardware
on the Silence Table; bearings, hoist, brace locks and force gauges on the Anchor Forge; and sealed
drawers, amber clamps, ritual fittings and crown ribs on the Crown Reliquary.

## Local production path

`tools/rebuild-underworld-stations.ps1`

The command authors the six Blender sources, exports GLB/runtime payloads, rebuild-verifies the model
catalog, renders matching Hammer icons and runs the icon gate.

## Runtime siting enforcement

The environmental siting semantics are now executable Core authority and are enforced by Hammer
placement without bypassing vanilla placement rules. Every station first requires its canonical
Underworld biome. The Tidal Basin samples its full footprint for simultaneous dry bank and Blackwater;
the Furnace Heart Forge requires proximity to one of three persistent owned geothermal-vent
vegetation prefabs; the Silence Table and Anchor Forge use deterministic low-hazard/low-relief
terrain pockets rather than transient weather; and the Crown Reliquary requires the completed Decay
Deepstone, whose prerequisite graph implies the full preceding Conclave chain. The same decision is
rechecked in TryPlacePiece so a stale ghost cannot bypass the frame-time placement check. A deployed
Anchor Spike can satisfy Fracture stability, but cannot waive the Anchor Forge's level-ground rule.

The revised r2 Blender sources, actual Valheim rendering, station interaction, multiplayer and
save/reload still require the ordinary production/live acceptance pass.
