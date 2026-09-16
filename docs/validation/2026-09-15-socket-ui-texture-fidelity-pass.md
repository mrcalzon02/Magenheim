# Socket UI and generated texture fidelity pass — 2026-09-15

## Intent

Double-check that Magenheim socketing has a real user-facing interaction surface and texture treatment, then materially improve the shared texture path used by generated blocks, furniture, staffs, weapons and other procedural equipment instead of accepting low-resolution flat-color stand-ins as finished art.

## Socket interface audit

The native Magenheim socket backend does have an implemented interface. `SocketWorkstationOverlay` opens while `InventoryGui` is visible at an admitted Magenheim socket station and provides:

- individual equipment selection;
- visible socket count / policy maximum;
- socket opening;
- crystal installation;
- crystal extraction when the Faceting Wheel requirement is satisfied;
- server-authority/revalidation diagnostics and status text.

`MagenheimPlugin` constructs the overlay only when the Magenheim socket backend owns mutation and explicitly Harmony-patches `SocketWorkstationSkinPatch` in the same branch. The skin therefore is not dead code.

The audit also confirms that C7 remains a legitimate presentation debt: the current interface is still `OnGUI` / `GUILayout` rather than a reconstruction inside Valheim's native crafting panel. This pass does not falsely mark that native-UI rebuild complete.

## Socket UI texture repair

The existing skin did have generated textures, but they were only 24-48px for fields/panels and 32px for buttons. That was technically textured but visually too close to an enlarged debug skin.

`SocketWorkstationSkinPatch` now generates materially higher-fidelity UI surfaces:

- 256px mineral-grain panel texture;
- 128x64 normal/hover/active button textures;
- 128px field texture;
- 128x64 section texture;
- deterministic coarse + fine grain;
- optional mineral veining;
- brushed-metal style directional detail for controls;
- an inner bevel band that remains readable when stretched at common desktop resolutions;
- larger GUI borders so nine-slice-style stretching does not smear the edge treatment.

The authoritative socket mutation path remains untouched.

## Shared world/equipment texture repair

The existing `GeneratedSurfaceTextures` authority was a good direction but still generated only 64x64 grayscale maps. More importantly, a source audit of owned procedural meshes found that many mesh builders never authored UV0 coordinates. A material can own a texture and still look flat if the whole mesh samples the same texel.

The texture authority is therefore upgraded in two dimensions:

1. Surface maps are now 256x256 with mipmaps, trilinear filtering and 4x anisotropy. Crystal, stone, timber, metal, cloth, bone, liquid, leather and generic materials each receive multi-scale deterministic surface detail instead of magnified 64px patterns.
2. After Jotunn finishes prefab registration, the same authority scans only readable meshes whose names begin with the owned `magenheim.` namespace. If UV0 is absent or degenerate, it creates triangle-projected UV0 coordinates while preserving geometry, submeshes, existing normals, tangents and vertex colors. Imported/file-backed meshes with valid UVs are left unchanged, as are skinned meshes.

The projection repair duplicates triangle vertices only for an owned mesh that actually lacks usable UVs. This prevents shared-face UV seams from collapsing the texture back into a single sampled region and avoids rewriting foreign meshes.

## Scope boundary

This is a texture/UV framework repair, not a claim that every procedural object is now final-art quality. Low-poly geometry that genuinely looks like a caricature still needs model-specific refinement. Likewise, visual families that have not yet been migrated off direct one-pixel/legacy material construction must still be reconciled with `GeneratedSurfaceTextures`.

The new framework substantially raises the floor: any migrated generated family now has enough texture resolution and usable UV0 data for its material detail to be visible.

## Validation boundary

This connector pass verifies authoritative source composition and Git ancestry. It does not provide the installed Valheim/.NET rendering profile, so compilation and screenshot-level runtime acceptance are not claimed here.

Required runtime checks:

- open the Geologist/Enchanting Dais socket surface and verify panel/button/field detail at 1080p and 1440p;
- verify the interface remains readable under UI scaling;
- inspect at least one crystal, stone, timber and metal generated object to confirm visible texture variation rather than single-texel tint;
- inspect curved/prismatic generated equipment for UV seam or orientation defects;
- confirm imported Earth assets retain their authored UVs/textures;
- profile generated-mesh vertex growth on a dense build scene.

## Next actionable slice

Finish the residual R1 material migration by locating every remaining generated visual that still installs `Texture2D.whiteTexture` or clears its owned surface after `GeneratedSurfaceTextures.Apply`. Then continue R2 geometry migration and move C7 from the skinned `OnGUI` bridge into Valheim's crafting UI without changing the server-authoritative socket transaction path.
