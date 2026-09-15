using System;
using System.Numerics;

namespace Magenheim.Core.DarkThrone;

/// <summary>
/// Durable lifecycle state for the unique Dark Throne encounter. This model deliberately contains
/// no Unity or Valheim runtime objects so the server runtime can serialize the minimum authoritative
/// facts and reconstruct transient presentation after a zone unload.
/// </summary>
public enum DarkThroneEncounterLifecycle
{
    NeverEncountered = 0,
    Engaged = 1,
    Disengaged = 2,
    Defeated = 3,
}

public sealed record DarkThroneEncounterSnapshot(
    int SchemaVersion,
    string EncounterId,
    Vector3 ThroneAnchor,
    DarkThroneEncounterLifecycle Lifecycle,
    double BossHealthFraction,
    bool RewardsCompleted)
{
    public const int CurrentSchemaVersion = 1;

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported Dark Throne encounter schema {SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(EncounterId))
            throw new InvalidOperationException("Dark Throne encounter identity is required.");
        if (!float.IsFinite(ThroneAnchor.X) || !float.IsFinite(ThroneAnchor.Y) || !float.IsFinite(ThroneAnchor.Z))
            throw new InvalidOperationException("Dark Throne throne anchor must be finite.");
        if (BossHealthFraction < 0d || BossHealthFraction > 1d || double.IsNaN(BossHealthFraction))
            throw new InvalidOperationException("Dark Throne boss health fraction must be between zero and one.");
        if (Lifecycle != DarkThroneEncounterLifecycle.Defeated && RewardsCompleted)
            throw new InvalidOperationException("An undefeated Dark Throne encounter cannot have completed rewards.");
        if (Lifecycle == DarkThroneEncounterLifecycle.Defeated && BossHealthFraction != 0d)
            throw new InvalidOperationException("A defeated Dark Throne encounter must have zero boss health.");
    }

    public bool RequiresLivingKing => Lifecycle != DarkThroneEncounterLifecycle.Defeated;
}

public sealed record DarkThroneEncounterPolicy(
    double DisengageGraceSeconds,
    bool ResetHealthOnDisengage)
{
    public static DarkThroneEncounterPolicy Default { get; } = new(12d, true);

    public void Validate()
    {
        if (DisengageGraceSeconds < 0d || double.IsNaN(DisengageGraceSeconds) || double.IsInfinity(DisengageGraceSeconds))
            throw new InvalidOperationException("Dark Throne disengage grace must be finite and non-negative.");
    }
}

public static class DarkThroneEncounterTransitions
{
    public static DarkThroneEncounterSnapshot Engage(DarkThroneEncounterSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);
        current.Validate();
        if (current.Lifecycle == DarkThroneEncounterLifecycle.Defeated)
            throw new InvalidOperationException("A defeated Dark Throne encounter cannot be re-engaged.");

        return current with { Lifecycle = DarkThroneEncounterLifecycle.Engaged };
    }

    public static DarkThroneEncounterSnapshot Disengage(
        DarkThroneEncounterSnapshot current,
        DarkThroneEncounterPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(policy);
        current.Validate();
        policy.Validate();
        if (current.Lifecycle == DarkThroneEncounterLifecycle.Defeated)
            return current;

        var health = policy.ResetHealthOnDisengage ? 1d : current.BossHealthFraction;
        return current with
        {
            Lifecycle = DarkThroneEncounterLifecycle.Disengaged,
            BossHealthFraction = health,
        };
    }

    public static DarkThroneEncounterSnapshot Defeat(DarkThroneEncounterSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);
        current.Validate();
        if (current.Lifecycle == DarkThroneEncounterLifecycle.Defeated)
            return current;

        return current with
        {
            Lifecycle = DarkThroneEncounterLifecycle.Defeated,
            BossHealthFraction = 0d,
        };
    }

    public static DarkThroneEncounterSnapshot CompleteRewards(DarkThroneEncounterSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);
        current.Validate();
        if (current.Lifecycle != DarkThroneEncounterLifecycle.Defeated)
            throw new InvalidOperationException("Dark Throne rewards cannot complete before defeat.");

        return current with { RewardsCompleted = true };
    }
}
