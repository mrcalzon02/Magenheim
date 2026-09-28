# Model-specific texture pass 3 — Enchanting Dais and Crystal Hearth — 2026-09-27

## Scope

This pass continues the actual null-texture closure on existing Magenheim assets only.

- `crystal-enchanting-dais`: 62/62 parts were null-textured.
- `architecture-crystal-hearth`: 21/21 parts were null-textured.

No showcase objects, invented props, or unrelated presentation art are introduced.

## Dais treatment

The Dais now has four 256x256 model-specific texture roles:

- dark structural stone;
- lighter face stone;
- cold wrought iron;
- neutral crystal for the eight elemental node crystals plus the central crystal.

The elemental node colors remain in the existing material Base Color values. A shared neutral Dais
crystal map therefore carries surface character without baking eight redundant colored PNGs or
fighting the runtime material tint.

## Hearth treatment

The Hearth now has four 256x256 model-specific texture roles:

- hearth masonry stone;
- soot-dark stone;
- forged iron;
- neutral rainbow-crystal carrier used by all seven colored crystal segments.

Again, the seven rainbow colors remain authoritative in their existing material Base Color values.

## UV and Valheim-scale constraint

Both models inherit the project's smart-projected/fragmented UV reality. The authored derivatives
therefore keep their useful feature scale small and restrained. They are not AAA prop atlases and do
not paint large object-shaped motifs that would fragment across UV islands.

All 83 runtime parts now reference these model-specific files explicitly. The Blender authoring/export
helper owns exact overrides for the existing material names so a future source export preserves the
same bindings instead of falling back to the generic family maps.

The model catalog runtime SHA-256 values are updated for both payloads.

## Ratchet

Pass 2 left the texture debt at 90 models / 1,158 null-texture parts. This pass removes two more models
and 83 more parts, tightening the admission ceiling to **88 models / 1,075 null-texture parts**.

Static repository validation does not claim live Valheim rendering acceptance.
