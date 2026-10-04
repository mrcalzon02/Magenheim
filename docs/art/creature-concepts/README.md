# Creature design references

The [3 October 2026 reference set](2026-10-03/README.md) is the adopted visual
target for Magenheim's Underworld creature construction. The user established
these sheets as the design language to distill into Valheim-scale geometry and
textures on 3 October 2026.

It covers 42 ordinary creatures and six bosses. Start with the
[filterable gallery](2026-10-03/gallery.html) or the
[boss overview](2026-10-03/bosses--overview.jpg). Each biome has its own overview
and each creature has an individual full-size sheet. The selected filenames,
prompts and image hashes are preserved with the dated set.

## Translating a sheet into a game creature

1. Establish the body plan and measured proportions from the creature art/rig
   standard. Resolve any disagreement between generated views into a single
   construction drawing before building the mesh. The concept's scale figure is
   illustrative; the production size bands govern actual dimensions.
2. Block out the silhouette, center of mass, joint placement and attack organs.
   Compare the result with the concept from the front, side and three-quarter
   view at player eye height and normal combat distance.
3. Build connected, purposeful anatomy. Muscles taper into joints; shell plates
   overlap in a load-bearing direction; fungal growth joins living tissue; stone
   masses have structural relationships. Adding detail to a collection of blobs
   does not satisfy the target.
4. Reserve geometry for the silhouette, joints, openings, major material edges
   and organs that must move or visibly telegraph attacks. Put fine pores,
   fibers, bark grain, gill striations, small cracks and surface weathering into
   owned albedo/normal/roughness maps. Keep texture scale consistent across body
   parts and comparable to the appropriate native Valheim creature.
5. Compare the actual donor and custom creature under the same lighting and
   framing. Check triangle count, renderers, material count, texture memory,
   animation cost and distant readability. A high polygon count is not evidence
   of fidelity; spend the budget where it changes the visible form.
6. Validate the complete animated body: planted feet, limb clearance, mouth
   articulation, attack anticipation, exposed vulnerable organs and death pose.
   Preserve native gameplay/network behavior and existing attack sockets while
   replacing presentation. Follow AGENTS.md for supported native materials.

Bosses need the distinct encounter silhouettes shown in their sheets. Related
creatures may share rig fundamentals, but their anatomy must retain the separate
identities in the references. Furnace, glacial and fracture constructs must not
collapse into one model with three material swaps.

## Reference stewardship

Keep the full-size PNGs, selected-image manifest, prompts and refinements in Git.
The gallery, thumbnails and contact sheets make the collection practical to
review, but the PNGs are the primary visual references. The current dated set
includes three retained earlier proposals; use the selected filenames in its
index and manifest, including the `--v2` revisions.

Preserve this adopted baseline when exploring new alternatives. Add another
dated set or sibling revision and explicitly identify which reference it
supersedes. Do not silently replace a creature's target during model generation.
The prompt preparation tool refuses to overwrite this existing curated set.

These are project documentation assets, separate from shipped runtime model and
texture payloads. No concept PNG is automatically a finished game texture.

The generation/review record is [REVIEW.md](2026-10-03/REVIEW.md). The underlying
anatomy, scale and rig requirements remain in
[the creature art/rig standard](../../UNDERWORLD_CREATURE_ART_RIG_STANDARD.md).
