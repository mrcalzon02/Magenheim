# Underworld map instance persistence repair — 2026-09-24

## Backlog defect closed

The Underworld Minimap payload was historically persisted only as `magenheim.map.underworld.v1.<worldUid>`. That parent-world-only key allowed exploration, fog and pins from one derived Underworld identity to become durable authority for a different derived Underworld beneath the same Valheim world.

## Material repair

`UnderworldMapPersistenceScope` now places the durable native Minimap payload under `magenheim.map.underworld.v2.<worldUid>.<DerivedSeedFingerprint>`. The existing `UnderworldMapTabRuntime` remains the sole owner of the Valheim Minimap payload and its vanilla codec; the scope adapter does not introduce a second map implementation, exploration bitmap, fog codec or pin codec.

The historical v1 key is retained only as a session compatibility bridge because `UnderworldMapTabRuntime` currently reads and writes that slot internally. Once an admitted derived identity exists, the adapter hydrates that bridge from the matching v2 key. If no v2 value exists, a historical v1 payload is migrated one-way into the currently admitted identity. Player save copies the current bridge payload to v2 and removes v1 before custom data is serialized, preventing the legacy parent-world-only key from remaining durable authority.

Instance admission can occur after vanilla `Minimap.LoadMapData`, so preparation is retried from the normal Minimap update path until one admitted identity has been prepared. A scope guard makes that retry one-shot for the admitted identity and prevents it from overwriting live session exploration with an older persisted payload.

`UnderworldTerrainRuntime.TryGetAdmittedIdentity` exposes the already-authoritative lifecycle identity to this persistence adapter. No player-population state or alternate layer identity is introduced.

## Verification boundary

Static source reconciliation confirms that the durable key now includes both parent world UID and `DerivedSeedFingerprint`, migration requires an admitted instance identity, and the legacy key is retired at player-save serialization. No live Valheim client/dedicated-server save/reload test was available in this connector environment, so runtime acceptance remains open.

## Next actionable slice

Continue the P0.-1 persistence audit for remaining Magenheim-owned Underworld world/player state keyed only by parent world or global names. Then reconcile the stale P0.-1 backlog checklist against implemented native chunk, residency, transition, map and progression authorities before spending cycles on cosmetic biome minutia.
