using System;
using System.Collections.Generic;
using Jotunn.Managers;
using Magenheim.Core.Worldgen;

namespace Magenheim.Runtime;

/// <summary>
/// Read-only boundary between validated Magenheim worldgen intent and the current Valheim/Jotunn
/// enum/registration surface. This class deliberately does not add, remove, or modify vegetation.
/// </summary>
internal static class JotunnWorldgenAdapter
{
    internal static Heightmap.Biome MapBiome(string biome)
    {
        if (string.IsNullOrWhiteSpace(biome))
            throw new ArgumentException("A validated biome name is required.", nameof(biome));

        return biome switch
        {
            "Meadows" => ParseBiome("Meadows"),
            "BlackForest" => ParseBiome("BlackForest"),
            "Swamp" => ParseBiome("Swamp"),
            "Mountain" => ParseBiome("Mountain"),
            "Plains" => ParseBiome("Plains"),
            "Mistlands" => ParseBiome("Mistlands"),
            "Ashlands" => ParseBiome("Ashlands", "AshLands"),
            "DeepNorth" => ParseBiome("DeepNorth"),
            _ => throw new InvalidOperationException($"Unsupported validated biome '{biome}' at the Jotunn adapter boundary."),
        };
    }

    internal static Heightmap.BiomeArea MapArea(SpawnArea area) =>
        area switch
        {
            SpawnArea.Median => Heightmap.BiomeArea.Median,
            SpawnArea.Edge => Heightmap.BiomeArea.Edge,
            SpawnArea.All => Heightmap.BiomeArea.Everywhere,
            _ => throw new InvalidOperationException(
                $"Unsupported spawn area '{area}' at the Jotunn adapter boundary. " +
                "Area values must be normalized by Magenheim.Core before runtime mapping."),
        };

    /// <summary>
    /// Observes only the host vegetation identities Magenheim intends to add. The result can be
    /// passed back to DefinitionWorldgenPlanner for collision decisions. No host entry is mutated.
    /// Invoke only when ZoneManager vanilla/custom vegetation is available for lookup.
    /// </summary>
    internal static IReadOnlyList<ObservedWorldgenRegistration> ObserveDesiredVegetation(
        IEnumerable<DesiredWorldgenAddition> desired)
    {
        if (desired is null)
            throw new ArgumentNullException(nameof(desired));

        var observed = new List<ObservedWorldgenRegistration>();
        foreach (var addition in desired)
        {
            if (addition is null || string.IsNullOrWhiteSpace(addition.PrefabName))
                continue;

            var existing = ZoneManager.Instance.GetZoneVegetation(addition.PrefabName);
            if (existing is null)
                continue;

            observed.Add(new ObservedWorldgenRegistration(
                string.Empty,
                "host-existing-vegetation",
                addition.PrefabName));
        }

        return observed.AsReadOnly();
    }

    private static Heightmap.Biome ParseBiome(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (Enum.TryParse(candidate, ignoreCase: true, out Heightmap.Biome parsed) &&
                Enum.IsDefined(typeof(Heightmap.Biome), parsed))
            {
                return parsed;
            }
        }

        throw new InvalidOperationException(
            $"None of the expected Valheim biome enum names [{string.Join(", ", candidates)}] exist in this runtime.");
    }
}
