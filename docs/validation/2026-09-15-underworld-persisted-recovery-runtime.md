# 2026-09-15 — Underworld persisted recovery runtime

Status: source implementation advanced; compile/live acceptance still required.

## Reconciliation

This bounded cycle began from live `main` after the concurrent Deep Fracture encounter-population commits. The existing Underworld transition transaction framework, runtime orchestrator, canonical persistence codec and new atomic state store were retained.

Inspection found one immediate root defect in the store: `UnderworldWorldIdentity` exposes `DerivedSeedFingerprint`, but the new record-path hash referenced a nonexistent `DerivedSeed`. That would prevent the Runtime project from compiling. The store now keys paired-world records with `ParentWorldId + DerivedWorldId + DerivedSeedFingerprint`, matching the authoritative Core identity.

## Material implementation

`UnderworldTransitionStateStore` now also admits the `.bak` file left by interrupted replacement as a recovery candidate. Primary state remains preferred; a valid backup is decoded against the same parent/derived identity and player identity and is used only when the primary cannot be admitted. Corrupt primary and corrupt backup both fail closed.

`UnderworldTransitionPersistenceRuntime.cs` adds the missing composition layer:

- `IUnderworldTransitionPlacementHost` contains only the eventual Valheim-specific context/placement/observation operations.
- `StoredUnderworldTransitionHost` implements the existing `IUnderworldTransitionHost` by routing every persistence write through `UnderworldTransitionStateStore` and delegating only placement work.
- `UnderworldTransitionRecoveryRuntime` is the load/reconnect entry point. It loads a canonical persisted record, distinguishes missing/stable/failed state, and routes every incomplete transition through `UnderworldWorldTransitionManager.ResumeOrRecover`.
- Prepared, TargetReady and RecoveryRequired snapshots therefore use the same recovery rules as live transition failures rather than acquiring a second reconnect-specific state machine.

## Invariants preserved

- Core remains the sole transition legality/state authority.
- Runtime does not invent a second codec or progression flag.
- Persistent records remain isolated by paired-world identity and player identity.
- World switching remains behind one small game-version-specific placement seam.
- Incomplete transitions are never guessed forward after reconnect.
- Failed decode/recovery does not silently clear the authoritative record.

## Validation boundary

This connector cycle can verify committed source structure and identity consistency but cannot claim the normal local Valheim/.NET Runtime build, game startup, disposable-world transition, reconnect or multiplayer acceptance.

The prior 0.0.49 closeout remains the last recorded local compiled/installed baseline. These new Underworld Runtime sources must be compiled against that same current Valheim/Jotunn environment before runtime admission.

## Next actionable slice

Implement the actual Valheim `IUnderworldTransitionPlacementHost` adapter after inspecting the installed 1.0.12 world/session APIs, then wire `UnderworldTransitionRecoveryRuntime.LoadAndResume` into authoritative player/session load. The first live gate remains deliberately narrow: surface -> derived Underworld -> surface, forced interruption at Prepared/TargetReady, restart/reconnect recovery to source, and proof that the surface save remains intact.
