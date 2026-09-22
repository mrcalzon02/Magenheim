# Rootforged starter set — 0.0.95

Seventeen player-buildable pieces under **Hammer > Rootforged**, consuming the existing validated Underworld architecture catalog. This is the separate placeables pass explicitly requested after the natural-scenery work.

| Pieces | Interim materials per piece | Station |
|---|---|---|
| Worldroot beams, 2 / 4 / 8 m | 2 / 4 / 8 core wood | Workbench |
| Worldroot Y brace, 4 m wide x 4 m high | 6 core wood | Workbench |
| Worldroot T brace, 4 m wide x 2 m high | 4 core wood | Workbench |
| Worldroot forked column, 4 m wide x 8 m high | 12 core wood | Workbench |
| Worldroot pillars, 2 / 4 / 8 m | 2 / 4 / 8 core wood | Workbench |
| Iron-banded Worldroot beams, 4 / 8 m | 4 / 8 core wood + 2 / 4 iron | Stonecutter |
| Worldroot floor, 4 x 4 x 1 m | 8 core wood | Workbench |
| Understone stairs, 2 m wide x 4 m run x 2 m rise | 12 stone | Stonecutter |
| Rootforged arch ribs, 4 / 8 m | 6 / 12 core wood + 4 / 8 iron | Stonecutter |
| Understone foundation, 2 x 2 x 1 m | 6 stone | Stonecutter |
| Great column plinth, 4 x 4 x 2 m | 16 stone | Stonecutter |

Worldroot Timber maps explicitly to native core wood (`RoundLog`), Understone to `Stone`, and iron to `Iron`. These are interim construction materials: no new harvesting sources or fictional runtime resource items are claimed. Catalog identities and quantities remain authoritative. Native station proximity, resource consumption and refunds apply.

The seventeen editable Blender models carry braided trunk masses, finer raised roots, dark iron collars on reinforced pieces and stepped stone seating. UVs follow the roots longitudinally. Packed bark, heartwood, stone and iron textures export through the existing importer. Each 256px Hammer icon is rendered from its corresponding source model.

Construction uses native Piece, WearNTear, ZNetView and Jotunn CustomPiece. Normal root pieces inherit the core-wood pole chassis with HardWood support; reinforced pieces use the iron-beam chassis and Iron support; bases use the stone-floor chassis and Stone support. No special support or saving subsystem. Long unsupported spans can still collapse under normal Valheim support rules.

Dedicated mesh collision stays active when wear visuals switch. Snap points are direct tagged children, matching the installed game's Piece.GetSnapPoints behavior. Bases have top/bottom center, edge and corner points; pillars have base/top plus cardinal attachments every two meters; beams have end/intermediate underside, centerline and top points; arches have feet, crown and shoulder points.

Verification includes source/export/icon coverage, declared dimensions, ground contact, collision presence, open arch passages, physical collars, neutral material tint and winding. The contact sheet is an offline source-art review. Hammer registration, live snapping, support/collapse, weathering, refund behavior and multiplayer require in-game acceptance and are not yet certified.

Source art: `assets/models/source/rootforged-*.blend`. Review: `artifacts/review/rootforged/rootforged-contact-sheet.png`.

The Y brace spreads a central trunk into two upper branches; the shallow T brace seats a crossbeam; the tall forked column provides paired elevated seats for halls. Each has three braided strands per limb and raised root veins, separate collision, lower clearance, and base/junction/branch-tip native snap points. These use the existing HardWood support rules.

Floors have eight split-root boards over three root joists, with level walking surfaces and top/bottom grid snaps. Stairs have eight 0.25m rises, climb toward local +Z, and provide bottom, midpoint and top snaps. Both use native mesh collision and existing wear states. The 8m reinforced beam uses three physical iron collars and the native iron support donor.
