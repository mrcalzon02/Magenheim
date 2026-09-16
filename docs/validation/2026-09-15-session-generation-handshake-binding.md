# Session-generation authority handshake binding — 2026-09-15

## Defect

`DefinitionAuthoritySynchronizer` already assigned a globally monotonic server-local session generation whenever Jötunn began initial synchronization for a peer. Replay guards for geode opening, refinement, socket installation and socket extraction were retired against that generation.

The authority acknowledgement itself was not bound to the generation, however. The server package carried only the message type and authority descriptor, and the client acknowledgement echoed only the authority descriptors. If a routed peer identity were reused after reconnect, a delayed acknowledgement from the older connection could not be distinguished from acknowledgement of the new server session. `_peerResults[sender]` could therefore be authorized by an acknowledgement that predated the current replay generation.

## Repair

`DefinitionAuthoritySynchronizer` now writes the freshly allocated positive `long` session generation into the server authority package before the descriptor. The client reads and validates the generation, exposes it as `ClientSessionGeneration` only when local/server gameplay authority comparison succeeds, and echoes that exact generation in the acknowledgement.

The server now requires an active `_peerSessionGenerations[sender]` entry and exact equality between the echoed generation and the current server generation before evaluating the authority descriptors. Missing or stale generations produce a fail-closed `InvalidDescriptor` result and cannot authorize mutation.

This makes the authority handshake and the operation replay guards share the same connection-generation boundary. A peer-id reuse is no longer sufficient to carry authority admission from an older session into a newer one.

## API boundary

The transport uses Valheim `ZPackage.Write(long)` / `ReadLong()` for the generation. This is the same native ZPackage long serialization used by Valheim networking; the normal runtime build remains the acceptance gate for the installed API.

## Acceptance boundary

Source repair is committed on `main`. Runtime acceptance still requires a current Valheim/Jötunn build plus a reconnect test demonstrating that a current-generation acknowledgement admits the peer and a deliberately stale-generation acknowledgement is rejected. No live-runtime claim is made by this record.
