using System;

namespace Magenheim.Core.Underworld;

public static class UnderworldSkyLighting
{
    // Presentation datum in native instance metres, independent of the engine layer offset.
    // This is a visual haze line, not a collision/build/flight ceiling.
    public const double HazeTopMeters = 4800d;
    public const double MaximumTerrainHeightMeters = 5100d;

    public const double MaximumDirectionalDay = .24d;
    public const double MaximumDirectionalNight = .075d;

    public static double FogBrightness(double dayFraction) =>
        .30d + .35d * (Exposure(dayFraction) - NightExposure) / (DayExposure - NightExposure);

    public const double NightExposure = .38d;
    public const double DayExposure = .80d;

    /// <summary>Native normalized day fraction; midnight=0, noon=.5. No independent clock.</summary>
    public static double Exposure(double dayFraction)
    {
        if (double.IsNaN(dayFraction) || double.IsInfinity(dayFraction))
            throw new ArgumentOutOfRangeException(nameof(dayFraction));
        var phase = dayFraction - Math.Floor(dayFraction);
        var daylight = .5d - .5d * Math.Cos(phase * Math.PI * 2d);
        return NightExposure + (DayExposure - NightExposure) * daylight;
    }
}
