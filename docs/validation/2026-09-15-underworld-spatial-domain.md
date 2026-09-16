# Underworld reserved spatial domain — 2026-09-15

## Intent

Advance the next dependency-valid Underworld framework slice after the logical world-context decision: give the logical layer a deterministic, gameplay-authoritative physical host domain inside the active Valheim world and make runtime placement consume it instead of treating derived-world coordinates as raw Valheim coordinates.

Starting authoritative `main`: `27b4c5d67fda59a40790acfb0a9d5d61155b7066`.

## Implemented

`src/Magenheim.Core/Underworld/UnderworldSpatialDomain.cs` now owns the first spatial-domain schema.

The default domain is deliberately separated vertically from ordinary surface terrain:

- playable logical radius: 8000 m;
- host base Y: 8192 m;
- logical vertical range: -256 m through +1792 m;
- resulting reserved host band: 7936 m through 9984 m;
- mapping algorithm: `seed32-quarter-turn-v1`.

Underworld X/Z remains radius-preserving, but is rotated by a deterministic 0/90/180/270 degree quarter-turn derived from `DerivedSeed32`. Heading rotates with the coordinates. Y is translated into the reserved host band. The inverse mapper reconstructs the logical derived-world anchor. Surface anchors remain ordinary parent-world coordinates.

The mapper fails closed on wrong logical world identity, non-finite values, positions outside the reserved radius/vertical range, unknown mapping algorithms, unsupported schemas, or a definition whose fields no longer match its fingerprint.

## Authority integration

The spatial domain has its own deterministic SHA-256 fingerprint. `UnderworldAuthoritySnapshot` now contains content, Rootforged architecture and the spatial domain under composite schema 2. Spatial-domain changes therefore alter canonical Underworld gameplay authority instead of silently changing where persistent Underworld state exists.

## Runtime integration

`ValheimUnderworldTransitionPlacementHost` now maps logical anchors through the authoritative spatial domain before `Character.TeleportTo` and repeats the same mapping during observed-placement verification.

To make seed-derived mapping possible without hidden mutable state, player placement now receives `UnderworldWorldIdentity` explicitly through `IUnderworldTransitionHost` and `IUnderworldTransitionPlacementHost`. `UnderworldWorldTransitionManager` passes identity on both normal commit placement and recovery placement.

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

The suite is wired into the existing `DefinitionAuthorityTests` harness. `UnderworldAuthorityCompositionTests` now verifies that spatial changes alter composite Underworld authority.

## Validation boundary

This connector cycle can establish source presence, cross-file consistency and remote Git state. It does not provide the normal Valheim/.NET runtime profile, so compilation, deterministic test execution and live placement are not claimed here.

The 7936-9984 m band is a source-level reservation pending runtime proof. U2 still requires a disposable-world test demonstrating that Valheim physics, zone loading, networking, map behavior and existing dungeon/interior content do not collide with this band.

## Next actionable slice

Wire one `ValheimLogicalUnderworldWorldContextController`, one `UnderworldSpatialDomainDefinition`, the atomic state store, placement adapter, transition manager and recovery runtime into plugin/world lifecycle as one owned service graph. Then add the first server-authoritative entry/return trigger boundary and execute the disposable Surface -> Underworld -> Surface / forced-interruption recovery gate before beginning U4 terrain production.
