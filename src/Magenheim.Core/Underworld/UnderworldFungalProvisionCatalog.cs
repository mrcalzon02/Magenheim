using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Canonical Magenheim-owned fungal provisions eligible for Spore Communion.
/// Foreign and vanilla foods are deliberately excluded: the boon may only amplify
/// content explicitly admitted here.
/// </summary>
public static class UnderworldFungalProvisionCatalog
{
    public const string GlowcapStew = "Magenheim_Underworld_Food_GlowcapStew";
    public const string MycelialBroth = "Magenheim_Underworld_Food_MycelialBroth";
    public const string HeartcapRation = "Magenheim_Underworld_Food_HeartcapRation";

    private static readonly string[] CanonicalPrefabs =
    {
        GlowcapStew,
        MycelialBroth,
        HeartcapRation
    };

    public static IReadOnlyList<string> PrefabNames => CanonicalPrefabs;

    public static bool IsEligible(string? prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName)) return false;
        return CanonicalPrefabs.Contains(prefabName, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns the bounded multiplier for an admitted provision. The configured bonus
    /// is a fraction (0.20 = +20%) and cannot exceed +50%. Ineligible foods always
    /// return the neutral multiplier so callers cannot accidentally amplify foreign food.
    /// </summary>
    public static float EfficiencyMultiplier(string? prefabName, float configuredBonus)
    {
        if (!IsEligible(prefabName)) return 1f;
        if (float.IsNaN(configuredBonus) || float.IsInfinity(configuredBonus)) return 1f;
        var bounded = Math.Max(0f, Math.Min(0.50f, configuredBonus));
        return 1f + bounded;
    }
}
