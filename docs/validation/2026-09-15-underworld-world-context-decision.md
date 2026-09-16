# Underworld world-context architecture decision — 2026-09-15

## Decision

Magenheim will not hot-swap Valheim's active `World`, `WorldGenerator`, `ZoneSystem`, or `ZNet` context while players remain connected. The Underworld derived identity remains authoritative as a deterministic logical namespace, but the playable layer is hosted inside the already-active parent Valheim world.

This replaces the earlier assumption that the derived identity necessarily required a second live Valheim world/save. Public/current Valheim operational behavior continues to model one active world per running server instance; changing the active world is a restart boundary. Existing mod practice also changes generation data inside the active world rather than exchanging the live network world beneath connected peers.

## Implementation consequence

`ValheimLogicalUnderworldWorldContextController` now implements the final `IUnderworldWorldContextController` framework seam without mutating engine-owned world singletons. It binds once to one deterministic parent/derived identity pair, tracks the authoritative logical layer requested by the transition transaction, fails closed without an active `ZNet` session, rejects cross-world-pair reuse, and can be reset on world unload.

The existing transition manager, atomic state store, recovery runtime and placement observer remain unchanged. Their derived `WorldId` values are logical Magenheim context identifiers. Runtime generation must place Underworld terrain/interiors into an isolated reserved coordinate/instance domain inside the parent world and must derive all layout decisions from `DerivedSeedFingerprint`/`DerivedSeed32` so the layer remains deterministic and does not collide with ordinary surface generation.

## Why this is the smallest safe repair

Attempting to mutate the active engine world would require coordinated teardown/reinitialization of network persistence, ZDO ownership, world generation, zone state, peers and saves. There is no repository-proven supported hot-swap contract for that operation. Treating the derived identity as a logical layer preserves every Core transition/persistence invariant already implemented while removing the unsafe engine-lifecycle dependency.

## Acceptance still required

This decision completes the world-context framework seam at source level, not U2 runtime acceptance. The next slice must define the reserved Underworld spatial domain and deterministic coordinate mapping, then wire the logical controller into plugin lifecycle/reconnect recovery. Disposable-world validation must still prove Surface -> Underworld -> Surface, interruption recovery, reload/reconnect while below, multiplayer isolation and no surface-generation collision before U2 can close.
