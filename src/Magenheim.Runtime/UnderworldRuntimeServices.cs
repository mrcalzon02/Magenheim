using System;
using System.IO;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldRuntimeServices
{
    private UnderworldRuntimeServices(UnderworldInstanceTerrainDomain terrainDomain,UnderworldDeepBoonSelectionStore deepBoonSelectionStore,UnderworldInstanceLifecycle instanceLifecycle,ManualLogSource log)
    {
        TerrainDomain=terrainDomain;DeepBoonSelectionStore=deepBoonSelectionStore;InstanceLifecycle=instanceLifecycle;
        StructureAdmission=new UnderworldStructureAdmissionController(log);ChunkStreaming=new UnderworldInstanceChunkStreamingRuntime(this,log);UniqueLocationAnchors=new UnderworldUniqueLocationAnchorResolver(this);ChunkMaterializer=new UnderworldInstanceChunkMaterializer(this,log);BiomeStructureResidency=new UnderworldBiomeStructureResidencyRuntime(this);
        BiomeStructureResidency.Register(new FungalMotherbedFamily(this));BiomeStructureResidency.Register(new FractureFaultLineFamily());BiomeStructureResidency.Register(new FractureCliffMonasteryFamily());BiomeStructureResidency.Register(new FractureSuspendedRoadStationFamily());BiomeStructureResidency.Register(new FractureAnchorTowerFamily());BiomeStructureResidency.Register(new FractureGreatBridgeFamily());BiomeStructureResidency.Register(new FractureDeepSigilFamily());BiomeStructureResidency.Register(new FractureSuspendedCourtFamily(this));BiomeStructureResidency.Register(new BlackwaterDrownedRingFamily(this));BiomeStructureResidency.Register(new BlackwaterSparsePocketFamily());BiomeStructureResidency.Register(new BlackwaterDeepSigilFamily());BiomeStructureResidency.Register(new FungalSplitPillarLandmarkFamily());BiomeStructureResidency.Register(new FungalDeepSigilFamily());BiomeStructureResidency.Register(new SulfurFurnaceHeartCalderaFamily(this));BiomeStructureResidency.Register(new SulfurThreeLavafallsLandmarkFamily());BiomeStructureResidency.Register(new SulfurDeepSigilFamily());BiomeStructureResidency.Register(new FrozenStillvaultFamily(this));BiomeStructureResidency.Register(new FrozenWallLandmarkFamily());BiomeStructureResidency.Register(new FrozenDeepSigilFamily());BiomeStructureResidency.Register(new GreatDecayCarrionCrownFamily(this));BiomeStructureResidency.Register(new GreatDecayVanishingRoadLandmarkFamily());BiomeStructureResidency.Register(new GreatDecayDeepSigilFamily());BiomeStructureResidency.Register(new BrokenAncientBridgeLandmarkFamily());
    }

    internal UnderworldInstanceTerrainDomain TerrainDomain{get;} internal UnderworldDeepBoonSelectionStore DeepBoonSelectionStore{get;} internal UnderworldInstanceLifecycle InstanceLifecycle{get;} internal UnderworldStructureAdmissionController StructureAdmission{get;} internal UnderworldInstanceChunkStreamingRuntime ChunkStreaming{get;} internal UnderworldUniqueLocationAnchorResolver UniqueLocationAnchors{get;} internal UnderworldInstanceChunkMaterializer ChunkMaterializer{get;} internal UnderworldBiomeStructureResidencyRuntime BiomeStructureResidency{get;}

    internal static UnderworldRuntimeServices Create(string pluginConfigDirectory,ManualLogSource log)
    {
        if(string.IsNullOrWhiteSpace(pluginConfigDirectory))throw new ArgumentException("Plugin config directory is required for Underworld persistence.",nameof(pluginConfigDirectory));if(log is null)throw new ArgumentNullException(nameof(log));
        var root=Path.Combine(Path.GetFullPath(pluginConfigDirectory),"Magenheim");var terrainDomain=UnderworldInstanceTerrainDomain.CreateDefault();var deepBoonSelectionStore=new UnderworldDeepBoonSelectionStore(Path.Combine(root,"underworld-deep-boon-selections"),log);var instanceLifecycle=new UnderworldInstanceLifecycle();var services=new UnderworldRuntimeServices(terrainDomain,deepBoonSelectionStore,instanceLifecycle,log);UnderworldTerrainRuntime.Configure(services,log);return services;
    }

    internal bool TryResolveWorldSession(out UnderworldWorldIdentity? identity,out string diagnostic)
    {
        identity=InstanceLifecycle.Identity;if(identity is null||InstanceLifecycle.Phase!=UnderworldInstancePhase.Active){identity=null;diagnostic="The paired Underworld instance is not active.";return false;}diagnostic=string.Empty;return true;
    }

    internal bool TryResolvePlayerSession(Player player,out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string diagnostic)
    {
        identity=null;layer=UnderworldLayer.Surface;if(player is null){diagnostic="Player is unavailable.";return false;}
        if(!TryResolveWorldSession(out identity,out diagnostic)||identity is null)return false;
        layer=UnderworldInstanceLayer.IsUnderworldEnginePosition(player.transform.position)?UnderworldLayer.Underworld:UnderworldLayer.Surface;diagnostic=string.Empty;return true;
    }

    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string playerId,out string diagnostic)
    {
        identity=null;layer=UnderworldLayer.Surface;playerId=string.Empty;
        if(!UnderworldRuntimeIdentityResolver.TryResolveLocalSession(out var liveIdentity,out playerId,out diagnostic)||liveIdentity is null)return false;
        var player=Player.m_localPlayer;if(player is null){identity=null;playerId=string.Empty;diagnostic="Local player is unavailable.";return false;}
        if(!TryResolvePlayerSession(player,out identity,out layer,out diagnostic)||identity is null){playerId=string.Empty;return false;}
        if(!SameInstance(identity,liveIdentity)){identity=null;playerId=string.Empty;diagnostic="The local Valheim session does not match the active Underworld instance authority.";return false;}
        return true;
    }

    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out string playerId,out string diagnostic)=>UnderworldRuntimeIdentityResolver.TryResolveLocalSession(out identity,out playerId,out diagnostic)&&identity is not null;

    internal void ResetForWorldUnload(){BiomeStructureResidency.Clear();UniqueLocationAnchors.Clear();ChunkMaterializer.Clear();ChunkStreaming.Clear();InstanceLifecycle.Reset();}
    internal void Shutdown(){BiomeStructureResidency.Clear();UniqueLocationAnchors.Clear();ChunkMaterializer.Clear();ChunkStreaming.Clear();InstanceLifecycle.Reset();}

    private static bool SameInstance(UnderworldWorldIdentity left,UnderworldWorldIdentity right)=>string.Equals(left.ParentWorldId,right.ParentWorldId,StringComparison.Ordinal)&&string.Equals(left.DerivedWorldId,right.DerivedWorldId,StringComparison.Ordinal)&&string.Equals(left.DerivedSeedFingerprint,right.DerivedSeedFingerprint,StringComparison.Ordinal);
}
