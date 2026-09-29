using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the authored Rootwarren room templates only after the complete model payload set exists.
/// Planned catalog status is not a substitute for asset readiness.
/// </summary>
internal sealed class UnderworldFungalRootwarrenRoomRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly List<string> _registeredRoomNames = new();
    private GameObject? _themeCarrier;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldFungalRootwarrenRoomRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        DungeonManager.OnVanillaRoomsAvailable += OnVanillaRoomsAvailable;
        _subscribed = true;
    }

    private void OnVanillaRoomsAvailable()
    {
        if (_registered) return;
        try
        {
            UnderworldFungalRootwarrenCatalog.Validate();
            if (!UnderworldFungalRootwarrenRoomVisuals.HasCompleteAssetSet(out var missing))
                throw new InvalidOperationException(
                    "Rootwarren room registration refused because authored model payload is missing: " +
                    missing);

            _themeCarrier = new GameObject("Magenheim_Underworld_FungalRootwarren_ThemeCarrier");
            _themeCarrier.SetActive(false);
            _themeCarrier.AddComponent<DungeonGenerator>();
            if (!DungeonManager.Instance.RegisterDungeonTheme(
                    _themeCarrier,
                    UnderworldFungalRootwarrenRoomVisuals.ThemeName))
                throw new InvalidOperationException(
                    "Jotunn refused Rootwarren dungeon theme '" +
                    UnderworldFungalRootwarrenRoomVisuals.ThemeName + "'.");

            foreach (var room in UnderworldFungalRootwarrenCatalog.Rooms)
                AddRoom(UnderworldFungalRootwarrenRoomVisuals.CreateRoomPrefab(room));
            AddRoom(UnderworldFungalRootwarrenRoomVisuals.CreatePassagePrefab());

            _registered = true;
            _log.LogInfo(
                "Registered Fungal Rootwarren theme with " +
                _registeredRoomNames.Count +
                " authored room/passage templates.");
        }
        catch (Exception exception)
        {
            _log.LogError("Fungal Rootwarren room registration failed: " + exception);
            throw;
        }
        finally
        {
            Unsubscribe();
        }
    }

    internal static Room ResolveRoom(string roomFamilyId)
    {
        foreach (var definition in UnderworldFungalRootwarrenCatalog.Rooms)
            if (string.Equals(definition.Id, roomFamilyId, StringComparison.Ordinal))
                return Resolve(
                    UnderworldFungalRootwarrenRoomVisuals.RoomPrefabName(definition));
        throw new InvalidOperationException(
            "Unknown Rootwarren room family '" + roomFamilyId + "'.");
    }

    internal static Room ResolvePassage() =>
        Resolve(UnderworldFungalRootwarrenRoomVisuals.PassagePrefabName);

    private void AddRoom(GameObject prefab)
    {
        if (CustomRoom.IsCustomRoom(prefab.name))
            throw new InvalidOperationException(
                "Rootwarren room identity is already occupied: " + prefab.name);
        var custom = new CustomRoom(
            prefab,
            fixReference: false,
            new RoomConfig(UnderworldFungalRootwarrenRoomVisuals.ThemeName)
            {
                Enabled = true,
                Entrance = false,
                Endcap = false,
                Divider = false,
                Weight = 1f,
            });
        if (!DungeonManager.Instance.AddCustomRoom(custom))
            throw new InvalidOperationException(
                "Jotunn refused Rootwarren room '" + prefab.name + "'.");
        _registeredRoomNames.Add(prefab.name);
    }

    private static Room Resolve(string prefabName)
    {
        var custom = DungeonManager.Instance.GetRoom(prefabName)
            ?? throw new InvalidOperationException(
                "Rootwarren room '" + prefabName + "' is not registered.");
        return custom.Room
            ?? throw new InvalidOperationException(
                "Rootwarren room '" + prefabName + "' has no Room prefab.");
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        DungeonManager.OnVanillaRoomsAvailable -= OnVanillaRoomsAvailable;
        _subscribed = false;
    }

    public void Dispose()
    {
        Unsubscribe();
        foreach (var name in _registeredRoomNames)
            DungeonManager.Instance.RemoveRoom(name);
        _registeredRoomNames.Clear();
        if (_themeCarrier is not null)
        {
            UnityEngine.Object.Destroy(_themeCarrier);
            _themeCarrier = null;
        }
        _registered = false;
    }
}
