# Authored model-surface gap repair — 2026-09-27

## Finding

The texture audit is real: 100 models / 1,303 material parts were exported with a null texture after
obsolete `magenheim.surface.*` runtime patterns were removed from the Blender-derived payloads.
Removing those old bakes fixed a semantic-classification defect, but the first replacement library
was only a 256x256 procedural diagnostic set. It proved the binding path worked; it did not meet the
visual quality bar for production Magenheim assets and is now superseded.

## Production repair

Nine production material-family fallbacks now live under `assets/models/textures/`: stone, timber,
metal, cloth, bone, liquid, leather, crystal, and generic. Each active texture is a 1024x1024
image-authored, painterly game material derived from the approved replacement artwork rather than a
mathematical placeholder pattern.

The families are intentionally distinct in material language:

- stone: large fractured slabs, chipped edges, gravel, moss, and lichen;
- timber: weathered plank grain, knots, splitting, and crevice growth;
- metal: forged/riveted plates, scratches, oxidation, and edge wear;
- cloth: coarse woven fibers with irregular weathering;
- bone: aged ivory, pores, striation, staining, and cracks;
- leather: worn hide, wrinkles, scarring, creases, and value variation;
- liquid: authored ripple/caustic structure;
- crystal: hand-painted faceting and mineral fracture structure;
- generic: packed dirt, rubble, aggregate, and small stone breakup.

Crystal and liquid use neutralized runtime copies so existing elemental/material tinting remains in
control instead of every crystal or fluid inheriting the blue/teal source palette. The authored
structure is preserved; only the production color treatment is neutralized for shader tinting.

The active 1024px PNGs are indexed production copies to keep repository and package size practical
while preserving the visible medium-scale brushwork and material definition.

`ModelAssets` resolves only null exported texture slots to the semantic authored family. Explicit
texture references remain authoritative and are not replaced.

`GeneratedSurfaceTextures` now prefers the same file-backed authored families for Magenheim-owned
procedural materials and retains the historical procedural builders only as a missing-file fallback.

`tools/model_surface_authoring.py` and `tools/author-library-surfaces.py` provide the Blender
source migration. The normal exporter calls the same binder before export, so every exported source
it touches replaces blank/obsolete generated-bake image slots and then saves that repaired .blend.
A material that already has a real image is left alone.

## Admission gate

`tools/verify-authored-surface-coverage.py` requires all nine production maps, distinct SHA-256
content, exact 1024x1024 resolution, source contrast, and contrast that survives a 64x64 gameplay-
scale downsample. It also resolves every null runtime slot through the same semantic rules, rejects
missing explicit textures, and prevents the audited fallback population from growing above 100
models / 1,303 parts. The count may fall as Blender sources are migrated and re-exported; zero is
valid.

Static asset/runtime validation does not replace live in-game acceptance. Material readability,
UV behavior, tiling, and shader interaction still require visual testing in Valheim.
