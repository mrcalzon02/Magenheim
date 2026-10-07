# First Bloom: precision shaping revision 4

[Matched full-body and cap/tissue close-ups](gallery.html) ·
[Topology and weight parity](precision-parity.json) · [Offline checks](verification.json)

This pass follows the user's request for smaller shape refinements to existing
tissue frames and drooping caps. Revision 3 is preserved. The mesh remains
**9,188 triangles, 1.48x Troll**, below the 9,309 soft cap. Source vertex, edge and
polygon counts, face connectivity and per-vertex bone weights are identical.
One mesh, one material and four 512px atlas maps remain the review budget.

## Shape changes

- Cap free rims sag toward exposed front lobes, with different amplitudes by
  cap size and deterministic per-cap phase. Stalk-side centers remain seated.
  Unequal scallops and shallow radial folds replace uniformly flat edges.
- Existing curtain anchors follow cap displacement. Their middle and tip
  sections bow and hang under gravity rather than dropping in straight lines.
- The chest rails flare around the rib chamber, pinch between anchors, twist
  out of plane and taper. Existing crosslinks and ribs have slightly unequal
  arches. These changes shape the existing frames; they add no new branches.
- Owned material maps are rebaked after the geometry and unwrap changes so the
  tangent normals follow the revised surfaces. Map dimensions stay unchanged.

Dedicated cap and tissue cameras compare revision 3 and revision 4 at matching
directions, framing, lighting and exposure, in both textured and clay modes.
The original broad concept comparison remains available alongside this focused
review. Shape changes, not additional surface density, are the purpose here.

## Checks and remaining work

Offline geometry, manifold edges, nonzero triangles, normalized weights, retained
actions, 4.65m height, packed maps and the skinned GLB pass. The GLB triangle count
also matches the source. Sampled attack bounds are finite. Topology and source
weights match revision 3 exactly; this does not prove visually correct native
deformation or unchanged GPU timing.

Thin tissue still has fewer branches and less fraying than the artwork. Further
precision passes should redistribute existing loops and refine attachment and
fold placement before adding geometry. Mips, seams and extreme poses still need
review. The consolidated skin remains outside production export and unsupported
by the current rigid-part creature path. No runtime admission or installation
changed; live performance acceptance remains pending.

## Reproduce

1. `tools/blender.ps1 refine-first-bloom-precision`
2. `tools/blender.ps1 review-first-bloom-precision`
3. `tools/blender.ps1 render-first-bloom-precision-closeups`
4. `python tools/build-first-bloom-precision-review.py`
