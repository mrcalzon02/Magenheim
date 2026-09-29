# Underworld resource and drop mapping — 0.0.97

Raw resource names follow the existing flora/terrain plan. All 22 have unique item identities and native Pickable nodes. Runtime visual authority now maps every raw material to an owned Magenheim model/icon identity. Eight Fungal/Blackwater raw models are already committed; the fourteen later-biome raw models are authored by the pending one-run production source and are not claimed generated until that forge output is accepted. Donors supply behavior, not final appearance.

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

Each registered pickup produces one matching custom material through native Pickable behavior. It has no extra donor loot or respawn timer. Resident native Underworld chunks now deterministically materialize only that biome's registered pickups through the server-owned chunk-streaming authority and native Valheim ZDO/ZNetView persistence; picked state is not tied to disposable ecology scenery or a local-player preview. All pickup prefabs remain console-spawnable for review. Registration now requires `m_hideWhenPicked`; with zero respawn time this is the native Valheim condition that preserves the picked flag in the pickup ZDO instead of destroying that ZDO. Spawn IDs are `Magenheim_Underworld_ResourcePickup_<NameWithoutSpaces>`; loose item IDs are `Magenheim_Underworld_Resource_<NameWithoutSpaces>`.

The 42 ordinary Underworld fauna no longer retain donor loot. Their CharacterDrop tables are replaced by Magenheim-owned biome-resource drops plus one species trophy. The twelve elemental Surtlings likewise replace donor loot with home-biome resources plus one trophy per element, shared across masculine/feminine bodies. The thirteen Deep Fracture chassis now drop Fracture-tier resources plus one trophy per chassis, shared across elemental alignments.

Creature loot supplements rather than replaces harvesting. Common fauna mostly return common biological/mineral inputs; rarer heavy/apex creatures provide an alternate source for progression bottlenecks such as Blackwater Pearl, Emberiron, Rimesilver, Titanbone and Carrion Amber. Static source coverage verifies that all 22 canonical raw resources have at least one creature source and at least one existing station/equipment/weapon/refining consumer.

The 72 scenery variants remain stripped visuals and cannot be harvested. Do not add resource drops to disposable local scenery: its rebuild would replenish the same ground repeatedly.

Pending: live creature-drop/trophy acceptance, remaining native mining/tree conversion work, and multiplayer/save/reload acceptance.

## Custom appearances — expanded 2026-09-29

The four Fungal Forest and four Blackwater raw resources already carry committed authored models,
model-derived icons and fitted colliders. Source authority now extends the same rule to all remaining
raw resources and all eighteen refined materials. The fourteen missing raw plus eighteen refined
models are queued in `underworld-material-item-models`; `underworld-resource-icons` owns icons for
the complete 40-item material family. The production gate requires source/GLB/runtime/icon coverage
for all forty and shared PBR normal/metallic maps for all thirty-two newly authored models. Those
thirty-two outputs remain pending until the manual Blender forge runs and its generated branch is accepted.

## Closeout

0.0.97 installed and hash-verified in Central Fuckery. Runtime compiled without warnings/errors; 43,504 Core assertions plus separate suites passed. Catalog tests check unique item/pickup identities, coverage of all six biomes and compatibility with the existing Worldroot Timber/Understone IDs. Live registration and pickup/save/multiplayer acceptance remain untested. Local closeout log: dist/resource-closeout.log.


## First economy consumer slice — 2026-09-29

The first dependency-valid economy consumers are now in source. Rootforged construction resolves
Worldroot Timber and Understone to these registered resource prefabs instead of the temporary
RoundLog/Stone aliases. Fungal provision recipes consume Glowcap Flesh. The Mycelial Bench is built
from Worldroot Timber, Understone and Spire Fibre, and the first two planned Fungal Forest weapons
are admitted there as upgrades of existing crystal weapons: Worldroot Club consumes a Crystal Mace
and Worldroot Bow consumes a Crystal Bow. Their runtime visuals inherit the existing crystal chassis
and receive a Fungal/Worldroot material accent; authored derivative geometry remains a separate asset
pass. All six biome weapon tiers are now runtime-admitted at their matching stations. Post-Fungal weapon and equipment recipes consume processed station outputs rather than raw pickup stacks.


### Fungal Forest refining rung — 2026-09-29

The first parity tier no longer jumps directly from loose pickups to finished gear. The Mycelial
Bench refines Worldroot Timber into Worldroot Planks, Spire Fibre into Spire Cord, and Glowcap Flesh
into Cured Glowcap. Worldroot Club and Worldroot Bow now require those refined identities in
addition to consuming their Crystal Mace / Crystal Bow chassis. Rootforged remains allowed to use
raw Worldroot Timber and Understone as building stock; food remains allowed to consume raw Glowcap
Flesh. This creates separate raw-building, raw-food and refined-equipment sinks instead of making
every material perform the same job.


### Complete station refining ladder — 2026-09-29

The five post-Fungal stations now add fifteen processed materials, three per biome, alongside the
three existing Mycelial refinements. Blackwater produces Flowstone Plate, Pale Cord and Brined Pearl;
Sulfur produces Emberiron Bar, Tempered Slag and Charred Root Grip; Frozen produces Rimesilver Bar,
Iceglass Lens and Rimewood Laminate; Fracture produces Titanbone Plate, Shardstone Block and Fracture
Prism; Great Decay produces Carrion Amber Seal, Ossuary Composite and Rotwood Laminate. Every one of
the fifteen later refinements has at least one admitted equipment or weapon consumer, and Core tests
reject dead refinement identities or raw-resource bypasses in post-Fungal gear.
