# First Bloom chest frills, revision 6

[Before/after gallery](gallery.html) · [Concept](../../../creature-concepts/2026-10-03/the-first-bloom.png)

The chest features 22 thin, broad fungal shelves rooted inside the cavity walls. Each has a free leading edge; opposing shelves do not converge into a sternum. Flat broad faces and folded edges replace rounded rib-like forms. A small recessed amber growth replaces the long central rod, and six straight glowing struts have been removed.

Surrounding white tissue contacts were reseated against the reshaped surfaces. The saved mesh retains explicit support and endpoint markers: 98 structural ends across 60 groups, including 22 frill wall roots. The 22 free frill edges are separately declared; they are supported at their opposite root.

The candidate uses 9,032 triangles versus the Troll donor's 6,206 (1.455×), below the 9,309 soft cap and 156 triangles fewer than revision 5. It retains one mesh, one material, four 512px atlas maps, and inherited rig actions.

Offline checks passed for source geometry, triangle budget, atlas, weights, retained actions and exported GLB count. All marked attachment endpoint vertices lie inside their designated support in the saved rest mesh. Five inherited attack samples measured at most 0.52mm outside separation, within the 1mm review tolerance. Contacts use intersecting surfaces rather than welded topology. These checks do not establish all-animation deformation quality or native runtime performance. This remains an uninstalled art study.

Editable source: `assets/models/studies/underworld-boss-first-bloom-frills-r6.blend`. Authoring starts from the prior attachment repair and preserves that revision. See `frill-anatomy.json`, `attachment-verification.json`, `attachment-pose-verification.json`, `verification.json`, and `provenance.json` for saved evidence.
