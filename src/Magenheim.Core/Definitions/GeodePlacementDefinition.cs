using System;

namespace Magenheim.Core.Definitions;

/// <summary>
/// Pure, fingerprintable world-placement authority for one geode family. Values mirror the
/// behavioral fields consumed by Jotunn vegetation without depending on Unity or Valheim types.
/// </summary>
public sealed record GeodePlacementDefinition(
    bool BlockCheck,
    bool ForcePlacement,
    double MinPerZone,
    double MaxPerZone,
    double MinAltitude,
    double MaxAltitude,
    double MinOceanDepth,
    double MaxOceanDepth,
    double MinTerrainDelta,
    double MaxTerrainDelta,
    double TerrainDeltaRadius,
    double MinTilt,
    double MaxTilt,
    bool InForest,
    double ForestThresholdMin,
    double ForestThresholdMax,
    double ScaleMin,
    double ScaleMax,
    int GroupSizeMin,
    int GroupSizeMax,
    double GroupRadius,
    double GroundOffset)
{
    public static GeodePlacementDefinition ConservativeMeadows { get; } = new(
        BlockCheck: true,
        ForcePlacement: false,
        MinPerZone: 0d,
        MaxPerZone: 0.35d,
        MinAltitude: 1d,
        MaxAltitude: 1000d,
        MinOceanDepth: 0d,
        MaxOceanDepth: 0d,
        MinTerrainDelta: 0d,
        MaxTerrainDelta: 2d,
        TerrainDeltaRadius: 2d,
        MinTilt: 0d,
        MaxTilt: 35d,
        InForest: false,
        ForestThresholdMin: 0d,
        ForestThresholdMax: 1d,
        ScaleMin: 0.85d,
        ScaleMax: 1.15d,
        GroupSizeMin: 1,
        GroupSizeMax: 1,
        GroupRadius: 0d,
        GroundOffset: -0.10d);

    public void Validate(string geodeId)
    {
        var id = string.IsNullOrWhiteSpace(geodeId) ? "<unknown>" : geodeId;

        RequireRuntimeFloat(MinPerZone, id, nameof(MinPerZone));
        RequireRuntimeFloat(MaxPerZone, id, nameof(MaxPerZone));
        if (MinPerZone < 0d || MaxPerZone < 0d || MinPerZone > MaxPerZone)
            throw new InvalidOperationException($"Geode '{id}' placement count/chance range must satisfy 0 <= MinPerZone <= MaxPerZone.");

        ValidateOrderedRuntimeFloat(MinAltitude, MaxAltitude, id, "altitude");
        ValidateOrderedRuntimeFloat(MinOceanDepth, MaxOceanDepth, id, "ocean depth");
        ValidateOrderedRuntimeFloat(MinTerrainDelta, MaxTerrainDelta, id, "terrain delta");
        if (MinTerrainDelta < 0d)
            throw new InvalidOperationException($"Geode '{id}' placement minimum terrain delta cannot be negative.");

        RequireRuntimeFloat(TerrainDeltaRadius, id, nameof(TerrainDeltaRadius));
        if (TerrainDeltaRadius < 0d)
            throw new InvalidOperationException($"Geode '{id}' placement terrain-delta radius cannot be negative.");

        ValidateOrderedRuntimeFloat(MinTilt, MaxTilt, id, "tilt");
        if (MinTilt < 0d || MaxTilt > 90d)
            throw new InvalidOperationException($"Geode '{id}' placement tilt must remain within 0..90 degrees.");

        ValidateOrderedRuntimeFloat(ForestThresholdMin, ForestThresholdMax, id, "forest threshold");

        ValidateOrderedRuntimeFloat(ScaleMin, ScaleMax, id, "scale");
        if (ScaleMin <= 0d)
            throw new InvalidOperationException($"Geode '{id}' placement scale must be greater than zero.");

        if (GroupSizeMin < 1 || GroupSizeMax < GroupSizeMin)
            throw new InvalidOperationException($"Geode '{id}' placement group sizes must satisfy 1 <= GroupSizeMin <= GroupSizeMax.");

        RequireRuntimeFloat(GroupRadius, id, nameof(GroupRadius));
        if (GroupRadius < 0d)
            throw new InvalidOperationException($"Geode '{id}' placement group radius cannot be negative.");

        RequireRuntimeFloat(GroundOffset, id, nameof(GroundOffset));
    }

    private static void ValidateOrderedRuntimeFloat(double minimum, double maximum, string id, string label)
    {
        RequireRuntimeFloat(minimum, id, $"minimum {label}");
        RequireRuntimeFloat(maximum, id, $"maximum {label}");
        if (minimum > maximum)
            throw new InvalidOperationException($"Geode '{id}' placement {label} minimum cannot exceed maximum.");
    }

    private static void RequireRuntimeFloat(double value, string id, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            throw new InvalidOperationException(
                $"Geode '{id}' placement field '{field}' must be finite and representable by the runtime float API.");
        }
    }
}
