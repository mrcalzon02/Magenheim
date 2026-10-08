# Ashmite feeding-pincer refinement — 2026-10-08

Scope: the established Sulfurous Wastes Ashmite only. Replace its two straight 14-sided cylindrical mandibles with inward-returning swept, tapered, physically serrated mouthparts. Keep their original root and tip anchors, Mandible_L/R bone assignments, mouthpart PBR family, Jaw/Scavenge sockets, five scutes, sulfur ridges, six-leg gait and existing action names.

Each pincer has 17 rings x 24 sides, two center cap vertices, 410 vertices, 432 faces and 816 triangles. The pair has 1,632 triangles versus 56 triangles for the two former capped 14-sided cylinders. This is component geometry, not the complete creature or Tick donor count. Each pincer has three inner denticle crests, dorsal keratin keel, stable projected sweep frame and authored cylindrical side UVs with independent cap UVs. Mirror symmetry and outward winding are explicitly tested.

Offline: five mandible regressions plus five previous scutella regressions passed, with topology, positive volume, anchor retention, envelope, mirror symmetry, physical denticle and keel relief, taper, UV seams and invalid inputs. Python syntax compilation passed.

Source-art acceptance is not established: no approved Ashmite concept image was found in the checked repository tree. Blender regeneration and verification, complete front/side/top creature silhouette, animation deformation, actual Tick donor triangle and texture measurement, and Valheim runtime acceptance are still required. Existing 1024px Ashmite authored PBR textures are not a substitute for measured donor texture resolution. Do not claim the donor minimum or production acceptance until measured and inspected.

Commands: PYTHONPATH=tools python -m unittest discover -s tests -p 'test_ashmite_*geometry.py' -v; then the established Blender author, animate, verify and GLB export pipeline. No GitHub Actions.
