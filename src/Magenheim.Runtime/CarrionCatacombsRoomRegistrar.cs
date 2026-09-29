using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class CarrionCatacombsRoomRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly List<string> _registeredRooms=new();
    private GameObject? _themeCarrier;
    private bool _subscribed;
    private bool _registered;

    internal CarrionCatacombsRoomRegistrar(ManualLogSource log)=>
        _log=log??throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if(_subscribed||_registered)return;
        if(UnderworldDungeonCatalog.GreatDecay.Status!=UnderworldDungeonStatus.RuntimeReady)
        {
            _log.LogDebug(
                "Carrion Catacombs room registration remains gated while Great Decay is Planned.");
            return;
        }
        if(!CarrionCatacombsRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException(
                "Carrion Catacombs are RuntimeReady but authored model payloads are missing: "+
                string.Join(", ",missing));

        DungeonManager.OnVanillaRoomsAvailable+=RegisterRooms;
        _subscribed=true;
    }

    private void RegisterRooms()
    {
        if(_registered)return;
        try
        {
            UnderworldGreatDecayCarrionCatacombsCatalog.Validate();
            _themeCarrier=new GameObject("Magenheim_CarrionCatacombs_ThemeCarrier");
            _themeCarrier.SetActive(false);
            _themeCarrier.AddComponent<DungeonGenerator>();
            if(!DungeonManager.Instance.RegisterDungeonTheme(
                   _themeCarrier,CarrionCatacombsRoomVisuals.ThemeName))
                throw new InvalidOperationException(
                    "Jotunn refused Carrion Catacombs dungeon theme '"+
                    CarrionCatacombsRoomVisuals.ThemeName+"'.");

            foreach(var definition in UnderworldGreatDecayCarrionCatacombsCatalog.Rooms)
                AddRoom(CarrionCatacombsRoomVisuals.CreateRoomPrefab(definition));
            AddRoom(CarrionCatacombsRoomVisuals.CreatePassagePrefab());

            _registered=true;
            _log.LogInfo(
                $"Registered Carrion Catacombs theme '{CarrionCatacombsRoomVisuals.ThemeName}' "+
                $"with {_registeredRooms.Count} authored room/passage prefabs.");
        }
        catch(Exception exception)
        {
            _log.LogError("Carrion Catacombs room registration failed: "+exception);
            throw;
        }
        finally{Unsubscribe();}
    }

    internal static Room ResolveRoom(string roomFamilyId)
    {
        var definition=UnderworldGreatDecayCarrionCatacombsCatalog.Require(roomFamilyId);
        return ResolvePrefab(CarrionCatacombsRoomVisuals.RoomPrefabName(definition));
    }

    internal static Room ResolvePassage()=>
        ResolvePrefab(CarrionCatacombsRoomVisuals.PassagePrefabName);

    private static Room ResolvePrefab(string prefabName)
    {
        var custom=DungeonManager.Instance.GetRoom(prefabName)
            ??throw new InvalidOperationException(
                $"Carrion Catacombs room '{prefabName}' is not registered.");
        return custom.Room
            ??throw new InvalidOperationException(
                $"Carrion Catacombs room '{prefabName}' has no loaded Room authority.");
    }

    private void AddRoom(GameObject prefab)
    {
        if(CustomRoom.IsCustomRoom(prefab.name))
            throw new InvalidOperationException(
                $"Occupied Carrion Catacombs room identity '{prefab.name}'.");

        var room=new CustomRoom(
            prefab,fixReference:false,
            new RoomConfig(CarrionCatacombsRoomVisuals.ThemeName)
            {
                Enabled=true,Entrance=false,Endcap=false,Divider=false,Weight=1f,
            });
        if(!DungeonManager.Instance.AddCustomRoom(room))
            throw new InvalidOperationException(
                $"Jotunn refused Carrion Catacombs room '{prefab.name}'.");
        _registeredRooms.Add(prefab.name);
    }

    private void Unsubscribe()
    {
        if(!_subscribed)return;
        DungeonManager.OnVanillaRoomsAvailable-=RegisterRooms;
        _subscribed=false;
    }

    public void Dispose()
    {
        Unsubscribe();
        foreach(var name in _registeredRooms)DungeonManager.Instance.RemoveRoom(name);
        _registeredRooms.Clear();
        if(_themeCarrier is not null)
        {
            UnityEngine.Object.Destroy(_themeCarrier);
            _themeCarrier=null;
        }
        _registered=false;
    }
}
