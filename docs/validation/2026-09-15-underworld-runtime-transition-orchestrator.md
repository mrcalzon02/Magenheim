# 2026-09-15 — Underworld runtime transition orchestrator

Status: **runtime orchestration source landed; concrete Valheim world-context binding and live U2 acceptance remain open**

## Intent

Advance U2 at the framework boundary instead of beginning biome minutia. The previous slice established the pure Core transition transaction. This slice adds the thinnest Runtime orchestration layer that consumes that authority without inventing a second transition state machine.

## Reconciled baseline

The run reconciled `main` after concurrent rendering repairs. The transition framework commit remained in ancestry and later Crystal Sentinel / Enchanting Dais winding repairs were preserved. No branch was created and no unrelated source was rewritten.

## Implemented

`src/Magenheim.Runtime/UnderworldWorldTransitionManager.cs` now provides one orchestration boundary around `UnderworldTransitionRules`.

For an already-authorized Prepared transition it:

1. validates the persisted Core state and operation/fingerprint identity;
2. persists the recoverable source state before attempting target initialization;
3. asks the host binding to initialize the target world context;
4. advances Core to TargetReady only after initialization returns successfully;
5. persists TargetReady before player movement;
6. places the player through the host binding;
7. requires observed target placement before Core commit;
8. persists the committed stable state;
9. converts failures into RecoveryRequired state and persists that state before attempting source recovery.

Interrupted persisted Prepared/TargetReady transitions recover to their deterministic source rather than guessing forward. Authority drift during an incomplete transition also recovers using the fingerprint captured by the original transaction.

`IUnderworldTransitionHost` is intentionally narrow: persistence, target-context initialization, player placement, and observed-placement verification. It is the only game-version-specific seam introduced by this slice. The manager does not call Unity/Valheim world-loading APIs directly and does not create a duplicate persistence engine.

## Failure invariant

If recovery placement itself cannot be observed, `RecoverToSource` is not called. The already-persisted RecoveryRequired snapshot therefore remains authoritative and retryable rather than falsely recording a stable layer.

## Validation boundary

This repository connector does not expose the local Valheim/.NET runtime used for normal build and live-world validation. Source has therefore been committed without claiming compilation, world switching, save/reload, reconnect, host/client, dedicated-server, or U2 admission.

## Next actionable slice

Bind `IUnderworldTransitionHost` to actual Valheim runtime facilities in the smallest possible adapter. First determine the supported world-context switching boundary in the installed/current Valheim API rather than reflecting guessed member names. The concrete adapter must persist transition snapshots durably, derive/load the `UnderworldWorldIdentity` context, place the player, and independently observe the resulting world/layer/position before acknowledging success.

Then compile and run the disposable U2 matrix: enter, return, failed target initialization, failed placement, save/reload while Prepared, save/reload while TargetReady, reconnect, authority drift, and host/client behavior. Do not begin biome production until that proof establishes a reversible derived-world transition.