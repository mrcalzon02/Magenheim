# Construction visibility and support repair — 0.0.169

The October 2 priority is reliable Worldroot/Rootforged placement and native crystal
support transfer. Bench changes are deferred. The last available game log boots
0.0.167; neither the installed 0.0.168 repairs nor this successor have live acceptance.

## Installed engine evidence

Inspected the installed `assembly_valheim.dll` WearNTear methods with Mono.Cecil,
and native wood_pole, wood_beam and stone_floor_2x2 prefabs with UnityPy.
Development evidence is in `dist/native-wear-inspection.txt`,
`dist/native-wear-visual-inspection.txt`, and `dist/construction-donor-evidence.txt`.
No native assets are shipped by the inspection tool.

- SetupColliders includes child colliders independently of active wear visuals.
  It excludes triggers and attached rigid bodies, builds oriented bounds for boxes,
  and expands each full bound size by 0.30 m: 0.15 m of search tolerance per side.
- UpdateSupport uses Physics.OverlapBoxNonAlloc on those bounds and the native
  piece/default/static/terrain layers. Neighbours must have `m_supports` enabled.
  Material and centre-of-mass rules determine the strength that transfers.
- FindSupportPoint uses ClosestPoint on primitive/convex collision. For nonconvex
  meshes it first raycasts down from the requesting centre of mass; a miss falls
  back to the midpoint between the pieces' centres of mass.
- All three inspected structural donors use the piece layer (10) and support
  neighbours. The issue is not a missing global collision overlap setting.

Actual exported Worldroot pillar-2m solid meshes begin at y=0.129 m and end at
y=1.873 m. End-to-end snapping at 2 m therefore leaves about 0.256 m of empty
collision space. Forked-column solid trunks begin around y=0.284 m; decorative
root flares reach the catalog base but do not own collision. These exceed the
engine's neighbour tolerance. The visual-envelope fit includes decoration, so
fitting the overall rendered dimensions did not guarantee solid contact planes.

## Repair

- Keep authored mesh collision and add native 0.30 m joint BoxColliders at the
  direct-child Rootforged snap positions. This supplies local solid contact and
  native ClosestPoint support without replacing arches/braces with filled boxes.
  Collision remains a sibling of the new/worn/weathered visual roots.
- Explicitly retain native structural neighbour support on Rootforged and crystal
  structures. Iron recipe pieces keep the native iron support material from 0.0.168.
- Crystal foundations replace the flattened donor snaps with 18 direct, tagged
  native snaps on y=0 and y=0.34 m for 2/4/8 m footprints. Stacking now has an
  explicit top surface, and lateral endpoints stay within the solid footprint.
- Remove inherited LodFadeInOut components from crystal geometry as already done
  for Rootforged. Rootforged new-state art is explicitly active and registration
  rejects an empty active visual hierarchy. The plain-pole donor, independent
  Rock_4 material template and obsolete-LOD removal from 0.0.168 are retained.

No custom support propagation, infinite stability, collapse suppression, or
per-frame visibility override is introduced. Visibility's live root cause remains
unconfirmed; registration/material checks do not substitute for viewing the game.

## Verification

`dist/construction-model-tests-169.log`: actual recessed-pillar geometry reproduces
the old gap; native joint geometry bridges stacked/perpendicular contact; all three
foundation footprints expose separate bottom/top snaps without donor tags; all
531 assets import twice with complete meshes/UVs and safe native static materials.

`dist/construction-closeout-169.log`: runtime compilation and installed package /
launcher verification. Runtime compiles with zero warnings/errors.

Live acceptance still required: place grounded Rootforged pillars, stack two 2 m
pillars, connect beams at right angles, build crystal columns and lateral spans
on each foundation, inspect native hammer support colours, and reload the world.
Check fresh and damaged Worldroot art and that arch openings remain traversable.
