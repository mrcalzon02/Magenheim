using System;
using System.IO;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class UnderworldFungalRootwarrenRoomVisuals
{
    internal const string ThemeName = "MagenheimUnderworldFungalRootwarren";
    internal const string PassageModelId = "underworld-dungeon-fungal-rootwarren-passage";
    internal const string PassagePrefabName = "Magenheim_Underworld_Dungeon_FungalRootwarren_Passage";

    internal static string RoomPrefabName(UnderworldDungeonRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate(
            UnderworldFungalRootwarrenCatalog.RoomIdPrefix,
            UnderworldFungalRootwarrenCatalog.ModelIdPrefix);
        var suffix = definition.Id.Substring(UnderworldFungalRootwarrenCatalog.RoomIdPrefix.Length);
        return "Magenheim_Underworld_Dungeon_FungalRootwarren_Room_" +
               suffix.Replace('-', '_');
    }

    internal static bool HasCompleteAssetSet(out string missing)
    {
        foreach (var room in UnderworldFungalRootwarrenCatalog.Rooms)
        {
            if (!HasModel(room.ModelId))
            {
                missing = room.ModelId;
                return false;
            }
        }

        if (!HasModel(PassageModelId))
        {
            missing = PassageModelId;
            return false;
        }

        missing = string.Empty;
        return true;
    }

    internal static GameObject CreateRoomPrefab(UnderworldDungeonRoomDefinition definition)
    {
        if (!HasModel(definition.ModelId))
            throw new FileNotFoundException(
                "Rootwarren room payload is required before runtime admission.",
                definition.ModelId + ".model.json");

        var root = CreateRoomRoot(
            RoomPrefabName(definition),
            new Vector3Int(
                Ceiling(definition.WidthMeters),
                Ceiling(definition.HeightMeters),
                Ceiling(definition.DepthMeters)));
        ModelAssets.Load(root, definition.ModelId, hideOriginal: false);
        root.AddComponent<UnderworldFungalRootwarrenRoomMarker>().Bind(
            definition.Id,
            definition.Role);
        return root;
    }

    internal static GameObject CreatePassagePrefab()
    {
        if (!HasModel(PassageModelId))
            throw new FileNotFoundException(
                "Rootwarren passage payload is required before runtime admission.",
                PassageModelId + ".model.json");

        var root = CreateRoomRoot(PassagePrefabName, new Vector3Int(12, 12, 16));
        ModelAssets.Load(root, PassageModelId, hideOriginal: false);
        return root;
    }

    private static GameObject CreateRoomRoot(string name, Vector3Int size)
    {
        var root = new GameObject(name);
        root.SetActive(false);
        var room = root.AddComponent<Room>();
        room.m_enabled = true;
        room.m_size = size;
        return root;
    }

    private static int Ceiling(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) ||
            value <= 0d || value > int.MaxValue)
            throw new InvalidOperationException(
                "Rootwarren room dimension cannot be represented by Valheim.");
        return checked((int)Math.Ceiling(value));
    }

    private static bool HasModel(string modelId)
    {
        var directory = Path.GetDirectoryName(
            typeof(UnderworldFungalRootwarrenRoomVisuals).Assembly.Location);
        return !string.IsNullOrEmpty(directory) &&
               File.Exists(Path.Combine(
                   directory,
                   "assets",
                   "models",
                   "runtime",
                   modelId + ".model.json"));
    }
}

internal sealed class UnderworldFungalRootwarrenRoomMarker : MonoBehaviour
{
    [SerializeField] private string _roomFamilyId = string.Empty;
    [SerializeField] private UnderworldDungeonRoomRole _role;

    internal string RoomFamilyId => _roomFamilyId;
    internal UnderworldDungeonRoomRole Role => _role;

    internal void Bind(string roomFamilyId, UnderworldDungeonRoomRole role)
    {
        if (string.IsNullOrWhiteSpace(roomFamilyId))
            throw new ArgumentException(
                "Rootwarren room family id is required.",
                nameof(roomFamilyId));
        _roomFamilyId = roomFamilyId;
        _role = role;
    }
}
