# Authored model-surface gap repair — 2026-09-27

## Finding

The texture audit is real, but the count has a specific history: 100 models / 1,303 material parts
were intentionally exported with a null texture after the old magenheim.surface.* 256px runtime
patterns were discovered baked into Blender sources. Those bakes predated the semantic-classifier
repair and froze the wrong surface class into many parts. Removing them stopped the
misclassification, but it also left those exported slots dependent on the procedural runtime repair.

A second quality problem remains even where that fallback executes: the shared generated grain is
too low-contrast to communicate material identity reliably at gameplay distance.

## Repair

Nine neutral authored albedo families now live under assets/models/textures/: stone, timber, metal,
cloth, bone, liquid, leather, crystal, and generic. They are grayscale by design so existing material
Base Color continues to own elemental/alignment tint. Their artwork carries materially different
medium-scale structure rather than relying on interchangeable fine grain.

ModelAssets resolves only null exported texture slots to the semantic authored family. Explicit
texture references remain authoritative and are not replaced.

GeneratedSurfaceTextures now prefers the same file-backed authored families for Magenheim-owned
procedural materials and retains the historical procedural builders only as a missing-file fallback.

tools/model_surface_authoring.py and tools/author-library-surfaces.py provide the Blender source
migration. The normal exporter calls the same binder before export, so every exported source it
touches replaces blank/obsolete generated-bake image slots and then saves that repaired .blend.
A material that already has a real image is left alone.

## Admission gate

tools/verify-authored-surface-coverage.py requires all nine maps, distinct SHA-256 content, 256x256
resolution, source contrast, and contrast that survives a 64x64 downsample. It also resolves every
null runtime slot through the same semantic rules, rejects missing explicit textures, and prevents
the audited fallback population from growing above 100 models / 1,303 parts. The count may fall as
Blender sources are migrated and re-exported; zero is valid.

This is static asset/runtime validation. In-game material readability still requires live visual
acceptance.
