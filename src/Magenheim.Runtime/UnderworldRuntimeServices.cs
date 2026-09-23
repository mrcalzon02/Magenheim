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
        UnderworldDeepBoonSelectionStore deepBoonSelectionStore,
        UnderworldInstanceLifecycle instanceLifecycle,
        ValheimUnderworldWorldContextController worldContext,
        ManualLogSource log)
    {
        TerrainDomain=terrainDomain;DeepBoonSelectionStore=deepBoonSelectionStore;InstanceLifecycle=instanceLifecycle;
        _worldContext=worldContext;
        StructureAdmission=new UnderworldStructureAdmissionController(log);
        ChunkStreaming=new UnderworldInstanceChunkStreamingRuntime(this,log);
        ChunkMaterializer=new UnderworldInstanceChunkMaterializer(this,log);
        BiomeStructureResidency=new UnderworldBiomeStructureResidencyRuntime(this);
        BiomeStructureResidency.Register(new FractureFaultLineFamily());
        BiomeStructureResidency.Register(new FractureCliffMonasteryFamily());
        BiomeStructureResidency.Register(new FractureSuspendedRoadStationFamily());
        BiomeStructureResidency.Register(new FractureAnchorTowerFamily());
        BiomeStructureResidency.Register(new BlackwaterSparsePocketFamily());
        BiomeStructureResidency.Register(new FungalSplitPillarLandmarkFamily());
        BiomeStructureResidency.Register(new SulfurThreeLavafallsLandmarkFamily());
        BiomeStructureResidency.Register(new FrozenWallLandmarkFamily());
        BiomeStructureResidency.Register(new GreatDecayVanishingRoadLandmarkFamily());
        BiomeStructureResidency.Register(new BrokenAncientBridgeLandmarkFamily());
    }

    internal UnderworldInstanceTerrainDomain TerrainDomain{get;}
    internal UnderworldDeepBoonSelectionStore DeepBoonSelectionStore{get;}
    internal UnderworldInstanceLifecycle InstanceLifecycle{get;}
    internal UnderworldStructureAdmissionController StructureAdmission{get;}
    internal UnderworldInstanceChunkStreamingRuntime ChunkStreaming{get;}
    internal UnderworldInstanceChunkMaterializer ChunkMaterializer{get;}
    internal UnderworldBiomeStructureResidencyRuntime BiomeStructureResidency{get;}

    internal static UnderworldRuntimeServices Create(string pluginConfigDirectory,ManualLogSource log)
    {
        if(string.IsNullOrWhiteSpace(pluginConfigDirectory))throw new ArgumentException("Plugin config directory is required for Underworld persistence.",nameof(pluginConfigDirectory));
        if(log is null)throw new ArgumentNullException(nameof(log));
        var root=Path.Combine(Path.GetFullPath(pluginConfigDirectory),"Magenheim");
        var terrainDomain=UnderworldInstanceTerrainDomain.CreateDefault();
        var deepBoonSelectionStore=new UnderworldDeepBoonSelectionStore(Path.Combine(root,"underworld-deep-boon-selections"),log);
        var instanceLifecycle=new UnderworldInstanceLifecycle();
        var worldContext=new ValheimUnderworldWorldContextController(log);
        var services=new UnderworldRuntimeServices(terrainDomain,deepBoonSelectionStore,instanceLifecycle,worldContext,log);
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

    internal void ActivateLayer(UnderworldWorldIdentity identity, UnderworldLayer layer) =>
        _worldContext.EnsureActive(identity, layer);

    internal void ResetForWorldUnload()
    {
        BiomeStructureResidency.Clear();
        ChunkMaterializer.Clear();
        ChunkStreaming.Clear();
        InstanceLifecycle.Reset();
        _worldContext.ResetForWorldUnload();
    }

    internal void Shutdown()
    {
        BiomeStructureResidency.Clear();
        ChunkMaterializer.Clear();
        ChunkStreaming.Clear();
        InstanceLifecycle.Reset();
        _worldContext.ResetForWorldUnload();
    }
}
