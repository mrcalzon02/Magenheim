# Geode World Prefab Registration Validation Boundary — 2026-09-14

## Implemented source

`src/Magenheim.Runtime/GeodeWorldPrefabRegistrar.cs` now creates one dedicated world-object prefab for every authoritative geode definition.

The world identity is derived only through `GeodeWorldPrefabIdentity.FromItemPrefabName`, so the current Meadows item/world pair remains:

- intact inventory item: `Magenheim_Geode_Meadows_Earth`
- mineable world object: `Magenheim_Geode_Meadows_Earth_World`

The runtime clones vanilla `Rock_4` only as the current world-object component/visual base. It never registers the temporary Stone-backed inventory item itself as vegetation.

## Admission invariants

Registration fails closed if any of these conditions are not satisfied:

- the world prefab identity is already occupied;
- the intact geode item prefab has not been registered first;
- the vanilla `Rock_4` clone cannot be created;
- the clone lacks `ZNetView`, `Destructible`, or `DropOnDestroyed`;
- required Valheim runtime fields are absent;
- the base destruction drop table is absent or has no reusable drop entry.

The cloned world prefab is configured as persistent network state, receives a small default destructible health/tool-tier contract, and its destruction drop table is reduced to exactly one guaranteed intact geode item. Existing vanilla `Rock_4` is never mutated.

Reflection is used only at the runtime adapter boundary for version-sensitive Valheim field access. Missing fields produce an explicit startup failure instead of silently registering an object with incorrect persistence or drops.

## Registration ordering

`MagenheimPlugin` subscribes `GeodeItemRegistrar` before `GeodeWorldPrefabRegistrar`. The world registrar requires `PrefabManager` to resolve the canonical intact item prefab so its drop table can point at that object. Both registrars unsubscribe after their one-time vanilla-prefab callback.

## What is not yet admitted

This source has not been compiled or executed in Valheim in the current automation environment. Therefore the following are not claimed:

- Jötunn 2.30.0 compile compatibility of the new registrar;
- current Valheim 1.0.x field compatibility for the reflected drop-table members;
- successful `ZNetScene` registration;
- actual mining/destruction behavior;
- exactly-one intact geode spawning in game;
- multiplayer persistence or save/load behavior;
- natural vegetation/world placement.

## Next exact gate

In a current Valheim/Jötunn development environment:

1. compile runtime 0.0.11 with warnings as errors;
2. start Valheim and verify `Magenheim_Geode_Meadows_Earth` and `Magenheim_Geode_Meadows_Earth_World` both register once;
3. spawn the `_World` prefab manually, destroy it with the intended early-game mining tool, and verify exactly one intact geode drops;
4. verify vanilla `Rock_4` still has its original components and drops;
5. verify host/client and dedicated-server persistence/destruction behavior;
6. only after this passes, bind approved `DefinitionWorldgenPlanner` additions through a thin Jötunn vegetation registrar and snapshot repeated-load registration state for idempotence.
