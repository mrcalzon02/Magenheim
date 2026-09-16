# Underworld Deepstone replay authority — 2026-09-17

## Changed

Deepstone remote mutation replay admission is now a deterministic `Magenheim.Core` authority instead of an untested collection embedded in the Valheim RPC adapter. `UnderworldDeepstoneRequestLedger` owns the bounded `(peer, authority-generation, request-id)` replay window. The runtime transport delegates admission to that authority before player resolution, inventory inspection, progression planning, trophy consumption, or persistent mutation.

The deterministic suite now covers invalid identity rejection, exact duplicate rejection, independent peer and authority-generation scopes, bounded eviction, and explicit reset behavior. This closes the replay portion of the runtime interaction boundary without duplicating progression rules in Runtime.

## Validation boundary

This connector cycle changed authoritative source and deterministic test registration on `main`, but did not execute the .NET test suite, compile against the installed Valheim API, or perform live multiplayer acceptance. Those remain runtime validation requirements rather than inferred completion claims.

## Next actionable slice

Complete the runtime interaction framework by extracting the trophy-consumption/compensation decision boundary into a deterministic transaction executor that can be exercised without Valheim objects. Validate success, partial removal failure, persistence rejection, inventory compensation failure, and state rollback failure. Keep actual `Inventory`, `Player`, `ZNet`, and global-key operations in the Runtime adapter. After that boundary is deterministic, run the complete six-stone physical interaction sequence through the strongest available runtime harness before beginning individual Deep Boon behavior.
