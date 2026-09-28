# Model-specific texture pass 4 — Crystalline Ice Box — 2026-09-27

## Scope

The existing `crystalline-ice-box` model had 27/27 runtime parts with a null texture:

- black stone: 6 parts;
- ice crystal: 4 parts;
- iron: 8 parts;
- silver: 2 parts;
- frost crystal: 7 parts.

This pass addresses those actual material slots only.

## Treatment

Five 256x256 Valheim-scale maps are derived from the approved production material donors and tuned to
the Ice Box's existing material intents:

- `crystalline-ice-box-black-stone.png`
- `crystalline-ice-box-ice-crystal.png`
- `crystalline-ice-box-iron.png`
- `crystalline-ice-box-silver.png`
- `crystalline-ice-box-frost-crystal.png`

Crystal maps remain neutral so the model's existing frost/ice material colors and alpha values stay
authoritative. Iron and silver remain distinct through value/contrast treatment rather than oversized
painted detail that would fragment across smart-projected UV islands.

All 27 runtime parts now reference the five model-specific textures explicitly. The Blender
authoring/export helper owns exact overrides for the five existing material names, and the catalog
runtime SHA-256 is updated.

## Ratchet

Pass 3 left the debt at 88 models / 1,075 null-texture parts. This pass removes one model and 27 parts,
tightening the admission ceiling to **87 models / 1,048 null-texture parts**.

Static repository validation does not claim live Valheim rendering acceptance.
