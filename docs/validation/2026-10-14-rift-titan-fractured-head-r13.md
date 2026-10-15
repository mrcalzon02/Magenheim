# Rift Titan r13 — first repair after systemic audit

User authorization: “Begin repairs,” followed by “Continue.” Scope: H01–H04
head outline, fracture planes, recessed fault and lower reveal. This is an art
study; no runtime package or installation is changed.

The user rejected the first r13 attempt during work because it was still oval.
That modified tapered shell was discarded before commit. The replacement has
an explicit twenty-corner extruded polygon perimeter with long straight shaft
edges, abrupt shoulders, an uneven summit and triangular lower breaks. Four
integrated cortex fracture points and an irregular recessed violet fault are
part of that same continuous shell. The rear wall remains solid. Solid front
and left-facing side views were inspected before the full material review.

Current candidate and exact-camera before/after evidence:
`../art/creature-models/2026-10-14/rift-titan-fractured-head-r13/gallery.html`.
Detailed limitations and finding status are beside it in `REPAIR.md` and
`repair-status.json`. All 44 visual findings remain open; no 85% claim,
completion estimate or boss acceptance is inferred.

Offline checks passed:

- Actor 1,800 triangles, native donor 1,200, soft cap 1,800. Head 116 triangles
  and 60 vertices; eight additional head triangles spend the r12 reserve.
- One mesh/material, four packed 512px maps, original eight bones/seven actions,
  compact normalized weights and valid skinned GLB with embedded maps.
- Head is one connected closed surface, Euler characteristic 2; floor behind
  lips and continuous solid rear wall. Six complete neck ends are seated with
  matching Body weights; 1,512 face/edge samples across 36 poses pass.
- Inherited full support, arm/digit closing-face, limb-crystal and thorax
  closing-face checks pass. Finite sampled attack bounds pass.
- Every retained body/limb/core component has identical counts and zero
  coordinate difference against saved r12. Only the head and six neck end
  sections are excluded from this preservation comparison.
- Actual saved r12 was rendered using exact current camera transforms,
  orthographic scales, lighting and resolution. Left-facing side projection
  guard passes. Original concept images and saved r12 remain unchanged.

These checks do not prove concept similarity or native deformation, collision,
loader admission or frame cost. The current head still lacks the reference's
small fractured layers and crisp stone detail. H02, H05, H06 and M07 remain
unresolved; the missing crest, cage/core, hands, shin/foot ordering and other
body findings were not modified. Boss tally stays 1/6 adequate.

The initial sandboxed GLB export failed on Blender's temporary image directory;
the approved elevated standard export then passed. A prior GitHub fetch review
had failed on reviewer usage availability; a fresh approved fetch succeeded,
with local main equal to origin/main before this repair commit. Local-file
browser automation remains unused; source/render images were inspected directly.
