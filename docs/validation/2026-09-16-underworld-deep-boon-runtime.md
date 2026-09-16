# Underworld Deep Boon Runtime Reconciliation Slice — 2026-09-16

## Implemented

The durable per-player Deep Boon selection now projects into a single server-owned runtime authority rather than stopping at persistence. `DeepBoonRuntime` reloads the player's durable selection, revalidates it through Core against current Conclave progression, and admits at most one canonical Magenheim Deep Boon identity per player.

The runtime projection is deliberately independent of vanilla Forsaken powers. It does not inspect, replace, clear, or mutate vanilla power state. Downstream boon behavior patches receive their authority through `DeepBoonRuntime.IsActive` / `GetActive`, preventing each boon implementation from inventing its own selection or persistence rules.

`UnderworldWorldSessionLifecycle` now configures this authority, reconstructs connected server players on world admission/reconnect and periodically reconciles durable selection so a successful selection switch or clear removes stale runtime identity without requiring a second state system. World unload or physical-session change clears the runtime projection before Underworld persistence services reset.

Invalid persisted selection cannot become a runtime boon: reconstruction still passes through `UnderworldDeepstoneRuntimeAuthority.ReconstructDeepBoonSelection`, and failure removes the player's runtime projection.

## Validation boundary

This cycle verified the committed source and integration on authoritative `main`. It does not claim local .NET compilation, Valheim startup, multiplayer execution, or status-effect behavior because those execution surfaces are not available through the repository connector.

## Next actionable slice

Implement the first actual Deep Boon behavior, Spore Communion, against `DeepBoonRuntime.IsActive` rather than adding another selection framework. Keep its combat/environment behavior server-authoritative and data-driven where practical, then use that implementation to establish the reusable boon-effect patch pattern for the remaining five boons.
