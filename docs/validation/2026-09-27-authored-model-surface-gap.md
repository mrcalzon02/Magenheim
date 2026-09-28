# Authored model-surface gap repair — 2026-09-27

## Finding

The texture audit is real: 100 models / 1,303 material parts were exported with a null texture after
obsolete `magenheim.surface.*` runtime patterns were removed from the Blender-derived payloads.
Removing those old bakes fixed a semantic-classification defect, but the first replacement library
was only a 256x256 procedural diagnostic set. It proved the binding path worked; it did not meet the
visual quality bar for production Magenheim assets and is now superseded.

## Production repair

Nine accepted material-family source artworks are retained at 1024x1024 under
`assets/models/texture-source/`. They preserve the approved painterly source work for future
model-specific authoring.

The packaged shared fallbacks under `assets/models/textures/` are 256x256 neutral derivatives,
matching the established shared world/equipment floor. They preserve material structure at
Valheim-scale while allowing each model's Base Color to remain authoritative for hue. The fallback
is deliberately a rendering floor rather than final art: model-specific texture passes replace it
where an existing asset needs its own wear, surface language, or close-view treatment.

`ModelAssets` resolves only null exported texture slots to the semantic authored family. Explicit
texture references remain authoritative and are not replaced.

`GeneratedSurfaceTextures` now prefers the same file-backed authored families for Magenheim-owned
procedural materials and retains the historical procedural builders only as a missing-file fallback.

`tools/model_surface_authoring.py` and `tools/author-library-surfaces.py` provide the Blender
source migration. The normal exporter calls the same binder before export, so every exported source
it touches replaces blank/obsolete generated-bake image slots and then saves that repaired .blend.
A material that already has a real image is left alone.

## Admission gate

`tools/verify-authored-surface-coverage.py` requires all nine packaged fallback maps, distinct SHA-256
content, exact 256x256 resolution, source contrast, and contrast that survives a 64x64 gameplay-
scale downsample. It also resolves every null runtime slot through the same semantic rules, rejects
missing explicit textures, and prevents the audited fallback population from growing above 100
models / 1,303 parts. The count may fall as Blender sources are migrated and re-exported; zero is
valid.

Static asset/runtime validation does not replace live in-game acceptance. Material readability,
UV behavior, tiling, and shader interaction still require visual testing in Valheim.
