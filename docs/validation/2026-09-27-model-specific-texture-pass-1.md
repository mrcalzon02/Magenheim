# Model-specific texture pass 1 — 2026-09-27

This pass corrects the earlier overreach where generated showcase boards invented props and presentation
art that were not Magenheim assets. Those boards are not production inputs.

The first production slice is grounded only in existing model/material identities:

- furniture-crystal-bench: 8/8 parts moved from generic fallback to four 256px model-specific maps
  (dark-stone, wood, iron, crystal).
- furniture-geode-table: 9/9 parts moved from generic fallback to three 256px model-specific maps
  (dark-stone, wood, iron).

The maps are deliberately restrained and tint-compatible. Both models use smart-projected UV islands,
so large illustrative features would fragment across seams. Material character is carried by
Valheim-scale surface breakup, short scratches/pitting/mineral speckle and the model's existing Base
Color, not by painting an unrelated high-resolution prop atlas.

The runtime payloads now reference the seven files explicitly. Blender source migration uses exact
material-name overrides before falling back to the shared family texture, so subsequent exports preserve
this model-specific decision. Existing non-fallback authored images still win.

Admission checks require the seven maps to remain 256x256 and require all 17 affected runtime parts to
reference the intended model-specific files.
