using System;

namespace Magenheim.Core.Underworld;

/// <summary>Materialization-neutral terrain payload for one native Underworld chunk.</summary>
public sealed record UnderworldInstanceChunkSample(
    UnderworldInstanceChunkKey Key,
    int VerticesPerEdge,
    double[] Heights,
    UnderworldTerrainBiome[] Biomes,
    bool[] Admitted);

public static class UnderworldInstanceChunkSampler
{
    public static UnderworldInstanceChunkSample Sample(UnderworldInstanceChunkGrid grid, UnderworldInstanceChunkKey key, int seed, double waterLevel)
    {
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (!grid.IntersectsPlayableDomain(key)) throw new InvalidOperationException("Cannot materialize a chunk outside the Underworld playable domain.");

        var bounds = grid.Bounds(key);
        var edge = grid.VerticesPerEdge;
        var count = checked(edge * edge);
        var heights = new double[count];
        var biomes = new UnderworldTerrainBiome[count];
        var admitted = new bool[count];

        for (var z = 0; z < edge; z++)
        for (var x = 0; x < edge; x++)
        {
            var px = bounds.MinimumX + x * grid.VertexSpacingMeters;
            var pz = bounds.MinimumZ + z * grid.VertexSpacingMeters;
            var noise = UnderworldTerrainNoise.Fractal01(seed, px, pz);
            var result = UnderworldTerrainLifecycle.Evaluate(
                grid.Domain,
                new UnderworldTerrainSample(px, 0d, pz, waterLevel, 0d, noise),
                seed);
            var index = z * edge + x;
            admitted[index] = result.Admitted;
            heights[index] = result.Height;
            biomes[index] = result.Biome;
        }

        return new UnderworldInstanceChunkSample(key, edge, heights, biomes, admitted);
    }
}
