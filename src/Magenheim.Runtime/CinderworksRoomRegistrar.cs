using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class CinderworksRoomRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly List<string> _registeredRooms = new();
    private GameObject? _themeCarrier;
    private bool _subscribed;
    private bool _registered;

    internal CinderworksRoomRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        if (UnderworldDungeonCatalog.SulfurousWastes.Status !=
            UnderworldDungeonStatus.RuntimeReady)
        {
            _log.LogDebug(
                "Cinderworks room registration remains gated while the Sulfur dungeon is Planned.");
            return;
        }

        if (!CinderworksRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException(
                "Cinderworks are RuntimeReady but authored model payloads are missing: " +
                string.Join(", ", missing));

        DungeonManager.OnVanillaRoomsAvailable += RegisterRooms;
        _subscribed = true;
    }

    private void RegisterRooms()
    {
        if (_registered) return;
        try
        {
            UnderworldSulfurCinderworksCatalog.Validate();

            _themeCarrier = new GameObject("Magenheim_Cinderworks_ThemeCarrier");
            _themeCarrier.SetActive(false);
            _themeCarrier.AddComponent<DungeonGenerator>();
            if (!DungeonManager.Instance.RegisterDungeonTheme(
                    _themeCarrier,
                    CinderworksRoomVisuals.ThemeName))
                throw new InvalidOperationException(
                    "Jotunn refused Cinderworks dungeon theme '" +
                    CinderworksRoomVisuals.ThemeName + "'.");

            foreach (var definition in UnderworldSulfurCinderworksCatalog.Rooms)
                AddRoom(CinderworksRoomVisuals.CreateRoomPrefab(definition));
            AddRoom(CinderworksRoomVisuals.CreatePassagePrefab());

            _registered = true;
            _log.LogInfo(
                $"Registered Cinderworks theme '{CinderworksRoomVisuals.ThemeName}' with " +
                $"{_registeredRooms.Count} authored room/passage prefabs.");
        }
        catch (Exception exception)
        {
            _log.LogError("Cinderworks room registration failed: " + exception);
            throw;
        }
        finally
        {
            Unsubscribe();
        }
    }

    internal static Room ResolveRoom(string roomFamilyId)
    {
        var definition = UnderworldSulfurCinderworksCatalog.Require(roomFamilyId);
        return ResolvePrefab(CinderworksRoomVisuals.RoomPrefabName(definition));
    }

    internal static Room ResolvePassage() =>
        ResolvePrefab(CinderworksRoomVisuals.PassagePrefabName);

    private static Room ResolvePrefab(string prefabName)
    {
        var custom = DungeonManager.Instance.GetRoom(prefabName)
            ?? throw new InvalidOperationException(
                $"Cinderworks room '{prefabName}' is not registered.");
        return custom.Room
            ?? throw new InvalidOperationException(
                $"Cinderworks room '{prefabName}' has no loaded Room authority.");
    }

    private void AddRoom(GameObject prefab)
    {
        if (CustomRoom.IsCustomRoom(prefab.name))
            throw new InvalidOperationException(
                $"Occupied Cinderworks room identity '{prefab.name}'.");

        var room = new CustomRoom(
            prefab,
            fixReference: false,
            new RoomConfig(CinderworksRoomVisuals.ThemeName)
            {
                Enabled = true,
                Entrance = false,
                Endcap = false,
                Divider = false,
                Weight = 1f,
            });
        if (!DungeonManager.Instance.AddCustomRoom(room))
            throw new InvalidOperationException(
                $"Jotunn refused Cinderworks room '{prefab.name}'.");
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
