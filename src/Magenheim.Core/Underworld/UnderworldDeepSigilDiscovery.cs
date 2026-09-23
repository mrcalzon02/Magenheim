using System;
using System.Linq;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Pure authority for resolving a Deep Sigil's biome into the canonical, stable
/// boss-location identity declared by the mounted Underworld definitions.
/// Coordinates are deliberately not owned here: world generation/location placement
/// resolves this identity to the current world's actual boss anchor.
/// </summary>
public sealed record UnderworldDeepSigilDiscovery(
    string BiomeId,
    string BossId,
    string UniqueLocationId);

public static class UnderworldDeepSigilDiscoveryResolver
{
    public static UnderworldDeepSigilDiscovery Resolve(
        UnderworldDefinitionSet definitions,
        string biomeId)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (string.IsNullOrWhiteSpace(biomeId))
            throw new ArgumentException("Deep Sigil biome id is required.", nameof(biomeId));

        var normalizedBiomeId = biomeId.Trim();
        var biome = definitions.Biomes.SingleOrDefault(value =>
            string.Equals(value.Id, normalizedBiomeId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Deep Sigil references unknown Underworld biome '{normalizedBiomeId}'.");

        var boss = definitions.Bosses.SingleOrDefault(value =>
            string.Equals(value.Id, biome.BossId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Underworld biome '{biome.Id}' references missing boss '{biome.BossId}'.");

        if (!string.Equals(boss.BiomeId, biome.Id, StringComparison.Ordinal))
            throw new InvalidOperationException($"Underworld boss '{boss.Id}' does not belong to Deep Sigil biome '{biome.Id}'.");
        if (string.IsNullOrWhiteSpace(boss.UniqueLocationId))
            throw new InvalidOperationException($"Underworld boss '{boss.Id}' has no stable unique location identity.");

        return new UnderworldDeepSigilDiscovery(biome.Id, boss.Id, boss.UniqueLocationId);
    }
}
