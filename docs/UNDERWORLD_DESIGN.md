# The Underworld — Durable Expansion Design

> Authoritative correction (2026-09-17): the Underworld uses altered surface-world
> terrain beneath one shared custom fantastical cavern-roof skybox for all biomes.
> The roof cannot vary by biome or terrain area and is not physical geometry.
> Ignore conflicting roof-shelf, ceiling-generation, roof-attachment and biome-specific
> ceiling requirements below. Use ground-supported terrain, flora and local props.
> See `UNDERWORLD_FLORA_TERRAIN_PLAN.md` for the active implementation sequence.

> Ecology, resources, equipment tiers and progression parity with the overworld are
> planned in [UNDERWORLD_FLORA_TERRAIN_PLAN.md](UNDERWORLD_FLORA_TERRAIN_PLAN.md).

**Project:** Magenheim for Valheim  
**Logical expansion identity:** The Underworld  
**Development location:** `mrcalzon02/Magenheim`, authoritative branch `main`  
**Status:** durable design authority; implementation not yet admitted  
**Unlock position:** post-Nowhere King  
**Primary dependency:** Magenheim systems, progression, persistence, compatibility rules, and release pipeline

## Binding player-facing objective

**The Underworld is not another continent next door, and it is definitely not a second save that requires quitting Valheim and loading another world. It is a second logical world layer inside the same persistent Valheim game world.**

Its distant reserved coordinates exist only so Valheim can simulate and persist a second heightfield region in the same session. They are not the geography shown to players. The Underworld has its own logical coordinate projection, biome map, exploration/cloud state and Surface | Underworld map tab. Surface and Underworld therefore share simulation and persistence while remaining distinct maps and world layers to the player. This objective outranks attempts to force the reserved region to behave like an ordinary adjacent vanilla continent.

## 1. Executive concept

The Underworld is a full expansion-scale world beneath Valheim. The surface world is treated as the prelude: after the Nowhere King is defeated, players gain access to a second persistent world-space representing the deep underside of Valheim itself.

The Underworld is not a normal biome, not a single dungeon, and not a decorative cave layer. It is intended to feel like another Valheim-sized world with its own geography, oceans, biomes, structures, creatures, resources, traversal systems, environmental hazards, settlements, progression, and long-term building possibilities.

There is no conventional sky, sun, moon, or visible branch of Yggdrasil. The world is lit by bioluminescent fungus, glowing moss, mineral seams, geothermal vents, magma, crystal formations, strange atmospheric light and artificial player settlement. In the distance, enormous geological masses disappear upward into darkness. Lava can pour from heights beyond view. Black underground seas reflect red and blue illumination rather than daylight.

The intended reveal is simple:

> You conquered Valheim. Now discover what it was standing on.

## 2. Repository and packaging decision

For the foreseeable future, The Underworld will be developed **inside Magenheim** rather than as a second GitHub repository or separately distributed dependency chain.

This is deliberate. Magenheim already owns the Nowhere King, Crystal Shaping, elemental systems, world-generation compatibility doctrine, multiplayer authority work, persistence conventions, build pipeline and release process that The Underworld depends upon. Splitting those systems prematurely would duplicate authority and create unnecessary dependency failure modes.

The Underworld therefore has two identities at once:

- **Player-facing identity:** a major expansion called **The Underworld**.
- **Implementation identity:** a bounded Magenheim subsystem, expected to live under namespaces such as `Magenheim.Underworld` and to ship through Magenheim until a future explicit decision changes that boundary.

A future split into a separate distributable mod remains possible only after the world architecture and integration contracts are proven. The current design must preserve modular boundaries so such a split would be technically possible, but **no second repository, duplicate engine, alternate authority, or parallel implementation is part of the current plan**.

## 3. Relationship to Magenheim

Magenheim is not merely a prerequisite installed beside The Underworld. It is the progression and systems foundation.

The Nowhere King encounter remains owned by Magenheim. The Underworld consumes a durable completion state indicating that the encounter has been legitimately defeated. It must not patch the boss merely to unlock itself.

The intended progression boundary is:

1. Surface Magenheim progression culminates in the Nowhere King.
2. Defeating the Nowhere King establishes an authoritative world progression flag.
3. Existing or newly awakened geological access points become capable of opening the route below.
4. The player enters The Underworld with their established character, inventory, skills, Magenheim Crystal Shaping progression, socketed equipment and other character-level state intact.
5. Underworld progression extends those systems rather than replacing them.

Existing Magenheim Deep Fractures and megadungeon language should act as foreshadowing. Deep Fractures remain discrete underground adventure spaces. The Underworld is the much larger world beneath them. Some late Deep Fracture environments may imply that deeper geology continues beyond accessible dungeon boundaries, but they are not secretly the same runtime space.

## 4. Core world illusion: not true volumetric caverns

A central technical concession is now authoritative for the design:

**The Underworld does not require a continuous physically modeled cavern ceiling.**

Valheim terrain is fundamentally strongest as a heightfield. Attempting to force ordinary world terrain into both a floor and a continuous overhead roof would create unnecessary complexity, fragile mesh streaming, collision problems, seams, multiplayer synchronization risk and severe performance cost.

Instead, The Underworld uses Valheim-style terrain to create the illusion of near-boundless caverns.

The world is composed as negative space between gigantic geological masses. Immense mountain walls rise beyond practical view distance and beyond normal playable/buildable height. Their upper regions flatten into broad inaccessible plateaus that function as the hidden "roof shelf" of the world. Atmospheric darkness, fog, occlusion, lighting and local hanging geometry prevent the player from seeing that the world is technically open above those walls.

The player experiences a cavern. The engine experiences an extreme heightfield world.

### 4.1 Three vertical bands

The world should be generated conceptually in three height bands.

**Habitable Underworld** is the actual traversable game world: valleys, fungal forests, lakes, oceans, settlements, roads, fractures, ruins, geothermal fields and player bases.

**Wall Country** is the zone of extremely steep mountains, mega-cliffs and geological barriers that create cavern boundaries. These masses frequently disappear upward into darkness and should be too steep or hazardous for ordinary traversal.

**Roof Shelf** is the inaccessible high plateau above useful play elevation. It exists to complete occlusion and prevent ordinary gameplay from reaching a vantage point where the world reads as an exterior heightmap. It is technical scenery, not exploration content.

### 4.2 Absolute visual invariant

Normal gameplay must never provide a reasonable vantage point from which the player can see over the wall masses and recognize an ordinary open world beyond them.

Flying cheats, noclip and developer tools may obviously break the illusion. Ordinary climbing, building, traversal and progression must not.

That invariant affects terrain slope, build-height restrictions, traversal abilities, fog, line of sight, wall spacing and the maximum height reachable by structures.

## 5. Cavern-scale geography

The Underworld should feel substantially larger than a dungeon and spatially comparable to a normal Valheim world.

Individual "cavern chambers" are therefore not rooms. They are basin-scale geographic spaces that may be kilometers across. A Fungal Forest chamber can contain several watersheds, settlements and major structures. A Blackwater sea can require long-distance sailing. A Fracture Zone can divide an entire region.

The world should be denser than the surface in terms of navigable land, but terrain constraints replace the function of open ocean gaps. Travel is shaped by wall masses, blackwater basins, fractures, volcanic belts, frozen escarpments, collapsed routes and narrow passes.

The player should gradually learn geography through landmarks rather than celestial orientation.

Examples:

- the blue forest beyond the split pillar;
- the sulfur coast below the three lavafalls;
- the frozen wall west of the Blackwater Deep;
- the road that vanishes into the Great Decay;
- the fracture crossed by the broken ancient bridge.

## 6. One world, one save: the Underworld is an instanced region

**The Underworld is a region of the same Valheim world and the same save file as the surface. It is
not a second save, not a second server, and not a world the player loads instead of theirs.**

This section previously specified a "separate persistent world instance" and speculated about
"controlled world-context switching". That was wrong, it was built, and it produced an Underworld
that could only be reached by quitting to the main menu and loading a different save — which defeats
the entire purpose. The corrected architecture below is binding. Do not reintroduce the old model.

### 6.1 The mechanism already exists in Valheim: dungeon interiors

Valheim's instanced dungeons are the working precedent, and they are *not* separate worlds. Verified
against the installed 1.0.12 assemblies:

| Element | What it does |
|---|---|
| `Location.m_hasInterior`, `m_interiorRadius` | Marks a location as owning an interior volume |
| `Location.m_interiorEnvironment` | Gives that volume its own sky, fog and lighting |
| `Location.m_interiorTransform`, `m_useCustomInteriorTransform` | Places the interior away from the surface footprint |
| `Location.m_generator` (`DungeonGenerator`) | Builds the interior contents |
| `DungeonGenerator.m_zoneCenter`, `m_zoneSize` | The interior occupies a reserved coordinate volume |
| `ZoneSystem::SpawnLocation` | Places it; the **same** `ZoneSystem` then streams it |

A dungeon interior lives at coordinates far from surface play, inside the one running world. It is
streamed by the same `ZoneSystem` (`m_zones`, `m_generatedZones`, `m_locationInstances`) and
persisted as ordinary ZDOs in the same `.db`. Both the surface and every dungeon interior are
resident in the same session at the same time. Third-party dungeon-map mods work precisely because
the interior is normal world space with a known coordinate offset.

### 6.2 The Underworld is that pattern at world scale

The Underworld applies the same instancing, with two differences: the reserved volume is
world-sized rather than room-sized, and its contents come from Valheim's own terrain generator
rather than from a room list.

- The reserved region is offset **horizontally**, not vertically. Valheim terrain is a 2D
  heightfield — `Heightmap.m_heights` is a flat list and `WorldGenerator.GetBiomeHeight` returns one
  height per column — so surface ground and Underworld ground can never share an (x, z). Valheim
  solves this the same way for its own far landmasses: `worldSize = 10000`, `waterEdge = 10500`,
  `deepNorthMinDistance = 12000` with `deepNorthYOffset = 4000`. The Underworld is another such
  region, streamed by the same ZoneSystem into the same save.
- The playable layer uses logical Underworld coordinates. Runtime adapters map those into the
  reserved region immediately before placement or generation. This is `UnderworldSpatialDomain` in
  `Magenheim.Core`, which is the authority for that mapping.
- Terrain inside the band is produced by Valheim's own world generator, reshaped through Harmony
  postfixes on `WorldGenerator.GetBiomeHeight` and `WorldGenerator.GetBiome`, so the Underworld is a
  real generated landmass with the six Underworld biomes rather than a hand-placed set.
- Surface coordinates outside the band pass through completely unmodified.

### 6.3 Required invariants

- One `ZNet`, one `ZoneSystem`, one `WorldGenerator`, one `ZDOMan`, one `.fwl`/`.db` pair. These are
  per-world singletons in Valheim; two simultaneously loaded worlds are not possible in one process
  and must never be attempted.
- Both layers are resident concurrently. A player standing on the surface and a player in the
  Underworld are in the same running world at the same time.
- Travel between layers is a **teleport within the world**. It is never a save load, never a
  reconnect, and never a return to the main menu.
- **The Underworld uses the surface world's seed verbatim.** It is the *generation algorithm* that
  differs, not the seed: the same seed run through the Underworld's biome fields, terrain rules and
  vertical bands produces an entirely different map. This satisfies §10 — the player cannot navigate
  below by copying their surface map — without introducing a second seed to keep in sync, and it
  guarantees that one surface world always yields exactly one Underworld. Do not hash, salt or
  derive a separate terrain seed.
- Underworld placed state, construction, resource depletion and progression persist as ordinary ZDOs
  in the shared save, distinguished by their location in the reserved band.
- Multiplayer needs no second server and no special session handling. Normal ZDO and `ZNetView`
  ownership applies because there is only ever one world.
- Map and exploration state are still separated per layer (§25). That is a presentation and
  discovery-state concern, not a reason to separate the save.

### 6.4 Explicitly forbidden

- A second save file, a derived save name, or any world-pair manifest that points at one.
- `IUnderworldPhysicalWorldLoader` or any equivalent "physical world switch" abstraction.
- Calling `ZNet.LoadWorld`, `WorldGenerator.Initialize` or `FejdStartup` paths to change worlds.
- Any design that requires the player to leave the session to reach the Underworld.
- A second dedicated server process as the mechanism for layer separation.


## 7. Entry and the Descent

Underworld access unlocks only after the Nowhere King has been defeated.

The preferred surface access language is geological rather than another ordinary portal frame. The Magenheim fracture/cave visual family should be reused so the transition feels like a natural escalation of systems the player has already encountered.

A Deep Gate site should resemble an enormous geological wound or ancient reinforced shaft. Before the unlock it may be dormant, blocked, sealed or apparently bottomless. After the Nowhere King falls, crystal activity, fungal growth, heat, strange air movement or ancient mechanisms indicate that the deeper route is now accessible.

The initial transition should communicate descent rather than simple teleportation. The player should experience darkness, passing stone or implied depth, increasing red/blue illumination, distant fungal lights and finally the opening of an immense subterranean landscape.

## 8. Environmental presentation

### 8.1 No conventional sky

The Underworld must not display the recognizable Valheim sky treatment or Yggdrasil branch.

The apparent overhead environment should read as unreachable darkness and stone through a dedicated environmental presentation:

- near-black upper atmosphere;
- high altitude fog/occlusion;
- deep red mineral or thermal glows;
- sparse blue/cyan "star-like" bioluminescent colonies;
- drifting dust, ash and spores;
- local silhouettes of high cliffs and hanging rock prefabs;
- occasional distant movement that can initially be mistaken for stars or geology.

The skybox is therefore a technical renderer requirement, not a literal sky in the fiction.

### 8.2 Lighting philosophy

There is no sun and no moon.

Ambient illumination should remain low enough that artificial light matters without making normal navigation miserable. Most visible light should appear to come from identifiable environmental sources.

Primary light languages include deep blue, cyan, violet, dark crimson, molten orange, mineral purple, fungal turquoise and nearly black regions with isolated points of light.

Thousands of glowing fungi must not become thousands of dynamic lights. Most environmental bioluminescence should use emissive materials and bounded pooled illumination.

### 8.3 Hanging geometry without a continuous roof

The world may still contain local overhead geometry:

- gigantic stalactites;
- hanging stone shelves;
- downward cliff masses;
- fungal curtains;
- mineral shelves;
- ancient suspended structures;
- roots or mycelial growth;
- lavafall source formations.

These are conventional prefabs or local meshes used to strengthen the cavern illusion. They do not have to tile into a universal ceiling.

## 9. Terrain generation model

The Underworld generator should produce large-scale fields that combine rather than simple circular biome islands.

Proposed procedural fields:

| Field | Purpose |
|---|---|
| Macro Basin | Creates major habitable chambers, valleys and regional depressions |
| Wall Mass | Determines giant mountain barriers and enclosure geometry |
| Roof Shelf | Controls inaccessible high plateaus and upper occlusion mass |
| Fracture | Produces deep ravines, tears, narrow passes and catastrophic geology |
| Thermal | Drives lava, sulfur, geothermal vents and high-heat regions |
| Moisture | Drives fungal density, springs, wetlands and subterranean rivers |
| Cryogenic | Drives Frozen Caverns and glacial basins |
| Decay | Drives Great Decay encroachment and corrupted hybrid zones |
| Ocean Basin | Determines Blackwater seas and coastal geography |
| Luminosity | Drives natural emissive ecology and mineral-light density |
| Pillar / Monolith | Controls huge free-standing masses and landmark geology |

Biome selection derives from combinations of these fields. Fractures in particular should be capable of cutting across other biomes rather than behaving as isolated paint regions.

## 10. Relationship to the surface world

The Underworld should subtly feel like the underside of the surface world without being a one-to-one inverted copy.

The derived seed may use surface macro information to influence deep geography. Large surface landmasses can correspond to greater geological mass below. Surface ocean areas may contribute to Blackwater basins or thinner structural zones. Great mountains may influence huge downward walls, pillars or compressed geology.

The correspondence should be discoverable only in broad patterns. A player must not be able to navigate the Underworld simply by copying their surface map.

## 11. Oceans and water systems

The Underworld contains true navigable water bodies.

The principal ocean system is the **Blackwater Deep**: enormous dark seas reflecting cavern illumination rather than sky. Coastlines are enclosed by walls and distant silhouettes, creating the feeling of sailing through an underground ocean.

Water sources can include:

- pressure springs;
- glacial melt;
- cliff-face rivers;
- waterfalls emerging from upper fog;
- ancient aqueducts;
- thermal reservoirs;
- flooded fracture systems.

Waterways should function as navigation systems and settlement corridors rather than decoration.

## 12. Lavafalls

Lavafalls are one of the signature landmarks of The Underworld.

Because the true upper world is hidden by fog and extreme wall height, lava can begin at a high prefab or cliff source concealed beyond visibility. From the player perspective it appears to pour from the unseen roof for hundreds of meters.

Lavafalls simultaneously function as:

- landmarks;
- light sources;
- environmental hazards;
- navigation aids;
- geothermal resource locations;
- biome markers;
- settlement/structure anchors.

Some strike land. Some disappear into fractures. Some enter Blackwater and create enormous steam fields.

## 13. Subterranean environmental events

The Underworld has no normal weather cycle. It has environmental events occupying the same gameplay role.

**Sporefall:** glowing fungal spores descend slowly through large chambers.

**Ashfall:** volcanic material and hot particulate fill sulfurous regions.

**Stone Rain:** unstable fracture regions periodically shed debris from high unseen geology.

**Deep Fog:** heavy moisture and temperature inversions reduce visibility around Blackwater.

**Crystal Resonance:** distant crystal formations begin glowing or resonating together.

**Thermal Surge:** geothermal activity, geysers and lava hazards intensify.

**Black Bloom:** the Great Decay becomes temporarily more active, increasing biological hazards and creature activity.

These systems should be biome-weighted, readable and mechanically relevant rather than arbitrary visual filters.

## 14. Primary biome catalog

The first full Underworld design contains five principal terrestrial biomes plus the Blackwater ocean system.

### 14.1 Fungal Forests

The closest thing the Underworld has to a welcoming region, although never truly safe.

Gigantic mushrooms occupy the ecological role of trees. Glowing moss blankets stone. Shelf fungi cover walls. Mycelial mats cross the ground. Large fungal structures can rise dozens of meters or more.

Dominant colors are blue, cyan, turquoise and violet.

Gameplay identity: **life, concealment, biological interaction and abundance**.

Fungal resources can be edible, poisonous, explosive, reactive to sound/light, useful for alchemy, structurally useful or capable of attracting creatures.

Representative environmental assets include Tower Caps, Lantern Fungi, Glow Reeds, Spore Trees, Shelf Colonies, Moss Curtains, Luminous Root Mats and Mycelial Fields.

### 14.2 Sulfurous Wastes

Hot, dry, mineral-rich and geologically unstable.

The landscape mixes black basalt, sulfur crust, boiling pools, geothermal vents, obsidian shelves, lava channels and hanging red illumination.

Gameplay identity: **environmental hostility, heat management and thermal engineering**.

Toxic gases and heat exposure make the biome dangerous even without enemies. Valuable ores and geothermal materials make long-term settlement worthwhile.

### 14.3 Frozen Caverns

Subterranean cryogenic basins rather than a copy of the surface Mountains.

Cold air pools in enclosed valleys. Ice sheets cover rock. Frozen waterfalls emerge from upper cliffs. Underground glaciers occupy large basins. Black stone, pale ice and violet-blue illumination dominate the visual language.

Gameplay identity: **cold, silence, brittle terrain and preservation**.

These regions should contain unusually intact ancient remains because decay and biological overgrowth progress slowly.

### 14.4 Fracture Zones

Regions where the geological structure of the Underworld appears to have failed.

Huge ravines divide landscapes. Natural bridges cross abysses. Roads terminate at empty space. Rivers fall into darkness. Wall masses become narrow and violently vertical.

Gameplay identity: **vertical traversal, catastrophic geology and unreliable movement**.

Advanced ropes, grapples, bridges, elevators, gliding tools or other late traversal technologies become especially valuable here.

Gravity or magical anomalies may appear later, but they should be localized systems rather than an excuse to ignore terrain readability.

### 14.5 The Great Decay

A biological-horror biome where the normal fungal ecosystem has collapsed into an aggressive consuming network.

Rock, ruins, mineral deposits and corpses become incorporated into black, red, brown and sickly luminous biomass. Some enemies emerge directly from the substrate. Spores remain permanently heavy in the air.

Gameplay identity: **consumption, contamination and ecosystem hostility**.

The central narrative question is whether the Great Decay originated naturally in the deep world or is the consequence of something older buried still farther below.

### 14.6 Blackwater Deep

The principal subterranean ocean system.

Water is nearly black except where it reflects fungal, crystal, volcanic or settlement light. Shores can belong to any adjoining biome. Large aquatic and shoreline creatures may eventually occupy the deepest areas.

Gameplay identity: **navigation, isolation and abyssal scale**.

## 15. Biome transitions and hybrids

Biome borders should grade through ecology and geology rather than switch abruptly.

Examples include:

- heat-tolerant fungal forest transitioning into sulfur terrain;
- frost slowly accumulating as fungal valleys approach Frozen Caverns;
- normal fungal growth becoming malformed before the Great Decay begins;
- Fracture Zones cutting through Frozen, Sulfurous or Decayed regions;
- Blackwater coasts inheriting the hazards and ecology of adjacent terrestrial biomes.

Hybrid spaces such as Frozen Fractures, Sulfur Fractures, Decayed Forests and Frozen Decay should occur naturally where field combinations justify them.

## 16. Geological landmarks

### 16.1 Great pillars and monoliths

Gigantic stone masses should occasionally rise so far into the darkness that their upper extent cannot be seen. They function as navigation landmarks, settlement sites, mining regions and structure foundations.

Some can contain carved roads, spiraling stairways, ruins or internal dungeon spaces. Others may be fractured, hollow or coated in fungal ecosystems.

### 16.2 Subterranean rivers

Rivers can emerge from cliffs, springs, glaciers or ancient infrastructure and then disappear into Blackwater or deep fractures. They should help establish coherent watersheds rather than random decorative channels.

### 16.3 Impossible scale

The world should repeatedly use distance and atmosphere to make the player misjudge size. A moving object on a wall may initially appear small because it is hundreds of meters away. A silhouette in Blackwater may initially appear to be an island.

Large scale must remain occasional enough to retain impact.

## 17. Structure philosophy

The Underworld should feel archaeologically old and culturally layered.

Not every ruin belongs to one civilization. Different building traditions imply multiple eras of settlement, exploitation, worship, containment and collapse.

Environmental storytelling should precede exposition. Roads, mines, ports, damaged bridges, sealed vaults and abandoned settlements explain history before text does.

Initial structure families:

| Family | Purpose |
|---|---|
| Deep Gates | Surface/Underworld transit |
| Ancient Roads | Navigation and evidence of former civilization |
| Pillar Settlements | Vertical construction around giant geological masses |
| Fungal Settlements | Biological/low-impact settlements in Fungal Forests |
| Geothermal Foundries | Sulfurous industry and high-tier crafting geography |
| Frozen Vaults | Preserved ancient facilities and sealed records |
| Fracture Bridges | Infrastructure crossing abyssal terrain |
| Decay Colonies | Settlements being consumed or fully reclaimed |
| Blackwater Ports | Underground maritime settlements |
| Deep Temples | Ritual, historical and containment complexes |
| Mines | Resource infrastructure and creature habitats |
| Lost Cities | Rare enormous multi-zone ruins |

Road systems should sometimes connect these sites across kilometers and degrade according to biome conditions.

## 18. Creature philosophy

Underworld creatures should not be surface enemies with larger numbers.

The ecological roster should include:

- fungal organisms;
- blind or vibration-sensing predators;
- subterranean insects;
- thermal creatures;
- crystalline organisms;
- ancient constructs;
- decay organisms;
- ceiling/wall-clinging threats;
- gliding creatures;
- abyssal aquatic life.

Vertical threat matters. Players should sometimes need to look upward, scan walls or read movement in fog rather than only watch the ground ahead.

Large creatures should be relatively rare. The environment supports enormous life, but constant giant monsters would normalize the spectacle.

## 19. Resource and crafting philosophy

The Underworld extends endgame progression rather than resetting it.

Surface materials, Magenheim crystals and existing equipment remain useful. New materials combine with those systems rather than replacing them immediately.

Major resource families can include:

- deep fungal materials;
- thermal minerals;
- sulfur compounds;
- underworld metals;
- abyssal biological materials;
- glacial crystal;
- fracture crystal;
- decay biomass;
- Blackwater materials;
- ancient recovered alloys.

High-tier crafting should increasingly depend on place as well as inventory. Geothermal stations, Blackwater ports, ancient machinery or other regional infrastructure can encourage permanent bases below.

## 20. Magenheim crystal integration

Crystal Shaping remains Magenheim authority.

The Underworld may introduce new sources, applications and environmental interactions without implementing a second crystal progression engine.

Examples of existing alignment interaction:

- Fire can help against thermal hazards or power heat-dependent mechanisms;
- Frost can support cryogenic survival and ice interactions;
- Earth can reduce stagger or geological displacement effects;
- Storm can interact with conductive fracture minerals and electrical environments;
- Radiance can be especially effective against Decay organisms or sanctified zones;
- Spirit and Seidr can interact with ancient entities, ruins or deep magical infrastructure.

These are extension hooks, not final balance commitments.

## 21. Progression philosophy

Players arrive after defeating an endgame Magenheim boss. They are already powerful.

The Underworld should therefore not pretend they have restarted Valheim progression with stone tools.

Progression is about **adaptation to a hostile world**:

- environmental protection;
- deep navigation;
- heat/cold/gas survival;
- better underground sailing;
- advanced mining;
- vertical traversal;
- bridge and elevator infrastructure;
- regional crafting;
- specialized equipment;
- eventually reliable surface/Underworld portal technology.

Difficulty should come from ecology, geography, hazards and travel logistics as much as enemy health inflation.

## 22. Vertical traversal

The world design justifies stronger traversal systems than ordinary surface Valheim.

Potential progression includes ropes, deployable anchors, grapples, climbing equipment, winches, elevators, bridges, controlled gliding and biome-specific traversal aids.

These systems must respect the cavern illusion invariant. They can make enormous walls and fractures navigable where intended without allowing ordinary players to reach the hidden Roof Shelf and expose the heightfield trick.

## 23. Building and settlement

Players must be able to establish permanent Underworld settlements.

The world is not an expedition-only dungeon. Normal building should work wherever terrain and hazard rules reasonably permit.

New construction can support underground identity:

- stone and mineral structural sets;
- fungal or mycelial building materials;
- hanging/suspended visual elements;
- bridge and elevator infrastructure;
- geothermal facilities;
- Blackwater docks;
- crystal lighting;
- deep fortifications.

Permanent cities below should eventually feel as legitimate as surface settlements.

## 24. Portal rules

Ordinary portals should not automatically make the initial Deep Gate irrelevant.

The intended progression is:

1. early travel between surface and Underworld requires Deep Gates;
2. players establish local infrastructure and routes below;
3. later progression unlocks a specialized cross-layer portal system such as **Deepbound Portals**;
4. those portals require significant Underworld resources and explicit world-layer handling.

Surface-local or Underworld-local portals may otherwise continue operating within their own world context according to runtime feasibility.

## 25. Map and navigation state

The Underworld must have separate exploration state from the surface.

Opening the map while below should display the Underworld map, not the surface world with an overlay. Pins and shared map data must retain layer identity so multiplayer destinations cannot become ambiguous.

Celestial orientation is absent by design, increasing the importance of roads, coastlines, pillars, lavafalls, settlements and memorable geography.

## 26. Audio and music

The world should sound enormous.

Ambient layers may include distant falling water, pressure wind, rock movement, fungal clicking, long creature calls, geothermal rumble, lava, cracking ice, unseen collapses and deep resonance.

Reverb should vary by apparent chamber size. Frozen Caverns can deliberately damp sound, while huge open Blackwater or Fracture regions carry long echoes.

Music should be comparatively sparse and allow ambience to dominate. Suitable musical language includes low drones, distant percussion, mineral/glass tones, restrained choral elements and motifs that the Great Decay can distort.

## 27. Technical subsystem layout inside Magenheim

The implementation should remain modular even while shipping inside Magenheim.

Suggested namespaces and source organization:

```text
src/
  Magenheim.Core/
    Underworld/
      UnderworldSeed.cs
      UnderworldProgressionState.cs
      UnderworldGenerationRules.cs
      UnderworldBiomeRules.cs
      UnderworldTransitionRules.cs

  Magenheim.Runtime/
    Underworld/
      UnderworldManager.cs
      UnderworldWorldContext.cs
      WorldTransitionManager.cs

      Generation/
        UnderworldGenerator.cs
        MacroBasinGenerator.cs
        WallMassField.cs
        RoofShelfGenerator.cs
        FractureField.cs
        ThermalField.cs
        MoistureField.cs
        CryogenicField.cs
        DecayField.cs
        OceanBasinField.cs
        LuminosityField.cs

      Geology/
        MegaCliffGenerator.cs
        PillarGenerator.cs
        OverhangPrefabManager.cs
        StalactiteManager.cs
        LavafallManager.cs

      Biomes/
        FungalForestRuntime.cs
        SulfurousWastesRuntime.cs
        FrozenCavernsRuntime.cs
        FractureZoneRuntime.cs
        GreatDecayRuntime.cs
        BlackwaterRuntime.cs

      Environment/
        CavernSkyController.cs
        HeightFogController.cs
        DistanceOcclusionController.cs
        UnderworldLighting.cs
        SubterraneanEventManager.cs

      Structures/
        DeepGateManager.cs
        AncientRoadGenerator.cs
        UnderworldStructureManager.cs
        StructurePlacementRules.cs

      Progression/
        UnderworldUnlockRuntime.cs
        DeepboundPortalRuntime.cs
        TraversalProgressionRuntime.cs

      Persistence/
        UnderworldSaveManager.cs
        UnderworldMapState.cs

      Multiplayer/
        UnderworldNetworkManager.cs
        WorldTransitionRpc.cs
        UnderworldStateSync.cs

      Debug/
        UnderworldDebugCommands.cs
        GenerationFieldVisualizer.cs
```

Names are planning targets rather than permission to create duplicate authority. Existing Magenheim source boundaries must be inspected before implementation and reused where applicable.

## 28. Generation order

The conceptual generation sequence is:

1. derive Underworld identity and deterministic seed from the parent world;
2. establish independent persistence namespace;
3. generate macro basin layout;
4. generate wall masses and hidden Roof Shelf geometry;
5. generate ocean basins and terrestrial elevation;
6. generate fractures and major geological corridors;
7. classify primary and hybrid biomes from field combinations;
8. apply biome terrain/material rules;
9. place major landmarks such as giant pillars and lavafalls;
10. establish environmental lighting, fog and cavern-sky illusion;
11. generate rivers, Blackwater coasts and thermal water interactions;
12. place major civilization anchors;
13. generate roads and regional structure networks;
14. populate minor structures, resources and ecological decoration;
15. establish creature population rules;
16. restore persistent world modifications and player construction;
17. synchronize multiplayer authority;
18. permit player entry only after critical initialization succeeds.

Generation failure should fail closed. A player should not be transferred into a half-initialized world whose persistence identity or terrain context is uncertain.

## 29. Performance doctrine

The heightfield-cavern approach exists partly to keep performance within a realistic Valheim modding envelope.

Required performance principles include:

- prefer native terrain and ordinary world streaming over a second continuous ceiling mesh system;
- use distant fog and occlusion to limit visible complexity;
- use LOD aggressively on giant geological prefabs;
- use GPU instancing where practical for repeated fungi and mineral assets;
- use emissive materials rather than uncontrolled dynamic lights;
- pool particles and environmental effects;
- unload unnecessary high-altitude collision/detail;
- keep large landmark silhouettes readable without requiring extreme mesh density.

## 30. Compatibility doctrine

The Underworld inherits Magenheim's additive, non-destructive compatibility rules.

Surface access points must not replace vanilla or foreign world locations simply to exist. They should use Magenheim-owned registrations, configurable spacing/exclusion rules and fail-closed collision handling.

Within the Underworld, Magenheim owns the generated world context, but external integrations should still be additive wherever possible. Future registration boundaries may allow compatible mods to add creatures, structures, resources, loot or decorations without patching the central generator.

The Underworld must not become a justification to mutate shared foreign prefabs or bypass Magenheim's established per-item and authority rules.

## 31. Debug and validation requirements

World generation of this scale requires field visualization from the beginning.

Debug tooling should be capable of exposing at minimum:

- macro basin field;
- wall-mass field;
- Roof Shelf threshold;
- fracture field;
- thermal field;
- moisture field;
- cryogenic field;
- decay field;
- ocean classification;
- luminosity field;
- major structure exclusion zones;
- Deep Gate eligibility;
- biome classification.

A local-zone regeneration/debug operation is desirable where Valheim runtime behavior makes it safe, but it must never become a second non-authoritative generation path.

## 32. Development phases

### Phase 0 — authority and feasibility

Document the Nowhere King completion flag, parent/Underworld world identity contract, persistence boundaries and multiplayer transition invariants.

### Phase 1 — empty secondary world prototype

Prove that a player can enter and leave a separate persistent derived world without corrupting the surface world. No elaborate content is required.

### Phase 2 — heightfield cavern illusion prototype

Generate one test region with:

- deep habitable basin;
- enormous wall masses;
- inaccessible Roof Shelf;
- dark cavern sky;
- distance fog/occlusion;
- no visible Yggdrasil or normal celestial presentation.

Acceptance condition: normal gameplay cannot obtain a vantage point that visually exposes the world as an ordinary open heightmap.

### Phase 3 — geological scale

Add giant pillars, fractures, ocean basins, local hanging geometry, rivers and lavafall landmarks.

### Phase 4 — atmosphere

Establish final baseline lighting, emissive ecology, subterranean events, audio and distant silhouettes.

### Phase 5 — Fungal Forest vertical slice

The Fungal Forest is the first complete biome because it exercises biome decoration, bioluminescence, ecology, resources, creatures and exploration without first requiring the harshest survival mechanics.

### Phase 6 — Blackwater

Prove underground coastal geography, sailing and long-distance subterranean navigation.

### Phase 7 — Sulfurous Wastes

Add geothermal hazards, toxic atmosphere, lava interactions and thermal resource/crafting systems.

### Phase 8 — Frozen Caverns

Add cryogenic survival, glacial terrain language, frozen structures and distinct acoustic behavior.

### Phase 9 — Fracture Zones

Add extreme vertical terrain and the first advanced traversal systems.

### Phase 10 — Great Decay

Add contamination ecology, consuming biomass, corrupted structures and late-game biological pressure.

### Phase 11 — civilization layer

Add roads, ports, settlements, bridges, foundries, vaults, mines, temples and rare Lost Cities.

### Phase 12 — progression layer

Complete environmental adaptation, traversal progression, specialized crafting and Deepbound Portal technology.

### Phase 13 — major encounters and deeper narrative

Introduce major Underworld progression encounters and develop the mystery of the Great Decay and whatever deeper systems lie beneath it.

### Phase 14 — expansion-scale validation

Perform long-distance traversal, save/load, world transition, multiplayer, persistence, performance, buildability, structure-density and compatibility testing.

## 33. First playable milestone

The first meaningful build is intentionally small.

It requires:

- Nowhere King unlock recognition;
- one Deep Gate;
- separate derived world identity;
- persistent Underworld state;
- deterministic seed derivation;
- one giant basin surrounded by wall masses;
- inaccessible Roof Shelf;
- cavern-sky and fog illusion;
- one crude Fungal Forest area;
- one return transition;
- multiplayer-safe transition authority.

It does **not** require five finished biomes, cities, bosses or a full item tree.

The purpose is to prove the architecture before creating a content mountain.

## 34. Second playable milestone

The second milestone proves that the world is worth exploring.

Target content:

- developed Fungal Forest;
- Blackwater shoreline and navigable water;
- giant pillars;
- at least one major fracture;
- basic lavafall generation;
- initial Underworld resources;
- several creature families;
- one ancient road network;
- one settlement family;
- one large ruin;
- primitive Underworld crafting and environmental adaptation.

A player should be able to spend several hours below without the world immediately reading as an empty prototype.

## 35. Authoritative design invariants

These are the current non-negotiable design rules unless explicitly revised later:

1. **The Underworld is a world, not a dungeon.**
2. **It unlocks after the Nowhere King.**
3. **For the foreseeable future it is developed and shipped inside Magenheim, not in a second repository.**
4. **Magenheim remains the authority for Crystal Shaping, elemental systems and shared progression.**
5. **The world target is a separate persistent derived map/world context.**
6. **There is no conventional sun, moon, sky or visible Yggdrasil branch.**
7. **The design does not depend on a continuous true cavern ceiling.**
8. **Cavern enclosure is produced primarily through extreme heightfield basins, wall masses, inaccessible Roof Shelf plateaus, darkness, fog and local hanging geometry.**
9. **Normal gameplay must not expose the high plateau trick from an ordinary vantage point.**
10. **Biomes must differ mechanically as well as visually.**
11. **The world supports long-term exploration, sailing, building and settlement.**
12. **Underworld progression extends surface/Magenheim investment rather than invalidating it.**
13. **World generation and compatibility remain additive and non-destructive.**
14. **Large-scale geology and bioluminescent ecology are defining visual identities.**
15. **The world should repeatedly create spaces whose apparent scale makes the player feel small.**

## 36. Long-term vision

A mature Magenheim save should eventually contain two civilizations belonging to the same player.

Above: farms, halls, ports, castles and familiar Valheim skies.

Below: Blackwater ports, geothermal workshops, fungal settlements, bridges over fractures, ancient roads, crystal-lit cities and settlements surrounded by geological walls that disappear upward into darkness.

The player should eventually travel confidently between both worlds, yet The Underworld must retain enough scale and darkness that even experienced players occasionally reach a new ridge or coastline, see blue forests and red lavafalls stretching into the distance, and remember that all of it was supposedly beneath their feet the entire time.
