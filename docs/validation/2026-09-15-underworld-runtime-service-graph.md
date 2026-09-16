# Underworld runtime service graph — 2026-09-15

## Intent

Complete the composition boundary needed after the deterministic spatial domain and multiplayer authority repair: provide one canonical construction path for Underworld transition persistence, logical world context, spatial placement, transaction orchestration and recovery.

## Reconciliation

Authoritative `main` advanced beyond the previous Underworld authority commit with Deep Fracture encounter-persistence work through `4672603e969defb2579ed9ffd9239902ef721eeb`. This slice preserves that work and adds only the missing Underworld runtime composition boundary.

## Implemented

`src/Magenheim.Runtime/UnderworldRuntimeServices.cs` now constructs one coherent service graph:

- one `UnderworldSpatialDomainDefinition` from the gameplay-authoritative default spatial policy;
- one atomic `UnderworldTransitionStateStore` rooted under the BepInEx config directory;
- one `ValheimLogicalUnderworldWorldContextController`;
- one `ValheimUnderworldTransitionPlacementHost` consuming that same spatial-domain instance;
- one `StoredUnderworldTransitionHost`;
- one `UnderworldWorldTransitionManager`;
- one `UnderworldTransitionRecoveryRuntime`.

The composition root exposes the transition manager and recovery runtime to the eventual entry/return boundary and owns the logical-world reset operation. No second persistence engine, transition manager, spatial mapper or world-context controller is introduced.

Construction is intentionally side-effect free with respect to the live Valheim world. The graph can exist before `ZNet.instance` is available; context activation remains fail-closed inside the existing placement/context seam when a transition actually executes.

## Persistence boundary

Transition records are designed to be rooted below the BepInEx config directory at `Magenheim/underworld-transitions`. This keeps durable transition recovery outside the plugin assembly directory while preserving the existing atomic store implementation and paired-world/player namespacing.

## Validation boundary

This connector cycle verifies remote source composition and commit ancestry. It does not execute the Valheim/.NET runtime profile, so compilation or live transition success is not claimed. Plugin lifecycle ownership is deliberately the next mutation rather than being claimed before it exists.

## Next actionable slice

Instantiate `UnderworldRuntimeServices` from `MagenheimPlugin.Awake` and reset it during plugin/world teardown, then bind live Valheim parent-world and player identity resolvers. After that, add the first server-authoritative entry/return trigger boundary that prepares transitions through Core rules, executes through `TransitionManager`, and invokes `RecoveryRuntime.LoadAndResume` on player/world admission before U4 terrain generation begins.
