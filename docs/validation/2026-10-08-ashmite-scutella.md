# Ashmite dorsal heat-scutella — authoring-source integration (2026-10-08)

Scope: established Sulfurous Wastes Ashmite only. Five flattened spherical dorsal plates are replaced by closed, heat-fractured 16-ring x 64-segment carapace scutella. Their existing anchors, dimensions, heat-chitin PBR material, abdomen/thorax bone assignments, six legs, paired mandibles, central vent scraper, ten sulfur ridges and HOST-SWARM-HEXAPOD rig are preserved.

Each new plate contains 1,026 vertices, 1,088 polygons and 2,048 triangles, replacing 1,280 triangles per former scute (10,240 versus 6,400 triangles across five scutes). This is a component-level source count, not the full creature or donor comparison. The raised axial crest, interrupted fracture valleys, perimeter scallops and bounded continuous radial UVs are physical geometry, not merely texture paint.

Offline tests: five Blender-free tests cover closed manifold topology, opposing edge orientation, positive volume, anatomical envelope, relief, perimeter irregularity, UV bounds, nondegenerate cap mapping and input rejection. These tests do not establish full-creature silhouette acceptance.

Production acceptance **pending**: Blender regeneration of authored .blend and GLB, Blender production verifier, approved concept-art front/side/top silhouette comparison, animated deformation and Valheim runtime inspection. The Ashmite's donor is Tick; actual donor mesh polygon and texture dimensions remain unmeasured, so donor-quality minimums are **not certified**. Do not substitute Seeker donor metrics or mark the model production-accepted.

Commands: `PYTHONPATH=tools python -m unittest discover -s tests -p test_ashmite_scute_geometry.py -v`; then `blender -b -P tools/author-underworld-ashmite.py` and the established animation, verifier and GLB-export pipeline. No GitHub Actions.
