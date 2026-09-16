using System;
using System.IO;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldRuntimeServices
{
    private readonly ValheimLogicalUnderworldWorldContextController _worldContext;
    private UnderworldRuntimeServices(UnderworldSpatialDomainDefinition spatialDomain,UnderworldTransitionStateStore stateStore,UnderworldWorldPairManifestStore worldPairStore,ValheimLogicalUnderworldWorldContextController worldContext,ValheimUnderworldTransitionPlacementHost placementHost,StoredUnderworldTransitionHost transitionHost,UnderworldWorldTransitionManager transitionManager,UnderworldTransitionRecoveryRuntime recoveryRuntime)
    {SpatialDomain=spatialDomain;StateStore=stateStore;WorldPairStore=worldPairStore;_worldContext=worldContext;PlacementHost=placementHost;TransitionHost=transitionHost;TransitionManager=transitionManager;RecoveryRuntime=recoveryRuntime;}
    internal UnderworldSpatialDomainDefinition SpatialDomain{get;} internal UnderworldTransitionStateStore StateStore{get;} internal UnderworldWorldPairManifestStore WorldPairStore{get;} internal ValheimUnderworldTransitionPlacementHost PlacementHost{get;} internal StoredUnderworldTransitionHost TransitionHost{get;} internal UnderworldWorldTransitionManager TransitionManager{get;} internal UnderworldTransitionRecoveryRuntime RecoveryRuntime{get;}
    internal static UnderworldRuntimeServices Create(string pluginConfigDirectory,ManualLogSource log)
    {
        if(string.IsNullOrWhiteSpace(pluginConfigDirectory))throw new ArgumentException("Plugin config directory is required for Underworld persistence.",nameof(pluginConfigDirectory));if(log is null)throw new ArgumentNullException(nameof(log));
        var root=Path.Combine(Path.GetFullPath(pluginConfigDirectory),"Magenheim");var spatialDomain=UnderworldSpatialDomain.CreateDefault();var stateStore=new UnderworldTransitionStateStore(Path.Combine(root,"underworld-transitions"),log);var worldPairStore=new UnderworldWorldPairManifestStore(Path.Combine(root,"underworld-world-pairs"),log);var worldContext=new ValheimLogicalUnderworldWorldContextController(log);var placementHost=new ValheimUnderworldTransitionPlacementHost(worldContext,spatialDomain,log);var transitionHost=new StoredUnderworldTransitionHost(stateStore,placementHost);var transitionManager=new UnderworldWorldTransitionManager(transitionHost,log);var recoveryRuntime=new UnderworldTransitionRecoveryRuntime(stateStore,transitionManager,log);
        return new UnderworldRuntimeServices(spatialDomain,stateStore,worldPairStore,worldContext,placementHost,transitionHost,transitionManager,recoveryRuntime);
    }
    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out UnderworldLayer layer,out string playerId,out string diagnostic)
    {
        if(!UnderworldRuntimeIdentityResolver.TryResolveLocalSession(WorldPairStore,out identity,out layer,out playerId,out diagnostic)||identity is null)return false;
        if(layer==UnderworldLayer.Surface)WorldPairStore.EnsureManifest(identity);
        return true;
    }
    internal bool TryResolveLocalSession(out UnderworldWorldIdentity? identity,out string playerId,out string diagnostic)=>TryResolveLocalSession(out identity,out _,out playerId,out diagnostic);
    internal void ResetForWorldUnload()=>_worldContext.ResetForWorldUnload();
}
