using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Single world-level authority for Underworld progression. The Dark Throne owns encounter state;
/// this authority promotes the first legitimate victory to Valheim's durable world-key system so
/// every Deep Gate, reconnecting client, and later-generated location observes the same unlock.
/// Durable state is scoped to the deterministic derived Underworld identity. The historical
/// parent-world key is retained only as a one-way migration source for existing saves.
/// </summary>
internal static class UnderworldProgressionAuthority
{
    internal const string LegacyNowhereKingDefeatedKey = "magenheim_nowhere_king_defeated";
    private const string NowhereKingDefeatedKeyPrefix = "magenheim.underworld.nowhere_king_defeated.";
    internal const string DeepGatePinName = "$magenheim_deep_gate";

    internal static bool IsUnlocked
    {
        get
        {
            var zone = ZoneSystem.instance;
            if (zone == null || !TryResolveIdentity(out var identity)) return false;
            TryMigrateLegacyState(zone, identity!);
            return zone.GetGlobalKey(ScopedNowhereKingDefeatedKey(identity!));
        }
    }

    internal static bool UnlockFromNowhereKingVictory()
    {
        var zone = ZoneSystem.instance;
        var znet = ZNet.instance;
        if (zone == null || znet == null || !znet.IsServer() || !TryResolveIdentity(out var identity)) return false;
        TryMigrateLegacyState(zone, identity!);
        var key = ScopedNowhereKingDefeatedKey(identity!);
        if (zone.GetGlobalKey(key)) return false;
        zone.SetGlobalKey(key);
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

    private static bool TryResolveIdentity(out UnderworldWorldIdentity? identity)
    {
        identity = null;
        var znet = ZNet.instance;
        var world = ZNet.World;
        if (znet is null || world is null) return false;
        return UnderworldRuntimeIdentityResolver.TryResolveWorldIdentity(znet, world, out identity, out _);
    }

    private static string ScopedNowhereKingDefeatedKey(UnderworldWorldIdentity identity) =>
        NowhereKingDefeatedKeyPrefix + identity.DerivedSeedFingerprint;

    private static void TryMigrateLegacyState(ZoneSystem zone, UnderworldWorldIdentity identity)
    {
        var znet = ZNet.instance;
        if (znet == null || !znet.IsServer()) return;

        var scopedKey = ScopedNowhereKingDefeatedKey(identity);
        if (!zone.GetGlobalKey(scopedKey) && zone.GetGlobalKey(LegacyNowhereKingDefeatedKey))
            zone.SetGlobalKey(scopedKey);

        // Retire the unscoped key after migration so it can never become authority for another
        // derived instance identity if identity derivation changes in a future schema migration.
        if (zone.GetGlobalKey(LegacyNowhereKingDefeatedKey))
            zone.RemoveGlobalKey(LegacyNowhereKingDefeatedKey);
    }
}
