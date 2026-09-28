# Model-specific texture pass 2 — Crystal Beds — 2026-09-27

## Scope

This pass returns to the actual texture-debt objective. It does not add showcase art, invented props,
or presentation boards. It touches the eight Crystal Bed models already present in the authoritative
model catalog:

- crystal-bed-earth
- crystal-bed-fire
- crystal-bed-frost
- crystal-bed-radiance
- crystal-bed-seidr
- crystal-bed-spirit
- crystal-bed-storm
- crystal-bed-venom

Before this pass each model had 16/16 runtime parts with a null texture: five stone parts, four iron
parts, one mineral-water part, and six crystal-growth parts. That is 128 real null-texture parts.

## Texture treatment

The family receives four 256x256 model-specific maps derived from the approved production material
donors:

- crystal-bed-stone.png
- crystal-bed-iron.png
- crystal-bed-mineral-water.png
- crystal-bed-crystal-growth.png

These are intentionally restrained Valheim-scale surfaces. The bed models use smart-projected UV
islands, so broad illustrative features would fragment into patchwork. The maps keep surface detail
below island scale while making the family materially distinct from the generic fallback.

The eight elemental variants share these neutral structural maps because their existing material Base
Color already carries Earth, Fire, Frost, Radiance, Seidr, Spirit, Storm, and Venom identity. Baking
those colors into the PNGs would duplicate data and make tinting fight the authored material values.

## Binding and source migration

All 128 runtime parts now reference one of the four Crystal Bed files explicitly. The Blender
authoring/export helper owns exact overrides for the four existing `magenheim.crystal-bed.*` material
names, so a future source export binds the same files instead of reverting to the generic family map.
Existing non-fallback authored images remain authoritative.

The model catalog runtime SHA-256 values were updated for all eight changed payloads.

## Ratchet

The original texture-debt ceiling was 100 models / 1,303 null-texture parts. Pass 1 removed two models
and 17 parts. This pass removes eight more models and 128 parts. The admission gate is therefore
ratcheted to **90 models / 1,158 null-texture parts**. A future change that restores any of this debt
fails verification instead of silently regressing.

Static repository validation does not claim live Valheim rendering acceptance.
