# Underworld Deep Boon Selection RPC Slice — 2026-09-16

## Implemented

Per-player Deep Boon selection now has a server-authoritative transport boundary. Clients submit only a canonical boon id or a clear request plus the active authority generation and monotonically increasing request id. Remote requests are session-authorized and replay-filtered before player or persistence mutation.

The server resolves the requesting peer's authoritative `Player`, resolves the current paired-world identity from live world metadata, requires the derived Underworld layer, reconstructs the complete Conclave from persistent world keys, reloads the player's durable selection, and revalidates that persisted selection through Core before executing `UnderworldDeepBoonSelection.Select` or `Clear`.

Only Core results marked as changed are written to `UnderworldDeepBoonSelectionStore`. Persistence failure returns failure and no runtime boon effect is applied. Unknown or locked boon identities therefore cannot become durable state through the transport. Dedicated-server requests use the resolved remote player id rather than relying on `Player.m_localPlayer`.

`UnderworldDeepstoneRuntimeAuthority` now exposes selection reconstruction/select/clear adapters so Runtime never duplicates definition or unlock rules. `MagenheimPlugin` registers the new transport beside the existing Deepstone interaction RPC.

## Validation boundary

This cycle verified authoritative source writes and registration on `main`. It does not claim local .NET compilation, Valheim startup, or multiplayer acceptance because those execution surfaces are not available through the repository connector.

## Next actionable slice

Build the generic `DeepBoonRuntime` application framework. It must reconstruct the selected boon from durable per-player state on admission/reconnect, map the canonical selected boon to exactly one Magenheim-owned status/effect application, remove stale Magenheim boon effects when selection changes or clears, never mutate vanilla Forsaken-power state, and remain server-authoritative. After that framework is closed, implement the six boon behaviors iteratively, beginning with Spore Communion.
