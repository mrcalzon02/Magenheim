# First Bloom buried geometry trim, revision 14

[Before-and-after review](gallery.html)

The mesh is a collection of intersecting closed surface components in one renderer. This pass removes whole polygon faces that are fully buried inside retained closed support surfaces. Candidate faces must have no potential triangle-boundary intersections, at least 1.5mm clearance at their vertices, and an interior centroid. The same containing support must cover them through five samples of every retained animation. Partial intersections remain intact.

The audit identifies 446 removable triangles. Four are retained to keep every structural endpoint marker attached to surviving surface geometry. The resulting reduction is **442 triangles: 9,252 → 8,810 (4.78%)**, or 1.420× the 6,206-triangle Troll donor, below the 9,309 soft cap. One mesh, one material and four 512px atlas maps remain; UVs and atlas image pixels are unchanged.

Removed faces leave intentionally open boundaries inside opaque closed support surfaces. The saved-mesh validator checks every boundary edge's endpoints and midpoint against retained closed supports, and rejects wire edges or edges with more than two incident faces. The original global watertight-component requirement is replaced by this targeted internal-boundary check for the trimmed study. This is not a Boolean union or a watertight solid suitable for manufacturing.

The saved contact audit still covers all 122 structural records, including the body flange and medallion base. Inherited attack contact samples, source weights, atlas and GLB checks also pass. The review builder compares six matched renders and verifies identical atlas file hashes. See the JSON records for measured visual differences and the full list of sampled actions.

The study remains uninstalled. Sampled checks do not prove every animation frame or native animation safety, and native runtime performance remains unverified. Protected support components and partial intersections leave room for a later individualized union/remesh pass, which may add triangles and require new UVs and skin weights.

Editable source: `assets/models/studies/underworld-boss-first-bloom-trim-r14.blend`. Tools: `audit-first-bloom-buried.py`, `trim-first-bloom-buried.py`, `review-first-bloom-trim.py`, `verify-first-bloom-trim-poses.py`, `render-first-bloom-trim-closeups.py` and `build-first-bloom-trim-review.py`.
