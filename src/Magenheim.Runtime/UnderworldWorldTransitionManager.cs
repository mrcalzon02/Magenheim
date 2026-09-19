using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>Runtime orchestration for durable transitions between Surface and the dedicated logical Underworld instance.</summary>
internal sealed class UnderworldWorldTransitionManager
{
    private readonly IUnderworldTransitionHost _host;
    private readonly UnderworldInstanceLifecycle _instanceLifecycle;
    private readonly ManualLogSource _log;

    internal UnderworldWorldTransitionManager(IUnderworldTransitionHost host, UnderworldInstanceLifecycle instanceLifecycle, ManualLogSource log)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _instanceLifecycle = instanceLifecycle ?? throw new ArgumentNullException(nameof(instanceLifecycle));
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
        if (!UnderworldProgressionAuthority.IsUnlocked) throw new InvalidOperationException("Deep Gate transition rejected: the Nowhere King has not been defeated in this world.");

        PrepareInstanceLifecycle(identity, active);
        _host.Persist(preparedState, identity);
        try { return ContinuePrepared(preparedState, identity, operationId, authorityFingerprint); }
        catch (Exception failure)
        {
            _log.LogWarning($"Underworld transition '{operationId}' failed; attempting source recovery: {failure.Message}");
            return Recover(preparedState, identity, operationId, authorityFingerprint, failure);
        }
    }

    internal UnderworldPlayerLayerState ResumeOrRecover(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity, string currentAuthorityFingerprint)
    {
        UnderworldTransitionRules.ValidatePersistedState(state, identity);
        var active = state.ActiveTransition;
        if (active is null)
        {
            RehydrateStableLifecycle(state, identity);
            return state;
        }
        ReconcileLifecycleForResume(identity, active);
        if (!string.Equals(active.AuthorityFingerprint, currentAuthorityFingerprint, StringComparison.Ordinal))
            return Recover(state, identity, active.OperationId, active.AuthorityFingerprint, new InvalidOperationException("Gameplay authority changed while an Underworld transition was incomplete."));
        if (active.Phase == UnderworldTransitionPhase.RecoveryRequired) return RecoverMarked(state, identity, active.OperationId, active.AuthorityFingerprint);
        if (active.Phase == UnderworldTransitionPhase.Prepared)
        {
            try { return ContinuePrepared(state, identity, active.OperationId, active.AuthorityFingerprint); }
            catch (Exception failure) { return Recover(state, identity, active.OperationId, active.AuthorityFingerprint, failure); }
        }
        return Recover(state, identity, active.OperationId, active.AuthorityFingerprint, new InvalidOperationException("Recovered an interrupted Underworld transition from a non-resumable phase."));
    }

    private UnderworldPlayerLayerState ContinuePrepared(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity, string operationId, string authorityFingerprint)
    {
        var active = state.ActiveTransition ?? throw new InvalidOperationException("Prepared transition disappeared before continuation.");
        ValidateOperation(active, operationId, authorityFingerprint);
        _host.EnsureTargetContext(identity, active.TargetLayer, active.TargetAnchor);
        var ready = UnderworldTransitionRules.MarkTargetReady(state, operationId, authorityFingerprint);
        _host.Persist(ready, identity);
        _host.PlacePlayer(identity, active.TargetLayer, active.TargetAnchor);
        if (!_host.ObservePlayerPlacement(identity, active.TargetLayer, active.TargetAnchor))
            throw new InvalidOperationException("Runtime did not observe both the requested world context and player placement at the Underworld transition target.");
        var committed = UnderworldTransitionRules.Commit(ready, operationId, authorityFingerprint);
        _host.Persist(committed, identity);
        CompleteInstanceTransition(identity, active.TargetLayer);
        _log.LogInfo($"Underworld transition '{operationId}' committed to {committed.CurrentLayer}.");
        return committed;
    }

    private void PrepareInstanceLifecycle(UnderworldWorldIdentity identity, UnderworldTransitionIntent active)
    {
        if (active.TargetLayer == UnderworldLayer.Underworld)
            _instanceLifecycle.EnsureAdmitted(identity);
    }

    private void RehydrateStableLifecycle(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity)
    {
        if (state.CurrentLayer == UnderworldLayer.Surface) return;
        if (state.CurrentLayer != UnderworldLayer.Underworld)
            throw new InvalidOperationException($"Cannot rehydrate unsupported stable Underworld layer '{state.CurrentLayer}'.");

        _instanceLifecycle.EnsureAdmitted(identity);
        _instanceLifecycle.EnsureActive(identity);
    }

    private void ReconcileLifecycleForResume(UnderworldWorldIdentity identity, UnderworldTransitionIntent active)
    {
        _instanceLifecycle.EnsureAdmitted(identity);
        if (active.SourceLayer == UnderworldLayer.Underworld)
            _instanceLifecycle.EnsureActive(identity);
    }

    private void CompleteInstanceTransition(UnderworldWorldIdentity identity, UnderworldLayer targetLayer)
    {
        if (targetLayer == UnderworldLayer.Underworld)
            _instanceLifecycle.EnsureActive(identity);
    }

    private static void ValidateOperation(UnderworldTransitionIntent active, string operationId, string authorityFingerprint)
    {
        if (!string.Equals(active.OperationId, operationId, StringComparison.Ordinal)) throw new InvalidOperationException("Underworld runtime operation id does not match the prepared Core transition.");
        if (!string.Equals(active.AuthorityFingerprint, authorityFingerprint, StringComparison.Ordinal)) throw new InvalidOperationException("Underworld runtime authority fingerprint drifted before execution.");
    }

    private UnderworldPlayerLayerState Recover(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity, string operationId, string authorityFingerprint, Exception failure)
    {
        var required = state.ActiveTransition?.Phase == UnderworldTransitionPhase.RecoveryRequired ? state : UnderworldTransitionRules.RequireRecovery(state, operationId, authorityFingerprint, failure.Message);
        _host.Persist(required, identity);
        return RecoverMarked(required, identity, operationId, authorityFingerprint);
    }

    private UnderworldPlayerLayerState RecoverMarked(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity, string operationId, string authorityFingerprint)
    {
        var active = state.ActiveTransition ?? throw new InvalidOperationException("Recovery requires an active Underworld transition.");
        _host.EnsureTargetContext(identity, active.SourceLayer, active.SourceAnchor);
        _host.PlacePlayer(identity, active.SourceLayer, active.SourceAnchor);
        if (!_host.ObservePlayerPlacement(identity, active.SourceLayer, active.SourceAnchor)) throw new InvalidOperationException("Underworld recovery could not verify source world context and placement; recovery state remains persisted.");
        var recovered = UnderworldTransitionRules.RecoverToSource(state, operationId, authorityFingerprint);
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
