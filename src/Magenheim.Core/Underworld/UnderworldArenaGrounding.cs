using System;

namespace Magenheim.Core.Underworld;

public static class UnderworldArenaGrounding
{
    public const float MinimumSpawnDistance = 768f;
    public const float ExteriorRadius = 24f;
    public const float FeatureClearance = 8f;
    public const double FloorHeightFromOrigin = .65d;
    // Valheim starts swimming 0.4 m before the Player prefab's 1.50 m swim depth.
    // Leave half a metre of wave clearance while keeping the entire ring inundated.
    public const double WadingDepth = .60d;
    // The Maw uses the Serpent's 1.16 m swim depth. Its capsule extends 1.02 m
    // below its pivot at native scale, or 1.592 m after the two encounter scales.
    // Allow 0.20 m below the capsule at its swimming equilibrium.
    public const float MinimumPoolDepth = 2.96f;
    public const float MaximumPoolDepth = 3.55f;
    public const float BossSwimmingDepth = 1.16f;
    private const double FloorRadius = 13d;
    private const double FoundationRadius = 14d;
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
        Func<double, double, double> groundHeight, out double originHeight) =>
        TryResolveGrade(x, z, centerHeight, waterLevel, drownedRing, groundHeight, out originHeight, out _);

    public static bool TryResolveGrade(
        double x, double z, double centerHeight, double waterLevel, bool drownedRing,
        Func<double, double, double> groundHeight, out double originHeight, out double footingHeight)
    {
        originHeight = centerHeight;
        footingHeight = centerHeight;
        if (double.IsNaN(centerHeight) || double.IsInfinity(centerHeight)) return false;
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

            var edge = groundHeight(x + dx * FoundationRadius, z + dz * FoundationRadius);
            var approach = groundHeight(x + dx * 21d, z + dz * 21d);
            if (double.IsNaN(edge) || double.IsInfinity(edge) ||
                double.IsNaN(approach) || double.IsInfinity(approach) ||
                Math.Abs(edge - centerHeight) > (drownedRing ? 1.15d : 1.75d) ||
                Math.Abs(approach - centerHeight) > (drownedRing ? 1.15d : 1.75d))
                return false;
            footingHeight = Math.Min(footingHeight, edge);
        }
        if (!drownedRing && floorHigh - floorLow > MaxLandFloorRelief) return false;

        // Near Valheim heightmaps triangulate the metre grid. Include vertices adjacent
        // to the rim so a mesh triangle cannot poke through between the radial probes.
        const double vertexMargin = 1.42d;
        var testRadius = FoundationRadius + vertexMargin;
        for (var gx = Math.Ceiling(x - testRadius); gx <= x + testRadius; gx++)
        for (var gz = Math.Ceiling(z - testRadius); gz <= z + testRadius; gz++)
        {
            var dx = gx - x;
            var dz = gz - z;
            var radiusSquared = dx * dx + dz * dz;
            if (radiusSquared > testRadius * testRadius) continue;
            var ground = groundHeight(gx, gz);
            if (double.IsNaN(ground) || double.IsInfinity(ground)) return false;
            if (drownedRing && radiusSquared <= Math.Pow(DrownedRingInnerRadius + vertexMargin, 2d) &&
                (waterLevel - ground < MinimumPoolDepth || waterLevel - ground > MaximumPoolDepth))
                return false;
            footingHeight = Math.Min(footingHeight, ground);
            if (radiusSquared > Math.Pow(FloorRadius + vertexMargin, 2d)) continue;
            floorLow = Math.Min(floorLow, ground);
            floorHigh = Math.Max(floorHigh, ground);
        }

        footingHeight = Math.Min(footingHeight, floorLow) - TerrainClearance;

        if (drownedRing)
        {
            var floor = waterLevel - WadingDepth;
            if (floorHigh > floor - TerrainClearance || footingHeight < waterLevel - MaximumPoolDepth - TerrainClearance)
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
