# Rift Titan — concept-driven primary body masses (2026-10-13)

**Reference:** inspected the adopted Rift Titan concept thumbnail and original r3 front/three-quarter study renders. The approved Titan has a heavy continuous shoulder/thorax geological silhouette and substantial side supports around a visibly exposed purple core. The r3 study still reads as two separated arm towers, floating crown and loose pelvis, even after limb alignment and surface faceting.

**Source correction:** add two large angled, faceted thorax mantle stones joining the shoulder envelopes to the top of the core housing; add two paired lower-core buttresses bridging the existing pelvis keystones toward the lower cage. Each uses the existing 24-triangle faceted-rock helper, existing automatic UV projection (authored UV acceptance pending), rift-strata material, Body rig binding and stable part name. The central X/Z window around the crystal remains unobstructed. No new floating debris, bones, animations, creatures or gameplay changes.

**Budget:** four new 24-triangle plates add 96 triangles. The previous 1,668-triangle total is **projected** from the last committed author change, not a Blender regeneration; revised **projected source is 1,764**, under the boss-study Stone Golem donor 1,200 x 1.5 soft ceiling 1,800. The soft ceiling applies to this boss study, not the non-boss elite band. Existing r3 study remains measured at 1,480 triangles and four 512px texture maps; those binaries were not regenerated. No runtime LOD count is available.

**Verification:** six local Blender-independent Python regressions passed for unique paired parts, 96-triangle increment, center-core window, shoulder contact, lower-cage/pelvis span and closed positive-volume mesh geometry. Tests use the actual rift_titan_rock_facets.py geometry. This is **not** Blender, rig-deformation, approved silhouette, UV-bake, GLB, native loader or frame-time acceptance. Source changes must be regenerated and visually compared before promoting the study.

**Scope:** Rift Titan boss source and tests only; no small/common creatures or non-creature assets touched. Boss acceptance tally remains 1 of 6.
