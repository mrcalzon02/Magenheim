using System;
using System.Globalization;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Resolves the live local Valheim session into the deterministic Underworld world/player identities.
/// This is intentionally a read-only boundary: it does not mutate ZNet, Player, World or persistence state.
/// </summary>
internal static class UnderworldRuntimeIdentityResolver
{
    internal static bool TryResolveLocalSession(
        out UnderworldWorldIdentity? identity,
        out string playerId,
        out string diagnostic)
    {
        identity = null;
        playerId = string.Empty;

        var znet = ZNet.instance;
        if (znet is null)
        {
            diagnostic = "Valheim network/world session is not active.";
            return false;
        }

        var world = ZNet.World;
        if (world is null)
        {
            diagnostic = "Valheim active world metadata is unavailable.";
            return false;
        }

        var player = Player.m_localPlayer;
        if (player is null)
        {
            diagnostic = znet.IsDedicated()
                ? "A dedicated server has no local player identity; remote admission must resolve the authoritative peer/player separately."
                : "Valheim local player is not spawned yet.";
            return false;
        }

        var parentWorldId = znet.GetWorldUID().ToString(CultureInfo.InvariantCulture);
        var parentSeed = string.IsNullOrWhiteSpace(world.m_seedName)
            ? world.m_seed.ToString(CultureInfo.InvariantCulture)
            : world.m_seedName.Trim();
        playerId = player.GetPlayerID().ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(playerId))
        {
            diagnostic = "Valheim local player identity resolved to an empty value.";
            playerId = string.Empty;
            return false;
        }

        identity = UnderworldWorldIdentityFactory.Derive(parentWorldId, parentSeed);
        diagnostic = string.Empty;
        return true;
    }
}
