using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Single world-level authority for Underworld progression. The Dark Throne owns encounter state;
/// this authority promotes the first legitimate victory to Valheim's durable world-key system so
/// every Deep Gate, reconnecting client, and later-generated location observes the same unlock.
/// </summary>
internal static class UnderworldProgressionAuthority
{
    internal const string NowhereKingDefeatedKey = "magenheim_nowhere_king_defeated";
    internal const string DeepGatePinName = "$magenheim_deep_gate";

    internal static bool IsUnlocked => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(NowhereKingDefeatedKey);

    internal static bool UnlockFromNowhereKingVictory()
    {
        var zone = ZoneSystem.instance;
        var znet = ZNet.instance;
        if (zone == null || znet == null || !znet.IsServer()) return false;
        if (zone.GetGlobalKey(NowhereKingDefeatedKey)) return false;
        zone.SetGlobalKey(NowhereKingDefeatedKey);
        return true;
    }

    internal static bool CanUse(UnderworldGateEndpoint endpoint) => endpoint != null && IsUnlocked;

    /// <summary>Adds the local player's surface Deep Gate pin after the durable unlock is visible.</summary>
    internal static void EnsureSurfaceGatePin(Vector3 gatePosition)
    {
        var map = Minimap.instance;
        if (map == null || !IsUnlocked) return;
        // Gate sites are thousands of metres apart, so an 8m identity radius is unambiguous while
        // preventing duplicate pins when the location reloads or the player reconnects.
        foreach (var pin in RuntimeGameApi.GetMapPins(map))
        {
            if (pin == null || pin.m_name != DeepGatePinName) continue;
            var delta = pin.m_pos - gatePosition;
            if (delta.sqrMagnitude <= 64f) return;
        }
        map.AddPin(gatePosition, Minimap.PinType.Icon3, DeepGatePinName, true, false,
            ownerID: 0L, author: default);
    }
}
