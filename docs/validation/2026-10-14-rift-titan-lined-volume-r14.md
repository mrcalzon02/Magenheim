# Rift Titan r14 — specific head-volume/fracture and neck correction

The user accepted r13's frontal outline, then specified an asymmetric fracture
predominantly on the front, receding back on the left with purple crystal lining.
They rejected the forward-pointing slab profile and near-planar iron neck bars.
This pass addresses those instructions while preserving the approved front.

`../art/creature-models/2026-10-14/rift-titan-lined-volume-r14/gallery.html`
contains actual saved r13/r14 comparisons, true left profile, oblique concept
angle and isolated saved head/neck/collar views. REPAIR.md and repair-status.json
beside it record the scope, limits and specific frontal-outline acceptance.

Offline checks passed:

- All twenty approved front-outline X/Z landmarks have zero difference against
  actual saved r13. The summit now sits 0.485 m behind the lower point in Y.
- Closed head volume changed from 0.246 to 0.450 cubic metres. It is one closed
  connected head, Euler characteristic 2, with a continuous rear wall.
- Seven fracture stations have deeper left than right recesses. Twelve inner
  left-wall triangles and twelve floor triangles have purple lining. Its total
  nominal 512px UV area is 262.2 texels, not that count per triangle.
- Six neck bars keep both end sections seated into collar/head with Body weights;
  3,024 closing-face samples across rest plus 35 action samples pass. Middle
  centers span 0.263 m in X and 0.648 m in Y; full neck coordinates are noncoplanar.
- Inherited supported endpoints, arm/digit closing faces, limb-crystal and
  thorax closing faces, normalized weights and finite sampled poses pass.
- Body/limb/core geometry, excluding the head and six remodeled neck bars, has
  identical counts and zero coordinate difference against actual saved r13.
- Actor remains 1,800 triangles / native donor 1,200 / soft cap 1,800; head
  116 triangles, neck 84; one mesh/material, four packed 512px maps, original
  eight-bone hierarchy and seven actions. Portable skinned GLB passes.
- Original r13 source hash is unchanged. Before/after uses exact current camera
  transforms, scales, resolution and lighting. Left-facing projection is checked.
  The isolated views filter actual saved components in memory; source unchanged.

The first local neck spacing was too narrow through its middle in X; geometry
was corrected, then the full checks and relevant review renders were repeated.
These measurements are technical evidence, not a visual similarity percentage.
Fine chips, crisp stone surfaces and other audit regions remain unfinished.
The r14 head/neck candidate and whole boss are not accepted. Adequate tally
stays 1/6; numerical completion forecast remains withdrawn. Native deformation,
collision, loader admission and live performance are unverified. No runtime
package or installation is modified.
