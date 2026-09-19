using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Pure authority for the lifecycle of the dedicated Underworld gameplay instance.
/// This intentionally contains no host coordinates: engine placement is an adapter concern and
/// cannot redefine the instance as a distant Surface landmass.
/// </summary>
public sealed class UnderworldInstanceLifecycle
{
    private readonly HashSet<string> _occupants = new(StringComparer.Ordinal);
    private UnderworldWorldIdentity? _identity;

    public UnderworldInstancePhase Phase { get; private set; } = UnderworldInstancePhase.Inactive;
    public UnderworldWorldIdentity? Identity => _identity;
    public string? FaultReason { get; private set; }
    public int OccupantCount => _occupants.Count;

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

    public void RegisterOccupant(UnderworldWorldIdentity identity, string playerId)
    {
        RequireIdentity(identity);
        if (Phase != UnderworldInstancePhase.Active)
            throw new InvalidOperationException("Players may be registered only while the Underworld instance is active.");
        _occupants.Add(RequirePlayerId(playerId));
    }

    public bool ContainsOccupant(string playerId) => _occupants.Contains(RequirePlayerId(playerId));

    public bool IsLastOccupant(string playerId)
    {
        var player = RequirePlayerId(playerId);
        return _occupants.Count == 1 && _occupants.Contains(player);
    }

    public void RemoveOccupant(UnderworldWorldIdentity identity, string playerId)
    {
        RequireIdentity(identity);
        if (Phase != UnderworldInstancePhase.Active && Phase != UnderworldInstancePhase.Releasing)
            throw new InvalidOperationException("Players may leave only an active or releasing Underworld instance.");
        _occupants.Remove(RequirePlayerId(playerId));
    }

    public void BeginRelease(UnderworldWorldIdentity identity)
    {
        RequireIdentity(identity);
        if (Phase != UnderworldInstancePhase.Active)
            throw new InvalidOperationException("Underworld instance release requires an active instance.");
        if (_occupants.Count > 1)
            throw new InvalidOperationException("Underworld instance cannot release while multiple players remain admitted.");
        Phase = UnderworldInstancePhase.Releasing;
    }

    public void CompleteRelease(UnderworldWorldIdentity identity)
    {
        RequireIdentity(identity);
        if (Phase != UnderworldInstancePhase.Releasing)
            throw new InvalidOperationException("Underworld instance release completion requires a releasing instance.");
        if (_occupants.Count != 0)
            throw new InvalidOperationException("Underworld instance cannot complete release while players remain admitted.");
        Reset();
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
        _occupants.Clear();
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

    private static string RequirePlayerId(string playerId)
    {
        var normalized = playerId?.Trim() ?? string.Empty;
        if (normalized.Length == 0) throw new ArgumentException("A player id is required.", nameof(playerId));
        return normalized;
    }
}

public enum UnderworldInstancePhase
{
    Inactive = 0,
    Admitting = 1,
    Active = 2,
    Releasing = 3,
    Faulted = 4,
}
