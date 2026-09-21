# Editable Magenheim model library

The editable model library now includes the established model set plus the eleven Rootforged A0 Underworld construction sources with matching GLB and runtime payloads. These are real mesh assets with UVs, named editable parts, and material slots. Runtime geometry comes from checked-in mesh data; the game does not construct stand-in art from Unity primitives.

The migration preserves the existing designs as editable starting models. It is not a claim that their artwork is finished: most creatures are still rigid forms, many shapes remain simple, and there is no new animation rig or LOD system. The Nowhere King still uses the game's humanoid body and animations with an owned crown attachment. Inherited vanilla projectiles, effects, and character chassis remain game assets.

## Quality revision 0.0.51

All 281 saved assets received a surface/material review pass. See [the review report](QUALITY_REVIEW.md) for actual changes, checks and remaining limits. The contact-sheet transform bug is fixed; previous sheets incorrectly collapsed part placement. The revised bow and axe silhouettes, creature faces and mineral contrast are actual source-model changes.

## Edit and use

1. Open the model's `.blend` source in Blender. Each mesh part has its own origin and material. Edit geometry, UVs, materials, or paint its packed texture.
2. Run Blender in background mode with `tools/export-model-assets.py -- MODEL_ID` to export only that saved model. Omitting the IDs exports the whole library. This reads the saved Blender file; it never executes the historical generators.
3. Run `python tools/verify-model-assets.py`, then the normal project closeout. The runtime payload and textures are packaged automatically. Source Blender files and GLBs remain in this library for editing and interchange.

Keep `game_node_path`, `game_collision`, and `game_crystal` custom properties on existing mesh objects. They identify the Sentinel's aiming parts, static collision surfaces, and destructible creature crystals. New mesh parts should have unique names. One material per object is currently required by the game exporter; split a part by material when adding materials. Modifiers are evaluated for export. Use Principled BSDF materials with at most one image texture; complex shader graphs need baking first.

Coordinates: Blender Z up, game Y up, meters. Export handles conversion and object transforms. Earth source models also update the original Earth mesh payload and painted atlas when exported. Their original OBJ files are migration references; edit the Blender sources going forward.

The migration scripts and old geometry sources under `tools/ModelExporter/LegacySources` are historical conversion tooling only. They are excluded from the game assembly and normal build. Never rerun them over edited source models.

## Coverage and limits

Includes all geometry formerly owned by active visual builders: 32 staffs; ten crystal weapons; furniture and geology decor; crystal construction; 24 elemental banners; eight crystal beds; the enchanting dais, ice box and complete aiming Sentinel; eight ammunition indicators; world and capstone artifacts; 104 creature/alignment variants; 20 dungeon districts and their passage/traversal pieces; the entrance and Dark Throne; geode and workshop assets; the crown and elemental focus.

The eleven Rootforged A0 construction definitions now have saved Blender sources, matching GLB/runtime mesh payloads, and owned Understone/Worldroot/forged-iron material textures. This is the asset layer only: Hammer registration, explicit snap transforms and live Valheim placement acceptance remain separate runtime work and are not implied by model presence.

Validation: all model triplets have valid geometry, finite vertices, UVs, normals, referenced PNGs and hashes. A test harness exercises the production importer against all 281 files twice and verifies cached mesh reuse and failure guards. Actual Unity rendering, gameplay collision, multiplayer and world placement still require a live game test.

## Review sheets

- [Review sheet 01](previews/catalog-01.png)
- [Review sheet 02](previews/catalog-02.png)
- [Review sheet 03](previews/catalog-03.png)
- [Review sheet 04](previews/catalog-04.png)
- [Review sheet 05](previews/catalog-05.png)
- [Review sheet 06](previews/catalog-06.png)
- [Review sheet 07](previews/catalog-07.png)
- [Review sheet 08](previews/catalog-08.png)
- [Review sheet 09](previews/catalog-09.png)
- [Review sheet 10](previews/catalog-10.png)
- [Review sheet 11](previews/catalog-11.png)
- [Review sheet 12](previews/catalog-12.png)

## All models

| Model | Blender source | GLB | Triangles |
|---|---|---|---:|
| architecture-crystal-beam-2m | [Open](source/architecture-crystal-beam-2m.blend) | [Open](glb/architecture-crystal-beam-2m.glb) | 2,108 |
| architecture-crystal-beam-4m | [Open](source/architecture-crystal-beam-4m.blend) | [Open](glb/architecture-crystal-beam-4m.glb) | 2,108 |
| architecture-crystal-beam-8m | [Open](source/architecture-crystal-beam-8m.blend) | [Open](glb/architecture-crystal-beam-8m.glb) | 2,556 |
| architecture-crystal-foundation-2m | [Open](source/architecture-crystal-foundation-2m.blend) | [Open](glb/architecture-crystal-foundation-2m.glb) | 764 |
| architecture-crystal-foundation-4m | [Open](source/architecture-crystal-foundation-4m.blend) | [Open](glb/architecture-crystal-foundation-4m.glb) | 1,436 |
| architecture-crystal-foundation-8m | [Open](source/architecture-crystal-foundation-8m.blend) | [Open](glb/architecture-crystal-foundation-8m.glb) | 4,124 |
| architecture-crystal-hearth | [Open](source/architecture-crystal-hearth.blend) | [Open](glb/architecture-crystal-hearth.glb) | 3,240 |
| boss-bonemass-rotheart | [Open](source/boss-bonemass-rotheart.blend) | [Open](glb/boss-bonemass-rotheart.glb) | 2,136 |
| boss-eikthyr-stormheart | [Open](source/boss-eikthyr-stormheart.blend) | [Open](glb/boss-eikthyr-stormheart.glb) | 1,144 |
| boss-elder-rootheart | [Open](source/boss-elder-rootheart.blend) | [Open](glb/boss-elder-rootheart.glb) | 1,688 |
| boss-fader-ashheart | [Open](source/boss-fader-ashheart.blend) | [Open](glb/boss-fader-ashheart.glb) | 1,928 |
| boss-kall-winterheart | [Open](source/boss-kall-winterheart.blend) | [Open](glb/boss-kall-winterheart.glb) | 1,584 |
| boss-moder-rimeheart | [Open](source/boss-moder-rimeheart.blend) | [Open](glb/boss-moder-rimeheart.glb) | 1,144 |
| boss-queen-veilheart | [Open](source/boss-queen-veilheart.blend) | [Open](glb/boss-queen-veilheart.glb) | 1,936 |
| boss-yagluth-sunheart | [Open](source/boss-yagluth-sunheart.blend) | [Open](glb/boss-yagluth-sunheart.glb) | 1,368 |
| broken-crown | [Open](source/broken-crown.blend) | [Open](glb/broken-crown.glb) | 1,600 |
| crystal-banner-pennant-earth | [Open](source/crystal-banner-pennant-earth.blend) | [Open](glb/crystal-banner-pennant-earth.glb) | 3,788 |
| crystal-banner-pennant-fire | [Open](source/crystal-banner-pennant-fire.blend) | [Open](glb/crystal-banner-pennant-fire.glb) | 3,788 |
| crystal-banner-pennant-frost | [Open](source/crystal-banner-pennant-frost.blend) | [Open](glb/crystal-banner-pennant-frost.glb) | 3,788 |
| crystal-banner-pennant-radiance | [Open](source/crystal-banner-pennant-radiance.blend) | [Open](glb/crystal-banner-pennant-radiance.glb) | 3,788 |
| crystal-banner-pennant-seidr | [Open](source/crystal-banner-pennant-seidr.blend) | [Open](glb/crystal-banner-pennant-seidr.glb) | 3,788 |
| crystal-banner-pennant-spirit | [Open](source/crystal-banner-pennant-spirit.blend) | [Open](glb/crystal-banner-pennant-spirit.glb) | 3,788 |
| crystal-banner-pennant-storm | [Open](source/crystal-banner-pennant-storm.blend) | [Open](glb/crystal-banner-pennant-storm.glb) | 3,788 |
| crystal-banner-pennant-venom | [Open](source/crystal-banner-pennant-venom.blend) | [Open](glb/crystal-banner-pennant-venom.glb) | 3,788 |
| crystal-banner-standard-earth | [Open](source/crystal-banner-standard-earth.blend) | [Open](glb/crystal-banner-standard-earth.glb) | 2,348 |
| crystal-banner-standard-fire | [Open](source/crystal-banner-standard-fire.blend) | [Open](glb/crystal-banner-standard-fire.glb) | 2,348 |
| crystal-banner-standard-frost | [Open](source/crystal-banner-standard-frost.blend) | [Open](glb/crystal-banner-standard-frost.glb) | 2,348 |
| crystal-banner-standard-radiance | [Open](source/crystal-banner-standard-radiance.blend) | [Open](glb/crystal-banner-standard-radiance.glb) | 2,348 |
| crystal-banner-standard-seidr | [Open](source/crystal-banner-standard-seidr.blend) | [Open](glb/crystal-banner-standard-seidr.glb) | 2,348 |
| crystal-banner-standard-spirit | [Open](source/crystal-banner-standard-spirit.blend) | [Open](glb/crystal-banner-standard-spirit.glb) | 2,348 |
| crystal-banner-standard-storm | [Open](source/crystal-banner-standard-storm.blend) | [Open](glb/crystal-banner-standard-storm.glb) | 2,348 |
| crystal-banner-standard-venom | [Open](source/crystal-banner-standard-venom.blend) | [Open](glb/crystal-banner-standard-venom.glb) | 2,348 |
| crystal-banner-swallowtail-earth | [Open](source/crystal-banner-swallowtail-earth.blend) | [Open](glb/crystal-banner-swallowtail-earth.glb) | 3,384 |
| crystal-banner-swallowtail-fire | [Open](source/crystal-banner-swallowtail-fire.blend) | [Open](glb/crystal-banner-swallowtail-fire.glb) | 3,384 |
| crystal-banner-swallowtail-frost | [Open](source/crystal-banner-swallowtail-frost.blend) | [Open](glb/crystal-banner-swallowtail-frost.glb) | 3,384 |
| crystal-banner-swallowtail-radiance | [Open](source/crystal-banner-swallowtail-radiance.blend) | [Open](glb/crystal-banner-swallowtail-radiance.glb) | 3,384 |
| crystal-banner-swallowtail-seidr | [Open](source/crystal-banner-swallowtail-seidr.blend) | [Open](glb/crystal-banner-swallowtail-seidr.glb) | 3,384 |
| crystal-banner-swallowtail-spirit | [Open](source/crystal-banner-swallowtail-spirit.blend) | [Open](glb/crystal-banner-swallowtail-spirit.glb) | 3,384 |
| crystal-banner-swallowtail-storm | [Open](source/crystal-banner-swallowtail-storm.blend) | [Open](glb/crystal-banner-swallowtail-storm.glb) | 3,384 |
| crystal-banner-swallowtail-venom | [Open](source/crystal-banner-swallowtail-venom.blend) | [Open](glb/crystal-banner-swallowtail-venom.glb) | 3,384 |
| crystal-bed-earth | [Open](source/crystal-bed-earth.blend) | [Open](glb/crystal-bed-earth.glb) | 2,304 |
| crystal-bed-fire | [Open](source/crystal-bed-fire.blend) | [Open](glb/crystal-bed-fire.glb) | 2,304 |
| crystal-bed-frost | [Open](source/crystal-bed-frost.blend) | [Open](glb/crystal-bed-frost.glb) | 2,304 |
| crystal-bed-radiance | [Open](source/crystal-bed-radiance.blend) | [Open](glb/crystal-bed-radiance.glb) | 2,304 |
| crystal-bed-seidr | [Open](source/crystal-bed-seidr.blend) | [Open](glb/crystal-bed-seidr.glb) | 2,304 |
| crystal-bed-spirit | [Open](source/crystal-bed-spirit.blend) | [Open](glb/crystal-bed-spirit.glb) | 2,304 |
| crystal-bed-storm | [Open](source/crystal-bed-storm.blend) | [Open](glb/crystal-bed-storm.glb) | 2,304 |
| crystal-bed-venom | [Open](source/crystal-bed-venom.blend) | [Open](glb/crystal-bed-venom.glb) | 2,304 |
| crystal-brazier | [Open](source/crystal-brazier.blend) | [Open](glb/crystal-brazier.glb) | 2,624 |
| crystal-enchanting-dais | [Open](source/crystal-enchanting-dais.blend) | [Open](glb/crystal-enchanting-dais.glb) | 8,208 |
| crystal-lantern | [Open](source/crystal-lantern.blend) | [Open](glb/crystal-lantern.glb) | 2,552 |
| crystal-sentinel | [Open](source/crystal-sentinel.blend) | [Open](glb/crystal-sentinel.glb) | 6,136 |
| crystal-wardstone | [Open](source/crystal-wardstone.blend) | [Open](glb/crystal-wardstone.glb) | 5,732 |
| crystal-weapon-atgeir | [Open](source/crystal-weapon-atgeir.blend) | [Open](glb/crystal-weapon-atgeir.glb) | 1,328 |
| crystal-weapon-axe | [Open](source/crystal-weapon-axe.blend) | [Open](glb/crystal-weapon-axe.glb) | 1,340 |
| crystal-weapon-battleaxe | [Open](source/crystal-weapon-battleaxe.blend) | [Open](glb/crystal-weapon-battleaxe.glb) | 2,120 |
| crystal-weapon-bow | [Open](source/crystal-weapon-bow.blend) | [Open](glb/crystal-weapon-bow.glb) | 1,296 |
| crystal-weapon-crossbow | [Open](source/crystal-weapon-crossbow.blend) | [Open](glb/crystal-weapon-crossbow.glb) | 1,116 |
| crystal-weapon-greatsword | [Open](source/crystal-weapon-greatsword.blend) | [Open](glb/crystal-weapon-greatsword.glb) | 1,228 |
| crystal-weapon-knife | [Open](source/crystal-weapon-knife.blend) | [Open](glb/crystal-weapon-knife.glb) | 760 |
| crystal-weapon-mace | [Open](source/crystal-weapon-mace.blend) | [Open](glb/crystal-weapon-mace.glb) | 1,792 |
| crystal-weapon-spear | [Open](source/crystal-weapon-spear.blend) | [Open](glb/crystal-weapon-spear.glb) | 1,004 |
| crystal-weapon-sword | [Open](source/crystal-weapon-sword.blend) | [Open](glb/crystal-weapon-sword.glb) | 1,032 |
| crystalline-ice-box | [Open](source/crystalline-ice-box.blend) | [Open](glb/crystalline-ice-box.glb) | 3,528 |
| dark-throne | [Open](source/dark-throne.blend) | [Open](glb/dark-throne.glb) | 880 |
| decor-crystal-coat-rack | [Open](source/decor-crystal-coat-rack.blend) | [Open](glb/decor-crystal-coat-rack.glb) | 2,576 |
| decor-crystal-end-table | [Open](source/decor-crystal-end-table.blend) | [Open](glb/decor-crystal-end-table.glb) | 1,376 |
| decor-crystal-wall-sconce | [Open](source/decor-crystal-wall-sconce.blend) | [Open](glb/decor-crystal-wall-sconce.glb) | 868 |
| decor-cut-geode-plaque | [Open](source/decor-cut-geode-plaque.blend) | [Open](glb/decor-cut-geode-plaque.glb) | 2,232 |
| decor-geode-bowl | [Open](source/decor-geode-bowl.blend) | [Open](glb/decor-geode-bowl.glb) | 2,360 |
| decor-geode-hearth-mantel | [Open](source/decor-geode-hearth-mantel.blend) | [Open](glb/decor-geode-hearth-mantel.glb) | 3,876 |
| decor-geologist-stool | [Open](source/decor-geologist-stool.blend) | [Open](glb/decor-geologist-stool.glb) | 1,028 |
| decor-mineral-display-case | [Open](source/decor-mineral-display-case.blend) | [Open](glb/decor-mineral-display-case.glb) | 2,616 |
| decor-specimen-sideboard | [Open](source/decor-specimen-sideboard.blend) | [Open](glb/decor-specimen-sideboard.glb) | 2,952 |
| decor-strata-map-table | [Open](source/decor-strata-map-table.blend) | [Open](glb/decor-strata-map-table.glb) | 1,752 |
| deep-fracture-creature-AnnoyanceWisp-earth | [Open](source/deep-fracture-creature-AnnoyanceWisp-earth.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-earth.glb) | 1,404 |
| deep-fracture-creature-AnnoyanceWisp-fire | [Open](source/deep-fracture-creature-AnnoyanceWisp-fire.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-fire.glb) | 1,404 |
| deep-fracture-creature-AnnoyanceWisp-frost | [Open](source/deep-fracture-creature-AnnoyanceWisp-frost.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-frost.glb) | 1,404 |
| deep-fracture-creature-AnnoyanceWisp-radiance | [Open](source/deep-fracture-creature-AnnoyanceWisp-radiance.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-radiance.glb) | 1,404 |
| deep-fracture-creature-AnnoyanceWisp-seidr | [Open](source/deep-fracture-creature-AnnoyanceWisp-seidr.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-seidr.glb) | 1,404 |
| deep-fracture-creature-AnnoyanceWisp-spirit | [Open](source/deep-fracture-creature-AnnoyanceWisp-spirit.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-spirit.glb) | 1,404 |
| deep-fracture-creature-AnnoyanceWisp-storm | [Open](source/deep-fracture-creature-AnnoyanceWisp-storm.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-storm.glb) | 1,404 |
| deep-fracture-creature-AnnoyanceWisp-venom | [Open](source/deep-fracture-creature-AnnoyanceWisp-venom.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-venom.glb) | 1,404 |
| deep-fracture-creature-Burrower-earth | [Open](source/deep-fracture-creature-Burrower-earth.blend) | [Open](glb/deep-fracture-creature-Burrower-earth.glb) | 3,836 |
| deep-fracture-creature-Burrower-fire | [Open](source/deep-fracture-creature-Burrower-fire.blend) | [Open](glb/deep-fracture-creature-Burrower-fire.glb) | 3,836 |
| deep-fracture-creature-Burrower-frost | [Open](source/deep-fracture-creature-Burrower-frost.blend) | [Open](glb/deep-fracture-creature-Burrower-frost.glb) | 3,836 |
| deep-fracture-creature-Burrower-radiance | [Open](source/deep-fracture-creature-Burrower-radiance.blend) | [Open](glb/deep-fracture-creature-Burrower-radiance.glb) | 3,836 |
| deep-fracture-creature-Burrower-seidr | [Open](source/deep-fracture-creature-Burrower-seidr.blend) | [Open](glb/deep-fracture-creature-Burrower-seidr.glb) | 3,836 |
| deep-fracture-creature-Burrower-spirit | [Open](source/deep-fracture-creature-Burrower-spirit.blend) | [Open](glb/deep-fracture-creature-Burrower-spirit.glb) | 3,836 |
| deep-fracture-creature-Burrower-storm | [Open](source/deep-fracture-creature-Burrower-storm.blend) | [Open](glb/deep-fracture-creature-Burrower-storm.glb) | 3,836 |
| deep-fracture-creature-Burrower-venom | [Open](source/deep-fracture-creature-Burrower-venom.blend) | [Open](glb/deep-fracture-creature-Burrower-venom.glb) | 3,836 |
| deep-fracture-creature-CrystalHound-earth | [Open](source/deep-fracture-creature-CrystalHound-earth.blend) | [Open](glb/deep-fracture-creature-CrystalHound-earth.glb) | 3,132 |
| deep-fracture-creature-CrystalHound-fire | [Open](source/deep-fracture-creature-CrystalHound-fire.blend) | [Open](glb/deep-fracture-creature-CrystalHound-fire.glb) | 3,132 |
| deep-fracture-creature-CrystalHound-frost | [Open](source/deep-fracture-creature-CrystalHound-frost.blend) | [Open](glb/deep-fracture-creature-CrystalHound-frost.glb) | 3,132 |
| deep-fracture-creature-CrystalHound-radiance | [Open](source/deep-fracture-creature-CrystalHound-radiance.blend) | [Open](glb/deep-fracture-creature-CrystalHound-radiance.glb) | 3,132 |
| deep-fracture-creature-CrystalHound-seidr | [Open](source/deep-fracture-creature-CrystalHound-seidr.blend) | [Open](glb/deep-fracture-creature-CrystalHound-seidr.glb) | 3,132 |
| deep-fracture-creature-CrystalHound-spirit | [Open](source/deep-fracture-creature-CrystalHound-spirit.blend) | [Open](glb/deep-fracture-creature-CrystalHound-spirit.glb) | 3,132 |
| deep-fracture-creature-CrystalHound-storm | [Open](source/deep-fracture-creature-CrystalHound-storm.blend) | [Open](glb/deep-fracture-creature-CrystalHound-storm.glb) | 3,132 |
| deep-fracture-creature-CrystalHound-venom | [Open](source/deep-fracture-creature-CrystalHound-venom.blend) | [Open](glb/deep-fracture-creature-CrystalHound-venom.glb) | 3,132 |
| deep-fracture-creature-CrystalParasite-earth | [Open](source/deep-fracture-creature-CrystalParasite-earth.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-earth.glb) | 2,800 |
| deep-fracture-creature-CrystalParasite-fire | [Open](source/deep-fracture-creature-CrystalParasite-fire.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-fire.glb) | 2,800 |
| deep-fracture-creature-CrystalParasite-frost | [Open](source/deep-fracture-creature-CrystalParasite-frost.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-frost.glb) | 2,800 |
| deep-fracture-creature-CrystalParasite-radiance | [Open](source/deep-fracture-creature-CrystalParasite-radiance.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-radiance.glb) | 2,800 |
| deep-fracture-creature-CrystalParasite-seidr | [Open](source/deep-fracture-creature-CrystalParasite-seidr.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-seidr.glb) | 2,800 |
| deep-fracture-creature-CrystalParasite-spirit | [Open](source/deep-fracture-creature-CrystalParasite-spirit.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-spirit.glb) | 2,800 |
| deep-fracture-creature-CrystalParasite-storm | [Open](source/deep-fracture-creature-CrystalParasite-storm.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-storm.glb) | 2,800 |
| deep-fracture-creature-CrystalParasite-venom | [Open](source/deep-fracture-creature-CrystalParasite-venom.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-venom.glb) | 2,800 |
| deep-fracture-creature-CrystalRevenant-earth | [Open](source/deep-fracture-creature-CrystalRevenant-earth.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-earth.glb) | 2,440 |
| deep-fracture-creature-CrystalRevenant-fire | [Open](source/deep-fracture-creature-CrystalRevenant-fire.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-fire.glb) | 2,440 |
| deep-fracture-creature-CrystalRevenant-frost | [Open](source/deep-fracture-creature-CrystalRevenant-frost.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-frost.glb) | 2,440 |
| deep-fracture-creature-CrystalRevenant-radiance | [Open](source/deep-fracture-creature-CrystalRevenant-radiance.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-radiance.glb) | 2,440 |
| deep-fracture-creature-CrystalRevenant-seidr | [Open](source/deep-fracture-creature-CrystalRevenant-seidr.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-seidr.glb) | 2,440 |
| deep-fracture-creature-CrystalRevenant-spirit | [Open](source/deep-fracture-creature-CrystalRevenant-spirit.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-spirit.glb) | 2,440 |
| deep-fracture-creature-CrystalRevenant-storm | [Open](source/deep-fracture-creature-CrystalRevenant-storm.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-storm.glb) | 2,440 |
| deep-fracture-creature-CrystalRevenant-venom | [Open](source/deep-fracture-creature-CrystalRevenant-venom.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-venom.glb) | 2,440 |
| deep-fracture-creature-FacetSentry-earth | [Open](source/deep-fracture-creature-FacetSentry-earth.blend) | [Open](glb/deep-fracture-creature-FacetSentry-earth.glb) | 1,792 |
| deep-fracture-creature-FacetSentry-fire | [Open](source/deep-fracture-creature-FacetSentry-fire.blend) | [Open](glb/deep-fracture-creature-FacetSentry-fire.glb) | 1,792 |
| deep-fracture-creature-FacetSentry-frost | [Open](source/deep-fracture-creature-FacetSentry-frost.blend) | [Open](glb/deep-fracture-creature-FacetSentry-frost.glb) | 1,792 |
| deep-fracture-creature-FacetSentry-radiance | [Open](source/deep-fracture-creature-FacetSentry-radiance.blend) | [Open](glb/deep-fracture-creature-FacetSentry-radiance.glb) | 1,792 |
| deep-fracture-creature-FacetSentry-seidr | [Open](source/deep-fracture-creature-FacetSentry-seidr.blend) | [Open](glb/deep-fracture-creature-FacetSentry-seidr.glb) | 1,792 |
| deep-fracture-creature-FacetSentry-spirit | [Open](source/deep-fracture-creature-FacetSentry-spirit.blend) | [Open](glb/deep-fracture-creature-FacetSentry-spirit.glb) | 1,792 |
| deep-fracture-creature-FacetSentry-storm | [Open](source/deep-fracture-creature-FacetSentry-storm.blend) | [Open](glb/deep-fracture-creature-FacetSentry-storm.glb) | 1,792 |
| deep-fracture-creature-FacetSentry-venom | [Open](source/deep-fracture-creature-FacetSentry-venom.blend) | [Open](glb/deep-fracture-creature-FacetSentry-venom.glb) | 1,792 |
| deep-fracture-creature-GeodeCrawler-earth | [Open](source/deep-fracture-creature-GeodeCrawler-earth.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-earth.glb) | 1,740 |
| deep-fracture-creature-GeodeCrawler-fire | [Open](source/deep-fracture-creature-GeodeCrawler-fire.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-fire.glb) | 1,740 |
| deep-fracture-creature-GeodeCrawler-frost | [Open](source/deep-fracture-creature-GeodeCrawler-frost.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-frost.glb) | 1,740 |
| deep-fracture-creature-GeodeCrawler-radiance | [Open](source/deep-fracture-creature-GeodeCrawler-radiance.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-radiance.glb) | 1,740 |
| deep-fracture-creature-GeodeCrawler-seidr | [Open](source/deep-fracture-creature-GeodeCrawler-seidr.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-seidr.glb) | 1,740 |
| deep-fracture-creature-GeodeCrawler-spirit | [Open](source/deep-fracture-creature-GeodeCrawler-spirit.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-spirit.glb) | 1,740 |
| deep-fracture-creature-GeodeCrawler-storm | [Open](source/deep-fracture-creature-GeodeCrawler-storm.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-storm.glb) | 1,740 |
| deep-fracture-creature-GeodeCrawler-venom | [Open](source/deep-fracture-creature-GeodeCrawler-venom.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-venom.glb) | 1,740 |
| deep-fracture-creature-Shardling-earth | [Open](source/deep-fracture-creature-Shardling-earth.blend) | [Open](glb/deep-fracture-creature-Shardling-earth.glb) | 1,512 |
| deep-fracture-creature-Shardling-fire | [Open](source/deep-fracture-creature-Shardling-fire.blend) | [Open](glb/deep-fracture-creature-Shardling-fire.glb) | 1,512 |
| deep-fracture-creature-Shardling-frost | [Open](source/deep-fracture-creature-Shardling-frost.blend) | [Open](glb/deep-fracture-creature-Shardling-frost.glb) | 1,512 |
| deep-fracture-creature-Shardling-radiance | [Open](source/deep-fracture-creature-Shardling-radiance.blend) | [Open](glb/deep-fracture-creature-Shardling-radiance.glb) | 1,512 |
| deep-fracture-creature-Shardling-seidr | [Open](source/deep-fracture-creature-Shardling-seidr.blend) | [Open](glb/deep-fracture-creature-Shardling-seidr.glb) | 1,512 |
| deep-fracture-creature-Shardling-spirit | [Open](source/deep-fracture-creature-Shardling-spirit.blend) | [Open](glb/deep-fracture-creature-Shardling-spirit.glb) | 1,512 |
| deep-fracture-creature-Shardling-storm | [Open](source/deep-fracture-creature-Shardling-storm.blend) | [Open](glb/deep-fracture-creature-Shardling-storm.glb) | 1,512 |
| deep-fracture-creature-Shardling-venom | [Open](source/deep-fracture-creature-Shardling-venom.blend) | [Open](glb/deep-fracture-creature-Shardling-venom.glb) | 1,512 |
| deep-fracture-creature-StoneGuardian-earth | [Open](source/deep-fracture-creature-StoneGuardian-earth.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-earth.glb) | 4,212 |
| deep-fracture-creature-StoneGuardian-fire | [Open](source/deep-fracture-creature-StoneGuardian-fire.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-fire.glb) | 4,212 |
| deep-fracture-creature-StoneGuardian-frost | [Open](source/deep-fracture-creature-StoneGuardian-frost.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-frost.glb) | 4,212 |
| deep-fracture-creature-StoneGuardian-radiance | [Open](source/deep-fracture-creature-StoneGuardian-radiance.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-radiance.glb) | 4,212 |
| deep-fracture-creature-StoneGuardian-seidr | [Open](source/deep-fracture-creature-StoneGuardian-seidr.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-seidr.glb) | 4,212 |
| deep-fracture-creature-StoneGuardian-spirit | [Open](source/deep-fracture-creature-StoneGuardian-spirit.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-spirit.glb) | 4,212 |
| deep-fracture-creature-StoneGuardian-storm | [Open](source/deep-fracture-creature-StoneGuardian-storm.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-storm.glb) | 4,212 |
| deep-fracture-creature-StoneGuardian-venom | [Open](source/deep-fracture-creature-StoneGuardian-venom.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-venom.glb) | 4,212 |
| deep-fracture-creature-StoneSentinel-earth | [Open](source/deep-fracture-creature-StoneSentinel-earth.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-earth.glb) | 2,092 |
| deep-fracture-creature-StoneSentinel-fire | [Open](source/deep-fracture-creature-StoneSentinel-fire.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-fire.glb) | 2,092 |
| deep-fracture-creature-StoneSentinel-frost | [Open](source/deep-fracture-creature-StoneSentinel-frost.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-frost.glb) | 2,092 |
| deep-fracture-creature-StoneSentinel-radiance | [Open](source/deep-fracture-creature-StoneSentinel-radiance.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-radiance.glb) | 2,092 |
| deep-fracture-creature-StoneSentinel-seidr | [Open](source/deep-fracture-creature-StoneSentinel-seidr.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-seidr.glb) | 2,092 |
| deep-fracture-creature-StoneSentinel-spirit | [Open](source/deep-fracture-creature-StoneSentinel-spirit.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-spirit.glb) | 2,092 |
| deep-fracture-creature-StoneSentinel-storm | [Open](source/deep-fracture-creature-StoneSentinel-storm.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-storm.glb) | 2,092 |
| deep-fracture-creature-StoneSentinel-venom | [Open](source/deep-fracture-creature-StoneSentinel-venom.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-venom.glb) | 2,092 |
| deep-fracture-crystal-golem-visual-earth | [Open](source/deep-fracture-crystal-golem-visual-earth.blend) | [Open](glb/deep-fracture-crystal-golem-visual-earth.glb) | 2,584 |
| deep-fracture-crystal-golem-visual-fire | [Open](source/deep-fracture-crystal-golem-visual-fire.blend) | [Open](glb/deep-fracture-crystal-golem-visual-fire.glb) | 2,584 |
| deep-fracture-crystal-golem-visual-frost | [Open](source/deep-fracture-crystal-golem-visual-frost.blend) | [Open](glb/deep-fracture-crystal-golem-visual-frost.glb) | 2,584 |
| deep-fracture-crystal-golem-visual-radiance | [Open](source/deep-fracture-crystal-golem-visual-radiance.blend) | [Open](glb/deep-fracture-crystal-golem-visual-radiance.glb) | 2,584 |
| deep-fracture-crystal-golem-visual-seidr | [Open](source/deep-fracture-crystal-golem-visual-seidr.blend) | [Open](glb/deep-fracture-crystal-golem-visual-seidr.glb) | 2,584 |
| deep-fracture-crystal-golem-visual-spirit | [Open](source/deep-fracture-crystal-golem-visual-spirit.blend) | [Open](glb/deep-fracture-crystal-golem-visual-spirit.glb) | 2,584 |
| deep-fracture-crystal-golem-visual-storm | [Open](source/deep-fracture-crystal-golem-visual-storm.blend) | [Open](glb/deep-fracture-crystal-golem-visual-storm.glb) | 2,584 |
| deep-fracture-crystal-golem-visual-venom | [Open](source/deep-fracture-crystal-golem-visual-venom.blend) | [Open](glb/deep-fracture-crystal-golem-visual-venom.glb) | 2,584 |
| deep-fracture-deep-colossus-visual-earth | [Open](source/deep-fracture-deep-colossus-visual-earth.blend) | [Open](glb/deep-fracture-deep-colossus-visual-earth.glb) | 4,888 |
| deep-fracture-deep-colossus-visual-fire | [Open](source/deep-fracture-deep-colossus-visual-fire.blend) | [Open](glb/deep-fracture-deep-colossus-visual-fire.glb) | 4,888 |
| deep-fracture-deep-colossus-visual-frost | [Open](source/deep-fracture-deep-colossus-visual-frost.blend) | [Open](glb/deep-fracture-deep-colossus-visual-frost.glb) | 4,888 |
| deep-fracture-deep-colossus-visual-radiance | [Open](source/deep-fracture-deep-colossus-visual-radiance.blend) | [Open](glb/deep-fracture-deep-colossus-visual-radiance.glb) | 4,888 |
| deep-fracture-deep-colossus-visual-seidr | [Open](source/deep-fracture-deep-colossus-visual-seidr.blend) | [Open](glb/deep-fracture-deep-colossus-visual-seidr.glb) | 4,888 |
| deep-fracture-deep-colossus-visual-spirit | [Open](source/deep-fracture-deep-colossus-visual-spirit.blend) | [Open](glb/deep-fracture-deep-colossus-visual-spirit.glb) | 4,888 |
| deep-fracture-deep-colossus-visual-storm | [Open](source/deep-fracture-deep-colossus-visual-storm.blend) | [Open](glb/deep-fracture-deep-colossus-visual-storm.glb) | 4,888 |
| deep-fracture-deep-colossus-visual-venom | [Open](source/deep-fracture-deep-colossus-visual-venom.blend) | [Open](glb/deep-fracture-deep-colossus-visual-venom.glb) | 4,888 |
| deep-fracture-entrance | [Open](source/deep-fracture-entrance.blend) | [Open](glb/deep-fracture-entrance.glb) | 384 |
| deep-fracture-obelisk-warden-visual-earth | [Open](source/deep-fracture-obelisk-warden-visual-earth.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-earth.glb) | 3,188 |
| deep-fracture-obelisk-warden-visual-fire | [Open](source/deep-fracture-obelisk-warden-visual-fire.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-fire.glb) | 3,188 |
| deep-fracture-obelisk-warden-visual-frost | [Open](source/deep-fracture-obelisk-warden-visual-frost.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-frost.glb) | 3,188 |
| deep-fracture-obelisk-warden-visual-radiance | [Open](source/deep-fracture-obelisk-warden-visual-radiance.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-radiance.glb) | 3,188 |
| deep-fracture-obelisk-warden-visual-seidr | [Open](source/deep-fracture-obelisk-warden-visual-seidr.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-seidr.glb) | 3,188 |
| deep-fracture-obelisk-warden-visual-spirit | [Open](source/deep-fracture-obelisk-warden-visual-spirit.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-spirit.glb) | 3,188 |
| deep-fracture-obelisk-warden-visual-storm | [Open](source/deep-fracture-obelisk-warden-visual-storm.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-storm.glb) | 3,188 |
| deep-fracture-obelisk-warden-visual-venom | [Open](source/deep-fracture-obelisk-warden-visual-venom.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-venom.glb) | 3,188 |
| deep-fracture-passage | [Open](source/deep-fracture-passage.blend) | [Open](glb/deep-fracture-passage.glb) | 60 |
| deep-fracture-traversal | [Open](source/deep-fracture-traversal.blend) | [Open](glb/deep-fracture-traversal.glb) | 172 |
| DF-01 | [Open](source/DF-01.blend) | [Open](glb/DF-01.glb) | 192 |
| DF-02 | [Open](source/DF-02.blend) | [Open](glb/DF-02.glb) | 144 |
| DF-03 | [Open](source/DF-03.blend) | [Open](glb/DF-03.glb) | 284 |
| DF-04 | [Open](source/DF-04.blend) | [Open](glb/DF-04.glb) | 156 |
| DF-05 | [Open](source/DF-05.blend) | [Open](glb/DF-05.glb) | 680 |
| DF-06 | [Open](source/DF-06.blend) | [Open](glb/DF-06.glb) | 680 |
| DF-07 | [Open](source/DF-07.blend) | [Open](glb/DF-07.glb) | 228 |
| DF-08 | [Open](source/DF-08.blend) | [Open](glb/DF-08.glb) | 180 |
| DF-09 | [Open](source/DF-09.blend) | [Open](glb/DF-09.glb) | 204 |
| DF-10 | [Open](source/DF-10.blend) | [Open](glb/DF-10.glb) | 216 |
| DF-11 | [Open](source/DF-11.blend) | [Open](glb/DF-11.glb) | 192 |
| DF-12 | [Open](source/DF-12.blend) | [Open](glb/DF-12.glb) | 284 |
| DF-13 | [Open](source/DF-13.blend) | [Open](glb/DF-13.glb) | 988 |
| DF-14 | [Open](source/DF-14.blend) | [Open](glb/DF-14.glb) | 488 |
| DF-15 | [Open](source/DF-15.blend) | [Open](glb/DF-15.glb) | 264 |
| DF-16 | [Open](source/DF-16.blend) | [Open](glb/DF-16.glb) | 760 |
| DF-17 | [Open](source/DF-17.blend) | [Open](glb/DF-17.glb) | 192 |
| DF-18 | [Open](source/DF-18.blend) | [Open](glb/DF-18.glb) | 264 |
| DF-19 | [Open](source/DF-19.blend) | [Open](glb/DF-19.glb) | 204 |
| DF-20 | [Open](source/DF-20.blend) | [Open](glb/DF-20.glb) | 376 |
| earth-advanced | [Open](source/earth-advanced.blend) | [Open](glb/earth-advanced.glb) | 180 |
| earth-crystal | [Open](source/earth-crystal.blend) | [Open](glb/earth-crystal.glb) | 108 |
| earth-faceting-wheel | [Open](source/earth-faceting-wheel.blend) | [Open](glb/earth-faceting-wheel.glb) | 304 |
| earth-fracturing-block | [Open](source/earth-fracturing-block.blend) | [Open](glb/earth-fracturing-block.glb) | 548 |
| earth-geode | [Open](source/earth-geode.blend) | [Open](glb/earth-geode.glb) | 352 |
| earth-master | [Open](source/earth-master.blend) | [Open](glb/earth-master.glb) | 180 |
| earth-resonance-frame | [Open](source/earth-resonance-frame.blend) | [Open](glb/earth-resonance-frame.glb) | 408 |
| earth-rough | [Open](source/earth-rough.blend) | [Open](glb/earth-rough.glb) | 108 |
| earth-shards | [Open](source/earth-shards.blend) | [Open](glb/earth-shards.glb) | 108 |
| earth-simple | [Open](source/earth-simple.blend) | [Open](glb/earth-simple.glb) | 36 |
| earth-workstation | [Open](source/earth-workstation.blend) | [Open](glb/earth-workstation.glb) | 1,172 |
| effect-orb | [Open](source/effect-orb.blend) | [Open](glb/effect-orb.glb) | 80 |
| effect-ring | [Open](source/effect-ring.blend) | [Open](glb/effect-ring.glb) | 384 |
| effect-shard | [Open](source/effect-shard.blend) | [Open](glb/effect-shard.glb) | 20 |
| elemental-focus | [Open](source/elemental-focus.blend) | [Open](glb/elemental-focus.glb) | 104 |
| fate-crystal | [Open](source/fate-crystal.blend) | [Open](glb/fate-crystal.glb) | 2,880 |
| fate-shard | [Open](source/fate-shard.blend) | [Open](glb/fate-shard.glb) | 336 |
| furniture-crystal-bed | [Open](source/furniture-crystal-bed.blend) | [Open](glb/furniture-crystal-bed.glb) | 996 |
| furniture-crystal-bench | [Open](source/furniture-crystal-bench.blend) | [Open](glb/furniture-crystal-bench.glb) | 936 |
| furniture-crystal-divider | [Open](source/furniture-crystal-divider.blend) | [Open](glb/furniture-crystal-divider.glb) | 1,380 |
| furniture-crystal-throne | [Open](source/furniture-crystal-throne.blend) | [Open](glb/furniture-crystal-throne.glb) | 1,472 |
| furniture-geo-desk | [Open](source/furniture-geo-desk.blend) | [Open](glb/furniture-geo-desk.glb) | 1,168 |
| furniture-geode-chair | [Open](source/furniture-geode-chair.blend) | [Open](glb/furniture-geode-chair.glb) | 1,224 |
| furniture-geode-pedestal | [Open](source/furniture-geode-pedestal.blend) | [Open](glb/furniture-geode-pedestal.glb) | 1,524 |
| furniture-geode-table | [Open](source/furniture-geode-table.blend) | [Open](glb/furniture-geode-table.glb) | 1,852 |
| furniture-lapidary-cabinet | [Open](source/furniture-lapidary-cabinet.blend) | [Open](glb/furniture-lapidary-cabinet.glb) | 1,352 |
| furniture-mineral-shelf | [Open](source/furniture-mineral-shelf.blend) | [Open](glb/furniture-mineral-shelf.glb) | 2,008 |
| geode-sample | [Open](source/geode-sample.blend) | [Open](glb/geode-sample.glb) | 1,259 |
| Magenheim_Staff_Earth_Advanced | [Open](source/Magenheim_Staff_Earth_Advanced.blend) | [Open](glb/Magenheim_Staff_Earth_Advanced.glb) | 2,824 |
| Magenheim_Staff_Earth_Crystal | [Open](source/Magenheim_Staff_Earth_Crystal.blend) | [Open](glb/Magenheim_Staff_Earth_Crystal.glb) | 1,936 |
| Magenheim_Staff_Earth_Master | [Open](source/Magenheim_Staff_Earth_Master.blend) | [Open](glb/Magenheim_Staff_Earth_Master.glb) | 5,112 |
| Magenheim_Staff_Earth_Simple | [Open](source/Magenheim_Staff_Earth_Simple.blend) | [Open](glb/Magenheim_Staff_Earth_Simple.glb) | 1,496 |
| Magenheim_Staff_Fire_Advanced | [Open](source/Magenheim_Staff_Fire_Advanced.blend) | [Open](glb/Magenheim_Staff_Fire_Advanced.glb) | 2,868 |
| Magenheim_Staff_Fire_Crystal | [Open](source/Magenheim_Staff_Fire_Crystal.blend) | [Open](glb/Magenheim_Staff_Fire_Crystal.glb) | 1,716 |
| Magenheim_Staff_Fire_Master | [Open](source/Magenheim_Staff_Fire_Master.blend) | [Open](glb/Magenheim_Staff_Fire_Master.glb) | 3,588 |
| Magenheim_Staff_Fire_Simple | [Open](source/Magenheim_Staff_Fire_Simple.blend) | [Open](glb/Magenheim_Staff_Fire_Simple.glb) | 1,552 |
| Magenheim_Staff_Storm_Advanced | [Open](source/Magenheim_Staff_Storm_Advanced.blend) | [Open](glb/Magenheim_Staff_Storm_Advanced.glb) | 2,136 |
| Magenheim_Staff_Storm_Crystal | [Open](source/Magenheim_Staff_Storm_Crystal.blend) | [Open](glb/Magenheim_Staff_Storm_Crystal.glb) | 1,644 |
| Magenheim_Staff_Storm_Master | [Open](source/Magenheim_Staff_Storm_Master.blend) | [Open](glb/Magenheim_Staff_Storm_Master.glb) | 3,408 |
| Magenheim_Staff_Storm_Simple | [Open](source/Magenheim_Staff_Storm_Simple.blend) | [Open](glb/Magenheim_Staff_Storm_Simple.glb) | 1,552 |
| norn-spindle | [Open](source/norn-spindle.blend) | [Open](glb/norn-spindle.glb) | 1,328 |
| passage-stone | [Open](source/passage-stone.blend) | [Open](glb/passage-stone.glb) | 3,332 |
| rune-engraver | [Open](source/rune-engraver.blend) | [Open](glb/rune-engraver.glb) | 1,436 |
| runed-totem | [Open](source/runed-totem.blend) | [Open](glb/runed-totem.glb) | 4,020 |
| runic-keelstone | [Open](source/runic-keelstone.blend) | [Open](glb/runic-keelstone.glb) | 1,980 |
| seidr-ritual-focus | [Open](source/seidr-ritual-focus.blend) | [Open](glb/seidr-ritual-focus.glb) | 3,060 |
| sentinel-ammo-earth | [Open](source/sentinel-ammo-earth.blend) | [Open](glb/sentinel-ammo-earth.glb) | 252 |
| sentinel-ammo-fire | [Open](source/sentinel-ammo-fire.blend) | [Open](glb/sentinel-ammo-fire.glb) | 252 |
| sentinel-ammo-frost | [Open](source/sentinel-ammo-frost.blend) | [Open](glb/sentinel-ammo-frost.glb) | 252 |
| sentinel-ammo-radiance | [Open](source/sentinel-ammo-radiance.blend) | [Open](glb/sentinel-ammo-radiance.glb) | 252 |
| sentinel-ammo-seidr | [Open](source/sentinel-ammo-seidr.blend) | [Open](glb/sentinel-ammo-seidr.glb) | 252 |
| sentinel-ammo-spirit | [Open](source/sentinel-ammo-spirit.blend) | [Open](glb/sentinel-ammo-spirit.glb) | 252 |
| sentinel-ammo-storm | [Open](source/sentinel-ammo-storm.blend) | [Open](glb/sentinel-ammo-storm.glb) | 252 |
| sentinel-ammo-venom | [Open](source/sentinel-ammo-venom.blend) | [Open](glb/sentinel-ammo-venom.glb) | 252 |
| spirit-fetish-raven | [Open](source/spirit-fetish-raven.blend) | [Open](glb/spirit-fetish-raven.glb) | 1,864 |
| spirit-fetish-warrior | [Open](source/spirit-fetish-warrior.blend) | [Open](glb/spirit-fetish-warrior.glb) | 1,072 |
| spirit-fetish-wolf | [Open](source/spirit-fetish-wolf.blend) | [Open](glb/spirit-fetish-wolf.glb) | 1,468 |
| staff-frost-advanced | [Open](source/staff-frost-advanced.blend) | [Open](glb/staff-frost-advanced.glb) | 2,664 |
| staff-frost-crystal | [Open](source/staff-frost-crystal.blend) | [Open](glb/staff-frost-crystal.glb) | 1,716 |
| staff-frost-master | [Open](source/staff-frost-master.blend) | [Open](glb/staff-frost-master.glb) | 4,080 |
| staff-frost-simple | [Open](source/staff-frost-simple.blend) | [Open](glb/staff-frost-simple.glb) | 1,552 |
| staff-radiance-advanced | [Open](source/staff-radiance-advanced.blend) | [Open](glb/staff-radiance-advanced.glb) | 3,160 |
| staff-radiance-crystal | [Open](source/staff-radiance-crystal.blend) | [Open](glb/staff-radiance-crystal.glb) | 1,792 |
| staff-radiance-master | [Open](source/staff-radiance-master.blend) | [Open](glb/staff-radiance-master.glb) | 3,664 |
| staff-radiance-simple | [Open](source/staff-radiance-simple.blend) | [Open](glb/staff-radiance-simple.glb) | 1,276 |
| staff-seidr-advanced | [Open](source/staff-seidr-advanced.blend) | [Open](glb/staff-seidr-advanced.glb) | 3,028 |
| staff-seidr-crystal | [Open](source/staff-seidr-crystal.blend) | [Open](glb/staff-seidr-crystal.glb) | 1,900 |
| staff-seidr-master | [Open](source/staff-seidr-master.blend) | [Open](glb/staff-seidr-master.glb) | 6,880 |
| staff-seidr-simple | [Open](source/staff-seidr-simple.blend) | [Open](glb/staff-seidr-simple.glb) | 1,384 |
| staff-spirit-advanced | [Open](source/staff-spirit-advanced.blend) | [Open](glb/staff-spirit-advanced.glb) | 6,168 |
| staff-spirit-crystal | [Open](source/staff-spirit-crystal.blend) | [Open](glb/staff-spirit-crystal.glb) | 2,352 |
| staff-spirit-master | [Open](source/staff-spirit-master.blend) | [Open](glb/staff-spirit-master.glb) | 13,584 |
| staff-spirit-simple | [Open](source/staff-spirit-simple.blend) | [Open](glb/staff-spirit-simple.glb) | 1,308 |
| staff-venom-advanced | [Open](source/staff-venom-advanced.blend) | [Open](glb/staff-venom-advanced.glb) | 3,216 |
| staff-venom-crystal | [Open](source/staff-venom-crystal.blend) | [Open](glb/staff-venom-crystal.glb) | 2,280 |
| staff-venom-master | [Open](source/staff-venom-master.blend) | [Open](glb/staff-venom-master.glb) | 3,936 |
| staff-venom-simple | [Open](source/staff-venom-simple.blend) | [Open](glb/staff-venom-simple.glb) | 1,552 |
| underworld-dais | [Open](source/underworld-dais.blend) | [Open](glb/underworld-dais.glb) | 572 |
| underworld-standing-stone | [Open](source/underworld-standing-stone.blend) | [Open](glb/underworld-standing-stone.glb) | 76 |
