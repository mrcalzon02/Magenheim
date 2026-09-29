# Lantern Moth donor-replacement benchmark — 2026-09-28

Status: **authoring pipeline committed; generated source/runtime binding still open**.

The current runtime Magenheim_Underworld_Prototype_LanternMoth is still a uniformly scaled/tinted
Valheim Bat. That is not accepted final art and cannot satisfy the loading-screen inhabitant
contract. This pass establishes the first explicit donor-chassis/custom-body benchmark instead of
attempting another recolor.

Authoritative tools:

- tools/generate-underworld-lantern-moth-textures.py
- tools/author-underworld-lantern-moth.py
- tools/animate-underworld-lantern-moth.py
- tools/verify-underworld-lantern-moth.py

The source contract requires a 0.45–0.70 m four-wing silhouette, fuzzy thorax edge, paired
two-segment antennae, six legs, localized wing-cell emission, explicit UVs, HOST-INSECT-FLY rig,
and ten presentation actions. Texture construction is deliberately low-frequency; visual identity
comes from anatomy and broad material zones, not crinkled procedural detail.

This commit does **not** claim that a .blend, GLB, runtime model payload or in-game visual exists.
The connected GitHub environment cannot execute Blender. The next local asset step is to run the
four tools through tools/blender.ps1 where applicable, visually inspect the source, export it
through the model pipeline, then bind accepted geometry to the Lantern Moth runtime prefab while
retaining Valheim-owned movement/network authority. Only after that can a loading vignette depict
the finished Lantern Moth as an implemented inhabitant.
