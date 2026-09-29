using System;

namespace Magenheim.Core.Underworld;

public readonly record struct UnderworldStationPlacementSite(
    bool IsUnderworld,
    UnderworldTerrainBiome CenterBiome,
    double CenterWaterDepth,
    double MinimumWaterDepth,
    double MaximumWaterDepth,
    double HeightSpanMeters,
    double MaximumHazard01,
    bool GeothermalVentNearby,
    bool AnchorStabilizerNearby,
    bool DeepstoneAttuned);

public readonly record struct UnderworldStationPlacementDecision(bool Allowed, string Diagnostic)
{
    public static UnderworldStationPlacementDecision Accept(string diagnostic) => new(true, diagnostic);
    public static UnderworldStationPlacementDecision Reject(string diagnostic) => new(false, diagnostic);
}

/// <summary>
/// Pure environmental siting authority for the six foundational Underworld stations.
/// Runtime supplies canonical terrain samples and world-state facts; this class owns the rules.
/// </summary>
public static class UnderworldStationPlacement
{
    public const double OpenGroundMaximumWaterDepth = .20d;
    public const double OpenGroundMaximumHeightSpanMeters = 1.25d;
    public const double ShorelineDryThresholdMeters = .15d;
    public const double ShorelineWetThresholdMeters = .55d;
    public const double ShorelineMaximumCenterWaterDepthMeters = 1.25d;
    public const double GeothermalMaximumHeightSpanMeters = 1.50d;
    public const double QuietMaximumHazard01 = .40d;
    public const double QuietMaximumHeightSpanMeters = .90d;
    public const double StableMaximumHazard01 = .35d;
    public const double StableMaximumHeightSpanMeters = .65d;
    public const double DrySiteMaximumWaterDepthMeters = .20d;

    public static UnderworldStationPlacementDecision Evaluate(
        UnderworldStationDefinition station,
        UnderworldStationPlacementSite site)
    {
        if (station is null) throw new ArgumentNullException(nameof(station));
        if (!Finite(site.CenterWaterDepth) || !Finite(site.MinimumWaterDepth) ||
            !Finite(site.MaximumWaterDepth) || !Finite(site.HeightSpanMeters) ||
            !Finite(site.MaximumHazard01))
            return UnderworldStationPlacementDecision.Reject("Station site sample is invalid.");
        if (!site.IsUnderworld)
            return UnderworldStationPlacementDecision.Reject(station.Name + " can only be placed inside the active Underworld instance.");
        if (site.CenterBiome != station.Biome)
            return UnderworldStationPlacementDecision.Reject(station.Name + " belongs in " + Display(station.Biome) + ".");

        return station.PlacementRule switch
        {
            UnderworldStationPlacementRule.OpenGround =>
                DryAndFlat(site, OpenGroundMaximumWaterDepth, OpenGroundMaximumHeightSpanMeters)
                    ? UnderworldStationPlacementDecision.Accept(station.Name + " has open, dry ground.")
                    : UnderworldStationPlacementDecision.Reject(station.Name + " needs open, dry ground with modest relief."),

            UnderworldStationPlacementRule.Shoreline =>
                site.CenterWaterDepth <= ShorelineMaximumCenterWaterDepthMeters &&
                site.MinimumWaterDepth <= ShorelineDryThresholdMeters &&
                site.MaximumWaterDepth >= ShorelineWetThresholdMeters
                    ? UnderworldStationPlacementDecision.Accept(station.Name + " straddles a Blackwater shoreline.")
                    : UnderworldStationPlacementDecision.Reject(station.Name + " must straddle dry bank and Blackwater."),

            UnderworldStationPlacementRule.GeothermalVent =>
                site.MaximumWaterDepth <= DrySiteMaximumWaterDepthMeters &&
                site.HeightSpanMeters <= GeothermalMaximumHeightSpanMeters &&
                site.GeothermalVentNearby
                    ? UnderworldStationPlacementDecision.Accept(station.Name + " is bound to a live geothermal vent.")
                    : UnderworldStationPlacementDecision.Reject(station.Name + " needs dry footing beside a live geothermal vent."),

            UnderworldStationPlacementRule.QuietGround =>
                site.MaximumWaterDepth <= DrySiteMaximumWaterDepthMeters &&
                site.MaximumHazard01 <= QuietMaximumHazard01 &&
                site.HeightSpanMeters <= QuietMaximumHeightSpanMeters
                    ? UnderworldStationPlacementDecision.Accept(station.Name + " is on a quiet Frozen-Cavern site.")
                    : UnderworldStationPlacementDecision.Reject(station.Name + " needs a quiet, low-hazard, level pocket of Frozen Cavern."),

            UnderworldStationPlacementRule.StableGround =>
                site.MaximumWaterDepth <= DrySiteMaximumWaterDepthMeters &&
                (site.MaximumHazard01 <= StableMaximumHazard01 || site.AnchorStabilizerNearby) &&
                site.HeightSpanMeters <= StableMaximumHeightSpanMeters
                    ? UnderworldStationPlacementDecision.Accept(site.AnchorStabilizerNearby
                        ? station.Name + " is stabilized by a deployed Anchor Spike."
                        : station.Name + " is anchored on naturally stable Fracture ground.")
                    : UnderworldStationPlacementDecision.Reject(station.Name + " needs a level Fracture shelf that is naturally stable or reinforced by an Anchor Spike."),

            UnderworldStationPlacementRule.DeepstoneAttuned =>
                site.MaximumWaterDepth <= DrySiteMaximumWaterDepthMeters &&
                site.HeightSpanMeters <= OpenGroundMaximumHeightSpanMeters &&
                site.DeepstoneAttuned
                    ? UnderworldStationPlacementDecision.Accept(station.Name + " is attuned to the completed Decay Deepstone.")
                    : UnderworldStationPlacementDecision.Reject(station.Name + " requires dry Great-Decay ground and the attuned Decay Deepstone."),

            _ => throw new InvalidOperationException("Unhandled Underworld station placement rule " + station.PlacementRule + "."),
        };
    }

    private static bool DryAndFlat(UnderworldStationPlacementSite site, double maxWater, double maxSpan) =>
        site.MaximumWaterDepth <= maxWater && site.HeightSpanMeters <= maxSpan;

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static string Display(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => "the Fungal Forest",
        UnderworldTerrainBiome.BlackwaterDeep => "the Blackwater Deep",
        UnderworldTerrainBiome.SulfurousWastes => "the Sulfurous Wastes",
        UnderworldTerrainBiome.FrozenCaverns => "the Frozen Caverns",
        UnderworldTerrainBiome.FractureZones => "the Fracture Zones",
        UnderworldTerrainBiome.GreatDecay => "the Great Decay",
        _ => biome.ToString(),
    };
}
