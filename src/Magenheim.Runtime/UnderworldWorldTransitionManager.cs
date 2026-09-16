using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime orchestration boundary for an already-authorized Underworld transition.
/// Core owns transition legality and state mutation. The host adapter owns persistence,
/// physical-context observation and player placement.
/// </summary>
internal sealed class UnderworldWorldTransitionManager
{
    private readonly IUnderworldTransitionHost _host;
    private readonly ManualLogSource _log;

    internal UnderworldWorldTransitionManager(IUnderworldTransitionHost host, ManualLogSource log)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal UnderworldPlayerLayerState ExecutePrepared(UnderworldPlayerLayerState preparedState, UnderworldWorldIdentity identity, string operationId, string authorityFingerprint)
    {
        if (preparedState is null) throw new ArgumentNullException(nameof(preparedState));
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        UnderworldTransitionRules.ValidatePersistedState(preparedState, identity);
        var active = preparedState.ActiveTransition ?? throw new InvalidOperationException("Underworld runtime transition requires a prepared Core transition.");
        if (active.Phase != UnderworldTransitionPhase.Prepared) throw new InvalidOperationException("Underworld runtime transition may start only from Prepared state.");
        ValidateOperation(active, operationId, authorityFingerprint);
        if (!UnderworldProgressionAuthority.IsUnlocked)
            throw new InvalidOperationException("Deep Gate transition rejected: the Nowhere King has not been defeated in this world.");

        _host.Persist(preparedState, identity);
        try
        {
            return ContinuePrepared(preparedState, identity, operationId, authorityFingerprint);
        }
        catch (UnderworldPhysicalWorldSwitchRequiredException)
        {
            // This is not transition failure. Prepared state is deliberately left durable so the
            // physical loader can change worlds and resume the same operation after admission.
            _log.LogInfo($"Underworld transition '{operationId}' is durably prepared and awaiting its physical target world.");
            throw;
        }
        catch (Exception transitionFailure)
        {
            _log.LogWarning($"Underworld transition '{operationId}' failed; attempting source recovery: {transitionFailure.Message}");
            return Recover(preparedState, identity, operationId, authorityFingerprint, transitionFailure);
        }
    }

    internal UnderworldPlayerLayerState ResumeOrRecover(UnderworldPlayerLayerState persistedState, UnderworldWorldIdentity identity, string currentAuthorityFingerprint)
    {
        UnderworldTransitionRules.ValidatePersistedState(persistedState, identity);
        var active = persistedState.ActiveTransition;
        if (active is null) return persistedState;
        if (!string.Equals(active.AuthorityFingerprint, currentAuthorityFingerprint, StringComparison.Ordinal))
            return Recover(persistedState, identity, active.OperationId, active.AuthorityFingerprint, new InvalidOperationException("Gameplay authority changed while an Underworld transition was incomplete."));
        if (active.Phase == UnderworldTransitionPhase.RecoveryRequired)
            return RecoverMarked(persistedState, identity, active.OperationId, active.AuthorityFingerprint);
        if (active.Phase == UnderworldTransitionPhase.Prepared)
        {
            try { return ContinuePrepared(persistedState, identity, active.OperationId, active.AuthorityFingerprint); }
            catch (UnderworldPhysicalWorldSwitchRequiredException) { throw; }
            catch (Exception failure) { return Recover(persistedState, identity, active.OperationId, active.AuthorityFingerprint, failure); }
        }
        return Recover(persistedState, identity, active.OperationId, active.AuthorityFingerprint, new InvalidOperationException("Recovered an interrupted Underworld transition from a non-resumable phase."));
    }

    private UnderworldPlayerLayerState ContinuePrepared(UnderworldPlayerLayerState preparedState, UnderworldWorldIdentity identity, string operationId, string authorityFingerprint)
    {
        var active = preparedState.ActiveTransition ?? throw new InvalidOperationException("Prepared transition disappeared before continuation.");
        ValidateOperation(active, operationId, authorityFingerprint);
        _host.EnsureTargetContext(identity, active.TargetLayer, active.TargetAnchor);
        var targetReady = UnderworldTransitionRules.MarkTargetReady(preparedState, operationId, authorityFingerprint);
        _host.Persist(targetReady, identity);
        _host.PlacePlayer(identity, active.TargetLayer, active.TargetAnchor);
        if (!_host.ObservePlayerPlacement(identity, active.TargetLayer, active.TargetAnchor))
            throw new InvalidOperationException("Runtime did not observe both the requested world context and player placement at the Underworld transition target.");
        var committed = UnderworldTransitionRules.Commit(targetReady, operationId, authorityFingerprint);
        _host.Persist(committed, identity);
        _log.LogInfo($"Underworld transition '{operationId}' committed to {committed.CurrentLayer}.");
        return committed;
    }

    private static void ValidateOperation(UnderworldTransitionRecord active, string operationId, string authorityFingerprint)
    {
        if (!string.Equals(active.OperationId, operationId, StringComparison.Ordinal)) throw new InvalidOperationException("Underworld runtime operation id does not match the prepared Core transition.");
        if (!string.Equals(active.AuthorityFingerprint, authorityFingerprint, StringComparison.Ordinal)) throw new InvalidOperationException("Underworld runtime authority fingerprint drifted before execution.");
    }

    private UnderworldPlayerLayerState Recover(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity, string operationId, string authorityFingerprint, Exception failure)
    {
        var recoveryRequired = state.ActiveTransition?.Phase == UnderworldTransitionPhase.RecoveryRequired ? state : UnderworldTransitionRules.RequireRecovery(state, operationId, authorityFingerprint, failure.Message);
        _host.Persist(recoveryRequired, identity);
        return RecoverMarked(recoveryRequired, identity, operationId, authorityFingerprint);
    }

    private UnderworldPlayerLayerState RecoverMarked(UnderworldPlayerLayerState recoveryRequired, UnderworldWorldIdentity identity, string operationId, string authorityFingerprint)
    {
        var active = recoveryRequired.ActiveTransition ?? throw new InvalidOperationException("Recovery requires an active Underworld transition.");
        _host.EnsureTargetContext(identity, active.SourceLayer, active.SourceAnchor);
        _host.PlacePlayer(identity, active.SourceLayer, active.SourceAnchor);
        if (!_host.ObservePlayerPlacement(identity, active.SourceLayer, active.SourceAnchor)) throw new InvalidOperationException("Underworld recovery could not verify source world context and placement; recovery state remains persisted.");
        var recovered = UnderworldTransitionRules.RecoverToSource(recoveryRequired, operationId, authorityFingerprint);
        _host.Persist(recovered, identity);
        _log.LogInfo($"Underworld transition '{operationId}' recovered to {recovered.CurrentLayer}.");
        return recovered;
    }
}

internal interface IUnderworldTransitionHost
{
    void Persist(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity);
    void EnsureTargetContext(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor);
    void PlacePlayer(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor);
    bool ObservePlayerPlacement(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor);
}
