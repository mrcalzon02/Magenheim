using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using Magenheim.Core.Socketing;

namespace Magenheim.Runtime.Definitions;

internal sealed record EquipmentSocketIntegrationSettings(
    bool JewelcraftingIntegrationEnabled,
    bool PreferJewelcrafting,
    bool FailClosedOnInteropError,
    IReadOnlyList<float> MagenheimResonanceMultipliers);

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

    internal static EquipmentSocketIntegrationSettings ReadEquipmentIntegration(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        const string integration = "Socketing.Integration";
        var enabled = config.Bind(integration, "EnableJewelcraftingIntegration", true,
            "When Jewelcrafting is installed and its public API is compatible, allow it to own equipment socket storage and UI while Magenheim retains Crystal Shaping, refinement, crystal identity, architecture, furniture and production consumers.");
        var preferJewelcrafting = config.Bind(integration, "PreferJewelcraftingEquipmentSockets", true,
            "Prefer Jewelcrafting for equipment socket storage/UI when both mods are available. Disable to keep Magenheim's standalone equipment socket implementation.");
        var failClosed = config.Bind(integration, "FailClosedOnJewelcraftingInteropError", true,
            "If Jewelcrafting is detected but its compatibility boundary cannot be verified, block Magenheim equipment socket mutation instead of risking competing socket metadata. Has no effect when Jewelcrafting is absent or integration is disabled.");
        var resonance = config.Bind(integration, "MagenheimResonanceMultipliers", "1,0.5,0.25,0.125",
            "Comma-separated contribution multipliers for repeated Magenheim crystals of the same element in Jewelcrafting sockets, strongest crystal first. The final value repeats for additional crystals. Default: 100%, 50%, 25%, then 12.5% each.");

        return new EquipmentSocketIntegrationSettings(
            enabled.Value,
            preferJewelcrafting.Value,
            failClosed.Value,
            ParseResonanceMultipliers(resonance.Value));
    }

    internal static IReadOnlyList<float> ParseResonanceMultipliers(string? value)
    {
        var tokens = ParseCsv(value);
        if (tokens.Length == 0)
            throw new InvalidOperationException("MagenheimResonanceMultipliers must contain at least one non-negative finite multiplier.");

        var multipliers = new float[tokens.Length];
        for (var index = 0; index < tokens.Length; index++)
        {
            if (!float.TryParse(tokens[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var multiplier)
                || float.IsNaN(multiplier)
                || float.IsInfinity(multiplier)
                || multiplier < 0f)
            {
                throw new InvalidOperationException(
                    $"Invalid Magenheim resonance multiplier '{tokens[index]}'. Values must be non-negative finite numbers using '.' as the decimal separator.");
            }

            multipliers[index] = multiplier;
        }

        return Array.AsReadOnly(multipliers);
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
