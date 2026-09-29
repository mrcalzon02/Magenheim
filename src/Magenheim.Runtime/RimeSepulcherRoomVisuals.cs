using System;
using System.Collections.Generic;
using System.IO;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class RimeSepulcherRoomVisuals
{
    internal const string ThemeName = "MagenheimRimeSepulcher";
    internal const string PassagePrefabName = "Magenheim_RimeSepulcher_Passage";
    internal const string PassageModelId = "underworld-dungeon-frozen-rime-sepulcher-passage";
    internal const float PassageLengthMeters = 16f;

    internal static string RoomPrefabName(UnderworldRimeSepulcherRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();
        var suffix = definition.Room.Id.Substring(
            UnderworldFrozenRimeSepulcherCatalog.RoomIdPrefix.Length);
        return "Magenheim_RimeSepulcher_" + suffix.Replace('-', '_');
    }

    internal static GameObject CreateRoomPrefab(UnderworldRimeSepulcherRoomDefinition definition)
    {
        definition.Validate();
        var room = definition.Room;
        var root = CreateRoomRoot(
            RoomPrefabName(definition),
            new Vector3Int(
                Mathf.CeilToInt((float)room.WidthMeters),
                Mathf.CeilToInt((float)room.HeightMeters),
                Mathf.CeilToInt((float)room.DepthMeters)));
        ModelAssets.Load(root, room.ModelId, hideOriginal: false);
        RimeSepulcherExposureRuntime.Attach(root, definition);
        return root;
    }

    internal static GameObject CreatePassagePrefab()
    {
        var root = CreateRoomRoot(PassagePrefabName, new Vector3Int(14, 14, 16));
        ModelAssets.Load(root, PassageModelId, hideOriginal: false);
        return root;
    }

    internal static bool TryGetMissingPayloads(out IReadOnlyList<string> missing)
    {
        var result = new List<string>();
        foreach (var definition in UnderworldFrozenRimeSepulcherCatalog.Rooms)
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
            typeof(RimeSepulcherRoomVisuals).Assembly.Location);
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
