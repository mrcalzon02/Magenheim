# Session-bound mutation RPCs — 2026-09-15

## Scope

The authority handshake now assigns and synchronizes a positive server-local session generation for each connection. The remote workstation and socket mutation transports previously used that generation only in the server-side replay guard key. Client requests, server responses and client acknowledgements did not carry it explicitly.

That left two stale-session hazards. First, a request created under an older client connection could be interpreted under a newer server generation if the routed peer identity was reused. Second, a delayed response could still match a client-side pending operation id after reconnect because the client pending dictionaries were keyed only by operation id.

## Repair

`WorkshopOperationRpc` and `SocketOperationRpc` now bind their full request/response/acknowledgement cycle to `DefinitionAuthoritySynchronizer.ClientSessionGeneration`.

For both transports:

- client submission requires a positive currently authorized session generation;
- the generation is stored in the client pending-operation record;
- request packages carry the generation immediately after the message type;
- the server compares the supplied generation with `GetPeerSessionGeneration(sender)` before preparing mutation;
- response packages carry the server's current generation;
- clients reject responses when the response generation differs from either the pending record or the currently authorized client generation;
- acknowledgements echo the generation;
- the server accepts an acknowledgement only when it matches the current peer generation.

The server-side operation keys remain `(PeerId, SessionGeneration, OperationId)`, so the transport envelope and the replay guards now use the same session identity instead of two partially independent notions of connection state.

## Commits

- `a443199074a00e944b1be28ae67ec1124e195908` — authority acknowledgement bound to server session generation.
- `6c8ec3e926a3e5e0b0167ba8baf042b9c98f2619` — workstation request/response/acknowledgement bound to authority session.
- `2f7065cb7fe3cc404eba8f7871bb17eb92bf1781` — socket request/response/acknowledgement bound to authority session.

## Acceptance boundary

This is a source-level network safety repair. A current Valheim/Jötunn build and multiplayer reconnect test remain required. Acceptance should prove normal current-generation operations succeed, a stale-generation request is rejected without preparation, a stale response cannot mutate client inventory, and a stale acknowledgement cannot commit a prepared server operation.

A follow-up source cleanup remains: retire old client pending-operation records when a new authority generation becomes active so safely rejected stale records cannot accumulate against the pending-operation cap. That cleanup is separate from mutation safety and should be completed before runtime acceptance is closed.
