# Magenheim staff visual standard

## Purpose

Magenheim staff models must read as native-quality Valheim equipment rather than debug geometry with a magical color applied. Existing Valheim magic weapons are the minimum visual bar for silhouette complexity, material separation, readable construction and magical focal treatment. This document is a production rule for every new or rebuilt Magenheim staff family.

The reference family includes the vanilla Staff of Embers (`StaffFireball`), Staff of Frost (`StaffIceShards`), Staff of Protection (`StaffShield`) and Dead Raiser (`StaffSkeleton`), plus later Ashlands/Deep North-era magic weapons where their construction language is useful. Magenheim may use a vanilla item as a hidden functional carrier, but a user-facing Magenheim staff must have an owned silhouette unless the design intentionally calls for a specific vanilla donor object.

## What Valheim's staves actually do visually

Valheim's staves are low-poly, but they are not primitive. Their quality comes from deliberate shape hierarchy rather than polygon count.

The shaft is never the entire model. It establishes a long two-handed proportion, then changes profile near the upper third through bends, forks, wraps, collars, cages, roots, bone, metal or other secondary construction. The eye is drawn to a head assembly that occupies substantial visual volume and has a recognizable silhouette even when the item is seen from several metres away.

The head is not simply a sphere on a stick. The Staff of Frost uses an elongated luminous crystal held by an open framework; the Staff of Embers uses an irregular branch-like cradle around a fiery core; blood-magic pieces use bone, skull, cage and ritual-object language. The focal magical object is supported by visible structure, so the model reads as an intentionally built implement rather than a primitive carrying an effect.

Vanilla models also use controlled asymmetry. Forks, branch bends, bone projections and support arms break perfect radial symmetry. Even when a head has a circular or caged theme, the supporting structure has enough variation that the item does not look lathed or procedurally stamped.

Material separation is part of the geometry language. Wood, bone, metal, crystal, wrapped grip material and magical emission occupy different physical components rather than being represented only by tint changes on one continuous mesh. The low-poly facets are visible, but each facet belongs to a coherent object with believable construction.

Emission is concentrated. The magical focus glows, while the entire staff does not. This preserves readable wood/bone/metal surfaces and makes the supernatural component look embedded in a physical object. Staff of Embers and Staff of Frost also provide faint light from their heads, reinforcing the focus without washing out the whole model.

Texture treatment is stylized rather than photorealistic. Large forms carry readable directional grain, mineral variation, metal response or bone variation. Textures support the silhouette; they do not substitute for it. A flat cuboid with a high-resolution texture is still a failed staff.

## Mandatory Magenheim construction rules

A Magenheim staff must satisfy all of the following before it is considered production-ready.

1. **Two-handed silhouette.** Overall proportion must be comparable to vanilla magic staves: a long shaft with a head assembly large enough to remain identifiable in third person and inventory presentation.
2. **Profile variation.** The shaft must change profile through taper, branching, collars, wraps, ribs, offsets or an equivalent authored transition. One uniform cylinder from hand to head is insufficient.
3. **Head assembly, not head primitive.** A magical focal crystal/orb/reliquary must be supported by a cradle, fork, cage, antler, rib, crown, ring or other multi-part structure. A visible sphere/cube/prism directly on a shaft is prohibited as the completed design.
4. **Intentional asymmetry.** At least one major silhouette feature must break perfect radial symmetry unless symmetry is essential to the item's fiction. Symmetric cages may exist, but they need secondary asymmetry in branches, hanging elements, grip construction or focal placement.
5. **Material hierarchy.** Production staffs require at least three visually distinct material roles chosen from wood/root, bone, metal, leather/wrap, crystal/mineral and magical focus. Higher tiers should normally use four or more.
6. **Tier growth is structural.** Tier progression cannot be represented only by scale or brighter emission. Higher tiers must gain additional construction layers, stronger head framing, more developed grips/collars, secondary focal pieces, or more complex silhouette work.
7. **No raw Unity primitive presentation.** `GameObject.CreatePrimitive` cubes, spheres and cylinders are not accepted as the final visible staff body. Canonical Magenheim mesh primitives may be used as low-level solids, but they must be composed into an authored silhouette and crystal forms should prefer faceted prisms over smooth primitive spheres.
8. **No dominant cuboids.** Boxes may be used for small clamps, plates and structural details. A box may not form the dominant shaft, head, crystal or magical focus.
9. **Owned surface treatment.** Visible owned materials must use the shared `GeneratedSurfaceTextures` authority or an authored file-backed texture. One-pixel white, null, debug or flat tint-only surfaces are prohibited.
10. **Concentrated emission.** Emission belongs on crystal/focus/rune components. Wood, bone and most structural metal should retain readable non-emissive shading.
11. **Readable at distance.** The silhouette must remain distinct when the head is reduced to roughly icon/third-person scale. Tiny decorative parts cannot be the only difference between tiers or elements.
12. **Icon coherence.** Inventory icons must represent the actual final visual family rather than a generic colored crystal if an owned model-specific icon path is available.

## Complexity floor

These are Magenheim production targets, not claims about exact vanilla polygon or object counts.

A **Simple** staff should normally use roughly 10-14 visible structural/focal components, at least three materials, one major fork/cradle feature and one secondary grip/collar detail.

A **Crystal** staff should normally use roughly 14-20 visible components, with a clearly framed crystal/focus, multiple support arms or ribs, at least one asymmetrical accent and a more developed lower shaft/grip.

An **Advanced** staff should normally use roughly 18-26 visible components, with layered head construction, secondary foci or repeated ritual elements, several profile changes down the shaft and four-material separation where appropriate.

A **Master** staff should normally use roughly 24-36 visible components, but the goal is hierarchy rather than noise. It should have a primary silhouette, secondary framing, tertiary ornament and a deliberately staged emissive focus. More parts do not excuse poor proportion.

## Geometry and performance

Valheim's style depends on strong low-poly forms, so Magenheim should spend geometry on silhouette changes rather than smoothness. Eight- to twelve-sided cylinders/prisms are generally enough for staff-scale parts. Crystals should remain visibly faceted. Repeated components should reuse cached canonical meshes instead of allocating private mesh copies.

For runtime-generated staff bodies, use `RuntimeMeshPrimitives` as the geometry authority so winding and closed-surface behavior remain deterministic. Build organic-looking shafts from several short, slightly rotated/tapered segments rather than one perfectly straight cylinder. Use `RuntimeMeshPrimitives.Prism` for focal crystals and shard clusters. Use boxes only for small clamps/plates.

## Texture and material target

Wood/root surfaces need directional coarse/fine variation that reads along the shaft. Bone should be pale but not flat white, with low gloss and subtle value breakup. Metal should have darker albedo, higher metallic response and restrained gloss. Crystal/mineral surfaces should combine faceted geometry, mineral variation and controlled emission. Leather/wrap should remain rough and low-metallic.

The shared generated texture baseline is 256x256 with mipmaps, trilinear filtering and anisotropic filtering. That is the minimum procedural surface quality. File-backed authored textures may exceed it. UVs must be usable; the owned post-registration UV repair remains a fallback, not an excuse to ignore mapping in authored models.

## Acceptance checklist

A staff family fails review if, at normal third-person distance, it can be described accurately as "a stick with a lump on the end." It also fails if its tier progression is only brighter color, if the magical head has no visible support construction, if large surfaces are untextured/flat, if the dominant form is a cube, or if the vanilla carrier remains visibly identifiable as the user-facing item.

For live acceptance, inspect every tier in hand, on the ground, in the inventory icon and under bright/dark lighting. Check silhouette, hand alignment, clipping, emission balance, UV seams, material readability and whether the family is visually distinct from its hidden vanilla carrier.
