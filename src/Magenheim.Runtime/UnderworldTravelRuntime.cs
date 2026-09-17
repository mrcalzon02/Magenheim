using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Stable caller-facing boundary for every cross-world travel mechanism. Deep Gates, developer
/// commands and future linked portals must begin here rather than manufacturing physical-switch
/// requests or mutating transition persistence themselves.
/// </summary>
internal sealed class UnderworldTravelRuntime
{
    private readonly UnderworldRuntimeServices _services;
    private readonly ManualLogSource _log;

    internal UnderworldTravelRuntime(UnderworldRuntimeServices services,ManualLogSource log)
    { _services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log)); }

    internal bool TryBegin(
        UnderworldLayer targetLayer,
        UnderworldAnchor targetAnchor,
        string authorityFingerprint,
        string operationId,
        out UnderworldPhysicalWorldSwitchRequest? request,
        out string diagnostic)
    {
        request=null;
        var znet=ZNet.instance;
        if(znet is null||!znet.IsServer()){diagnostic="Cross-world travel is server/host-owned; clients must request travel through the server.";return false;}
        if(!_services.TryResolveLocalSession(out var identity,out var currentLayer,out var playerId,out diagnostic)||identity is null)return false;
        if(currentLayer==targetLayer){diagnostic=$"Cross-world travel target {targetLayer} is already the admitted physical layer.";return false;}
        if(!UnderworldProgressionAuthority.IsUnlocked){diagnostic="Cross-world travel is locked until the Nowhere King progression key is present.";return false;}

        var player=Player.m_localPlayer;
        if(player is null){diagnostic="Cross-world travel requires an admitted local host player; dedicated-server player RPC binding remains a separate multiplayer handoff step.";return false;}
        var source=CaptureAnchor(player.transform,identity,currentLayer);
        UnderworldPlayerLayerState state;
        if(!_services.StateStore.TryLoad(playerId,identity,out var loaded,out _)||loaded is null)
        {
            if(currentLayer!=UnderworldLayer.Surface){diagnostic="Cannot synthesize missing transition state while already inside the derived Underworld.";return false;}
            state=UnderworldTransitionRules.CreateSurfaceState(playerId,identity,source);
            _services.StateStore.Save(state,identity);
        }
        else state=loaded;

        try
        {
            UnderworldPlayerLayerState prepared;
            if(targetLayer==UnderworldLayer.Underworld)
            {
                // Runtime progression is already server-verified above. The Core API still accepts
                // the encounter snapshot for deterministic unit-test authority; production entry
                // therefore remains initiated by the gate/command caller until that API is collapsed.
                diagnostic="Underworld entry transaction requires the Core Nowhere-King encounter snapshot; caller binding remains unfinished.";
                return false;
            }
            prepared=UnderworldTransitionRules.BeginReturn(state,identity,source,authorityFingerprint,operationId);
            _services.StateStore.Save(prepared,identity);
            try{_services.TransitionManager.ExecutePrepared(prepared,identity,operationId,authorityFingerprint);}
            catch(UnderworldPhysicalWorldSwitchRequiredException){/* expected: durable Prepared state now owns the handoff */}
            if(!_services.WorldSwitchDriver.TryPrepare(prepared,identity,out request,out diagnostic))return false;
            _log.LogInfo($"Cross-world travel '{operationId}' prepared {currentLayer} -> {targetLayer}; physical target '{request!.TargetSaveName}'.");
            return true;
        }
        catch(Exception exception){diagnostic="Cross-world travel preparation failed closed: "+exception.Message;_log.LogError(diagnostic);return false;}
    }

    private static UnderworldAnchor CaptureAnchor(Transform transform,UnderworldWorldIdentity identity,UnderworldLayer layer)
    {
        var position=transform.position;
        var worldId=layer==UnderworldLayer.Surface?identity.ParentWorldId:identity.DerivedWorldId;
        return new UnderworldAnchor(worldId,position.x,position.y,position.z,transform.eulerAngles.y);
    }
}
