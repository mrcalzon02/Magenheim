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
    private readonly object _sync = new();
    private UnderworldWorldIdentity? _identity;
    private UnderworldInstancePhase _phase = UnderworldInstancePhase.Inactive;

    public UnderworldInstancePhase Phase { get { lock (_sync) return _phase; } }
    public UnderworldWorldIdentity? Identity { get { lock (_sync) return _identity; } }

    /// <summary>
    /// Idempotently admits the persistent instance identity. Concurrent/re-entrant first-entry
    /// attempts for the same derived world converge on one admission; a different identity fails.
    /// </summary>
    public void EnsureAdmitted(UnderworldWorldIdentity identity)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        lock (_sync)
        {
            if (_phase == UnderworldInstancePhase.Inactive)
            {
                _identity = identity;
                _phase = UnderworldInstancePhase.Admitting;
                return;
            }
            RequireIdentityUnsafe(identity);
        }
    }

    /// <summary>Idempotently promotes an admitted instance to active.</summary>
    public void EnsureActive(UnderworldWorldIdentity identity)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        lock (_sync)
        {
            RequireIdentityUnsafe(identity);
            if (_phase == UnderworldInstancePhase.Admitting)
            {
                _phase = UnderworldInstancePhase.Active;
                return;
            }
            if (_phase != UnderworldInstancePhase.Active)
                throw new InvalidOperationException($"Underworld instance cannot become active from lifecycle phase {_phase}.");
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _identity = null;
            _phase = UnderworldInstancePhase.Inactive;
        }
    }

    private void RequireIdentityUnsafe(UnderworldWorldIdentity identity)
    {
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
}
