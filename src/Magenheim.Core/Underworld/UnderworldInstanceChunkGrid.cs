using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Deterministic native chunk addressing for the dedicated Underworld instance.
/// Chunk keys and bounds are instance-local and cannot encode Surface-world placement.
/// </summary>
public sealed class UnderworldInstanceChunkGrid
{
    public const int DefaultChunkSizeMeters = 64;
    public const int DefaultVertexSpacingMeters = 2;

    public UnderworldInstanceChunkGrid(UnderworldInstanceTerrainDomain domain, int chunkSizeMeters = DefaultChunkSizeMeters, int vertexSpacingMeters = DefaultVertexSpacingMeters)
    {
        Domain = domain ?? throw new ArgumentNullException(nameof(domain));
        if (chunkSizeMeters <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSizeMeters));
        if (vertexSpacingMeters <= 0 || chunkSizeMeters % vertexSpacingMeters != 0)
            throw new ArgumentOutOfRangeException(nameof(vertexSpacingMeters), "Vertex spacing must divide chunk size exactly.");
        ChunkSizeMeters = chunkSizeMeters;
        VertexSpacingMeters = vertexSpacingMeters;
    }

    public UnderworldInstanceTerrainDomain Domain { get; }
    public int ChunkSizeMeters { get; }
    public int VertexSpacingMeters { get; }
    public int VerticesPerEdge => ChunkSizeMeters / VertexSpacingMeters + 1;

    public UnderworldInstanceChunkKey KeyAt(double x, double z) =>
        new UnderworldInstanceChunkKey(FloorDiv(x, ChunkSizeMeters), FloorDiv(z, ChunkSizeMeters));

    public UnderworldInstanceChunkBounds Bounds(UnderworldInstanceChunkKey key)
    {
        var minX = key.X * (double)ChunkSizeMeters;
        var minZ = key.Z * (double)ChunkSizeMeters;
        return new UnderworldInstanceChunkBounds(minX, minZ, minX + ChunkSizeMeters, minZ + ChunkSizeMeters);
    }

    public bool IntersectsPlayableDomain(UnderworldInstanceChunkKey key)
    {
        var b = Bounds(key);
        var nearestX = Clamp(0d, b.MinimumX, b.MaximumX);
        var nearestZ = Clamp(0d, b.MinimumZ, b.MaximumZ);
        return nearestX * nearestX + nearestZ * nearestZ <= Domain.RadiusMeters * Domain.RadiusMeters;
    }

    public IReadOnlyList<UnderworldInstanceChunkKey> EnumerateSquare(UnderworldInstanceChunkKey center, int radiusChunks)
    {
        if (radiusChunks < 0) throw new ArgumentOutOfRangeException(nameof(radiusChunks));
        var result = new List<UnderworldInstanceChunkKey>();
        for (var z = center.Z - radiusChunks; z <= center.Z + radiusChunks; z++)
        for (var x = center.X - radiusChunks; x <= center.X + radiusChunks; x++)
        {
            var key = new UnderworldInstanceChunkKey(x, z);
            if (IntersectsPlayableDomain(key)) result.Add(key);
        }
        return result.AsReadOnly();
    }

    private static int FloorDiv(double value, int divisor) => checked((int)Math.Floor(value / divisor));
    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;
}

public readonly record struct UnderworldInstanceChunkKey(int X, int Z);
public readonly record struct UnderworldInstanceChunkBounds(double MinimumX, double MinimumZ, double MaximumX, double MaximumZ);
