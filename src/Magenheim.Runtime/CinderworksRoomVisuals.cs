using System;
using System.Collections.Generic;
using System.IO;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CinderworksRoomVisuals
{
    internal const string ThemeName = "MagenheimCinderworks";
    internal const string PassagePrefabName = "Magenheim_Cinderworks_Passage";
    internal const string PassageModelId = "underworld-dungeon-sulfur-cinderworks-passage";
    internal const float PassageLengthMeters = 16f;

    internal static string RoomPrefabName(UnderworldCinderworksRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();
        var suffix = definition.Room.Id.Substring(
            UnderworldSulfurCinderworksCatalog.RoomIdPrefix.Length);
        return "Magenheim_Cinderworks_" + suffix.Replace('-', '_');
    }

    internal static GameObject CreateRoomPrefab(
        UnderworldCinderworksRoomDefinition definition)
    {
        definition.Validate();
        var roomDefinition = definition.Room;
        var root = CreateRoomRoot(
            RoomPrefabName(definition),
            new Vector3Int(
                Mathf.CeilToInt((float)roomDefinition.WidthMeters),
                Mathf.CeilToInt((float)roomDefinition.HeightMeters),
                Mathf.CeilToInt((float)roomDefinition.DepthMeters)));

        ModelAssets.Load(root, roomDefinition.ModelId, hideOriginal: false);
        CinderworksThermalRuntime.Attach(root, definition);
        return root;
    }

    internal static GameObject CreatePassagePrefab()
    {
        var root = CreateRoomRoot(
            PassagePrefabName,
            new Vector3Int(14, 14, 16));
        ModelAssets.Load(root, PassageModelId, hideOriginal: false);
        return root;
    }

    internal static bool TryGetMissingPayloads(
        out IReadOnlyList<string> missing)
    {
        var result = new List<string>();
        foreach (var definition in UnderworldSulfurCinderworksCatalog.Rooms)
            if (!HasModel(definition.Room.ModelId))
                result.Add(definition.Room.ModelId);
        if (!HasModel(PassageModelId))
            result.Add(PassageModelId);
        missing = result.AsReadOnly();
        return result.Count == 0;
    }

    private static bool HasModel(string modelId)
    {
        var directory = Path.GetDirectoryName(
            typeof(CinderworksRoomVisuals).Assembly.Location);
        return !string.IsNullOrEmpty(directory) &&
               File.Exists(Path.Combine(
                   directory,
                   "assets",
                   "models",
                   "runtime",
                   modelId + ".model.json"));
    }

    private static GameObject CreateRoomRoot(
        string name,
        Vector3Int size)
    {
        var root = new GameObject(name);
        root.SetActive(false);
        var room = root.AddComponent<Room>();
        room.m_enabled = true;
        room.m_size = size;
        return root;
    }
}
