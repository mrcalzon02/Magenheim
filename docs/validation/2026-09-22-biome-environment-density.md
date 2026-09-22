# Biome environment enrichment — 0.0.96

Expands the donor-derived ground-cover palette from 48 to 72 entries, twelve per biome. All additions reuse donor prefab names already present in the scenery catalog. These are visual variants, not new creature models, harvestable resources or player-placeable pieces.

| Biome | Added environmental forms |
|---|---|
| Fungal Forest | Rose spore fern fan, turquoise cap nursery, amber root skirt, violet canopy sapling |
| Blackwater Deep | Silver bank fern, drowned root fingers, pearl silt cobbles, teal bank thicket |
| Sulfurous Wastes | Rust fan scrub, sulfur mineral shelf, black glass splinter, ochre ash thicket |
| Frozen Caverns | Blue rime fan, lavender frost brush, pale glacier plate, silver sheltered fern |
| Fracture Zones | Copper fault rubble, violet crevice fan, quartz needles, plum ridge thicket |
| Great Decay | Burgundy rot fern, ivory shelf caps, sour root spires, ochre rot thicket |

The existing local ecology preview now uses twelve clusters: four anchors 28–53m away in separate angular sectors and eight anchors 65–145m away. Existing biome-specific grove, root-corridor, shard-fan and fault-line composition remains. Refresh distance is 90m instead of 180m so the player does not walk past the local scenery patch before it refreshes. This retains the existing disposable local preview; it does not claim persistent world vegetation.

At most 84 main forms plus 336 ground-cover objects are proposed per rebuild (420 total in lush biomes, 252 in sparse biomes), up from 280/168. Terrain, matching biome, water height and slope admission can reduce those totals. Cover remains noncolliding. Blackwater ferns and shrubs remain on banks; drowned roots and mineral shelves have separate signed water-height ranges. No additional lights, custom mesh generation, AI, map or save system is introduced.

Tests compile the actual runtime donor catalog through existing model-test shims and verify distinct names, positive scales, valid habitat ranges, nonblocking cover and rejection of underwater bank ferns. Runtime donor availability and final appearance require in-game review, as do frame-time and refresh visibility under dense vegetation.

Full closeout passed: 300 models, 134 icons, runtime palette habitat checks, 43,426 Core assertions plus separate suites. Runtime compilation: zero warnings/errors. Installed 0.0.96 with all payload hashes and enabled launcher metadata verified. DLL SHA-256: D7E5EC1C4C0417758A0EA4DE7A7C603B2D2225841BD85606626AD32CE4EE42F9. Backups: backups/Local-Magenheim-20260922-150946.zip and backups/mods-20260922-150958-618.yml. Local log: dist/biome-environment-closeout.log.
