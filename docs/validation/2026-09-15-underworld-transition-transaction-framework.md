# 2026-09-15 — Underworld transition transaction framework

Status: **source/static validation only in this connector cycle; runtime transition acceptance remains open**

## Intent

Advance the highest-priority dependency-valid Underworld framework slice after schema-5 content/architecture authority integration. U2 requires a safe derived-world transition before biome production. This slice establishes the pure server-owned transaction rules that runtime world loading, persistence and RPC adapters must consume.

## Starting authority

- Repository: `mrcalzon02/Magenheim`
- Branch: `main`
- Reconciled remote baseline immediately before mutation: `6fbfabb91bcd7c39fd106fdd6bd3330f8bf3ef77`
- Existing schema-5 Underworld definition and architecture authority remains unchanged.
- Existing derived identity in `UnderworldWorldIdentity.cs` remains the only logical parent/derived world identity authority.

## Implemented source

`src/Magenheim.Core/Underworld/UnderworldTransitionRules.cs` adds one pure transaction state machine for cross-layer movement.

The framework now requires:

- one stable surface or Underworld layer state per player;
- one parent/derived world identity pair;
- a persisted surface return anchor before entry transfer begins;
- an explicit operation id for replay/mismatch rejection;
- the gameplay-authority fingerprint captured at transition start;
- `Prepared -> TargetReady -> Commit` ordering so player state cannot move before target initialization acknowledgement;
- recovery-required state with a retained source anchor when loading/transfer fails;
- deterministic recovery back to the source layer;
- fail-closed validation for wrong-world anchors, non-finite coordinates, missing Underworld return anchors, direction/layer disagreement and authority drift.

The pure Core layer deliberately performs no Unity, Valheim, Jötunn, RPC, filesystem or world-loading work. Runtime adapters remain responsible for actually creating/loading the derived world, persisting state and moving the character.

## Deterministic coverage

`tests/Magenheim.Core.Tests/UnderworldTransitionTests.cs` covers:

- locked entry rejection before Nowhere King defeat;
- source-anchor persistence before entry;
- prohibition on commit before target-ready acknowledgement;
- operation-id mismatch rejection;
- authority-fingerprint drift rejection;
- successful entry and stable Underworld state;
- successful return and surface-anchor cleanup;
- failed-entry recovery to surface;
- failed-return recovery back below while preserving the retryable surface anchor;
- foreign-world anchor rejection;
- persisted-below state rejection when its surface anchor is missing;
- non-finite coordinate rejection.

The suite is wired into the existing `DefinitionAuthorityTests` harness so it cannot silently fall outside normal deterministic Core validation.

## Validation boundary

This connector cycle does not provide the normal local .NET/Valheim build environment. Therefore this record does **not** claim compilation, harness execution, runtime world creation, character transfer, persistence, multiplayer, death handling or disposable-world acceptance for this new source.

The previous authoritative project state records 36,868 deterministic assertions and a clean 0.0.49 runtime build before later staff/model commits; those historical results do not validate this new transition source.

## U2 status

U2 remains open. The pure transaction contract is now present, but the implementation plan explicitly requires a disposable-runtime proof of enter, return, save/reload, reconnect, failure recovery and multiplayer authority before production biome work may depend on it.

## Next actionable framework slice

Implement the thinnest runtime `UnderworldWorldTransitionManager` adapter around this Core transaction authority. It should first prove a reversible derived-world context switch in a disposable environment without adding biome production content. The adapter must:

1. resolve the canonical `UnderworldWorldIdentity` from the current parent world;
2. consume the existing Nowhere King encounter state and synchronized gameplay authority;
3. persist the Core transition snapshot before any transfer;
4. initialize or load the derived target context;
5. call `MarkTargetReady` only after critical target initialization succeeds;
6. move the player and commit only after the runtime observes successful placement;
7. invoke recovery rules on every failed path;
8. preserve enough state to survive disconnect/reload during an incomplete transition.

Only after that reversible runtime proof should U3 persistence/multiplayer hardening and U4 terrain-generation framework work proceed.
