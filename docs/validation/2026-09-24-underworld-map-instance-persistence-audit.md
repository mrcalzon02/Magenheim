# Underworld map instance persistence audit — 2026-09-24

## Scope

This bounded P0.-1 cycle continued the persistence audit after Nowhere King progression was moved under the admitted `UnderworldWorldIdentity`. The target was the remaining player-owned exploration/map state because the Underworld is a derived instanced world-space and no persistent payload may silently use only the parent Surface world as its authority boundary.

## Confirmed architecture

`UnderworldMapTabRuntime` is correctly a thin binding layer around Valheim's single `Minimap`. Surface and Underworld retain separate native Minimap payload/texture sets while exploration, fog, pins, shared-map behavior, zoom, input and serialization remain Valheim-owned. This is the required architecture: there is no second Magenheim map engine.

The runtime also correctly prevents a bound Underworld payload from being written into the ordinary Surface `PlayerProfile` map slot. `BeginSurfaceProfileSave` captures the active layer and temporarily restores the Surface binding before vanilla profile serialization; during asynchronous Underworld map generation it fails closed and defers that Surface write rather than risk cross-contamination.

## Open defect found

The Underworld payload's custom-data key is currently:

`magenheim.map.underworld.v1.<worldUid>`

`LoadUnderworldPayloadFromPlayerIfAvailable` and `PersistUnderworldMapToPlayer` both derive that key exclusively from `ZNet.GetWorldUID()`. The admitted derived instance identity (`ParentWorldId`, `DerivedWorldId`, `DerivedSeedFingerprint`) is not represented.

That is inconsistent with the corrected instance contract and with the already-repaired Deepstone, Deep Boon and Nowhere King persistence boundaries. A parent world whose derived Underworld identity changes can therefore inherit an exploration/fog/pin payload belonging to the prior derived instance. The payload is player-owned, but it is still instance-specific state and must not be admitted by parent-world identity alone.

## Required repair

Replace the live key with an instance-scoped key containing the admitted `DerivedSeedFingerprint` in addition to the parent `worldUid`. Loading and saving must fail closed when no `UnderworldWorldIdentity` is admitted.

Preserve existing saves through a one-way migration of the historical `magenheim.map.underworld.v1.<worldUid>` value into the new instance-scoped key. Migration is legitimate only while the current derived instance is admitted. After a successful copy, retire the legacy key so it cannot later become authority for another derived instance. Do not invent a Magenheim exploration codec; the stored value remains Valheim's native Minimap payload.

## Acceptance boundary

Static inspection is sufficient to identify the authority defect, not to claim runtime acceptance. After the key repair, the disposable-world matrix must verify: reveal Underworld terrain/pins; save; relog below; confirm the same derived instance restores the payload; confirm Surface map state remains unchanged; and confirm a deliberately different derived identity cannot consume the prior Underworld payload.

## Next dependency-valid slice

Patch `UnderworldMapTabRuntime` so map persistence is keyed by parent world plus admitted derived-instance fingerprint with one-way legacy migration, then continue the P0.-1 persistence audit for any remaining Magenheim-owned state that is still keyed only to the parent world. Do not return to cosmetic biome work until this authority boundary is closed.