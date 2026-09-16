# Underworld reserved spatial domain — 2026-09-15

## Intent

Advance the next dependency-valid Underworld framework slice after the logical world-context decision: give the logical layer a deterministic, gameplay-authoritative physical host domain inside the active Valheim world and make runtime placement consume it instead of treating derived-world coordinates as raw Valheim coordinates.

Initial observed `main`: `27b4c5d67fda59a40790acfb0a9d5d61155b7066`. Before mutation, `main` concurrently advanced to `875694a2ce2d21b88a3ba0a88620622f52e170ca` (`Condense authority sessions to monotonic generations`). That unrelated authority-session repair was preserved and became the actual parent of the spatial-domain commit rather than being overwritten.

## Implemented

`src/Magenheim.Core/Underworld/UnderworldSpatialDomain.cs` owns the first spatial-domain schema.

The default domain is deliberately separated vertically from ordinary surface terrain:

- playable logical radius: 8000 m;
- host base Y: 8192 m;
- logical vertical range: -256 m through +1792 m;
- resulting reserved host band: 7936 m through 9984 m;
- mapping algorithm: `seed32-quarter-turn-v1`.

Underworld X/Z remains radius-preserving, but is rotated by a deterministic 0/90/180/270 degree quarter-turn derived from `DerivedSeed32`. Heading rotates with the coordinates. Y is translated into the reserved host band. The inverse mapper reconstructs the logical derived-world anchor. Surface anchors remain ordinary parent-world coordinates.

The mapper fails closed on wrong logical world identity, non-finite values, positions outside the reserved radius/vertical range, unknown mapping algorithms, unsupported schemas, or a definition whose fields no longer match its fingerprint.

## Authority integration

The spatial domain has its own deterministic SHA-256 fingerprint. `UnderworldAuthoritySnapshot` contains content, Rootforged architecture and the spatial domain under composite schema 2.

A follow-up authority repair also feeds the spatial-domain fingerprint into `GameplayAuthorityFingerprint` whenever the loaded Magenheim definitions contain the Underworld. `DefinitionAuthoritySynchronizer` therefore uses the existing gameplay-authority handshake to reject peers that disagree about the active Underworld spatial mapping. No second network handshake was added.

The spatial policy is still code-authoritative through `UnderworldSpatialDomain.CreateDefault()` rather than a separate schema-6 JSON field. That distinction is intentional at this framework stage: the multiplayer authority boundary is closed now without prematurely creating a second data migration. A later definition-schema change can expose spatial tuning once the reserved band is runtime-proven.

## Runtime integration

`ValheimUnderworldTransitionPlacementHost` maps logical anchors through the authoritative spatial domain before `Character.TeleportTo` and repeats the same mapping during observed-placement verification.

To make seed-derived mapping possible without hidden mutable state, player placement receives `UnderworldWorldIdentity` explicitly through `IUnderworldTransitionHost` and `IUnderworldTransitionPlacementHost`. `UnderworldWorldTransitionManager` passes identity on both normal commit placement and recovery placement.

This does not create a second transition engine or second worldgen stack. Core owns the mapping rules; the Runtime adapter only converts the resulting host anchor to Unity coordinates.

## Deterministic coverage added

`UnderworldSpatialDomainTests` covers:

- default schema/fingerprint;
- reserved high-altitude host band;
- deterministic logical -> host mapping;
- exact inverse round trip within floating-point tolerance;
- seed-derived quarter-turn variation;
- surface pass-through behavior;
- reserved-radius rejection;
- reserved-vertical-range rejection;
- wrong-world rejection;
- fingerprint drift after spatial policy changes;
- rejection of forged field/fingerprint combinations.

The suite is wired into the existing `DefinitionAuthorityTests` harness. `UnderworldAuthorityCompositionTests` verifies that spatial changes alter composite Underworld authority. `SocketCompatibilityAuthorityTests` now also verifies that presence/change/malformed spatial fingerprints affect or reject the synchronized gameplay-authority fingerprint.

## Validation boundary

This connector cycle establishes source presence, cross-file consistency and remote Git state. It does not provide the normal Valheim/.NET runtime profile, so compilation, deterministic test execution and live placement are not claimed here.

The 7936-9984 m band is a source-level reservation pending runtime proof. U2 still requires a disposable-world test demonstrating that Valheim physics, zone loading, networking, map behavior and existing dungeon/interior content do not collide with this band.

## Next actionable slice

Wire one `ValheimLogicalUnderworldWorldContextController`, one `UnderworldSpatialDomainDefinition`, the atomic state store, placement adapter, transition manager and recovery runtime into plugin/world lifecycle as one owned service graph. Then add the first server-authoritative entry/return trigger boundary and execute the disposable Surface -> Underworld -> Surface / forced-interruption recovery gate before beginning U4 terrain production.
