using System;
using System.Globalization;
using System.Reflection;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>Resolves the parent-world-derived identity used by the dedicated Underworld instance.</summary>
internal static class UnderworldRuntimeIdentityResolver
{
    /// <summary>Resolves the local player and parent-derived instance identity without inferring a gameplay layer from coordinates.</summary>
    internal static bool TryResolveLocalSession(out UnderworldWorldIdentity? identity, out string playerId, out string diagnostic)
    {
        identity = null;
        playerId = string.Empty;
        var znet = ZNet.instance;
        if (znet is null) { diagnostic = "Valheim network/world session is not active."; return false; }
        var world = ZNet.World;
        if (world is null) { diagnostic = "Valheim active world metadata is unavailable."; return false; }
        if (!TryResolveWorldIdentity(znet, world, out identity, out diagnostic)) return false;

        var player = Player.m_localPlayer;
        if (player is null)
        {
            diagnostic = znet.IsDedicated()
                ? "A dedicated server has no local player identity; remote admission must resolve the authoritative peer/player separately."
                : "Valheim local player is not spawned yet.";
            identity = null;
            return false;
        }

        playerId = player.GetPlayerID().ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(playerId))
        {
            diagnostic = "Valheim local player identity resolved to an empty value.";
            identity = null;
            playerId = string.Empty;
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    /// <summary>Resolves identity without making any claim about active Surface/Underworld context.</summary>
    internal static bool TryResolveWorldIdentity(ZNet znet, object world, out UnderworldWorldIdentity? identity, out string diagnostic)
    {
        if (znet is null) throw new ArgumentNullException(nameof(znet));
        if (world is null) throw new ArgumentNullException(nameof(world));
        identity = null;

        var worldType = world.GetType();
        var seedName = ReadField<string>(worldType, world, "m_seedName");
        var seed = ReadField<int?>(worldType, world, "m_seed");
        var parentWorldId = znet.GetWorldUID().ToString(CultureInfo.InvariantCulture);
        var parentSeed = !string.IsNullOrWhiteSpace(seedName)
            ? seedName!.Trim()
            : (seed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (string.IsNullOrWhiteSpace(parentSeed))
        {
            diagnostic = "Valheim active world seed metadata is unavailable.";
            return false;
        }

        identity = UnderworldWorldIdentityFactory.Derive(parentWorldId, parentSeed);
        diagnostic = string.Empty;
        return true;
    }

    internal static string ResolveWorldSaveName(object world)
    {
        if (world is null) return string.Empty;
        var type = world.GetType();
        foreach (var name in new[] { "m_name", "m_fileName", "m_filename" })
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field?.GetValue(world) is string value && !string.IsNullOrWhiteSpace(value)) return value.Trim();
        }
        return string.Empty;
    }

    private static T? ReadField<T>(Type type, object instance, string name)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field is null) return default;
        var value = field.GetValue(instance);
        if (value is T typed) return typed;
        return default;
    }
}
