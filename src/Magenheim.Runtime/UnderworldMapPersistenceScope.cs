using System;
using HarmonyLib;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Compatibility boundary between the historical parent-world-only Underworld Minimap slot and
/// the authoritative derived-instance identity. UnderworldMapTabRuntime still owns Valheim's native
/// Minimap payload; this adapter only scopes the player custom-data slot that carries that payload.
///
/// The legacy key is permitted only as an in-memory/session bridge for the existing map runtime.
/// Durable saves are written to an instance-scoped key and the legacy key is retired after save so
/// it cannot become authority for a different derived Underworld beneath the same parent world.
/// </summary>
internal static class UnderworldMapPersistenceScope
{
    private const string LegacyPrefix = "magenheim.map.underworld.v1.";
    private const string ScopedPrefix = "magenheim.map.underworld.v2.";
    internal static bool PrepareForMapLoad()
    {
        var player = Player.m_localPlayer;
        var net = ZNet.instance;
        if (player is null || net is null) return false;
        if (!UnderworldTerrainRuntime.TryGetAdmittedIdentity(out var identity) || identity is null) return false;

        var legacyKey = LegacyKey(net.GetWorldUID());
        var scopedKey = ScopedKey(net.GetWorldUID(), identity);

        // Never overwrite a live session bridge with the older durable snapshot. If a bridge is
        // already present, it is the current native Minimap payload; only perform one-way migration
        // when the scoped slot has never existed.
        if (player.m_customData.TryGetValue(legacyKey, out var legacy) &&
            !string.IsNullOrWhiteSpace(legacy))
        {
            if (!player.m_customData.TryGetValue(scopedKey, out var migrated) ||
                string.IsNullOrWhiteSpace(migrated))
                player.m_customData[scopedKey] = legacy;
            return false;
        }

        if (!player.m_customData.TryGetValue(scopedKey, out var scoped) ||
            string.IsNullOrWhiteSpace(scoped))
            return false;

        player.m_customData[legacyKey] = scoped;
        return true;
    }

    internal static void CommitScopedPayload(Player player)
    {
        var net = ZNet.instance;
        if (player is null || net is null || player != Player.m_localPlayer) return;
        if (!UnderworldTerrainRuntime.TryGetAdmittedIdentity(out var identity) || identity is null) return;

        var legacyKey = LegacyKey(net.GetWorldUID());
        if (!player.m_customData.TryGetValue(legacyKey, out var encoded) || string.IsNullOrWhiteSpace(encoded))
            return;

        player.m_customData[ScopedKey(net.GetWorldUID(), identity)] = encoded;
        player.m_customData.Remove(legacyKey);
    }

    internal static void RestoreSessionBridge(Player player)
    {
        var net = ZNet.instance;
        if (player is null || net is null || player != Player.m_localPlayer) return;
        if (!UnderworldTerrainRuntime.TryGetAdmittedIdentity(out var identity) || identity is null) return;

        var scopedKey = ScopedKey(net.GetWorldUID(), identity);
        if (player.m_customData.TryGetValue(scopedKey, out var encoded) &&
            !string.IsNullOrWhiteSpace(encoded))
            player.m_customData[LegacyKey(net.GetWorldUID())] = encoded;
    }

    private static string LegacyKey(long worldUid) => LegacyPrefix + worldUid.ToString("X16");

    private static string ScopedKey(long worldUid, UnderworldWorldIdentity identity) =>
        ScopedPrefix + worldUid.ToString("X16") + "." + identity.DerivedSeedFingerprint;
}

[HarmonyPatch(typeof(Minimap), "LoadMapData")]
internal static class UnderworldMapPersistenceScopeLoadPatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix() => _ = UnderworldMapPersistenceScope.PrepareForMapLoad();
}

// Instance admission can complete after vanilla Minimap.LoadMapData during world startup. Retry the
// preparation boundary from the ordinary Minimap update until one admitted identity is prepared;
// the scope guard makes this a one-shot operation rather than a per-frame persistence path.
[HarmonyPatch(typeof(Minimap), "Update")]
internal static class UnderworldMapPersistenceScopeAdmissionPatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix() => UnderworldMapPersistenceScope.PrepareForMapLoad();
}

[HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SavePlayerData), new[] { typeof(Player) })]
internal static class UnderworldMapPersistenceScopeSavePatch
{
    // UnderworldMinimapPlayerSavePatch runs at Priority.First and publishes the current native
    // payload into the legacy session bridge. Run last, copy that exact payload to the scoped slot,
    // then retire the bridge before PlayerProfile serializes custom data.
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Player __0) => UnderworldMapPersistenceScope.CommitScopedPayload(__0);

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Player __0) => UnderworldMapPersistenceScope.RestoreSessionBridge(__0);
}
