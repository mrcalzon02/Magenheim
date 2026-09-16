# Underworld Deepstone interaction authority — 2026-09-16

## Reconciliation

The existing framework already had canonical six-stone definitions, pure Core trophy/prerequisite/boon planning, reserved-domain physical Deepstones, durable world-key state, and a runtime bridge that reconstructs the full Conclave state. The missing dependency-valid framework slice was the actual player interaction and multiplayer transaction path.

## Implemented

`UnderworldDeepstoneRuntime` is now a Valheim `Interactable`. Physical stone use delegates to `UnderworldDeepstoneInteractionRpc` rather than accepting a client-selected trophy.

The RPC is registered beside the existing session-bound workshop transport. A remote client sends only its current authority generation and canonical Deepstone id. The server verifies the peer's active Magenheim mutation session, resolves the peer's server-side Player object, verifies that the requested physical stone is within five metres, resolves the canonical boss/trophy from validated Underworld definitions, derives trophy count from the resolved inventory, and calls `UnderworldDeepstoneRuntimeAuthority.PlanMount`.

Only a `Ready` Core transaction plan can consume inventory. Consumption uses the plan's exact `TrophyConsumeCount`, after which the returned atomic trophy-mounted/boon-unlocked state is persisted through the existing server-only Deepstone persistence boundary. Rejected, stale, out-of-range, wrong-prerequisite, already-mounted, and missing-trophy requests do not receive an authorized mutation path. The response sent to clients contains only the bounded applied flag and diagnostic.

Host/server interaction uses the same server transaction method rather than a separate progression implementation.

## Validation boundary

Repository/API reconciliation was performed against the current `main` implementation and the existing `CustomRPC`, peer-resolution, inventory-removal, definition-authority and Deepstone transaction contracts. No local Valheim runtime is available through this connector execution, so compilation and live multiplayer acceptance are not claimed here.

## Next actionable slice

Close the remaining Deepstone framework reliability gap before expanding minutiae: add replay/idempotency protection for remote Deepstone requests and a recoverable inventory/persistence commit boundary so a persistence exception cannot strand a consumed trophy. Then validate the six-stone sequence end-to-end in the installed Valheim runtime before moving into Deep Boon gameplay effects.
