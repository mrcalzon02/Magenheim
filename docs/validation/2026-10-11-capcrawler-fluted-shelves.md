# Fungal Forest Capcrawler — bracket-shelf sculpt (2026-10-11)

**Scope:** Only the four established `Capcrawler_ShelfPlate_{L,R}{1,2}` meshes. Preserves names, Body bone, original anchors (±0.245, -0.16/0.04, 0.382), CapcrawlerCarapace material, crown, rim scutes, nodules, gills, mandibles, legs, animations and combat identity. No new creatures, stations or resources.

**Geometry:** Replaces four generic subdivision-2 flattened ico-spheres (320 triangles each, 1,280 combined) with closed mirrored bracket-fungus fans. Each has a tapered root, broad scalloped outward rim, twelve radial dorsal ribs, convex cuticle, downturned edge and ventral shell. Six radial rings and 64 sectors produce 770 vertices and 1,536 triangles per plate; 3,080 vertices and 6,144 triangles total.

**UV:** Separate dorsal, ventral and perimeter islands in `CapcrawlerUV`; existing carapace PBR material and texture files are unchanged. Texture size must still be measured against the actual installed Seeker donor.

**Offline QA:** Seven pure-Python shelf regressions and six existing shield regressions passed. Independently parsed OBJ: four objects, 3,080 vertices, 6,144 triangles. Orthographic QA compared shelves with the established shield and nodule positions. This is not a Blender render or approved concept overlay.

**Production acceptance pending:** Regenerate and verify `underworld-creature-capcrawler.blend` using Blender, inspect animation deformation, export GLB/runtime assets, measure the actual Seeker donor polygon and texture floor, compare against approved concept artwork and inspect in Valheim. No approved Capcrawler concept image was located in the inspected repository. Existing binary creature assets remain stale until regeneration.

**Integration:** This source patch changes only Capcrawler authoring, source geometry, production verifier, tests and validation documentation. A successful GitHub commit and main-head read-back are required before claiming integration.
