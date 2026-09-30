# Deep Dungeon Expansion — content source completion checkpoint

**Date:** 2026-09-30  
**Authority:** `docs/DEEP_DUNGEON_EXPANSION.md`

## Ordinary dungeon architecture

All five ordinary expanded-vanilla dungeon families remain RuntimeReady together while preserving
their recognizable vanilla donor grammar. Deep Fracture remains the separate bespoke dungeon lane.

The generic donor-room path now applies owning-biome customization rather than only scaling and
population replacement:

- biome-specific architecture material/palette treatment;
- collisionless wall/ceiling structural overlays with connection clearance;
- authored/stripped biome edge and ground dressing;
- local biome lighting;
- cloned donor doors and entrances with matching biome treatment;
- real native Blackwater water pools in suitable deep/flat rooms;
- bounded Sulfur geothermal pressure pockets;
- Frozen rooms bound to the established Rime exposure/Rimeward mitigation authority;
- Great Decay rooms bound to the established Carrion contamination/Defiant/Censer authority.

The structural overlays use the normal biome visual vocabulary and do not reintroduce the retired
bespoke ordinary dungeon room kits.

## Ordinary dungeon fauna

The five ordinary biomes contain **35 fauna entries** and all 35 now have authored custom-body model
identities wired through `UnderworldCreaturePrototypes.AuthoredModelId`.

The existing Underworld production run contains exactly 35 ordinary creature runtime model IDs.

The shared rigid-segment creature path:

- preserves authored bone rotation, translation and scale animation;
- leaves donor AI, hitboxes, attacks, persistence and networking as the gameplay chassis;
- disables donor MeshRenderer/SkinnedMeshRenderer silhouettes when an authored runtime payload is
  available;
- compensates inherited donor chassis scale so authored models render at source-authored metre scale;
- falls back to the donor/retexture path only when the authored runtime payload does not yet exist.

## Species mechanics implemented in source

The ordinary-biome identity layer now includes, among other donor-derived combat:

- Mycelial Stalker concealed proximity pounce;
- Shelf Lurker wall-cling staging and drop pounce;
- Puffback reactive spore burst;
- Cave Ray hit-triggered flee response;
- Blackwater Lamprey timed attach/feed latch;
- Lantern Angler lure pulse, pressure tell and radial pressure release;
- Abyss Shellback armor with pickaxe counter;
- Deep Hunter apex ram and close tail strike;
- Cinder Hound nearby pack rally/call;
- Vent Spitter thermal retaliation tied to pressure-body presentation;
- Furnace Golem two-stage degrading armor with vent/break presentation;
- hearing-led Iceblind perception;
- Pale Burrower subterranean proximity emergence;
- Rimewing dormant perch-to-flight release;
- rooted Carrion Bloom behavior;
- rooted Corpse Orchard Rotling propagation.

Seeker-derived species intended to be terrestrial are explicitly grounded rather than inheriting
Seeker flight.

## Source integrity readback

Authoritative `main` readback immediately before this checkpoint confirmed:

- ordinary fauna entries: **35**;
- authored creature model mappings: **35**;
- unique ordinary creature IDs in the production run: **35**;
- no stale source/docs language claiming the implemented wall-cling, burrow, perch, armor-break,
  pack-rally, lure or Deep Hunter follow-up mechanics are still pending;
- no forbidden C# tuple syntax or `System.ValueTuple` forms in the changed runtime files;
- structural overlays, Blackwater water, Frozen exposure and Great Decay contamination are present;
- donor silhouette disabling and authored visual scale compensation are present.

## Execution boundary

This checkpoint is **source completion**, not generated-asset or installed-game acceptance.

This execution environment has no Blender executable. Direct provisioning of the pinned Blender
5.0.0 Linux archive was attempted from Blender's official release host, but the container cannot
resolve external DNS. GitHub Actions were not used.

Therefore this checkpoint does **not** claim:

- execution of the Blender 5.0 creature production run;
- generation/readback of the newly authored runtime model payloads;
- local .NET/Valheim compile success;
- installed-Valheim dungeon generation or visual inspection;
- navigation/AI/multiplayer/save-reload acceptance.

The next executable production step is the existing Underworld production run with Blender 5.0,
followed by the installed-Valheim DDE acceptance sequence. Any failure there is a defect to repair
in the authoritative source; it is not a reason to restore one-at-a-time candidate gating or donor
silhouette fallback as the intended final state.
