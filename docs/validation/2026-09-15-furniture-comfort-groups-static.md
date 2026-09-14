# Crystal Furniture Comfort-Group Static Validation — 2026-09-15

## Scope

This bounded cycle reconciled the live furniture implementation after explicit comfort values and Crystal Shaping-gated costs had already been added. The remaining defect was that Magenheim pieces still inherited `Piece.m_comfortGroup` from whichever compatible vanilla prefab happened to be selected as the clone source.

That made comfort semantics dependent on source-prefab fallback order rather than Magenheim design authority. In the worst case, visually equivalent Magenheim furnishings could contribute comfort independently when they were intended to compete within the same vanilla comfort category.

## Repair

`FurnitureRegistrar.cs` now owns both comfort amount and comfort group explicitly.

- Geode Table, Mineral Shelf, Lapidary Cabinet, Geologist's Desk, Geode Pedestal, and Crystal Screen use the Valheim `Table` comfort group.
- Geode Chair, Crystal Bench, and Crystal Throne use the `Chair` comfort group.
- Crystal-set Bed uses the `Bed` comfort group.
- Group names are resolved against the runtime `Piece.ComfortGroup` enum and registration fails closed if an expected group is unavailable; no numeric enum cast or clone-source inheritance is accepted as authority.

This preserves the intended balance: crystal furniture is expensive primarily because it consumes shaped Magenheim Earth crystals, while its comfort remains in the same general class as established mid/late-game furniture instead of becoming an additive ten-piece comfort exploit. Within each category, the strongest nearby Magenheim piece should supersede weaker members under normal Valheim comfort-group rules.

## Verification performed

Static source read-back confirmed:

1. every one of the ten definitions supplies an explicit comfort group;
2. no furniture definition depends on inherited source-prefab comfort grouping;
3. the shaped-crystal recipes remain unchanged from the prior balance pass;
4. `ResolveComfortGroup` validates the named runtime enum member and throws on absence rather than silently selecting another group.

No local Valheim/Jotunn build or runtime was available in this connector execution. This record therefore admits source-level implementation only, not compile or in-game acceptance.

## Next step

Rebuild the current package in the normal Valheim development environment, place representative members of each group together, and verify the Rested comfort calculation selects the strongest Table, Chair, and Bed contribution rather than stacking same-group pieces. Also verify Hammer recipe/refund behavior for the shaped-crystal costs. Any discrepancy should be repaired in `FurnitureRegistrar.cs` before expanding furniture content.
