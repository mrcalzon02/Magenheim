# Underworld Deepstone atomic transaction planning — 2026-09-15

## Reconciliation

Current `main` already contains the six-stone Deepstone Conclave geometry and pure `UnderworldDeepstoneProgression` authority for canonical boss/trophy/prerequisite/boon relationships. Runtime still lacks the server-owned trophy interaction/RPC and durable Conclave state adapter. Before that runtime mutation can be safely admitted, Core must define the complete atomic inventory/state mutation contract rather than leaving trophy consumption policy to the Valheim adapter.

A separate architectural inconsistency was also observed during reconciliation: `UnderworldWorldCenterRegistrar` exists but is not currently instantiated, and `UnderworldWorldSessionLifecycle` contains a stale comment referring to a separate-world loader even though the current authoritative runtime uses the logical reserved spatial-domain model. This was not silently papered over in this slice; spatial admission of the Conclave must be repaired at the authoritative runtime boundary rather than registering the center as ordinary parent-world random worldgen.

## Implemented

`UnderworldDeepstoneProgression` now exposes `PlanMountTrophy`, producing an immutable `UnderworldDeepstoneMountTransactionPlan`.

The plan explicitly distinguishes Ready, MissingTrophy, AlreadyMounted, WrongTrophy, PrerequisiteMissing and UnknownDeepstone outcomes. Only Ready plans authorize inventory consumption, and every Ready plan consumes exactly one canonical boss trophy while carrying the same resulting state that atomically marks the trophy mounted and its Deep Boon unlocked. Every rejected plan has a zero consume count.

This keeps deterministic progression and mutation planning in `Magenheim.Core` and leaves the future runtime adapter responsible for server authority, authoritative inventory observation/removal, ZDO persistence and presentation.

## Deterministic coverage

`UnderworldDeepstoneProgressionTests` now additionally verifies:

- possession count zero rejects an otherwise valid trophy without authorizing consumption;
- a valid possessed trophy produces exactly one-unit consumption and mounted+boon state together;
- prerequisite rejection consumes nothing;
- duplicate requests consume nothing.

The existing aggregate already executes this test suite.

## Validation boundary

This connector cycle verified source integration and committed tests but did not execute the .NET harness, compile the runtime, start Valheim, or perform multiplayer persistence testing. Those states are not claimed.

## Next actionable slice

Implement the server-owned Deepstone runtime transaction bridge: bind each physical Conclave stone to its canonical Deepstone id, reconstruct all six states from durable synchronized persistence, route trophy-use requests through the existing session-bound mutation-authority pattern, call `PlanMountTrophy` on the server, remove exactly the planned trophy count, persist mounted/boon state, and synchronize presentation. In the same framework phase, reconcile Conclave spatial admission with the current logical reserved-domain architecture so the world center is actually instantiated below without becoming ordinary parent-world random worldgen.
