using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Socketing;

public sealed class SocketEligibilityPolicy
{
    public SocketEligibilityPolicy(
        int weaponMaxSlots = 1,
        int armorMaxSlots = 1,
        int shieldMaxSlots = 1,
        int toolMaxSlots = 1,
        int utilityMaxSlots = 1,
        int explicitIncludeMaxSlots = 1,
        IEnumerable<string>? includedPrefabNames = null,
        IEnumerable<string>? excludedPrefabNames = null,
        IEnumerable<string>? includedModOrigins = null,
        IEnumerable<string>? excludedModOrigins = null,
        IEnumerable<EquipmentCategory>? excludedCategories = null,
        SocketIdentityComparison identityComparison = SocketIdentityComparison.Exact,
        IEnumerable<string>? includedItemNames = null,
        IEnumerable<string>? excludedItemNames = null)
    {
        if (!Enum.IsDefined(typeof(SocketIdentityComparison), identityComparison))
            throw new InvalidOperationException($"Unknown socket identity comparison value '{(int)identityComparison}'.");

        WeaponMaxSlots = ValidateLimit(weaponMaxSlots, nameof(weaponMaxSlots));
        ArmorMaxSlots = ValidateLimit(armorMaxSlots, nameof(armorMaxSlots));
        ShieldMaxSlots = ValidateLimit(shieldMaxSlots, nameof(shieldMaxSlots));
        ToolMaxSlots = ValidateLimit(toolMaxSlots, nameof(toolMaxSlots));
        UtilityMaxSlots = ValidateLimit(utilityMaxSlots, nameof(utilityMaxSlots));
        ExplicitIncludeMaxSlots = ValidateLimit(explicitIncludeMaxSlots, nameof(explicitIncludeMaxSlots));
        IdentityComparison = identityComparison;

        var comparer = identityComparison == SocketIdentityComparison.CaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        IncludedItemNames = NormalizeIdentities(includedItemNames, comparer);
        ExcludedItemNames = NormalizeIdentities(excludedItemNames, comparer);
        IncludedPrefabNames = NormalizeIdentities(includedPrefabNames, comparer);
        ExcludedPrefabNames = NormalizeIdentities(excludedPrefabNames, comparer);
        IncludedModOrigins = NormalizeIdentities(includedModOrigins, comparer);
        ExcludedModOrigins = NormalizeIdentities(excludedModOrigins, comparer);
        ExcludedCategories = (excludedCategories ?? Array.Empty<EquipmentCategory>()).Distinct().ToArray();

        foreach (var category in ExcludedCategories)
        {
            if (!Enum.IsDefined(typeof(EquipmentCategory), category))
                throw new InvalidOperationException($"Unknown excluded equipment category value '{(int)category}'.");
        }
    }

    public int WeaponMaxSlots { get; }
    public int ArmorMaxSlots { get; }
    public int ShieldMaxSlots { get; }
    public int ToolMaxSlots { get; }
    public int UtilityMaxSlots { get; }
    public int ExplicitIncludeMaxSlots { get; }
    public SocketIdentityComparison IdentityComparison { get; }
    public IReadOnlyList<string> IncludedItemNames { get; }
    public IReadOnlyList<string> ExcludedItemNames { get; }
    public IReadOnlyList<string> IncludedPrefabNames { get; }
    public IReadOnlyList<string> ExcludedPrefabNames { get; }
    public IReadOnlyList<string> IncludedModOrigins { get; }
    public IReadOnlyList<string> ExcludedModOrigins { get; }
    public IReadOnlyList<EquipmentCategory> ExcludedCategories { get; }

    public int GetCategoryLimit(EquipmentCategory category) => category switch
    {
        EquipmentCategory.Weapon => WeaponMaxSlots,
        EquipmentCategory.Armor => ArmorMaxSlots,
        EquipmentCategory.Shield => ShieldMaxSlots,
        EquipmentCategory.Tool => ToolMaxSlots,
        EquipmentCategory.Utility => UtilityMaxSlots,
        _ => 0
    };

    private static int ValidateLimit(int value, string field)
    {
        if (value < 0 || value > SocketState.MaximumSupportedSlots)
            throw new ArgumentOutOfRangeException(field,
                $"Socket limits must be between 0 and {SocketState.MaximumSupportedSlots}.");
        return value;
    }

    private static IReadOnlyList<string> NormalizeIdentities(
        IEnumerable<string>? values,
        StringComparer comparer)
    {
        return (values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(comparer)
            .ToArray();
    }
}

public enum SocketEligibilityOutcome
{
    Eligible = 0,
    UnknownCategory = 1,
    CategoryDisabled = 2,
    ExcludedPrefab = 3,
    ExcludedModOrigin = 4,
    ExcludedCategory = 5,
    ExcludedItem = 6
}

public sealed record SocketEligibilityResult(
    SocketEligibilityOutcome Outcome,
    int MaximumSlots,
    string Reason)
{
    public bool IsEligible => Outcome == SocketEligibilityOutcome.Eligible;
}

public static class SocketEligibilityService
{
    public static SocketEligibilityResult Evaluate(EquipmentDescriptor equipment, SocketEligibilityPolicy policy)
    {
        if (equipment is null) throw new ArgumentNullException(nameof(equipment));
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        equipment.Validate();

        var comparer = policy.IdentityComparison == SocketIdentityComparison.CaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        if (Contains(policy.ExcludedPrefabNames, equipment.PrefabName, comparer))
            return Ineligible(SocketEligibilityOutcome.ExcludedPrefab,
                $"Prefab '{equipment.PrefabName}' is explicitly excluded from Magenheim socketing.");

        if (!string.IsNullOrWhiteSpace(equipment.ItemName) &&
            Contains(policy.ExcludedItemNames, equipment.ItemName, comparer))
            return Ineligible(SocketEligibilityOutcome.ExcludedItem,
                $"Item identity '{equipment.ItemName}' is explicitly excluded from Magenheim socketing.");

        if (!string.IsNullOrWhiteSpace(equipment.ModOrigin) &&
            Contains(policy.ExcludedModOrigins, equipment.ModOrigin, comparer))
            return Ineligible(SocketEligibilityOutcome.ExcludedModOrigin,
                $"Mod origin '{equipment.ModOrigin}' is explicitly excluded from Magenheim socketing.");

        if (policy.ExcludedCategories.Contains(equipment.Category))
            return Ineligible(SocketEligibilityOutcome.ExcludedCategory,
                $"Equipment category '{equipment.Category}' is excluded from Magenheim socketing.");

        var explicitInclude = Contains(policy.IncludedPrefabNames, equipment.PrefabName, comparer) ||
            (!string.IsNullOrWhiteSpace(equipment.ItemName) &&
             Contains(policy.IncludedItemNames, equipment.ItemName, comparer)) ||
            (!string.IsNullOrWhiteSpace(equipment.ModOrigin) &&
             Contains(policy.IncludedModOrigins, equipment.ModOrigin, comparer));

        var categoryLimit = policy.GetCategoryLimit(equipment.Category);
        if (categoryLimit > 0)
            return new SocketEligibilityResult(SocketEligibilityOutcome.Eligible, categoryLimit,
                $"Adaptive category '{equipment.Category}' permits up to {categoryLimit} socket(s).");

        if (explicitInclude && policy.ExplicitIncludeMaxSlots > 0)
            return new SocketEligibilityResult(SocketEligibilityOutcome.Eligible, policy.ExplicitIncludeMaxSlots,
                $"Explicit compatibility inclusion permits up to {policy.ExplicitIncludeMaxSlots} socket(s).");

        if (equipment.Category == EquipmentCategory.Unknown)
            return Ineligible(SocketEligibilityOutcome.UnknownCategory,
                "Equipment could not be classified safely; add an explicit item, prefab, or mod-origin include rule to opt it in.");

        return Ineligible(SocketEligibilityOutcome.CategoryDisabled,
            $"Equipment category '{equipment.Category}' has a configured socket limit of zero.");
    }

    private static bool Contains(IReadOnlyList<string> values, string candidate, StringComparer comparer)
    {
        for (var i = 0; i < values.Count; i++)
            if (comparer.Equals(values[i], candidate)) return true;
        return false;
    }

    private static SocketEligibilityResult Ineligible(SocketEligibilityOutcome outcome, string reason) =>
        new(outcome, 0, reason);
}
