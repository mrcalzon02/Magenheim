# Blackwater Deep B1b — source-art review candidate

Continues Claude's `b0d85b7` (shared flora kit and 16 drafted sources) and `d6dbbe5`
(first spire/pillar rework and failed visual review). The working tree was clean at pickup.
`docs/UNDERWORLD_CUSTOM_ECOLOGY_SLICES.md` remains the handoff and acceptance authority.

## Changes

- Broken column: ten deeper flutes with four samples per flute, irregular broken shaft rim,
  fallen capital laid nearly horizontal and lowered into the silt, thinner sunken silt.
- Rimstone: four offset, irregular/scalloped basins with flat dark pool surfaces and spill
  curtains. Replaces the circular stepped mound.
- Root arch: two tapered knuckled roots, longitudinal bark ridges, broken side branches,
  anchoring rootlets, hanging pale growths and foot silt. The opening stays legible.
- Wet stone and lakebed shelf: overlapping irregular strata, mineral crust and detached chips;
  696 triangles each. Fingerstone rubble: fluted broken mineral fragments and crust, 736 triangles.
- Root fan and root fingers: taper and bark profile. Flowstone resource: layered mineral chunk.
- Brine fern and palefinger geometry preserved from the user's accepted versions. Pearl caps,
  pale fibre, pearl and salt are included in the individual four-view review.
- Added `render-blackwater-review.py` and `verify-blackwater-sources.py`. The author tool can
  now be imported without generating models, so both tools reuse its exact 16-model list.

## Bake defect found by visual inspection

The pearl showed stair-stepped dark seams along UV islands. Inspecting the raw Blender 5 EMIT
bake found all 1,048,576 atlas pixels had alpha 1, including unused black pixels. The existing
alpha-based gutter padding consequently did nothing. This was a texture defect, not pearl detail.

`magenheim_flora_kit.bake_flora_atlas` now bakes at full resolution, derives pixel coverage from
UV triangles, pads using that coverage, and only then downsamples and packs the atlas. It does
not infer coverage from colour, so authored black paint inside an island survives. The rebaked
pearl was rendered again: the dark seam network disappeared. Blackwater uses this reusable
helper; the existing general Blender kit and other generators were not modified.

## Reproduce and inspect

From the repository root:

```powershell
./tools/blender.ps1 author-underworld-blackwater-deep
./tools/blender.ps1 verify-blackwater-sources
./tools/blender.ps1 render-blackwater-review
```

The four 2000px sheets are under `artifacts/review/blackwater/`, with one model per row:

1. Flowstone spire, broken column, rimstone mound, drowned root arch.
2. Brine fern, palefinger, pearl caps, wet stone.
3. Fingerstone rubble, root fan, lakebed shelf, root fingers.
4. Flowstone, pale fibre, pearl, deep salt resources.

Columns show front, side, three-quarter and silhouette. Views share scale within each row;
rows normalize independently and label actual height and triangle count. Studio lighting is
for form inspection. There is no terrain plane: buried root/stone bases remain visible here.
These are source renders, not runtime export or in-world screenshots.

The source verifier checks model identities, revision, finite geometry and UVs, packed texture
dimensions, size bounds, triangle budgets, no lights, and canopy-only collision. It writes
`source-validation.json` beside the sheets. The pearl also guards against black atlas gutters.

Final run: all 16 sources passed after rebaking with UV coverage; all four sheets were rendered
and inspected. Python compilation and `git diff --check` passed. The pearl atlas has 0.1221%
dark pixels after padding (below the 1% regression ceiling); the visible seam network is gone
in the studio render, but this is not a claim of mathematically perfect texture continuity.
The three stone-cover assets meet the requested 600–1500 triangle envelope. The largest source
is the root arch at 7,932 triangles; its repeated-placement cost remains an in-game check.

## Remaining gates

User visual acceptance remains pending for B1b. No runtime catalog, resource appearance,
manifest count, release number or installed profile was changed. Installed release remains
0.0.101. There was no game launch, world inspection, collision test or frame-time measurement.

At B2, wire the approved 16 sources, add the Blackwater manifest/rebuild entry, list both shared
kits as inputs, resolve the resource-output glob overlap, and regenerate Blackwater before the
pinned model-count gate. Regenerate Fungal Forest after the existing shared-kit split; consider
switching its current author tool to `bake_flora_atlas` during that required regeneration.
The previously recorded Fungal Forest freshness failure remains expected until B2. Run the
normal offline closeout and installation only after the review gate is satisfied.
