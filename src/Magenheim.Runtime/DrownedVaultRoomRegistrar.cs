using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class DrownedVaultRoomRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly List<string> _registeredRooms = new();
    private GameObject? _themeCarrier;
    private bool _subscribed;
    private bool _registered;

    internal DrownedVaultRoomRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        if (UnderworldDungeonCatalog.BlackwaterDeep.Status != UnderworldDungeonStatus.RuntimeReady)
        {
            _log.LogDebug(
                "Drowned Vault room registration remains gated while the Blackwater dungeon is Planned.");
            return;
        }

        if (!DrownedVaultRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException(
                "Drowned Vaults are RuntimeReady but authored model payloads are missing: " +
                string.Join(", ", missing));

        DungeonManager.OnVanillaRoomsAvailable += RegisterRooms;
        _subscribed = true;
    }

    private void RegisterRooms()
    {
        if (_registered) return;
        try
        {
            UnderworldBlackwaterDrownedVaultsCatalog.Validate();
            DrownedVaultWaterRuntime.ValidateDonor();

            _themeCarrier = new GameObject("Magenheim_DrownedVaults_ThemeCarrier");
            _themeCarrier.SetActive(false);
            _themeCarrier.AddComponent<DungeonGenerator>();
            if (!DungeonManager.Instance.RegisterDungeonTheme(
                    _themeCarrier,
                    DrownedVaultRoomVisuals.ThemeName))
                throw new InvalidOperationException(
                    "Jotunn refused Drowned Vault dungeon theme '" +
                    DrownedVaultRoomVisuals.ThemeName + "'.");

            foreach (var definition in UnderworldBlackwaterDrownedVaultsCatalog.Rooms)
                AddRoom(DrownedVaultRoomVisuals.CreateRoomPrefab(definition));
            AddRoom(DrownedVaultRoomVisuals.CreatePassagePrefab());

            _registered = true;
            _log.LogInfo(
                $"Registered Drowned Vault theme '{DrownedVaultRoomVisuals.ThemeName}' with " +
                $"{_registeredRooms.Count} authored room/passage prefabs using native WaterVolume rooms.");
        }
        catch (Exception exception)
        {
            _log.LogError("Drowned Vault room registration failed: " + exception);
            throw;
        }
        finally
        {
            Unsubscribe();
        }
    }

    internal static Room ResolveRoom(string roomFamilyId)
    {
        var definition = UnderworldBlackwaterDrownedVaultsCatalog.Require(roomFamilyId);
        return ResolvePrefab(DrownedVaultRoomVisuals.RoomPrefabName(definition));
    }

    internal static Room ResolvePassage() =>
        ResolvePrefab(DrownedVaultRoomVisuals.PassagePrefabName);

    private static Room ResolvePrefab(string prefabName)
    {
        var custom = DungeonManager.Instance.GetRoom(prefabName)
            ?? throw new InvalidOperationException(
                $"Drowned Vault room '{prefabName}' is not registered.");
        return custom.Room
            ?? throw new InvalidOperationException(
                $"Drowned Vault room '{prefabName}' has no loaded Room authority.");
    }

    private void AddRoom(GameObject prefab)
    {
        if (CustomRoom.IsCustomRoom(prefab.name))
            throw new InvalidOperationException(
                $"Occupied Drowned Vault room identity '{prefab.name}'.");
        var room = new CustomRoom(
            prefab,
            fixReference: false,
            new RoomConfig(DrownedVaultRoomVisuals.ThemeName)
            {
                Enabled = true,
                Entrance = false,
                Endcap = false,
                Divider = false,
                Weight = 1f,
            });
        if (!DungeonManager.Instance.AddCustomRoom(room))
            throw new InvalidOperationException(
                $"Jotunn refused Drowned Vault room '{prefab.name}'.");
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
