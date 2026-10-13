# Rift Titan continued silhouette and joint refinement r8

The user's r7 assessment is “still so far off but you're improving.” This is
authorization to continue the front/left-side silhouette, not art acceptance or
permission to skip ahead to texture work. r7 and all preceding reviews remain.

r8 changes the existing stone allocations rather than adding density: shoulder
plates slope and hang more steeply, elbows move back and wrists forward, and
arm plates follow the full three-dimensional joint direction rather than an
XZ-only tilt. Individual wedge sections reduce the repeated square-ended
profiles. The stance is slightly closer, knees project forward, and shorter
stone digits are seated into the palms. An intermediate foot shape read too
flat in clay; its thickness was restored before publication. The r7 cleft
head, both closed torso rings and the body/cage supports remain.

The same spatial adjustment is applied to the arm/leg rest positions while
preserving all eight bone identities and parent relationships and seven
actions. Two proximal arm roots now have six full-section vertices seated into
the shoulder stones with matching Body weights. Each proximal stone has two
45% Body / 55% Arm face-ridge vertices; its remaining vertices follow Arm.
Each of six digit roots has six vertices seated into its palm with matching
Forearm weights. These weights are independently checked in the saved mesh;
no vertex exceeds two influences. All old supports are reseated after shaping.

Source: `assets/models/studies/underworld-boss-rift-titan-joints-r8.blend`.
Review: `docs/art/creature-models/2026-10-13/rift-titan-joints-r8/gallery.html`.
Measured cost remains 1,800 triangles / Stone Golem 1,200 / soft cap 1,800,
one mesh/material and four packed 512px maps. Saved finite, manifold,
nondegenerate mesh, normalized weights, portable skin export and original rig
hierarchy checks pass. Both torso rings retain connected closed topology.
All 58 support contacts pass rest and 35 sampled action poses at 1 mm tolerance
with zero outside distance. The two arm and six digit seatings and four blended
ridge vertices pass the additional ownership/weight audit.

The 32 closing triangles at these eight new root sections also pass 8,064
center/interior/edge sample checks across rest and 35 action samples. Their
measured outside distance is zero at the same 1 mm tolerance. This checks the
closing faces, rather than inferring hidden caps from their seated vertices.

Review cameras retain the left-facing guard: +Y front projects screen-left.
True profile is separate from the front-biased reference-angle view. Matched
r7/r8 renders use the same framing/cameras; the preserved r7 images are copied
without mirroring. Source and recipe provenance is recorded in the gallery.

The finer rock fragmentation and silhouette irregularity remain unfinished.
Offline support checks do not establish good every-frame native deformation,
native loader admission or live frame cost. No runtime model promotion or
installation. Boss adequate tally remains 1/6; Blackwater Maw remains paused.
