# Underworld dungeon native placement closure — 2026-09-29

## Goal

Prove that a dungeon marked RuntimeReady is not merely registered as a prefab: Valheim must
pregenerate actual location positions for it inside the owning Underworld biome, with the requested
quantity and same-family spacing, and old Underworld saves must receive newly admitted families.

## Source/runtime changes

- UnderworldWorldgenContentBridge now requires exactly one detached ZoneLocation row for every
  RuntimeReady dungeon and requires that row's biome mask to equal—not merely overlap—the canonical
  Magenheim Underworld biome bit. Planned dungeon rows in the detached catalog are an error.
- UnderworldDungeonPlacementRuntime is attached directly to the detached native Underworld
  ZoneSystem. It waits for Valheim's m_locationsGenerated completion flag and audits the real
  m_locationInstances position table.
- Every generated dungeon position is sampled back through UnderworldTerrainRuntime. A placement
  outside the playable Underworld or in a different canonical biome fails the audit.
- Actual generated count must equal the catalog Quantity. Same-family positions must preserve
  MinDistanceFromSimilarMeters.
- On the authoritative server, a saved Underworld that is missing a newly RuntimeReady family gets
  one recovery pass through Valheim's own GenerateLocationsTimeSliced coroutine. The existing
  world-instance iterator scope keeps that coroutine bound to the Underworld WorldGenerator,
  ZoneSystem and Unity scene across yields.
- If native placement still cannot reach the required quantity after that recovery pass, the
  dungeon placement state is Failed and the condition is logged explicitly rather than silently
  accepting a dungeonless world.
- magenheim_underworld dungeons reports the live placement state and each admitted family's
  expected/found count, owning biome and observed minimum spacing.

## Static compatibility gates

- verify-underworld-dungeon-worldgen.py covers all six dungeon programs from catalog registrar
  through native biome mapping and AddCustomLocation.
- verify-underworld-instance-architecture.py now requires the native placement reconciler and its
  m_locationInstances / biome / quantity / spacing contracts.
- verify-dynamic-patch-targets.ps1 forces UnderworldDungeonPlacementRuntime's static constructor
  against the installed game assembly and verifies m_locationsGenerated, m_locationInstances and
  the current three-argument GenerateLocationsTimeSliced signature.
- rebuild-underworld-production.ps1 runs the cheap dungeon worldgen contract before entering the
  Blender-heavy production forge.

## Current evidence boundary

The remote source path is committed and read-back verified. This execution did not launch Valheim,
so no claim is made yet about observed generated coordinates in a live save. At present only
RuntimeReady dungeon families participate; Planned asset-gated families continue to be deliberately
absent until their real payloads are forged and promoted.

Live closure requires the TESTING.md fresh-world and existing-save dungeon placement passes.
