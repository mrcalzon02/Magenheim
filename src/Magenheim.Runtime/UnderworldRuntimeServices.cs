using System;
using System.IO;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldRuntimeServices
{
    private readonly ValheimUnderworldWorldContextController _worldContext;

    private UnderworldRuntimeServices(
        UnderworldInstanceTerrainDomain terrainDomain,
        UnderworldTransitionStateStore stateStore,
        UnderworldDeepBoonSelectionStore deepBoonSelectionStore,
        UnderworldExplorationStateStore explorationStateStore,
        UnderworldGeneratedObjectStateStore generatedObjectStateStore,
        UnderworldInstanceLifecycle instanceLifecycle,
        ValheimUnderworldWorldContextController worldContext,
        ValheimUnderworldTransitionPlacementHost placementHost,
        StoredUnderworldTransitionHost transitionHost,
        UnderworldWorldTransitionManager transitionManager,
        UnderworldTransitionRecoveryRuntime recoveryRuntime,
        ManualLogSource log)
    {
        TerrainDomain=terrainDomain;StateStore=stateStore;DeepBoonSelectionStore=deepBoonSelectionStore;ExplorationStateStore=explorationStateStore;GeneratedObjectStateStore=generatedObjectStateStore;InstanceLifecycle=instanceLifecycle;
        _worldContext=worldContext;PlacementHost=placementHost;TransitionHost=transitionHost;
        TransitionManager=transitionManager;RecoveryRuntime=recoveryRuntime;
        StructureAdmission=new UnderworldStructureAdmissionController(generatedObjectStateStore,log);
        ChunkStreaming=new UnderworldInstanceChunkStreamingRuntime(this,log);
        ChunkMaterializer=new UnderworldInstanceChunkMaterializer(this,log);
        FungalStructureResidency=new UnderworldFungalStructureResidencyRuntime(this);
    }

    internal UnderworldInstanceTerrainDomain TerrainDomain{get;}
    internal UnderworldTransitionStateStore StateStore{get;}
    internal UnderworldDeepBoonSelectionStore DeepBoonSelectionStore{get;}
    internal UnderworldExplorationStateStore ExplorationStateStore{get;}
    internal UnderworldGeneratedObjectStateStore GeneratedObjectStateStore{get;}
    internal UnderworldInstanceLifecycle InstanceLifecycle{get;}
    internal ValheimUnderworldTransitionPlacementHost PlacementHost{get;}
    internal StoredUnderworldTransitionHost TransitionHost{get;}
    internal UnderworldWorldTransitionManager TransitionManager{get;}
    internal UnderworldTransitionRecoveryRuntime RecoveryRuntime{get;}
    internal UnderworldStructureAdmissionController StructureAdmission{get;}
    internal UnderworldInstanceChunkStreamingRuntime ChunkStreaming{get;}
    internal UnderworldInstanceChunkMaterializer ChunkMaterializer{get;}
    internal UnderworldFungalStructureResidencyRuntime FungalStructureResidency{get;}

    internal static UnderworldRuntimeServices Create(string pluginConfigDirectory,ManualLogSource log)
    {
        if(string.IsNullOrWhiteSpace(pluginConfigDirectory))throw new ArgumentException("Plugin config directory is required for Underworld persistence.",nameof(pluginConfigDirectory));
        if(log is null)throw new ArgumentNullException(nameof(log));
        var root=Path.Combine(Path.GetFullPath(pluginConfigDirectory),"Magenheim");
        var terrainDomain=UnderworldInstanceTerrainDomain.CreateDefault();
        var stateStore=new UnderworldTransitionStateStore(Path.Combine(root,"underworld-transitions"),log);
        var deepBoonSelectionStore=new UnderworldDeepBoonSelectionStore(Path.Combine(root,"underworld-deep-boon-selections"),log);
        var explorationStateStore=new UnderworldExplorationStateStore(Path.Combine(root,"underworld-exploration"),log);
        var generatedObjectStateStore=new UnderworldGeneratedObjectStateStore(Path.Combine(root,"underworld-generated-objects"),log);
        var instanceLifecycle=new UnderworldInstanceLifecycle();
        var worldContext=new ValheimUnderworldWorldContextController(log);
        var placementHost=new ValheimUnderworldTransitionPlacementHost(worldContext,log);
        var transitionHost=new StoredUnderworldTransitionHost(stateStore,placementHost);
        var transitionManager=new UnderworldWorldTransitionManager(transitionHost,instanceLifecycle,log);
        var recoveryRuntime=new UnderworldTransitionRecoveryRuntime(stateStore,transitionManager,log);
        var services=new UnderworldRuntimeServices(terrainDomain,stateStore,deepBoonSelectionStore,explorationStateStore,generatedObjectStateStore,instanceLifecycle,worldContext,placementHost,transitionHost,transitionManager,recoveryRuntime,log);
        UnderworldTerrainRuntime.Configure(services,log);
        return services;
    }

    internal bool TryResolveWorldSession(out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string diagnostic)
    {
        identity=null;layer=UnderworldLayer.Surface;
        var znet=ZNet.instance;var world=ZNet.World;
        if(znet is null||world is null){diagnostic="Valheim network/world metadata is unavailable.";return false;}
        if(!UnderworldRuntimeIdentityResolver.TryResolveWorldIdentity(znet,world,out var liveIdentity,out diagnostic)||liveIdentity is null)return false;
        if(!_worldContext.TryGetActiveContext(liveIdentity,out identity,out layer)||identity is null){diagnostic="Magenheim instance layer has not been established by the explicit world-context controller.";return false;}
        diagnostic=string.Empty;return true;
    }

    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string playerId,out string diagnostic)
    {
        layer=UnderworldLayer.Surface;
        if(!UnderworldRuntimeIdentityResolver.TryResolveLocalSession(out identity,out playerId,out diagnostic)||identity is null)return false;
        if(_worldContext.IsActive(identity,UnderworldLayer.Underworld))layer=UnderworldLayer.Underworld;
        else if(_worldContext.IsActive(identity,UnderworldLayer.Surface))layer=UnderworldLayer.Surface;
        else
        {
            diagnostic="Local Magenheim layer has not been established by the explicit world-context controller.";
            identity=null;playerId=string.Empty;return false;
        }
        diagnostic=string.Empty;return true;
    }

    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out string playerId,out string diagnostic)=>
        UnderworldRuntimeIdentityResolver.TryResolveLocalSession(out identity,out playerId,out diagnostic)&&identity is not null;

    internal void ResetForWorldUnload()
    {
        PlacementHost.ResetForWorldBoundary();
        FungalStructureResidency.Clear();
        ChunkMaterializer.Clear();
        ChunkStreaming.Clear();
        InstanceLifecycle.Reset();
        _worldContext.ResetForWorldUnload();
    }

    internal void Shutdown()
    {
        PlacementHost.ResetForWorldBoundary();
        FungalStructureResidency.Clear();
        ChunkMaterializer.Clear();
        ChunkStreaming.Clear();
        InstanceLifecycle.Reset();
        _worldContext.ResetForWorldUnload();
    }
}
