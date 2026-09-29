using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldStationPlacementRule
{
    OpenGround,
    Shoreline,
    GeothermalVent,
    QuietGround,
    StableGround,
    DeepstoneAttuned,
}

public sealed record UnderworldStationCost(string ResourcePrefab, int Amount);

public sealed record UnderworldStationDimensions(
    float WidthMeters,
    float HeightMeters,
    float DepthMeters);

public sealed record UnderworldStationDefinition(
    string Prefab,
    string Name,
    string ModelId,
    UnderworldTerrainBiome Biome,
    string DonorPrefab,
    string BuildStationPrefab,
    string Description,
    UnderworldStationPlacementRule PlacementRule,
    UnderworldStationDimensions Dimensions,
    bool PreserveDonorParticles,
    IReadOnlyList<UnderworldStationCost> Costs);

/// <summary>
/// Canonical six-biome crafting-station ladder for the Underworld. Runtime registration,
/// weapon recipes and authored station assets consume these identities rather than
/// independently re-declaring station names or progression order.
/// </summary>
public static class UnderworldStationCatalog
{
    public const string MycelialBenchPrefab = "Magenheim_Underworld_Station_MycelialBench";
    public const string TidalBasinPrefab = "Magenheim_Underworld_Station_TidalBasin";
    public const string FurnaceHeartForgePrefab = "Magenheim_Underworld_Station_FurnaceHeartForge";
    public const string SilenceTablePrefab = "Magenheim_Underworld_Station_SilenceTable";
    public const string AnchorForgePrefab = "Magenheim_Underworld_Station_AnchorForge";
    public const string CrownReliquaryPrefab = "Magenheim_Underworld_Station_CrownReliquary";

    public static IReadOnlyList<UnderworldStationDefinition> All { get; } = Freeze(new[]
    {
        Station(
            MycelialBenchPrefab, "Mycelial Bench", "underworld-station-mycelial-bench",
            UnderworldTerrainBiome.FungalForest, "piece_workbench", string.Empty,
            "A grown Worldroot workbench anchored in Understone. The first permanent foothold of the Underworld economy.",
            UnderworldStationPlacementRule.OpenGround, 3.4f, 2.2f, 2.0f, false,
            Cost("Magenheim_Underworld_Resource_WorldrootTimber", 10),
            Cost("Magenheim_Underworld_Resource_Understone", 6),
            Cost("Magenheim_Underworld_Resource_SpireFibre", 4)),

        Station(
            TidalBasinPrefab, "Tidal Basin", "underworld-station-tidal-basin",
            UnderworldTerrainBiome.BlackwaterDeep, "piece_workbench", MycelialBenchPrefab,
            "A flowstone curing basin for Deep Salt, Pale Fibre and Blackwater Pearl work. Its finished placement is intended to require shoreline access.",
            UnderworldStationPlacementRule.Shoreline, 3.6f, 2.4f, 3.2f, false,
            Cost("Magenheim_Underworld_Resource_BlackwaterFlowstone", 12),
            Cost("Magenheim_Underworld_Resource_PaleFibre", 8),
            Cost("Magenheim_Underworld_Resource_BlackwaterPearl", 4),
            Cost("Magenheim_Underworld_Resource_DeepSalt", 4)),

        Station(
            FurnaceHeartForgePrefab, "Furnace Heart Forge", "underworld-station-furnace-heart-forge",
            UnderworldTerrainBiome.SulfurousWastes, "forge", TidalBasinPrefab,
            "A slagstone and Emberiron forge built around a contained geothermal heart. Final placement must bind to a live vent rather than providing free heat anywhere.",
            UnderworldStationPlacementRule.GeothermalVent, 4.0f, 3.6f, 3.0f, true,
            Cost("Magenheim_Underworld_Resource_Slagstone", 16),
            Cost("Magenheim_Underworld_Resource_Emberiron", 10),
            Cost("Magenheim_Underworld_Resource_CharredTimber", 6),
            Cost("Magenheim_Underworld_Resource_Sulfur", 4)),

        Station(
            SilenceTablePrefab, "Silence Table", "underworld-station-silence-table",
            UnderworldTerrainBiome.FrozenCaverns, "piece_artisanstation", FurnaceHeartForgePrefab,
            "A Rimewood precision table framed in Rimesilver and Clear Ice for quiet, exacting work at the edge of Crystal Shaping.",
            UnderworldStationPlacementRule.QuietGround, 3.4f, 2.5f, 2.4f, false,
            Cost("Magenheim_Underworld_Resource_Rimewood", 10),
            Cost("Magenheim_Underworld_Resource_ClearIce", 12),
            Cost("Magenheim_Underworld_Resource_Rimesilver", 8)),

        Station(
            AnchorForgePrefab, "Anchor Forge", "underworld-station-anchor-forge",
            UnderworldTerrainBiome.FractureZones, "forge", SilenceTablePrefab,
            "A Titanbone-braced heavy forge that locks itself into Shardstone and Fracture Crystal. Final placement must reject tremor-unstable ground.",
            UnderworldStationPlacementRule.StableGround, 4.2f, 3.2f, 3.0f, true,
            Cost("Magenheim_Underworld_Resource_Shardstone", 16),
            Cost("Magenheim_Underworld_Resource_Titanbone", 12),
            Cost("Magenheim_Underworld_Resource_FractureCrystal", 8)),

        Station(
            CrownReliquaryPrefab, "Crown Reliquary", "underworld-station-crown-reliquary",
            UnderworldTerrainBiome.GreatDecay, "piece_artisanstation", AnchorForgePrefab,
            "The final Underworld station: Rotwood, bone and Carrion Amber bound around Deepstone authority for Defiant-tier work.",
            UnderworldStationPlacementRule.DeepstoneAttuned, 4.0f, 3.8f, 3.2f, false,
            Cost("Magenheim_Underworld_Resource_Rotwood", 12),
            Cost("Magenheim_Underworld_Resource_CarrionAmber", 10),
            Cost("Magenheim_Underworld_Resource_BoneGravel", 8),
            Cost("Magenheim_Underworld_Resource_DecaySpore", 6)),
    });

    public static UnderworldStationDefinition Require(string prefab)
    {
        var station = All.SingleOrDefault(value => string.Equals(value.Prefab, prefab, StringComparison.Ordinal));
        return station ?? throw new InvalidOperationException("Unknown Underworld station prefab '" + prefab + "'.");
    }

    private static IReadOnlyList<UnderworldStationDefinition> Freeze(IEnumerable<UnderworldStationDefinition> definitions)
    {
        var all = definitions.ToArray();
        if (all.Length != 6) throw new InvalidOperationException("The Underworld progression requires exactly six foundational stations.");
        if (all.Select(value => value.Prefab).Distinct(StringComparer.Ordinal).Count() != all.Length)
            throw new InvalidOperationException("Underworld station prefab identities must be unique.");
        if (all.Select(value => value.ModelId).Distinct(StringComparer.Ordinal).Count() != all.Length)
            throw new InvalidOperationException("Underworld station model identities must be unique.");

        for (var index = 0; index < all.Length; index++)
        {
            var station = all[index];
            if (string.IsNullOrWhiteSpace(station.Prefab) || !station.Prefab.StartsWith("Magenheim_Underworld_Station_", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid Underworld station prefab identity.");
            if (string.IsNullOrWhiteSpace(station.ModelId) || !station.ModelId.StartsWith("underworld-station-", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid Underworld station model identity for " + station.Name + ".");
            if (station.Dimensions.WidthMeters <= 0f || station.Dimensions.HeightMeters <= 0f || station.Dimensions.DepthMeters <= 0f)
                throw new InvalidOperationException("Underworld station dimensions must be positive for " + station.Name + ".");
            if (station.Costs.Count < 3 || station.Costs.Any(cost => cost.Amount <= 0 || string.IsNullOrWhiteSpace(cost.ResourcePrefab)))
                throw new InvalidOperationException("Underworld station costs are incomplete for " + station.Name + ".");
            if (index == 0 && station.BuildStationPrefab.Length != 0)
                throw new InvalidOperationException("The Mycelial Bench must be the ungated Underworld foothold.");
            if (index > 0 && !string.Equals(station.BuildStationPrefab, all[index - 1].Prefab, StringComparison.Ordinal))
                throw new InvalidOperationException(station.Name + " must be built from the immediately preceding Underworld station.");
        }

        return Array.AsReadOnly(all);
    }

    private static UnderworldStationDefinition Station(
        string prefab,
        string name,
        string modelId,
        UnderworldTerrainBiome biome,
        string donorPrefab,
        string buildStationPrefab,
        string description,
        UnderworldStationPlacementRule placementRule,
        float width,
        float height,
        float depth,
        bool preserveDonorParticles,
        params UnderworldStationCost[] costs) =>
        new(prefab, name, modelId, biome, donorPrefab, buildStationPrefab, description, placementRule,
            new UnderworldStationDimensions(width, height, depth), preserveDonorParticles, Array.AsReadOnly(costs));

    private static UnderworldStationCost Cost(string resourcePrefab, int amount) => new(resourcePrefab, amount);
}
