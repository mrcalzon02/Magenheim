# Rift Titan r12 — systematic concept comparison

Audit date: 2026-10-14. **44 visual discrepancies and 4 process failures.** Current model is not visually accepted and is not close enough to claim 85% matching. This audit changes no geometry, rig, material or texture.

Baseline: `assets/models/studies/underworld-boss-rift-titan-cracked-head-r12.blend`. Source SHA-256: `4c1a6da379f7beebab234d7b274b058c50c0d41ebc1ee3cb50c44ee2722ba700`.

Compare the adopted full concept sheet, supplied frontal/left-facing crops, and actual r12 front, profile, three-quarter, head and torso renders. [Enlarged source/model regions](regions.html) use SVG viewports over original images; no retouching or raster mirroring. [Current r12 gallery](../rift-titan-cracked-head-r12/gallery.html).

## Main diagnosis

The model captures some labels—stone limbs, a cage, purple crystals—but not the construction that gives the reference its identity. A smooth leaf head, missing floating crest, sparse rectangular cage/core, repeated limb wedges, short stubby hands and blurry shared stone noise are major errors. Adding isolated spikes and single notches did not solve those systems.

## Correction order

1. Replace the leaf silhouette with the reference’s asymmetric fractured crystal while keeping a single connected recessed head. Block in the missing raised broken crest and upper thorax arrangement.
2. Fit front and side landmarks for shoulder/elbow/wrist/fingertip, hip/knee/ankle and foot contact. Correct hand reach and armor below foot level.
3. Remodel the rounded rib cage and dense core together, preserving the thin wrap and upper collar ring.
4. Individualize shoulder/arm/leg/palm/digit/toe plates, joint interruptions, crystal thickness and fracture-led growth placement.
5. Author crisp directional stone/metal/crystal surfaces, then reallocate atlas density within the current budget.
6. Recheck all corrected regions in the same views, clay, a standing-plane view and sampled retained poses; native acceptance is separate.

## Measured issues in the saved model

- The lower left and right shin plates reach **0.196 m and 0.188 m below the foot minimum**, respectively. `FloatingPrism4` extends 0.028 m below it. This is a rest-geometry ordering defect; actual native grounding still needs runtime evidence.
- Fingertip minima lie **1.197–1.236 m above foot level** in the 7 m rest model. The reference hands visually hang much nearer the feet; its exact joint coordinates still require a view/pose fit.
- The head’s purple floor occupies **about 32.4 nominal atlas texels of total UV area** at 512px, not 32.4 texels per triangle. This is an allocation diagnostic, not proof that resolution alone caused the bad head.
- Current measured head height is about 27.2% of actor height. Similar overall height does not establish a similar outline; the demonstrated head problem is the leaf profile and missing fractured planes.
- The actor is **1,792 triangles**, below the **1,800** soft cap. It has four 512px maps, one mesh/material and the retained rig/actions. Those are constraints and technical facts, not art acceptance.

## Priorities and evidence

P0 = primary identity/proportion/grounding (14); P1 = construction and secondary form (23); P2 = authored surface/atlas detail (7). All entries remain open. High confidence describes visible evidence or saved-geometry measurement; medium confidence requires additional angle/landmark fitting.

### Head

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **H01 P0 — Head silhouette is a leaf** | A crooked angular crystal with an uneven peak, stepped sides and a broken taper. | r12 is a smooth, almost bilateral lens with a clean point and continuous curved-looking sides. | Rebuild the outer profile from the front reference landmarks while retaining one connected crystal and an inset fissure. |
| **H02 P0 — Head lacks fractured surface construction** | Long irregular fracture planes, overlapping angular ledges and local chips break up the head surface. | A handful of broad uninterrupted planes wrap seven regular cross-sections. | Sculpt large secondary fracture planes and steps into the same shell before adding small texture cracks. |
| **H03 P0 — Fissure reads as an inlaid stripe** | The violet reveal changes width and direction, with broken uneven lips and locally opened fracture faces. | The narrow near-vertical purple ribbon and its bevels run with almost uniform visual rhythm. | Shape an irregular fault path, variable opening and asymmetric bevel depth; keep purple behind the lips and a solid rear wall. |
| **H04 P1 — Lower head reveal is missing** | The lower head has a pronounced broken angular taper and wider exposed violet fracture detail. | The leaf closes into a neat dark taper with a small terminating purple strip. | Match the lower fracture silhouette and violet reveal without making two separate head pieces. |
| **H05 P1 — Head profile is too uniformly slab-like** (provisional) | The side and three-quarter drawings show irregular crystal thickness and changing fracture planes. | The current profile has one long blade face and a simple continuous back. | Vary depth and the side planes at the actual reference break points; calibrate the oblique comparison before choosing a thickness change. |
| **H06 P2 — Head surface has no useful crystal/stone hierarchy** | Sharp dark seams, brighter facets, chipped gray/brown faces and violet recess contrast distinguish the head. | The same soft cloudy surface covers most of the head, obscuring any small facet variation. | Author the head surface around its fracture planes, with deliberate edge, recess and violet contrast. |
| **M07 P2 — Important head detail has very little atlas area** | The head fissure needs a legible irregular material transition. | Its purple floor currently totals only about 32 nominal 512px atlas texels of UV area; this is not 32 pixels per face. | Reallocate texel density and simplify low-value hidden UVs within the current map budget after the head shape is correct. |

### Halo

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **C01 P0 — Broken floating crest architecture is absent** | Prominent broken horizontal stone bars form an interrupted arrangement behind and above the upper torso. | There is no comparable raised arrangement; a couple of rear shoulder slabs do not supply it. | Build the large broken crest/back bars at their observed heights, lengths and setbacks, as intentional floating pieces. |
| **C02 P1 — Upper floating stone fragments are missing** | Small vertical and oblique stone chunks sit around the raised crest and head. | The air around the head is largely empty. | Place varied subordinate stone fragments using both front and side references, after the main crest is correct. |
| **C03 P1 — Violet crest motes are missing** | Several small suspended violet shards punctuate the spaces around the head and upper crest. | The remaining crystals are concentrated inside the cage or attached to limbs. | Add the deliberately floating motes outside the head; do not restore the rejected loose slivers inside the head crack. |

### Shoulders

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **S01 P0 — Shoulders remain a few oversized wedges** | The mantle is an assembly of interlocking irregular slabs with multiple visible depth layers. | Three large cut wedges per side dominate the shoulder shape. | Reorganize the primary mantle into overlapping fractured masses and redistribute existing topology to the visible layered contours. |
| **S02 P1 — Shoulder chips look like cut shelves** | Chips and ledges vary in position, orientation and depth and belong to larger fracture planes. | The new long notches read as repeated horizontal slices across otherwise plain blocks. | Replace repeated shelf cuts with individually shaped breaks, corner losses and shorter irregular ledges. |
| **S03 P0 — Collar-to-mantle transition is too bare** | The upper ring is nested into a fuller broken upper torso, with stone around the neck base. | A clean exposed bridge/ring spans a large open central region between wedges. | Fit the surrounding thorax masses to the reference while preserving the upper ring and its real connections. |
| **S04 P1 — Shoulder organization is too repetitive** | The reference has unequal plate sizes, overlaps and local orientations. | Both sides repeat similar three-piece organization and wedge proportions. | Author corresponding left and right plates individually; preserve overall balance without copying the same local arrangement. |
| **S05 P1 — Rear mantle layering is not resolved** | The side view separates the upper thorax, rear shoulder stones and raised broken crest in depth. | The current side reads as coarse stacked blocks, with an abrupt broad dark cut. | Resolve those three depth groups independently; do not use a rear shoulder pancake as the missing crest. |

### Torso

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **T01 P0 — Upper cage reads as a rectangular frame** | The reference cage is a rounded, organic rib assembly enclosing a substantial core. | The upper half is dominated by a nearly flat top and long straight side posts. | Reshape the envelope and individual rib curves into the observed rounder bowl/cage silhouette. |
| **T02 P0 — Rib flow and density are wrong** | Curved, canted and overlapping dark ribs surround the core above and below the thin wrap. | Sparse upright rods and a simple converging lower basket expose large uninterrupted openings. | Match the main rib directions, spacing and overlap before adding fine metal detail; do not substitute a bone sternum. |
| **T03 P1 — Lower cage ends in a schematic point** | The lower rib bowl is irregular and rounded around the crystalline interior. | Most lower rods terminate in a neat pointed basket. | Distribute lower rib endings and curvature around the reference volume while retaining supported roots. |
| **T04 P1 — Middle stone wrap is too mechanically regular** | The very thin sculpted stone band wraps the cage, with a small V and irregular surface edges. | The correct wrap concept is present, but its width, section and polygonal edges are too uniform. | Keep its continuity and small V; individually shape edge breaks and relief rather than replacing it with a front-only plate. |
| **T05 P0 — Core is a few exposed pyramids** | A dense irregular cluster of violet crystalline material fills much of the rib cage. | Four large clean spikes are easy to count against empty background. | Rebuild the core into interlocking varied crystalline masses with broken internal planes and smaller subordinate shards. |
| **T06 P0 — Torso negative space is substantially wrong** | Ribs, core and shadow form a dense layered interior with smaller irregular glimpses through it. | Large empty windows dominate between the rods and sparse core. | Correct core volume and rib placement together; compare the remaining openings in front and side views. |
| **T07 P1 — Upper ring and neck structure look generic** | A sculpted upper collar is surrounded by fractured stone and dark ribbed neck structure into the head. | The ring looks like a plain bar and the neck like a short set of exposed sticks. | Model the visible collar section and neck rib arrangement against the close-up while retaining their supported connections. |
| **M05 P2 — Metal is not sufficiently distinct from stone** | Dark corroded metal, warm worn edges and small roughness changes identify the cage. | The cage reads as fairly plain gray rods with weak weathered-metal detail. | Author dark metal color/roughness and restrained corrosion within owned maps; verify native material response separately. |
| **M06 P2 — Violet material lacks internal hierarchy** | Deep violet, bright fracture faces, dark occlusion and localized pale highlights suggest crystalline depth. | The core and small growths are predominantly flat lavender-purple surfaces. | Use facet-aware violet value variation, local emission and darker bases; do not rely on increasing uniform glow. |

### Pelvis

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **P01 P1 — Pelvic fragments are broad horizontal blocks** | The front view shows narrower uneven hanging and hip fragments under the core. | Two broad symmetric blocks and a coarse bridging arch dominate the pelvic region. | Rework the fragment proportions and orientations using the front image; preserve intentional floating gaps rather than filling all of them. |
| **P02 P1 — Upper-leg arch and stance need a landmark fit** (provisional) | The visible thigh tops and knee masses form more distinct vertical groups. | The model reads as a wide inverted U of large stones with strongly bowed inner leg contours. | Mark hip, knee and ankle landmarks in the same-facing front/side references and fit those groups before refining small chips. |

### Arms

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **A01 P0 — Hands do not hang low enough** | The concept hands reach much nearer the foot/ground level. | Saved fingertip minima are about 1.20–1.24 m above foot level in the 7 m rest mesh. | Fit shoulder, elbow, wrist and fingertip landmarks; determine the required hand length and limb placement rather than blindly stretching the entire arm. |
| **A02 P1 — Arms remain repeated large chunks** | The arms have long broken longitudinal plate assemblies with distinct overlapping fragments. | A sequence of similarly sized diamond/wedge chunks supplies most of each limb. | Individually remodel the upper-arm and forearm assemblies into the observed long plates and smaller interlocking pieces. |
| **A03 P1 — Elbow and wrist transitions lack convincing construction** | Dark joint bands and fractured armor transitions distinguish the articulations. | Abrupt black gaps and simple connector rods separate coarse stone blocks. | Build the visible cuffs, overlaps and transition contours within the retained rig; keep the whole seated root sections. |
| **D01 P0 — Digits are too short and blunt** | The hands contain substantial hanging angular stone digits with uneven ends. | Three short parallel stubs give a compact mitten-like end. | Remodel the visible digit lengths, offsets and tapered ends from the large three-quarter and front reference. |
| **D02 P1 — Palm and knuckle fragmentation is missing** | The hand mass has deep clefts and layered broken plates around its digits. | A simple palm cap meets a few short digit blocks. | Separate the visible palm/knuckle fracture planes and shape their recesses while keeping connected digit roots. |
| **D03 P1 — Finger spacing and individual gesture are too uniform** | Visible digits have unequal spacing, angles and armor termination. | Digit parts repeat one spacing and broadly parallel alignment. | Give each visible digit an individual direction and gap; establish exact digit count only from supported views, not an invented thumb. |
| **G01 P1 — Limb crystals look like fins or thin pegs** | The reference exposes chunky faceted shards with broken bases and volumetric depth. | Many additions read as thin triangular spikes or purple patches on a plain face. | Replace the large visible shards with meaningful prism thickness and internal facet changes; use texture for the smallest chips. |
| **G02 P1 — Crystal placement is too regular** | Crystals emerge through local broken seams, exposed joints and varied fracture openings. | A similar isolated shard sits on the front of many successive plates. | Place growths according to actual stone fractures and joint cavities rather than a per-plate repeating rule. |
| **G03 P1 — Crystal size and grouping lack hierarchy** | Large exposures, small subordinate shards and deliberate floating chips coexist. | The model has a sparse set of similarly legible solitary spikes. | Design a few reference-led clusters with varied size and direction; distinguish attached growth from intentional motes. |

### Legs

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **L01 P0 — Lower shins extend below the feet** | The reference reads as a creature supported by its rock feet. | LongFractureLegL2 and R2 extend about 19.6 cm and 18.8 cm below the foot minimum; one small crystal extends 2.8 cm below it. | Establish a standing foot plane, correct armor/shard endpoints relative to it, then normalize height and check a ground-plane render. |
| **L02 P1 — Knees are not clearly articulated visually** | The concept has recognizable knee interruptions and dark joints between upper and lower stone groups. | The knee region is lost in a continuous bowed sequence of wedges. | Shape a distinct knee assembly and transition; a retained single leg bone does not prevent the visual joint from being modeled. |
| **L03 P1 — Shin silhouette and layering are too generic** | The lower legs are fractured pillar-like masses with narrow seams, edge fragments and local crystal exposure. | A few broad diamond faces and a recent inward notch dominate each shin. | Rebuild the major longitudinal faces, corner fragments and inter-plate gaps before surface noise. |
| **F01 P1 — Feet are monolithic wedges** | Multiple broken toe/foot rocks and dark separations create an irregular planted footprint. | Each foot is one simple faceted wedge with a smooth broad front. | Model the visible toe groupings, uneven fronts and heel mass inside the existing foot envelope and budget. |
| **F02 P1 — Ankle-to-foot transition is crude** | The ankle is framed by broken lower-leg plates above a layered foot. | A broad step from the shin into the foot reads like a shoe sole/block. | Fit the ankle overlap and foot height with the standing plane visible, retaining the intended joint clearance. |

### Surface

| ID / priority | Concept | Current error | Required correction |
| --- | --- | --- | --- |
| **M01 P0 — Stone reads as blurry marble** | The art uses crisp angular surface patches and directional fractured faces. | Soft gray/olive clouds cover broad smooth planes. | Remove the cloud-noise appearance; create fracture-aligned face variation and preserve clear large planes at gameplay scale. |
| **M02 P2 — Color grouping is not controlled** | Cool dark stone, warm gray/tan chips and localized ochre/olive patches form deliberate groups. | Similar mottling appears over head, shoulder, limb and foot surfaces. | Author regional palettes and weathering masks around actual plates rather than one spatial noise field everywhere. |
| **M03 P2 — Cracks and edge damage do not follow the modeled slabs** | Long faults, chips and smaller veins reinforce the large fragment directions. | The generic procedural pattern is independent of individual stone construction. | Align major painted/baked cracks and wear to each remodeled slab and its real bevels. |
| **M04 P2 — Edge highlights and recess contrast are missing** | Bright chipped edges and dark seams separate adjoining layers. | Adjacent surfaces blend together under low-contrast texture and broad facets. | Bake or paint edge/recess contrast from deliberate high-detail forms; avoid drawing false seams across unbroken planes. |

## Process failures

| ID | Failure | Correction |
| --- | --- | --- |
| **Q01: Comparison framing is not calibrated** | Different perspective and pose were treated too much like matched orthographic drawings. | Use the frontal reference for primary landmarks, the same left-facing side for depth, and label angle-dependent judgments as provisional. |
| **Q02: No standing-plane review caught the grounding defect** | A free-floating preview hid the shins below foot level and gave no contact/shadow cue. | Add an audit ground plane at the intended sole level and check rest plus sampled poses without changing the native root convention by guesswork. |
| **Q03: Geometry validity was confused with art progress** | Closed meshes, contact tests, equal counts and many revisions were reported without a sufficiently strict visual rejection step. | Keep those checks, but reject obvious silhouette/material mismatches before calling a pass ready; close technical defects without calling them concept matching. |
| **Q04: The run-count estimate had no dependable basis** | The earlier 85% projection was not supported by a stable measured improvement rate, and a fundamental head mistake survived several passes. | Withdraw the numerical completion forecast and use this discrepancy list with visible correction evidence and user art review. |

## Preserve during correction

- one connected head with recessed beveled purple fissure; fix its shape without splitting it.
- continuous upper collar and thin wrapped middle stone band with small V; ribs above and below.
- same left-facing side orientation.
- native eight-bone hierarchy and seven actions, compact weights and supported full root sections.
- 1792 current triangles, 1800 soft cap and four 512px maps; improve allocation rather than raising the budget.

## Uncertain or unassessed

- Source is painted concept imagery with different poses/perspective; no exact pixel-match or percentage is claimed.
- Exact finger/toe counts and unseen rear construction are not established from the supplied views.
- Native deformation, collision and live frame cost are not established by this visual audit.

Do not invent a complete rear design, a finger count, an exact target percentage or a triangle allowance increase to make the list look complete. The floating crest/motes and separated pelvic fragments are intentional where the concept supports them; floating geometry is not inherently an error.

The previous numerical completion projection is withdrawn. Next modeling work should identify the finding IDs it addresses and show the same region before/after; unresolved items remain visible in the backlog.

## Audit integrity

[Geometry inventory](geometry-inventory.json) confirms that the baseline source file hash is unchanged. [Structured backlog](findings.json) records all findings. Original image files are referenced, not modified. Local-file browser automation was blocked by browser URL policy; inspection used the source images directly, and comparison-page links/viewports are checked programmatically. No browser policy workaround was used.
