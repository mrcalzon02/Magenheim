# Underworld resource and drop mapping — 0.0.97

Raw resource names follow the existing flora/terrain plan. All 22 now have unique item identities backed by vanilla models/icons and native Pickable review nodes. They are materials, including Glowcap Flesh: raw-food effects are not yet balanced or enabled. No smelting recipes, new stations or world harvesting claims are implied.

| Biome | Resource | Item model/icon donor | Pickup donor | Intended source (not populated yet) |
|---|---|---|---|---|
| FungalForest | Worldroot Timber | `RoundLog` | `Pickable_Branch` | Shed root and dead fungal trunks |
| FungalForest | Glowcap Flesh | `MushroomMagecap` | `Pickable_Mushroom_Magecap` | Glowcap clusters |
| FungalForest | Spire Fibre | `Flax` | `Pickable_Branch` | Spirestalk fibre bundles |
| FungalForest | Understone | `Stone` | `Pickable_Stone` | Loose understone and outcrops |
| BlackwaterDeep | Blackwater Flowstone | `Stone` | `Pickable_Stone` | Shore and lakebed flowstone |
| BlackwaterDeep | Pale Fibre | `Flax` | `Pickable_Branch` | Bank vegetation |
| BlackwaterDeep | Blackwater Pearl | `AmberPearl` | `Pickable_Stone` | Shore deposits; shellfish loot pending |
| BlackwaterDeep | Deep Salt | `Crystal` | `Pickable_Stone` | Shore salt crusts |
| SulfurousWastes | Slagstone | `Stone` | `Pickable_Stone` | Volcanic scree |
| SulfurousWastes | Sulfur | `Resin` | `Pickable_Stone` | Vent mineral crusts |
| SulfurousWastes | Charred Timber | `Coal` | `Pickable_Branch` | Charred root debris |
| SulfurousWastes | Emberiron | `IronScrap` | `Pickable_Stone` | Ore seams; mining conversion pending |
| FrozenCaverns | Rimewood | `RoundLog` | `Pickable_Branch` | Frozen root debris |
| FrozenCaverns | Clear Ice | `Crystal` | `Pickable_Stone` | Ice deposits |
| FrozenCaverns | Rimesilver | `SilverOre` | `Pickable_Stone` | Ore seams; mining conversion pending |
| FractureZones | Fracture Crystal | `Crystal` | `Pickable_Stone` | Crystal seams |
| FractureZones | Shardstone | `Stone` | `Pickable_Stone` | Fault scree |
| FractureZones | Titanbone | `BoneFragments` | `Pickable_Stone` | Ancient bone deposits |
| GreatDecay | Rotwood | `Wood` | `Pickable_Branch` | Decaying root debris |
| GreatDecay | Decay Spore | `Ooze` | `Pickable_Mushroom_Magecap` | Rotcap clusters |
| GreatDecay | Carrion Amber | `Amber` | `Pickable_Stone` | Amber-bearing deposits |
| GreatDecay | Bone Gravel | `BoneFragments` | `Pickable_Stone` | Ossuary scree |

## What actually drops

Each new console-spawned pickup produces one matching custom material through native Pickable behavior. It has no extra donor loot or respawn timer. Spawn IDs are `Magenheim_Underworld_ResourcePickup_<NameWithoutSpaces>`; loose item IDs are `Magenheim_Underworld_Resource_<NameWithoutSpaces>`. Example: `spawn Magenheim_Underworld_ResourcePickup_WorldrootTimber 1`.

The 42 creature review prototypes still retain vanilla donor loot. No per-creature custom resource drops are claimed. The 72 scenery variants are stripped visuals and cannot be harvested. Rootforged still consumes core wood/stone/iron; fungal provisions still use their original vanilla ingredients. Those recipes stay playable until natural resource acquisition is admitted.

Pending: biome-valid persistent node placement, native mining/tree donor conversion, creature-specific loot balance, food/refining recipes, multiplayer/save/reload and item pickup acceptance in-game. Do not add resource drops to disposable local scenery: its rebuild would replenish the same ground repeatedly.

## Custom appearances — 2026-09-22

The four Fungal Forest resources (Worldroot Timber, Glowcap Flesh, Spire Fibre, Understone) now
carry authored models, icons rendered from those models, and a collider fitted to the model, on both
the item and its pickup. The donors in the table above still supply item behaviour (stacking,
physics, pickup interaction); only their appearance is replaced. The other eighteen resources keep
their vanilla appearance until their biome's custom pass. See
`docs/validation/2026-09-22-fungal-forest-custom-pass.md`.

## Closeout

0.0.97 installed and hash-verified in Central Fuckery. Runtime compiled without warnings/errors; 43,504 Core assertions plus separate suites passed. Catalog tests check unique item/pickup identities, coverage of all six biomes and compatibility with the existing Worldroot Timber/Understone IDs. Live registration and pickup/save/multiplayer acceptance remain untested. Local closeout log: dist/resource-closeout.log.
