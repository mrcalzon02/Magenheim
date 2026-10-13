# Rift Titan ribbed torso r5

The user corrected the r4 basket interpretation against the adopted concept:
numerous metal torso ribs around the crystal core, a central stone V band,
an upper stone collarbone and metal neck ribs into the head. Their supplied
close-up shows a solid downward bend in the band. Front and side crops are
preserved beside the matched render in the r5 gallery.

The torso rebuild has six front ribs seated into the V band, eight swept ribs
continuing beneath it to a small lower keel, and six collar-to-head neck ribs.
Parallel-transported cross-sections avoid roll flips along the bent ribs. The
central stone band uses explicit connected strips: triangulating a nonplanar
concave ngon bridged the V in an intermediate render and was corrected before
publication. Metal ribs have a rough dark baked surface; final corrosion and
stone fracture/weathering detail remain unfinished.

Saved source: `assets/models/studies/underworld-boss-rift-titan-torso-r5.blend`.
Review: `docs/art/creature-models/2026-10-13/rift-titan-torso-r5/gallery.html`.
1,780 triangles against native Stone Golem 1,200 / soft cap 1,800; one mesh and
material, four packed 512px maps, eight bones and seven retained actions.
Saved geometry/export checks pass. All 40 full-section rib contacts pass the
independent saved-mesh audit across 35 sampled poses with 1 mm tolerance and
zero measured outside distance. Front, side, clay, close-up and attack renders
are supplied. These checks do not establish art or native runtime acceptance.

This is a torso/neck-only comparison against r4. `rift_titan_r4_base.py` preserves
the builder used by r4 (971e393a), while the evolving primary builder retains
the concurrent online mantle/head/eye-socket changes through 560bf35e. Those
changes are preserved in main and not silently included in the r5 screenshots.
The adequate boss tally remains 1/6; Rift Titan remains under review and
Blackwater Maw remains paused. No runtime model promotion or installation.
