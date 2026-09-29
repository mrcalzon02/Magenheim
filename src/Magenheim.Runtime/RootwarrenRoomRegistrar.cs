using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class RootwarrenRoomRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly List<string> _registeredRooms = new();
    private GameObject? _themeCarrier;
    private bool _subscribed;
    private bool _registered;

    internal RootwarrenRoomRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        if (UnderworldDungeonCatalog.FungalForest.Status != UnderworldDungeonStatus.RuntimeReady)
        {
            _log.LogDebug("Rootwarren room registration remains gated while the Fungal dungeon is Planned.");
            return;
        }

        if (!RootwarrenRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException(
                "Rootwarren is RuntimeReady but authored model payloads are missing: " +
                string.Join(", ", missing));

        DungeonManager.OnVanillaRoomsAvailable += RegisterRooms;
        _subscribed = true;
    }

    private void RegisterRooms()
    {
        if (_registered) return;
        try
        {
            UnderworldFungalRootwarrenCatalog.Validate();
            _themeCarrier = new GameObject("Magenheim_Rootwarren_ThemeCarrier");
            _themeCarrier.SetActive(false);
            _themeCarrier.AddComponent<DungeonGenerator>();
            if (!DungeonManager.Instance.RegisterDungeonTheme(
                    _themeCarrier,
                    RootwarrenRoomVisuals.ThemeName))
                throw new InvalidOperationException(
                    "Jotunn refused Rootwarren dungeon theme '" +
                    RootwarrenRoomVisuals.ThemeName + "'.");

            foreach (var definition in UnderworldFungalRootwarrenCatalog.Rooms)
                AddRoom(RootwarrenRoomVisuals.CreateRoomPrefab(definition));
            AddRoom(RootwarrenRoomVisuals.CreatePassagePrefab());

            _registered = true;
            _log.LogInfo(
                $"Registered Rootwarren theme '{RootwarrenRoomVisuals.ThemeName}' with " +
                $"{_registeredRooms.Count} authored room/passage prefabs.");
        }
        catch (Exception exception)
        {
            _log.LogError("Rootwarren room registration failed: " + exception);
            throw;
        }
        finally
        {
            Unsubscribe();
        }
    }

    internal static Room ResolveRoom(string roomFamilyId)
    {
        var definition = FindDefinition(roomFamilyId);
        return ResolvePrefab(RootwarrenRoomVisuals.RoomPrefabName(definition));
    }

    internal static Room ResolvePassage() =>
        ResolvePrefab(RootwarrenRoomVisuals.PassagePrefabName);

    private static UnderworldDungeonRoomDefinition FindDefinition(string id)
    {
        foreach (var definition in UnderworldFungalRootwarrenCatalog.Rooms)
            if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                return definition;
        throw new InvalidOperationException(
            $"Unknown Rootwarren room family '{id}'.");
    }

    private static Room ResolvePrefab(string prefabName)
    {
        var custom = DungeonManager.Instance.GetRoom(prefabName)
            ?? throw new InvalidOperationException(
                $"Rootwarren room '{prefabName}' is not registered.");
        return custom.Room
            ?? throw new InvalidOperationException(
                $"Rootwarren room '{prefabName}' has no loaded Room authority.");
    }

    private void AddRoom(GameObject prefab)
    {
        if (CustomRoom.IsCustomRoom(prefab.name))
            throw new InvalidOperationException(
                $"Occupied Rootwarren room identity '{prefab.name}'.");
        var room = new CustomRoom(
            prefab,
            fixReference: false,
            new RoomConfig(RootwarrenRoomVisuals.ThemeName)
            {
                Enabled = true,
                Entrance = false,
                Endcap = false,
                Divider = false,
                Weight = 1f,
            });
        if (!DungeonManager.Instance.AddCustomRoom(room))
            throw new InvalidOperationException(
                $"Jotunn refused Rootwarren room '{prefab.name}'.");
        _registeredRooms.Add(prefab.name);
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        DungeonManager.OnVanillaRoomsAvailable -= RegisterRooms;
        _subscribed = false;
    }

    public void Dispose()
    {
        Unsubscribe();
        foreach (var name in _registeredRooms)
            DungeonManager.Instance.RemoveRoom(name);
        _registeredRooms.Clear();
        if (_themeCarrier is not null)
        {
            UnityEngine.Object.Destroy(_themeCarrier);
            _themeCarrier = null;
        }
        _registered = false;
    }
}
