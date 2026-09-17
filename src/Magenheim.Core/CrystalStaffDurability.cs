using System;

namespace Magenheim.Core;

/// <summary>
/// Service life for the elemental staff families.
///
/// Every Magenheim staff clones StaffIceShards, a vanilla magic staff that spends Eitr
/// instead of durability. The C1 pass removed the inherited Eitr economy but nothing
/// replaced it, so the clones carried no durability at all and never degraded: a staff
/// was a permanent item with no maintenance cost, unlike every other Magenheim weapon.
///
/// Durability restores that cost and puts staves back in the ordinary repair loop.
/// Values rise with tier because a higher tier already demands a higher station level and
/// scarcer crystals; a Master staff should outlast a Simple one rather than merely hit
/// harder. Rough is not a staff tier and fails closed.
/// </summary>
public static class CrystalStaffDurability
{
    /// <summary>Durability of a Simple-tier staff, the first craftable staff tier.</summary>
    public const float SimpleDurability = 150f;

    /// <summary>Additional durability granted by each tier above Simple.</summary>
    public const float DurabilityPerTier = 75f;

    /// <summary>
    /// Returns the maximum durability for a staff of the given crystal tier.
    /// Throws for <see cref="CrystalTier.Rough"/> and for undefined tiers, because a staff
    /// is never built at those tiers and a silent default would ship an unintended value.
    /// </summary>
    public static float ForTier(CrystalTier tier) => tier switch
    {
        CrystalTier.Simple => SimpleDurability,
        CrystalTier.Crystal => SimpleDurability + DurabilityPerTier,
        CrystalTier.Advanced => SimpleDurability + (2f * DurabilityPerTier),
        CrystalTier.Master => SimpleDurability + (3f * DurabilityPerTier),
        CrystalTier.Rough => throw new ArgumentOutOfRangeException(
            nameof(tier), tier, "Rough is not a staff tier; staves begin at Simple."),
        _ => throw new ArgumentOutOfRangeException(
            nameof(tier), tier, "Unknown crystal tier cannot resolve staff durability."),
    };

    /// <summary>
    /// Resolves the staff tier from a canonical Magenheim staff prefab identity such as
    /// <c>Magenheim_Staff_Venom_Crystal</c>. Foreign or malformed identities fail closed so
    /// a renamed prefab cannot silently acquire a default service life.
    /// </summary>
    public static bool TryResolveTier(string? prefabName, out CrystalTier tier)
    {
        tier = default;
        if (string.IsNullOrWhiteSpace(prefabName)) return false;

        const string prefix = "Magenheim_Staff_";
        if (!prefabName!.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var separator = prefabName.LastIndexOf('_');
        if (separator <= prefix.Length - 1 || separator == prefabName.Length - 1) return false;

        var suffix = prefabName.Substring(separator + 1);
        if (!Enum.TryParse(suffix, ignoreCase: false, out CrystalTier parsed)) return false;
        if (parsed == CrystalTier.Rough) return false;

        tier = parsed;
        return true;
    }
}
