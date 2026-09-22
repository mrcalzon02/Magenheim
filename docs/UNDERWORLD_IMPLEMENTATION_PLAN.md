# The Underworld — Detailed Implementation Program

> Authoritative correction (2026-09-17): the Underworld uses altered surface-world
> terrain beneath one shared custom fantastical cavern-roof skybox for all biomes.
> The roof cannot vary by biome or terrain area and is not physical geometry.
> Ignore conflicting roof-shelf, ceiling-generation, roof-attachment and biome-specific
> ceiling requirements below. Use ground-supported terrain, flora and local props.
> See `UNDERWORLD_FLORA_TERRAIN_PLAN.md` for the active implementation sequence.

**Project:** Magenheim for Valheim  
**Authority relationship:** execution plan subordinate to `docs/UNDERWORLD_DESIGN.md` and project `INSTRUCTIONS.md`  
**Development location:** `mrcalzon02/Magenheim`, authoritative branch `main`  
**Status:** durable implementation authority; no implementation phase is admitted merely because it appears here  
**Primary objective:** turn the Underworld design into a staged, testable framework and then into six production biomes with ecology, structures, bosses, progression, building, traversal, multiplayer, persistence and end-to-end world validation

---

# 1. Program objective

The Underworld must be implemented as a major Magenheim expansion subsystem rather than as a disconnected collection of prefabs. Every major feature discussed in the durable design is therefore assigned to an authority, data contract, implementation phase and acceptance gate.

The program must move in this order:

1. prove world-layer feasibility;
2. establish deterministic definitions and multiplayer fingerprints;
3. establish terrain, biome and environment frameworks;
4. establish shared progression, boss, trophy and Deepstone systems;
5. complete one biome vertically before multiplying content;
6. add subsequent biomes through the same framework;
7. add civilization, traversal, building and cross-layer portal systems;
8. validate persistence, multiplayer, world generation, performance and long-session behavior before claiming production readiness.

Content production must never outrun the framework required to make that content durable.

---

# 2. Authority hierarchy for Underworld work

Underworld implementation uses the normal Magenheim authority hierarchy.

1. Current user instruction.
2. `INSTRUCTIONS.md`.
3. Verified committed source on `main` and current project state.
4. `docs/UNDERWORLD_DESIGN.md` for durable Underworld design invariants.
5. This document for implementation sequence, file boundaries and acceptance gates.
6. `BACKLOG.md`, validation records and active execution state.
7. Historical conversation and exploratory drafts.

Where implementation evidence disproves an assumed technical path, the design must be revised explicitly rather than silently working around it.

---

# 3. Program-wide invariants

The following rules govern every phase.

- The Underworld remains inside Magenheim for the foreseeable development horizon.
- `main` remains the only active branch.
- No second repository, replacement mod, duplicate world generator or parallel persistence engine is created.
- `Magenheim.Core` owns deterministic rules and definition validation where practical.
- Runtime code consumes validated Core authority rather than recreating equivalent logic independently.
- World generation remains additive and non-destructive at the surface boundary.
- The Underworld heightfield cavern illusion remains authoritative: giant basins, wall masses, inaccessible Roof Shelf, darkness, fog and local hanging geometry instead of a universal procedural ceiling.
- Normal gameplay must not reach a vantage point that exposes the high-plateau illusion.
- Every boss must use durable encounter state, server authority, bounded arena logic, defeat/reward guards and reconstructable persistence.
- Every biome must have a distinct mechanical identity, not merely different materials and colors.
- Every creature family must have an ecological or combat role; do not fill spawn tables with reskinned health bars.
- Every persistent mutation must be multiplayer-safe before it is relied upon for progression.
- Static source presence is not runtime acceptance.

---

# 4. Definition-first framework

Before full biome production, the Underworld needs one validated definition set that can describe the world without hard-coding every biome, creature and boss into registration code.

## 4.1 Proposed authoritative data root

```text
default-data/
  underworld/
    underworld.schema.json
    world.json
    biomes.json
    environments.json
    events.json
    creatures.json
    bosses.json
    deepstones.json
    structures.json
    resources.json
    progression.json
    traversal.json
```

These names are planning targets. Existing Magenheim definition loaders and schema conventions must be reused or extended before a second loader is created.

## 4.2 Core definition authorities

Proposed Core types:

```text
Magenheim.Core/Underworld/
  UnderworldDefinitionSet.cs
  UnderworldDefinitionValidator.cs
  UnderworldDefinitionFingerprint.cs
  UnderworldWorldDefinition.cs
  UnderworldBiomeDefinition.cs
  UnderworldEnvironmentDefinition.cs
  UnderworldEventDefinition.cs
  UnderworldCreatureDefinition.cs
  UnderworldSpawnDefinition.cs
  UnderworldBossDefinition.cs
  UnderworldBossRewardDefinition.cs
  UnderworldDeepstoneDefinition.cs
  UnderworldStructureDefinition.cs
  UnderworldResourceDefinition.cs
  UnderworldProgressionGraph.cs
  UnderworldTraversalDefinition.cs
```

## 4.3 Biome definition fields

Each biome definition must provide enough information for generation, environment, ecology and progression without relying on hidden values in a runtime class.

Required conceptual fields:

| Group | Required content |
|---|---|
| Identity | stable ID, display name, localization keys, map presentation |
| Generation | macro field weights/thresholds, basin preference, wall relationship, altitude/depth range, water/coast relationship |
| Terrain | ground material family, cliff material family, erosion/roughness profile, feature density |
| Environment | ambient light, fog, distance occlusion, emissive bias, particle profile, audio profile |
| Events | allowed subterranean event IDs and weights |
| Hazards | poison, heat, cold, decay, fracture, visibility or other biome-specific systems |
| Ecology | ambient/common/uncommon/elite creature spawn groups |
| Resources | harvestables, ores, biological materials, liquids and special nodes |
| Structures | minor, major and unique structure pools |
| Boss | boss definition ID and unique location definition ID |
| Transitions | compatible neighboring biomes, hybrid tags and blend rules |
| Progression | recommended entry state, required tools/protection if any, rewards enabled by biome |

## 4.4 Creature definition fields

Each creature definition should contain stable identity, prefab/factory authority, biome tags, spawn tier, weight, group size, altitude/water constraints, environmental/event conditions, hostility behavior, AI profile, combat role, damage profile, resistances, stagger profile, loot table, trophy rule if any, network ownership policy and persistence/despawn policy.

Creature definitions do not replace AI implementation. They provide validated content authority consumed by shared creature registration and spawn systems.

## 4.5 Boss definition fields

Each boss definition must include:

- stable boss ID;
- display/localization keys;
- owning biome;
- unique location ID;
- summon altar ID;
- summon offering identity and quantity;
- encounter-state schema version;
- boss prefab/factory authority;
- arena profile;
- attack-director profile;
- phase thresholds;
- leash/recovery policy;
- persistence policy;
- trophy ID;
- unique reward IDs;
- Deepstone slot ID;
- Deep Boon ID;
- progression unlock IDs;
- rematch behavior;
- map-location discovery rule.

## 4.6 Fingerprint and multiplayer authority

The effective Underworld definition set must contribute to the same gameplay-authority synchronization philosophy already used by Magenheim. A server and client must not silently disagree about biome hazards, boss progression, spawn eligibility or Deepstone effects while persistent gameplay continues.

At minimum the Underworld fingerprint must include generation-significant values, biome identity, progression graph, persistent boss identities, Deepstone/boon mapping and gameplay-significant creature/boss rules. Purely cosmetic local presentation may remain outside gameplay fingerprint authority when safe.

---

# 5. World-layer framework

The first executable Underworld milestone is not a finished biome. It is a safe world transition.

## 5.1 Parent and derived identity

Create one deterministic derived-world identity from the parent world identity and seed. The exact implementation must be proven against current Valheim runtime behavior before promotion.

Required logical facts:

```text
ParentWorldId
ParentSeed
UnderworldWorldId
UnderworldSeed
UnderworldSchemaVersion
GenerationFingerprint
Created/Initialized state
```

The same parent world must always resolve to the same Underworld identity unless an explicit migration changes schema behavior.

## 5.2 Proposed authorities

```text
Magenheim.Core/Underworld/
  UnderworldSeed.cs
  UnderworldWorldIdentity.cs
  UnderworldTransitionRules.cs

Magenheim.Runtime/Underworld/
  UnderworldManager.cs
  UnderworldWorldContext.cs
  UnderworldWorldTransitionManager.cs
  UnderworldUnlockRuntime.cs
  UnderworldPersistenceRuntime.cs
  UnderworldMapTabRuntime.cs  # thin layer/data switch around Valheim Minimap; never a second map engine
```

## 5.3 Transition contract

Entering the Underworld must:

1. verify the Nowhere King unlock state;
2. verify server authority and transition eligibility;
3. persist the player's source-layer return anchor;
4. verify the target Underworld identity exists or can be initialized;
5. finish critical target initialization before player placement;
6. transfer the player without duplicating or losing character state;
7. select the Underworld environment/worldgen context and bind the Underworld payload to the existing Valheim Minimap;
8. reconstruct persistent target-zone state;
9. acknowledge successful transition before clearing recovery information.

A failure must preserve enough state to return or recover the player safely.

## 5.4 Multiplayer transition rule

The server owns layer identity and target state. Clients must not independently construct a second version of the world or decide that a transition succeeded. Remote-client transition requests must validate boss unlock state, target identity, gate eligibility and player participation.

## 5.5 First transition acceptance gate

Do not proceed into production worldgen until repeated tests prove:

- enter Underworld;
- return to surface;
- save in Underworld and reload;
- disconnect/reconnect while below;
- host and remote client transition independently;
- death below resolves to an intentional valid rule;
- failure during transition cannot strand or duplicate the character;
- surface world state remains unchanged except for intended gate/progression state.

---

# 6. Heightfield cavern-illusion framework

## 6.1 Generator layers

Create the terrain framework as deterministic fields rather than one giant generator method.

Proposed authorities:

```text
Underworld/Generation/
  UnderworldGenerator.cs
  UnderworldGenerationContext.cs
  MacroBasinField.cs
  WallMassField.cs
  RoofShelfField.cs
  FractureField.cs
  ThermalField.cs
  MoistureField.cs
  CryogenicField.cs
  DecayField.cs
  OceanBasinField.cs
  LuminosityField.cs
  LandmarkField.cs
  UnderworldBiomeClassifier.cs
```

## 6.2 Generation passes

The generator should separate these passes conceptually:

1. world-scale basin layout;
2. wall-mass barriers;
3. inaccessible Roof Shelf formation;
4. traversable floor elevation;
5. ocean basin cut;
6. fracture/ravine pass;
7. biome classification;
8. terrain material/profile application;
9. major landmark placement;
10. rivers/waterfall/lavafall anchors;
11. civilization anchor placement;
12. minor structures/resources/ecology.

## 6.3 Roof Shelf enforcement

The Roof Shelf is technical scenery. The implementation plan must therefore include explicit tests for maximum ordinary player reach using:

- normal jumping;
- terrain climbing;
- ladders/stairs/building pieces;
- Underworld traversal tools;
- knockback;
- boss launch mechanics;
- gliding;
- multiplayer assistance.

If progression later introduces stronger climbing, the Roof Shelf constraint must be revalidated before shipping that traversal feature.

## 6.4 Cavern presentation framework

Proposed runtime systems:

```text
Underworld/Environment/
  UnderworldSkyRuntime.cs
  UnderworldLightingRuntime.cs
  UnderworldFogRuntime.cs
  UnderworldDistanceOcclusionRuntime.cs
  UnderworldAmbientAudioRuntime.cs
  UnderworldParticleRuntime.cs
```

The first visual gate is not beauty. It is whether a normal player believes the space is subterranean and cannot see Yggdrasil, conventional sky, sun, moon or the hidden plateau edge.

---

# 7. Custom biome runtime framework

The biome framework must be complete before six biome-specific runtime classes proliferate.

## 7.1 Runtime authorities

```text
Underworld/Biomes/
  UnderworldBiomeManager.cs
  UnderworldBiomeRuntime.cs
  UnderworldBiomeEnvironmentRuntime.cs
  UnderworldBiomeHazardRuntime.cs
  UnderworldBiomeDecorationRuntime.cs
  UnderworldBiomeSpawnRuntime.cs
  UnderworldBiomeTransitionRuntime.cs
```

Biome-specific code should ideally provide behavior only where generic data cannot express it safely.

## 7.2 Biome lifecycle

Each loaded Underworld zone resolves:

1. generation fields;
2. primary biome;
3. secondary/hybrid tags;
4. terrain profile;
5. environment profile;
6. hazard profile;
7. decoration/resource pools;
8. structure eligibility;
9. creature spawn table;
10. event eligibility;
11. map presentation.

## 7.3 Hybrid biome rule

Hybrids such as Frozen Fracture or Decayed Forest are modifier combinations, not full duplicate biomes unless testing proves a distinct authority is necessary. A Fracture modifier can affect geometry and spawns while preserving the primary biome's resources/environment.

---

# 8. Ecology and creature framework

## 8.1 Shared creature registration

Create one Underworld creature registration pipeline that resolves validated definitions, builds or locates Magenheim-owned prefabs, registers localization, assigns network behavior and connects spawn profiles.

Do not create one separate spawn registrar per creature.

## 8.2 Spawn tiers

Every biome uses the same semantic tiers:

| Tier | Purpose |
|---|---|
| Ambient | non-hostile or low-threat life that makes the biome feel inhabited |
| Common | normal biome pressure and resource loop |
| Uncommon | tactical variation and stronger rewards |
| Elite | rare high-pressure encounter with valuable drops |
| Apex | rare non-boss large creature where appropriate |
| Boss | unique or summonable persistent encounter owned by boss framework |

## 8.3 Spawn planner requirements

The planner should understand land/water, water depth, slope, altitude, distance from structures, group limits, event modifiers, day-equivalent environmental state if used, elite caps and local population budget.

Creature density must be globally budgeted enough that biome decoration and networking remain performant.

---

# 9. Shared boss encounter framework

The Nowhere King plan already establishes useful doctrine: durable encounter state, hard arena authority, server-owned phases, separate attack director, persistent defeat/reward guards and reconstruction after unloading. Underworld bosses should reuse that architecture rather than reinventing encounter policy six times.

## 9.1 Proposed shared authorities

```text
Underworld/Bosses/
  UnderworldBossRegistry.cs
  UnderworldBossDefinitionRuntime.cs
  UnderworldBossEncounterState.cs
  UnderworldBossEncounterRuntime.cs
  UnderworldBossArenaRuntime.cs
  UnderworldBossAttackDirector.cs
  UnderworldBossPersistence.cs
  UnderworldBossRewardRuntime.cs
  UnderworldBossSummonRuntime.cs
  UnderworldBossTrophyRuntime.cs
```

Individual bosses then own bounded model/AI/attack implementations while consuming shared lifecycle rules.

## 9.2 Encounter state

Base lifecycle:

`Dormant -> Summoned -> Engaged -> PhaseState(s) -> Disengaged -> Defeated`

Unique location state and boss entity state are related but not identical. Unloading a zone does not count as defeating a boss.

## 9.3 Rematch rule

Boss locations remain summonable after first defeat using the biome's offering item. Unique progression rewards must be guarded so rematches cannot duplicate one-time unlock transitions, while ordinary repeat loot/trophies may remain available according to design.

---

# 10. The Deepstone Conclave — Underworld standing stones

The Underworld requires its own equivalent of Valheim's central boss standing stones, but it should have its own identity and avoid patching vanilla Forsaken infrastructure.

## 10.1 Structure identity

Working name: **The Deepstone Conclave**.

The Conclave is a unique Underworld structure generated at or immediately adjacent to the player's first stable arrival region. It consists of six monumental dark-stone monoliths arranged around a central **Descent Monolith**.

The six boss stones are:

| Slot | Stone | Boss |
|---|---|---|
| Bloom | Stone of Bloom | The First Bloom |
| Tide | Stone of the Deep Tide | The Blackwater Maw |
| Cinder | Stone of Cinder | The Furnace Heart |
| Rime | Stone of Rime | The White Silence |
| Fracture | Stone of Fracture | The Rift Titan |
| Decay | Stone of Decay | The Carrion Crown |

## 10.2 Structure role

The Conclave performs four jobs:

1. visibly communicates Underworld boss progression;
2. accepts and displays boss trophies;
3. grants/selects an Underworld-specific **Deep Boon** from mounted trophies;
4. tracks completion of the six major world-boss lines and drives the final progression unlock.

It does not reveal boss locations automatically.

## 10.3 Trophy mounting

Boss trophies are real inventory items. Mounting a trophy consumes/attaches that physical trophy to the corresponding Deepstone, changes the stone's material/emission state and activates its boon.

Mounted state is world-persistent and server-authoritative. One player cannot locally activate a stone that the server has not admitted.

## 10.4 Deep Boons

Deep Boons are separate from vanilla Forsaken powers. Do not overwrite the player's vanilla selected power or standing-stone state.

Initial design uses **one selected Deep Boon at a time**. Boons are primarily Underworld adaptations rather than globally superior replacements for vanilla powers.

Provisional boon identities:

| Boss | Deep Boon | Mechanical direction |
|---|---|---|
| The First Bloom | Spore Communion | resistance to fungal toxins/poison pressure and improved efficiency from Underworld fungal provisions |
| The Blackwater Maw | Deep Current | reduced Wet burden, improved swimming stamina and improved Underworld vessel handling |
| The Furnace Heart | Furnace Blood | reduced thermal/heat buildup and improved resistance to fire/geothermal hazards without immunity |
| The White Silence | Rimebound | reduced deep-cold burden, improved stamina efficiency in frozen terrain and frost resistance |
| The Rift Titan | Stone Anchor | reduced fall/shockwave stagger and improved stamina efficiency for approved Underworld traversal systems |
| The Carrion Crown | Defiant Flesh | reduced Great Decay contamination pressure and improved recovery from decay-specific afflictions |

Exact values are balance work and must remain data-driven.

## 10.5 Proposed authorities

```text
Magenheim.Core/Underworld/Deepstones/
  DeepstoneDefinition.cs
  DeepstoneProgressionState.cs
  DeepBoonDefinition.cs
  DeepBoonSelectionRules.cs

Magenheim.Runtime/Underworld/Deepstones/
  DeepstoneConclaveRegistrar.cs
  DeepstoneConclavePrefabFactory.cs
  DeepstoneConclaveRuntime.cs
  DeepstonePedestalRuntime.cs
  DeepstoneTrophyRuntime.cs
  DeepBoonRuntime.cs
  DeepstonePersistence.cs
```

## 10.6 Multiplayer state split

World state:

- Conclave generated identity/anchor;
- mounted trophy slots;
- boss first-defeat flags;
- final completion state.

Player state:

- selected Deep Boon;
- any per-player discovery/UI acknowledgement required.

A shared trophy unlocks the boon for all players on that world, but each player selects their own active boon.

## 10.7 Final completion event

When all six trophies are mounted, the central Descent Monolith activates. Initial reward direction is to unlock the final Deepbound Portal technology and preserve a future hook for content below the Great Decay. It must not silently create another world layer before such a layer has its own design authority.

---

# 11. Boss-location discovery system

The Deepstone Conclave shows progression but does not hand out boss coordinates.

Create an Underworld equivalent to biome boss-location clues using **Deep Sigils**: carved monolith fragments, tablets, fossils, ruin maps or ritual markers generated in biome-appropriate structures.

A Deep Sigil interaction can reveal the current world's unique boss location for that biome, using a stable boss-location identity rather than a hard-coded coordinate.

Deep Sigils should be placed through the normal structure framework and remain additive to the exploration loop.

---

# 12. Progression graph

The intended primary progression is:

```text
Nowhere King defeated
        |
        v
Underworld + Deepstone Conclave
        |
        v
The First Bloom
        |
        v
The Blackwater Maw
        |
        +------------------+
        |                  |
        v                  v
The Furnace Heart     The White Silence
        |                  |
        +--------+---------+
                 |
                 v
            The Rift Titan
                 |
                 v
          The Carrion Crown
                 |
                 v
All Deepstones complete / final Underworld infrastructure unlock
```

Sulfurous Wastes and Frozen Caverns are deliberately parallel after Blackwater. Their major progression rewards are both needed to fully prepare for Fracture Zone traversal, but players may choose which environment to master first.

---

# 13. Biome package template

No biome is considered production-complete until its package contains all of the following:

| Package area | Required artifact |
|---|---|
| Design | completed biome identity and gameplay loop |
| Generation | field eligibility, terrain profile, coast/fracture relationship |
| Environment | lighting, fog, particles, ambient audio, events |
| Hazards | implemented and configurable biome-specific pressures |
| Decoration | vegetation/geology/ecology dressing with performance budget |
| Resources | harvestables and progression materials |
| Structures | minor structures, one or more major families, Deep Sigil source |
| Ecology | ambient/common/uncommon/elite creature roster |
| Boss | unique location, summon loop, full encounter, trophy, rewards |
| Deepstone | trophy mount, visual state and boon |
| Progression | recipes/traversal/unlocks fed by biome materials |
| Persistence | relevant structures/boss/resource state |
| Multiplayer | spawn, boss, hazards and rewards authoritative |
| Validation | deterministic + runtime + save/load + multiplayer evidence |

---

# 14. Fungal Forest production package

The Fungal Forest is the first complete biome and the vertical slice for the entire Underworld content pipeline.

## 14.1 Generation and environment

Primary fields: high Moisture, moderate Luminosity, low Thermal, low Cryogenic, low Decay, broad Macro Basin eligibility.

Terrain language: rolling dark-stone floors, shallow water, broad shelves, isolated wall masses, mossed boulders, fungal soil mats and occasional giant pillars.

Lighting: cyan/blue/violet bioluminescence with restrained ambient fill.

Environmental events: Sporefall primary; Crystal Resonance occasional; Deep Fog near water.

Primary hazard: fungal toxin/spore exposure in localized pockets, not permanent full-biome poison.

## 14.2 Resource families

Initial resources:

- Glowcap tissue;
- Mycelial Fiber;
- Lantern Spores;
- Deep Moss;
- Resinous Fungal Wood equivalent;
- Bioluminescent Gland material;
- Heartcap fragments from elite/boss ecology.

These should feed food, lighting, building, alchemy and early Underworld survival instead of existing solely as boss-key materials.

## 14.3 Creature roster

| Creature | Tier | Role |
|---|---|---|
| Lantern Moth | Ambient | harmless flying bioluminescent life; local visual landmark |
| Sporeling | Common | small swarm creature; weak individually; releases a brief spore puff on death |
| Capcrawler | Common | low-profile melee skirmisher moving through fungal cover |
| Mycelial Stalker | Uncommon | concealment predator; ambushes from dark fungus and retreats after failed pounce |
| Puffback | Uncommon/neutral | bulky territorial herbivore-like organism; charge plus defensive spore burst |
| Shelf Lurker | Uncommon | wall/shelf ambusher emphasizing vertical awareness |
| Crowncap Brute | Elite | slow armored fungal mass with frontal protection and large ground-control attacks |

## 14.4 Major structures

- Fungal Hamlet ruins;
- Mycelial Shrine;
- abandoned Deep Gate waystation;
- Lantern Grove;
- First Bloom Deep Sigil site;
- **The Motherbed**, unique boss location.

## 14.5 Boss — The First Bloom

**Identity:** ancient mobile mycelial sovereign representing the healthy but overwhelming life of the Underworld before corruption.

**Location:** The Motherbed, a giant fungal hollow with broad sightlines, rooted columns and luminous safe navigation markers.

**Summon offering:** provisional `Magenheim_Underworld_BloomOffering`, crafted from rare Fungal Forest ecology materials rather than a single random drop.

**Combat language:** rooting, growth, spores, area occupation and sudden mobile uprooting.

Phase One focuses on sweeping root attacks, targeted mycelial eruptions and readable spore zones. Phase Two opens large cap structures that seed temporary fungal hazards while exposing vulnerable luminous tissue. Phase Three partially uproots the boss, increasing movement and direct pressure while reducing the amount of safe arena territory.

The fight should not become a permanent add swarm. Temporary growth nodes may appear, but the boss remains the central threat.

**Unique progression reward:** `Heartcap Core` working identity. Unlock direction: advanced fungal survival, Deep Lantern technology and first meaningful Underworld food/alchemy expansion.

**Trophy:** `Magenheim_Underworld_Trophy_FirstBloom`.

**Deep Boon:** Spore Communion.

## 14.6 Fungal Forest acceptance gate

The Fungal Forest vertical slice must prove the complete pipeline: generation, environment, resources, seven-creature ecology, structures, Deep Sigil discovery, boss summoning, first defeat, trophy mount, Deep Boon selection, save/load and multiplayer behavior.

Only after this gate passes should content production accelerate across the remaining biomes.

---

# 15. Blackwater Deep production package

## 15.1 Generation and environment

Blackwater is primarily an Ocean Basin result rather than ordinary painted land biome. Shores inherit adjacent primary biome modifiers.

Visual identity: near-black water reflecting distant blue/red light, heavy depth fog, silhouettes of pillars and shoreline fungus.

Events: Deep Fog primary; Crystal Resonance rare; Black Bloom possible near Decay coast.

## 15.2 Resource families

- Abyssal membrane;
- blackwater shell;
- lantern oil/gland;
- deep salt/mineral deposits;
- shell plates;
- abyssal tendon;
- rare Keelstone fragments.

## 15.3 Creature roster

| Creature | Tier | Role |
|---|---|---|
| Cave Ray | Ambient | large harmless or skittish surface-gliding aquatic life |
| Gloomfin | Common | fast pack fish/predator around boats and swimmers |
| Blackwater Lamprey | Common | small clinging/swarming aquatic threat |
| Shoreclaw | Common/Uncommon | amphibious armored crustacean controlling coastlines |
| Lantern Angler | Uncommon | lure-based predator with ranged light/pressure attack |
| Abyss Shellback | Elite | slow heavily armored coastal/aquatic creature with valuable shell resources |
| Deep Hunter | Apex | rare large aquatic predator, below boss scale, used sparingly |

## 15.4 Structures

- Blackwater dock ruins;
- flooded shrines;
- collapsed causeways;
- drowned watchtowers;
- Blackwater Deep Sigil;
- **The Drowned Ring**, unique boss location.

## 15.5 Boss — The Blackwater Maw

**Identity:** immense blind subterranean leviathan that owns a flooded caldera rather than roaming the entire ocean.

**Arena:** The Drowned Ring uses a central deep pool surrounded by rock shelves/islets so the fight remains readable and does not require perfect ship combat.

**Combat language:** breach attacks, tail/wave displacement, suction currents, blackwater jets, shoreline lunges and temporary denial of specific islets.

The boss remains anchored to a bounded aquatic arena. It must not chase ordinary boats across the world.

**Unique progression reward:** `Abyssal Keelstone`. Unlock direction: improved Underworld vessel construction/handling, Blackwater navigation and the next deep-route infrastructure.

**Trophy:** `Magenheim_Underworld_Trophy_BlackwaterMaw`.

**Deep Boon:** Deep Current.

---

# 16. Sulfurous Wastes production package

## 16.1 Generation and environment

Primary fields: high Thermal, low Moisture, moderate Fracture, low Cryogenic.

Terrain: basalt, sulfur crust, obsidian shelves, caldera bowls, vent fields and lava channels.

Events: Ashfall, Thermal Surge and Stone Rain near fracture hybrids.

Hazards: heat buildup and toxic gas pockets. These must be separately readable and separately mitigated.

## 16.2 Resource families

- Deep Sulfur;
- Obsidian Glass;
- Cinderstone;
- Geothermal Salts;
- Furnace Slag;
- Vent Crystal;
- heat-resistant hide/plates from fauna.

## 16.3 Creature roster

| Creature | Tier | Role |
|---|---|---|
| Ashmite | Ambient/Common | small scavenger swarm around vents and corpses |
| Cinder Hound | Common | fast pack predator encouraging movement |
| Basalt Crawler | Common | armored low creature with strong frontal defense |
| Vent Spitter | Uncommon | ranged geothermal/sulfur attack; weak when approached |
| Fume Wraith | Uncommon | gas-associated drifting threat that uses visibility pressure |
| Magma Leaper | Uncommon | jumps across lava/terrain barriers and punishes static ranged play |
| Furnace Golem | Elite | slow mineral construct-like creature built around heat and armor breaking |

## 16.4 Structures

- Geothermal Foundry;
- sulfur mine complex;
- vent shrine;
- ruined thermal pumps/aqueducts;
- Sulfur Deep Sigil;
- **The Crucible Hollow**, unique boss location.

## 16.5 Boss — The Furnace Heart

**Identity:** a geothermal titan whose basalt armor contains a visible molten core.

**Combat language:** vent telegraphs, heavy physical impact, heat-zone control and progressive exposure of the molten core.

Phase One uses armored strikes and directional vents. Phase Two cracks armor and activates arena lavafall/vent anchors. Phase Three becomes a controlled overheat state with shorter recovery but larger vulnerability after major attacks.

Do not use unavoidable ambient damage as the boss's main difficulty. Hazards must remain readable and survivable with proper preparation.

**Unique progression reward:** `Thermal Crucible`. Unlock direction: geothermal crafting, high-temperature refining and advanced heat protection.

**Trophy:** `Magenheim_Underworld_Trophy_FurnaceHeart`.

**Deep Boon:** Furnace Blood.

---

# 17. Frozen Caverns production package

## 17.1 Generation and environment

Primary fields: high Cryogenic, low Thermal, variable Moisture, broad basins with sharper compressed passes.

Terrain: black stone, glacial shelves, ice fields, frozen rivers and preserved ruins.

Events: deep cold fronts, Crystal Resonance, ice particulate drift and rare Stone Rain at fracture transitions.

Hazard: deep-cold exposure should be distinct from ordinary surface Mountain cold and interact with preparation rather than simply copy the vanilla freezing status.

## 17.2 Resource families

- Rime Crystal;
- Pale Ice Resin;
- Frozen Marrow;
- Glacial Glass;
- Ancient Preservative Salts;
- Cryolith fragments.

## 17.3 Creature roster

| Creature | Tier | Role |
|---|---|---|
| Rime Moth | Ambient | pale low-light flying life |
| Frost Tick | Common | small parasitic pack threat |
| Iceblind | Common | blind quadruped predator tracking movement/noise |
| Pale Burrower | Common/Uncommon | emerges from snow/ice and relocates beneath terrain-compatible surfaces |
| Rimewing | Uncommon | gliding wall-to-wall aerial predator |
| Glacier Stalker | Uncommon | patient ambusher with strong opening attack and weak sustained defense |
| Cryolith Guardian | Elite | ancient ice/mineral construct protecting preserved sites |

## 17.4 Structures

- Frozen Vault;
- preserved caravan/road station;
- ice shrine;
- sealed archive;
- Frozen Deep Sigil;
- **The Stillvault**, unique boss location.

## 17.5 Boss — The White Silence

**Identity:** enormous pale blind cavern predator preserved and empowered by deep cold.

**Combat language:** ice fracture, sudden burrow/reposition, freezing breath, sound suppression as presentation, and attacks that leave readable brittle zones in the arena.

The boss should never rely on invisible attacks simply because the theme is silence. Telegraphs can use ground frost, visible breath, lifted ice dust and directional cracking even when audio is deliberately suppressed.

**Unique progression reward:** `Rimeheart`. Unlock direction: deep-cold protection, preservation crafting and materials needed alongside the Thermal Crucible path for Fracture traversal technology.

**Trophy:** `Magenheim_Underworld_Trophy_WhiteSilence`.

**Deep Boon:** Rimebound.

---

# 18. Fracture Zones production package

## 18.1 Generation and environment

Primary field: high Fracture, with the zone able to cut across other biome geography.

Terrain: narrow ridges, chasms, split wall masses, natural bridges, suspended-looking rock prefabs and extreme vertical silhouettes.

Events: Stone Rain, Crystal Resonance and localized gravity anomalies.

Hazard identity: displacement, falling and route instability rather than constant damage-over-time.

## 18.2 Traversal dependency

The Fracture Zone is where the Underworld traversal system becomes mandatory. The expected progression package uses materials unlocked through both Sulfurous and Frozen boss lines to build safe anchors, ropes, winches, lifts or equivalent tools.

Traversal tools must use explicit legal anchors/geometry rules where necessary so they do not permit ordinary access to the Roof Shelf.

## 18.3 Resource families

- Fracture Crystal;
- Gravity Stone;
- Rift Fiber;
- Suspended Ore;
- Resonant Slate;
- ancient bridge metal/alloy.

## 18.4 Creature roster

| Creature | Tier | Role |
|---|---|---|
| Fracture Wisp | Ambient/Common | mobile luminous anomaly that may become hostile in groups/events |
| Rift Skitter | Common | sure-footed cliff creature using steep terrain |
| Shardwing | Common/Uncommon | gliding crystalline predator |
| Gravity Leech | Uncommon | creates local pull fields or weighted movement pressure |
| Chasm Stalker | Uncommon | long-limbed ridge predator that threatens narrow routes |
| Stonebound | Uncommon | floating/segmented mineral construct using push attacks |
| Rift Colossus | Elite | large segmented guardian designed around stagger and terrain displacement |

## 18.5 Structures

- broken great bridges;
- cliff monasteries/temples;
- suspended road stations;
- anchor towers;
- Fracture Deep Sigil;
- **The Suspended Court**, unique boss location.

## 18.6 Boss — The Rift Titan

**Identity:** ancient stone guardian broken into multiple floating or partially separated masses held together by fracture forces.

**Combat language:** directed pull/push fields, falling stone, segmented slams, local gravity wells and movement across separated arena platforms.

This boss must remain distinct from the Nowhere King's Gravity Inversion. It does not globally invert gravity or launch the entire formation upward. Its power is localized directional force and unstable mass.

As health falls, body segments are destroyed or detached, changing attack reach and exposing the central binding core.

**Unique progression reward:** `Fracture Anchor`. Unlock direction: full advanced traversal, heavy bridge/elevator infrastructure and access routes into the Great Decay interior.

**Trophy:** `Magenheim_Underworld_Trophy_RiftTitan`.

**Deep Boon:** Stone Anchor.

---

# 19. Great Decay production package

## 19.1 Generation and environment

Primary field: high Decay, usually invading or modifying pre-existing geological forms.

Terrain dressing: black/red/brown biomass, sickly luminescent growth, consumed ruins, organic mats and parasitic towers.

Event: Black Bloom is the signature event.

Hazard: **Decay contamination**, a dedicated pressure system separate from ordinary poison. It should build through exposure to specific growths, attacks and environmental zones rather than being a simple permanent biome debuff.

## 19.2 Resource families

- Decay Biomass;
- Marrow Fiber;
- Canker Resin;
- Black Spore Mass;
- Graft Bone;
- purified anti-decay reagent;
- rare Crown tissue.

## 19.3 Creature roster

| Creature | Tier | Role |
|---|---|---|
| Rotling | Common | small aggressive biomass scavenger |
| Carrion Bloom | Common | rooted organism using ranged spores/tendrils |
| Spore Husk | Common/Uncommon | animated consumed body/structure shell |
| Marrow Creeper | Uncommon | fast low predator using organic cover |
| Decay Hound | Uncommon | pack hunter spreading contamination on successful attacks |
| Graft Warden | Elite | large fused guardian protecting mature colonies |
| Corpse Orchard | Elite stationary | living spawn/area-control organism that must be destroyed deliberately |

## 19.4 Structures

- consumed settlements;
- overtaken roads;
- quarantine/containment ruins;
- deep temples swallowed by biomass;
- Decay Deep Sigil;
- **The Consumed Basilica**, unique boss location.

## 19.5 Boss — The Carrion Crown

**Identity:** apex Decay colony formed from accumulated organisms, bones and swallowed architecture, wearing a crown-like mass of ruin and calcified growth.

**Combat language:** contamination zones, tendril sweeps, seed projectiles, controlled reclamation of fixed biomass nodes and progressive shedding of external mass.

The fight should not scan arbitrary corpses in the world and create unpredictable healing. Arena-owned biomass nodes provide deterministic reclamation targets. Players can destroy them to deny healing/empowerment.

Final phase strips much of the external colony away and exposes the central living core, making the boss faster and more direct rather than simply adding more summons.

**Unique progression reward:** `Decay Heart` working identity. Unlock direction: strongest anti-decay technology, final Underworld crafting tier and completion of the six-boss progression.

**Trophy:** `Magenheim_Underworld_Trophy_CarrionCrown`.

**Deep Boon:** Defiant Flesh.

---

# 20. Structure and civilization framework

After the Fungal vertical slice proves the structure pipeline, expand one shared structure system rather than one placement engine per biome.

## 20.1 Structure tiers

- micro landmarks: camps, shrines, wreckage, resource signs;
- minor structures: small ruins, stations, caves, towers;
- regional structures: settlements, ports, foundries, vaults, bridges;
- major complexes: temples, mines, road hubs, large settlements;
- unique structures: Deepstone Conclave and six boss locations;
- rare megastructures: Lost Cities and giant pillar complexes.

## 20.2 Road network

Road generation should connect selected regional/major anchors after those anchors are placed. Roads are evidence of former civilization and navigation aids, not a universal grid.

Road degradation is biome-aware: fungus overtakes roads, sulfur cracks them, ice preserves them, fractures break them and Decay consumes them.

## 20.3 Lost Cities

Lost Cities are late production content. They must not block first playable milestones. Their implementation should use modular districts and infrastructure rather than one monolithic prefab.

---

# 21. Resource, crafting and equipment progression

Underworld resource progression should extend Magenheim rather than replace surface investment.

## 21.1 Progression bands

**Band U0 — Arrival:** surface endgame/Magenheim gear remains viable; Fungal resources produce survival, food and lighting tools.

**Band U1 — Bloom:** Heartcap Core unlocks first Underworld specialization.

**Band U2 — Blackwater:** Abyssal Keelstone unlocks reliable deep sailing and materials transport.

**Band U3A/U3B — Thermal and Rime:** Sulfur and Frozen branches provide complementary metallurgy/protection technologies.

**Band U4 — Fracture:** Fracture Anchor unlocks advanced vertical infrastructure and safe access to the deepest routes.

**Band U5 — Decay:** final anti-decay and high-tier Underworld crafting, Deepstone completion and Deepbound Portal technology.

## 21.2 Crystal Shaping integration

Underworld materials may create new crystal applications, environmental sockets or crafting recipes, but existing Magenheim Crystal Shaping tier/alignment authority remains unchanged unless explicitly revised in its own authority documents.

---

# 22. Traversal framework

Traversal is a system family, not a collection of cheat movement buffs.

Planned tools/features:

- deployable rope/anchor;
- winch or lift;
- engineered bridge components;
- climb-assist equipment;
- controlled gliding where appropriate;
- biome-specific traversal consumables;
- late-game permanent infrastructure.

Each traversal method must define valid anchor surfaces, maximum reach, multiplayer ownership, persistence of placed devices, recovery behavior and Roof Shelf protection.

---

# 23. Building framework

Underworld construction should use ordinary Valheim building expectations plus new material families.

Initial planned building families:

- deep stone/black stone structural pieces;
- fungal/mycelial decorative and light materials;
- Blackwater dock components;
- geothermal industrial components;
- reinforced bridges and supports;
- crystal lighting;
- elevator/winch infrastructure;
- late-game suspended visual pieces where technically safe.

No building set is admitted until placement, snapping, support, refund, wear/weather equivalent and multiplayer behavior are tested.

---

# 24. Deepbound Portals

Deepbound Portals are a late Underworld progression reward and must not exist early enough to erase Deep Gate logistics.

Implementation requirements:

- explicit source and target world-layer identity;
- server-owned link resolution;
- unique portal network namespace to prevent accidental collision with ordinary portal tags;
- valid destination checks;
- safe fallback when target world is unavailable;
- player/inventory transport rules consistent with selected Magenheim/Valheim portal policy;
- save/load and dedicated-server persistence;
- no duplication of world transition logic: Deepbound Portals call the authoritative transition service.

---

# 25. Subterranean environmental event framework

Shared event authorities should support:

- Sporefall;
- Ashfall;
- Stone Rain;
- Deep Fog;
- Whiteout;
- Crystal Resonance;
- Thermal Surge;
- Black Bloom.

The current authority uses the paired Underworld seed plus Valheim's server-authoritative world clock
to derive the same deterministic four-minute event window on every peer. That deliberately avoids a
second Magenheim weather-state replication subsystem. Clients may evaluate the pure schedule for
presentation, but gameplay effects must be re-evaluated/admitted by the server from the same seed,
time window, biome and hazard inputs rather than trusting client-reported event state.

Each event needs start/stop transitions, save/reload policy where state cannot be regenerated,
multiplayer agreement, audio profile, visual budget and gameplay modifiers. The deterministic
seed/time schedule itself does not require persistence.

---

# 26. Audio and music implementation sequence

Audio is implemented after environment geometry and creature ecology are stable enough to mix meaningfully.

Passes:

1. global Underworld low-frequency room tone;
2. biome ambience;
3. landmark loops such as lavafalls, rivers and Blackwater;
4. creature distant calls;
5. environmental event layers;
6. structure interior/reverb profiles;
7. boss presentation;
8. sparse biome/encounter music.

Audio must not be used to hide unclear mechanical telegraphs.

---

# 27. Configuration

Server-facing configuration should expose meaningful design controls without forcing operators to tune raw noise equations.

Recommended categories:

- Underworld enablement/testing override;
- gate frequency/placement constraints;
- world scale if technically supportable;
- basin scale;
- wall/roof threshold presets;
- biome weighting;
- structure density;
- boss rematch/summon settings;
- creature density/difficulty multipliers;
- hazard intensity;
- environmental event frequency;
- progression strictness where safe;
- Deep Boon balance values.

Expert raw field/noise controls may exist separately and must pass full definition validation.

---

# 28. Debug tooling

Underworld development is not viable without deterministic inspection tools.

Required debug capabilities should include:

```text
underworld status
underworld layer
underworld seed
underworld field <name>
underworld biome
underworld weather
underworld structure-eligibility
underworld spawn-table
underworld boss-state <boss-id>
underworld deepstones
underworld transition-test
underworld regen-zone   # only if runtime-safe and authoritative
```

Names are provisional. Debug operations must never silently mutate production saves without explicit safeguards.

---

# 29. Persistence model

Persist only facts that cannot safely be regenerated.

World-level examples:

- Underworld identity/schema;
- initialized generation fingerprint;
- unique structure identities/anchors where required;
- Deepstone Conclave state;
- boss encounter/defeat/reward state;
- placed player structures through normal world persistence;
- resource depletion where Valheim normally persists it;
- progression flags;
- Deepbound Portal links.

Player-level examples:

- return anchor/recovery state;
- selected Deep Boon;
- Underworld map discovery;
- layer-specific map pins if player-owned;
- normal inventory/equipment/skills through existing character persistence.

Do not serialize transient particles, attack GameObjects, AI paths or regenerable noise arrays.

---

# 30. Performance gates

Performance must be measured progressively.

Key budgets:

- terrain generation time per zone;
- decoration object count;
- active networked creature count;
- dynamic lights;
- particle systems;
- boss transient objects;
- structure collider count;
- Blackwater rendering cost;
- long sightline draw cost;
- memory after repeated transitions.

The project should establish representative benchmark locations: dense Fungal Forest, open Blackwater coast, Sulfur vent field, Frozen vault region, complex Fracture crossing and Great Decay colony.

---

# 31. Detailed execution phases

## Phase U0 — Reconcile and declare authority

**Goal:** establish source boundaries and technical assumptions before code.

Actions:

- inspect current Magenheim worldgen, persistence, network and definition-loading authorities;
- identify reusable systems and prohibit duplicate replacements;
- define exact Nowhere King completion state consumed by Underworld;
- declare source/data/test paths;
- add Underworld schema/version constants;
- add initial validation record.

Acceptance: authority map exists and no implementation relies on an unidentified duplicate system.

## Phase U1 — Definition schema and Core validation

**Goal:** create schema-validated Underworld definitions and deterministic fingerprint.

Actions:

- implement Core records/types;
- implement validation rules;
- implement canonical fingerprint;
- create minimum `world.json`, `biomes.json`, `bosses.json`, `deepstones.json` stubs with no false gameplay registration;
- add deterministic tests for malformed IDs, duplicate IDs, progression cycles, missing boss/stone links and fingerprint stability.

Acceptance: pure tests pass and runtime can load a validated no-content Underworld snapshot.

## Phase U2 — World identity and transition spike

**Goal:** prove a second persistent derived world context is feasible.

Actions:

- derive identity/seed;
- implement temporary test Deep Gate;
- transition one player into a minimal derived context;
- return safely;
- add save/reload and disconnect recovery;
- document exact Valheim APIs/hooks required.

Acceptance: transition matrix in Section 5.5 passes in a disposable environment.

If this phase disproves the intended architecture, stop biome implementation and revise the design authority explicitly.

## Phase U3 — Persistent world/map/multiplayer framework

**Goal:** turn the spike into a durable service.

Actions:

- create stable world context manager;
- map-state separation;
- server-authoritative transition RPC;
- return/recovery records;
- host/client admission;
- layer-aware pins/state;
- persistence migration versioning.

Acceptance: repeated multiplayer transitions, save/load and reconnect pass without cross-layer state contamination.

## Phase U4 — Terrain/cavern illusion prototype

**Goal:** prove the heightfield architecture visually and mechanically.

Actions:

- Macro Basin;
- Wall Mass;
- Roof Shelf;
- basic floor elevation;
- cavern-sky replacement;
- fog/occlusion;
- debug field visualizers;
- reachability tests against Roof Shelf.

Acceptance: normal gameplay cannot expose the world as an ordinary exterior map in the prototype basin.

## Phase U5 — Biome/environment framework

**Goal:** create generic biome classification and environment runtime.

Actions:

- biome classifier;
- environment profiles;
- hazard profile interface;
- transition modifiers;
- map display;
- event eligibility;
- data-driven decoration/resource hooks.

Acceptance: at least two synthetic test biome definitions can occupy one test world and transition without separate hard-coded generators.

## Phase U6 — Shared creature/spawn framework

**Goal:** register and populate data-defined Underworld ecology.

Actions:

- creature registry;
- spawn planner;
- population budgets;
- land/water eligibility;
- elite caps;
- multiplayer ownership;
- test dummy creatures.

Acceptance: deterministic spawn planning and live host/client population behavior work without per-creature registrars.

## Phase U7 — Boss + Deepstone framework

**Goal:** make boss progression infrastructure before producing six bosses.

Actions:

- shared boss encounter state;
- arena/leash runtime;
- summon runtime;
- defeat/reward guard;
- trophy registration;
- Deepstone Conclave unique location;
- six empty stone slots;
- Deep Boon selection state;
- Deep Sigil discovery mechanism.

Acceptance: a test boss can be summoned, defeated, trophy-mounted, boon-selected, saved/reloaded and repeated without duplicate progression rewards.

## Phase U8 — Fungal Forest vertical slice

Implement the entire Section 14 package.

Acceptance: full biome package template passes.

## Phase U9 — First Bloom production encounter

The boss receives a dedicated implementation pass after general Fungal ecology is stable. Build model/presentation, animator, attack director, arena, summon offering, persistent encounter state, rewards, trophy and boon. Test solo, multiplayer, disengage, unload/reload, death/restart and rematch.

## Phase U10 — Blackwater systems and biome package

Implement Ocean Basin generation, Blackwater rendering/environment, shoreline blending, aquatic spawn support, Blackwater structures/resources/ecology and vessel progression.

Acceptance includes long-distance sailing and host/client aquatic AI behavior.

## Phase U11 — Blackwater Maw

Implement The Drowned Ring and Blackwater Maw encounter. Validate shore/water movement, arena boundary, player death, vessel interaction and rematch persistence.

## Phase U12 — Sulfurous Wastes package

Implement Thermal field production rules, heat/gas hazard systems, lava/vent landmarks, resources, structures and seven-creature ecology.

## Phase U13 — Furnace Heart

Implement boss, thermal arena controls, reward, trophy and boon. Validate that hazard difficulty remains readable and preparation-based rather than unavoidable damage.

## Phase U14 — Frozen Caverns package

Implement Cryogenic field production rules, deep-cold hazard, glaciers/frozen water presentation, resources, structures and seven-creature ecology.

## Phase U15 — White Silence

Implement boss, Stillvault arena, readable sound-suppression presentation, Rimeheart reward, trophy and boon.

## Phase U16 — Traversal framework

Before Fracture production, implement the first safe rope/anchor/winch/bridge vertical traversal family. Validate multiplayer persistence and Roof Shelf protection.

## Phase U17 — Fracture Zones package

Implement high-Fracture generation, ravines, natural bridges, landmark placement, structures, resources, events and seven-creature ecology.

## Phase U18 — Rift Titan

Implement localized force mechanics, segmented body behavior, arena platform validation, reward, trophy and boon. Explicitly regression-test against Nowhere King Gravity Inversion code to avoid duplicate or conflicting physics authority.

## Phase U19 — Great Decay package

Implement Decay field, contamination system, consumed structure variants, Black Bloom event, resources and seven-creature ecology.

## Phase U20 — Carrion Crown

Implement Consumed Basilica, deterministic biomass nodes, contamination encounter pressure, Decay Heart reward, trophy and boon.

## Phase U21 — Deepstone completion and Deepbound Portals

When all six boss/trophy lines are production-functional, enable final Conclave completion logic and Deepbound Portal technology using the existing authoritative transition service.

## Phase U22 — Civilization expansion

Add mature road networks, pillar settlements, fungal settlements, geothermal foundries, frozen vault complexes, fracture bridges, Decay colonies, Blackwater ports, deep temples, mines and rare Lost Cities.

## Phase U23 — Building expansion

Add production-quality Underworld building sets and infrastructure. Validate support, snapping, wear/refund, multiplayer and save/load.

## Phase U24 — Events/audio/music polish

Complete subterranean event presentation, acoustic profiles, distant creature calls, biome ambience and music once gameplay timing is stable.

## Phase U25 — Configuration and compatibility pass

Expose validated server configuration, compatibility registration hooks and fail-closed handling for invalid worldgen/definition changes.

## Phase U26 — Expansion-scale validation

Run long-session worlds and complete the full validation matrix below.

---

# 32. Validation matrix

Every production candidate must distinguish these gates:

| Gate | Required evidence |
|---|---|
| Core | deterministic tests and schema/fingerprint checks |
| Compile | warnings/errors outcome for runtime and Core |
| Startup | plugin bootstrap and definition load |
| World transition | surface ↔ Underworld repeated transition |
| Fresh Underworld | deterministic generation in a new derived world |
| Existing Underworld | save migration/reload after source update |
| Biome | intended generation, environment and hazards |
| Creature | spawn, AI, drops, despawn/persistence and multiplayer ownership |
| Boss | summon, phases, disengage, reload, defeat, reward guard, rematch |
| Deepstone | trophy mount, visual state, boon selection, multiplayer persistence |
| Structures | placement, terrain adaptation, exclusion, entrances and persistence |
| Resources | harvest/depletion/crafting persistence |
| Building | placement/support/refund/save/load/multiplayer |
| Portals | cross-layer link and failure recovery |
| Performance | benchmark locations and repeated transition memory behavior |
| Compatibility | surface additive behavior and representative mod interactions |

---

# 33. First production milestones

## Milestone A — Technical descent

Deliverable: Nowhere King unlock → Deep Gate → persistent derived Underworld → return, with no production biome content.

## Milestone B — Believable cavern world

Deliverable: one large basin with wall masses, inaccessible Roof Shelf, no normal sky/Yggdrasil, stable fog/lighting and safe multiplayer entry.

## Milestone C — Living Fungal Forest

Deliverable: complete Fungal Forest generation, resources, structures and ecology without its boss.

## Milestone D — First boss progression loop

Deliverable: Deepstone Conclave + Deep Sigil + First Bloom + trophy + Spore Communion, fully persistent.

This is the first point at which the Underworld should be considered a genuine playable expansion slice rather than a technical prototype.

## Milestone E — Regional exploration

Deliverable: Fungal Forest + Blackwater + Blackwater Maw + first vessel progression + roads/settlements sufficient for multi-hour exploration.

## Milestone F — Mid-Underworld branch

Deliverable: complete Sulfurous and Frozen branches with their bosses and complementary progression rewards.

## Milestone G — Deep traversal

Deliverable: Fracture Zone, traversal infrastructure and Rift Titan.

## Milestone H — Underworld endgame

Deliverable: Great Decay, Carrion Crown, all Deepstones, final portal/infrastructure unlocks and completed primary progression loop.

---

# 34. Definition-completion workflow for each biome

Before implementing a new biome after Fungal Forest, perform a bounded design-definition pass in this sequence:

1. lock biome fantasy and gameplay identity;
2. define field thresholds and transition tags;
3. define terrain/material language;
4. define lighting/fog/audio/events;
5. define hazards and mitigation;
6. define resource families and their actual uses;
7. define structure families and Deep Sigil source;
8. define complete creature roster with ecological roles;
9. define unique boss location and summon offering;
10. define boss combat language, progression reward, trophy and boon;
11. validate progression dependencies against earlier biomes;
12. add definitions to schema-validated data;
13. implement through shared registries/frameworks;
14. run biome package acceptance gate;
15. only then begin the next biome.

This prevents the project from creating six partially realized biomes at once.

---

# 35. Boss-completion workflow

Each boss receives the same production discipline:

1. final encounter design;
2. location/arena design;
3. offering and summon contract;
4. model/visual authority;
5. animation plan;
6. shared encounter-state binding;
7. shared arena/leash binding;
8. attack-director implementation;
9. phase transitions;
10. multiplayer authority;
11. unload/reload reconstruction;
12. defeat transaction;
13. unique reward;
14. trophy;
15. Deepstone slot activation;
16. Deep Boon;
17. rematch behavior;
18. solo runtime validation;
19. multiplayer validation;
20. persistence/restart validation.

No boss is considered complete because it can be spawned and killed once.

---

# 36. Initial file-production order

Once implementation begins, the expected first source additions are deliberately framework-heavy:

```text
src/Magenheim.Core/Underworld/
  UnderworldConstants.cs
  UnderworldWorldIdentity.cs
  UnderworldDefinitionSet.cs
  UnderworldDefinitionValidator.cs
  UnderworldDefinitionFingerprint.cs
  UnderworldBiomeDefinition.cs
  UnderworldCreatureDefinition.cs
  UnderworldBossDefinition.cs
  UnderworldDeepstoneDefinition.cs
  UnderworldProgressionGraph.cs

src/Magenheim.Runtime/Underworld/
  UnderworldManager.cs
  UnderworldWorldContext.cs
  UnderworldWorldTransitionManager.cs
  UnderworldUnlockRuntime.cs

src/Magenheim.Runtime/Underworld/Generation/
  UnderworldGenerationContext.cs
  UnderworldGenerator.cs
  MacroBasinField.cs
  WallMassField.cs
  RoofShelfField.cs
  UnderworldBiomeClassifier.cs

src/Magenheim.Runtime/Underworld/Environment/
  UnderworldSkyRuntime.cs
  UnderworldFogRuntime.cs
  UnderworldLightingRuntime.cs

src/Magenheim.Runtime/Underworld/Deepstones/
  DeepstoneConclaveRuntime.cs
  DeepstonePersistence.cs
  DeepBoonRuntime.cs

src/Magenheim.Runtime/Underworld/Bosses/
  UnderworldBossEncounterState.cs
  UnderworldBossEncounterRuntime.cs
  UnderworldBossArenaRuntime.cs
  UnderworldBossPersistence.cs
```

Actual paths must be reconciled against the live source tree before creation. Reuse existing Magenheim authorities when equivalent functionality already exists.

---

# 37. Work explicitly deferred until framework proof

The following work is intentionally not first-wave production:

- all six final boss models at once;
- complete Lost Cities;
- full music score;
- every building set;
- final balance values;
- a second deeper world beneath the Great Decay;
- separate Underworld repository/mod packaging;
- elaborate compatibility APIs before the internal registration contracts are stable.

This is not scope removal. It is dependency ordering.

---

# 38. Immediate next implementation target

The first dependency-valid code slice after this plan is adopted is **Phase U0/U1 combined only far enough to establish the Underworld authority boundary and schema skeleton**.

Specifically:

- inspect the live Magenheim source tree for existing definition loader, fingerprint, world state, network and Nowhere King defeat authorities;
- create a source map showing exactly which existing systems will be extended;
- define the minimal Underworld Core records and schema version;
- add deterministic validation/fingerprint tests without yet registering a biome or world transition;
- record the exact world-transition feasibility questions that Phase U2 must answer.

The next step is therefore framework authority, not creature art, not biome decoration and not six simultaneous bosses.

That preserves the central development rule of the Underworld: build the machinery once, prove it, and then let every biome, creature, boss, structure and Deepstone use it.