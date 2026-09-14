# Geode Replay Identity Hardening

## Scope

Bounded advanced-code repair on the authoritative `main` branch.

## Repository condensation

The redundant legacy `master` branch already redirects to `main`; no independent development branch remains. The singleton `src/Magenheim.Core/Transactions/` directory was flattened by moving `RefinementTransactionPlanner.cs` to `src/Magenheim.Core/RefinementTransactionPlanner.cs` without changing its namespace or implementation.

## Defect found

`GeodeOpeningOperationGuard.Begin` recalculated the complete mutation plan before checking whether an operation key had already been seen. After a successful opening consumed its source geode, retransmitting the same operation id with current inventory state could be rejected as `MissingSource` before the guard recognized it as `DuplicateApplied`. This was fail-safe against duplicate mutation, but it broke exact-once replay identity and made retransmission results depend on post-mutation inventory state.

The same ordering also meant conflicting replay detection depended on a freshly valid transaction plan rather than on the immutable operation intent.

## Repair

The guard now derives and stores immutable geode-opening intent from:

- geode definition id;
- exact IEEE-754 bits of the second-crystal roll;
- exact IEEE-754 bits of the third-crystal roll;
- exact IEEE-754 bits and ordering of all element rolls.

For an existing operation key, the guard compares this immutable intent before performing any fresh inventory/capacity planning. Matching intent returns `DuplicatePrepared` or `DuplicateApplied` from the stored plan. Different intent returns `ConflictingReplay`. Fresh operation ids still pass through the normal authority, inventory, capacity, and cracking planner before being admitted.

Mutable inventory counts and capacity are deliberately not part of replay identity: those are pre-mutation admission facts, not the identity of the requested random geode outcome.

## Verification

Source and Git-object verification completed. Remote `main` contains commit `f60a1f7ecd28921a30fa10b65a93ac2f9cd6c061` with the geode guard change and transaction-directory flattening.

The current execution environment has no .NET SDK and cannot reach GitHub through the shell, so compilation and executable deterministic tests were not rerun in this cycle. Runtime admission is therefore deferred.

## Remaining risk

Host-local workshop inventory mutation still commits inventory and replay state as separate steps. A process crash between inventory mutation and replay-state persistence is not solved by an in-memory guard. Durable exact-once semantics across server restarts will require persistent transaction state or a recoverable journal rather than additional in-memory flags.

## Next dependency-valid slice

Apply the same immutable-intent-before-replanning rule to refinement operation replay, then bind inventory mutation and replay commit into one rollback boundary for host-local geode/refinement execution. After that, implement the remote-client request/approval RPC using server-owned RNG and the same operation identities rather than inventing a parallel transaction path.
