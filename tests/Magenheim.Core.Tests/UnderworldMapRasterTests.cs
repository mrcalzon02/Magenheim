using System;
using Magenheim.Core.Underworld;

internal static class UnderworldMapRasterTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld map raster assertion {assertions} failed: {message}");
        }

        var domain = UnderworldInstanceTerrainDomain.CreateDefault();
        const int seed = 12345;
        var raster = UnderworldMapRaster.BuildBiomeRaster(domain, seed, 33, 33);
        Assert(raster.Length == 1089, "Raster dimensions must be exact.");
        var center = UnderworldMapProjection.CellIndex(33, 33, 16, 16);
        Assert(raster[center] == UnderworldTerrainBiome.FungalForest,
            "Logical map center must rasterize the protected Fungal Forest arrival basin.");

        var repeat = UnderworldMapRaster.BuildBiomeRaster(domain, seed, 33, 33);
        for (var i = 0; i < raster.Length; i++)
            Assert(raster[i] == repeat[i], "Same instance seed must produce an identical logical biome raster.");

        var different = UnderworldMapRaster.BuildBiomeRaster(domain, seed + 1, 33, 33);
        var changed = false;
        for (var i = 0; i < raster.Length; i++) changed |= raster[i] != different[i];
        Assert(changed, "Changing the instance seed must be able to rotate/change the logical Underworld map.");
        return assertions;
    }
}
