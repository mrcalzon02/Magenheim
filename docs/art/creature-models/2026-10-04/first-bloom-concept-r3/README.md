# First Bloom: concept anatomy revision 3

[Concept / donor / previous pilot / revision gallery](gallery.html) ·
[Measured geometry costs](refinement-metrics.json) · [Offline checks](verification.json) ·
[Regional concept comparison](concept-comparison.json)

This pass follows the user's request for another in-depth concept comparison.
The soft 1.5x donor triangle ceiling continues to apply: native Troll 6,206,
ceiling 9,309. The candidate is **9,188 triangles (1.48x donor)**, one skinned
mesh and one atlas material. Four maps remain 512px. Cost is not art acceptance.

## Reference findings and remodeling

The 7,940-triangle pilot was coherent and economical but still read as a humanoid
with a broad mushroom hat, two bright chest panels, bare legs and toe fans. The
concept instead emphasizes a tapering fungal cathedral, a recessed circular rib
chamber, pale hanging scaffold, dense leg growths and spreading ground roots.

- Removed the humanoid face, eyes and jaw. Eight offset, tapering fungal brackets
  now surround a branching crown spine. Thinner brackets have rolled pale rims
  and separate upper/underside pigmentation.
- Opened the front trunk shell. A concave oval chamber sits behind the opening,
  with 22 curved ribs and a central amber organ. It is actual recessed geometry,
  replacing the previous flat bright chest panels.
- Added closed tapered tissue ribbons, sparse crosslinks and localized hanging
  growths. Curtain roots are placed under their tilted cap surfaces. They remain
  a coarse scaffold; fraying and richer branching need another pass.
- Layered brackets down arms and legs, added opposed root thumbs, spreading foot
  buttresses and hip forks. Narrowed lower-limb rest positions to 75% of donor X
  spacing to reduce the exaggerated bow-legged stance. Retained action rotations
  are a baseline, not evidence of successful retargeting.
- Side review exposed a bare back. Reduced loops in the small crown tips and
  spent those savings on three staggered rear trunk brackets.
- Replaced the previous source maps with owned procedural pigmentation: darker
  directional bark, ochre/moss top surfaces, pale tissue and warm rib bark.
  Isolated amber emission keeps the chest as the visual focus. No concept pixels
  or donor textures are copied into these maps.
- Initial projection fragmented the UVs and reduced usable detail. The delivered
  atlas unwrap follows authored UV discontinuities and cap-end seams, averages
  island scale and repacks at the same 512px size. Bake margins are two pixels.

The first anatomical rebuild was 8,856 triangles. Clay and material review led to
narrower stance, better curtain attachment, hip forks, warmer ribs and revised
UV packing, yielding 9,032. Side review then redistributed crown-tip loops into
rear growths, yielding the delivered 9,188. The added 1,248 triangles over the pilot
are spent on concept anatomy rather than generalized subdivision.

## Remaining visual and runtime work

This remains a modeling iteration. The concept has more irregular shelf shapes,
reticulated/frayed tissue, integrated root masses and richer local surface
variation than the candidate. Continued remodeling should redistribute triangles
within the current ceiling before increasing density. The current atlas also
needs seam/mip and close-range detail review; 512px exceeds the donor's extracted
256px maps, so texture parity is not claimed.

Offline checks pass closed manifold edges, nonzero triangles, normalized one/two
bone weights, 4.65m height, four packed 512px maps, inherited action retention and
a one-primitive skinned GLB. Five sampled attack frames pass finite coordinates
and bounded extents. That does not establish visually correct deformation.

The consolidated skin is still unsupported by the production rigid-part creature
path. Sources stay in `assets/models/studies`, outside production source export.
Runtime admission, shader behavior, LODs, GPU residency, live frame time and combat
compatibility remain unverified. No runtime registration or installation changed.

## Reproduce

1. `tools/blender.ps1 refine-first-bloom-concept`
2. `tools/blender.ps1 review-first-bloom-concept`
3. `python tools/build-first-bloom-concept-review.py`

The anatomy module is specific to First Bloom. It reuses the pilot's bake/export
and comparison checks through review-only wrappers; the original pilot and other
boss sources are preserved. Regional findings are recorded so the next pass can
address specific mismatches instead of repeatedly adding generic detail.
