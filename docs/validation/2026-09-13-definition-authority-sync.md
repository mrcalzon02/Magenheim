# Definition Authority Synchronization Validation — 2026-09-13

## Reconciled baseline

The live `main` branch already contained definition schema 2, deterministic effective fingerprints, controlled balance/worldgen compatibility overrides, and a concurrent worldgen-policy integration commit. That work was retained as authority.

The active execution host still does not expose a .NET SDK/compiler or a Valheim runtime. This record therefore distinguishes committed/static verification from runtime admission.

## Implemented this cycle

- Added pure-core `DefinitionAuthorityHandshake` comparison under `Magenheim.Core.Networking`.
- Authority descriptors contain only the validated schema version and lowercase SHA-256 fingerprint.
- Invalid descriptors, schema mismatches, and fingerprint mismatches fail closed with `MutationAuthorized = false`.
- Added runtime `DefinitionAuthoritySynchronizer` using a Jötunn `CustomRPC` registered during plugin `Awake`.
- Registered the RPC with Jötunn `SynchronizationManager.AddInitialSynchronization`, so the server's effective authority package is scheduled during connection establishment before the client enters the world.
- The client compares the received server authority against its own effective validated snapshot and sends an acknowledgement containing its local authority descriptor.
- The server independently compares that acknowledgement against its own authority and records admission per peer ID.
- Exposed fail-closed client and per-peer mutation gates for later inventory/refinement/socket transaction code.
- No gameplay mutation was enabled in this cycle.
- Runtime version advanced to `0.0.7` and pre-release network compatibility strictness was tightened from Minor to Patch because this release introduces a new RPC contract. This prevents an older `0.0.x` client lacking the handshake from being admitted merely because its major/minor version matches.

## Static invariants verified by source review

- The server never trusts a client-supplied boolean admission result; it recomputes compatibility from the client's schema/fingerprint.
- The client never becomes mutation-authorized before receiving and matching server authority.
- Unknown/malformed message types fail closed.
- Malformed descriptors fail closed.
- Schema mismatch fails closed even if the fingerprint text were otherwise equal.
- Fingerprint mismatch fails closed.
- The authority comparison is ordinal and fingerprints are required to be canonical lowercase 64-character SHA-256 hex strings.
- The handshake consumes the already validated effective definition snapshot; it does not introduce a second definition or balance authority.

## Runtime evidence boundary

The Jötunn documentation currently describes `NetworkManager.AddRPC`/`CustomRPC` for custom RPC registration and `SynchronizationManager.AddInitialSynchronization` for server-to-client data delivered before the client fully enters the world. The implementation follows that documented shape.

Compilation, package restore, actual RPC serialization, connection timing, host/client behavior, and dedicated-server behavior have not been directly executed in this environment and are not claimed as passing.

## Next exact action

In a current Valheim development environment with .NET and Jötunn 2.30.0:

1. run the existing `Magenheim.Core.Tests` harness;
2. compile `Magenheim.Core` and `Magenheim.Runtime` with warnings as errors;
3. verify a matching host/client pair reaches compatible authority on both sides;
4. verify a deliberately changed server balance override causes the client and server peer gate to remain unauthorized;
5. verify malformed or stale authority messages fail closed without enabling gameplay mutation;
6. only after those gates pass, register permanent Crystal Shaping skill ID `magenheim.crystal_shaping` and continue toward item/worldgen registration.
