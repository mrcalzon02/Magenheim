# Editable Magenheim model library

276 saved Blender models and matching GLB exports. These are real mesh assets with UVs, named editable parts, and material slots. Runtime geometry comes from exported mesh data; the game no longer executes the former shape builders.

The migration preserves the existing designs as editable starting models. It is not a claim that their artwork is finished: most creatures are still rigid forms, many shapes remain simple, and there is no new animation rig or LOD system. The Nowhere King still uses the game's humanoid body and animations with an owned crown attachment. Inherited vanilla projectiles, effects, and character chassis remain game assets.

## Edit and use

1. Open the model's `.blend` source in Blender. Each mesh part has its own origin and material. Edit geometry, UVs, materials, or paint its packed texture.
2. Run Blender in background mode with `tools/export-model-assets.py -- MODEL_ID` to export only that saved model. Omitting the IDs exports the whole library. This reads the saved Blender file; it never executes the historical generators.
3. Run `python tools/verify-model-assets.py`, then the normal project closeout. The runtime payload and textures are packaged automatically. Source Blender files and GLBs remain in this library for editing and interchange.

Keep `game_node_path`, `game_collision`, and `game_crystal` custom properties on existing mesh objects. They identify the Sentinel's aiming parts, static collision surfaces, and destructible creature crystals. New mesh parts should have unique names. One material per object is currently required by the game exporter; split a part by material when adding materials. Modifiers are evaluated for export. Use Principled BSDF materials with at most one image texture; complex shader graphs need baking first.

Coordinates: Blender Z up, game Y up, meters. Export handles conversion and object transforms. Earth source models also update the original Earth mesh payload and painted atlas when exported. Their original OBJ files are migration references; edit the Blender sources going forward.

The migration scripts and old geometry sources under `tools/ModelExporter/LegacySources` are historical conversion tooling only. They are excluded from the game assembly and normal build. Never rerun them over edited source models.

## Coverage and limits

Includes all geometry formerly owned by active visual builders: 32 staffs; ten crystal weapons; furniture and geology decor; crystal construction; 24 elemental banners; eight crystal beds; the enchanting dais, ice box and complete aiming Sentinel; eight ammunition indicators; world and capstone artifacts; 104 creature/alignment variants; 20 dungeon districts and their passage/traversal pieces; the entrance and Dark Throne; geode and workshop assets; the crown and elemental focus.

Definition-only future Underworld content (six bosses/biomes and eleven Rootforged construction definitions) had no runtime model generators to convert. Those remain future content in the project plans, not silently invented or counted as completed assets here.

Validation: all model triplets have valid geometry, finite vertices, UVs, normals, referenced PNGs and hashes. A test harness exercises the production importer against all 276 files twice and verifies cached mesh reuse and failure guards. Actual Unity rendering, gameplay collision, multiplayer and world placement still require a live game test.

## Review sheets

- [Review sheet 01](previews/catalog-01.jpg)
- [Review sheet 02](previews/catalog-02.jpg)
- [Review sheet 03](previews/catalog-03.jpg)
- [Review sheet 04](previews/catalog-04.jpg)
- [Review sheet 05](previews/catalog-05.jpg)
- [Review sheet 06](previews/catalog-06.jpg)
- [Review sheet 07](previews/catalog-07.jpg)
- [Review sheet 08](previews/catalog-08.jpg)
- [Review sheet 09](previews/catalog-09.jpg)
- [Review sheet 10](previews/catalog-10.jpg)
- [Review sheet 11](previews/catalog-11.jpg)
- [Review sheet 12](previews/catalog-12.jpg)

## All models

| Model | Blender source | GLB | Triangles |
|---|---|---|---:|
| architecture-crystal-beam-2m | [Open](source/architecture-crystal-beam-2m.blend) | [Open](glb/architecture-crystal-beam-2m.glb) | 284 |
| architecture-crystal-beam-4m | [Open](source/architecture-crystal-beam-4m.blend) | [Open](glb/architecture-crystal-beam-4m.glb) | 284 |
| architecture-crystal-beam-8m | [Open](source/architecture-crystal-beam-8m.blend) | [Open](glb/architecture-crystal-beam-8m.glb) | 284 |
| architecture-crystal-foundation-2m | [Open](source/architecture-crystal-foundation-2m.blend) | [Open](glb/architecture-crystal-foundation-2m.glb) | 92 |
| architecture-crystal-foundation-4m | [Open](source/architecture-crystal-foundation-4m.blend) | [Open](glb/architecture-crystal-foundation-4m.glb) | 188 |
| architecture-crystal-foundation-8m | [Open](source/architecture-crystal-foundation-8m.blend) | [Open](glb/architecture-crystal-foundation-8m.glb) | 572 |
| architecture-crystal-hearth | [Open](source/architecture-crystal-hearth.blend) | [Open](glb/architecture-crystal-hearth.glb) | 392 |
| boss-bonemass-rotheart | [Open](source/boss-bonemass-rotheart.blend) | [Open](glb/boss-bonemass-rotheart.glb) | 292 |
| boss-eikthyr-stormheart | [Open](source/boss-eikthyr-stormheart.blend) | [Open](glb/boss-eikthyr-stormheart.glb) | 136 |
| boss-elder-rootheart | [Open](source/boss-elder-rootheart.blend) | [Open](glb/boss-elder-rootheart.glb) | 204 |
| boss-fader-ashheart | [Open](source/boss-fader-ashheart.blend) | [Open](glb/boss-fader-ashheart.glb) | 272 |
| boss-kall-winterheart | [Open](source/boss-kall-winterheart.blend) | [Open](glb/boss-kall-winterheart.glb) | 192 |
| boss-moder-rimeheart | [Open](source/boss-moder-rimeheart.blend) | [Open](glb/boss-moder-rimeheart.glb) | 184 |
| boss-queen-veilheart | [Open](source/boss-queen-veilheart.blend) | [Open](glb/boss-queen-veilheart.glb) | 232 |
| boss-yagluth-sunheart | [Open](source/boss-yagluth-sunheart.blend) | [Open](glb/boss-yagluth-sunheart.glb) | 240 |
| broken-crown | [Open](source/broken-crown.blend) | [Open](glb/broken-crown.glb) | 384 |
| crystal-banner-pennant-earth | [Open](source/crystal-banner-pennant-earth.blend) | [Open](glb/crystal-banner-pennant-earth.glb) | 216 |
| crystal-banner-pennant-fire | [Open](source/crystal-banner-pennant-fire.blend) | [Open](glb/crystal-banner-pennant-fire.glb) | 216 |
| crystal-banner-pennant-frost | [Open](source/crystal-banner-pennant-frost.blend) | [Open](glb/crystal-banner-pennant-frost.glb) | 216 |
| crystal-banner-pennant-radiance | [Open](source/crystal-banner-pennant-radiance.blend) | [Open](glb/crystal-banner-pennant-radiance.glb) | 216 |
| crystal-banner-pennant-seidr | [Open](source/crystal-banner-pennant-seidr.blend) | [Open](glb/crystal-banner-pennant-seidr.glb) | 216 |
| crystal-banner-pennant-spirit | [Open](source/crystal-banner-pennant-spirit.blend) | [Open](glb/crystal-banner-pennant-spirit.glb) | 216 |
| crystal-banner-pennant-storm | [Open](source/crystal-banner-pennant-storm.blend) | [Open](glb/crystal-banner-pennant-storm.glb) | 216 |
| crystal-banner-pennant-venom | [Open](source/crystal-banner-pennant-venom.blend) | [Open](glb/crystal-banner-pennant-venom.glb) | 216 |
| crystal-banner-standard-earth | [Open](source/crystal-banner-standard-earth.blend) | [Open](glb/crystal-banner-standard-earth.glb) | 176 |
| crystal-banner-standard-fire | [Open](source/crystal-banner-standard-fire.blend) | [Open](glb/crystal-banner-standard-fire.glb) | 176 |
| crystal-banner-standard-frost | [Open](source/crystal-banner-standard-frost.blend) | [Open](glb/crystal-banner-standard-frost.glb) | 176 |
| crystal-banner-standard-radiance | [Open](source/crystal-banner-standard-radiance.blend) | [Open](glb/crystal-banner-standard-radiance.glb) | 176 |
| crystal-banner-standard-seidr | [Open](source/crystal-banner-standard-seidr.blend) | [Open](glb/crystal-banner-standard-seidr.glb) | 176 |
| crystal-banner-standard-spirit | [Open](source/crystal-banner-standard-spirit.blend) | [Open](glb/crystal-banner-standard-spirit.glb) | 176 |
| crystal-banner-standard-storm | [Open](source/crystal-banner-standard-storm.blend) | [Open](glb/crystal-banner-standard-storm.glb) | 176 |
| crystal-banner-standard-venom | [Open](source/crystal-banner-standard-venom.blend) | [Open](glb/crystal-banner-standard-venom.glb) | 176 |
| crystal-banner-swallowtail-earth | [Open](source/crystal-banner-swallowtail-earth.blend) | [Open](glb/crystal-banner-swallowtail-earth.glb) | 228 |
| crystal-banner-swallowtail-fire | [Open](source/crystal-banner-swallowtail-fire.blend) | [Open](glb/crystal-banner-swallowtail-fire.glb) | 228 |
| crystal-banner-swallowtail-frost | [Open](source/crystal-banner-swallowtail-frost.blend) | [Open](glb/crystal-banner-swallowtail-frost.glb) | 228 |
| crystal-banner-swallowtail-radiance | [Open](source/crystal-banner-swallowtail-radiance.blend) | [Open](glb/crystal-banner-swallowtail-radiance.glb) | 228 |
| crystal-banner-swallowtail-seidr | [Open](source/crystal-banner-swallowtail-seidr.blend) | [Open](glb/crystal-banner-swallowtail-seidr.glb) | 228 |
| crystal-banner-swallowtail-spirit | [Open](source/crystal-banner-swallowtail-spirit.blend) | [Open](glb/crystal-banner-swallowtail-spirit.glb) | 228 |
| crystal-banner-swallowtail-storm | [Open](source/crystal-banner-swallowtail-storm.blend) | [Open](glb/crystal-banner-swallowtail-storm.glb) | 228 |
| crystal-banner-swallowtail-venom | [Open](source/crystal-banner-swallowtail-venom.blend) | [Open](glb/crystal-banner-swallowtail-venom.glb) | 228 |
| crystal-bed-earth | [Open](source/crystal-bed-earth.blend) | [Open](glb/crystal-bed-earth.glb) | 264 |
| crystal-bed-fire | [Open](source/crystal-bed-fire.blend) | [Open](glb/crystal-bed-fire.glb) | 264 |
| crystal-bed-frost | [Open](source/crystal-bed-frost.blend) | [Open](glb/crystal-bed-frost.glb) | 264 |
| crystal-bed-radiance | [Open](source/crystal-bed-radiance.blend) | [Open](glb/crystal-bed-radiance.glb) | 264 |
| crystal-bed-seidr | [Open](source/crystal-bed-seidr.blend) | [Open](glb/crystal-bed-seidr.glb) | 264 |
| crystal-bed-spirit | [Open](source/crystal-bed-spirit.blend) | [Open](glb/crystal-bed-spirit.glb) | 264 |
| crystal-bed-storm | [Open](source/crystal-bed-storm.blend) | [Open](glb/crystal-bed-storm.glb) | 264 |
| crystal-bed-venom | [Open](source/crystal-bed-venom.blend) | [Open](glb/crystal-bed-venom.glb) | 264 |
| crystal-brazier | [Open](source/crystal-brazier.blend) | [Open](glb/crystal-brazier.glb) | 336 |
| crystal-enchanting-dais | [Open](source/crystal-enchanting-dais.blend) | [Open](glb/crystal-enchanting-dais.glb) | 1,136 |
| crystal-lantern | [Open](source/crystal-lantern.blend) | [Open](glb/crystal-lantern.glb) | 320 |
| crystal-sentinel | [Open](source/crystal-sentinel.blend) | [Open](glb/crystal-sentinel.glb) | 728 |
| crystal-wardstone | [Open](source/crystal-wardstone.blend) | [Open](glb/crystal-wardstone.glb) | 672 |
| crystal-weapon-atgeir | [Open](source/crystal-weapon-atgeir.blend) | [Open](glb/crystal-weapon-atgeir.glb) | 176 |
| crystal-weapon-axe | [Open](source/crystal-weapon-axe.blend) | [Open](glb/crystal-weapon-axe.glb) | 164 |
| crystal-weapon-battleaxe | [Open](source/crystal-weapon-battleaxe.blend) | [Open](glb/crystal-weapon-battleaxe.glb) | 248 |
| crystal-weapon-bow | [Open](source/crystal-weapon-bow.blend) | [Open](glb/crystal-weapon-bow.glb) | 160 |
| crystal-weapon-crossbow | [Open](source/crystal-weapon-crossbow.blend) | [Open](glb/crystal-weapon-crossbow.glb) | 124 |
| crystal-weapon-greatsword | [Open](source/crystal-weapon-greatsword.blend) | [Open](glb/crystal-weapon-greatsword.glb) | 148 |
| crystal-weapon-knife | [Open](source/crystal-weapon-knife.blend) | [Open](glb/crystal-weapon-knife.glb) | 96 |
| crystal-weapon-mace | [Open](source/crystal-weapon-mace.blend) | [Open](glb/crystal-weapon-mace.glb) | 264 |
| crystal-weapon-spear | [Open](source/crystal-weapon-spear.blend) | [Open](glb/crystal-weapon-spear.glb) | 140 |
| crystal-weapon-sword | [Open](source/crystal-weapon-sword.blend) | [Open](glb/crystal-weapon-sword.glb) | 144 |
| crystalline-ice-box | [Open](source/crystalline-ice-box.blend) | [Open](glb/crystalline-ice-box.glb) | 408 |
| dark-throne | [Open](source/dark-throne.blend) | [Open](glb/dark-throne.glb) | 880 |
| decor-crystal-coat-rack | [Open](source/decor-crystal-coat-rack.blend) | [Open](glb/decor-crystal-coat-rack.glb) | 368 |
| decor-crystal-end-table | [Open](source/decor-crystal-end-table.blend) | [Open](glb/decor-crystal-end-table.glb) | 208 |
| decor-crystal-wall-sconce | [Open](source/decor-crystal-wall-sconce.blend) | [Open](glb/decor-crystal-wall-sconce.glb) | 120 |
| decor-cut-geode-plaque | [Open](source/decor-cut-geode-plaque.blend) | [Open](glb/decor-cut-geode-plaque.glb) | 292 |
| decor-geode-bowl | [Open](source/decor-geode-bowl.blend) | [Open](glb/decor-geode-bowl.glb) | 312 |
| decor-geode-hearth-mantel | [Open](source/decor-geode-hearth-mantel.blend) | [Open](glb/decor-geode-hearth-mantel.glb) | 464 |
| decor-geologist-stool | [Open](source/decor-geologist-stool.blend) | [Open](glb/decor-geologist-stool.glb) | 140 |
| decor-mineral-display-case | [Open](source/decor-mineral-display-case.blend) | [Open](glb/decor-mineral-display-case.glb) | 312 |
| decor-specimen-sideboard | [Open](source/decor-specimen-sideboard.blend) | [Open](glb/decor-specimen-sideboard.glb) | 364 |
| decor-strata-map-table | [Open](source/decor-strata-map-table.blend) | [Open](glb/decor-strata-map-table.glb) | 212 |
| deep-fracture-creature-AnnoyanceWisp-earth | [Open](source/deep-fracture-creature-AnnoyanceWisp-earth.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-earth.glb) | 156 |
| deep-fracture-creature-AnnoyanceWisp-fire | [Open](source/deep-fracture-creature-AnnoyanceWisp-fire.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-fire.glb) | 156 |
| deep-fracture-creature-AnnoyanceWisp-frost | [Open](source/deep-fracture-creature-AnnoyanceWisp-frost.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-frost.glb) | 156 |
| deep-fracture-creature-AnnoyanceWisp-radiance | [Open](source/deep-fracture-creature-AnnoyanceWisp-radiance.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-radiance.glb) | 156 |
| deep-fracture-creature-AnnoyanceWisp-seidr | [Open](source/deep-fracture-creature-AnnoyanceWisp-seidr.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-seidr.glb) | 156 |
| deep-fracture-creature-AnnoyanceWisp-spirit | [Open](source/deep-fracture-creature-AnnoyanceWisp-spirit.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-spirit.glb) | 156 |
| deep-fracture-creature-AnnoyanceWisp-storm | [Open](source/deep-fracture-creature-AnnoyanceWisp-storm.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-storm.glb) | 156 |
| deep-fracture-creature-AnnoyanceWisp-venom | [Open](source/deep-fracture-creature-AnnoyanceWisp-venom.blend) | [Open](glb/deep-fracture-creature-AnnoyanceWisp-venom.glb) | 156 |
| deep-fracture-creature-Burrower-earth | [Open](source/deep-fracture-creature-Burrower-earth.blend) | [Open](glb/deep-fracture-creature-Burrower-earth.glb) | 444 |
| deep-fracture-creature-Burrower-fire | [Open](source/deep-fracture-creature-Burrower-fire.blend) | [Open](glb/deep-fracture-creature-Burrower-fire.glb) | 444 |
| deep-fracture-creature-Burrower-frost | [Open](source/deep-fracture-creature-Burrower-frost.blend) | [Open](glb/deep-fracture-creature-Burrower-frost.glb) | 444 |
| deep-fracture-creature-Burrower-radiance | [Open](source/deep-fracture-creature-Burrower-radiance.blend) | [Open](glb/deep-fracture-creature-Burrower-radiance.glb) | 444 |
| deep-fracture-creature-Burrower-seidr | [Open](source/deep-fracture-creature-Burrower-seidr.blend) | [Open](glb/deep-fracture-creature-Burrower-seidr.glb) | 444 |
| deep-fracture-creature-Burrower-spirit | [Open](source/deep-fracture-creature-Burrower-spirit.blend) | [Open](glb/deep-fracture-creature-Burrower-spirit.glb) | 444 |
| deep-fracture-creature-Burrower-storm | [Open](source/deep-fracture-creature-Burrower-storm.blend) | [Open](glb/deep-fracture-creature-Burrower-storm.glb) | 444 |
| deep-fracture-creature-Burrower-venom | [Open](source/deep-fracture-creature-Burrower-venom.blend) | [Open](glb/deep-fracture-creature-Burrower-venom.glb) | 444 |
| deep-fracture-creature-CrystalHound-earth | [Open](source/deep-fracture-creature-CrystalHound-earth.blend) | [Open](glb/deep-fracture-creature-CrystalHound-earth.glb) | 368 |
| deep-fracture-creature-CrystalHound-fire | [Open](source/deep-fracture-creature-CrystalHound-fire.blend) | [Open](glb/deep-fracture-creature-CrystalHound-fire.glb) | 368 |
| deep-fracture-creature-CrystalHound-frost | [Open](source/deep-fracture-creature-CrystalHound-frost.blend) | [Open](glb/deep-fracture-creature-CrystalHound-frost.glb) | 368 |
| deep-fracture-creature-CrystalHound-radiance | [Open](source/deep-fracture-creature-CrystalHound-radiance.blend) | [Open](glb/deep-fracture-creature-CrystalHound-radiance.glb) | 368 |
| deep-fracture-creature-CrystalHound-seidr | [Open](source/deep-fracture-creature-CrystalHound-seidr.blend) | [Open](glb/deep-fracture-creature-CrystalHound-seidr.glb) | 368 |
| deep-fracture-creature-CrystalHound-spirit | [Open](source/deep-fracture-creature-CrystalHound-spirit.blend) | [Open](glb/deep-fracture-creature-CrystalHound-spirit.glb) | 368 |
| deep-fracture-creature-CrystalHound-storm | [Open](source/deep-fracture-creature-CrystalHound-storm.blend) | [Open](glb/deep-fracture-creature-CrystalHound-storm.glb) | 368 |
| deep-fracture-creature-CrystalHound-venom | [Open](source/deep-fracture-creature-CrystalHound-venom.blend) | [Open](glb/deep-fracture-creature-CrystalHound-venom.glb) | 368 |
| deep-fracture-creature-CrystalParasite-earth | [Open](source/deep-fracture-creature-CrystalParasite-earth.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-earth.glb) | 336 |
| deep-fracture-creature-CrystalParasite-fire | [Open](source/deep-fracture-creature-CrystalParasite-fire.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-fire.glb) | 336 |
| deep-fracture-creature-CrystalParasite-frost | [Open](source/deep-fracture-creature-CrystalParasite-frost.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-frost.glb) | 336 |
| deep-fracture-creature-CrystalParasite-radiance | [Open](source/deep-fracture-creature-CrystalParasite-radiance.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-radiance.glb) | 336 |
| deep-fracture-creature-CrystalParasite-seidr | [Open](source/deep-fracture-creature-CrystalParasite-seidr.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-seidr.glb) | 336 |
| deep-fracture-creature-CrystalParasite-spirit | [Open](source/deep-fracture-creature-CrystalParasite-spirit.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-spirit.glb) | 336 |
| deep-fracture-creature-CrystalParasite-storm | [Open](source/deep-fracture-creature-CrystalParasite-storm.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-storm.glb) | 336 |
| deep-fracture-creature-CrystalParasite-venom | [Open](source/deep-fracture-creature-CrystalParasite-venom.blend) | [Open](glb/deep-fracture-creature-CrystalParasite-venom.glb) | 336 |
| deep-fracture-creature-CrystalRevenant-earth | [Open](source/deep-fracture-creature-CrystalRevenant-earth.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-earth.glb) | 284 |
| deep-fracture-creature-CrystalRevenant-fire | [Open](source/deep-fracture-creature-CrystalRevenant-fire.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-fire.glb) | 284 |
| deep-fracture-creature-CrystalRevenant-frost | [Open](source/deep-fracture-creature-CrystalRevenant-frost.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-frost.glb) | 284 |
| deep-fracture-creature-CrystalRevenant-radiance | [Open](source/deep-fracture-creature-CrystalRevenant-radiance.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-radiance.glb) | 284 |
| deep-fracture-creature-CrystalRevenant-seidr | [Open](source/deep-fracture-creature-CrystalRevenant-seidr.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-seidr.glb) | 284 |
| deep-fracture-creature-CrystalRevenant-spirit | [Open](source/deep-fracture-creature-CrystalRevenant-spirit.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-spirit.glb) | 284 |
| deep-fracture-creature-CrystalRevenant-storm | [Open](source/deep-fracture-creature-CrystalRevenant-storm.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-storm.glb) | 284 |
| deep-fracture-creature-CrystalRevenant-venom | [Open](source/deep-fracture-creature-CrystalRevenant-venom.blend) | [Open](glb/deep-fracture-creature-CrystalRevenant-venom.glb) | 284 |
| deep-fracture-creature-FacetSentry-earth | [Open](source/deep-fracture-creature-FacetSentry-earth.blend) | [Open](glb/deep-fracture-creature-FacetSentry-earth.glb) | 208 |
| deep-fracture-creature-FacetSentry-fire | [Open](source/deep-fracture-creature-FacetSentry-fire.blend) | [Open](glb/deep-fracture-creature-FacetSentry-fire.glb) | 208 |
| deep-fracture-creature-FacetSentry-frost | [Open](source/deep-fracture-creature-FacetSentry-frost.blend) | [Open](glb/deep-fracture-creature-FacetSentry-frost.glb) | 208 |
| deep-fracture-creature-FacetSentry-radiance | [Open](source/deep-fracture-creature-FacetSentry-radiance.blend) | [Open](glb/deep-fracture-creature-FacetSentry-radiance.glb) | 208 |
| deep-fracture-creature-FacetSentry-seidr | [Open](source/deep-fracture-creature-FacetSentry-seidr.blend) | [Open](glb/deep-fracture-creature-FacetSentry-seidr.glb) | 208 |
| deep-fracture-creature-FacetSentry-spirit | [Open](source/deep-fracture-creature-FacetSentry-spirit.blend) | [Open](glb/deep-fracture-creature-FacetSentry-spirit.glb) | 208 |
| deep-fracture-creature-FacetSentry-storm | [Open](source/deep-fracture-creature-FacetSentry-storm.blend) | [Open](glb/deep-fracture-creature-FacetSentry-storm.glb) | 208 |
| deep-fracture-creature-FacetSentry-venom | [Open](source/deep-fracture-creature-FacetSentry-venom.blend) | [Open](glb/deep-fracture-creature-FacetSentry-venom.glb) | 208 |
| deep-fracture-creature-GeodeCrawler-earth | [Open](source/deep-fracture-creature-GeodeCrawler-earth.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-earth.glb) | 204 |
| deep-fracture-creature-GeodeCrawler-fire | [Open](source/deep-fracture-creature-GeodeCrawler-fire.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-fire.glb) | 204 |
| deep-fracture-creature-GeodeCrawler-frost | [Open](source/deep-fracture-creature-GeodeCrawler-frost.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-frost.glb) | 204 |
| deep-fracture-creature-GeodeCrawler-radiance | [Open](source/deep-fracture-creature-GeodeCrawler-radiance.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-radiance.glb) | 204 |
| deep-fracture-creature-GeodeCrawler-seidr | [Open](source/deep-fracture-creature-GeodeCrawler-seidr.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-seidr.glb) | 204 |
| deep-fracture-creature-GeodeCrawler-spirit | [Open](source/deep-fracture-creature-GeodeCrawler-spirit.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-spirit.glb) | 204 |
| deep-fracture-creature-GeodeCrawler-storm | [Open](source/deep-fracture-creature-GeodeCrawler-storm.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-storm.glb) | 204 |
| deep-fracture-creature-GeodeCrawler-venom | [Open](source/deep-fracture-creature-GeodeCrawler-venom.blend) | [Open](glb/deep-fracture-creature-GeodeCrawler-venom.glb) | 204 |
| deep-fracture-creature-Shardling-earth | [Open](source/deep-fracture-creature-Shardling-earth.blend) | [Open](glb/deep-fracture-creature-Shardling-earth.glb) | 176 |
| deep-fracture-creature-Shardling-fire | [Open](source/deep-fracture-creature-Shardling-fire.blend) | [Open](glb/deep-fracture-creature-Shardling-fire.glb) | 176 |
| deep-fracture-creature-Shardling-frost | [Open](source/deep-fracture-creature-Shardling-frost.blend) | [Open](glb/deep-fracture-creature-Shardling-frost.glb) | 176 |
| deep-fracture-creature-Shardling-radiance | [Open](source/deep-fracture-creature-Shardling-radiance.blend) | [Open](glb/deep-fracture-creature-Shardling-radiance.glb) | 176 |
| deep-fracture-creature-Shardling-seidr | [Open](source/deep-fracture-creature-Shardling-seidr.blend) | [Open](glb/deep-fracture-creature-Shardling-seidr.glb) | 176 |
| deep-fracture-creature-Shardling-spirit | [Open](source/deep-fracture-creature-Shardling-spirit.blend) | [Open](glb/deep-fracture-creature-Shardling-spirit.glb) | 176 |
| deep-fracture-creature-Shardling-storm | [Open](source/deep-fracture-creature-Shardling-storm.blend) | [Open](glb/deep-fracture-creature-Shardling-storm.glb) | 176 |
| deep-fracture-creature-Shardling-venom | [Open](source/deep-fracture-creature-Shardling-venom.blend) | [Open](glb/deep-fracture-creature-Shardling-venom.glb) | 176 |
| deep-fracture-creature-StoneGuardian-earth | [Open](source/deep-fracture-creature-StoneGuardian-earth.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-earth.glb) | 496 |
| deep-fracture-creature-StoneGuardian-fire | [Open](source/deep-fracture-creature-StoneGuardian-fire.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-fire.glb) | 496 |
| deep-fracture-creature-StoneGuardian-frost | [Open](source/deep-fracture-creature-StoneGuardian-frost.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-frost.glb) | 496 |
| deep-fracture-creature-StoneGuardian-radiance | [Open](source/deep-fracture-creature-StoneGuardian-radiance.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-radiance.glb) | 496 |
| deep-fracture-creature-StoneGuardian-seidr | [Open](source/deep-fracture-creature-StoneGuardian-seidr.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-seidr.glb) | 496 |
| deep-fracture-creature-StoneGuardian-spirit | [Open](source/deep-fracture-creature-StoneGuardian-spirit.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-spirit.glb) | 496 |
| deep-fracture-creature-StoneGuardian-storm | [Open](source/deep-fracture-creature-StoneGuardian-storm.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-storm.glb) | 496 |
| deep-fracture-creature-StoneGuardian-venom | [Open](source/deep-fracture-creature-StoneGuardian-venom.blend) | [Open](glb/deep-fracture-creature-StoneGuardian-venom.glb) | 496 |
| deep-fracture-creature-StoneSentinel-earth | [Open](source/deep-fracture-creature-StoneSentinel-earth.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-earth.glb) | 268 |
| deep-fracture-creature-StoneSentinel-fire | [Open](source/deep-fracture-creature-StoneSentinel-fire.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-fire.glb) | 268 |
| deep-fracture-creature-StoneSentinel-frost | [Open](source/deep-fracture-creature-StoneSentinel-frost.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-frost.glb) | 268 |
| deep-fracture-creature-StoneSentinel-radiance | [Open](source/deep-fracture-creature-StoneSentinel-radiance.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-radiance.glb) | 268 |
| deep-fracture-creature-StoneSentinel-seidr | [Open](source/deep-fracture-creature-StoneSentinel-seidr.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-seidr.glb) | 268 |
| deep-fracture-creature-StoneSentinel-spirit | [Open](source/deep-fracture-creature-StoneSentinel-spirit.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-spirit.glb) | 268 |
| deep-fracture-creature-StoneSentinel-storm | [Open](source/deep-fracture-creature-StoneSentinel-storm.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-storm.glb) | 268 |
| deep-fracture-creature-StoneSentinel-venom | [Open](source/deep-fracture-creature-StoneSentinel-venom.blend) | [Open](glb/deep-fracture-creature-StoneSentinel-venom.glb) | 268 |
| deep-fracture-crystal-golem-visual-earth | [Open](source/deep-fracture-crystal-golem-visual-earth.blend) | [Open](glb/deep-fracture-crystal-golem-visual-earth.glb) | 444 |
| deep-fracture-crystal-golem-visual-fire | [Open](source/deep-fracture-crystal-golem-visual-fire.blend) | [Open](glb/deep-fracture-crystal-golem-visual-fire.glb) | 444 |
| deep-fracture-crystal-golem-visual-frost | [Open](source/deep-fracture-crystal-golem-visual-frost.blend) | [Open](glb/deep-fracture-crystal-golem-visual-frost.glb) | 444 |
| deep-fracture-crystal-golem-visual-radiance | [Open](source/deep-fracture-crystal-golem-visual-radiance.blend) | [Open](glb/deep-fracture-crystal-golem-visual-radiance.glb) | 444 |
| deep-fracture-crystal-golem-visual-seidr | [Open](source/deep-fracture-crystal-golem-visual-seidr.blend) | [Open](glb/deep-fracture-crystal-golem-visual-seidr.glb) | 444 |
| deep-fracture-crystal-golem-visual-spirit | [Open](source/deep-fracture-crystal-golem-visual-spirit.blend) | [Open](glb/deep-fracture-crystal-golem-visual-spirit.glb) | 444 |
| deep-fracture-crystal-golem-visual-storm | [Open](source/deep-fracture-crystal-golem-visual-storm.blend) | [Open](glb/deep-fracture-crystal-golem-visual-storm.glb) | 444 |
| deep-fracture-crystal-golem-visual-venom | [Open](source/deep-fracture-crystal-golem-visual-venom.blend) | [Open](glb/deep-fracture-crystal-golem-visual-venom.glb) | 444 |
| deep-fracture-deep-colossus-visual-earth | [Open](source/deep-fracture-deep-colossus-visual-earth.blend) | [Open](glb/deep-fracture-deep-colossus-visual-earth.glb) | 740 |
| deep-fracture-deep-colossus-visual-fire | [Open](source/deep-fracture-deep-colossus-visual-fire.blend) | [Open](glb/deep-fracture-deep-colossus-visual-fire.glb) | 740 |
| deep-fracture-deep-colossus-visual-frost | [Open](source/deep-fracture-deep-colossus-visual-frost.blend) | [Open](glb/deep-fracture-deep-colossus-visual-frost.glb) | 740 |
| deep-fracture-deep-colossus-visual-radiance | [Open](source/deep-fracture-deep-colossus-visual-radiance.blend) | [Open](glb/deep-fracture-deep-colossus-visual-radiance.glb) | 740 |
| deep-fracture-deep-colossus-visual-seidr | [Open](source/deep-fracture-deep-colossus-visual-seidr.blend) | [Open](glb/deep-fracture-deep-colossus-visual-seidr.glb) | 740 |
| deep-fracture-deep-colossus-visual-spirit | [Open](source/deep-fracture-deep-colossus-visual-spirit.blend) | [Open](glb/deep-fracture-deep-colossus-visual-spirit.glb) | 740 |
| deep-fracture-deep-colossus-visual-storm | [Open](source/deep-fracture-deep-colossus-visual-storm.blend) | [Open](glb/deep-fracture-deep-colossus-visual-storm.glb) | 740 |
| deep-fracture-deep-colossus-visual-venom | [Open](source/deep-fracture-deep-colossus-visual-venom.blend) | [Open](glb/deep-fracture-deep-colossus-visual-venom.glb) | 740 |
| deep-fracture-entrance | [Open](source/deep-fracture-entrance.blend) | [Open](glb/deep-fracture-entrance.glb) | 96 |
| deep-fracture-obelisk-warden-visual-earth | [Open](source/deep-fracture-obelisk-warden-visual-earth.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-earth.glb) | 436 |
| deep-fracture-obelisk-warden-visual-fire | [Open](source/deep-fracture-obelisk-warden-visual-fire.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-fire.glb) | 436 |
| deep-fracture-obelisk-warden-visual-frost | [Open](source/deep-fracture-obelisk-warden-visual-frost.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-frost.glb) | 436 |
| deep-fracture-obelisk-warden-visual-radiance | [Open](source/deep-fracture-obelisk-warden-visual-radiance.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-radiance.glb) | 436 |
| deep-fracture-obelisk-warden-visual-seidr | [Open](source/deep-fracture-obelisk-warden-visual-seidr.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-seidr.glb) | 436 |
| deep-fracture-obelisk-warden-visual-spirit | [Open](source/deep-fracture-obelisk-warden-visual-spirit.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-spirit.glb) | 436 |
| deep-fracture-obelisk-warden-visual-storm | [Open](source/deep-fracture-obelisk-warden-visual-storm.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-storm.glb) | 436 |
| deep-fracture-obelisk-warden-visual-venom | [Open](source/deep-fracture-obelisk-warden-visual-venom.blend) | [Open](glb/deep-fracture-obelisk-warden-visual-venom.glb) | 436 |
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
| elemental-focus | [Open](source/elemental-focus.blend) | [Open](glb/elemental-focus.glb) | 8 |
| fate-crystal | [Open](source/fate-crystal.blend) | [Open](glb/fate-crystal.glb) | 336 |
| fate-shard | [Open](source/fate-shard.blend) | [Open](glb/fate-shard.glb) | 56 |
| furniture-crystal-bed | [Open](source/furniture-crystal-bed.blend) | [Open](glb/furniture-crystal-bed.glb) | 124 |
| furniture-crystal-bench | [Open](source/furniture-crystal-bench.blend) | [Open](glb/furniture-crystal-bench.glb) | 120 |
| furniture-crystal-divider | [Open](source/furniture-crystal-divider.blend) | [Open](glb/furniture-crystal-divider.glb) | 188 |
| furniture-crystal-throne | [Open](source/furniture-crystal-throne.blend) | [Open](glb/furniture-crystal-throne.glb) | 192 |
| furniture-geo-desk | [Open](source/furniture-geo-desk.blend) | [Open](glb/furniture-geo-desk.glb) | 152 |
| furniture-geode-chair | [Open](source/furniture-geode-chair.blend) | [Open](glb/furniture-geode-chair.glb) | 156 |
| furniture-geode-pedestal | [Open](source/furniture-geode-pedestal.blend) | [Open](glb/furniture-geode-pedestal.glb) | 232 |
| furniture-geode-table | [Open](source/furniture-geode-table.blend) | [Open](glb/furniture-geode-table.glb) | 248 |
| furniture-lapidary-cabinet | [Open](source/furniture-lapidary-cabinet.blend) | [Open](glb/furniture-lapidary-cabinet.glb) | 176 |
| furniture-mineral-shelf | [Open](source/furniture-mineral-shelf.blend) | [Open](glb/furniture-mineral-shelf.glb) | 280 |
| geode-sample | [Open](source/geode-sample.blend) | [Open](glb/geode-sample.glb) | 186 |
| Magenheim_Staff_Earth_Advanced | [Open](source/Magenheim_Staff_Earth_Advanced.blend) | [Open](glb/Magenheim_Staff_Earth_Advanced.glb) | 376 |
| Magenheim_Staff_Earth_Crystal | [Open](source/Magenheim_Staff_Earth_Crystal.blend) | [Open](glb/Magenheim_Staff_Earth_Crystal.glb) | 256 |
| Magenheim_Staff_Earth_Master | [Open](source/Magenheim_Staff_Earth_Master.blend) | [Open](glb/Magenheim_Staff_Earth_Master.glb) | 648 |
| Magenheim_Staff_Earth_Simple | [Open](source/Magenheim_Staff_Earth_Simple.blend) | [Open](glb/Magenheim_Staff_Earth_Simple.glb) | 200 |
| Magenheim_Staff_Fire_Advanced | [Open](source/Magenheim_Staff_Fire_Advanced.blend) | [Open](glb/Magenheim_Staff_Fire_Advanced.glb) | 400 |
| Magenheim_Staff_Fire_Crystal | [Open](source/Magenheim_Staff_Fire_Crystal.blend) | [Open](glb/Magenheim_Staff_Fire_Crystal.glb) | 228 |
| Magenheim_Staff_Fire_Master | [Open](source/Magenheim_Staff_Fire_Master.blend) | [Open](glb/Magenheim_Staff_Fire_Master.glb) | 636 |
| Magenheim_Staff_Fire_Simple | [Open](source/Magenheim_Staff_Fire_Simple.blend) | [Open](glb/Magenheim_Staff_Fire_Simple.glb) | 208 |
| Magenheim_Staff_Storm_Advanced | [Open](source/Magenheim_Staff_Storm_Advanced.blend) | [Open](glb/Magenheim_Staff_Storm_Advanced.glb) | 328 |
| Magenheim_Staff_Storm_Crystal | [Open](source/Magenheim_Staff_Storm_Crystal.blend) | [Open](glb/Magenheim_Staff_Storm_Crystal.glb) | 228 |
| Magenheim_Staff_Storm_Master | [Open](source/Magenheim_Staff_Storm_Master.blend) | [Open](glb/Magenheim_Staff_Storm_Master.glb) | 648 |
| Magenheim_Staff_Storm_Simple | [Open](source/Magenheim_Staff_Storm_Simple.blend) | [Open](glb/Magenheim_Staff_Storm_Simple.glb) | 208 |
| norn-spindle | [Open](source/norn-spindle.blend) | [Open](glb/norn-spindle.glb) | 200 |
| passage-stone | [Open](source/passage-stone.blend) | [Open](glb/passage-stone.glb) | 396 |
| rune-engraver | [Open](source/rune-engraver.blend) | [Open](glb/rune-engraver.glb) | 184 |
| runed-totem | [Open](source/runed-totem.blend) | [Open](glb/runed-totem.glb) | 468 |
| runic-keelstone | [Open](source/runic-keelstone.blend) | [Open](glb/runic-keelstone.glb) | 228 |
| seidr-ritual-focus | [Open](source/seidr-ritual-focus.blend) | [Open](glb/seidr-ritual-focus.glb) | 408 |
| sentinel-ammo-earth | [Open](source/sentinel-ammo-earth.blend) | [Open](glb/sentinel-ammo-earth.glb) | 28 |
| sentinel-ammo-fire | [Open](source/sentinel-ammo-fire.blend) | [Open](glb/sentinel-ammo-fire.glb) | 28 |
| sentinel-ammo-frost | [Open](source/sentinel-ammo-frost.blend) | [Open](glb/sentinel-ammo-frost.glb) | 28 |
| sentinel-ammo-radiance | [Open](source/sentinel-ammo-radiance.blend) | [Open](glb/sentinel-ammo-radiance.glb) | 28 |
| sentinel-ammo-seidr | [Open](source/sentinel-ammo-seidr.blend) | [Open](glb/sentinel-ammo-seidr.glb) | 28 |
| sentinel-ammo-spirit | [Open](source/sentinel-ammo-spirit.blend) | [Open](glb/sentinel-ammo-spirit.glb) | 28 |
| sentinel-ammo-storm | [Open](source/sentinel-ammo-storm.blend) | [Open](glb/sentinel-ammo-storm.glb) | 28 |
| sentinel-ammo-venom | [Open](source/sentinel-ammo-venom.blend) | [Open](glb/sentinel-ammo-venom.glb) | 28 |
| spirit-fetish-raven | [Open](source/spirit-fetish-raven.blend) | [Open](glb/spirit-fetish-raven.glb) | 180 |
| spirit-fetish-warrior | [Open](source/spirit-fetish-warrior.blend) | [Open](glb/spirit-fetish-warrior.glb) | 124 |
| spirit-fetish-wolf | [Open](source/spirit-fetish-wolf.blend) | [Open](glb/spirit-fetish-wolf.glb) | 168 |
| staff-frost-advanced | [Open](source/staff-frost-advanced.blend) | [Open](glb/staff-frost-advanced.glb) | 360 |
| staff-frost-crystal | [Open](source/staff-frost-crystal.blend) | [Open](glb/staff-frost-crystal.glb) | 228 |
| staff-frost-master | [Open](source/staff-frost-master.blend) | [Open](glb/staff-frost-master.glb) | 608 |
| staff-frost-simple | [Open](source/staff-frost-simple.blend) | [Open](glb/staff-frost-simple.glb) | 208 |
| staff-radiance-advanced | [Open](source/staff-radiance-advanced.blend) | [Open](glb/staff-radiance-advanced.glb) | 472 |
| staff-radiance-crystal | [Open](source/staff-radiance-crystal.blend) | [Open](glb/staff-radiance-crystal.glb) | 272 |
| staff-radiance-master | [Open](source/staff-radiance-master.blend) | [Open](glb/staff-radiance-master.glb) | 640 |
| staff-radiance-simple | [Open](source/staff-radiance-simple.blend) | [Open](glb/staff-radiance-simple.glb) | 172 |
| staff-seidr-advanced | [Open](source/staff-seidr-advanced.blend) | [Open](glb/staff-seidr-advanced.glb) | 404 |
| staff-seidr-crystal | [Open](source/staff-seidr-crystal.blend) | [Open](glb/staff-seidr-crystal.glb) | 252 |
| staff-seidr-master | [Open](source/staff-seidr-master.blend) | [Open](glb/staff-seidr-master.glb) | 944 |
| staff-seidr-simple | [Open](source/staff-seidr-simple.blend) | [Open](glb/staff-seidr-simple.glb) | 184 |
| staff-spirit-advanced | [Open](source/staff-spirit-advanced.blend) | [Open](glb/staff-spirit-advanced.glb) | 5,272 |
| staff-spirit-crystal | [Open](source/staff-spirit-crystal.blend) | [Open](glb/staff-spirit-crystal.glb) | 1,648 |
| staff-spirit-master | [Open](source/staff-spirit-master.blend) | [Open](glb/staff-spirit-master.glb) | 12,016 |
| staff-spirit-simple | [Open](source/staff-spirit-simple.blend) | [Open](glb/staff-spirit-simple.glb) | 892 |
| staff-venom-advanced | [Open](source/staff-venom-advanced.blend) | [Open](glb/staff-venom-advanced.glb) | 448 |
| staff-venom-crystal | [Open](source/staff-venom-crystal.blend) | [Open](glb/staff-venom-crystal.glb) | 312 |
| staff-venom-master | [Open](source/staff-venom-master.blend) | [Open](glb/staff-venom-master.glb) | 584 |
| staff-venom-simple | [Open](source/staff-venom-simple.blend) | [Open](glb/staff-venom-simple.glb) | 208 |
