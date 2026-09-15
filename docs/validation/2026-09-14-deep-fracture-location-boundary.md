# Deep Fracture additive Jötunn location boundary — 2026-09-14

## Scope

This record covers the additive surface-location boundary for Deep Fractures. It adds pure-core location placement authority, host-collision planning, Jötunn `LocationConfig` translation, a generated geological-fracture surface shell, and a registrar that can add the Magenheim-owned entrance only after a real dungeon interior binder has attached.

This slice does **not** activate Deep Fracture locations from `MagenheimPlugin.Awake`. That is deliberate: the interior `DungeonGenerator`/room implementation does not yet exist, and registering live fracture mouths before it exists would knowingly inject unusable entrances into player worlds.

## Pure-core location authority

`DeepFractureLocationCatalog.SurfaceFractureEntrance` defines the current surface placement intent:

- registration key `magenheim.location.deep_fracture_entrance`;
- prefab identity `Magenheim_DeepFracture_Entrance`;
- group `magenheim.deep_fracture`;
- all eight designed land biomes: Meadows, Black Forest, Swamp, Mountain, Plains, Mistlands, Ashlands, and Deep North;
- normalized `SpawnArea.All` so median and edge areas are both valid;
- quantity 24;
- 12m exterior radius;
- minimum altitude 1m;
- maximum terrain delta 28m;
- 1024m minimum distance from similar fracture entrances;
- random rotation;
- no prioritized placement and no broad vegetation-clearing surface footprint.

These values are validated before runtime translation. Placement remains a Magenheim-owned definition rather than a second set of Jötunn-only constants.

## Additive collision planning

`DeepFractureLocationRegistrationPlanner` accepts desired Magenheim location definitions and a read-only snapshot of observed host location identities. It returns only Add, Skip, or Error decisions.

An occupied target prefab identity is skipped with a diagnostic. Unrelated vanilla or foreign locations do not block the Magenheim identity. Foreign registration namespaces, foreign prefab identities, duplicate desired keys/prefabs, duplicate biome configuration, malformed areas, and invalid numeric placement values fail before mutation. The planner never removes, renames, edits, reorders, or disables an observed host location.

## Jötunn 2.30.0 adapter

The runtime project is pinned to `JotunnLib` 2.30.0. `JotunnDeepFractureLocationAdapter` translates the validated core definition into Jötunn's current location surface:

- `ZoneManager.AnyBiomeOf(...)` for multi-biome placement;
- `JotunnWorldgenAdapter.MapArea(...)` for the existing fail-closed runtime biome-area semantics;
- `LocationConfig` for quantity, radii, terrain filters, grouping, clear-area behavior, rotation, and interior metadata;
- `ZoneManager.GetZoneLocation(...)` plus `CustomLocation.IsCustomLocation(...)` for read-only exact-identity occupancy observation after vanilla locations are available.

The registrar subscribes to `ZoneManager.OnVanillaLocationsAvailable`, because that lifecycle point provides the host location table needed for non-destructive preflight. It performs a second exact-identity check immediately before mutation and refuses registration if the identity became occupied after planning.

## Geological-fracture surface shell

`DeepFractureEntranceVisuals` creates a compact Magenheim-owned fracture-mouth shell inside a Jötunn location container. It uses the loaded vanilla `Rock_4` material as a read-only visual source, clones that material for Magenheim-owned stone/shadow/crystal-seam rendering, and does not modify the vanilla prefab or its shared material.

The current shell contains asymmetric fractured side masses, an overhang, talus, a dark recessed mouth, two restrained crystal seams, and a named `Magenheim_DeepFracture_InteriorAnchor`. The anchor is the authoritative attachment boundary for the later dungeon interior implementation.

## Interior admission gate

`DeepFractureLocationRegistrar` requires an `IDeepFractureInteriorBinder` in its constructor. The binder must attach a real interior and return a validated `DeepFractureInteriorBinding` with a positive interior radius and environment identity. The registrar refuses to add the location if the binder reports no interior, if the authoritative entrance anchor is missing/replaced, if the location identity is occupied, or if Jötunn rejects the additive registration.

This is intentionally stronger than registering a decorative cave and promising to fill it later: there is no code path from the current plugin bootstrap that can seed dead Deep Fracture entrances into a world.

## Deterministic coverage

`DeepFractureLocationRegistrationTests` is wired into `DefinitionAuthorityTests.Run()`. Source coverage validates canonical identity, all-eight-biome coverage, additive admission, occupied-identity Skip behavior, observation immutability, unrelated-foreign coexistence, duplicate desired identity rejection, namespace ownership, prefab ownership, and duplicate-biome rejection.

## Runtime API verification

The implementation was checked against the Jötunn 2.30.0 location API surface used by the current project: `CustomLocation`, `LocationConfig`, `ZoneManager.AnyBiomeOf`, `ZoneManager.GetZoneLocation`, `ZoneManager.AddCustomLocation`, `ZoneManager.CreateLocationContainer`, and `ZoneManager.OnVanillaLocationsAvailable`.

The current connector execution environment does not expose the local Valheim managed assemblies or a C# compiler, so this remains **static-source admission only**. No compile, plugin-startup, world-generation, placement, multiplayer, persistence, or fresh-world acceptance is claimed.

## Next exact action

Implement the concrete Deep Fracture interior binder and Magenheim-owned Jötunn room set/DungeonGenerator binding. That implementation must consume the existing pure-core district graph and encounter plan rather than rerolling layout or enemies in the runtime layer. Only after the interior binder passes compile/static validation should `DeepFractureLocationRegistrar` be instantiated from `MagenheimPlugin.Awake` and admitted to fresh-world runtime testing.
