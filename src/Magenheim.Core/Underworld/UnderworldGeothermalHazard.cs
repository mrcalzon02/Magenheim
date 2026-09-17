using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Canonical authority for Underworld geothermal exposure sources.
/// Biome/runtime adapters identify a source and intensity; this policy converts that
/// signal into bounded thermal pressure without owning player state or damage.
/// </summary>
public static class UnderworldGeothermalHazard
{
    public const string VentField = "vent_field";
    public const string LavaChannel = "lava_channel";
    public const string ThermalSurge = "thermal_surge";

    public const float MaximumIntensity = 2f;
    public const float MaximumDeltaSeconds = 1f;

    private static readonly IReadOnlyDictionary<string, float> HeatPerSecond =
        new Dictionary<string, float>(StringComparer.Ordinal)
        {
            [VentField] = 8f,
            [LavaChannel] = 14f,
            [ThermalSurge] = 20f,
        };

    public static bool IsCanonical(string? hazardId)
        => hazardId is not null && HeatPerSecond.ContainsKey(hazardId);

    /// <summary>
    /// Converts one canonical hazard sample into thermal buildup units. Unknown hazards,
    /// malformed samples and non-positive intensity fail neutral. Intensity and frame time
    /// are bounded so a bad adapter or hitch cannot inject an unbounded heat spike.
    /// </summary>
    public static float Exposure(string? hazardId, float intensity, float deltaSeconds)
    {
        if (hazardId is null || !HeatPerSecond.TryGetValue(hazardId, out var rate)) return 0f;
        if (!IsFinite(intensity) || !IsFinite(deltaSeconds) || intensity <= 0f || deltaSeconds <= 0f) return 0f;

        var boundedIntensity = Clamp(intensity, 0f, MaximumIntensity);
        var boundedDelta = Clamp(deltaSeconds, 0f, MaximumDeltaSeconds);
        return rate * boundedIntensity * boundedDelta;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static float Clamp(float value, float minimum, float maximum)
        => value < minimum ? minimum : value > maximum ? maximum : value;
}
