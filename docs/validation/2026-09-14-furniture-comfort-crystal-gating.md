# Furniture comfort and crystal gating — static source validation

Date: 2026-09-14

## Intent

Magenheim geology/crystal furniture should provide comfort in roughly the same practical band as stone/iron-era furnishings, while its dominant progression cost is shaped crystal volume rather than ordinary wood, stone, or metal.

## Source correction

`src/Magenheim.Runtime/FurnitureRegistrar.cs` now owns explicit comfort values for all ten furniture definitions instead of inheriting an incidental value from whichever compatible vanilla clone source resolves at runtime.

Current comfort values:

- Geode Table: 1
- Geode Chair: 2
- Crystal Bench: 2
- Crystal-set Bed: 2
- Mineral Shelf: 1
- Lapidary Cabinet: 1
- Geologist's Desk: 1
- Geode Pedestal: 1
- Crystal Screen: 1
- Crystal Throne: 3

The registrar now also requires `Magenheim_Crystal_Earth_Simple`, tying furniture construction to the actual Magenheim Crystal Shaping progression instead of vanilla `Crystal` inventory.

Current shaped-crystal costs:

- Geode Table: 8
- Geode Chair: 5
- Crystal Bench: 8
- Crystal-set Bed: 10
- Mineral Shelf: 10
- Lapidary Cabinet: 12
- Geologist's Desk: 12
- Geode Pedestal: 8
- Crystal Screen: 14
- Crystal Throne: 20

Wood, stone, bronze, iron, and hide remain supporting costs. The intended progression bottleneck is the quantity of shaped crystals.

## Registration dependency

`EarthContentRegistrar` subscribes before `FurnitureRegistrar` during plugin startup, so the elemental crystal item family is registered before the furniture callback consumes the shaped-crystal prefab identities. The furniture registrar additionally fails closed if the required crystal identity is absent.

## Static verification

Read-back from live `main` confirms the explicit `m_comfort` assignment, shaped-crystal requirement helper, and the ten revised recipes are present in `FurnitureRegistrar.cs`.

This is static-source admission only. No claim is made that the current expanded source has compiled or passed a live Valheim placement test in this connector-only cycle.

## Next step

Build/install the current source in the normal Valheim development profile. Verify every furniture recipe resolves the shaped Earth crystal requirement, all ten pieces contribute the intended comfort values in-game, the comfort-group behavior does not produce unintended stacking, and dismantling refunds the revised recipe correctly. Repair any comfort-group or registration-order defect at the authoritative registrar before further furniture expansion.
