# Valheim-first duplication audit — 2026-09-20

## Purpose

This audit resets Magenheim's implementation rule: **custom code exists only where the Underworld's new instance-space, terrain, map, biome/environment, or Magenheim-specific gameplay requires an adapter Valheim does not already provide.** Player lifecycle, character persistence, inventory, networking, teleport execution, ordinary object persistence, ordinary zone/location registration, and other engine-owned behavior stay Valheim-owned.

The audit is deliberately biased toward deletion. A subsystem is not justified merely because it is robust, tested, or already committed.

## Removed now — confirmed redundant architecture

The entire durable player-transition framework was disconnected and deleted:

- `Magenheim.Core/Underworld/UnderworldTransitionRules.cs`
- `Magenheim.Core/Underworld/UnderworldTransitionStateCodec.cs`
- `Magenheim.Runtime/UnderworldTransitionStateStore.cs`
- `Magenheim.Runtime/UnderworldTransitionPersistenceRuntime.cs`
- `Magenheim.Runtime/UnderworldWorldTransitionManager.cs`
- `Magenheim.Runtime/ValheimUnderworldTransitionPlacementHost.cs`
- transition rule and codec test suites
- transition recovery/player enumeration in `UnderworldWorldSessionLifecycle`
- transition service composition in `UnderworldRuntimeServices`
- gameplay-fingerprint plumbing that existed only to police that transition state machine

Those facilities duplicated responsibilities already belonging to Valheim: player identity/session lifecycle, player persistence, teleport execution and ordinary character recovery. Deep Gate travel does not justify a parallel player database.

## Keep — genuinely Magenheim-specific instance adapters

These remain justified because vanilla Valheim does not natively describe Magenheim's separate Underworld coordinate/terrain product:

- `UnderworldWorldIdentity`: deterministic identity for the Magenheim-owned instance.
- `UnderworldInstanceTerrainDomain` and deterministic terrain/biome rules: actual new content.
- `UnderworldInstanceChunkGrid`, `UnderworldInstanceChunkStreamingRuntime`, and `UnderworldInstanceChunkMaterializer`: adapter/materialization boundary for native Underworld terrain. These must stay narrow and must not become replacements for Valheim player/zone/network systems.
- independent Underworld map presentation/exploration data: required because the Underworld map is intentionally not the Surface minimap.
- Deep Gate prefab/location registration: normal Jötunn registration around Magenheim content, not a replacement world/player lifecycle.

## Replace or reduce next — probable duplication

### Generated object persistence

`UnderworldGeneratedObjectStateStore` currently implements its own filesystem records, hashing, temporary files, backups, identity envelopes and generated-object admission. This is a high-risk duplication candidate. Valheim already has networked persistent world-object identity/state through ZDO/ZNetView and ordinary world persistence.

**Required revision:** determine whether Underworld instance objects can use normal ZDO/ZNetView persistence with an instance identity/coordinate adapter. If yes, delete the custom generated-object filesystem store and make `UnderworldStructureAdmissionController` a thin deterministic spawn/ZDO adapter. Do not build a second save engine.

### World context controller

`ValheimUnderworldWorldContextController` is retained only as a temporary instance adapter. Its identity/layer fields must not evolve into player state. Audit whether the instance adapter can expose its active identity directly through `UnderworldInstanceLifecycle`; if so, collapse the controller rather than maintaining two authorities.

### World-session lifecycle

`UnderworldWorldSessionLifecycle` should remain only a Valheim world-load/unload hook plus instance-content admission boundary. Any player recovery, player enumeration for world truth, custom session generation, or synthetic player-layer tracking is forbidden.

### Exploration persistence

Independent Underworld fog is a legitimate custom dataset because the Surface map is not the Underworld map. Its filesystem persistence is nevertheless an adapter choice, not sacred architecture. Prefer Valheim-supported player/custom-data persistence if it can safely store the independent fog payload. Keep the custom codec only if the engine does not provide an appropriate scoped storage surface.

## Valheim/Jötunn functionality we should use instead of recreating

- `Player` / `Character.TeleportTo` for actual player movement.
- Valheim character/player save authority for character state, inventory and skills.
- `ZNet` and existing peer/session authority for connected-player identity and network lifecycle.
- `ZNetView` / ZDO for ordinary persistent networked world objects wherever the instance adapter can provide correct ownership/coordinates.
- `ZoneSystem` / Jötunn `ZoneManager` and `CustomLocation` for ordinary Surface locations such as the entrance Deep Gate.
- Jötunn `PrefabManager` for prefab registration rather than custom prefab registries.
- Valheim inventory APIs for inventory mutation rather than replacement inventories.
- Valheim status effects/damage/skills where the desired mechanic is expressible as normal game content; Harmony patches remain justified only for mechanics with no supported registration/API seam.

## Deliverable rule

For every remaining subsystem, the audit question is:

1. What player-visible Magenheim behavior is required?
2. Which Valheim/Jötunn facility already performs the generic part?
3. What is the smallest Magenheim adapter needed for the new Underworld instance/terrain/content?
4. Delete everything between (2) and (3) that merely mirrors engine behavior.

No further architecture-hardening pass is allowed on a subsystem that fails this test.

## Immediate next audit order

1. Replace custom generated-object persistence with ZDO/ZNetView-backed instance object persistence if the runtime API supports the required instance scoping.
2. Collapse duplicate world-context/instance-lifecycle authority.
3. Audit custom exploration storage against Valheim player/custom-data storage.
4. Audit Underworld ecology/structure residency for places where normal Valheim/Jötunn prefab, spawn, and ZNetView behavior can replace custom lifecycle code.
5. Only then reconnect Deep Gate interaction as a thin transport adapter and proceed to terrain/biome/content deliverables.
