using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Socketing;

public enum EquipmentCategory
{
    Unknown = 0,
    Weapon = 1,
    Armor = 2,
    Shield = 3,
    Tool = 4,
    Utility = 5
}

public enum SocketIdentityComparison
{
    Exact = 0,
    CaseInsensitive = 1
}

public sealed record EquipmentDescriptor(
    string PrefabName,
    string ModOrigin,
    EquipmentCategory Category)
{
    /// <summary>
    /// Stable per-item identity when the runtime exposes one separately from prefab identity.
    /// For Valheim this is the shared item name/localization token. It is intentionally
    /// optional so pure callers and unknown foreign items can remain fail-safe.
    /// </summary>
    public string ItemName { get; init; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PrefabName))
            throw new InvalidOperationException("Equipment prefab name is required.");

        if (!Enum.IsDefined(typeof(EquipmentCategory), Category))
            throw new InvalidOperationException($"Unknown equipment category value '{(int)Category}'.");
    }
}

public sealed class SocketState
{
    // The engine and metadata format can represent multiple sockets, but the supported
    // server-configurable gameplay range is deliberately capped at three. Version-one
    // defaults remain one socket per eligible item until multi-socket UI/balance is proven.
    public const int MaximumSupportedSlots = 3;
    private readonly Crystal[] _installedCrystals;

    public SocketState(int unlockedSlots, IEnumerable<Crystal>? installedCrystals = null)
    {
        if (unlockedSlots < 0 || unlockedSlots > MaximumSupportedSlots)
            throw new ArgumentOutOfRangeException(nameof(unlockedSlots),
                $"Unlocked socket count must be between 0 and {MaximumSupportedSlots}.");

        _installedCrystals = installedCrystals?.ToArray() ?? Array.Empty<Crystal>();
        if (_installedCrystals.Length > unlockedSlots)
            throw new InvalidOperationException("Installed crystals cannot exceed unlocked socket count.");

        foreach (var crystal in _installedCrystals)
        {
            if (!Enum.IsDefined(typeof(ElementalAlignment), crystal.Element))
                throw new InvalidOperationException($"Unknown installed crystal element value '{(int)crystal.Element}'.");
            if (!Enum.IsDefined(typeof(CrystalTier), crystal.Tier))
                throw new InvalidOperationException($"Unknown installed crystal tier value '{(int)crystal.Tier}'.");
        }

        UnlockedSlots = unlockedSlots;
    }

    public static SocketState Empty { get; } = new(0);

    public int UnlockedSlots { get; }
    public IReadOnlyList<Crystal> InstalledCrystals => _installedCrystals;
    public int FreeSlots => UnlockedSlots - _installedCrystals.Length;
}

public enum SocketMutationOutcome
{
    Success = 0,
    AtCapacity = 1,
    NoFreeSlot = 2,
    NoInstalledCrystal = 3,
    InvalidPolicy = 4,
    InvalidIndex = 5,
    RoughCrystalNotSocketable = 6
}

public sealed record SocketMutationResult(
    SocketMutationOutcome Outcome,
    SocketState State,
    Crystal? RemovedCrystal,
    string Reason)
{
    public bool Changed => Outcome == SocketMutationOutcome.Success;
}

public static class SocketingService
{
    public static SocketMutationResult AddSlot(SocketState state, int maximumSlots)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (maximumSlots < 0 || maximumSlots > SocketState.MaximumSupportedSlots)
            return new SocketMutationResult(SocketMutationOutcome.InvalidPolicy, state, null,
                $"Maximum socket count must be between 0 and {SocketState.MaximumSupportedSlots}.");

        if (state.UnlockedSlots >= maximumSlots)
            return new SocketMutationResult(SocketMutationOutcome.AtCapacity, state, null,
                $"Item already has the maximum permitted {maximumSlots} socket(s).");

        return new SocketMutationResult(
            SocketMutationOutcome.Success,
            new SocketState(state.UnlockedSlots + 1, state.InstalledCrystals),
            null,
            "Added one socket.");
    }

    public static SocketMutationResult InstallCrystal(SocketState state, Crystal crystal)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (crystal.Tier == CrystalTier.Rough)
            return new SocketMutationResult(SocketMutationOutcome.RoughCrystalNotSocketable, state, null,
                "Rough crystals cannot be socketed; refine the crystal to Simple or better first.");
        if (state.FreeSlots <= 0)
            return new SocketMutationResult(SocketMutationOutcome.NoFreeSlot, state, null,
                "Item has no free socket.");

        var crystals = state.InstalledCrystals.Concat(new[] { crystal }).ToArray();
        return new SocketMutationResult(
            SocketMutationOutcome.Success,
            new SocketState(state.UnlockedSlots, crystals),
            null,
            $"Installed {crystal.Tier} {crystal.Element} crystal.");
    }

    public static SocketMutationResult RemoveCrystal(SocketState state, int index)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.InstalledCrystals.Count == 0)
            return new SocketMutationResult(SocketMutationOutcome.NoInstalledCrystal, state, null,
                "Item has no installed crystals.");
        if (index < 0 || index >= state.InstalledCrystals.Count)
            return new SocketMutationResult(SocketMutationOutcome.InvalidIndex, state, null,
                "Installed crystal index is outside the item socket state.");

        var removed = state.InstalledCrystals[index];
        var crystals = state.InstalledCrystals.Where((_, i) => i != index).ToArray();
        return new SocketMutationResult(
            SocketMutationOutcome.Success,
            new SocketState(state.UnlockedSlots, crystals),
            removed,
            $"Removed {removed.Tier} {removed.Element} crystal.");
    }
}
