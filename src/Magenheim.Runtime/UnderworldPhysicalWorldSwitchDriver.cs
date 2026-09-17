using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Condenses the runtime handoff lifecycle around the persisted transition. It deliberately does
/// not guess at Valheim menu/session internals: an engine adapter receives an exact manifest-backed
/// target save, while this driver owns idempotency, admission verification and resume semantics.
/// </summary>
internal sealed class UnderworldPhysicalWorldSwitchDriver
{
    private readonly UnderworldPhysicalWorldSwitchCoordinator _coordinator;
    private readonly UnderworldTransitionRecoveryRuntime _recovery;
    private readonly ManualLogSource _log;
    private UnderworldPhysicalWorldSwitchRequest? _inFlight;
    private bool _dispatchIssued;

    internal UnderworldPhysicalWorldSwitchDriver(UnderworldPhysicalWorldSwitchCoordinator coordinator,UnderworldTransitionRecoveryRuntime recovery,ManualLogSource log)
    { _coordinator=coordinator??throw new ArgumentNullException(nameof(coordinator));_recovery=recovery??throw new ArgumentNullException(nameof(recovery));_log=log??throw new ArgumentNullException(nameof(log)); }

    internal bool TryPrepare(UnderworldPlayerLayerState state,UnderworldWorldIdentity identity,out UnderworldPhysicalWorldSwitchRequest? request,out string diagnostic)
    {
        request=null;
        try
        {
            var resolved=_coordinator.ResolvePending(state,identity);
            if(_inFlight is not null)
            {
                if(SameRequest(_inFlight,resolved)){request=_inFlight;diagnostic=$"Physical world switch to '{resolved.TargetSaveName}' is already in flight.";return true;}
                diagnostic=$"Refusing overlapping physical world switch '{_inFlight.TargetSaveName}' -> '{resolved.TargetSaveName}'.";return false;
            }
            _inFlight=resolved;_dispatchIssued=false;request=resolved;diagnostic=$"Prepared physical world switch to '{resolved.TargetSaveName}'.";return true;
        }
        catch(Exception exception){diagnostic="Physical world-switch preparation failed closed: "+exception.Message;_log.LogError(diagnostic);return false;}
    }

    /// <summary>
    /// Dispatches the already prepared manifest-backed request through the one engine adapter.
    /// A successful adapter invocation is issued at most once for a given in-process handoff;
    /// durable transition state, not this volatile flag, remains the restart authority.
    /// </summary>
    internal bool TryDispatchPending(IUnderworldPhysicalWorldLoader loader,out string diagnostic)
    {
        if(loader is null)throw new ArgumentNullException(nameof(loader));
        var request=_inFlight;
        if(request is null){diagnostic="No prepared Underworld physical world switch is available for loader dispatch.";return false;}
        var znet=ZNet.instance;
        if(znet is null||!znet.IsServer()){diagnostic="Physical Underworld loader dispatch rejected: only the active Valheim server/host may change the hosted save.";return false;}
        if(_dispatchIssued){diagnostic=$"Physical world loader dispatch for '{request.TargetSaveName}' was already issued; awaiting target-session admission.";return true;}
        try
        {
            if(!loader.TryLoad(request,out var loaderDiagnostic)){diagnostic=$"Physical world loader rejected '{request.TargetSaveName}': {loaderDiagnostic}";return false;}
            _dispatchIssued=true;
            diagnostic=$"Physical world loader accepted '{request.TargetSaveName}': {loaderDiagnostic}";
            _log.LogInfo(diagnostic);
            return true;
        }
        catch(Exception exception)
        {
            diagnostic=$"Physical world loader failed closed before handoff to '{request.TargetSaveName}': {exception.Message}";
            _log.LogError(diagnostic);
            return false;
        }
    }

    /// <summary>Called only after Valheim reports a newly admitted world/session.</summary>
    internal UnderworldRecoveryLoadResult OnWorldAdmitted(string currentAuthorityFingerprint,out UnderworldPlayerLayerState? state,out string diagnostic)
    {
        state=null;var request=_inFlight;if(request is null){diagnostic="No Magenheim physical world switch is awaiting admission.";return UnderworldRecoveryLoadResult.Stable;}
        if(!_coordinator.IsTargetAdmitted(request,out diagnostic))return UnderworldRecoveryLoadResult.PendingWorldSwitch;
        var result=_recovery.LoadAndResume(request.PlayerId,request.Identity,currentAuthorityFingerprint,out state,out diagnostic);
        if(result is UnderworldRecoveryLoadResult.Stable or UnderworldRecoveryLoadResult.Recovered){_inFlight=null;_dispatchIssued=false;}
        return result;
    }

    /// <summary>
    /// Reconstructs volatile handoff ownership from the durable transaction after a process restart.
    /// The persisted transition remains authoritative; no second request record is serialized.
    /// </summary>
    internal UnderworldRecoveryLoadResult ReconcileAdmittedSession(string playerId,UnderworldWorldIdentity identity,string currentAuthorityFingerprint,out UnderworldPlayerLayerState? state,out UnderworldPhysicalWorldSwitchRequest? pendingRequest,out string diagnostic)
    {
        pendingRequest=null;
        if(_inFlight is not null)
        {
            var admitted=OnWorldAdmitted(currentAuthorityFingerprint,out state,out diagnostic);
            if(admitted==UnderworldRecoveryLoadResult.PendingWorldSwitch)pendingRequest=_inFlight;
            return admitted;
        }

        var result=_recovery.LoadAndResume(playerId,identity,currentAuthorityFingerprint,out state,out diagnostic);
        if(result!=UnderworldRecoveryLoadResult.PendingWorldSwitch||state is null)return result;
        if(!TryPrepare(state,identity,out pendingRequest,out var prepareDiagnostic))
        {
            diagnostic=$"{diagnostic} Failed to reconstruct physical handoff: {prepareDiagnostic}";
            return UnderworldRecoveryLoadResult.Failed;
        }
        diagnostic=$"{diagnostic} Reconstructed manifest-backed physical handoff to '{pendingRequest!.TargetSaveName}'.";
        return UnderworldRecoveryLoadResult.PendingWorldSwitch;
    }

    internal void ResetForWorldUnload()
    {
        // Intentionally retain _inFlight and _dispatchIssued across the source-world unload. Reissuing
        // the engine load while teardown is in progress is more dangerous than waiting for admission.
    }

    internal void AbandonForProcessShutdown(){_inFlight=null;_dispatchIssued=false;}

    private static bool SameRequest(UnderworldPhysicalWorldSwitchRequest left,UnderworldPhysicalWorldSwitchRequest right)=>
        string.Equals(left.PlayerId,right.PlayerId,StringComparison.Ordinal)&&left.TargetLayer==right.TargetLayer&&
        string.Equals(left.TargetSaveName,right.TargetSaveName,StringComparison.Ordinal)&&
        string.Equals(left.Identity.DerivedSeedFingerprint,right.Identity.DerivedSeedFingerprint,StringComparison.Ordinal);
}

/// <summary>
/// Narrow engine boundary. Implementations may select/load the exact requested Valheim save but may
/// not derive another target, mutate transition persistence, place the player, or commit the crossing.
/// </summary>
internal interface IUnderworldPhysicalWorldLoader
{
    bool TryLoad(UnderworldPhysicalWorldSwitchRequest request,out string diagnostic);
}
