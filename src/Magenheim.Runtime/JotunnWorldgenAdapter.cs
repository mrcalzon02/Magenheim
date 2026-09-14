using System;
using System.Collections.Generic;
using Jotunn.Managers;
using Magenheim.Core.Worldgen;
using CoreSpawnArea = Magenheim.Core.Worldgen.SpawnArea;

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

    internal static Heightmap.BiomeArea MapArea(CoreSpawnArea area) =>
        area switch
        {
            CoreSpawnArea.Median => ParseBiomeArea("Median"),
            CoreSpawnArea.Edge => ParseBiomeArea("Edge"),
            CoreSpawnArea.All => ParseAllBiomeArea(),
            _ => throw new InvalidOperationException(
                $"Unsupported spawn area '{area}' at the Jotunn adapter boundary. " +
                "Area values must be normalized by Magenheim.Core before runtime mapping."),
        };

    /// <summary>
    /// Observes only prefab identities Magenheim intends to claim. PrefabManager is deliberately
    /// used instead of ZoneManager.GetZoneVegetation here: the registrar runs from
    /// OnVanillaPrefabsAvailable, while Jotunn's GetZoneVegetation directly dereferences
    /// ZoneSystem.instance and is not safe to call before a ZoneSystem exists. AddCustomVegetation
    /// itself claims the prefab through PrefabManager, so prefab occupancy is the decisive
    /// non-destructive collision boundary at this lifecycle stage.
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

    private static Heightmap.BiomeArea ParseAllBiomeArea()
    {
        // Resolve the authoritative components first. A runtime alias is only acceptable if it
        // demonstrably contains both distinct component bits; trusting a familiar enum name alone
        // could silently map Magenheim's All intent onto a changed or malformed runtime value.
        var median = ParseBiomeArea("Median");
        var edge = ParseBiomeArea("Edge");

        if (median.Equals(default(Heightmap.BiomeArea)) ||
            edge.Equals(default(Heightmap.BiomeArea)) ||
            median.Equals(edge))
        {
            throw new InvalidOperationException(
                $"The current Valheim biome-area enum does not expose distinct non-empty Median and Edge values " +
                $"(Median={median}, Edge={edge}). Magenheim will not guess an All mapping.");
        }

        var composed = median | edge;
        if (composed.Equals(default(Heightmap.BiomeArea)) ||
            !ContainsArea(composed, median) ||
            !ContainsArea(composed, edge))
        {
            throw new InvalidOperationException(
                $"The current Valheim biome-area enum could not compose a valid Median|Edge value " +
                $"(Median={median}, Edge={edge}, composed={composed}).");
        }

        // Prefer a named runtime alias only when it is semantically equivalent to the composed
        // components. Extra bits are rejected so a future broader 'Everything' value cannot make
        // Magenheim geodes appear in an area category that the validated core did not request.
        if (TryParseDefinedBiomeArea("Everything", out var alias) ||
            TryParseDefinedBiomeArea("Everywhere", out alias))
        {
            if (!alias.Equals(composed))
            {
                throw new InvalidOperationException(
                    $"The current Valheim biome-area alias '{alias}' is not equivalent to Median|Edge '{composed}'. " +
                    "Magenheim will fail closed rather than broaden or narrow placement semantics.");
            }

            return alias;
        }

        // Flags enums do not need a named value for every valid combination.
        return composed;
    }

    private static bool ContainsArea(Heightmap.BiomeArea combined, Heightmap.BiomeArea component) =>
        (combined & component).Equals(component);

    private static Heightmap.BiomeArea ParseBiomeArea(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (TryParseDefinedBiomeArea(candidate, out var parsed))
                return parsed;
        }

        throw new InvalidOperationException(
            $"None of the expected Valheim biome-area enum names [{string.Join(", ", candidates)}] exist in this runtime.");
    }

    private static bool TryParseDefinedBiomeArea(string candidate, out Heightmap.BiomeArea parsed)
    {
        return Enum.TryParse(candidate, ignoreCase: true, out parsed) &&
               Enum.IsDefined(typeof(Heightmap.BiomeArea), parsed);
    }
}
