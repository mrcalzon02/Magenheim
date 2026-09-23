# Underworld custom ecology — slice ledger

Execution state for replacing the Underworld's vanilla ecology donors with authored Magenheim
models, biome by biome (user directive 2026-09-22; plan: `UNDERWORLD_FLORA_TERRAIN_PLAN.md`).
Each slice ends in a pushed commit. **Resume at the first unchecked slice.** Record the commit SHA
when a slice closes. Static validation only unless a slice says "observed in world".

Decisions in force: no regrowth; Motherbloom deferred to F9 (plan section 10, decisions 1 and 6).

## Pattern every biome follows

1. `tools/author-underworld-<biome>.py` authors canopy, ground cover and resource models at real
   size (shared helpers: `tools/magenheim_blender_kit.py`, `tools/magenheim_flora_kit.py`).
2. `tools/rebuild-underworld-<biome>.ps1` + manifest entry `underworld-<biome>-models` (inputs: both kits).
3. Catalog slots in `UnderworldVanillaDonorCatalog.cs` switch one-for-one to `model:<id>` (landmark
   selection is by index; never add or remove slots).
4. Resource prefabs added to `UnderworldResourceVisuals.Models`; icons via `underworld-resource-icons`.
5. Raise the `verify-model-assets.py` count pin; regenerate through the manifest, **the new biome's
   entry first** (every rebuild runs the pinned count).
6. `closeout.ps1 -Offline`, records, commit, push.

## Fungal Forest — done, 0.0.101 (`cd955dc`)

- [x] 16 models, catalog, resources, icons, dev commands. Static only; not observed in world.

## Blackwater Deep

Donors today: canopy `cliff_mistlands1`, `RockFinger`, `RockThumb`, `YggdrasilRoot` (7 slots);
cover `Fiddleheadfern`, `shrub_2`, `Pickable_Mushroom_blue`, `Rock_4`, `RockFingerBroken`,
`YggdrasilRoot`, `root08`, `root11` (12 slots); resources Flowstone, Pale Fibre, Blackwater Pearl,
Deep Salt.

- [ ] **B1 — shared flora kit + author tool.** Move the fungal tool's `Model`, `tube`, `cap`, `gills`,
  `buttresses`, `small_mushrooms` into `tools/magenheim_flora_kit.py`; author Blackwater models:
  canopy flowstone spire, broken column, rimstone mound, drowned root arch; cover brine fern,
  palefinger cluster, pearl caps, wet stone, fingerstone rubble, root fan, lakebed shelf, root
  fingers; resources flowstone chunk, pale fibre, pearl, deep salt. Review render; commit unwired.
- [ ] **B2 — wire and ship.** Catalog, resource visuals, manifest, count pin, regenerate (Blackwater
  first, then fungal for the kit move), closeout, commit, push.

## Later biomes (not started)

Sulfurous Wastes, Frozen Caverns, Fracture Zones, Great Decay — same pattern, one slice pair each.
