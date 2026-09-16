# Socket UI and generated texture fidelity pass — 2026-09-15

## Intent

Verify that Magenheim socketing has a real player-facing interface and visual treatment, then raise the minimum texture quality for generated blocks, furniture, staffs, weapons, artifacts and other owned procedural equipment so flat one-pixel/low-resolution stand-ins are not accepted as finished presentation.

## Reconciliation

This work overlapped another active repair stream. The first texture-fidelity commit (`1b6847d3307d75b509b3b03a463ad908f9ef4160`) upgraded the old socket skin and shared surface authority. Immediately afterward `main` advanced through `543805f60a1baecf742e1642f81f6d06dfd80b61`, which rebuilt socketing directly on Valheim's `InventoryGui`, and `3629c3a68e019481c99b49acff0e92393481ce21`, which removed the now-obsolete IMGUI skin patch. The native UI implementation is newer authority and is preserved here; the removed skin is not reintroduced.

## Socket interface audit result

The socketing system now has a concrete native interface rather than only transaction code or a debug-style window.

`SocketWorkstationOverlay` is hosted under Valheim's active `InventoryGui` and creates a native Unity UI panel using `RectTransform`, `Image`, `Mask`, `ScrollRect`, `VerticalLayoutGroup`, `ContentSizeFitter`, text and button components. It copies active crafting-panel/button/text presentation from the game rather than maintaining a detached IMGUI skin.

The interface provides:

- individual equipment selection;
- category/origin information;
- installed/unlocked socket counts and policy maximums;
- opening a socket;
- crystal installation;
- crystal extraction when requirements are met;
- multiplayer/server-authority and stale-state diagnostics;
- operation status text.

The same existing server-authoritative socket transaction paths remain responsible for mutation. The UI rebuild changes presentation and interaction composition, not socket authority.

## Shared world/equipment texture repair

The first shared texture authority was directionally correct but generated only 64x64 grayscale maps. A deeper audit also found that many owned procedural mesh builders create vertices and triangles without UV0 coordinates. Giving such a material a texture is insufficient because the geometry can sample effectively one texel across the entire object.

`GeneratedSurfaceTextures` now raises the shared baseline to 256x256 with mipmaps, trilinear filtering and 4x anisotropy. Crystal, stone, timber, metal, cloth, bone, liquid, leather and generic surfaces use multi-scale deterministic detail instead of flat tint or visibly magnified tiny patterns.

## Post-registration visual reconciliation

After Jotunn finishes prefab registration, the shared authority now performs an owned-only quality pass.

For Magenheim-owned materials, it replaces only obvious placeholder surfaces:

- no texture;
- Unity's one-pixel `Texture2D.whiteTexture`;
- an older `magenheim.surface.*` generated surface that should be rebound to the current 256px authority;
- another 1-4px placeholder texture.

Authored/file-backed textures with real dimensions are preserved, including the Earth asset pipeline. Likewise, an inherited non-placeholder vanilla texture is not silently overwritten; that is model-specific debt and must be repaired deliberately at its authoritative builder.

For readable Magenheim-owned procedural meshes, the pass checks UV0. A mesh with absent or degenerate UV0 receives triangle-projected UV coordinates. The repair preserves geometry, submesh separation, existing normals, tangents and vertex colors, skips skinned meshes, and does not touch foreign meshes. Triangle vertices are split only where necessary so faces can carry stable projection without shared-vertex UV conflicts.

This means residual procedural blocks/equipment no longer remain visually flat merely because a builder forgot UVs or left a one-pixel placeholder behind.

## Scope boundary

This repair raises the rendering floor; it does not claim that every model is final art. Geometry that genuinely looks like a crude caricature still needs model-specific silhouette, proportion and detail refinement. Inappropriate inherited vanilla textures that are larger than placeholder size also require explicit per-model repair rather than an indiscriminate global override.

Several model-specific repairs had already landed before this pass, including generated surface migration for elemental staffs, furniture, crystal architecture, banners, beds, Ice Box, world/capstone artifacts, geology decor, Crystal Sentinel and crystal weapons. Crystal weapon geometry also received winding repair. During reconciliation, the Crystal Bed, Ice Box and Crystal Enchanting Dais were additionally migrated toward the new canonical Unity mesh primitives. Those individual repairs remain authoritative and are complemented rather than replaced by the shared fallback.

## Validation boundary

Repository source and commit ancestry are verified. This connector environment does not run the installed Valheim rendering profile, so compilation and screenshot-level runtime acceptance are not claimed.

Runtime acceptance still needs to confirm:

- socket panel readability and layout at common resolutions/UI scales;
- native button/text styling actually resolves from the active InventoryGui;
- visible texture variation on representative crystal, stone, timber and metal pieces;
- no severe seams/orientation errors on projected curved/prismatic geometry;
- imported Earth assets retain authored UVs/textures;
- residual white/null owned placeholder materials are rebound after prefab registration;
- generated vertex growth remains acceptable in dense construction scenes.

## Next visual slice

Continue with model-specific fidelity rather than stacking another generic mutator: identify Magenheim-owned objects still inheriting inappropriate non-placeholder vanilla materials, finish remaining inside-out/winding migrations, and refine the worst low-poly silhouettes/proportions one family at a time. The native socket UI is now an implementation to refine and test, not a missing interface.
