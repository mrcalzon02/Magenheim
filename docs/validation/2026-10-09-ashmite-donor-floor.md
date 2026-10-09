# Ashmite Tick donor quality gate — 2026-10-09

The established Sulfurous Wastes Ashmite must not be accepted below the actual installed Valheim Tick donor skeleton's measured mesh triangle count or texture dimensions. `tools/verify-underworld-ashmite-donor-floor.py` checks the exported Tick OBJ vertices/faces against the exporter manifest and reads donor MTL-referenced PNG dimensions. Every one of Ashmite's fifteen albedo/normal/roughness textures must meet both measured donor dimensions, and the regenerated Blender creature must meet max(6,500 triangles, donor triangles).

Run the tool in Blender with `MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR` set to a fresh export of the Tick prefab from the installed Valheim build. The check deliberately fails closed if the export, manifest, MTL, PNG, Blender source or scene identity is absent or inconsistent. The Blender source and donor data have not been measured in this execution environment.

Seven Blender-free fixture regressions exercise real OBJ triangle counts, manifest mismatches, missing geometry, unsafe MTL paths, PNG headers, texture minima and missing/ambiguous Tick exports. Passing fixtures do **not** certify the real donor or final asset.

Production acceptance also requires regenerated .blend/GLB, complete concept-art silhouette review, deformation under the ten existing actions and Valheim runtime inspection. No unrelated creature or station/resource/equipment assets are changed.
