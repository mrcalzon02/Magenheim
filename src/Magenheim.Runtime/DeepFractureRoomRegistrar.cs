using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.DeepFractures;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers every Magenheim-owned Deep Fracture room identity during Jotunn's supported
/// OnVanillaRoomsAvailable boundary. Jotunn builds its custom-room hash table immediately after
/// this event, so these room identities participate in Valheim's persisted dungeon loading path.
/// Surface worldgen is not registered here.
/// </summary>
internal sealed class DeepFractureRoomRegistrar : IDisposable
{
    internal const string ThemeName = "MagenheimDeepFracture";

    private readonly ManualLogSource _log;
    private readonly List<string> _registeredRoomNames = new List<string>();
    private GameObject? _themeCarrier;
    private bool _subscribed;
    private bool _registered;

    internal DeepFractureRoomRegistrar(ManualLogSource log)
        => _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered)
            return;

        DungeonManager.OnVanillaRoomsAvailable += OnVanillaRoomsAvailable;
        _subscribed = true;
    }

    private void OnVanillaRoomsAvailable()
    {
        if (_registered)
            return;

        try
        {
            DeepFractureCatalog.ValidateDefaults();

            _themeCarrier = new GameObject("Magenheim_DF_ThemeCarrier");
            _themeCarrier.SetActive(false);
            _themeCarrier.AddComponent<DungeonGenerator>();
            if (!DungeonManager.Instance.RegisterDungeonTheme(_themeCarrier, ThemeName))
                throw new InvalidOperationException($"Jotunn refused Deep Fracture dungeon theme '{ThemeName}'.");

            foreach (var family in DeepFractureCatalog.PieceFamilies)
                AddRoom(DeepFractureRoomVisuals.CreateDistrictPrefab(family));

            AddRoom(DeepFractureRoomVisuals.CreatePassagePrefab());
            AddRoom(DeepFractureRoomVisuals.CreateTraversalNodePrefab());

            _registered = true;
            _log.LogInfo($"Registered Deep Fracture theme '{ThemeName}' with {_registeredRoomNames.Count} exact-plan room prefabs before Jotunn room hash generation.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Deep Fracture room registration failed: {exception}");
            throw;
        }
        finally
        {
            Unsubscribe();
        }
    }

    internal static DungeonDB.RoomData ResolveDistrict(string pieceFamilyId)
        => ResolveRoom(DeepFractureRoomVisuals.RoomPrefabName(pieceFamilyId));

    internal static DungeonDB.RoomData ResolvePassage()
        => ResolveRoom(DeepFractureRoomVisuals.PassagePrefabName);

    internal static DungeonDB.RoomData ResolveTraversalNode()
        => ResolveRoom(DeepFractureRoomVisuals.TraversalNodePrefabName);

    private void AddRoom(GameObject prefab)
    {
        if (prefab is null)
            throw new ArgumentNullException(nameof(prefab));
        if (CustomRoom.IsCustomRoom(prefab.name))
            throw new InvalidOperationException($"Deep Fracture room identity '{prefab.name}' is already occupied; Magenheim will not replace it.");

        var customRoom = new CustomRoom(
            prefab,
            fixReference: false,
            new RoomConfig(ThemeName)
            {
                Enabled = true,
                Entrance = false,
                Endcap = false,
                Divider = false,
                Weight = 1f,
            });

        if (!DungeonManager.Instance.AddCustomRoom(customRoom))
            throw new InvalidOperationException($"Jotunn refused Deep Fracture room '{prefab.name}'.");

        _registeredRoomNames.Add(prefab.name);
    }

    private static DungeonDB.RoomData ResolveRoom(string prefabName)
    {
        var room = DungeonManager.Instance.GetRoom(prefabName)
            ?? throw new InvalidOperationException($"Deep Fracture room '{prefabName}' is not registered in Jotunn's dungeon manager.");
        return room.RoomData
            ?? throw new InvalidOperationException($"Deep Fracture room '{prefabName}' has no RoomData authority.");
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;
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
