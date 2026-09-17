using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Canonical admission boundary for Magenheim-owned vessels that may receive
/// Underworld-specific handling adaptations. Vanilla and third-party ships are
/// deliberately excluded; runtime code must not infer eligibility from Ship alone.
/// </summary>
public static class UnderworldVesselCatalog
{
    public const string BlackwaterSkiff = "Magenheim_Underworld_Vessel_BlackwaterSkiff";

    private static readonly string[] CanonicalPrefabNames =
    {
        BlackwaterSkiff,
    };

    public static IReadOnlyList<string> PrefabNames => CanonicalPrefabNames;

    public static bool IsCanonical(string? prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return false;

        for (var i = 0; i < CanonicalPrefabNames.Length; i++)
        {
            if (string.Equals(CanonicalPrefabNames[i], prefabName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a bounded handling multiplier for a canonical Underworld vessel.
    /// A value of 1 is neutral. The bonus is intentionally capped so Deep Current
    /// improves handling without turning the vessel into a globally superior ship.
    /// </summary>
    public static float HandlingMultiplier(string? prefabName, float configuredBonus)
    {
        if (!IsCanonical(prefabName) || float.IsNaN(configuredBonus) || float.IsInfinity(configuredBonus))
            return 1f;

        var boundedBonus = Math.Max(0f, Math.Min(0.50f, configuredBonus));
        return 1f + boundedBonus;
    }
}
