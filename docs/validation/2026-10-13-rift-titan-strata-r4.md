# Rift Titan strata and supported cage r4

The user resumed work with “Continue.” Blackwater Maw remains paused, and First
Bloom remains the sole adequately reworked boss in the rolling tally.

## Changes and inputs

Rebuilt online commits `7bce5313` (long, bone-aligned limb strata and thinner
shoulders) and `d9c8c9b3` (shallow fracture facets) into a new saved r4 source.
The old r3 source, maps and review renders are preserved for comparison.

The local cage pass replaces coincident hoop end caps with cyclic closed
cross-sections and seats the complete four-vertex section at both ends of each
diagonal brace into its hoop. All cage vertices retain Body weights; supported
brace roots move with their hoops. Eight redundant seam-cap triangles are removed.
This is a cage topology/attachment correction, not a claim of a Boolean-unioned
or fully optimized creature mesh.

## Measured evidence

- Saved source and GLB: **1,660 triangles**, 1.383x the native Stone Golem's 1,200;
  below the 1,800 soft ceiling. The online facet-only source estimate was 1,668.
- One mesh/material and four packed 512px atlas maps; eight rig bones and seven
  retained actions. Normalized weights use at most two influences per vertex.
- Eleven existing geometry/layout tests pass.
- Saved geometry is finite, nondegenerate and manifold. GLB skin attributes and
  embedded images pass the portable-export check.
- Eight saved brace attachments pass REST plus 35 sampled action poses. Maximum
  measured outside-support distance is zero; the tolerance remains 1 mm.
- Surface, clay, front, side, core and inherited-attack renders were inspected.
  Source SHA and all comparison-gallery links were checked.

## Review and remaining work

`docs/art/creature-models/2026-10-13/rift-titan-strata-r4/gallery.html` compares
the saved r3 and r4 models in the same views, with the adopted full concept and
native donor reference. Editable source, skinned export, contact evidence and
authoring provenance are linked.

The concept still needs closer matching of crown/shoulder masses, layered
fracture density and individualized weathered stone/lichen detail. This pass is
**not user art acceptance**, native loader admission, complete deformation
acceptance or live performance evidence. No runtime model registration, release,
installation or ordinary-creature source was changed.
