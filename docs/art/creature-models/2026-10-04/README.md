# Boss construction pass 1

Latest Blackwater Maw study: [revision 8 — corrected spined fins](blackwater-maw-spined-fins-r8/gallery.html).
Four dorsal fins sweep tailward, with raised rays, recessed torn membranes and
real perforations. Large forward pectorals and smaller rear pelvic fins have
separate roots and bindings. The broad tail fan has 4.66× the previous projected
membrane area. Flank blocks are removed; owned painted scales carry the detail.
The body/jaw scale is preserved; the tail extends the art-study span to about
10.52 m. Final count: 7,224 triangles (1.483× Serpent), below the 7,308 soft cap.
Saved geometry, fin inventory/direction and 112 contacts across 65 pose samples
pass. Native runtime and full animation acceptance remain pending.

Previous Blackwater Maw study: [revision 5 — oval gape and pointed jaws](blackwater-maw-jaws-r5/gallery.html).
The mouth is a tall tapered oval with a peaked brow, a pointed lower-jaw chin,
curved tooth beds, broad palate, fleshy posterior wall and raised mouth floor.
The separate jaw remains articulated on the retained Deep Hunter rig. The body
has connected fin webs, cheek armour, physical flank scales and sensory sockets.
Final count: 6,412 triangles (1.316× the native Serpent), below the 7,308 soft cap.
One mesh/material, four 512px maps, at most two influences per vertex. Saved
geometry, 155 contacts, 65 action samples and conservative buried-face trimming
pass offline checks. This is an uninstalled art candidate; native acceptance
and further concept-detail refinement remain pending.

Latest First Bloom study: [revision 22 — repaired arm-to-shoulder seams](first-bloom-arm-seams-r22/gallery.html).
The visible pinched closing polygon also occurred before hidden-face trimming.
Both arm roots now close on planar patches inside rigid torso support triangles,
with ordered transition rings and refitted tissue contacts. Closing patch interiors
are sampled as well as their boundary vertices. Local paired clay and backface
views show the repaired connection. Final trimmed count: 8,846 triangles (1.425×
donor), with 157 attachment records and passing offline geometry and sampled
pose checks. Native acceptance remains pending.

Previous First Bloom study: [revision 21 — shoulder bundles, four-cap head and beard](first-bloom-bundles-r21/gallery.html).
Four broad lateral caps form torso-grown shoulder bundles bound to Chest. Four
central caps form a separate forward-projecting head bound to Head. Six existing
pale curtains are widened into a tissue beard with supported head and chest
contacts. Explicit roots attach the shoulder bundles, head core and central caps.
The body scale is preserved; the art-study height is approximately 4.112 m.
Final trimmed count: 8,862 triangles (1.428× donor), with 157 attachment records
and passing offline geometry and sampled pose checks. Native acceptance is pending.

Previous First Bloom study: [revision 20 — compact concept crown](first-bloom-head-r20/gallery.html).
The head is compressed vertically above the crown base, with flatter mushroom
centers, a low crest and a gentle forward sweep in profile. Width and body scale
are preserved. Hanging tissue and dependent crosslinks are reseated. The art-study
height is approximately 4.222 m; the skeleton retains its previous transforms
within float32 rescaling roundoff. Final trimmed count: 8,886 triangles (1.432×
donor), below the 9,309 soft cap. Offline geometry and sampled contacts pass;
native animation and performance acceptance remain pending.

Previous First Bloom study: [revision 19 — compressed shoulder joints](first-bloom-shoulders-r19/gallery.html).
The arm tops are narrowed and pulled inward/back into the torso beneath the
mantle. Ten root vertices per arm are seated in the torso with a two-bone
transition toward each upper arm. Dependent tissue contacts are reseated.
All 22 saved skeleton bones retain identical hierarchy and rest transforms.
Final trimmed count: 8,884 triangles (1.432× donor), with 148 attachment records
and passing offline geometry and sampled pose checks. Native acceptance remains
pending; this is an uninstalled art study.

Previous First Bloom study: [revision 18 — lateral shelves and central bark](first-bloom-sides-r18/gallery.html).
The hip mushrooms grow from the sides of the torso, leaving continuous exposed
bark down its middle. Every cap remains on its own side with embedded torso roots.
Final trimmed count: 8,880 triangles (1.431× donor), below the 9,309 soft cap.
This is an uninstalled art study with offline geometry and sampled pose checks.

Previous First Bloom study: [revision 17 — torso shelves over hips](first-bloom-pelvic-r17/gallery.html).
The four upper-leg caps are reinterpreted as overlapping lower-torso shelves:
embedded roots, broad drooping overhangs above the joints, and torso skin weights.
Existing geometry is reshaped without adding loops. Final trimmed count: 8,872
triangles (1.430× donor), with 146 attachment records and passing offline geometry
and sampled pose checks. This is an uninstalled art study.

Previous First Bloom study: [revision 16 — hips and lower torso](first-bloom-hips-r16/gallery.html).
The upper legs have fuller hip roots and explicit pelvis contacts. Short branching
hip buttresses connect at both ends; a raised groin arch replaces the straight
lower torso apron. Existing loops are reshaped without adding triangles. The final
trimmed study has 8,824 triangles (1.422× donor), with 142 attachment records and
passing offline geometry and sampled pose checks. The chest and feet shaping is
preserved. Live performance and full animation acceptance remain pending.

Previous First Bloom study: [revision 15 — root feet](first-bloom-feet-r15/gallery.html).
Existing foot loops form broader, staggered root fans and lower ankle brackets;
ten root bases have explicit seated contacts. The saved study has 8,830 triangles
(1.423× the Troll donor), below the 9,309 soft cap. Geometry, 132 contacts,
sampled attack poses and buried cut boundaries pass offline checks. This remains
an uninstalled art study; live performance and full animation acceptance are pending.

The 4 October instruction starts with bosses and continues through the largest
creatures toward the smallest. This overrides the older biome-first art order
for this concept adaptation campaign.

[Open the comparison gallery](gallery.html). Each of the six bosses has an
independent editable Blender source, a portable rest-pose GLB, three inspection
views and an inherited action pose. The gallery links each source and the
corresponding adopted concept. Original ordinary-creature sources are preserved.

These are **first construction studies**, not art-complete or accepted runtime
creatures. Triangle counts reflect silhouette blocking rather than final boss
detail. Provisional boss dimensions are recorded for review; they do not change
encounter scale, collision, balance or gameplay. No production runtime model
identity, boss registration, release or installed profile was changed.

## Authored changes

| Boss | Initial construction | Size study |
| --- | --- | --- |
| First Bloom | Asymmetric ochre shelves, exposed ribbed gills, living root cords, separate talons and root feet | 4.65 m tall |
| Blackwater Maw | Thick open jaw vault, inner throat, physical teeth, full fin webs, sensory organs and tapered trunk | 10 m long |
| Furnace Heart | Heavy boiler cage, broken masonry braces, impact fists and unequal vent chimneys | 5.2 m tall |
| White Silence | Narrow hollow spine, hanging shard mantle, cleft crown and long ice claws | 5.5 m tall |
| Rift Titan | Suspended stone segments, open binding cage and split menhirs | 7 m tall |
| Carrion Crown | Massive root trunk, bone/sepal crown, amber organ and hanging seed pods | 6.8 m tall |

Seventeen owned 2048 px material studies include albedo, normal, roughness and
metallic/smoothness maps. Six organ materials include emission. Concept images
remain references; no concept painting was cropped into a game texture. Maps
are packed into the editable sources; the models use the existing supported
source PBR convention for eventual native import.

The six species skeletons, sockets and named actions are retained as retargeting
baselines. Rest and selected inherited action poses were rendered and visually
inspected. This is not acceptance of locomotion, attack timing, joint clearance
or animation quality. The current runtime supports rigid bone-bound parts; these
studies retain that contract rather than claiming continuous soft-skin deformation.

## Validation

[Source verification](source-verification.json) records six passing art-integrity
checks: finite/nondegenerate geometry, closed surfaces including the open-mouth
shell, UVs, one material per renderer, complete rigid bindings, packed 2K maps,
measured scale, retained named actions and unchanged adopted concept hashes.

[Provenance](provenance.json) records generator, source, texture, GLB and render
hashes. [The construction manifest](boss-construction-manifest.json) records each
source, reference, dimensions, material list and remaining acceptance state.
[Interchange verification](interchange-verification.json) checks all six portable
GLBs, including embedded images/buffers, mesh attributes and recorded hashes.

Rebuild in this order, using the bundled Python with NumPy and Pillow for maps:

1. `generate-boss-concept-textures.py`
2. `tools/blender.ps1 author-boss-concept-models`
3. `tools/blender.ps1 render-boss-concept-review`
4. `tools/blender.ps1 verify-boss-concept-sources`
5. `tools/blender.ps1 export-boss-concept-studies`
6. `build-boss-concept-review.py`
7. `verify-boss-concept-review.py`

## Next production work

Refine the remaining concept gaps listed in the gallery, inspect all inherited
actions, then author boss-specific animation, collision/hitbox and LOD plans.
Runtime promotion must replace the enlarged-species presentation in the boss
registrar and use the existing native material and creature binder paths. Native
shader, locomotion, attacks and multiplayer observation remain required before
calling a boss accepted. Installation belongs to that runtime testing closeout.

[The 48-entry production order](production-order.json) places all six bosses
first, followed by ordinary creatures sorted by the upper end of the adopted
size band. The next largest are Deep Hunter, Corpse Orchard, Rift Colossus,
Abyss Shellback, Furnace Golem and Graft Warden. Wing spans, reaches and shoulder
heights are retained as their stated dimensions rather than relabeled as height.
