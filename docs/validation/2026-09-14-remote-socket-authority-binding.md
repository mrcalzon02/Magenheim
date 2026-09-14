# Remote Socket Authority Binding — Static Validation

Date: 2026-09-14
Source target: Magenheim 0.0.38
Validation class: static source/authority review only; runtime multiplayer acceptance deferred

## Implemented boundary

`SocketOperationRpc` binds non-host Geologist's Workstation socket operations to the existing synchronized gameplay-authority and replay-guard infrastructure.

For add-slot and install operations, the client submits immutable equipment/socket intent. The server requires an admitted peer authority session, validates that the requesting character is in range of a Magenheim Geologist's Workstation, reconstructs the equipment descriptor from the server-registered prefab, rejects client category/mod-origin/item-name disagreement, reevaluates the authoritative socket policy, and prepares the existing `SocketOperationGuard` plan.

For extraction, the server additionally requires a nearby Faceting Wheel, supplies the random extraction roll, and prepares the existing `SocketExtractionOperationGuard` plan. The client cannot supply a success/failure outcome or extraction roll.

The server response contains original/result socket state and any consumption/output plan. The client retains the exact selected `ItemData` reference and refuses the response if that item left inventory or its `magenheim.sockets.v1` state changed. Install also revalidates the exact selected crystal stack and identity. Extraction preflights the returned crystal/shards and inventory capacity. Mutations use inventory/metadata rollback on failure. Only after successful local application does the client acknowledge the server, which then marks the prepared replay guard applied; a negative acknowledgement aborts the prepared guard.

## Compatibility and persistence invariants

- No shared vanilla or foreign prefab is mutated.
- Socket state remains per-item metadata under the existing namespaced key.
- Unrelated foreign custom-data keys are untouched.
- Server policy is the synchronized fingerprinted `SocketEligibilityPolicy`; no second socket-rule authority was introduced.
- Remote equipment classification is not trusted from the wire: server prefab registration is the canonical descriptor source.
- Operation ids remain session-scoped and exact-once through existing guard keys plus server response replay records.
- Disconnect/session rollover continues to retire stale guard records through `DefinitionAuthoritySynchronizer`'s existing peer-generation retirement path.

## Failure modes reviewed

Rejected before planning: missing authority session, gameplay fingerprint mismatch, missing/out-of-range workstation, missing Faceting Wheel for extraction, unknown/mismatched server equipment prefab identity, invalid socket/crystal serialization, malformed operation intent, invalid skill snapshot, ineligible equipment, policy-cap overflow, missing/free-slot failures.

Rejected before client mutation: selected equipment no longer present, original socket state drift, install source crystal changed or vanished, invalid extraction output, insufficient extraction output capacity, response kind mismatch.

Rollback after mutation attempt: source consumption/output insertion or metadata write path throws/refuses; inventory stack topology and the Magenheim metadata value are restored before a negative acknowledgement.

## Verification performed

Verified by source inspection against the current `SocketTransactionPlanner`, `SocketExtractionService`, `ItemSocketAdapter`, `DefinitionAuthoritySynchronizer`, existing `WorkshopOperationRpc` transport pattern, and the workstation overlay call sites. Plugin registration and project assembly identity are advanced together to 0.0.38.

No local GitHub checkout, Valheim managed assemblies, running Jötunn environment, or multiplayer process is available in this execution environment, so compilation and runtime behavior are not claimed.

## Runtime gates still required

1. Compile 0.0.38 against the current Valheim/Jötunn development profile with warnings as errors.
2. Host plus one remote client: open a socket, install a crystal, and extract both successful and shattered outcomes.
3. Confirm an altered/stale client item state is rejected without metadata overwrite.
4. Confirm a policy fingerprint mismatch blocks requests.
5. Confirm a spoofed/ineligible third-party item descriptor is rejected against the server prefab classification.
6. Confirm duplicate response/request delivery cannot double-consume crystals, duplicate extraction output, or double-award Crystal Shaping XP.
7. Confirm save/load, drop/pickup, storage, repair/upgrade, death, transfer, and dedicated-server persistence remain intact after remote mutations.
