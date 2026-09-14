using System;
using System.Collections.Generic;
using Jotunn.Managers;
using Magenheim.Core.Worldgen;

namespace Magenheim.Runtime;

/// <summary>
/// Translation and observation boundary between validated Magenheim worldgen intent and the
/// current Valheim/Jotunn enum/registration surface. Observation is read-only; mutation remains
/// owned by the dedicated Magenheim registrar and is limited to new Magenheim vegetation.
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
            SpawnArea.Median => ParseBiomeArea("Median"),
            SpawnArea.Edge => ParseBiomeArea("Edge"),
            // Valheim/Jotunn builds have used both names for the combined value. Resolve the
            // actual runtime enum instead of compiling against one spelling and guessing.
            SpawnArea.All => ParseBiomeArea("Everything", "Everywhere"),
            _ => throw new InvalidOperationException(
                $"Unsupported spawn area '{area}' at the Jotunn adapter boundary. " +
                "Area values must be normalized by Magenheim.Core before runtime mapping."),
        };

    /// <summary>
    /// Observes only host identities Magenheim intends to add. Both ZoneManager vegetation and
    /// the broader PrefabManager namespace are checked because AddCustomVegetation registers the
    /// prefab itself. No host entry is mutated.
    /// </summary>
    internal static IReadOnlyList<ObservedWorldgenRegistration> ObserveDesiredHostIdentities(
        IEnumerable<DesiredWorldgenAddition> desired)
    {
        if (desired is null)
            throw new ArgumentNullException(nameof(desired));

        var observed = new List<ObservedWorldgenRegistration>();
        var seenPrefabs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var addition in desired)
        {
            if (addition is null || string.IsNullOrWhiteSpace(addition.PrefabName))
                continue;
            if (!seenPrefabs.Add(addition.PrefabName))
                continue;

            var existingVegetation = ZoneManager.Instance.GetZoneVegetation(addition.PrefabName);
            if (existingVegetation is not null)
            {
                observed.Add(new ObservedWorldgenRegistration(
                    string.Empty,
                    "host-existing-vegetation",
                    addition.PrefabName));
                continue;
            }

            if (PrefabManager.Instance.GetPrefab(addition.PrefabName) is not null)
            {
                observed.Add(new ObservedWorldgenRegistration(
                    string.Empty,
                    "host-existing-prefab",
                    addition.PrefabName));
            }
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

    private static Heightmap.BiomeArea ParseBiomeArea(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (Enum.TryParse(candidate, ignoreCase: true, out Heightmap.BiomeArea parsed) &&
                Enum.IsDefined(typeof(Heightmap.BiomeArea), parsed))
            {
                return parsed;
            }
        }

        throw new InvalidOperationException(
            $"None of the expected Valheim biome-area enum names [{string.Join(", ", candidates)}] exist in this runtime.");
    }
}
