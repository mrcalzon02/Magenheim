using System;
using System.Globalization;
using System.Reflection;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>Read-only authority that identifies whether the live Valheim session is the parent surface world or its persisted derived Underworld.</summary>
internal static class UnderworldRuntimeIdentityResolver
{
    internal static bool TryResolveLocalSession(UnderworldWorldPairManifestStore pairStore,out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string playerId,out string diagnostic)
    {
        if(pairStore is null)throw new ArgumentNullException(nameof(pairStore));identity=null;layer=UnderworldLayer.Surface;playerId=string.Empty;
        var znet=ZNet.instance;if(znet is null){diagnostic="Valheim network/world session is not active.";return false;}
        var world=ZNet.World;if(world is null){diagnostic="Valheim active world metadata is unavailable.";return false;}
        var player=Player.m_localPlayer;if(player is null){diagnostic=znet.IsDedicated()?"A dedicated server has no local player identity; remote admission must resolve the authoritative peer/player separately.":"Valheim local player is not spawned yet.";return false;}
        playerId=player.GetPlayerID().ToString(CultureInfo.InvariantCulture);if(string.IsNullOrWhiteSpace(playerId)){diagnostic="Valheim local player identity resolved to an empty value.";playerId=string.Empty;return false;}

        var saveName=ResolveWorldSaveName(world);
        if(!string.IsNullOrWhiteSpace(saveName)&&pairStore.TryResolveByDerivedSaveName(saveName,out identity)&&identity is not null)
        {
            layer=UnderworldLayer.Underworld;diagnostic=string.Empty;return true;
        }

        var parentWorldId=znet.GetWorldUID().ToString(CultureInfo.InvariantCulture);
        var parentSeed=string.IsNullOrWhiteSpace(world.m_seedName)?world.m_seed.ToString(CultureInfo.InvariantCulture):world.m_seedName.Trim();
        identity=UnderworldWorldIdentityFactory.Derive(parentWorldId,parentSeed);layer=UnderworldLayer.Surface;diagnostic=string.Empty;return true;
    }

    private static string ResolveWorldSaveName(object world)
    {
        var type=world.GetType();
        foreach(var name in new[]{"m_name","m_fileName","m_filename"})
        {
            var field=type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(field?.GetValue(world) is string value&&!string.IsNullOrWhiteSpace(value))return value.Trim();
        }
        return string.Empty;
    }
}
