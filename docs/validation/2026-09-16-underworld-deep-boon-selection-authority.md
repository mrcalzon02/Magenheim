# Underworld Deep Boon selection authority

## Reconciliation

Authoritative `main` already owns the complete six-stone world unlock graph, replay admission, compensated trophy/persistence transactions, and physical-world transition recovery. The design authority separately requires Deep Boons to remain independent from vanilla Forsaken powers, with one per-player selected boon at a time and world-shared trophy unlocks.

The highest-priority dependency-valid slice after closing Deepstone transaction plumbing was therefore the missing Core selection boundary. Beginning individual boon effects before this boundary would force Runtime code to invent selection, stacking, and reconnect semantics.

## Implemented

`UnderworldDeepBoonSelection` now owns per-player Deep Boon selection semantics in `Magenheim.Core`.

It admits only boon identities present in validated Underworld definitions and unlocked by the authoritative world Conclave state. A player can have zero or one selected Deep Boon; selecting another unlocked boon replaces the prior selection rather than stacking it. Selection is independent of vanilla Forsaken-power state by construction because the Core contract contains no vanilla power field or adapter.

Reconnect reconstruction fails closed when a persisted boon identity is unknown or no longer unlocked in the current authoritative world state. World-state input must contain the complete canonical Conclave and cannot contain partial trophy/boon activation.

Deterministic coverage was added for locked and unknown rejection, first selection, idempotent reselection, replacement rather than stacking, reconnect reconstruction, clearing, and rejection of persisted selection whose world unlock is absent. The suite is registered in `DefinitionAuthorityTests`.

## Validation boundary

This connector cycle changed authoritative source and deterministic test registration on `main`. It did not execute the .NET test suite, compile against the installed Valheim API, or perform live multiplayer acceptance, so those outcomes are not claimed.

## Next actionable slice

Persist the selected Deep Boon as per-player Underworld state and expose a server-authoritative selection interaction/RPC that reconstructs world unlocks before accepting a selection. Once that boundary is durable across reconnect, implement the generic `DeepBoonRuntime` status-effect/application framework and then the six data-driven boon behaviors, beginning with Spore Communion.