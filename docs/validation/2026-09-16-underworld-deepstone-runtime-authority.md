# Underworld Deepstone runtime authority bridge

Date: 2026-09-16
Branch: `main`
Scope: source/static implementation boundary

## Reconciliation

The physical Conclave stones already carried canonical Deepstone ids and persisted mounted/boon facts, while `Magenheim.Core.Underworld.UnderworldDeepstoneProgression` already owned trophy, prerequisite and boon transaction planning. The missing framework boundary was a runtime authority capable of reconstructing the complete six-stone world state from persistence and feeding that state back into Core without duplicating progression rules.

Concurrent main work after the previous Underworld slice added Burrower emergence behavior. This slice preserves that work and develops directly on the current main tip.

## Implementation

Added `UnderworldDeepstoneRuntimeAuthority` as the single runtime bridge for Conclave progression. It is configured from the validated effective Magenheim Underworld definitions during plugin bootstrap, requires exactly six canonical stones, reconstructs all six `UnderworldDeepstoneState` values from durable world keys, rejects impossible persisted boon-without-trophy state, exposes canonical boss/trophy lookup, and delegates mount planning to `UnderworldDeepstoneProgression.PlanMountTrophy`.

`UnderworldDeepstoneRuntime` now exposes one coherent persistent-state read operation instead of requiring callers to independently reconstruct mounted and boon facts.

The plugin now fails closed if the effective validated definition set lacks Underworld authority and configures the runtime bridge from that same effective definition set used by the rest of Magenheim.

## Architectural effect

There is now one continuous authority path:

validated Magenheim definitions -> runtime Deepstone state reconstruction -> Core transaction planning -> server-only atomic persistence.

The remaining interaction adapter no longer needs to invent progression state or inspect definitions itself. It only needs to resolve the requesting player and authoritative inventory, identify the offered trophy, call `PlanMount`, consume exactly the returned count when `Ready`, and persist the returned state.

## Validation boundary

This connector cycle does not claim compilation, Valheim startup, multiplayer RPC behavior, inventory mutation, or in-game interaction. Those require the normal build/install/runtime closeout environment.

## Next actionable slice

Implement the server-authoritative Deepstone interaction/RPC transaction: resolve the requesting peer and player, derive the offered trophy from authoritative server inventory, call `UnderworldDeepstoneRuntimeAuthority.PlanMount`, consume exactly the authorized trophy count, persist the resulting atomic state, and return a bounded result to the client. No client-provided inventory count or progression fact may be trusted.
