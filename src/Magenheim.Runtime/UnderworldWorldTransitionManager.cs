using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime orchestration boundary for an already-authorized Underworld transition.
/// Core owns transition legality and state mutation. The host adapter owns persistence,
/// derived-context initialization and observed player placement. No Valheim world state is
/// mutated directly from this class, keeping the eventual game-version-specific binding thin.
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

    internal UnderworldPlayerLayerState ExecutePrepared(
        UnderworldPlayerLayerState preparedState,
        UnderworldWorldIdentity identity,
        string operationId,
        string authorityFingerprint)
    {
        if (preparedState is null) throw new ArgumentNullException(nameof(preparedState));
        if (identity is null) throw new ArgumentNullException(nameof(identity));

        UnderworldTransitionRules.ValidatePersistedState(preparedState, identity);
        var active = preparedState.ActiveTransition
            ?? throw new InvalidOperationException("Underworld runtime transition requires a prepared Core transition.");
        if (active.Phase != UnderworldTransitionPhase.Prepared)
            throw new InvalidOperationException("Underworld runtime transition may start only from Prepared state.");
        if (!string.Equals(active.OperationId, operationId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld runtime operation id does not match the prepared Core transition.");
        if (!string.Equals(active.AuthorityFingerprint, authorityFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld runtime authority fingerprint drifted before execution.");

        // Persist the recoverable source state before target initialization. A crash or disconnect
        // after this point therefore leaves enough authority to recover rather than strand a player.
        _host.Persist(preparedState, identity);

        try
        {
            _host.EnsureTargetContext(identity, active.TargetLayer, active.TargetAnchor);

            var targetReady = UnderworldTransitionRules.MarkTargetReady(
                preparedState,
                operationId,
                authorityFingerprint);
            _host.Persist(targetReady, identity);

            _host.PlacePlayer(active.TargetLayer, active.TargetAnchor);
            if (!_host.ObservePlayerPlacement(active.TargetLayer, active.TargetAnchor))
                throw new InvalidOperationException("Runtime did not observe the player at the requested Underworld transition target.");

            var committed = UnderworldTransitionRules.Commit(
                targetReady,
                operationId,
                authorityFingerprint);
            _host.Persist(committed, identity);
            _log.LogInfo($"Underworld transition '{operationId}' committed to {committed.CurrentLayer}.");
            return committed;
        }
        catch (Exception transitionFailure)
        {
            _log.LogWarning($"Underworld transition '{operationId}' failed; attempting source recovery: {transitionFailure.Message}");
            return Recover(preparedState, identity, operationId, authorityFingerprint, transitionFailure);
        }
    }

    internal UnderworldPlayerLayerState ResumeOrRecover(
        UnderworldPlayerLayerState persistedState,
        UnderworldWorldIdentity identity,
        string currentAuthorityFingerprint)
    {
        UnderworldTransitionRules.ValidatePersistedState(persistedState, identity);
        var active = persistedState.ActiveTransition;
        if (active is null)
            return persistedState;

        if (!string.Equals(active.AuthorityFingerprint, currentAuthorityFingerprint, StringComparison.Ordinal))
        {
            return Recover(
                persistedState,
                identity,
                active.OperationId,
                active.AuthorityFingerprint,
                new InvalidOperationException("Gameplay authority changed while an Underworld transition was incomplete."));
        }

        if (active.Phase == UnderworldTransitionPhase.RecoveryRequired)
            return RecoverMarked(persistedState, identity, active.OperationId, active.AuthorityFingerprint);

        // Prepared and TargetReady snapshots are deliberately recovered rather than guessed forward.
        // The runtime cannot prove whether an interrupted placement completed, so source is the only
        // deterministic safe destination.
        return Recover(
            persistedState,
            identity,
            active.OperationId,
            active.AuthorityFingerprint,
            new InvalidOperationException("Recovered an interrupted Underworld transition from persistent state."));
    }

    private UnderworldPlayerLayerState Recover(
        UnderworldPlayerLayerState state,
        UnderworldWorldIdentity identity,
        string operationId,
        string authorityFingerprint,
        Exception failure)
    {
        var recoveryRequired = state.ActiveTransition?.Phase == UnderworldTransitionPhase.RecoveryRequired
            ? state
            : UnderworldTransitionRules.RequireRecovery(
                state,
                operationId,
                authorityFingerprint,
                failure.Message);
        _host.Persist(recoveryRequired, identity);
        return RecoverMarked(recoveryRequired, identity, operationId, authorityFingerprint);
    }

    private UnderworldPlayerLayerState RecoverMarked(
        UnderworldPlayerLayerState recoveryRequired,
        UnderworldWorldIdentity identity,
        string operationId,
        string authorityFingerprint)
    {
        var active = recoveryRequired.ActiveTransition
            ?? throw new InvalidOperationException("Recovery requires an active Underworld transition.");

        _host.EnsureTargetContext(identity, active.SourceLayer, active.SourceAnchor);
        _host.PlacePlayer(active.SourceLayer, active.SourceAnchor);
        if (!_host.ObservePlayerPlacement(active.SourceLayer, active.SourceAnchor))
            throw new InvalidOperationException("Underworld recovery could not verify placement at the source anchor; recovery state remains persisted.");

        var recovered = UnderworldTransitionRules.RecoverToSource(
            recoveryRequired,
            operationId,
            authorityFingerprint);
        _host.Persist(recovered, identity);
        _log.LogInfo($"Underworld transition '{operationId}' recovered to {recovered.CurrentLayer}.");
        return recovered;
    }
}

/// <summary>
/// Minimal game-version-specific seam required by Underworld transition orchestration.
/// A Valheim binding must prove each operation from observed runtime state rather than assuming
/// a load/teleport call succeeded.
/// </summary>
internal interface IUnderworldTransitionHost
{
    void Persist(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity);
    void EnsureTargetContext(UnderworldWorldIdentity identity, UnderworldLayer layer, UnderworldAnchor anchor);
    void PlacePlayer(UnderworldLayer layer, UnderworldAnchor anchor);
    bool ObservePlayerPlacement(UnderworldLayer layer, UnderworldAnchor anchor);
}
