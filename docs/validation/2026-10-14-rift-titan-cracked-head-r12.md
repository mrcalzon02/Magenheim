# Rift Titan one-crystal head with recessed beveled crack r12

The user corrects the interpretation: the purple crystal belongs inside the
gap, and the apparent halves are surfaces of one crystal. It is a crack in
one shape/surface with a recessed bevel. r11's independent upright shells and
floating purple slivers are incorrect for this head and are superseded.

r12 replaces four outer head fragments and the two loose head slivers with
one connected closed shell. Seven cross-sections each share eight vertices:
outer front cortex, fissure lips, bevel walls, recessed purple floor and a
continuous rear wall. The purple floor is part of the same geometry as the
bevel and outer surface, not a detached object protruding through an air slot.
The original forward rake and tip height remain. Six neck-root sections are
reseated into the new rear/base surface with matching Body weights.

Source: `assets/models/studies/underworld-boss-rift-titan-cracked-head-r12.blend`.
Review: `docs/art/creature-models/2026-10-14/rift-titan-cracked-head-r12/gallery.html`.
The head has 56 vertices and 108 triangles: 76 cortex/rear/end triangles,
24 bevel triangles and eight purple floor triangles. Its component is fully
connected, closed, with Euler characteristic two. The floor is behind the
front lips and ahead of the solid rear wall at all seven measured sections.

The complete actor now has 1,792 triangles, eight fewer than r11, against the
Stone Golem's 1,200 and soft cap 1,800 (1.493×). One mesh/material, four packed
512px maps, original eight-bone hierarchy and seven actions remain; 7 m study
height is preserved. The existing owned material style is re-baked for the
new head geometry. No native material-provider or runtime loader changes.

Saved finite/manifold/nondegenerate geometry, normalized compact weights,
hierarchy/actions, atlas and portable skinned export checks pass. All 76
existing support groups pass rest plus 35 sampled action poses. Closing-face
interiors/edges pass 8,064 arm/digit, 14,112 limb-crystal, 2,016 upper-thorax
and 1,512 neck/head samples across 36 poses, with zero measured outside
distance at the unchanged 1 mm tolerance. The head and neck seatings have
matching Body ownership.

All retained body/limb/shoulder/torso/core components are compared against
the actual saved r11 mesh. Triangle/vertex allocations match and the maximum
coordinate difference is zero. The replacement head and six deliberately
reseated neck-end sections are excluded from that preservation comparison.

Actual r11 front and left-facing head close-ups are rendered into the r12
before folder. Prior study files remain unchanged. The side projection guard
and source/recipe hashes are retained. User acceptance of this interpretation,
85% overall match, native every-frame deformation and live performance remain
pending. No runtime promotion or installation. Adequate boss tally stays 1/6;
Blackwater Maw remains paused.
