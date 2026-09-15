# Underworld Valheim placement binding — 2026-09-15

## Scope

This bounded slice advances U2/U3 from generic transition orchestration into the current Valheim runtime without claiming that cross-world context switching is solved or runtime-accepted.

## Implemented

- Added `ValheimUnderworldTransitionPlacementHost` as the concrete player-placement adapter.
- Placement uses the repository-confirmed current `Character.TeleportTo(Vector3, Quaternion, bool)` API rather than the removed `Character.SetPos` path.
- Added `IUnderworldWorldContextController` as the single remaining world-selection seam. It must both activate and independently report the requested parent/derived context.
- Strengthened `IUnderworldTransitionHost.ObservePlayerPlacement` so commit/recovery observation receives `UnderworldWorldIdentity` and cannot validate coordinates while ignoring the active world context.
- Commit now requires observed target world context, position and heading. Recovery requires the same proof at the source context.
- Existing atomic transition persistence and reconnect recovery remain the only persistence authority.

## Reconciliation

The slice was applied directly to `main` and retained concurrent rendering/workstation repairs that landed during execution. No branch, GitHub Action, duplicate persistence engine or duplicate transition state machine was introduced.

## Evidence boundary

Source review confirms the repository already uses `Player.m_localPlayer` and `Character.TeleportTo`, so this adapter follows current Magenheim/Valheim API usage. Compilation, game startup and disposable-world transition acceptance were not executed by the GitHub connector and are not claimed.

External Valheim 1.0-era server tooling documentation also indicates a server instance has one active world at a time and changing the active save normally takes effect on restart. That is feasibility evidence, not sufficient proof of the in-process mod API boundary. The derived-world design therefore remains unchanged until the installed 1.0.12 assemblies or a live probe prove a supported context-switch mechanism.

## Next actionable slice

Implement and compile the concrete `IUnderworldWorldContextController` against the installed Valheim 1.0.12 assemblies. First inspect the actual `ZNet`, `World`, `WorldGenerator`, `ZoneSystem`, save/load and peer lifecycle members rather than reflecting guessed names. If the installed API cannot safely replace the active world context while peers remain connected, record that result and explicitly revise the Underworld layer architecture before proceeding. If it can, bind activation/observation, wire the persisted recovery runtime into player reconnect/load, and execute Surface -> Underworld -> Surface plus forced-interruption recovery in a disposable world.
