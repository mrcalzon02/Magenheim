# Underworld Biome Dungeon Program

**Status:** ACTIVE STRUCTURE PROGRAM — Deep Fracture admitted; five biome dungeon kits pending authored interiors.

The Underworld is a world-scale realm, not a chain of dungeons. Dungeons therefore function as
major local destinations inside its biomes rather than replacing biome exploration.

## Shared production rule

Every ordinary biome dungeon is authored from **15–20 large themed room families**. A generated run
uses each family **2–3 times**, producing roughly 30–60 large room placements before optional
transitions, shafts, connectors and special chambers. Reuse must alter orientation, branch context,
damage state, dressing, enemies/resources and approach so repeated geometry does not read as the
same room pasted down a corridor.

UnderworldBiomeDungeonPlanner owns deterministic topology for these ordinary biome dungeons.
It produces a connected branching tree plus bounded cross-links/loops. It deliberately does not own
geometry, encounters, loot or persistence.

A dungeon entrance is never admitted merely because exterior art exists. Runtime registration
requires a real interior binder. Dead decorative cave mouths are prohibited.

## Fracture Zones — Deep Fracture

**Runtime status: ADMITTED.**

The established Deep Fracture remains its bespoke megadungeon rather than being converted to the
generic reusable-kit algorithm. It keeps the existing exact-plan 20-district expedition, encounter
planner, passage assembler, traversal links, return portal and persistence authority.

Two entrance contexts now use that same expedition:

- the existing Surface geological fractures;
- six sparse Underworld Fracture Zones entrances, minimum 1.4 km similar-site spacing.

The Underworld entrance does not change world layer. Its return portal resolves back to the
Fracture Zones entrance that owns that expedition.

## Fungal Forest — working dungeon program: Rootwarren

**Runtime status: PLANNED; no entrance may spawn yet.**

A living geological fracture swallowed by fungal/root growth. The room kit should emphasize broad
root-vault halls, fungal shelves, spore basins, collapsed ancient masonry and vertical root shafts.
It should be the first new 15–20-piece kit because Fungal Forest is the first progression biome.

Gameplay language: visibility, spores, living cover, vertical root paths, fungal resources and
ambush fauna. Avoid making it another stone crypt painted green.

## Blackwater Deep — working dungeon program: Drowned Vaults

**Runtime status: PLANNED; no entrance may spawn yet.**

A partially flooded ruin/cavern network with dry shelves, submerged passages, air pockets,
collapsed docks and deep chambers. Water level is structural gameplay, not decoration.

The kit needs both walkable and flooded route alternatives so the Deep Current boon/equipment tier
has real dungeon value. Entrances should read as flooded sinkholes or fractured shore vaults.

## Sulfurous Wastes — working dungeon program: Cinderworks

**Runtime status: PLANNED; no entrance may spawn yet.**

A geothermal ruin/industrial-sacral complex built around vents, slag channels and failed furnace
infrastructure. Rooms should use height, heat exposure and vent timing rather than generic lava
floors.

The Furnace Blood/Emberiron tier must materially change how aggressively players can route through
hot chambers.

## Frozen Caverns — working dungeon program: Rime Sepulcher

**Runtime status: PLANNED; no entrance may spawn yet.**

A frozen cavern/ruin system with ice fins, pressure cracks, buried chambers and still-air vaults.
The layout should preserve long cold sightlines in some rooms and tight whiteout-prone transitions
in others.

Rimebound/Rimeward mitigation and cold-weather readability should matter without turning every
room into a constant damage field.

## Great Decay — working dungeon program: Carrion Catacombs

**Runtime status: PLANNED; no entrance may spawn yet.**

An ancient underground complex being biologically consumed: root ingress, collapsed burial spaces,
amber growth, bone deposits and contaminated chambers. The dungeon should visibly transition from
recognizable construction into living/rotting occupation deeper in the run.

Defiant Flesh, Defiant armour and the Defiant Censer should create route and safe-work-area
advantages rather than merely reducing a number in the HUD.

## Admission sequence

1. Deep Fracture Underworld placement and live acceptance.
2. Fungal Rootwarren 15–20 room-family kit + entrance/interior binder.
3. Blackwater Drowned Vaults kit.
4. Sulfur Cinderworks and Frozen Rime Sepulcher in parallel after their environmental interactions
   are stable.
5. Great Decay Carrion Catacombs after contamination/Censer live tuning.

Each promotion from PLANNED to RuntimeReady must include:

- authored room models and collision;
- deterministic room-family catalog;
- entrance model integrated with terrain;
- interior binder and return path;
- encounter/resource population authority;
- native location registration in only its owning Underworld biome;
- startup catalog validation;
- host/client, save/reload and return-path acceptance.

The catalog status itself is a safety gate: planned dungeons exist as production authority but are
not allowed to seed empty entrances into worlds.
