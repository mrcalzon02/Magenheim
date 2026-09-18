using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Deterministic logical-map rasterization. The raster is built from logical Underworld coordinates
/// and the surface world's seed; physical host coordinates never enter this boundary.
/// </summary>
public static class UnderworldMapRaster
{
    public static UnderworldTerrainBiome[] BuildBiomeRaster(
        UnderworldSpatialDomainDefinition domain,
        int surfaceSeed,
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
            var logical = UnderworldMapPresentation.CellToLogical(viewport, x, y);
            var index = UnderworldMapProjection.CellIndex(width, height, x, y);
            if (logical.X * logical.X + logical.Z * logical.Z > domain.RadiusMeters * domain.RadiusMeters)
            {
                // Corners of the square texture lie outside the circular playable domain. The caller
                // owns masking/presentation; no fake physical geography is generated there.
                result[index] = outsideBiome;
                continue;
            }
            result[index] = UnderworldMapProjection.UnderworldBiomeAt(
                domain, surfaceSeed, logical.X, logical.Z);
        }
        return result;
    }
}
