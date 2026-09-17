using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldPhysicalWorldSwitchCoordinator
{
    private readonly UnderworldWorldPairManifestStore _pairStore;
    private readonly ValheimPhysicalUnderworldWorldContextController _worldContext;
    private readonly ManualLogSource _log;

    internal UnderworldPhysicalWorldSwitchCoordinator(UnderworldWorldPairManifestStore pairStore,ValheimPhysicalUnderworldWorldContextController worldContext,ManualLogSource log)
    {
        _pairStore=pairStore??throw new ArgumentNullException(nameof(pairStore));
        _worldContext=worldContext??throw new ArgumentNullException(nameof(worldContext));
        _log=log??throw new ArgumentNullException(nameof(log));
    }

    internal UnderworldPhysicalWorldSwitchRequest ResolvePending(UnderworldPlayerLayerState state,UnderworldWorldIdentity identity)
    {
        if(state is null)throw new ArgumentNullException(nameof(state));if(identity is null)throw new ArgumentNullException(nameof(identity));
        var active=state.ActiveTransition??throw new InvalidOperationException("Cannot resolve a physical world switch without an active persisted transition.");
        if(active.TargetLayer==state.CurrentLayer)throw new InvalidOperationException("Persisted transition target is already the player's committed layer; physical switch request is inconsistent.");
        var targetSave=_pairStore.GetTargetSaveName(identity,active.TargetLayer);
        if(string.IsNullOrWhiteSpace(targetSave))throw new InvalidOperationException("World-pair manifest resolved an empty physical target save name.");
        var request=new UnderworldPhysicalWorldSwitchRequest(identity,state.PlayerId,state.CurrentLayer,active.TargetLayer,targetSave);
        _log.LogInfo($"Resolved persisted Underworld physical switch {request.CurrentLayer} -> {request.TargetLayer} using save '{request.TargetSaveName}'.");
        return request;
    }

    internal bool IsTargetAdmitted(UnderworldPhysicalWorldSwitchRequest request,out string diagnostic)
    {
        if(request is null)throw new ArgumentNullException(nameof(request));
        if(!_worldContext.IsActive(request.Identity,request.TargetLayer)){diagnostic=$"Loaded Valheim session has not admitted requested {request.TargetLayer} world '{request.TargetSaveName}'.";return false;}
        var world=ZNet.World;if(world is null){diagnostic="Valheim world metadata disappeared after target-layer admission.";return false;}
        var liveSave=UnderworldRuntimeIdentityResolver.ResolveWorldSaveName(world);
        if(!string.Equals(liveSave,request.TargetSaveName,StringComparison.Ordinal)){diagnostic=$"Loaded save '{liveSave}' does not match requested physical target '{request.TargetSaveName}'.";return false;}
        diagnostic=$"Verified physical target '{request.TargetSaveName}' as {request.TargetLayer} for the persisted world pair.";return true;
    }
}

internal sealed class UnderworldPhysicalWorldSwitchRequest
{
    internal UnderworldPhysicalWorldSwitchRequest(UnderworldWorldIdentity identity,string playerId,UnderworldLayer currentLayer,UnderworldLayer targetLayer,string targetSaveName)
    {Identity=identity??throw new ArgumentNullException(nameof(identity));PlayerId=string.IsNullOrWhiteSpace(playerId)?throw new ArgumentException("Player identity is required.",nameof(playerId)):playerId;CurrentLayer=currentLayer;TargetLayer=targetLayer;TargetSaveName=string.IsNullOrWhiteSpace(targetSaveName)?throw new ArgumentException("Target save name is required.",nameof(targetSaveName)):targetSaveName;}
    internal UnderworldWorldIdentity Identity{get;}internal string PlayerId{get;}internal UnderworldLayer CurrentLayer{get;}internal UnderworldLayer TargetLayer{get;}internal string TargetSaveName{get;}
}
