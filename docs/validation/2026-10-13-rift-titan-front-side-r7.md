# Rift Titan: side orientation correction and front-first silhouette r7

The user requested the whole-creature front silhouette followed by side
silhouette matching before any surface-detail work, then identified another
opposite-facing side view. The r5/r6 saved models were re-rendered from -X so
the authored +Y front projects screen-left. Both whole-model and isolated
comparisons use that direction. The previous opposite-facing whole-model PNGs
are preserved with `--side-right-facing.png` names. Camera projection assertions
reject a side view that sends +Y toward screen-right. No raster mirroring.

r7 fits the frontal primary masses: one long, tapered cleft menhir with a
dominant uneven peak; fuller shoulder/mantle, arm, forearm, hand, leg and foot
stone masses; revised head/body/leg proportions and overall arm spread. Four
former floating stones are reshaped into paired thorax and lower-side masses.
The side fit tapers rock depth toward the ends, staggers depth, projects the
head/core forward and adds knee/foot depth. Both corrected torso rings and the
continuous cage ribs remain, with a modest cage-width fit. Surface texture
content is not refined in this pass; the same compact owned maps are rebaked.

The same spatial fit is applied to the original rig rest positions. All eight
bone identities and parent relationships are independently checked against
the original hierarchy; all seven original actions remain. Rest-layout changes
are an offline art fit, not verified native animation compatibility. The full
support dependency chain is reseated after reshaping, starting with the collar
into its larger shoulder supports, followed by ribs, band and neck.

Source: `assets/models/studies/underworld-boss-rift-titan-silhouette-r7.blend`.
Review: `docs/art/creature-models/2026-10-13/rift-titan-silhouette-r7/gallery.html`.
Measured cost: 1,800 triangles / native Stone Golem 1,200 / soft cap 1,800;
one mesh and material, four packed 512px maps, 7 m study height. Saved finite,
nondegenerate, manifold mesh/weights/export checks pass; both rings retain
closed connected topology. All 50 attachment contacts pass rest and 35 sampled
action poses with zero measured outside distance at 1 mm tolerance.

Review includes frontal clay, true left-facing side clay, front-biased
left-facing views and matched r6/r7 comparison cameras. Additional portraits
fit framing to the supplied crops; an oblique view is labeled separately from
the true profile. Saved proportions and source provenance are recorded. The
reference camera/perspective is not known exactly; neither these measurements
nor checks establish an exact concept match or adequate boss art acceptance.

Native loader admission, every-frame animation deformation and live frame cost
remain separate. No runtime model promotion or installation. The accepted boss
tally remains 1/6. Continue silhouette refinement before texture/surface detail;
Blackwater Maw remains paused and the accepted First Bloom baseline is retained.
