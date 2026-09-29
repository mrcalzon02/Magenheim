# Underworld donor prototype review — 2026-09-21

## 2026-09-28 creature visual status correction

The donor prototypes are still **runtime stand-ins**, not finished re-bodies, but the flat-tint
placeholder pass has been retired. All 42 non-boss prototype prefabs now receive Magenheim-owned
procedural albedo textures at registration. The texture seed includes biome, creature identity,
renderer and material slot, so creatures that share a Valheim chassis do not share the same skin.
Surface families distinguish hide, carapace, scale, stone, membrane and decayed flesh, while each
Underworld biome supplies its own material language (mycelial/spore growth, blackwater pressure
banding, sulfur/basalt fissures, rime striation, fracture seams, or rot lesions).

This is deliberately an intermediate art layer: donor skeletons and silhouettes remain visible
until their authored bodies are bound. Lantern Moth is still the first explicit donor-replacement
benchmark; Sporeling and Capcrawler also have committed source/texture work without a runtime model
consumer. The loading-screen inhabitant contract therefore still requires continued re-body work,
but the current in-game prototypes are no longer simple hue-shift duplicates.

See docs/validation/2026-09-28-underworld-creature-biome-retexture.md and
docs/validation/2026-09-28-lantern-moth-donor-replacement.md.


> 0.0.90 scope correction: infrastructure in this task means natural terrain/scenery features, not player construction. No Hammer entries or new player-buildable content are introduced. The 18 console review identities from 0.0.89 are retained only for compatibility; the infrastructure section below is historical. Current work focuses on vegetation, roots, rock formations, banks and lakebeds.

## 0.0.90 natural scenery refinement

The ground-cover palette now contains 48 variants (eight per biome). Added broad jade fern and fungal stepping slab; Blackwater root fan and lakebed stone shelf; cinder thorn fan and basalt stepping slab; low rime fern and ice scree shelf; fault fan scrub and fractured shale plate; rotroot fern fan and carrion root mat. These reuse donor geometry with distinct proportions and colors; they are not newly authored plant meshes.

Each variant declares signed height-above-water and slope limits. Blackwater bank plants stay within four meters above water; lakebed shelves occupy submerged ground down to 30 meters; roots and rubble have their own ranges. All four neighboring terrain samples must remain inside the biome, and the steepest neighbor differences determine slope. Missing or unsupported donors fall through to another habitat-compatible variant. If no habitat fits, the patch stays bare.

The preview now uses the existing six biome-specific placement compositions: fungal groves, sparse Blackwater banks, sulfur outcrop chains, frozen shard fans, fracture fault lines and decay root corridors. Ground-cover randomness is separate from canopy layout, so changing fill density no longer rearranges subsequent canopy positions. Existing disposable-preview and terrain lifecycles remain in charge.



Current direction: content expansion using Valheim-owned animation, AI, combat, networking and saving. The earlier bespoke HOST rig requirements are final-art aspirations, not prerequisites for these rough prototypes. Existing authored art is preserved.

42 non-boss creatures are registered as native-spawning Underworld creatures, seven per biome, and remain console-spawnable for direct review. They retain the original donor skeleton, controller, clips, events, attack items, colliders, AI, faction and drops, but now replace donor surface appearance with Magenheim-owned biome/creature textures at registration. The creatures are not finished unique meshes or fully balanced encounters, but they are no longer display-only prototypes: each owns a spawn row restricted to its canonical Underworld biome. Small fauna use higher local caps and shorter intervals; hero/heavy forms are deliberately sparse; aquatic donors are restricted to submerged/near-water altitude bands. Ambient roles are still constrained by donor behavior where no custom AI exists.

Natural spawning now uses the same registered identities. For direct review, spawn with `spawn Magenheim_Underworld_Prototype_<NameWithoutSpaces> 1` after enabling the normal Valheim developer console. Donor availability and animation controller are checked during registration; the log reports the actual registered count. Exact bone paths are deliberately not guessed: the whole native hierarchy remains intact.


## 2026-09-28 identity, temperament and physiology pass

The reused chassis are now deliberately separated in behavior as well as surface art. Registration runs
`UnderworldCreatureIdentityPass` after the owned skin material pass and before Jötunn creature
registration.

- Every creature receives a low-budget biome VFX layer: Fungal spore drift, Blackwater
  bioluminescent motes, Sulfurous smoke, Frozen rime mist, Fracture rift sparks, or Great Decay
  motes. Selected identity creatures also carry local point-light glow; large/spectral forms receive
  stronger but still capped effects.
- The original `MonsterAI` remains authoritative. Magenheim tunes its alert/view/hearing ranges,
  target-circling cadence and roaming pressure instead of replacing it. Predators and apex forms are
  substantially more assertive than ambient fauna, and eight apex identities opt into Jötunn's
  `HuntPlayer` spawn behavior.
- Home-biome physiology is applied through Valheim's native `HitData.DamageModifiers` plus
  `Character` environmental-tolerance flags. Sulfurous fauna resist fire and tolerate smoke/fire;
  Frozen fauna heavily resist frost but are vulnerable to fire; Blackwater fauna tolerate water but
  are vulnerable to lightning; Fungal and Great Decay fauna resist poison while retaining
  appropriate fire/spirit weaknesses; Fracture fauna resist lightning/spirit pressure.
- A small elemental damage rider is attached at `Character.Damage` after a real donor attack has
  selected and landed. This preserves donor attack prefabs, clips, sockets and AI while adding
  biome identity: poison for Fungal/Decay, frost for Blackwater/Frozen, fire for Sulfurous, and
  lightning for Fracture. Vanilla donor attack prefabs are never mutated.
- Physical donor resistances, factions, loot, animation controllers, attack definitions, hitboxes
  and networking remain inherited unless a later creature-specific design explicitly replaces them.

This is intentionally the middle ground between a palette swap and forty-two bespoke creatures:
reuse the expensive native machinery, then spend Magenheim-owned work on the parts the player
actually reads as species identity.

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


## 2026-09-29 combat balance ladder

The ordinary Underworld fauna and elemental Surtlings now share an explicit progression/role combat
policy instead of inheriting donor health and damage wholesale.

Biome progression supplies the baseline:

| Biome | Base HP | Damage progression | Base per-hit cap |
|---|---:|---:|---:|
| Fungal Forest | 180 | 0.90x | 36 |
| Blackwater Deep | 220 | 1.00x | 44 |
| Sulfurous Wastes | 270 | 1.10x | 54 |
| Frozen Caverns | 330 | 1.20x | 66 |
| Fracture Zones | 410 | 1.32x | 80 |
| Great Decay | 500 | 1.45x | 96 |

Combat role then deliberately trades mobility for punishment:

| Role | HP scale | Damage scale | Hit-cap scale | Movement | Minimum attack recovery |
|---|---:|---:|---:|---:|---:|
| Swarm | 0.55x | 0.45x | 0.55x | 1.15x | 0.75 s |
| Skirmisher | 0.80x | 0.65x | 0.80x | 1.08x | 1.00 s |
| Hunter | 1.10x | 0.85x | 1.00x | 1.00x | 1.40 s |
| Bruiser | 1.55x | 1.05x | 1.25x | 0.88x | 2.00 s |
| Heavy | 2.15x | 1.25x | 1.55x | 0.72x | 2.80 s |
| Apex | 3.00x | 1.45x | 1.90x | 0.60x | 3.60 s |

This is the governing fairness rule: fast enemies are allowed to be difficult to pin down but do
small per-hit damage; high-damage enemies become slower, tougher and more deliberate, with enough
attack recovery that a player can read the threat and own a failed dodge. Biome progression raises
the whole envelope without breaking that inverse relationship.

Valheim monsters do not use the player's ordinary stamina economy for attack pacing. The equivalent
balance lever is native `MonsterAI.m_minAttackInterval`, so the role policy uses attack recovery
rather than inventing a fake monster-stamina system.

Outgoing damage is applied per Magenheim creature instance at the `Character.Damage` boundary and
then capped by the role/biome envelope. The incoming `HitData` is cloned before modification so an
AoE or shared donor hit cannot accumulate the multiplier/rider from one victim to the next. Vanilla
donor attack prefabs remain untouched.
