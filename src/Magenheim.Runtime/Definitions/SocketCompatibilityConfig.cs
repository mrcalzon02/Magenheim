using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Magenheim.Core.Socketing;

namespace Magenheim.Runtime.Definitions;

internal static class SocketCompatibilityConfig
{
    internal static SocketEligibilityPolicy Read(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        const string limits = "Socketing.Limits";
        const string compatibility = "Socketing.Compatibility";

        var weapon = config.Bind(limits, "WeaponMaxSlots", 1,
            "Maximum sockets on adaptively classified weapons. Supported range 0..3.");
        var armor = config.Bind(limits, "ArmorMaxSlots", 1,
            "Maximum sockets on adaptively classified armor. Supported range 0..3.");
        var shield = config.Bind(limits, "ShieldMaxSlots", 1,
            "Maximum sockets on adaptively classified shields. Supported range 0..3.");
        var tool = config.Bind(limits, "ToolMaxSlots", 1,
            "Maximum sockets on adaptively classified tools. Supported range 0..3.");
        var utility = config.Bind(limits, "UtilityMaxSlots", 1,
            "Maximum sockets on adaptively classified utility equipment. Supported range 0..3.");
        var explicitInclude = config.Bind(limits, "ExplicitIncludeMaxSlots", 1,
            "Maximum sockets granted to otherwise-unknown equipment explicitly opted in by item, prefab, or mod origin. Supported range 0..3.");

        var identity = config.Bind(compatibility, "IdentityComparison", SocketIdentityComparison.Exact,
            "Comparison mode for socket include/exclude identities: Exact or CaseInsensitive.");
        var includedItems = config.Bind(compatibility, "IncludedItemNames", string.Empty,
            "Comma-separated shared item identities/localization tokens explicitly allowed for socketing. Exclusions still win.");
        var excludedItems = config.Bind(compatibility, "ExcludedItemNames", string.Empty,
            "Comma-separated shared item identities/localization tokens that Magenheim must never socket.");
        var includedPrefabs = config.Bind(compatibility, "IncludedPrefabNames", string.Empty,
            "Comma-separated prefab identities explicitly allowed for socketing. Exclusions still win.");
        var excludedPrefabs = config.Bind(compatibility, "ExcludedPrefabNames", string.Empty,
            "Comma-separated prefab identities that Magenheim must never socket or rewrite.");
        var includedOrigins = config.Bind(compatibility, "IncludedModOrigins", string.Empty,
            "Comma-separated mod-origin identities explicitly allowed for socketing when runtime origin discovery is available.");
        var excludedOrigins = config.Bind(compatibility, "ExcludedModOrigins", string.Empty,
            "Comma-separated mod-origin identities that Magenheim must never socket when runtime origin discovery is available.");
        var excludedCategories = config.Bind(compatibility, "ExcludedCategories", string.Empty,
            "Comma-separated equipment categories to exclude: Weapon, Armor, Shield, Tool, Utility.");

        return new SocketEligibilityPolicy(
            weaponMaxSlots: weapon.Value,
            armorMaxSlots: armor.Value,
            shieldMaxSlots: shield.Value,
            toolMaxSlots: tool.Value,
            utilityMaxSlots: utility.Value,
            explicitIncludeMaxSlots: explicitInclude.Value,
            includedPrefabNames: ParseCsv(includedPrefabs.Value),
            excludedPrefabNames: ParseCsv(excludedPrefabs.Value),
            includedModOrigins: ParseCsv(includedOrigins.Value),
            excludedModOrigins: ParseCsv(excludedOrigins.Value),
            excludedCategories: ParseCategories(excludedCategories.Value),
            identityComparison: identity.Value,
            includedItemNames: ParseCsv(includedItems.Value),
            excludedItemNames: ParseCsv(excludedItems.Value));
    }

    private static string[] ParseCsv(string? value)
    {
        if (value is null || string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0)
            .ToArray();
    }

    private static EquipmentCategory[] ParseCategories(string? value)
    {
        var categories = new List<EquipmentCategory>();
        foreach (var token in ParseCsv(value))
        {
            if (!Enum.TryParse(token, true, out EquipmentCategory category) ||
                !Enum.IsDefined(typeof(EquipmentCategory), category) ||
                category == EquipmentCategory.Unknown)
            {
                throw new InvalidOperationException(
                    $"Unknown socket exclusion category '{token}'. Expected Weapon, Armor, Shield, Tool, or Utility.");
            }
            categories.Add(category);
        }
        return categories.ToArray();
    }
}
