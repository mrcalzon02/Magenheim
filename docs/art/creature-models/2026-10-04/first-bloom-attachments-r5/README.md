# First Bloom: floating attachment repair

[Before/after gallery](gallery.html) · [Saved contact audit](attachment-verification.json) ·
[Five-pose contact measurements](attachment-pose-verification.json)

The user identified an incorrect modeling standard: several pale tissue groups
started or ended in empty space. The prior topology and bounds checks missed
these spatial attachment errors. This repair establishes explicit support
contacts rather than relying on the appearance of a nearby cap or body surface.

## Repair and acceptance check

All **60 groups / 120 ends / 524 endpoint vertices** are checked: 32 curtain/body
frame groups, 22 chamber ribs and six crosslinks. Each start and end has a named
support, and every endpoint vertex is seated inside that support's closed surface.
Whole endpoint cross-sections are seated; checking only their center had allowed
wide rail corners to protrude above the cap as pale flecks.

Parents are repaired before dependent frames and crosslinks. Cap curtains seat
into their source cap and lower canopy, trunk or shoulder growth. Chest frames
seat into canopy and chamber/body supports. Limb tissue seats into its growth
and limb supports. Ribs seat into the core and chamber wall; crosslinks seat into
already rooted tissue or core supports. Intermediate sections retain their
existing bends where possible.

The attachment mesh component and endpoint markers persist in the Blender source.
The verifier reloads the combined saved mesh, reconstructs each support surface
and checks every endpoint vertex with a closed-surface inside test. This is not
a loose proximity test. Supports intersect the tissue; they are not topologically
welded. The retained mesh connectivity and triangle budget allow this correction
without adding density.

Bone weights at each seated endpoint interpolate the actual support triangle's
weights. Five inherited attack samples measure at most approximately **0.52 mm**
of outside separation, below the **1 mm** review tolerance. This is sampled
offline evidence, not proof across all native animations. Rest geometry requires
every endpoint vertex to be inside its support with no free ends.

The candidate remains **9,188 triangles, 1.48x Troll, one mesh and one material**.
Four owned maps remain 512px and are rebaked for the repaired shape. Geometry,
manifold edges, nonzero triangles, normalized weights, scale, portable export and
sampled bounds checks also pass. The current runtime creature path still expects
rigid parts, so this consolidated skin remains uninstalled and outside production
export. No runtime registration or release changed.

## Modeling standard going forward

Every structural tissue group must declare its root and terminal support. A
deliberately free fringe tip must be identified as such and rooted at its other
end; it must never be silently used as an excuse for detached frame geometry.
Inspect attachment close-ups and clay views, verify saved support contacts, then
check contacts in representative poses. Topology validity alone is insufficient.

## Reproduce

1. `tools/blender.ps1 refine-first-bloom-attachments`
2. `tools/blender.ps1 review-first-bloom-attachments`
3. `tools/blender.ps1 verify-first-bloom-attachment-poses`
4. `tools/blender.ps1 render-first-bloom-attachment-closeups`
5. `python tools/build-first-bloom-attachment-review.py`
