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

    internal static void PrepareForMapLoad()
    {
        var player = Player.m_localPlayer;
        var net = ZNet.instance;
        if (player is null || net is null) return;
        if (!TryResolveAdmittedIdentity(out var identity)) return;

        var legacyKey = LegacyKey(net.GetWorldUID());
        var scopedKey = ScopedKey(net.GetWorldUID(), identity!);

        // Prefer already-scoped state. This also overwrites any stale session bridge left behind by
        // an interrupted save from another derived instance.
        if (player.m_customData.TryGetValue(scopedKey, out var scoped) && !string.IsNullOrWhiteSpace(scoped))
        {
            player.m_customData[legacyKey] = scoped;
            return;
        }

        // One-way migration for pre-v2 saves. Migration is allowed only while an authoritative
        // derived instance is admitted; after the next player save the legacy slot is retired.
        if (player.m_customData.TryGetValue(legacyKey, out var legacy) && !string.IsNullOrWhiteSpace(legacy))
            player.m_customData[scopedKey] = legacy;
    }

    internal static void CommitScopedPayload(Player player)
    {
        var net = ZNet.instance;
        if (player is null || net is null || player != Player.m_localPlayer) return;
        if (!TryResolveAdmittedIdentity(out var identity)) return;

        var legacyKey = LegacyKey(net.GetWorldUID());
        if (!player.m_customData.TryGetValue(legacyKey, out var encoded) || string.IsNullOrWhiteSpace(encoded))
            return;

        player.m_customData[ScopedKey(net.GetWorldUID(), identity!)] = encoded;
        player.m_customData.Remove(legacyKey);
    }

    private static bool TryResolveAdmittedIdentity(out UnderworldWorldIdentity? identity)
    {
        identity = UnderworldRuntimeServices.Instance?.InstanceLifecycle.Identity;
        if (identity is null) return false;
        var phase = UnderworldRuntimeServices.Instance!.InstanceLifecycle.Phase;
        return phase == UnderworldInstancePhase.Admitting || phase == UnderworldInstancePhase.Active;
    }

    private static string LegacyKey(long worldUid) => LegacyPrefix + worldUid.ToString("X16");

    private static string ScopedKey(long worldUid, UnderworldWorldIdentity identity) =>
        ScopedPrefix + worldUid.ToString("X16") + "." + identity.DerivedSeedFingerprint;
}

[HarmonyPatch(typeof(Minimap), "LoadMapData")]
internal static class UnderworldMapPersistenceScopeLoadPatch
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
}
