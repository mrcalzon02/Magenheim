using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class RimeSepulcherRoomRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly List<string> _registeredRooms = new();
    private GameObject? _themeCarrier;
    private bool _subscribed;
    private bool _registered;

    internal RimeSepulcherRoomRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        if (UnderworldDungeonCatalog.FrozenCaverns.Status != UnderworldDungeonStatus.RuntimeReady)
        {
            _log.LogDebug(
                "Rime Sepulcher room registration remains gated while the Frozen dungeon is Planned.");
            return;
        }

        if (!RimeSepulcherRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException(
                "Rime Sepulcher is RuntimeReady but authored model payloads are missing: " +
                string.Join(", ", missing));

        DungeonManager.OnVanillaRoomsAvailable += RegisterRooms;
        _subscribed = true;
    }

    private void RegisterRooms()
    {
        if (_registered) return;
        try
        {
            UnderworldFrozenRimeSepulcherCatalog.Validate();

            _themeCarrier = new GameObject("Magenheim_RimeSepulcher_ThemeCarrier");
            _themeCarrier.SetActive(false);
            _themeCarrier.AddComponent<DungeonGenerator>();
            if (!DungeonManager.Instance.RegisterDungeonTheme(
                    _themeCarrier,
                    RimeSepulcherRoomVisuals.ThemeName))
                throw new InvalidOperationException(
                    "Jotunn refused Rime Sepulcher dungeon theme '" +
                    RimeSepulcherRoomVisuals.ThemeName + "'.");

            foreach (var definition in UnderworldFrozenRimeSepulcherCatalog.Rooms)
                AddRoom(RimeSepulcherRoomVisuals.CreateRoomPrefab(definition));
            AddRoom(RimeSepulcherRoomVisuals.CreatePassagePrefab());

            _registered = true;
            _log.LogInfo(
                $"Registered Rime Sepulcher theme '{RimeSepulcherRoomVisuals.ThemeName}' with " +
                $"{_registeredRooms.Count} authored room/passage prefabs.");
        }
        catch (Exception exception)
        {
            _log.LogError("Rime Sepulcher room registration failed: " + exception);
            throw;
        }
        finally
        {
            Unsubscribe();
        }
    }

    internal static Room ResolveRoom(string roomFamilyId)
    {
        var definition = UnderworldFrozenRimeSepulcherCatalog.Require(roomFamilyId);
        return ResolvePrefab(RimeSepulcherRoomVisuals.RoomPrefabName(definition));
    }

    internal static Room ResolvePassage() =>
        ResolvePrefab(RimeSepulcherRoomVisuals.PassagePrefabName);

    private static Room ResolvePrefab(string prefabName)
    {
        var custom = DungeonManager.Instance.GetRoom(prefabName)
            ?? throw new InvalidOperationException(
                $"Rime Sepulcher room '{prefabName}' is not registered.");
        return custom.Room
            ?? throw new InvalidOperationException(
                $"Rime Sepulcher room '{prefabName}' has no loaded Room authority.");
    }

    private void AddRoom(GameObject prefab)
    {
        if (CustomRoom.IsCustomRoom(prefab.name))
            throw new InvalidOperationException(
                $"Occupied Rime Sepulcher room identity '{prefab.name}'.");

        var room = new CustomRoom(
            prefab,
            fixReference: false,
            new RoomConfig(RimeSepulcherRoomVisuals.ThemeName)
            {
                Enabled = true,
                Entrance = false,
                Endcap = false,
                Divider = false,
                Weight = 1f,
            });
        if (!DungeonManager.Instance.AddCustomRoom(room))
            throw new InvalidOperationException(
                $"Jotunn refused Rime Sepulcher room '{prefab.name}'.");
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
