# Ashmite six terminal feet and vent rasp claws — authoring integration (2026-10-09)

Scope: established Sulfurous Wastes Ashmite only. Six cylindrical tarsal shells and six conical scraping claws are replaced by sculpted, closed tarsal/rasp meshes from `tools/ashmite_tarsal_geometry.py`. Each foot preserves the approved six-legged anatomy, the existing ankle/toe anchors, its L/R_Tarsus1..3 bone, and the protected-joint or mouthpart PBR material. Tarsi have two physical armor sutures; backward-facing rasps have three underside ridges and an inward-hooking tip. No new creature identity, gameplay behavior, equipment, resource or station is introduced.

Source counts: each tarsus 440 triangles (formerly 52); each claw 680 triangles (formerly 52). The twelve parts total **6,720 triangles**, replacing 624. These are component counts only, not whole-creature counts or proof of the Tick donor minimum. Existing topology/UV regression tests check all twelve closed meshes, symmetry, endpoints, physical relief, and claw/foot proximity.

The Blender author now instantiates the sculpted parts. The production verifier checks each part's geometry contract, side/pair/kind, vertex and triangle counts, exact anchor, Tarsus armature binding, original material, and UV bounds.

**Acceptance still pending:** Blender source/GLB regeneration, production verifier run, approved concept-art full-creature silhouette comparison in multiple views, gait deformation and clipping inspection, actual exported Valheim Tick donor polygon and texture dimensions, and in-game Valheim testing. A committed authoring change is not a regenerated or runtime-accepted asset. No GitHub Actions were used.
