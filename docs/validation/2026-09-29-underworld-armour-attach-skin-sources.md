# 2026-09-29 — Underworld armour attach_skin source pipeline

The six armour families now have an authored-source pipeline rather than a future-art placeholder:
Sporeweave, Palewater, Emberiron, Rimeward, Stoneanchor and Defiant, each with helmet, chest, legs and
cape sources.

The authoring script uses the canonical 53-name Valheim player bone order documented by existing
Valheim armour tooling: Hips/Spine/Spine1/Spine2/Neck/Head/Jaw; both shoulder/arm/forearm/hand and
finger chains; both upper-leg/leg/foot/toe chains. Every mesh object receives explicit ArmourUV,
an Armature modifier and non-zero weights to a canonical bone.

Each biome changes physical construction, not only colour:
- Sporeweave: woven/root structure with fungal nodes.
- Palewater: pale-fibre shells, flowstone structure and pearl accents.
- Emberiron: heavy heat-forged plate language with hot accents.
- Rimeward: narrow Rimesilver structure and Clear-Ice accents.
- Stoneanchor: Titanbone/shardstone masses with Fracture Crystal anchors.
- Defiant: Rotwood/bone structure with Carrion Amber reliquary accents.

`verify-underworld-armour.py` refuses missing pieces, wrong bone order, absent ArmourUV, missing
armature modifiers, unknown vertex groups or unweighted vertices.

The project model exporter can preserve the armature in GLB, but the lightweight runtime model JSON
does not serialize per-vertex skin weights. Therefore these twenty-four assets are deliberately NOT
claimed runtime-wearable yet. The runtime admission layer must consume an attach_skin-capable asset
path and apply Jotunn bone reordering before any recipe is enabled. Flattening these sources through
ModelAssets would destroy the very skinning contract this pass establishes.

This is the correct stopping boundary for source authoring; local Blender generation and in-game
male/female fit tests remain required before runtime admission.
