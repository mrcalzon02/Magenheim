using System;

namespace Magenheim.Core.Underworld;

public static class UnderworldArenaGrounding
{
    public const float MinimumSpawnDistance = 768f;
    public const float ExteriorRadius = 24f;
    public const float FeatureClearance = 8f;
    public const double FloorHeightFromOrigin = .65d;
    public const double WadingDepth = .60d;
    public const float MinimumPoolDepth = 1.9f;
    public const float MaximumPoolDepth = 3.2f;
    private const double FloorRadius = 13d;
    private const double DrownedRingInnerRadius = 10d;
    private const double TerrainClearance = .10d;
    private const double MaxLandFloorRelief = 1.35d;

    public static bool IsOutsideArrival(double x, double z) =>
        x * x + z * z >= MinimumSpawnDistance * MinimumSpawnDistance;

    public static bool HasFeatureClearance(double dx, double dz, double firstRadius, double secondRadius)
    {
        var required = firstRadius + secondRadius + FeatureClearance;
        return dx * dx + dz * dz >= required * required;
    }

    public static bool TryResolveOriginHeight(
        double x, double z, double centerHeight, double waterLevel, bool drownedRing,
        Func<double, double, double> groundHeight, out double originHeight)
    {
        originHeight = centerHeight;
        if (drownedRing
            ? waterLevel - centerHeight < MinimumPoolDepth || waterLevel - centerHeight > MaximumPoolDepth
            : centerHeight < waterLevel + 1d)
            return false;

        var floorHigh = drownedRing ? double.NegativeInfinity : centerHeight;
        var floorLow = drownedRing ? double.PositiveInfinity : centerHeight;
        for (var sample = 0; sample < 24; sample++)
        {
            var angle = sample * Math.PI / 12d;
            var dx = Math.Cos(angle);
            var dz = Math.Sin(angle);
            foreach (var radius in FloorSampleRadii)
            {
                var ground = groundHeight(x + dx * radius, z + dz * radius);
                if (double.IsNaN(ground) || double.IsInfinity(ground)) return false;
                if (drownedRing && radius < DrownedRingInnerRadius)
                {
                    if (waterLevel - ground < MinimumPoolDepth || waterLevel - ground > MaximumPoolDepth)
                        return false;
                    continue;
                }

                floorHigh = Math.Max(floorHigh, ground);
                floorLow = Math.Min(floorLow, ground);
            }

            var approach = groundHeight(x + dx * 21d, z + dz * 21d);
            if (double.IsNaN(approach) || double.IsInfinity(approach) ||
                Math.Abs(approach - centerHeight) > (drownedRing ? 1.15d : 1.75d))
                return false;
        }

        if (drownedRing)
        {
            var floor = waterLevel - WadingDepth;
            if (floorHigh > floor - TerrainClearance || floorLow < waterLevel - MaximumPoolDepth)
                return false;
            originHeight = floor - FloorHeightFromOrigin;
            return true;
        }

        if (floorHigh - floorLow > MaxLandFloorRelief || floorLow < waterLevel + .25d) return false;
        originHeight = floorHigh + TerrainClearance - FloorHeightFromOrigin;
        return true;
    }

    private static readonly double[] FloorSampleRadii = { 4d, 8d, 10d, 11.5d, FloorRadius };
}
