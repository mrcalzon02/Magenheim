using System;
using System.Collections.Generic;
using System.IO;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class RootwarrenRoomVisuals
{
    internal const string ThemeName = "MagenheimRootwarren";
    internal const string PassagePrefabName = "Magenheim_Rootwarren_Passage";
    internal const string PassageModelId = "underworld-dungeon-fungal-rootwarren-passage";
    internal const float PassageLengthMeters = 16f;

    internal static string RoomPrefabName(UnderworldDungeonRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate(
            UnderworldFungalRootwarrenCatalog.RoomIdPrefix,
            UnderworldFungalRootwarrenCatalog.ModelIdPrefix);
        var suffix = definition.Id.Substring(UnderworldFungalRootwarrenCatalog.RoomIdPrefix.Length);
        return "Magenheim_Rootwarren_" + suffix.Replace('-', '_');
    }

    internal static GameObject CreateRoomPrefab(UnderworldDungeonRoomDefinition definition)
    {
        var root = CreateRoomRoot(
            RoomPrefabName(definition),
            new Vector3Int(
                Mathf.CeilToInt((float)definition.WidthMeters),
                Mathf.CeilToInt((float)definition.HeightMeters),
                Mathf.CeilToInt((float)definition.DepthMeters)));
        ModelAssets.Load(root, definition.ModelId, hideOriginal: false);
        return root;
    }

    internal static GameObject CreatePassagePrefab()
    {
        var root = CreateRoomRoot(PassagePrefabName, new Vector3Int(12, 12, 16));
        ModelAssets.Load(root, PassageModelId, hideOriginal: false);
        return root;
    }

    internal static bool TryGetMissingPayloads(out IReadOnlyList<string> missing)
    {
        var result = new List<string>();
        foreach (var room in UnderworldFungalRootwarrenCatalog.Rooms)
            if (!HasModel(room.ModelId))
                result.Add(room.ModelId);
        if (!HasModel(PassageModelId))
            result.Add(PassageModelId);
        missing = result.AsReadOnly();
        return result.Count == 0;
    }

    private static bool HasModel(string modelId)
    {
        var directory = Path.GetDirectoryName(typeof(RootwarrenRoomVisuals).Assembly.Location);
        return !string.IsNullOrEmpty(directory) &&
               File.Exists(Path.Combine(
                   directory, "assets", "models", "runtime", modelId + ".model.json"));
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
}
