using System;
using System.IO;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldRuntimeServices
{
    private readonly ValheimUnderworldWorldContextController _worldContext;

    private UnderworldRuntimeServices(
        UnderworldSpatialDomainDefinition spatialDomain,
        UnderworldTransitionStateStore stateStore,
        UnderworldDeepBoonSelectionStore deepBoonSelectionStore,
        UnderworldExplorationStateStore explorationStateStore,
        ValheimUnderworldWorldContextController worldContext,
        ValheimUnderworldTransitionPlacementHost placementHost,
        StoredUnderworldTransitionHost transitionHost,
        UnderworldWorldTransitionManager transitionManager,
        UnderworldTransitionRecoveryRuntime recoveryRuntime)
    {
        SpatialDomain=spatialDomain;StateStore=stateStore;DeepBoonSelectionStore=deepBoonSelectionStore;ExplorationStateStore=explorationStateStore;
        _worldContext=worldContext;PlacementHost=placementHost;TransitionHost=transitionHost;
        TransitionManager=transitionManager;RecoveryRuntime=recoveryRuntime;
    }

    internal UnderworldSpatialDomainDefinition SpatialDomain{get;}
    internal UnderworldTransitionStateStore StateStore{get;}
    internal UnderworldDeepBoonSelectionStore DeepBoonSelectionStore{get;}
    internal UnderworldExplorationStateStore ExplorationStateStore{get;}
    internal ValheimUnderworldTransitionPlacementHost PlacementHost{get;}
    internal StoredUnderworldTransitionHost TransitionHost{get;}
    internal UnderworldWorldTransitionManager TransitionManager{get;}
    internal UnderworldTransitionRecoveryRuntime RecoveryRuntime{get;}

    internal static UnderworldRuntimeServices Create(string pluginConfigDirectory,ManualLogSource log)
    {
        if(string.IsNullOrWhiteSpace(pluginConfigDirectory))throw new ArgumentException("Plugin config directory is required for Underworld persistence.",nameof(pluginConfigDirectory));
        if(log is null)throw new ArgumentNullException(nameof(log));
        var root=Path.Combine(Path.GetFullPath(pluginConfigDirectory),"Magenheim");
        var spatialDomain=UnderworldSpatialDomain.CreateDefault();
        var stateStore=new UnderworldTransitionStateStore(Path.Combine(root,"underworld-transitions"),log);
        var deepBoonSelectionStore=new UnderworldDeepBoonSelectionStore(Path.Combine(root,"underworld-deep-boon-selections"),log);
        var explorationStateStore=new UnderworldExplorationStateStore(Path.Combine(root,"underworld-exploration"),log);
        var worldContext=new ValheimUnderworldWorldContextController(spatialDomain,log);
        var placementHost=new ValheimUnderworldTransitionPlacementHost(worldContext,spatialDomain,log);
        var transitionHost=new StoredUnderworldTransitionHost(stateStore,placementHost);
        var transitionManager=new UnderworldWorldTransitionManager(transitionHost,log);
        var recoveryRuntime=new UnderworldTransitionRecoveryRuntime(stateStore,transitionManager,log);
        var services=new UnderworldRuntimeServices(spatialDomain,stateStore,deepBoonSelectionStore,explorationStateStore,worldContext,placementHost,transitionHost,transitionManager,recoveryRuntime);
        UnderworldTerrainRuntime.Configure(services,log);
        return services;
    }

    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string playerId,out string diagnostic)=>
        UnderworldRuntimeIdentityResolver.TryResolveLocalSession(SpatialDomain,out identity,out layer,out playerId,out diagnostic)&&identity is not null;

    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out string playerId,out string diagnostic)=>
        TryResolveLocalSession(out identity,out _,out playerId,out diagnostic);

    internal void ResetForWorldUnload()=>_worldContext.ResetForWorldUnload();
    internal void Shutdown()=>_worldContext.ResetForWorldUnload();
}
