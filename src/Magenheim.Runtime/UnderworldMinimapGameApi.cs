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
    private static readonly FieldInfo NamePin = Required("m_namePin");
    private static readonly FieldInfo DeathPin = Required("m_deathPin");
    private static readonly FieldInfo SpawnPointPin = Required("m_spawnPointPin");
    private static readonly FieldInfo LocationPins = Required("m_locationPins");
    private static readonly FieldInfo PingPins = Required("m_pingPins");
    private static readonly FieldInfo ShoutPins = Required("m_shoutPins");
    private static readonly FieldInfo PlayerPins = Required("m_playerPins");
    private static readonly FieldInfo RandomEventPin = Required("m_randEventPin");
    private static readonly FieldInfo RandomEventAreaPin = Required("m_randEventAreaPin");
    private static readonly FieldInfo UpdateLocationsTimer = Required("m_updateLocationsTimer");
    private static readonly FieldInfo UpdateEventTime = Required("m_updateEventTime");

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

    private static FieldInfo Required(string name) =>
        AccessTools.Field(typeof(Minimap), name)
        ?? throw new MissingFieldException(typeof(Minimap).FullName, name);
}
