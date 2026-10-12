# Furnace Golem primary silhouette source refinement — 2026-10-12

**Creature:** established Sulfurous Wastes Furnace Golem (elite), not a new creature. **Authority:** `docs/UNDERWORLD_CREATURE_ART_RIG_STANDARD.md` and the existing `tools/author-underworld-sulfur-remainder.py` creature anatomy, rig and animation contract.

## Implementation

The existing Furnace Golem was a 34-part, **984-triangle** runtime model with four material slots in `assets/models/catalog.tsv`. Its torso, pelvis, head, gauntlets, feet and four armor plates were generic boxes. This source-authoring change replaces those **eleven** boxes with closed, chamfered and UV-authored sculpted slabs, and adds **eleven** new major anatomical armor pieces: paired overhanging shoulder pauldrons, forearm shields, knee guards, thigh plates, lower-leg greaves and one armored brow. All pieces use the existing armature's `Head`, `Torso`, `Pelvis`, `Upper_L/R`, `Lower_L/R`, `UpperLeg_L/R`, and `LowerLeg_L/R` bindings; no new creature or gameplay behavior is introduced.

The torso now has a real **recessed furnace throat** with closed cavity walls and a back surface. Its glowing furnace insert is moved behind the open armor and the four separate physical grates are aligned with the aperture. The existing twin exhaust stacks, claws, spikes, animations, materials and attack origin remain intact. Geometry is concentrated on primary silhouette and armor mass rather than decorative microdetail. `tools/furnace_golem_armor_geometry.py` supplies Blender-independent mesh/UV loops for the author, and `tools/test_furnace_golem_armor_geometry.py` checks all five profiles, manifoldness, winding/positive volume, UV bounds, cavity depth and silhouette taper.

**Triangle accounting:** the 22 sculpted/added parts contain **1,496 source triangles** by the procedural generator. They replace 132 triangles from eleven cubes, implying approximately **2,348 triangles** for the regenerated whole model if all untouched parts remain identical. This is a *source-code projection*, not a Blender-exported count. The currently committed binary still contains **984 triangles**. The governing elite runtime target remains **14,000–24,000**; this pass is not production-accepted and does not claim donor-model parity or sufficient final-art complexity.

**Texture/material accounting:** the existing four source material slots are preserved, with explicit independent UV islands on sculpted armor and a separate dark cavity material region. No new PBR maps were produced; actual texture dimensions and donor textures were not measured. The normal 2048px large/elite source-texture expectation is not yet satisfied or waived.

## Evidence and acceptance boundary

Five Blender-independent geometry regressions passed against the exact `furnace_golem_armor_geometry.py` Git blob. A local orthographic projection of the procedural pieces was inspected, but it is **not** a Blender render, animation-deformation test or comparison to the user's approved concept illustration. The visual concept sheet was not recovered from accessible repository/Drive/Library searches. Existing authored anatomy and the approved written creature specification were used to constrain this limited primary-mass pass; **concept-art silhouette acceptance remains open**.

Blender source regeneration, updated GLB/runtime JSON/catalog, donor triangle and texture measurement, concept-art overlay, material bake, 2048px source texture review, LOD1/LOD2/distant levels, attack/locomotion deformation and Valheim runtime performance remain mandatory. Do not describe the old binary as updated or the Furnace Golem as art-complete.
