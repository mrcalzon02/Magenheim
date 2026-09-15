# Deep Fracture isolated interior binder — 2026-09-14

## Scope

This bounded cycle advances the existing collision-safe Deep Fracture passage authority into a concrete isolated-interior composition boundary. It does not enable surface Deep Fracture world generation.

## Implemented

`DeepFractureInteriorBinder` now implements the existing `IDeepFractureInteriorBinder` contract without replacing the entrance anchor. It attaches a single Magenheim-owned interior root, refuses duplicate binding, reports the pure-core maximum supported interior radius, and declares the current interior environment identity.

`DeepFractureInteriorRuntime` derives a deterministic expedition seed from the spawned location transform using explicit FNV-style integer mixing rather than runtime-randomized string hashing or client-local RNG. It consumes `DeepFractureExpeditionPlanner` as the sole topology/scale/encounter/interior authority.

The runtime composition path now instantiates every authoritative district Room in sequence, applies the district's elemental visual state, invokes `DeepFracturePassageAssembler` for collision-safe MainRoute/Branch corridors, and realizes Loop/Shortcut edges as paired registered traversal nodes configured to teleport to one another. Runtime placement does not reroll the core graph and does not mutate vanilla or foreign room registrations.

## Verification performed

The committed file was read back through GitHub at commit `62437bcbc2f06c8280c91dabe49e9cf859828393`. Static reconciliation confirms it consumes the existing `DeepFractureExpeditionPlanner`, `DeepFractureRoomRegistrar`, `DeepFracturePassageAssembler`, `DeepFractureRoomVisuals`, and `DeepFractureTraversalPortal` authorities rather than duplicating their rules.

No local Valheim/Jötunn runtime is available through this connector execution, so compilation, spawned-location lifecycle timing, environment identity acceptance, portal interaction, multiplayer generation equivalence, and save/load persistence are not claimed as verified by this cycle.

## Gate intentionally retained

`MagenheimPlugin` does not yet construct/register `DeepFractureLocationRegistrar`. The new binder therefore cannot inject surface entrances into a world by itself. This is intentional: the cave mouth must remain gated until an explicit entrance/return travel boundary and persistence behavior are implemented and validated. Encounter definitions are preserved in the expedition plan but enemy spawning is also not claimed by this binder.

## Next dependency-valid slice

Implement the entrance/return travel component and persistent interior identity boundary. The surface entrance must carry a stable expedition identity across save/load and multiplayer, enter at DF-01, return safely to its originating surface fracture, and restore traversal targets deterministically. Only after that boundary compiles and passes disposable-world testing should `DeepFractureLocationRegistrar` be wired into `MagenheimPlugin` and surface spawning admitted.