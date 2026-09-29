# Underworld Deep Fracture admission and biome-dungeon authority

Date: 2026-09-29

## Scope

This slice moves Magenheim from exterior Underworld landmarks into dungeon-scale structure work.
It admits the existing Deep Fracture expedition as a sparse Fracture Zones dungeon and establishes
the production/topology authority for five additional biome-specific dungeon programs.

## Deep Fracture Underworld admission

`UnderworldDeepFractureLocationRegistrar` registers the Magenheim-owned
`Magenheim_Underworld_Dungeon_DeepFracture` location on the reserved Fracture Zones biome bit.
The catalog target is six locations with 1.4 km minimum similar-site spacing.

The new entrance reuses:

- `DeepFractureEntranceVisuals`;
- `DeepFractureInteriorBinder`;
- the exact-plan 20-district expedition;
- passage assembly and traversal portals;
- encounter/crystal/persistence authority.

No second dungeon generator was created.

`DeepFractureSurfaceTravel` is now entrance-context aware. Its historical class/method names remain
for compatibility, but travel itself does not switch Underworld layers. Surface entrances label
their return as Surface Fracture; Underworld entrances label it Fracture Zones.

## Catalog isolation

The Underworld location prefab uses the `Magenheim_Underworld_` ownership prefix and the reserved
Fracture Zones biome flag. Existing `UnderworldZoneCatalogGuard` therefore requires the detached
Underworld ZoneSystem to receive the row before removing it from Surface generation.

`UnderworldWorldgenContentBridge.ValidateUnderworldCatalog` now additionally verifies every
`RuntimeReady` dungeon definition survived partitioning. Missing runtime-ready dungeons fail
Underworld startup closed.

## Generic biome dungeon authority

`UnderworldDungeonCatalog` defines exactly one dungeon program per canonical Underworld biome.
Deep Fracture is the only `RuntimeReady` entry. Five future programs remain `Planned`, which is a
safety boundary: planned entries are production authority but cannot create dead entrances.

Ordinary biome dungeon production contract:

- 15-20 large authored room families;
- each family used 2-3 times per generated dungeon;
- roughly 30-60 large room placements before special connectors/chambers;
- deterministic branching topology;
- bounded cross-links/loops;
- real interior binder required before runtime admission.

`UnderworldBiomeDungeonPlanner` implements that deterministic topology and explicitly rejects Deep
Fracture so the established megadungeon cannot be silently replaced by the generic planner.

## First ordinary room kit

`UnderworldFungalRootwarrenCatalog` now fixes sixteen Fungal Rootwarren room/model identities with
explicit entrance, main-route, junction, vertical, hazard, resource, encounter and landmark roles
and large target dimensions. The generic planner consumes that manifest in source tests. This is
an asset-production contract only; Rootwarren remains Planned and cannot register an entrance until
the authored Blender/runtime payloads and interior binder exist.
## Current production sequence

1. Live-accept Underworld Deep Fracture.
2. Fungal Rootwarren.
3. Blackwater Drowned Vaults.
4. Sulfur Cinderworks and Frozen Rime Sepulcher.
5. Great Decay Carrion Catacombs.

## Verification boundary

Core source tests are wired for the catalog and generic topology rules. This execution environment
does not establish a local Valheim compile, package install or live world run. 0.0.155 therefore
remains a source candidate until the project build/closeout and the TESTING.md 0.0.155 matrix pass.
