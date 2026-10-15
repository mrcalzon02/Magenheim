# Rift Titan r13 — first audited head repair

This is an unaccepted head repair candidate for **H01–H04**. All 44 baseline findings remain open. The original r12 audit and source are preserved. No concept-match percentage or completion forecast is claimed.

[Comparison gallery](gallery.html) · [Structured repair status](repair-status.json) · [Original audit](../rift-titan-systematic-audit-r12/AUDIT.md)

| Finding | Authored change | Still unresolved |
| --- | --- | --- |
| H01 | The tapered shell was discarded after the user rejected its still-oval shape. An explicit twenty-corner extruded slab perimeter now has long straight sides, abrupt ledges, an uneven summit and triangular lower wings. | The slab is still a coarse interpretation; finer local breaks and reference-led facet proportions remain unresolved. |
| H02 | Four integrated cortex fracture points interrupt the broad slab faces; front/back share the polygon perimeter and the fault remains recessed into one connected shell. | The source has a much denser hierarchy of chips, staggered layers and secondary fracture planes. This is only a first structural repair. |
| H03 | The connected fault changes direction, width and depth; asymmetric bevels expose a recessed violet floor backed by a solid shell. | The opening still reads too cleanly in places, and the violet surface lacks the reference crystalline facet hierarchy. |
| H04 | The lower reveal widens between broken, unequal cortex edges before closing into the connected base. | The lower tip remains too simple; fractured ledges and the violet-to-stone transition need further fitting. |

## Visual review

The user rejected the first r13 attempt during work: the head was still oval. That attempt had only modified the same tapered ring shell. It was discarded before commit. The replacement is an explicit extruded polygonal slab with twenty perimeter corners, long straight shaft edges, abrupt shoulders and triangular lower breaks. Its solid front/side views were inspected before the full material review. The model still lacks the reference’s smaller layered fracture construction and crisp stone surface treatment. This pass does not solve H02, H05, H06 or M07.

The previous head is rendered from the actual saved r12 source using the exact camera transforms, scales, resolution and lighting of the current head views. Concept panels are labelled original source views; their perspective differs from the orthographic model. Side views project anatomical +Y toward screen-left. No source image is mirrored or retouched.

## Cost and verification

- Actor: **1,800 triangles**, versus native Stone Golem **1,200** and soft cap **1,800**. Head: **116 triangles / 60 vertices**, up from 108 / 56. The eight triangles reserved in r12 were spent on integrated fracture planes; no body triangles were removed.
- One mesh/material, four packed 512px maps, original eight-bone hierarchy and seven actions. The owned r10 material recipe was re-baked for the new topology; the cloudy stone appearance remains an open defect.
- One connected closed head, Euler characteristic 2; shared floor/bevel edges, purple floor recessed behind the lips and a continuous rear wall. Six complete neck end sections are seated into the head with Body weights.
- Neck closing faces: 1,512 interior/edge samples across rest plus 35 retained-action samples. Inherited arm/digit, limb-crystal, thorax and support checks also pass.
- Every retained body/limb/core component compared with the actual saved r12 has identical face/vertex counts and **zero coordinate difference**. Only the head and its six reseated neck sections are excluded.
- Saved model geometry, normalized compact weights, atlas, finite sampled pose bounds and the skinned GLB export pass the offline review. These checks do not establish native runtime deformation, collision, performance or art acceptance.

## Next repair priorities

Continue the head’s fractured layer construction and surface hierarchy before treating it as matched. The missing raised crest, torso cage/core, hand reach, shin/foot ordering and all other audit regions remain unchanged. Boss tally remains **1/6 adequate**.
