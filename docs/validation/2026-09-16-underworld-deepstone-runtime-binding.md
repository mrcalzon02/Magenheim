# Underworld Deepstone runtime binding

Date: 2026-09-16
Branch: `main`
Scope: source/static implementation validation

## Reconciliation

The Conclave is now admitted through the reserved Underworld spatial domain and Core already owns `UnderworldDeepstoneProgression.PlanMountTrophy`. The next missing framework boundary was physical runtime identity and durable synchronized state: the six visible stones had names, but no component bound them to canonical Deepstone ids and no runtime boundary existed for mounted/boon persistence.

## Implemented

Added `UnderworldDeepstoneRuntime` as the presentation/persistence adapter for a physical Conclave stone. It:

- binds exactly one canonical `magenheim.underworld.deepstone.*` identity to the object;
- rejects rebinding to a different identity;
- reconstructs mounted-trophy and unlocked-boon facts from synchronized Valheim global keys;
- exposes a server-only `PersistAuthorizedActivation` boundary that requires the atomic mounted+boon result and refuses partial/client mutation;
- exposes hover presentation from persisted state without recreating Core progression decisions.

`UnderworldWorldCenterRegistrar` now constructs the six physical stones from explicit object-name/canonical-id bindings and attaches `UnderworldDeepstoneRuntime` to each stone. The mapping is Bloom, Tide, Cinder, Rime, Fracture and Decay.

## Architecture preservation

This slice does not decide whether a trophy is valid, whether prerequisites are met, or which boon unlocks. Those decisions remain exclusively in `Magenheim.Core.Underworld.UnderworldDeepstoneProgression`. The runtime component is intentionally incapable of authorizing a trophy by itself.

Persistence uses Valheim global keys because the Conclave progression is world-global, durable and synchronized rather than per-player or per-item state. Mutation is server-gated.

## Validation boundary

GitHub source state is verified. Compilation, plugin startup, multiplayer replication, actual global-key save/load, and live Conclave hover behavior are not claimed in this connector-only cycle.

## Next actionable slice

Complete the interaction transaction bridge: supply the authoritative Underworld definition set and gameplay authority session to the Conclave runtime, add the session-bound RPC request path for remote clients, resolve the requesting player and exact trophy server-side, call `PlanMountTrophy`, consume exactly the authorized quantity, then call `PersistAuthorizedActivation`. The client must never choose the result or directly write progression state.
