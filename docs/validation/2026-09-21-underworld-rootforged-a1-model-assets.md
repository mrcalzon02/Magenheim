# 2026-09-21 — Underworld Rootforged A1 model payload

Status: **real asset payload committed; runtime placement admission not claimed**

This slice closes the model-asset gap behind the eleven A0 Underworld construction definitions. The saved Blender sources already existed on `main`; this change adds the missing game-readable runtime meshes, GLB 2.0 interchange meshes and owned material textures. No Unity primitive fallback is introduced.

The geometry follows the existing Rootforged authoring language in `tools/author-rootforged-placeables.py`: Understone foundation/plinth masses with root inlays, three-strand Worldroot beams and pillars, and forged-iron collar members on reinforced beams and arch ribs. Declared A0 dimensions and stable identities remain unchanged.

## Inventory

- `rootforged-understone-foundation-2x2`: 5 parts, 580 triangles
- `rootforged-great-column-plinth`: 5 parts, 580 triangles
- `rootforged-worldroot-beam-2m`: 3 parts, 1,800 triangles
- `rootforged-worldroot-beam-4m`: 3 parts, 2,952 triangles
- `rootforged-worldroot-beam-8m`: 3 parts, 5,832 triangles
- `rootforged-worldroot-pillar-2m`: 3 parts, 1,800 triangles
- `rootforged-worldroot-pillar-4m`: 3 parts, 2,952 triangles
- `rootforged-worldroot-pillar-8m`: 3 parts, 5,832 triangles
- `rootforged-iron-banded-worldroot-beam-4m`: 15 parts, 3,336 triangles
- `rootforged-rootforged-arch-rib-4m`: 15 parts, 3,336 triangles
- `rootforged-rootforged-arch-rib-8m`: 15 parts, 6,216 triangles

Total: **35,216 triangles across 11 models**.

Four owned 256x256 diffuse material textures accompany the set: Understone, Worldroot bark, Worldroot heartwood and forged iron. Roughness and metallic response remain material parameters, consistent with the one-image-per-material model pipeline.

## Asset checks performed

Before the tree was committed, the generated mesh payload was checked for non-empty parts, valid indexed triangles, finite unit normals, non-degenerate faces, face/normal winding agreement and per-vertex UV coverage. The GLB payloads contain the required POSITION, NORMAL and TEXCOORD_0 streams. Model catalog coverage is raised from 283 to 294 sets and records SHA-256 hashes for the existing Blender source plus the new runtime and GLB payloads.

## Boundary

This is the model/texture layer. It does **not** claim that the pieces are already visible in the Hammer menu, that snap/support behavior has passed a live Valheim test, or that the normal local Blender export/closeout has been replayed. The checked-in `.blend` files remain editable authoring authority; a later Blender round-trip may canonicalize binary serialization without changing the A0 identities or geometry contract.
