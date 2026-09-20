using System;

namespace Magenheim.Core.Underworld;

public enum MagenheimMapLayer
{
    Surface = 0,
    Underworld = 1,
}

public readonly struct MagenheimMapPoint
{
    public MagenheimMapPoint(double x, double z) { X = x; Z = z; }
    public double X { get; }
    public double Z { get; }
}

/// <summary>
/// Native Underworld map helpers. Underworld coordinates are instance-local map coordinates;
/// there is no Surface host-band projection in this API.
/// </summary>
public static class UnderworldMapProjection
{
    public static UnderworldTerrainBiome UnderworldBiomeAt(
        UnderworldInstanceTerrainDomain domain, int instanceSeed, double logicalX, double logicalZ)
    {
        if (domain is null) throw new ArgumentNullException(nameof(domain));
        if (!Finite(logicalX) || !Finite(logicalZ)) throw new ArgumentOutOfRangeException(nameof(logicalX));
        if (logicalX * logicalX + logicalZ * logicalZ > domain.RadiusMeters * domain.RadiusMeters)
            throw new InvalidOperationException("Underworld map point lies outside the playable radius.");

        var result = UnderworldTerrainLifecycle.Evaluate(
            domain,
            new UnderworldTerrainSample(logicalX, 0d, logicalZ, UnderworldTerrainLifecycle.BaseElevationMeters, 0d,
                UnderworldTerrainNoise.Fractal01(instanceSeed, logicalX, logicalZ)),
            instanceSeed);
        if (!result.Admitted) throw new InvalidOperationException("Underworld biome authority rejected an admitted map point.");
        return result.Biome;
    }

    public static int CellIndex(int width, int height, int x, int y)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (x < 0 || x >= width || y < 0 || y >= height) throw new ArgumentOutOfRangeException(nameof(x));
        return checked(y * width + x);
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
