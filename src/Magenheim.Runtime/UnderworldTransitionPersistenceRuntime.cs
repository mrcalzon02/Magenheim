using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal interface IUnderworldTransitionPlacementHost{void EnsureTargetContext(UnderworldWorldIdentity identity,UnderworldLayer layer,UnderworldAnchor anchor);void PlacePlayer(UnderworldWorldIdentity identity,UnderworldLayer layer,UnderworldAnchor anchor);bool ObservePlayerPlacement(UnderworldWorldIdentity identity,UnderworldLayer layer,UnderworldAnchor anchor);}
internal sealed class StoredUnderworldTransitionHost:IUnderworldTransitionHost
{
    private readonly UnderworldTransitionStateStore _store;private readonly IUnderworldTransitionPlacementHost _placement;
    internal StoredUnderworldTransitionHost(UnderworldTransitionStateStore store,IUnderworldTransitionPlacementHost placement){_store=store??throw new ArgumentNullException(nameof(store));_placement=placement??throw new ArgumentNullException(nameof(placement));}
    public void Persist(UnderworldPlayerLayerState state,UnderworldWorldIdentity identity)=>_store.Save(state,identity);public void EnsureTargetContext(UnderworldWorldIdentity identity,UnderworldLayer layer,UnderworldAnchor anchor)=>_placement.EnsureTargetContext(identity,layer,anchor);public void PlacePlayer(UnderworldWorldIdentity identity,UnderworldLayer layer,UnderworldAnchor anchor)=>_placement.PlacePlayer(identity,layer,anchor);public bool ObservePlayerPlacement(UnderworldWorldIdentity identity,UnderworldLayer layer,UnderworldAnchor anchor)=>_placement.ObservePlayerPlacement(identity,layer,anchor);
}
internal enum UnderworldRecoveryLoadResult{Missing,Stable,PendingWorldSwitch,Recovered,Failed}
internal sealed class UnderworldTransitionRecoveryRuntime
{
    private readonly UnderworldTransitionStateStore _store;private readonly UnderworldWorldTransitionManager _manager;private readonly ManualLogSource _log;
    internal UnderworldTransitionRecoveryRuntime(UnderworldTransitionStateStore store,UnderworldWorldTransitionManager manager,ManualLogSource log){_store=store??throw new ArgumentNullException(nameof(store));_manager=manager??throw new ArgumentNullException(nameof(manager));_log=log??throw new ArgumentNullException(nameof(log));}
    internal UnderworldRecoveryLoadResult LoadAndResume(string playerId,UnderworldWorldIdentity identity,string currentAuthorityFingerprint,out UnderworldPlayerLayerState? state,out string diagnostic)
    {
        state=null;if(!_store.TryLoad(playerId,identity,out var persisted,out diagnostic))return diagnostic.StartsWith("No persisted",StringComparison.Ordinal)?UnderworldRecoveryLoadResult.Missing:UnderworldRecoveryLoadResult.Failed;
        if(persisted is null){diagnostic="Transition-state store reported success without a decoded state.";_log.LogError(diagnostic);return UnderworldRecoveryLoadResult.Failed;}
        try
        {
            var active=persisted.ActiveTransition;state=_manager.ResumeOrRecover(persisted,identity,currentAuthorityFingerprint);
            if(active is null){diagnostic="Loaded stable Underworld layer state.";return UnderworldRecoveryLoadResult.Stable;}
            diagnostic=state.ActiveTransition is null?"Completed or recovered persisted Underworld transition after physical-world admission.":"Persisted Underworld transition remains active.";return UnderworldRecoveryLoadResult.Recovered;
        }
        catch(UnderworldPhysicalWorldSwitchRequiredException switchRequired)
        {
            state=persisted;diagnostic=$"Persisted Underworld transition is valid and awaits physical world switch {switchRequired.CurrentLayer} -> {switchRequired.TargetLayer}.";_log.LogInfo(diagnostic);return UnderworldRecoveryLoadResult.PendingWorldSwitch;
        }
        catch(Exception exception){state=null;diagnostic="Underworld persisted-state resume/recovery failed closed: "+exception.Message;_log.LogError(diagnostic);return UnderworldRecoveryLoadResult.Failed;}
    }
}
