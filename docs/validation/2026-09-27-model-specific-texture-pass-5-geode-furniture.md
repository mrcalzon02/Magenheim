# Model-specific texture pass 5 — Geode furniture — 2026-09-27

This pass continues the corrected production rule established by the Crystal Bench and Geode Table: the texture is authored against the model that actually ships, not presented as a donor sheet or preview and then mistaken for a finished asset.

## Furniture Geode Chair

All 11 committed mesh objects now own a separate 256x256 grayscale/tint-compatible texture. Pixels are rasterized through each object's committed UV triangles, value breakup is derived from the mesh orientation and position, and a five-pixel bleed surrounds sampled islands. Stone, timber, iron and crystal retain distinct surface tooth without inventing large atlas features that would fragment across the smart-projected islands.

The audit strip `assets/models/previews/furniture-geode-chair-uv-texture-audit.png` places the exact baked maps side by side and overlays the committed UV triangle edges in magenta.

## Furniture Geode Pedestal

All 9 committed mesh objects now follow the same rule: base, column, iron cap, geode shell and five crystal pieces each reference their own 256x256 UV-conformed map. The audit strip `assets/models/previews/furniture-geode-pedestal-uv-texture-audit.png` is generated from the same bytes committed to the runtime package.

`assets/models/texture-overrides.json` binds model id + object name to these files so a normal Blender export preserves the per-object assignments. `tools/verify-authored-surface-coverage.py` now makes all 20 bindings and their 256x256 dimensions package admission requirements.

This pass does not claim an in-game visual test. It proves committed UV binding, authored texture dimensions, runtime references and exporter persistence; live rendering remains part of the normal closeout/world test.
