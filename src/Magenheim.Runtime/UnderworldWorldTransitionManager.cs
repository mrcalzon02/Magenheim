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

        PrepareInstanceLifecycle(identity, preparedState.PlayerId, active);
        _host.Persist(preparedState, identity);
        try { return ContinuePrepared(preparedState, identity, operationId, authorityFingerprint); }
        catch (Exception failure)
        {
            MarkLifecycleFault(identity, failure);
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
        ReconcileLifecycleForResume(state, identity, active);
        if (!string.Equals(active.AuthorityFingerprint, currentAuthorityFingerprint, StringComparison.Ordinal))
            return Recover(state, identity, active.OperationId, active.AuthorityFingerprint, new InvalidOperationException("Gameplay authority changed while an Underworld transition was incomplete."));
        if (active.Phase == UnderworldTransitionPhase.RecoveryRequired) return RecoverMarked(state, identity, active.OperationId, active.AuthorityFingerprint);
        if (active.Phase == UnderworldTransitionPhase.Prepared)
        {
            try { return ContinuePrepared(state, identity, active.OperationId, active.AuthorityFingerprint); }
            catch (Exception failure) { MarkLifecycleFault(identity, failure); return Recover(state, identity, active.OperationId, active.AuthorityFingerprint, failure); }
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
        CompleteInstanceTransition(identity, state.PlayerId, active.TargetLayer);
        _log.LogInfo($"Underworld transition '{operationId}' committed to {committed.CurrentLayer}.");
        return committed;
    }

    private void PrepareInstanceLifecycle(UnderworldWorldIdentity identity, string playerId, UnderworldTransitionIntent active)
    {
        if (active.TargetLayer == UnderworldLayer.Underworld)
        {
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Inactive) _instanceLifecycle.BeginAdmission(identity);
            else if (_instanceLifecycle.Phase == UnderworldInstancePhase.Active)
            {
                RequireAdmittedIdentity(identity);
                return;
            }
            else if (_instanceLifecycle.Phase != UnderworldInstancePhase.Admitting) throw new InvalidOperationException($"Underworld admission cannot start while instance lifecycle is {_instanceLifecycle.Phase}.");
            return;
        }
        if (active.SourceLayer == UnderworldLayer.Underworld && active.TargetLayer == UnderworldLayer.Surface)
        {
            EnsureActiveOccupant(identity, playerId);
            if (_instanceLifecycle.IsLastOccupant(playerId)) _instanceLifecycle.BeginRelease(identity);
        }
    }

    private void RehydrateStableLifecycle(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity)
    {
        if (state.CurrentLayer == UnderworldLayer.Surface)
        {
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Active && _instanceLifecycle.ContainsOccupant(state.PlayerId))
                _instanceLifecycle.RemoveOccupant(identity, state.PlayerId);
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Active && _instanceLifecycle.OccupantCount == 0)
            {
                _instanceLifecycle.BeginRelease(identity);
                _instanceLifecycle.CompleteRelease(identity);
            }
            return;
        }

        if (state.CurrentLayer != UnderworldLayer.Underworld)
            throw new InvalidOperationException($"Cannot rehydrate unsupported stable Underworld layer '{state.CurrentLayer}'.");

        if (_instanceLifecycle.Phase == UnderworldInstancePhase.Inactive)
        {
            _instanceLifecycle.BeginAdmission(identity);
            _instanceLifecycle.MarkActive(identity);
        }
        else if (_instanceLifecycle.Phase != UnderworldInstancePhase.Active)
            throw new InvalidOperationException($"Stable Underworld state conflicts with instance lifecycle phase {_instanceLifecycle.Phase}.");
        else RequireAdmittedIdentity(identity);

        _instanceLifecycle.RegisterOccupant(identity, state.PlayerId);
    }

    private void ReconcileLifecycleForResume(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity, UnderworldTransitionIntent active)
    {
        if (_instanceLifecycle.Phase == UnderworldInstancePhase.Inactive)
        {
            _instanceLifecycle.BeginAdmission(identity);
            if (active.SourceLayer == UnderworldLayer.Underworld) _instanceLifecycle.MarkActive(identity);
        }
        else RequireAdmittedIdentity(identity);

        if (active.SourceLayer == UnderworldLayer.Underworld)
        {
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Admitting) _instanceLifecycle.MarkActive(identity);
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Active)
            {
                _instanceLifecycle.RegisterOccupant(identity, state.PlayerId);
                if (active.TargetLayer == UnderworldLayer.Surface && _instanceLifecycle.IsLastOccupant(state.PlayerId)) _instanceLifecycle.BeginRelease(identity);
            }
        }
    }

    private void CompleteInstanceTransition(UnderworldWorldIdentity identity, string playerId, UnderworldLayer targetLayer)
    {
        if (targetLayer == UnderworldLayer.Underworld)
        {
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Admitting) _instanceLifecycle.MarkActive(identity);
            if (_instanceLifecycle.Phase != UnderworldInstancePhase.Active) throw new InvalidOperationException($"Underworld entry committed while lifecycle is {_instanceLifecycle.Phase}.");
            _instanceLifecycle.RegisterOccupant(identity, playerId);
            return;
        }

        if (_instanceLifecycle.Phase == UnderworldInstancePhase.Releasing)
        {
            _instanceLifecycle.RemoveOccupant(identity, playerId);
            _instanceLifecycle.CompleteRelease(identity);
        }
        else if (_instanceLifecycle.Phase == UnderworldInstancePhase.Active)
            _instanceLifecycle.RemoveOccupant(identity, playerId);
    }

    private void MarkLifecycleFault(UnderworldWorldIdentity identity, Exception failure)
    {
        if (_instanceLifecycle.Phase != UnderworldInstancePhase.Inactive && _instanceLifecycle.Phase != UnderworldInstancePhase.Faulted)
            _instanceLifecycle.MarkFaulted(identity, failure.Message);
    }

    private void RestoreLifecycleAfterRecovery(UnderworldWorldIdentity identity, string playerId, UnderworldLayer sourceLayer)
    {
        if (sourceLayer == UnderworldLayer.Underworld)
        {
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Inactive) _instanceLifecycle.BeginAdmission(identity);
            if (_instanceLifecycle.Phase == UnderworldInstancePhase.Admitting) _instanceLifecycle.MarkActive(identity);
            else if (_instanceLifecycle.Phase != UnderworldInstancePhase.Active) _instanceLifecycle.RestoreActive(identity);
            _instanceLifecycle.RegisterOccupant(identity, playerId);
            return;
        }

        if (_instanceLifecycle.Phase == UnderworldInstancePhase.Faulted) _instanceLifecycle.RestoreActive(identity);
        if (_instanceLifecycle.Phase == UnderworldInstancePhase.Active && _instanceLifecycle.ContainsOccupant(playerId))
            _instanceLifecycle.RemoveOccupant(identity, playerId);
        if (_instanceLifecycle.Phase == UnderworldInstancePhase.Active && _instanceLifecycle.OccupantCount == 0)
        {
            _instanceLifecycle.BeginRelease(identity);
            _instanceLifecycle.CompleteRelease(identity);
        }
    }

    private void EnsureActiveOccupant(UnderworldWorldIdentity identity, string playerId)
    {
        if (_instanceLifecycle.Phase == UnderworldInstancePhase.Inactive)
        {
            _instanceLifecycle.BeginAdmission(identity);
            _instanceLifecycle.MarkActive(identity);
        }
        if (_instanceLifecycle.Phase != UnderworldInstancePhase.Active)
            throw new InvalidOperationException($"Underworld return requires an active lifecycle, not {_instanceLifecycle.Phase}.");
        RequireAdmittedIdentity(identity);
        _instanceLifecycle.RegisterOccupant(identity, playerId);
    }

    private void RequireAdmittedIdentity(UnderworldWorldIdentity identity)
    {
        var admitted = _instanceLifecycle.Identity ?? throw new InvalidOperationException("Active Underworld lifecycle has no admitted identity.");
        if (!SameIdentity(admitted, identity)) throw new InvalidOperationException("Underworld transition belongs to a different admitted instance identity.");
    }

    private static bool SameIdentity(UnderworldWorldIdentity left, UnderworldWorldIdentity right) =>
        string.Equals(left.ParentWorldId, right.ParentWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedWorldId, right.DerivedWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedSeedFingerprint, right.DerivedSeedFingerprint, StringComparison.Ordinal);

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
        RestoreLifecycleAfterRecovery(identity, state.PlayerId, active.SourceLayer);
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
