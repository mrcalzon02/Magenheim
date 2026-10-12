# Fungal Forest Capcrawler — plated leg sculpt (2026-10-11)

**Scope:** Existing Capcrawler only. Replaces the 16 generic cylinders forming `Capcrawler_{L,R}_Leg{1..4}{A,B}` with closed tapered leg plates. Preserves eight legs/four bilateral pairs, hip/knee/foot anchors, existing `Coxa` and `Tibia` bone bindings, `CapcrawlerLegPlate` material, original object names, foot pads, terminal claws, mandibles, gills, carapace, four bracket shelves, three spore nodules and all authored actions. No stations, resources, equipment or new creature identities.

**Geometry:** Each segment uses 20 circumferential sectors and seven axial rings, with a plated external keel, flared upper-knee shoulder, and tapered lower tibia. Closed manifold topology uses 142 vertices and 280 triangles per segment: 4,480 triangles across 16 segments, compared with approximately 960 triangles from the previous 16-sided capped Blender cylinders. Segment midpoint origins and endpoint cap centers remain unchanged. A/B segments stay independently weighted to the original leg bones; there is no shared deformation across joints.

**UV/material:** Explicit `CapcrawlerUV` islands separate the leg sidewall from proximal and distal end caps. The existing leg-plate albedo, normal and roughness material identities remain unchanged; texture bitmaps are not regenerated. No new emission or silhouette appendages are introduced.

**Offline QA:** Seven new pure-Python tests plus 13 existing carapace/shelf tests passed (20/20). All 16 segments were checked for exact anchors, left/right mirroring, closed outward topology, positive volume, nondegenerate triangles, silhouette bounds and UV separation. OBJ round-trip and source-coordinate dorsal/lateral QA generated. Blender source regeneration and Valheim runtime inspection were **not** performed.

**Quality floor:** The installed Valheim Seeker donor triangle count and texture dimensions have not been measured. `tools/verify-underworld-capcrawler-donor-floor.py` remains the fail-closed acceptance gate using a fresh Seeker donor export. Triangle growth is not evidence that the donor floor is met.

**Pending acceptance:** Regenerate `assets/models/source/underworld-creature-capcrawler.blend`, run `tools/verify-underworld-capcrawler.py` and donor-floor verification in Blender, export GLB/runtime assets, compare approved concept artwork (not found in the inspected repository), inspect limb deformation and full silhouette at gameplay distance, then test in Valheim. Existing binary assets remain unchanged until those gates pass.
