# Fungal Forest custom pass and Underworld dev commands — 2026-09-22

## Reconciled authority

`main` at `126551b` (0.0.100 pushed). Design authority: `docs/UNDERWORLD_FLORA_TERRAIN_PLAN.md`
sections 3.1, 4.1, 5 and 7.1. User directive: replace the Underworld's vanilla ecology donors with
custom variants, Fungal Forest first; confirm the developer commands for reaching the Underworld
work and are documented.

Open plan decisions taken conservatively and recorded here, not assumed elsewhere: **no regrowth**
(vanilla parity; section 10 decision 1) and **the Motherbloom stays deferred** to the F9 landmark
stage (decision 6). Nothing in this pass depends on either.

## What was built

`tools/author-underworld-fungal-forest.py` authors sixteen models at real size, registered in
`assets/generated.manifest.json` as `underworld-fungal-forest-models`:

| role | models | replaces |
|---|---|---|
| canopy | Glowcap (7.3m), Spirestalk (11.9m), Puffcap (6.1m), Tanglecap (6.6m) | YggaShoot1/2/3, Magecap, JotunPuffs and blue mushroom at 5-12x |
| ground | jade fern, sporebrush, amber cap bed, lantern caps, nursery caps, moss stone, root skirt, Glowcap sapling | Fiddleheadfern, shrub_2, Rock_4, root08, small shoots and mushrooms |
| resources | Worldroot Timber, Glowcap Flesh, Spire Fibre, Understone | RoundLog, MushroomMagecap, Flax, Stone items; Pickable_Branch/Mushroom/Stone pickups |

- **Catalog.** `UnderworldVanillaDonorCatalog` entries may now name `model:<id>`; the visual factory
  builds those through `ModelAssets` with the model's own authored colliders (only tree stalks
  collide). The eleven canopy and twelve ground-cover Fungal Forest slots were mapped one-for-one,
  so index-based landmark selection (the Split Pillar) is unchanged and now shows custom trees.
  Authored models are not tinted.
- **Resources.** `UnderworldResourceVisuals` gives the four Fungal Forest items and their pickups the
  authored body, refits the solid collider to it (a timber bundle must not keep a two-metre RoundLog
  capsule), sets the pickup's `m_hideWhenPicked`, and assigns an icon rendered from the model by
  `tools/render-underworld-resource-icons.py`. Item identities, stacks and pickup behaviour are unchanged.
- **Kit.** `blob`, `spike`, `plate`, `arc`, `transformed` and the spec-driven painter moved into
  `tools/magenheim_blender_kit.py`; the painter gained an optional grain `stretch`, and `bake_atlas`
  an optional shipped size (trees ship their full 1024px bake). Weapons and Surtlings were
  regenerated through the changed kit.
- Bioluminescence is emissive gills, caps and bulbs; no per-plant lights.

## Developer commands

No console command had ever existed in the repository (`git log -S ConsoleCommand` is empty), so
this is new rather than a refactor. `magenheim_underworld enter|return|status` is a cheat command
(`devcommands`) that calls `UnderworldGateTransitRuntime` -- the Deep Gate's own transit -- so layer
switching, return anchors and logging are identical to the gate. `enter` skips the progression
unlock; `return` falls back to the bed or home point when no anchor exists. Documented in
`TESTING.md`.

## Measurements

Canopy: Glowcap 7.3m / 2.8k triangles, Spirestalk 11.9m / 4.7k, Puffcap 6.1m / 2.0k, Tanglecap
6.6m / 5.0k, each with a 1024px atlas. Ground cover 0.2-1.8m, 0.9k-2.8k triangles, 512px. Resources
0.04k-0.9k triangles. `verify-model-assets` caught a genuine defect during the pass: 12 fern faces
folded inside-out where fronds curl back down (the sweep's automatic reference axis flipped
mid-curl); `spike()` gained an explicit `up`, and each frond uses the normal of the plane it curls in.

## Verification boundary

`closeout.ps1 -Offline` passed and installed 0.0.101 (DLL `B5877CA9...DEDCBA`): 328 model sets,
315 generated files across 12 generators, 10 held-model alignments, launcher entry verified.

Closeout first failed on `UnderworldFractureCliffMonasteryFamily.cs` from concurrently landed commits
`3bb7d84`/`14298bf` (`Vector3 *= Vector3` does not exist in Unity); repaired with `Vector3.Scale`,
the evident intent. The same concurrent checkout rewrote the Blender kit with CRLF line endings
mid-run, which the freshness gate correctly flagged; endings were restored and the Surtling entry
re-recorded.

**Static only. Not observed:** the Fungal Forest in a world (placement, scale, collision, emissive read
under the cavern skybox), resource items and pickups in the hand and on the ground, and all three
dev commands. Each needs a loaded world.
