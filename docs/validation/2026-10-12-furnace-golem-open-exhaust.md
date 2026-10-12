# Furnace Golem functional exhaust silhouette — 2026-10-12

**Scope:** established Sulfurous Wastes Furnace Golem elite, continuing the primary-silhouette source refinement. This corrects two sealed chimney caps without inventing anatomy or touching non-creature assets.

The original author created capped cylinders with solid conical caps as exhaust stacks. The new `furnace_golem_vent_geometry.py` provides two hollow, closed-manifold tube profiles per side: a tapered iron stack and a flared slag cowl with an unobstructed bore, a physical annular lip, and independent UV islands for outer wall, dark inner shaft and both rims. Original `FG_VentStack_L/R` and `FG_VentCap_L/R` names and `Torso` rigid bindings are preserved. `VentFX_L/R` rig sockets now identify exhaust mouths for later smoke placement. Existing animations, furnace core and other anatomy remain unchanged.

**Offline measurements:** 16 circumferential sectors; each stack is 256 triangles and each cowl is 320 triangles, for **1,152 source triangles** across both sides. This is a component count, not a measured whole-creature runtime mesh. The last committed runtime binary still contains **984 triangles**; the earlier projected regenerated source count was approximately 2,348 triangles before this change. The elite LOD0 target is 14k–24k triangles, so this is not production-accepted.

**Validation:** six Blender-independent tests cover manifold edge incidence, outward winding/positive volume, open bores without sealing faces, cowl/stack overlap, flared outline, separate UV islands and invalid input rejection. The geometry tests passed locally. No Blender executable is available in the current environment: the .blend source, GLB, runtime JSON, material textures, LODs, donor comparison and Valheim behavior have **not** been regenerated or verified.

**Art authority:** an approved visual concept illustration was not recovered from accessible repository/Drive/Library searches. The correction follows the existing authored twin-stack anatomy and written elite specification, not a claimed visual-concept comparison. The normal 2048px elite source-texture expectation remains outstanding.
