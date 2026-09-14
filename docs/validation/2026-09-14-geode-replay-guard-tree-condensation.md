# Session-scoped geode replay guard and tree condensation — 2026-09-14

## Target

Condense redundant singleton source branches first, then close the duplicate/replayed-RPC gap in the pure geode-opening transaction authority without enabling inventory mutation prematurely.

Starting `main`: `4279dfd86f8c986943c45fca2484d682339360e0`.

## Repository reconciliation

- `main` and `master` both resolved to the same starting commit, so no content divergence existed.
- GitHub repository metadata still declares `master` as the default branch even though `INSTRUCTIONS.md` declares `main` as the sole development authority.
- The available repository connector can move branch refs but cannot change the repository default branch or delete a branch. No false branch-deletion claim is made.
- This cycle keeps both refs synchronized while continuing all development on `main`.

## Tree condensation

Three singleton source directories were flattened without changing namespaces or creating parallel implementations:

- `src/Magenheim.Core/Networking/DefinitionAuthorityHandshake.cs` -> `src/Magenheim.Core/DefinitionAuthorityHandshake.cs`
- `src/Magenheim.Core/Transactions/GeodeOpeningTransactionPlanner.cs` -> `src/Magenheim.Core/GeodeOpeningTransactionPlanner.cs`
- `src/Magenheim.Runtime/Networking/DefinitionAuthoritySynchronizer.cs` -> `src/Magenheim.Runtime/DefinitionAuthoritySynchronizer.cs`

The multi-file `Definitions` and `Worldgen` branches remain because they currently contain coherent related authorities rather than one-file directory ceremony.

## Defect found

The existing `GeodeOpeningTransactionPlanner` correctly failed closed on authority, source, cracking, and capacity, but it had no operation identity or replay memory. A duplicated/retransmitted gameplay RPC could therefore obtain the same Ready mutation plan more than once. The earlier definition-authority session reset prevented stale peer authorization but did not prevent duplicate application inside one admitted session.

Using only routed peer id as a replay key would also be insufficient because peer ids may be reused after reconnect.

## Repair

`GeodeOpeningOperationGuard` now owns session-scoped exact-once admission for geode-opening plans:

- operation identity is `(peerId, serverSessionGeneration, operationId)`;
- only a fresh `Ready` decision authorizes mutation;
- a replay while the first operation is prepared returns `DuplicatePrepared` and cannot mutate;
- a replay after application returns `DuplicateApplied` and cannot mutate;
- reusing the same operation key for a different mutation plan returns `ConflictingReplay` and fails closed;
- runtime may call `AbortPrepared` only when no mutation occurred, allowing a legitimate retry;
- `MarkApplied` succeeds only once;
- older operation records for a routed peer are retired when a new authority-sync session begins.

`DefinitionAuthoritySynchronizer` now assigns a strictly increasing server-local session generation each time initial synchronization starts for a peer id. It clears prior mutation admission and retires older geode replay records before sending the authority descriptor. Generation exhaustion fails closed rather than recycling an old replay namespace.

`RuntimeServices` owns one shared `GeodeOpeningOperationGuard`, and the plugin injects that single authority into the synchronizer. The plugin version advances to `0.0.9` because the runtime network/transaction contract changed.

## Deterministic source coverage added

`GeodeOpeningTransactionTests` now covers fresh admission, duplicate-prepared denial, one-time applied transition, duplicate-applied denial, conflicting replay denial, abort-and-retry behavior, old-session retirement, new-session identity separation, and invalid generation rejection in addition to the existing authority/source/capacity/cracking cases.

## Failure boundaries

This guard protects duplicate/replayed requests within the running server session and separates reconnect sessions. It does not by itself make a non-atomic inventory adapter crash-safe. The eventual runtime executor must still:

1. obtain `Ready` from the guard;
2. revalidate ownership, source count, and output capacity immediately before mutation;
3. consume the source and grant outputs as one server-side transaction as far as Valheim APIs permit;
4. call `MarkApplied` immediately after successful mutation;
5. call `AbortPrepared` only when it can prove no mutation occurred.

A process crash after inventory mutation but before `MarkApplied` would require durable transaction state to guarantee exact-once behavior across server restarts. That is intentionally not fabricated in the pure core.

## Validation boundary

Static source/Git review only in this host. No .NET SDK, Valheim runtime, Jötunn compile, live RPC replay, inventory mutation, or dedicated-server test is claimed.

## Next exact action

Implement the thin server geode-opening RPC/executor around this guard: server-owned random generation, authority result + session generation lookup, fresh operation-id admission, immediate inventory revalidation, atomic source/output mutation, Crystal Shaping XP grant, and applied/abort finalization. Then validate duplicate RPCs, disconnect/reconnect, full inventory, and interrupted transactions in a disposable host/client environment.
