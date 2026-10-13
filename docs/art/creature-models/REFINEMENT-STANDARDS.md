# Creature refinement standards

These standards record the user's First Bloom review corrections and apply to
subsequent concept-guided boss studies. Each creature needs its own anatomy pass.

- Identify the real anatomical groups from front, side and detail concept views
  before moving geometry. Distinguish head, torso growths, joints, cavities and
  attached tissue; do not infer anatomy from the current primitive construction.
- Use the corresponding native donor's visible high-detail model as the quality
  bar and cost reference. The soft triangle cap is 1.5 times that donor's count.
  Counts and valid files alone do not establish comparable visual quality.
- Improve coherent surfaces, silhouette and articulation before adding density.
  Use geometry for major folds, plates, shelves and membranes; bake fine texture
  detail into a compact owned atlas. Avoid unsupported collections of rods.
- Record explicit supports for roots of teeth, fins, armour, sensory organs and
  tissue. Seat the whole endpoint section and use matching support weights.
  Check the saved mesh independently, including sampled retained actions.
- A supported boundary does not prove its closing face is hidden. Inspect and
  sample closing-face interiors; avoid nonplanar caps and collapsed root loops.
  Review close-ups, clay, side profiles and local backfaces when a defect appears.
- Remove only wholly buried faces verified against retained closed supports.
  Audit them across retained action samples. Preserve partial intersections,
  structural marker vertices, UVs, materials and weights; check every saved cut
  boundary. Conservative trimming is not a claim of a watertight Boolean union.
- Preserve the appropriate rig and retained actions. Keep study skinning compact
  (currently no more than two influences per vertex), finite and normalized.
  Respect the donor's texture/rendering cost; record the actual study costs.
- Deliver editable study sources, portable skinned exports, matched comparison
  renders, concept and donor references, measurements and provenance. Preserve
  preceding reviews. Keep user art acceptance, offline checks and native runtime
  admission/deformation/performance acceptance separate.

Current working references: Troll 6,206 triangles / soft cap 9,309 for First
Bloom; Serpent 4,872 / soft cap 7,308 for Blackwater Maw. Counts are from the
local extracted native donor evidence, not from generated creature studies.

Rift Titan torso correction (2026-10-13): the very narrow sculpted stone band
wraps around the entire torso; continuous metal ribs surround the crystal core
above and below it. The central V is a small bend in that band, not a thick
front plate or medallion. The upper stone collarbone is also a ring, joining
the larger upper torso stones at both sides and supporting the metal neck ribs
into the head. Check front, side and rear views; inspect isolated saved torso
geometry when arms hide the wrap. Stone Golem reference: 1,200 triangles,
soft cap 1,800. r6 remains an unaccepted torso candidate.

Rift Titan side-review orientation is fixed: head/front screen-left and back
screen-right, matching the supplied concept. The authored front axis is +Y;
render from -X and assert that +Y projects toward screen-left. Apply that same
camera direction to both sides of every comparison, including isolated torso
views. Do not infer direction from the current model or mirror raster images.

The user's next Rift Titan priority is the whole-creature front silhouette,
followed by the side silhouette, before surface detail. Compare primary masses
in clay against the supplied front and left-facing side references. Preserve
the corrected torso rings and attachments while fitting proportions and depth.
Show the true profile separately from an oblique reference-angle view. r7 is a
shape candidate; “work is improving” is not adequate boss acceptance.

Limb plate alignment must follow the joint direction in three dimensions,
including elbow/wrist depth. An XZ-only tilt can match the front while still
reading as stacked blocks in profile. Inspect those same joints in side clay;
seat the full proximal section into its support and use compatible root/blend
weights. Digit roots need the same support/weight check as larger attachments.

Blackwater Maw anatomy correction: the front reference depicts a tall oval gape
between a peaked upper skull and a pointed, projecting lower jaw. Perspective
depth is not a flared cone. Tooth beds curve toward the cheek hinges; the inside
contains a palate, cheek tissue, mouth floor, posterior wall and recessed throat.

Direction in review renders must match the concept: Blackwater Maw mouth left, tail right. Compare dorsal sweep and lower fin roots in that same view before changing anatomy. Projected fin silhouettes must not be treated as interchangeable duplicate fans.

Blackwater Maw lower-fin correction: the forward blade attaches directly to the body with no shoulder or stubby arm; only the middle blade has a powerful tapered muscular member. The upper jaw follows the body skyline. Distinguish the snout-to-cheek armor ridge from the body crown that joins the first dorsal fin; crown feelers sweep over the body.

Blackwater Maw face: scale the outer rear head; confine flesh to a narrow seam between low armor sections. Keep the crown nub small and mouth brow low. Spread sensory feelers independently in projection; inspect blue bulb tips and eye surfaces for tiny unbaked atlas patches. Fangs need thickness and distinct upper/lower rake. Small-detail supersampling is an offline bake step; final atlas dimensions remain within the donor budget.
