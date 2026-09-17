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
            _inFlight=resolved;request=resolved;diagnostic=$"Prepared physical world switch to '{resolved.TargetSaveName}'.";return true;
        }
        catch(Exception exception){diagnostic="Physical world-switch preparation failed closed: "+exception.Message;_log.LogError(diagnostic);return false;}
    }

    /// <summary>Called only after Valheim reports a newly admitted world/session.</summary>
    internal UnderworldRecoveryLoadResult OnWorldAdmitted(string currentAuthorityFingerprint,out UnderworldPlayerLayerState? state,out string diagnostic)
    {
        state=null;var request=_inFlight;if(request is null){diagnostic="No Magenheim physical world switch is awaiting admission.";return UnderworldRecoveryLoadResult.Stable;}
        if(!_coordinator.IsTargetAdmitted(request,out diagnostic))return UnderworldRecoveryLoadResult.PendingWorldSwitch;
        var result=_recovery.LoadAndResume(request.PlayerId,request.Identity,currentAuthorityFingerprint,out state,out diagnostic);
        if(result is UnderworldRecoveryLoadResult.Stable or UnderworldRecoveryLoadResult.Recovered)_inFlight=null;
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
        // Intentionally retain _inFlight across the source-world unload. The request is cleared only
        // after the target session proves its manifest identity and recovery consumes the transaction.
    }

    internal void AbandonForProcessShutdown()=>_inFlight=null;

    private static bool SameRequest(UnderworldPhysicalWorldSwitchRequest left,UnderworldPhysicalWorldSwitchRequest right)=>
        string.Equals(left.PlayerId,right.PlayerId,StringComparison.Ordinal)&&left.TargetLayer==right.TargetLayer&&
        string.Equals(left.TargetSaveName,right.TargetSaveName,StringComparison.Ordinal)&&
        string.Equals(left.Identity.DerivedSeedFingerprint,right.Identity.DerivedSeedFingerprint,StringComparison.Ordinal);
}

/// <summary>Engine boundary implemented by the Valheim session hook; policy remains in the driver.</summary>
internal interface IUnderworldPhysicalWorldLoader
{
    bool TryLoad(UnderworldPhysicalWorldSwitchRequest request,out string diagnostic);
}
