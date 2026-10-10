# Ashmite six folded coxal membranes — 2026-10-10

**Scope:** Only the established Sulfurous Wastes Ashmite. Replace its six generic, irregular ico-sphere hip sockets with sculpted protected-joint membranes, retaining all six original hip anchors, their Coxa bone assignments, original joint half-extents (0.020, 0.022, 0.017 m), and the existing protected-joint albedo/normal/roughness family.

**Geometry:** Each membrane has two physically recessed circumferential compression folds and restrained irregular cuticle relief. A 20-section, 48-sector closed ellipsoid uses 914 vertices and 1,824 triangles per socket, or 10,944 triangles across all six; the former subdivision-3 ico-spheres used 1,280 triangles each, or 7,680 across six. This is component geometry, not an Ashmite total or an installed Tick donor comparison.

**UVs:** The mesh authors a continuous side strip and independent end-cap islands, preserving per-face-loop UVs at the wrap seam. The production author instantiates these meshes; the production verifier checks exact vertex relief, face winding, UV loops, material, original anchors, six identity contracts and all six Coxa rig attachments.

**Offline verification:** Five Python tests cover original anchors, closed/wound topology, positive volume, original joint envelope, two physical folds and noncollapsed UVs. Blender-free tests do not certify regenerated assets. **Pending:** regenerate the source .blend, re-export GLB, measure the installed Tick donor polygon and texture floors, compare whole-creature silhouettes against approved source art, inspect animation deformation, and verify the model in Valheim. No station, resource, equipment or unrelated creature was modified.
