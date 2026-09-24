# Underworld custom ecology — slice ledger

Execution state for replacing the Underworld's vanilla ecology donors with authored Magenheim
models, biome by biome (user directive 2026-09-22; plan: `UNDERWORLD_FLORA_TERRAIN_PLAN.md`).
Each slice ends in a pushed commit. **Resume at the first unchecked slice.** Record the commit SHA
when a slice closes. Static validation only unless a slice says "observed in world".

Decisions in force: no regrowth; Motherbloom deferred to F9 (plan section 10, decisions 1 and 6).

## Terrain direction — user correction 2026-09-23

Occasional cavern landforms must be **sheer cliffs, needle spires and monstrously tall plateaus
rising for miles**, visibly distinct from rounded mountainous terrain. Keep ordinary relief in
other areas and preserve the walkable arrival basin. This direction takes priority over the next
ecological authoring slice.

- [x] Implement deterministic 3.2–5.6 km monuments, enlarged vertical domain, distant terrain and
  near/far boundary stitching, longer clear-air visibility and vertical cliff texture projection.
- [x] Inspect Core-derived spire/plateau geometry at detail and horizon sampling; broaden plateau
  crowns, break regular support planes and cut irregular buttresses/recesses into cliff boundaries.
- [x] 0.0.103 checkpoint installed and pushed (`41a02be`, records `f6439f2`).
- [x] User confirmed the Underworld haze below the lava/fungal roof as the height reference.
- [x] Implement 0.0.104: native haze at 4800m, absolute terrain cap 5100m; shared authored
  sunless panorama; dim existing-clock light/sky/fog cycle; dungeon-height classification
  corrected only for the admitted native instance.
- [ ] Finish 0.0.104 closeout, installation and push.
- [ ] Live world acceptance: cliff silhouettes, transition seams, skyline/fog, camera restoration,
  performance, plateau collision and biome location placement. Offline geometry renders are not
  evidence of live gameplay acceptance.

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

- [x] **B1 — shared flora kit + author tool.** DONE (commit below). 16 sources authored in `assets/models/source/underworld-{flora-blackwater,resource}-*.blend`, exported and wired in B2. **Historical stale state before B2:** `verify-generated-freshness.py` flags `underworld-fungal-forest-models` because its tool now imports `magenheim_flora_kit.py`; B2's regeneration clears it.
  Original scope: Move the fungal tool's `Model`, `tube`, `cap`, `gills`,
  `buttresses`, `small_mushrooms` into `tools/magenheim_flora_kit.py`; author Blackwater models:
  canopy flowstone spire, broken column, rimstone mound, drowned root arch; cover brine fern,
  palefinger cluster, pearl caps, wet stone, fingerstone rubble, root fan, lakebed shelf, root
  fingers; resources flowstone chunk, pale fibre, pearl, deep salt. Review render; commit unwired.
- [x] **B1b — quality rework (user review 2026-09-22: FAILED except palefinger and brine fern).**
  The bar is the Fungal Forest canopy sheet, not "a lathe". Render every model with the scratch
  review sheet (front / side / 3/4 / silhouette) and get user sign-off before B2.
  **Codex continuation from Claude's `d6dbbe5`:** geometry reworked and a repeatable four-view
  renderer added (`tools/render-blackwater-review.py`). All 16 models are included across
  `artifacts/review/blackwater/blackwater-review-01.png` through `04.png`. A Blender 5 opaque
  bake-gutter defect was also reproduced on the pearl and fixed in the reusable flora kit with
  UV-derived coverage before texture reduction. Source verification:
  `tools/blender.ps1 verify-blackwater-sources`. See
  `validation/2026-09-22-blackwater-quality-rework.md` for changes, evidence and remaining gates.
  **User authorized continuing to B2 and closeout on 2026-09-22. Review candidate: `fbf4bca`.**
  The bullets below preserve the incoming failure report, not the current geometry:
  - flowstone-spire: reworked (fused fluted columns, deposit rings, pool, stalagmites, 4.1k tris) --
    reads better; needs user verdict.
  - broken-column: reworked but still failing: flutes too shallow to read (raise `fluted` depth
    0.07 -> ~0.18, fewer sides-per-flute aliasing), capital stands on edge like a wheel (lay it on its
    side, tilted into the silt), silt disc floats (sink it: z ~0.02, flatter), add cracks/chips on shaft.
  - rimstone-mound: stepped wedding cake. Needs irregular non-circular terraces (noise the ring
    radius per angle), scalloped pool lips, water-filled pools (flat 'silt' or new wet surface), and
    a flowstone curtain down one side.
  - drowned-root-arch: three plain tubes. Needs root taper with knuckles, secondary rootlets, bark
    ridges (fluted with low depth), hanging pale growths, silt at feet.
  - wet-stone, fingerstone-rubble, lakebed-shelf: single low-poly blobs -- rebuild with layered
    shelf strata, chips, and flowstone crust; aim 600-1500 tris each.
  - pearl-caps, root-fan, root-fingers and the four resources: not reviewed individually; review
    them on the sheet before B2.
- [x] **B2 — wire and ship.** Catalog, resource visuals, manifest, count pin, regenerate (Blackwater
  first, then fungal for the kit move), closeout, commit, push.
  **Complete: 0.0.102 installed and hash/catalog verified 2026-09-23 (`c3dc7e0`).** Evidence:
  `validation/2026-09-23-blackwater-closeout.md`. Static closeout passed; not observed in world.

## Later biomes (not started)

Sulfurous Wastes, Frozen Caverns, Fracture Zones, Great Decay — same pattern, one slice pair each.
