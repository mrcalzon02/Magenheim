using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Same-world context authority for the reserved Underworld region. Layer is observed from the
/// player's admitted position; requesting a target layer never implies a save/session switch.
/// </summary>
internal sealed class ValheimUnderworldWorldContextController : IUnderworldWorldContextController
{
    private readonly UnderworldSpatialDomainDefinition _spatialDomain;
    private readonly ManualLogSource _log;

    internal ValheimUnderworldWorldContextController(UnderworldSpatialDomainDefinition spatialDomain,ManualLogSource log)
    { _spatialDomain=spatialDomain??throw new ArgumentNullException(nameof(spatialDomain));_log=log??throw new ArgumentNullException(nameof(log)); }

    public void EnsureActive(UnderworldWorldIdentity identity,UnderworldLayer layer)
    {
        if(identity is null)throw new ArgumentNullException(nameof(identity));
        if(!TryResolveLive(out var liveIdentity,out _,out var diagnostic)||liveIdentity is null)
            throw new InvalidOperationException($"Cannot establish Magenheim world context: {diagnostic}");
        if(!SameWorld(liveIdentity,identity))
            throw new InvalidOperationException("The active Valheim world does not match the Underworld transition identity.");
        // Both layers are regions of this same world. The placement operation changes the observed
        // layer by teleporting into/out of the reserved spatial domain; no pre-teleport layer match
        // is required or desirable here.
    }

    public bool IsActive(UnderworldWorldIdentity identity,UnderworldLayer layer)
    {
        if(identity is null)return false;
        return TryResolveLive(out var liveIdentity,out var liveLayer,out _)&&liveIdentity is not null&&
               SameWorld(liveIdentity,identity)&&liveLayer==layer;
    }

    internal void ResetForWorldUnload()=>_log.LogDebug("Released Underworld same-world context observation for world unload.");

    private bool TryResolveLive(out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string diagnostic)
    {
        identity=null;layer=UnderworldLayer.Surface;
        var znet=ZNet.instance;var world=ZNet.World;
        if(znet is null||world is null){diagnostic="Valheim network/world metadata is unavailable.";return false;}
        return UnderworldRuntimeIdentityResolver.TryResolveWorldSession(_spatialDomain,znet,world,out identity,out layer,out diagnostic);
    }

    private static bool SameWorld(UnderworldWorldIdentity left,UnderworldWorldIdentity right)=>
        string.Equals(left.ParentWorldId,right.ParentWorldId,StringComparison.Ordinal)&&
        string.Equals(left.ParentSeed,right.ParentSeed,StringComparison.Ordinal);
}
