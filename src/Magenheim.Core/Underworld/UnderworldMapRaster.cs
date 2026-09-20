using System;

namespace Magenheim.Core.Underworld;

/// <summary>Deterministic rasterization in native Underworld instance coordinates.</summary>
public static class UnderworldMapRaster
{
    public static UnderworldTerrainBiome[] BuildBiomeRaster(
        UnderworldInstanceTerrainDomain domain,
        int instanceSeed,
        int width,
        int height,
        UnderworldTerrainBiome outsideBiome = UnderworldTerrainBiome.FungalForest)
    {
        if (domain is null) throw new ArgumentNullException(nameof(domain));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        var viewport = UnderworldMapPresentation.CreateUnderworldViewport(domain, width, height);
        var result = new UnderworldTerrainBiome[checked(width * height)];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var logical = UnderworldMapPresentation.CellCenterToLogical(viewport, x, y);
            var index = UnderworldMapProjection.CellIndex(width, height, x, y);
            if (logical.X * logical.X + logical.Z * logical.Z > domain.RadiusMeters * domain.RadiusMeters)
            {
                result[index] = outsideBiome;
                continue;
            }

            var terrain = UnderworldTerrainLifecycle.Evaluate(
                domain,
                new UnderworldTerrainSample(
                    logical.X,
                    0d,
                    logical.Z,
                    UnderworldTerrainLifecycle.BaseElevationMeters,
                    0d,
                    UnderworldTerrainNoise.Fractal01(instanceSeed, logical.X, logical.Z)),
                instanceSeed);
            if (!terrain.Admitted)
                throw new InvalidOperationException("Native Underworld terrain rejected an in-domain map cell.");
            result[index] = terrain.Biome;
        }
        return result;
    }
}
