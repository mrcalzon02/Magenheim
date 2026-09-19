# Underworld world-context architecture decision — 2026-09-15

## Status: SUPERSEDED

This validation record is retained only as history. Its former conclusion — hosting the playable Underworld as a logical layer inside the parent Surface world — is **not authoritative and must not be implemented**.

The current authority is `INSTRUCTIONS.md`, `docs/UNDERWORLD_DESIGN.md` §6, `docs/UNDERWORLD_INSTANCE_CONTRACT.md`, and `Magenheim.Core.Underworld.UnderworldInstanceContract`.

## Corrected decision

The Underworld is a dedicated Magenheim-owned instanced world-space. It is not a distant Surface landmass, not a coordinate-projected region of Surface `WorldGenerator`, and not a second save that the player manually selects. Surface and Underworld remain one progression experience while the Underworld owns its own terrain/chunk, biome/environment, exploration, map and persistence authorities.

The active Valheim parent-world metadata may be used to authenticate and deterministically derive the instance identity. It must not be treated as proof that Underworld gameplay is hosted by Surface generation.

`ValheimUnderworldWorldContextController` is therefore only a transition/context-selection boundary. It may bind and validate the deterministic parent/derived identity pair and selected layer, but it is not itself the terrain or instance-world implementation. Native instance terrain is supplied through `UnderworldInstanceTerrainDomain`, `UnderworldInstanceChunkGrid`, `UnderworldInstanceChunkSampler` and `UnderworldInstanceChunkStreamingRuntime`; renderer/materializer and remaining engine integration must consume those native instance payloads without routing them through Surface `WorldGenerator`.

## Runtime invariant

A runtime session binds to one deterministic derived Underworld identity for the lifetime of the parent-world session. Concurrent or recovered transitions carrying a different derived identity must fail closed rather than silently replacing instance authority. Binding is released only at the parent world-unload/shutdown boundary.

## Acceptance still required

Source-level native chunk generation and persistence do not by themselves complete runtime acceptance. U2 remains open until the runtime materializer/engine boundary proves Surface -> Underworld -> Surface, interruption recovery, reload/reconnect while below, multiplayer isolation, dedicated map/exploration behavior, and absence of Surface-generation collision in a disposable live world.
