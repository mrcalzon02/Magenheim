# Underworld donor prototype review — 2026-09-21

> 0.0.90 scope correction: infrastructure in this task means natural terrain/scenery features, not player construction. No Hammer entries or new player-buildable content are introduced. The 18 console review identities from 0.0.89 are retained only for compatibility; the infrastructure section below is historical. Current work focuses on vegetation, roots, rock formations, banks and lakebeds.

## 0.0.90 natural scenery refinement

The ground-cover palette now contains 48 variants (eight per biome). Added broad jade fern and fungal stepping slab; Blackwater root fan and lakebed stone shelf; cinder thorn fan and basalt stepping slab; low rime fern and ice scree shelf; fault fan scrub and fractured shale plate; rotroot fern fan and carrion root mat. These reuse donor geometry with distinct proportions and colors; they are not newly authored plant meshes.

Each variant declares signed height-above-water and slope limits. Blackwater bank plants stay within four meters above water; lakebed shelves occupy submerged ground down to 30 meters; roots and rubble have their own ranges. All four neighboring terrain samples must remain inside the biome, and the steepest neighbor differences determine slope. Missing or unsupported donors fall through to another habitat-compatible variant. If no habitat fits, the patch stays bare.

The preview now uses the existing six biome-specific placement compositions: fungal groves, sparse Blackwater banks, sulfur outcrop chains, frozen shard fans, fracture fault lines and decay root corridors. Ground-cover randomness is separate from canopy layout, so changing fill density no longer rearranges subsequent canopy positions. Existing disposable-preview and terrain lifecycles remain in charge.



Current direction: content expansion using Valheim-owned animation, AI, combat, networking and saving. The earlier bespoke HOST rig requirements are final-art aspirations, not prerequisites for these rough prototypes. Existing authored art is preserved.

42 non-boss creatures are registered as console-spawnable donor prototypes, seven per biome. These are tinted, uniformly scaled donor bodies with their original skeleton, controller, clips, events, attack items, colliders, AI, faction and drops. They are not finished unique meshes or balanced encounters. No natural spawns are enabled by this review pass. Ambient roles are design intentions: Bat/Serpent stand-ins still use hostile donor behavior. Use a disposable review world.

Spawn with `spawn Magenheim_Underworld_Prototype_<NameWithoutSpaces> 1` after enabling the normal Valheim developer console. Donor availability and animation controller are checked during registration; the log reports the actual registered count. Exact bone paths are deliberately not guessed: the whole native hierarchy remains intact.

| Biome | Creature | Donor skeleton/controller | Uniform scale | Prototype limitation |
|---|---|---|---:|---|
| Fungal Forest | Lantern Moth | Bat | 0.65 | Winged hover/bite stand-in; ambient temperament and four-wing art pending |
| Fungal Forest | Sporeling | Tick | 0.6 | Native scuttle/latch; fungal cap art pending |
| Fungal Forest | Capcrawler | Seeker | 0.65 | Native insect walk/bite/flight; ground-only behavior not claimed |
| Fungal Forest | Mycelial Stalker | Wolf | 1.05 | Native quadruped run/bite; no custom concealment or pounce |
| Fungal Forest | Puffback | Lox | 0.8 | Native graze/walk/stomp; no inflation or spore burst |
| Fungal Forest | Shelf Lurker | Seeker | 1 | Native insect movement; no wall climbing |
| Fungal Forest | Crowncap Brute | Troll | 0.9 | Native heavy biped swings; crown/gill art pending |
| Blackwater Deep | Cave Ray | Serpent | 0.45 | Swim-chain motion proxy only; no fin-wing deformation or ambient temperament |
| Blackwater Deep | Gloomfin | Serpent | 0.55 | Native swimming/bite; fish body art pending |
| Blackwater Deep | Blackwater Lamprey | Leech | 1 | Native swimming/bite; no attached-player latch |
| Blackwater Deep | Shoreclaw | Neck | 1.5 | Native amphibious walk/swim/bite; shell and claws pending |
| Blackwater Deep | Lantern Angler | Serpent | 0.7 | Native swim/bite; no lure or ranged pressure attack |
| Blackwater Deep | Abyss Shellback | Neck | 2 | Amphibious motion proxy; shell armor and guard pending |
| Blackwater Deep | Deep Hunter | Serpent | 1.2 | Native large aquatic predator; hero anatomy pending |
| Sulfurous Wastes | Ashmite | Tick | 0.45 | Native scuttle/latch; heat-shell art pending |
| Sulfurous Wastes | Cinder Hound | Wolf | 1 | Native run/bite; heat-adapted art pending |
| Sulfurous Wastes | Basalt Crawler | SeekerBrute | 0.65 | Native armored insect locomotion/strikes |
| Sulfurous Wastes | Vent Spitter | Neck | 1.25 | Quadruped motion proxy; no pressure sac or ranged attack |
| Sulfurous Wastes | Fume Wraith | Wraith | 1 | Native spectral flight/melee; no new smoke rig |
| Sulfurous Wastes | Magma Leaper | Fenring | 0.75 | Native leap/attack; biped donor silhouette retained for review |
| Sulfurous Wastes | Furnace Golem | StoneGolem | 1.1 | Native heavy construct attacks; no armor-break mechanic |
| Frozen Caverns | Rime Moth | Bat | 0.7 | Winged motion proxy; crystalline wing anatomy pending |
| Frozen Caverns | Frost Tick | Tick | 0.7 | Native tick locomotion/latch |
| Frozen Caverns | Iceblind | Wolf | 1.1 | Native quadruped locomotion; sensory head art pending |
| Frozen Caverns | Pale Burrower | Seeker | 0.85 | Native insect locomotion; burrowing deferred |
| Frozen Caverns | Rimewing | Hatchling | 1 | Native flight/projectile tells; no perch lifecycle |
| Frozen Caverns | Glacier Stalker | Wolf | 1.2 | Native run/bite; low ice-armored anatomy pending |
| Frozen Caverns | Cryolith Guardian | StoneGolem | 1 | Native heavy construct locomotion/attack |
| Fracture Zones | Fracture Wisp | FrostWisp | 0.8 | Native flying wisp; no orbiting-piece system |
| Fracture Zones | Rift Skitter | Seeker | 0.65 | Native insect locomotion; no wall adhesion |
| Fracture Zones | Shardwing | Hatchling | 0.9 | Native flight/projectile; mineral wing art pending |
| Fracture Zones | Gravity Leech | Leech | 1.1 | Aquatic motion proxy only; no land crawl or pull field |
| Fracture Zones | Chasm Stalker | Seeker | 1.15 | Native insect locomotion; long-limb art pending |
| Fracture Zones | Stonebound | StoneGolem | 0.7 | Native grounded construct; independent floating masses deferred |
| Fracture Zones | Rift Colossus | StoneGolem | 1.3 | Native heavy construct; no custom displacement system |
| Great Decay | Rotling | Tick | 0.8 | Native scuttle/latch; biomass anatomy pending |
| Great Decay | Carrion Bloom | Greydwarf_Shaman | 0.85 | Native poison-cast proxy; mobile donor retained, rooted art pending |
| Great Decay | Spore Husk | Draugr | 1 | Native humanoid equipment/attacks; grafted silhouette pending |
| Great Decay | Marrow Creeper | Seeker | 0.7 | Native insect locomotion; bone-supported anatomy pending |
| Great Decay | Decay Hound | Wolf | 1.1 | Native quadruped attacks; contamination art pending |
| Great Decay | Graft Warden | Troll | 1 | Native heavy biped attacks; asymmetric graft art pending |
| Great Decay | Corpse Orchard | Greydwarf_Shaman | 1.5 | Poison-cast motion proxy; mobile donor, no rooted colony or spawning |

## Scenery pass

36 additional named ground-cover variants feed the existing disposable ecology preview. They retain donor textures and use per-renderer tints, varied scales and mixed silhouettes. These are prototype compositions, not newly authored botanical meshes or harvestable resources. Existing large-tree/rock palettes remain intact. Dense Fungal Forest and Great Decay supports receive four nearby fill objects; the other biomes receive two. Every fill is sampled against the existing terrain and biome boundary.

- **FungalForest**: Jade fiddlefern, Violet sporebrush, Amber cap bed, Blue lantern caps, Moss pillow stone, Young worldroot.
- **BlackwaterDeep**: Brine fern, Drowned bank brush, Pearl bank caps, Wet bank stone, Fingerstone rubble, Drowned rootlet.
- **SulfurousWastes**: Copper ashbrush, Sulfur scrub, Vent fringe fern, Obsidian cobble, Sulfur stone, Charred sapling.
- **FrozenCaverns**: Silver shelter fern, Rime brush, Iceblue caps, Glacial pebble, Rime crystal sprig, Sheltered rootlet.
- **FractureZones**: Amethyst fault fern, Ridge brush, Crevice caps, Fault scree, Quartz cobble, Split ridge sapling.
- **GreatDecay**: Sourgreen fern, Wine rotbrush, Ochre decay caps, Marrow pale caps, Rotroot tangle, Mossgrave stone.

Donor-name reference: [Jotunn generated prefab catalog](https://valheim-modding.github.io/Jotunn/data/prefabs/prefab-list.html). The installed game remains the runtime source of truth; missing donor assets are logged/skipped.

## Infrastructure review pieces

18 console-spawnable pieces extend the same donor-first pass: one walkway, support and footing per biome. These are native `wood_floor`, `wood_pole2` and `stone_floor_2x2` clones at original dimensions, with biome tints. Snap points, colliders, support, wear and demolition stay with Valheim. No Hammer recipes or natural ruin placement are added yet.

| Biome | Kit prefix | Intended composition |
|---|---|---|
| Fungal Forest | Mycelial | Raised grove paths and root-supported rest platforms |
| Blackwater Deep | Blackwater | Bank boardwalks and landing footings above the water |
| Sulfurous Wastes | Cinder | Broken service walks around vent fields |
| Frozen Caverns | Rime | Sheltered paths and ice-edge observation platforms |
| Fracture Zones | Rift | Ridge paths and broken crossing approaches |
| Great Decay | Rotroot | Raised paths through tangled understory |

Spawn names use the same prototype prefix; for example `Magenheim_Underworld_Prototype_MycelialWalkway`, `Magenheim_Underworld_Prototype_RiftSupport`, `Magenheim_Underworld_Prototype_BlackwaterFooting`. These compositions are intended review uses, not claims of generated settlements.
