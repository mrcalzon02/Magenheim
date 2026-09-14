# Peer authority session reset validation — 2026-09-14

## Defect

`DefinitionAuthoritySynchronizer` cached server-side mutation admission in `_peerResults` by routed peer id. The cache was overwritten after a new acknowledgement, but it was not invalidated when a new connection began. Because a transport/session peer id is not a durable player identity, a reconnect or reused id could temporarily inherit an earlier connection's `MutationAuthorized=true` result before the new definition-authority acknowledgement completed.

That violates Magenheim's fail-closed multiplayer rule: every connection must begin non-authorized and earn mutation admission from the current effective schema/fingerprint handshake.

## Authoritative repair

The initial synchronization callback now uses Jötunn's peer-aware `AddInitialSynchronization(CustomRPC, Func<ZNetPeer, ZPackage>)` path. `BuildServerAuthorityPackage(ZNetPeer peer)` removes any cached result for `peer.m_uid` before constructing the server authority package.

This repairs the authority lifecycle at the point a new session begins rather than relying on a disconnect hook, timeout, or secondary cleanup shim.

## Static invariants reviewed

- a never-seen peer remains `Pending` and non-authorized;
- a previously admitted peer id is invalidated before the new session receives the server descriptor;
- only a successfully parsed acknowledgement can repopulate `_peerResults`;
- malformed, mismatched, or absent acknowledgements remain non-authorizing;
- no gameplay mutation path is enabled merely by this repair;
- definition comparison semantics and fingerprint authority are unchanged;
- no branch, parallel handshake, or alternate authorization store was introduced.

## External API verification

Current Jötunn documentation exposes `SynchronizationManager.AddInitialSynchronization(CustomRPC, Func<ZNetPeer, ZPackage>)` and states that it runs server-side during login before the client connection is fully established. That is the lifecycle boundary used by this repair.

## Runtime boundary

The current execution host does not provide the Valheim/Jötunn compile/runtime environment. The change is source-reviewed but not compiled or exercised in-game. Runtime admission remains deferred.

## Next dependency-valid advanced slice

After the existing runtime compile gate is available, validate reconnect and peer-id reuse behavior on host/client and dedicated server. Then bind the existing definition-authority admission gate into the first atomic geode/refinement gameplay transaction so no inventory mutation can occur while the requesting peer is Pending or mismatched.
