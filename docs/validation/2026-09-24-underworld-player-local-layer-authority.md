# Underworld player-local layer authority repair

Date: 2026-09-24

## Reconciliation

The six Deep Sigil source/destination chains are now resident, but the higher-priority P0.-1 instance correction still contained a multiplayer authority defect. `ValheimUnderworldWorldContextController` stored one mutable `_activeLayer` for the entire loaded parent-world session. A Deep Gate transit by one player therefore changed the layer reported for every player. `UnderworldWorldSessionLifecycle` then either admitted every connected player transform as an Underworld chunk focus or none of them. This violated the dedicated-instance contract and the explicit rule that instance lifetime and identity must not be reconstructed from player population or per-player transition state.

The 2026-09-20 Valheim-first duplication audit had already identified the controller as temporary and instructed development to collapse it if `UnderworldInstanceLifecycle` could expose the active identity directly. It can, and now does so as the sole loaded-world instance identity/phase authority.

## Changed

- Removed `ValheimUnderworldWorldContextController` and its duplicate instance identity/layer fields.
- `UnderworldRuntimeServices` now validates local sessions against `UnderworldInstanceLifecycle.Identity` and derives only the querying local player's layer from the disjoint `UnderworldInstanceLayer` engine-space adapter.
- Deep Gate transit no longer mutates a global layer before or after teleport. Valheim player movement is the transition; successful physical placement determines Surface versus Underworld classification.
- Native chunk residency now examines every connected player independently and accepts a transform as an instance-space focus only when that transform is physically inside the dedicated Underworld engine layer. Surface players are ignored rather than projected into Underworld coordinates.
- The instance origin remains resident independently of player population, preserving Conclave/return-gate availability.

## Architectural result

There is now one persistent loaded-world authority for instance identity and lifecycle: `UnderworldInstanceLifecycle`. Player layer is observation of physical placement, not stored multiplayer truth. No player count is tracked, and entering/leaving does not create, destroy, activate or deactivate the persistent instance.

## Verification boundary

This connector cycle verifies the committed source graph and remote `main` state. It does not claim a local Valheim/.NET build or live multiplayer runtime test because the connector environment does not expose the repository checkout, installed Valheim assemblies or game process.

## Next dependency-valid slice

Continue P0.-1 rather than returning to cosmetic biome work. Audit remaining `UnderworldSpatialDomain.ToHostAnchor` / host-band consumers and replace the highest-impact live placement consumer with native `UnderworldInstanceLayer` / instance-anchor placement. Then reconcile persistence keys to parent-world + derived-instance identity and execute the disposable-world Surface -> Underworld -> Surface, save/reload below, reconnect, host/client and dedicated-server acceptance matrix when a Valheim runtime is available.
