# First Bloom: individualized surface refinement pilot

The user sets a soft triangle ceiling of **1.5 times the comparable donor**.
For First Bloom, the extracted Troll is 6,206 triangles, giving a 9,309 target
ceiling. Do not treat that ceiling as a target to fill. Any proposed exception
needs a measured anatomical benefit, a recorded reason and performance review.

[Matched comparison gallery](gallery.html) · [Measured costs](refinement-metrics.json) ·
[Offline verification](verification.json)

## Remodeling results

The 91,516-triangle detail revision is retained as a reference. The first surface
rebuild used 8,188 triangles. Its clay review exposed segmented elbows and knees,
loose fingers and overly rectangular chest panels. A second rebuild joined each
limb across the joint, added palm masses and tapered the folded chest tissue.
The delivered pilot is **7,940 triangles, 1.28 times donor, 91.3% below the previous
revision**, with one skinned mesh, one material and four 512px atlas maps.

- The trunk is one fluted surface from hips to shoulders, with pelvis/chest weights.
- Elbows and knees share rings and blend across the retained adjacent bones.
- Bark ribs are shaped into cross-sections, replacing many separate root cords.
- Cap top and underside share a rolled rim; folded undersides replace detached
  gill tubes. Silhouette scallops stay in geometry.
- Chest gills are two shaped closed panels; fingers, toes and eyes retain geometry
  because their negative spaces or articulation matter.
- Owned r2 materials and fine radial gill normals are self-baked into a packed
  atlas. This is **not** a high-to-low geometry bake; removed high-source geometry
  has not been transferred into normal maps.

The shader uses four atlas maps: albedo, tangent normal, roughness and emission.
At uncompressed RGBA32 with full mip chains these would total approximately
5.33 MiB; this is an estimate, not measured Unity residency. Actual runtime format,
compression and channel packing remain undecided. The 512px atlas exceeds the
Troll extraction's 256px maps; it is a measured pilot choice, not texture-budget
parity. Next texturing work should establish local bark/cavity detail and seam
quality within the compact atlas rather than restoring many large material maps.

## Repeatable procedure, individual anatomy

1. Measure the actual comparable donor: triangles, mesh/material count, maps,
   skeleton, scale and relevant poses. Record its 1.5x soft ceiling.
2. Mark identity-defining silhouette, negative spaces, joints and focal anatomy
   against the adopted concept. Allocate triangles by anatomical region.
3. Review clay surfaces to identify disconnected primitives, hidden overlaps,
   excessive tessellation and detail that belongs in texture maps.
4. Rebuild those regions into connected or deliberately layered surfaces.
   Preserve silhouette and articulation first; avoid global decimation as the
   main solution. Spend fewer loops where the contour is smooth or obscured.
5. Review front, side, three-quarter and close-up views against both concept and
   donor. Adjust shape and loops; repeat until coherent within the soft ceiling.
6. Consolidate material families into an atlas; bake or paint local surface detail
   at a documented texture budget. Inspect seams, mip behavior and normals.
7. Check geometry, weights, portable export and multiple poses. Keep numerical
   validity distinct from deformation and visual acceptance.
8. Admit a supported runtime mesh/shader/LOD representation, then measure frame
   time, render/shadow cost and texture residency with comparable combat scenes.

Share measurement, review cameras and verification, **not identical modeling
operations**. Bloom needs gill folds and branching woody limbs; Maw needs jaw
shells and a continuous swimming body; constructs need purposeful fracture
planes; Carrion needs rooted branching and torn floral anatomy. Each needs its
own loop placement, texture projection and deformation decisions.

## Verification and remaining work

Offline checks pass the cap, closed manifold edges, nonzero triangles, finite
vertices, normalized one/two-bone influences, packed maps, 4.65m height, retained
actions and one-primitive skinned GLB. Five sampled attack frames pass finite
coordinates and bounded extents. These checks do not establish good deformation.

This is an editable art pilot, not a final creature. Texture detail, skin weights
through extreme poses, detailed concept alignment and LOD decisions still need
iteration. The current production creature exporter/loader requires rigid
bone-bound parts. This study declares a review-only skinned contract and lives
under `assets/models/studies`, outside that exporter's source glob. Consolidated
runtime skin support must be evaluated before admission; no runtime behavior,
colliders, release or installed profile was changed.

## Reproduce

1. `tools/blender.ps1 refine-first-bloom`
2. `tools/blender.ps1 review-first-bloom-refinement`
3. `python tools/build-first-bloom-refinement-review.py`

The detailed reference and all other boss sources remain intact.
