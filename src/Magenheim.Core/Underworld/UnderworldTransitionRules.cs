using System;
using System.Linq;
using Magenheim.Core.DarkThrone;

namespace Magenheim.Core.Underworld;

public enum UnderworldLayer
{
    Surface,
    Underworld,
}

public enum UnderworldTransitionDirection
{
    Enter,
    Return,
}

public enum UnderworldTransitionPhase
{
    Prepared,
    TargetReady,
    RecoveryRequired,
}

public sealed record UnderworldAnchor(
    string WorldId,
    double X,
    double Y,
    double Z,
    float HeadingDegrees);

public sealed record UnderworldTransitionIntent(
    string OperationId,
    UnderworldTransitionDirection Direction,
    UnderworldLayer SourceLayer,
    UnderworldLayer TargetLayer,
    UnderworldAnchor SourceAnchor,
    UnderworldAnchor TargetAnchor,
    string AuthorityFingerprint,
    UnderworldTransitionPhase Phase,
    string Diagnostic);

public sealed record UnderworldPlayerLayerState(
    int SchemaVersion,
    string PlayerId,
    UnderworldLayer CurrentLayer,
    string CurrentWorldId,
    UnderworldAnchor? SurfaceReturnAnchor,
    UnderworldTransitionIntent? ActiveTransition);

/// <summary>
/// Pure server-side transition transaction authority. Runtime adapters perform world loading,
/// placement, persistence and RPC work, but may only advance a transition through these rules.
/// Incomplete transitions retain a recoverable source anchor until commit succeeds.
/// </summary>
public static class UnderworldTransitionRules
{
    public const int CurrentSchemaVersion = 1;

    public static UnderworldPlayerLayerState CreateSurfaceState(
        string playerId,
        UnderworldWorldIdentity identity,
        UnderworldAnchor currentAnchor)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var player = RequireText(playerId, nameof(playerId));
        var anchor = ValidateAnchor(currentAnchor, nameof(currentAnchor));
        if (!string.Equals(anchor.WorldId, identity.ParentWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Initial surface anchor does not belong to the parent world.");

        return new UnderworldPlayerLayerState(
            CurrentSchemaVersion,
            player,
            UnderworldLayer.Surface,
            identity.ParentWorldId,
            null,
            null);
    }

    public static UnderworldPlayerLayerState BeginEnter(
        UnderworldPlayerLayerState state,
        UnderworldWorldIdentity identity,
        DarkThroneEncounterSnapshot encounter,
        UnderworldAnchor sourceAnchor,
        UnderworldAnchor targetAnchor,
        string authorityFingerprint,
        string operationId)
    {
        ValidateStableState(state, identity);
        if (state.CurrentLayer != UnderworldLayer.Surface)
            throw new InvalidOperationException("Underworld entry can begin only from the surface layer.");
        if (!UnderworldUnlockRules.IsUnlocked(encounter))
            throw new InvalidOperationException("The Underworld remains locked until the Nowhere King is defeated.");

        var source = ValidateAnchor(sourceAnchor, nameof(sourceAnchor));
        var target = ValidateAnchor(targetAnchor, nameof(targetAnchor));
        if (!string.Equals(source.WorldId, identity.ParentWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld entry source anchor must belong to the parent surface world.");
        if (!string.Equals(target.WorldId, identity.DerivedWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld entry target anchor must belong to the derived Underworld world.");

        return state with
        {
            SurfaceReturnAnchor = source,
            ActiveTransition = NewIntent(
                operationId,
                UnderworldTransitionDirection.Enter,
                UnderworldLayer.Surface,
                UnderworldLayer.Underworld,
                source,
                target,
                authorityFingerprint),
        };
    }

    public static UnderworldPlayerLayerState BeginReturn(
        UnderworldPlayerLayerState state,
        UnderworldWorldIdentity identity,
        UnderworldAnchor sourceAnchor,
        string authorityFingerprint,
        string operationId)
    {
        ValidateStableState(state, identity);
        if (state.CurrentLayer != UnderworldLayer.Underworld)
            throw new InvalidOperationException("Underworld return can begin only while the player is below.");
        if (state.SurfaceReturnAnchor is null)
            throw new InvalidOperationException("A persisted surface return anchor is required before returning from the Underworld.");

        var source = ValidateAnchor(sourceAnchor, nameof(sourceAnchor));
        if (!string.Equals(source.WorldId, identity.DerivedWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld return source anchor must belong to the derived world.");
        var target = ValidateAnchor(state.SurfaceReturnAnchor, nameof(state.SurfaceReturnAnchor));
        if (!string.Equals(target.WorldId, identity.ParentWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Persisted surface return anchor does not belong to the parent world.");

        return state with
        {
            ActiveTransition = NewIntent(
                operationId,
                UnderworldTransitionDirection.Return,
                UnderworldLayer.Underworld,
                UnderworldLayer.Surface,
                source,
                target,
                authorityFingerprint),
        };
    }

    public static UnderworldPlayerLayerState MarkTargetReady(
        UnderworldPlayerLayerState state,
        string operationId,
        string authorityFingerprint)
    {
        var active = RequireActive(state, operationId, authorityFingerprint);
        if (active.Phase != UnderworldTransitionPhase.Prepared)
            throw new InvalidOperationException("Only a prepared Underworld transition can become target-ready.");

        return state with
        {
            ActiveTransition = active with
            {
                Phase = UnderworldTransitionPhase.TargetReady,
                Diagnostic = string.Empty,
            },
        };
    }

    public static UnderworldPlayerLayerState Commit(
        UnderworldPlayerLayerState state,
        string operationId,
        string authorityFingerprint)
    {
        var active = RequireActive(state, operationId, authorityFingerprint);
        if (active.Phase != UnderworldTransitionPhase.TargetReady)
            throw new InvalidOperationException("An Underworld transition cannot commit before target initialization is confirmed.");

        if (active.Direction == UnderworldTransitionDirection.Enter)
        {
            return state with
            {
                CurrentLayer = UnderworldLayer.Underworld,
                CurrentWorldId = active.TargetAnchor.WorldId,
                SurfaceReturnAnchor = active.SourceAnchor,
                ActiveTransition = null,
            };
        }

        return state with
        {
            CurrentLayer = UnderworldLayer.Surface,
            CurrentWorldId = active.TargetAnchor.WorldId,
            SurfaceReturnAnchor = null,
            ActiveTransition = null,
        };
    }

    public static UnderworldPlayerLayerState RequireRecovery(
        UnderworldPlayerLayerState state,
        string operationId,
        string authorityFingerprint,
        string diagnostic)
    {
        var active = RequireActive(state, operationId, authorityFingerprint);
        if (active.Phase == UnderworldTransitionPhase.RecoveryRequired)
            return state;

        return state with
        {
            ActiveTransition = active with
            {
                Phase = UnderworldTransitionPhase.RecoveryRequired,
                Diagnostic = RequireText(diagnostic, nameof(diagnostic)),
            },
        };
    }

    public static UnderworldPlayerLayerState RecoverToSource(
        UnderworldPlayerLayerState state,
        string operationId,
        string authorityFingerprint)
    {
        var active = RequireActive(state, operationId, authorityFingerprint);
        if (active.Phase != UnderworldTransitionPhase.RecoveryRequired)
            throw new InvalidOperationException("Recovery is permitted only after the active transition is marked recovery-required.");

        return state with
        {
            CurrentLayer = active.SourceLayer,
            CurrentWorldId = active.SourceAnchor.WorldId,
            SurfaceReturnAnchor = active.Direction == UnderworldTransitionDirection.Enter
                ? null
                : state.SurfaceReturnAnchor,
            ActiveTransition = null,
        };
    }

    public static void ValidatePersistedState(
        UnderworldPlayerLayerState state,
        UnderworldWorldIdentity identity)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (state.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported Underworld transition-state schema {state.SchemaVersion}.");
        RequireText(state.PlayerId, nameof(state.PlayerId));

        var expectedWorld = state.CurrentLayer == UnderworldLayer.Surface
            ? identity.ParentWorldId
            : identity.DerivedWorldId;
        if (!string.Equals(state.CurrentWorldId, expectedWorld, StringComparison.Ordinal))
            throw new InvalidOperationException("Persisted Underworld layer and world identity disagree.");

        if (state.CurrentLayer == UnderworldLayer.Underworld && state.SurfaceReturnAnchor is null)
            throw new InvalidOperationException("A stable Underworld player state must retain its surface return anchor.");
        if (state.SurfaceReturnAnchor is not null)
        {
            var surface = ValidateAnchor(state.SurfaceReturnAnchor, nameof(state.SurfaceReturnAnchor));
            if (!string.Equals(surface.WorldId, identity.ParentWorldId, StringComparison.Ordinal))
                throw new InvalidOperationException("Persisted surface return anchor belongs to the wrong world.");
        }

        if (state.ActiveTransition is null)
            return;

        var active = state.ActiveTransition;
        RequireText(active.OperationId, nameof(active.OperationId));
        RequireSha256(active.AuthorityFingerprint);
        ValidateAnchor(active.SourceAnchor, nameof(active.SourceAnchor));
        ValidateAnchor(active.TargetAnchor, nameof(active.TargetAnchor));

        if (active.SourceLayer != state.CurrentLayer ||
            !string.Equals(active.SourceAnchor.WorldId, state.CurrentWorldId, StringComparison.Ordinal))
            throw new InvalidOperationException("Active transition source does not match the player's persisted stable layer.");

        var expectedSource = active.Direction == UnderworldTransitionDirection.Enter
            ? UnderworldLayer.Surface
            : UnderworldLayer.Underworld;
        var expectedTarget = active.Direction == UnderworldTransitionDirection.Enter
            ? UnderworldLayer.Underworld
            : UnderworldLayer.Surface;
        if (active.SourceLayer != expectedSource || active.TargetLayer != expectedTarget)
            throw new InvalidOperationException("Active transition direction and layer endpoints disagree.");

        var expectedTargetWorld = expectedTarget == UnderworldLayer.Surface
            ? identity.ParentWorldId
            : identity.DerivedWorldId;
        if (!string.Equals(active.TargetAnchor.WorldId, expectedTargetWorld, StringComparison.Ordinal))
            throw new InvalidOperationException("Active transition target belongs to the wrong world.");
    }

    private static void ValidateStableState(
        UnderworldPlayerLayerState state,
        UnderworldWorldIdentity identity)
    {
        ValidatePersistedState(state, identity);
        if (state.ActiveTransition is not null)
            throw new InvalidOperationException("A new Underworld transition cannot begin while another transition is active.");
    }

    private static UnderworldTransitionIntent NewIntent(
        string operationId,
        UnderworldTransitionDirection direction,
        UnderworldLayer sourceLayer,
        UnderworldLayer targetLayer,
        UnderworldAnchor sourceAnchor,
        UnderworldAnchor targetAnchor,
        string authorityFingerprint)
    {
        var operation = RequireText(operationId, nameof(operationId));
        var authority = RequireSha256(authorityFingerprint);
        return new UnderworldTransitionIntent(
            operation,
            direction,
            sourceLayer,
            targetLayer,
            sourceAnchor,
            targetAnchor,
            authority,
            UnderworldTransitionPhase.Prepared,
            string.Empty);
    }

    private static UnderworldTransitionIntent RequireActive(
        UnderworldPlayerLayerState state,
        string operationId,
        string authorityFingerprint)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        var active = state.ActiveTransition
            ?? throw new InvalidOperationException("No Underworld transition is active.");
        var operation = RequireText(operationId, nameof(operationId));
        if (!string.Equals(active.OperationId, operation, StringComparison.Ordinal))
            throw new InvalidOperationException("Transition operation id does not match the active transaction.");
        var authority = RequireSha256(authorityFingerprint);
        if (!string.Equals(active.AuthorityFingerprint, authority, StringComparison.Ordinal))
            throw new InvalidOperationException("Gameplay authority changed while the Underworld transition was active.");
        return active;
    }

    private static UnderworldAnchor ValidateAnchor(UnderworldAnchor? anchor, string field)
    {
        if (anchor is null)
            throw new ArgumentNullException(field);
        var worldId = RequireText(anchor.WorldId, field + ".WorldId");
        if (double.IsNaN(anchor.X) || double.IsInfinity(anchor.X) ||
            double.IsNaN(anchor.Y) || double.IsInfinity(anchor.Y) ||
            double.IsNaN(anchor.Z) || double.IsInfinity(anchor.Z) ||
            float.IsNaN(anchor.HeadingDegrees) || float.IsInfinity(anchor.HeadingDegrees))
            throw new InvalidOperationException($"{field} contains non-finite coordinates or heading.");
        return anchor with { WorldId = worldId };
    }

    private static string RequireSha256(string value)
    {
        var normalized = RequireText(value, "authorityFingerprint");
        if (normalized.Length != 64 || normalized.Any(character =>
                (character < '0' || character > '9') &&
                (character < 'a' || character > 'f')))
            throw new InvalidOperationException("Underworld transition authority fingerprint must be canonical lowercase SHA-256 hex.");
        return normalized;
    }

    private static string RequireText(string? value, string field)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException("A non-empty value is required.", field);
        if (normalized.Any(char.IsControl))
            throw new ArgumentException("Control characters are not permitted.", field);
        return normalized;
    }
}
