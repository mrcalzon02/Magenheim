# Underworld player-transition drift discrepancy — 2026-09-20

## Status

**CONFIRMED ARCHITECTURAL DRIFT. DEVELOPMENT ON THIS ROUTE IS CUT OFF.**

The Underworld is a separate Magenheim-owned instance world-space. During implementation, the runtime accumulated a second concern that was not required by that architecture: Magenheim-owned durable player-layer and transition-recovery state.

## What drifted

The following mechanisms grew beyond the required Deep Gate transport boundary:

- durable per-player Surface/Underworld layer state;
- Prepared / TargetReady / RecoveryRequired player transition phases;
- server enumeration of connected players to reconstruct transition state;
- per-player recovery reconciliation and retry bookkeeping;
- terrain residency decisions derived from persisted player transition records;
- global/local layer resolution derived from the transition/context controller;
- placement acknowledgement/retry machinery developed as a general player-state subsystem.

This work confused **moving an existing Valheim player through the Deep Gate** with **owning the player's world/layer lifecycle**.

## Correct authority

Magenheim owns the Underworld instance: identity, native terrain/chunks, biome/environment, structures/ecology, exploration/map state, persistence namespace, and instance multiplayer synchronization.

Valheim continues to own players, character persistence, connection/session identity, inventory, skills, and ordinary network lifecycle.

The Deep Gate needs only a thin entry/return transport adapter. It may keep minimal ephemeral safety information while a transfer is actually in flight, but it must not create a durable Magenheim player-layer database, reconstruct instance state from players, track population, or make terrain/chunk residency depend on transition records.

## Cut line

Effective immediately:

1. No new features, fixes, retries, persistence schemas, multiplayer reconciliation, or lifecycle branches may be added to the superseded player-transition subsystem.
2. Runtime consumers that use transition persistence to decide instance admission, chunk residency, ecology, map state, or world truth must be removed.
3. Existing transition/recovery source is legacy quarantine until the thin Deep Gate adapter replaces it. It is not an architectural dependency for new Underworld work.
4. Reusable low-level placement safety may be retained only if the thin gate adapter actually needs it.
5. Underworld development resumes at the instance boundary: native chunk/world materialization, environment, persistence, structures/ecology, and multiplayer synchronization of the instance itself.

Historical validation records describing the superseded player-transition state machine remain historical evidence only and are not current implementation authority.
