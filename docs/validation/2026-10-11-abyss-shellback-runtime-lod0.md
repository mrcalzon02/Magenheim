# Abyss Shellback elite runtime LOD0 performance repair — 2026-10-11

The existing committed Abyss Shellback runtime mesh is **50,088 triangles**, more than twice the governing **24,000-triangle elite ceiling**. Its high-detail editable source must be preserved; the export-time LOD0 reducer budgets evaluated geometry, favors the five layered shell plates, protects eyes, leg shafts and claw fingers, rejects missing UVs and excessive bounding-envelope drift, and removes its temporary modifiers before saving the original .blend.

The standalone budget verifier rejects runtime LOD0 outside 14,000–24,000 triangles, missing shell plates, amphibious paddles, claws, vulnerable underside or eyes, broken UVs and absent creature-rig binding. Existing source texture generator uses six 1024px albedo/normal/roughness material families; no texture resolution was changed.

**Not yet accepted:** Blender regeneration and GLB/JSON parity, actual donor measurements, approved concept-art silhouette comparison, animation deformation, LOD1/LOD2/distant levels, and Valheim runtime performance. The current committed 50,088-triangle binary remains over budget until regenerated. No new creature anatomy or unrelated assets were invented.
