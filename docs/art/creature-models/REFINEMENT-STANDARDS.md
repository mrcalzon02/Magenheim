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

Rift Titan head correction (2026-10-14): the head is one crystal form. The
apparent halves are facets surrounding a recessed, beveled crack in that
continuous shape. Purple crystal belongs deeper inside the fissure. Do not
build independent upright halves with floating purple slivers or a through-air
slot between them. Keep the rear crystal wall continuous, connect the crack
floor and bevels to the outer shell, and check front and left-facing close-ups.
Verify one connected closed head surface and floor depth behind the lips;
reseat the complete neck end sections into that new head with matching weights.

Rift Titan r12 visual audit (2026-10-14): the connected recessed head is still
visually rejected because its smooth leaf outline and plain faces do not match
the angular fractured crystal. Read
`2026-10-14/rift-titan-systematic-audit-r12/AUDIT.md` before further modeling.
Address its open finding IDs and compare the actual reference regions; a closed
head, passing roots or staying under 1,800 triangles does not establish visual
similarity. Preserve the correct single-crystal interpretation while replacing
its incorrect silhouette and surface construction. Check hand reach and a real
foot-contact plane; the audit found shin geometry below the named foot parts.
The old numerical completion forecast is withdrawn.

The user also rejected the first r13 attempt because its overall shape was
still oval. Adding corners, asymmetric facets or a crooked crack to the same
tapered shell does not repair that primary silhouette. The replacement uses an
explicit polygonal slab perimeter with long straight shaft edges, abrupt ledges
and triangular lower breaks. Inspect the actual solid silhouette first; the
replacement remains a candidate, not permission to mark the head accepted.

The user accepted r13's frontal outline, then specified that the fracture is
predominantly frontal, asymmetric and recedes toward the back on the left side,
with purple crystal lining the fracture. Preserve that approved front outline
while correcting its volume: the side reference has a volumetric crystal head,
whose upper point rakes back from the lower point. A forward-pointing extrusion
of the front silhouette is incorrect. Review true and oblique left profiles.
The neck bars must occupy front/back depth around the neck, with both ends
seated into their hosts; a near-planar row of bars is incorrect. r14 is a
candidate for these corrections; it does not establish full head acceptance.

Blackwater Maw anatomy correction: the front reference depicts a tall oval gape
between a peaked upper skull and a pointed, projecting lower jaw. Perspective
depth is not a flared cone. Tooth beds curve toward the cheek hinges; the inside
contains a palate, cheek tissue, mouth floor, posterior wall and recessed throat.

Direction in review renders must match the concept: Blackwater Maw mouth left, tail right. Compare dorsal sweep and lower fin roots in that same view before changing anatomy. Projected fin silhouettes must not be treated as interchangeable duplicate fans.

Blackwater Maw lower-fin correction: the forward blade attaches directly to the body with no shoulder or stubby arm; only the middle blade has a powerful tapered muscular member. The upper jaw follows the body skyline. Distinguish the snout-to-cheek armor ridge from the body crown that joins the first dorsal fin; crown feelers sweep over the body.

Blackwater Maw face: scale the outer rear head; confine flesh to a narrow seam between low armor sections. Keep the crown nub small and mouth brow low. Spread sensory feelers independently in projection; inspect blue bulb tips and eye surfaces for tiny unbaked atlas patches. Fangs need thickness and distinct upper/lower rake. Small-detail supersampling is an offline bake step; final atlas dimensions remain within the donor budget.

## October 16 interpretation and review continuation

The supplied side crop supersedes the earlier Rift Titan median-fracture
instructions above. Preserve a continuous solid stone front plate, with a
separate deep crystal-lined recess on each flank behind it, resembling recessed
eyes. No violet slit passes through the median front plate. Preserve the twenty
accepted frontal X/Z landmarks while matching the long backward side rake and
overlapping stone planes. The upper recess is dark; violet is localized lower
down. Inspect front, true anatomical left, oblique left and opposite-side images.

r15's formerly unbuilt height mask was built during this continuation. Its broad
smooth sockets were rejected in direct comparison; r16 is the narrower stepped
head candidate. r17 preserves that saved head/neck and continues the body. It
corrects the rest-geometry sole/shin ordering and long hand reach, but all broader
art acceptance remains pending. Use the actual current source hash and render
proof; inherited authoring-stage JSON is not proof of the final saved mesh.

Carrion Crown refinement was explicitly requested next. Its native donor is
Greydwarf_Shaman: 1,422 triangles, soft cap 2,133. Read the actual front/side
concept before moving geometry: hollow timber abdomen, broad buttress roots,
one central open crown cup plus four grafted cups, thick uneven rim teeth,
crooked rear crown timber, hanging stem-supported amber pods and diagonal ribs.
Do not substitute a solid barrel, a flat flower mask, or disconnected paddle
petals. Preserve the source's seven bone identities/hierarchy and five retained
actions; inspect complete supports across retained poses. r3 is a starting
anatomy candidate, not adequate art acceptance. Boss tally remains 1/6.

## Rift Titan r18 continuation

Six fingers now have embedded four-point roots, a five-corner knuckle with a forward ridge, and a thick unequal lower section. Each foot comprises two broad flat-topped toe stones and a rear heel fragment, with all sole vertices sharing the existing standing plane. Six mantle slabs retain their chipped fronts while simplified rear sections fund the two inner front ridges and thicker digits. Sixteen unmarked limb plates replace radial face hubs with paired long fracture edges, constrained into their existing outlines, and follow each bone's actual three-dimensional axis. Two high crest stones gain chipped five-corner profiles; the lower rear crest bends around the back through three sections. Secondary core splinters and small crest motes fund the changes at 1,800 triangles. Stone marks now follow each component's local direction, using gray strata, sparse warm/olive mineral, recessed seams and baked edge shading within the existing four 512px maps.

The saved head, six neck bars, stone collarbone and narrow wrapped torso band have zero coordinate difference from r17. All eight saved rest-bone matrices and parent identities also match exactly; seven retained source actions remain. All 76 attachment contracts pass. 30,240 actual marked closing-face samples pass over 36 rest/action poses. Both feet retain the prior sole plane, both shins clear it, and the unequal fingertips clear it by 0.019–0.185 m. The final export has one mesh, one material, four embedded 512px maps and compact normalized skin weights; unused construction paint channels are absent.

The large shoulders, upper cage and core still need more reference-specific construction. The crest needs smaller supporting architecture and more broken layering. Hands and feet are thicker and separated, but their forms remain coarse; stone detail still needs sharper authored chips and more deliberate lichen. This is another revision candidate. The user's “getting closer” does not establish full head or boss acceptance. No native or new adequate-boss acceptance is inferred; tally stays 1/6.
