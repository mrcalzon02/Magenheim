using System;

namespace Magenheim.Core.Socketing;

public enum ExternalSocketProvider
{
    None = 0,
    Jewelcrafting = 1,
}

public enum ExternalSocketOwnershipMode
{
    MagenheimOnly = 0,
    ExternalOnly = 1,
    SharedCapacity = 2,
    MagenheimPolicyExternalStorage = 3,
}

public sealed record ExternalSocketCompatibilityPolicy(
    ExternalSocketProvider Provider,
    ExternalSocketOwnershipMode OwnershipMode,
    bool PreferExternalUi,
    bool CountExternalSocketsAgainstMagenheimLimit,
    bool AllowMagenheimCrystalsInExternalSockets,
    bool AllowExternalGemsInMagenheimSockets,
    bool PreserveExistingExternalSockets,
    bool PreserveExistingMagenheimSockets,
    bool FailClosedOnInteropError)
{
    public static ExternalSocketCompatibilityPolicy Disabled => new(
        ExternalSocketProvider.None,
        ExternalSocketOwnershipMode.MagenheimOnly,
        false,
        false,
        false,
        false,
        true,
        true,
        true);

    public void Validate()
    {
        if (!Enum.IsDefined(typeof(ExternalSocketProvider), Provider))
            throw new InvalidOperationException($"Unknown external socket provider '{(int)Provider}'.");
        if (!Enum.IsDefined(typeof(ExternalSocketOwnershipMode), OwnershipMode))
            throw new InvalidOperationException($"Unknown external socket ownership mode '{(int)OwnershipMode}'.");
        if (Provider == ExternalSocketProvider.None && OwnershipMode != ExternalSocketOwnershipMode.MagenheimOnly)
            throw new InvalidOperationException("An external socket ownership mode requires an external provider.");
    }
}

public sealed record SocketCapacityResolution(
    int MagenheimMaximumSlots,
    int ExternalOccupiedSlots,
    int EffectiveMagenheimMaximumSlots,
    bool MagenheimMayMutate,
    bool ExternalProviderOwnsStorage,
    string Diagnostic);

public static class ExternalSocketCompatibility
{
    public static SocketCapacityResolution ResolveCapacity(
        SocketEligibilityResult eligibility,
        ExternalSocketCompatibilityPolicy policy,
        bool providerPresent,
        int externalOccupiedSlots)
    {
        if (eligibility is null) throw new ArgumentNullException(nameof(eligibility));
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        policy.Validate();
        if (externalOccupiedSlots < 0 || externalOccupiedSlots > SocketState.MaximumSupportedSlots)
            throw new ArgumentOutOfRangeException(nameof(externalOccupiedSlots));

        if (!eligibility.IsEligible)
            return new SocketCapacityResolution(0, externalOccupiedSlots, 0, false, false, eligibility.Reason);

        if (!providerPresent || policy.Provider == ExternalSocketProvider.None)
            return new SocketCapacityResolution(
                eligibility.MaximumSlots,
                0,
                eligibility.MaximumSlots,
                true,
                false,
                "No configured external socket provider is active; Magenheim owns socket storage and capacity.");

        return policy.OwnershipMode switch
        {
            ExternalSocketOwnershipMode.MagenheimOnly => new SocketCapacityResolution(
                eligibility.MaximumSlots,
                externalOccupiedSlots,
                eligibility.MaximumSlots,
                true,
                false,
                "External provider is present but Magenheim exclusively owns socket mutations by configuration."),

            ExternalSocketOwnershipMode.ExternalOnly => new SocketCapacityResolution(
                eligibility.MaximumSlots,
                externalOccupiedSlots,
                0,
                false,
                true,
                "External provider exclusively owns socket mutations by configuration; Magenheim compatibility policy still controls eligibility."),

            ExternalSocketOwnershipMode.MagenheimPolicyExternalStorage => new SocketCapacityResolution(
                eligibility.MaximumSlots,
                externalOccupiedSlots,
                eligibility.MaximumSlots,
                true,
                true,
                "Magenheim controls eligibility/capacity while the external provider owns physical socket storage."),

            ExternalSocketOwnershipMode.SharedCapacity => ResolveSharedCapacity(eligibility.MaximumSlots, externalOccupiedSlots, policy),

            _ => throw new InvalidOperationException($"Unsupported external socket ownership mode '{policy.OwnershipMode}'.")
        };
    }

    private static SocketCapacityResolution ResolveSharedCapacity(
        int magenheimMaximumSlots,
        int externalOccupiedSlots,
        ExternalSocketCompatibilityPolicy policy)
    {
        var available = policy.CountExternalSocketsAgainstMagenheimLimit
            ? Math.Max(0, magenheimMaximumSlots - externalOccupiedSlots)
            : magenheimMaximumSlots;

        return new SocketCapacityResolution(
            magenheimMaximumSlots,
            externalOccupiedSlots,
            available,
            available > 0,
            false,
            policy.CountExternalSocketsAgainstMagenheimLimit
                ? $"Shared socket capacity leaves {available} Magenheim slot(s) after {externalOccupiedSlots} externally occupied slot(s)."
                : "Shared ownership is enabled with independent Magenheim and external socket capacities.");
    }
}
