# Ashmite cephalic vent shield — geometry-only candidate (2026-10-09)

Scope: established Sulfurous Wastes Ashmite only. The generic ellipsoidal `Ashmite_Head` authoring primitive is replaced with a closed, tapered cephalic shield retaining the same Head origin (0, 0.105, 0.070), nominal radii (0.080, 0.072, 0.048), existing bilateral eye placements, paired mandible sockets, central vent scraper, heat-chitin PBR material, `Head` bone and HOST-SWARM-HEXAPOD rig. It introduces no new appendages or creatures.

The source mesh has 1,410 vertices, 1,472 polygons and 2,816 triangulated faces, with physical brow buttresses, a shallow median heat suture, a low anterior scraping lip and UVs. The original Blender ico-sphere count was not independently measured; no before/after triangle increase is claimed.

Offline acceptance: six cephalic geometry tests, manifold/outward winding/positive volume, anatomical envelope, bilateral symmetry, posterior-to-anterior taper, eye-anchor proximity, surface relief, UV integrity and malformed input rejection. A separate orthographic OBJ preview provides top/side inspection against the former ellipsoid envelope and schematic existing anatomy, not the approved concept artwork. Integration patch requires exact `38a7b13` author and verifier blob baselines; the sulfur-ridge commit must first be integrated or used as parent.

Not production-accepted: Blender source regeneration, animation deformation, actual Tick donor polygon and texture measurement, complete approved-concept silhouette comparison, Valheim runtime inspection and profile installation are outstanding. Do not use the preview OBJ as a Valheim runtime asset.
