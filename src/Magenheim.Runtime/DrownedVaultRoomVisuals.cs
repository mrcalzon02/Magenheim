using System;
using System.Collections.Generic;
using System.IO;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DrownedVaultRoomVisuals
{
    internal const string ThemeName = "MagenheimDrownedVaults";
    internal const string PassagePrefabName = "Magenheim_DrownedVaults_Passage";
    internal const string PassageModelId = "underworld-dungeon-blackwater-drowned-vaults-passage";
    internal const float PassageLengthMeters = 16f;
    internal const float PassageWidthMeters = 14f;
    internal const float PassageDepthMeters = 16f;

    internal static string RoomPrefabName(UnderworldDrownedVaultRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();
        var suffix = definition.Room.Id.Substring(
            UnderworldBlackwaterDrownedVaultsCatalog.RoomIdPrefix.Length);
        return "Magenheim_DrownedVaults_" + suffix.Replace('-', '_');
    }

    internal static GameObject CreateRoomPrefab(UnderworldDrownedVaultRoomDefinition definition)
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
        var water = DrownedVaultWaterRuntime.Attach(root, definition);
        if (definition.RouteMode == UnderworldDrownedVaultRouteMode.Dry)
        {
            if (water is not null)
                throw new InvalidOperationException(
                    "Dry Drowned Vault room unexpectedly received native water.");
        }
        else
        {
            water?.Validate();
        }
        return root;
    }

    internal static GameObject CreatePassagePrefab()
    {
        var root = CreateRoomRoot(
            PassagePrefabName,
            new Vector3Int(
                Mathf.CeilToInt(PassageWidthMeters),
                14,
                Mathf.CeilToInt(PassageDepthMeters)));
        ModelAssets.Load(root, PassageModelId, hideOriginal: false);
        return root;
    }

    internal static bool TryGetMissingPayloads(out IReadOnlyList<string> missing)
    {
        var result = new List<string>();
        foreach (var definition in UnderworldBlackwaterDrownedVaultsCatalog.Rooms)
            if (!HasModel(definition.Room.ModelId))
                result.Add(definition.Room.ModelId);
        if (!HasModel(PassageModelId))
            result.Add(PassageModelId);
        missing = result.AsReadOnly();
        return result.Count == 0;
    }

    private static bool HasModel(string modelId)
    {
        var directory = Path.GetDirectoryName(typeof(DrownedVaultRoomVisuals).Assembly.Location);
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
