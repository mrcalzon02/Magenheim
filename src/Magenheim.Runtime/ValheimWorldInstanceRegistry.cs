using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime authority for native Valheim world instances that coexist inside one save/server.
/// Each instance owns its own Valheim world-service bundle. Coordinates never determine identity.
/// </summary>
internal sealed class ValheimWorldInstanceRegistry
{
    private readonly Dictionary<UnderworldWorldInstanceId, ValheimWorldInstanceContext> _contexts = new();
    private readonly Dictionary<long, UnderworldWorldInstanceId> _players = new();

    internal void BindSurface(World world, WorldGenerator generator, ZoneSystem zoneSystem, ZDOMan zdoMan)
    {
        var scene = zoneSystem.gameObject.scene;
        Bind(new ValheimWorldInstanceContext(UnderworldWorldInstanceId.Surface, world, generator, zoneSystem, zdoMan, scene, scene.GetPhysicsScene()));
    }

    internal void BindUnderworld(
        World world,
        WorldGenerator generator,
        ZoneSystem zoneSystem,
        ZDOMan zdoMan,
        Scene scene,
        PhysicsScene physicsScene)
    {
        Bind(new ValheimWorldInstanceContext(
            UnderworldWorldInstanceId.Underworld,
            world,
            generator,
            zoneSystem,
            zdoMan,
            scene,
            physicsScene));
    }

    internal void Bind(ValheimWorldInstanceContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        _contexts[context.InstanceId] = context;
    }

    internal bool TryGetContext(UnderworldWorldInstanceId instanceId, out ValheimWorldInstanceContext? context) =>
        _contexts.TryGetValue(instanceId, out context);

    internal bool TryGetContextForScene(int sceneHandle, out ValheimWorldInstanceContext? context)
    {
        foreach (var candidate in _contexts.Values)
        {
            if (candidate.Scene.handle != sceneHandle) continue;
            context = candidate;
            return true;
        }
        context = null;
        return false;
    }

    internal bool HasUnderworldContext => _contexts.ContainsKey(UnderworldWorldInstanceId.Underworld);

    internal void BindPlayer(long playerId, UnderworldWorldInstanceId instanceId)
    {
        if (!_contexts.ContainsKey(instanceId))
            throw new InvalidOperationException($"World instance {instanceId} is not bound to native Valheim services.");
        _players[playerId] = instanceId;
    }

    internal UnderworldWorldInstanceId GetOrBindSurface(long playerId)
    {
        if (_players.TryGetValue(playerId, out var instanceId)) return instanceId;
        if (!_contexts.ContainsKey(UnderworldWorldInstanceId.Surface))
            throw new InvalidOperationException("Surface world instance is not bound to native Valheim services.");
        _players[playerId] = UnderworldWorldInstanceId.Surface;
        return UnderworldWorldInstanceId.Surface;
    }

    internal bool TryGetPlayerInstance(long playerId, out UnderworldWorldInstanceId instanceId) =>
        _players.TryGetValue(playerId, out instanceId);

    internal void MovePlayer(long playerId, UnderworldWorldInstanceId target)
    {
        if (!_contexts.ContainsKey(target))
            throw new InvalidOperationException($"World instance {target} is not bound to native Valheim services.");
        _players[playerId] = target;
    }

    internal void ClearPlayer(long playerId) => _players.Remove(playerId);

    internal void Reset()
    {
        _players.Clear();
        _contexts.Clear();
    }
}

internal sealed class ValheimWorldInstanceContext
{
    internal ValheimWorldInstanceContext(
        UnderworldWorldInstanceId instanceId,
        World world,
        WorldGenerator worldGenerator,
        ZoneSystem zoneSystem,
        ZDOMan zdoMan,
        Scene scene,
        PhysicsScene physicsScene)
    {
        if (!scene.IsValid()) throw new ArgumentException("World instance requires a valid Unity scene.", nameof(scene));
        if (!physicsScene.IsValid()) throw new ArgumentException("World instance requires a valid Unity physics scene.", nameof(physicsScene));
        InstanceId = instanceId;
        World = world ?? throw new ArgumentNullException(nameof(world));
        WorldGenerator = worldGenerator ?? throw new ArgumentNullException(nameof(worldGenerator));
        ZoneSystem = zoneSystem ?? throw new ArgumentNullException(nameof(zoneSystem));
        ZdoMan = zdoMan ?? throw new ArgumentNullException(nameof(zdoMan));
        Scene = scene;
        PhysicsScene = physicsScene;
    }

    internal UnderworldWorldInstanceId InstanceId { get; }
    internal World World { get; }
    internal WorldGenerator WorldGenerator { get; }
    internal ZoneSystem ZoneSystem { get; }
    internal ZDOMan ZdoMan { get; }
    internal Scene Scene { get; }
    internal PhysicsScene PhysicsScene { get; }
}
