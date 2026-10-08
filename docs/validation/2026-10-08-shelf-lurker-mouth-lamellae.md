# Shelf Lurker four-lobe feeding collar — 2026-10-08

Scope: creature authoring source only. This change replaces the four existing
primitive mouth-lobe spheres with four closed, downturned, physically fluted
feeding lamellae. The central mouth, eight hooked feeding barbs, six gripping
limbs, 18 talons, 24 shelf gills, rig identities and PBR material names remain
unchanged.

Each new lobe has 25 longitudinal rings and 32 circumferential samples:
800 vertices, 770 faces and 1,596 triangulated faces, up from the previous
1,280-triangle icosphere. Four lobes contribute 6,384 triangles, up from 5,120.
The lamellae keep a fourfold cardinal collar, 0.10 m root radius, 0.36 m outer
reach, downturned tips, six longitudinal surface flutes, explicit side UV seams
and planar nondegenerate cap UVs. All remain bound to Head and use the existing
ShelfLurkerMouthGill albedo/normal/roughness material.

Verification performed: four Blender-independent tests passed for all four
lobes (closed outward-wound topology, positive signed volume, anatomical
reach and downturned centerlines, actual flute relief, UV bounds/seams and
noncollapsed caps). Offline top/frontal projections included the eight
existing mouth-barb centerlines to check the feeding-aperture layout.

**Not accepted in production:** Blender is unavailable in this execution
environment, so the existing .blend and GLB were not regenerated. The
production verifier now requires this geometry and will fail on the old source
until regeneration. The actual Seeker donor triangle count and maximum donor
texture resolution have not been measured from a fresh in-game export;
approved concept-art silhouette comparison, animation deformation and Valheim
runtime acceptance remain outstanding. No donor-quality minimum is waived.

Rebuild sequence: run tools/author-underworld-shelf-lurker.py, then
tools/animate-underworld-shelf-lurker.py, then
tools/verify-underworld-shelf-lurker.py, and verify the fresh Seeker donor floor
with tools/verify-underworld-shelf-lurker-donor-floor.py before exporting
the model. Do not substitute GitHub Actions for local production validation.
