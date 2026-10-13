# Rift Titan primary r3 — faceted rock surfaces (2026-10-13)

**Reference inspected:** adopted `docs/art/creature-concepts/2026-10-03/the-rift-titan--thumb.jpg` and the full-image provenance in the concept manifest, compared with the existing r3 front and three-quarter render plates. The concept depicts craggy, faceted, layered stone masses; the r3 study presents overly planar hexagonal slabs.

**Authoring change:** preserve each existing plate's six-sided outer outline, rig binding, pivot, material assignment, and location, but replace the two flat cap faces with shallow, offset front/back ridge vertices. This adds four triangles per plate, makes actual faceted surfaces instead of relying on flat-texture shading, and retains the deliberately open chest cage. Existing shoulder-depth reductions and bone-aligned limb strata from the immediately preceding candidate remain intact.

**Cost estimate:** 47 plate calls x 4 added triangles = 188 new triangles. Earlier measured study 1,480 -> **projected 1,668**; StoneGolem donor 1,200 and boss-art soft ceiling 1,800. This is a **source estimate, not a regenerated triangle measurement**. The ordinary elite triangle band is not the governing boss-study cap. Four existing 512px maps are unchanged; no LOD, texture or runtime binary was regenerated.

**Offline evidence:** six Blender-independent geometry regressions passed (closed edge incidence, winding/positive volume, preserved perimeter, reduced shoulder depth, nonplanar rock faces, invalid input rejection). This change does not establish concept silhouette acceptance: full-size art/Blender renders, retained action clearance, actual triangle count, UV atlas parity, portable GLB, native loader and performance remain unverified.

**Scope:** Rift Titan boss only. No small creatures, stations, resources, equipment or environment assets changed.
