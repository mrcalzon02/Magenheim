using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime presentation catalog for unique Underworld locations that are physically resident.
/// Canonical boss/location ownership remains in the definition authority; this catalog only tells
/// discovery output how a completed location maps to native terrain and vanilla map presentation.
/// Add an entry only after the corresponding unique location has real residency.
/// </summary>
internal static class UnderworldUniqueLocationDiscoveryCatalog
{
    internal readonly record struct Entry(
        string BiomeId,
        string LocationId,
        UnderworldTerrainBiome TerrainBiome,
        string PinName);

    private static readonly IReadOnlyDictionary<string, Entry> ByBiome =
        new Dictionary<string, Entry>(StringComparer.Ordinal)
        {
            ["magenheim.underworld.biome.blackwater_deep"] = new(
                "magenheim.underworld.biome.blackwater_deep",
                "magenheim.underworld.location.blackwater_maw",
                UnderworldTerrainBiome.BlackwaterDeep,
                "$magenheim_underworld_drowned_ring"),
            ["magenheim.underworld.biome.fungal_forest"] = new(
                "magenheim.underworld.biome.fungal_forest",
                "magenheim.underworld.location.first_bloom",
                UnderworldTerrainBiome.FungalForest,
                "$magenheim_underworld_motherbed"),
            ["magenheim.underworld.biome.fracture"] = new(
                "magenheim.underworld.biome.fracture",
                "magenheim.underworld.location.fracture",
                UnderworldTerrainBiome.FractureZones,
                "$magenheim_underworld_suspended_court")
        };

    internal static bool TryResolve(string biomeId, string locationId, out Entry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(biomeId) || string.IsNullOrWhiteSpace(locationId)) return false;
        if (!ByBiome.TryGetValue(biomeId.Trim(), out var candidate)) return false;

        // Fail closed if definition authority and runtime residency ever disagree. A Sigil must never
        // reveal a different resident location merely because the biome happens to have one catalogued.
        if (!string.Equals(candidate.LocationId, locationId.Trim(), StringComparison.Ordinal)) return false;
        entry = candidate;
        return true;
    }
}
