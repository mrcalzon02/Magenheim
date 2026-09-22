using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace Magenheim.Runtime;

/// <summary>
/// Private Minimap cache access used only to make repeated vanilla SetMapData swaps behave like a
/// fresh map load. The actual pin/exploration implementations remain Valheim-owned.
/// </summary>
internal static class UnderworldMinimapGameApi
{
    private static readonly FieldInfo ForestMaskTexture = AccessTools.Field(typeof(Minimap), "m_forestMaskTexture")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_forestMaskTexture");
    internal static UnityEngine.Texture2D GetForestMaskTexture(Minimap map) => (UnityEngine.Texture2D)ForestMaskTexture.GetValue(map);
    internal static void SetForestMaskTexture(Minimap map, UnityEngine.Texture2D value) => ForestMaskTexture.SetValue(map, value);
    private static readonly FieldInfo HeightTexture = AccessTools.Field(typeof(Minimap), "m_heightTexture")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_heightTexture");
    internal static UnityEngine.Texture2D GetHeightTexture(Minimap map) => (UnityEngine.Texture2D)HeightTexture.GetValue(map);
    internal static void SetHeightTexture(Minimap map, UnityEngine.Texture2D value) => HeightTexture.SetValue(map, value);
    private static readonly FieldInfo FogTexture = AccessTools.Field(typeof(Minimap), "m_fogTexture")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_fogTexture");
    internal static UnityEngine.Texture2D GetFogTexture(Minimap map) => (UnityEngine.Texture2D)FogTexture.GetValue(map);
    internal static void SetFogTexture(Minimap map, UnityEngine.Texture2D value) => FogTexture.SetValue(map, value);
    private static readonly MethodInfo GenerateMap = AccessTools.Method(typeof(Minimap), "GenerateWorldMap", Type.EmptyTypes)
        ?? throw new MissingMethodException(typeof(Minimap).FullName, "GenerateWorldMap");
    internal static void GenerateWorldMap(Minimap map) => GenerateMap.Invoke(map, Array.Empty<object>());

    private static readonly FieldInfo NamePin = AccessTools.Field(typeof(Minimap), "m_namePin")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_namePin");
    private static readonly FieldInfo DeathPin = AccessTools.Field(typeof(Minimap), "m_deathPin")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_deathPin");
    private static readonly FieldInfo SpawnPointPin = AccessTools.Field(typeof(Minimap), "m_spawnPointPin")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_spawnPointPin");
    private static readonly FieldInfo LocationPins = AccessTools.Field(typeof(Minimap), "m_locationPins")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_locationPins");
    private static readonly FieldInfo PingPins = AccessTools.Field(typeof(Minimap), "m_pingPins")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_pingPins");
    private static readonly FieldInfo ShoutPins = AccessTools.Field(typeof(Minimap), "m_shoutPins")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_shoutPins");
    private static readonly FieldInfo PlayerPins = AccessTools.Field(typeof(Minimap), "m_playerPins")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_playerPins");
    private static readonly FieldInfo RandomEventPin = AccessTools.Field(typeof(Minimap), "m_randEventPin")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_randEventPin");
    private static readonly FieldInfo RandomEventAreaPin = AccessTools.Field(typeof(Minimap), "m_randEventAreaPin")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_randEventAreaPin");
    private static readonly FieldInfo UpdateLocationsTimer = AccessTools.Field(typeof(Minimap), "m_updateLocationsTimer")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_updateLocationsTimer");
    private static readonly FieldInfo UpdateEventTime = AccessTools.Field(typeof(Minimap), "m_updateEventTime")
        ?? throw new MissingFieldException(typeof(Minimap).FullName, "m_updateEventTime");

    internal static void ResetDynamicPinCaches(Minimap map)
    {
        if (map is null) throw new ArgumentNullException(nameof(map));

        NamePin.SetValue(map, null);
        DeathPin.SetValue(map, null);
        SpawnPointPin.SetValue(map, null);
        RandomEventPin.SetValue(map, null);
        RandomEventAreaPin.SetValue(map, null);

        ClearCollection(LocationPins.GetValue(map));
        ClearCollection(PingPins.GetValue(map));
        ClearCollection(ShoutPins.GetValue(map));
        ClearCollection(PlayerPins.GetValue(map));

        // Force the native update paths to reconstruct their caches on the next eligible frame.
        UpdateLocationsTimer.SetValue(map, 0f);
        UpdateEventTime.SetValue(map, 0f);
    }

    private static void ClearCollection(object? value)
    {
        switch (value)
        {
            case IDictionary dictionary:
                dictionary.Clear();
                break;
            case IList list:
                list.Clear();
                break;
        }
    }

}
