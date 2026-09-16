# Valheim-style staff baseline and Spirit rebuild — 2026-09-15

## Intent

Use the existing Valheim magic-weapon family as the minimum production bar for Magenheim staff silhouette, construction complexity, material separation and magical focal treatment, then immediately apply that bar to one existing Magenheim family that still presented as primitive geometry.

## Reference analysis

The vanilla Staff of Embers, Staff of Frost, Staff of Protection and Dead Raiser are low-poly assets, but their low polygon count is not equivalent to low design complexity. Their visible quality comes from shape hierarchy: long two-handed proportions, shaft profile changes, branch/fork/cage structures, a substantial head assembly, physical material separation and a concentrated magical focus.

The Frost staff demonstrates an elongated luminous focus held by a surrounding framework rather than a crystal simply attached to the top of a cylinder. Embers uses irregular branch/cradle language around its focus. Blood-magic equipment shifts toward bone/skull/ritual-object language. Across the family, emission is concentrated on the supernatural component while wood, bone and metal remain readable as physical surfaces.

This analysis is now durable policy in `docs/STAFF_VISUAL_STANDARD.md`. The standard explicitly rejects completed staffs that can accurately be described as a straight stick with a lump on the end.

## Spirit source defect

`SpiritVisuals` still assembled all four Spirit tiers from `GameObject.CreatePrimitive` cubes, cylinders and spheres. Although the family had multiple pieces, the primitives produced smooth generic shafts, spherical magical heads and cage elements with weak silhouette hierarchy. The result did not meet the established Valheim equipment bar and was inconsistent with the repository's newer canonical mesh authority.

## Implemented repair

`SpiritStaffRegistrar.cs` now builds the four Spirit models through `RuntimeMeshPrimitives` and shared cached canonical cylinder/prism geometry. Raw Unity primitive presentation has been removed from the Spirit model path.

The rebuilt family uses segmented, slightly crooked/tapering shafts; visible grip wraps; forked and ribbed head construction; faceted prism foci instead of smooth spheres; bone, dark wood, leather, silver and crystal material roles; deliberate asymmetry; tier-specific secondary shards/chimes; and small focal lights concentrated on the magical head.

Tier growth is structural rather than a color/scale change:

- Simple uses a crooked segmented shaft, wrapped grip, asymmetric bone fork, broken tine and two faceted Spirit pieces.
- Crystal adds a reinforced shaft, metal collar, four-part lantern cage, return ribs, large framed crystal, inner heart and hanging splinter.
- Advanced adds a six-voice ritual cage, paired rib construction, secondary crystals, asymmetric bone hook/chime and additional shaft reinforcement.
- Master adds layered shaft bands, three major upper spires, an eight-rib reliquary cage with braces, central reliquary/heart, six satellite echo shards and a hanging chime.

All owned materials still flow through `GeneratedSurfaceTextures`; the magical focus carries emission while structural wood/bone/metal remain non-emissive. The hidden vanilla `StaffIceShards` prefab remains only a functional animation/equipment carrier and its renderers remain disabled after the owned model is composed.

## Authority and architecture

No new geometry authority was introduced. The rebuild consumes the existing tested `Magenheim.Core.Geometry.MeshPrimitives` through `RuntimeMeshPrimitives`. Meshes are cached by side count and reused within the family rather than generated privately per component.

No combat, recipe, persistence, socket or multiplayer authority changed in this slice.

## Validation boundary

The authoritative source and commit ancestry are verified on `main`. This connector environment does not compile against the installed Valheim/Jotunn runtime and does not render the model, so compile and screenshot-level acceptance are not claimed.

Live acceptance must inspect all four Spirit tiers in hand, on the ground and in inventory presentation. Verify hand alignment, overall two-handed proportion, no hidden-carrier bleed-through, no severe UV seams, visible wood/bone/metal/crystal separation, restrained focal emission and readable tier silhouettes at normal third-person distance.

A model fails acceptance if any tier still reads primarily as a straight rod plus one terminal primitive.

## Next actionable slice

Apply `docs/STAFF_VISUAL_STANDARD.md` across the remaining Magenheim staff families. Audit Fire, Frost, Storm, Earth, Venom, Radiance and Seidr for the same failure modes: uniform single-segment shafts, primitive dominant heads, insufficient material roles, radial procedural symmetry, generic icons or tier progression expressed mainly through brightness/scale. Repair the worst offender directly at its authoritative visual builder, one family per bounded slice.
