using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Pure authority for the lifecycle of the dedicated Underworld gameplay instance.
/// This intentionally contains no host coordinates or player-population state: engine placement
/// and per-player transitions are adapter concerns. The persistent instance is not created or
/// destroyed because players enter or leave it.
/// </summary>
public sealed class UnderworldInstanceLifecycle
{
    private UnderworldWorldIdentity? _identity;

    public UnderworldInstancePhase Phase { get; private set; } = UnderworldInstancePhase.Inactive;
    public UnderworldWorldIdentity? Identity => _identity;
    public string? FaultReason { get; private set; }

    public void BeginAdmission(UnderworldWorldIdentity identity)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (Phase != UnderworldInstancePhase.Inactive)
            throw new InvalidOperationException("Underworld instance admission requires an inactive lifecycle.");

        _identity = identity;
        FaultReason = null;
        Phase = UnderworldInstancePhase.Admitting;
    }

    public void MarkActive(UnderworldWorldIdentity identity)
    {
        RequireIdentity(identity);
        if (Phase != UnderworldInstancePhase.Admitting)
            throw new InvalidOperationException("Underworld instance can become active only after admission begins.");
        Phase = UnderworldInstancePhase.Active;
    }

    public void RestoreActive(UnderworldWorldIdentity identity)
    {
        RequireIdentity(identity);
        if (Phase == UnderworldInstancePhase.Inactive)
            throw new InvalidOperationException("An inactive Underworld instance cannot be restored active.");
        FaultReason = null;
        Phase = UnderworldInstancePhase.Active;
    }

    public void MarkFaulted(UnderworldWorldIdentity identity, string reason)
    {
        RequireIdentity(identity);
        if (Phase == UnderworldInstancePhase.Inactive)
            throw new InvalidOperationException("An inactive Underworld instance cannot fault.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A fault reason is required.", nameof(reason));
        FaultReason = reason.Trim();
        Phase = UnderworldInstancePhase.Faulted;
    }

    public void Reset()
    {
        _identity = null;
        FaultReason = null;
        Phase = UnderworldInstancePhase.Inactive;
    }

    private void RequireIdentity(UnderworldWorldIdentity identity)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (_identity is null)
            throw new InvalidOperationException("No Underworld instance identity is admitted.");
        if (!string.Equals(_identity.ParentWorldId, identity.ParentWorldId, StringComparison.Ordinal) ||
            !string.Equals(_identity.DerivedWorldId, identity.DerivedWorldId, StringComparison.Ordinal) ||
            !string.Equals(_identity.DerivedSeedFingerprint, identity.DerivedSeedFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld instance identity changed during its lifecycle.");
    }
}

public enum UnderworldInstancePhase
{
    Inactive = 0,
    Admitting = 1,
    Active = 2,
    Faulted = 4,
}
