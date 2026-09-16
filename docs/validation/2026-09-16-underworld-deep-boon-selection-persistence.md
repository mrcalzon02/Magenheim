# Underworld Deep Boon selection persistence

## Implemented

Per-player Deep Boon selection now has a canonical versioned Core codec and a server-owned durable Runtime store. Records are isolated by paired-world identity and player identity, written through a durable temporary file, read-back validated, atomically promoted where supported, and retain the previous verified generation as recovery backup.

Persistence does not grant a boon. Decoded selection state remains an untrusted persisted fact and must be passed through `UnderworldDeepBoonSelection.Reconstruct` with the current authoritative Conclave state before runtime application or mutation.

`UnderworldRuntimeServices` now owns the selection store under `Magenheim/underworld-deep-boon-selections`, separate from transition and world-pair persistence. Core deterministic validation covers empty and selected round trips, canonical encoding, delimiter-safe Unicode identities, unsupported versions, malformed base64, and blank selections.

## Validation boundary

This connector cycle verifies authoritative repository writes and Core harness registration. It does not claim local .NET execution, Valheim startup, or multiplayer persistence acceptance.

## Next actionable slice

Implement the server-authoritative Deep Boon selection interaction/RPC. The server must resolve player/world identity, reconstruct current Conclave unlock state, load and revalidate persisted selection, execute `UnderworldDeepBoonSelection.Select` or `Clear`, persist only an admitted changed state, and return a bounded result. After that boundary is durable across reconnect, implement the generic Deep Boon runtime status-effect/application framework before individual boon minutiae.
