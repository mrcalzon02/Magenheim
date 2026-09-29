# Underworld Biome Dungeon Program

**Status:** ACTIVE STRUCTURE PROGRAM — Deep Fracture admitted; Rootwarren, Drowned Vaults, Cinderworks and Rime Sepulcher source/runtime architecture complete but asset-gated; Carrion Catacombs remains.

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

## Fungal Forest — Rootwarren

**Runtime status: PREFORGE SOURCE COMPLETE; catalog remains PLANNED until the full asset family is forged and reviewed.**

Rootwarren is a living geological fracture swallowed by fungal/root growth. Its source authority is
now concrete: sixteen large room families plus one reusable passage, each with stable model identity,
role and dimensions. The generic planner uses every room family two or three times, pins Fracture
Mouth as room zero, creates branching/looping topology, embeds rooms on a non-overlapping spatial
grid, and routes physical corridors around unrelated occupied room cells.

The runtime side is also present before admission: authored room/theme registration, a reduced
Fracture Mouth exterior, buried same-instance interior anchor, Fungal environment selection,
walkable routed passages, deterministic server-owned Fungal encounters, one-shot native resource
pickups with harvest persistence, and an exact entrance-room return path. Normal worldgen remains
disabled because the catalog status is still Planned.

The r2 Blender production author builds collidable cavern shells for all sixteen chambers and a
collidable passage shell, with dedicated source verification, render/contact-sheet review, generated
asset provenance recording and a narrow promotion script. Promotion is fail-closed until exactly
17 source, 17 GLB and 17 runtime payloads exist.

Gameplay language remains visibility, spores, living cover, vertical root paths, fungal resources
and ambush fauna. It must read as a fungal cave ecology, not another stone crypt painted green.

## Blackwater Deep — working dungeon program: Drowned Vaults

**Runtime status: SOURCE COMPLETE / ASSET-GATED; catalog remains PLANNED until the production forge admits all 17 payloads.**

A partially flooded ruin/cavern network with dry shelves, submerged passages, air pockets,
collapsed docks and deep chambers. Water level is structural gameplay, not decoration.

The kit needs both walkable and flooded route alternatives so the Deep Current boon/equipment tier
has real dungeon value. Entrances should read as flooded sinkholes or fractured shore vaults.

## Sulfurous Wastes — working dungeon program: Cinderworks

**Runtime status: SOURCE COMPLETE / ASSET-GATED; catalog remains PLANNED until the production forge admits all 17 payloads.**

A geothermal ruin/industrial-sacral complex built around vents, slag channels and failed furnace
infrastructure. Rooms should use height, heat exposure and vent timing rather than generic lava
floors.

The Furnace Blood/Emberiron tier must materially change how aggressively players can route through
hot chambers.

## Frozen Caverns — working dungeon program: Rime Sepulcher

**Runtime status: PLANNED; source/runtime architecture complete, forge pending.**

The sixteen-family kit now owns Shelter, ClearGallery, FrostField, WhiteoutChoke and IceShear route
states. Rooms use real authored dimensions and the generic topology/spatial planners; adaptive
passage modules physically connect the embedded layout rather than substituting teleports.

Room and passage exposure reuse the existing Frozen Caverns atmosphere authority. Shelter and clear
routes can suppress local obscuration, while FrostField/IceShear/Whiteout routes raise the ordinary
hazard floor and reuse Deep Fog or Whiteout. Rimebound/Rimeward therefore changes route pressure
without creating a second dungeon-only cold meter.

Encounter dressing is role-aware: Frost Tick, Rime Moth, Iceblind, Pale Burrower, Rimewing,
Glacier Stalker and Cryolith Guardian are used only in pressure/encounter/final spaces that call for
them. Resource rooms use the canonical Rimewood, Clear Ice and Rimesilver pickup vocabulary with
server-owned harvest persistence.

The buried interior, physical passage assembly, exact same-instance return path and Frozen-only
location registrar are implemented, but registration stays fail-closed while the 17 authored
room/passage payloads are absent.

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
3. Blackwater Drowned Vaults 16-room + adaptive-passage forge/admission.
4. Sulfur Cinderworks 16-room + adaptive-passage forge/admission.
5. Frozen Rime Sepulcher forge/admission.
6. Great Decay Carrion Catacombs after contamination/Censer live tuning.

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
