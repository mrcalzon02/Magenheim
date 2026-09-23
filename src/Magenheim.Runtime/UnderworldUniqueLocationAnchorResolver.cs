using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Resolves a canonical unique-location identity to exactly one native Underworld chunk for the
/// active derived world. This is location placement authority, not player discovery state: callers
/// persist/reveal the stable location id and ask this resolver for its world-specific anchor.
/// </summary>
internal sealed class UnderworldUniqueLocationAnchorResolver
{
    private readonly UnderworldRuntimeServices _services;
    private readonly Dictionary<CacheKey, UnderworldUniqueLocationAnchor> _cache = new();

    internal UnderworldUniqueLocationAnchorResolver(UnderworldRuntimeServices services) =>
        _services = services ?? throw new ArgumentNullException(nameof(services));

    internal UnderworldUniqueLocationAnchor Resolve(string locationId, UnderworldTerrainBiome biome)
    {
        if (string.IsNullOrWhiteSpace(locationId))
            throw new ArgumentException("Canonical Underworld location id is required.", nameof(locationId));

        var identity = _services.InstanceLifecycle.Identity
            ?? throw new InvalidOperationException("Cannot resolve an Underworld unique location without an active derived-world identity.");
        var normalized = locationId.Trim();
        var cacheKey = new CacheKey(identity.DerivedWorldId, identity.DerivedSeed32, normalized, biome);
        if (_cache.TryGetValue(cacheKey, out var cached)) return cached;

        var grid = _services.ChunkStreaming.Grid;
        var radiusChunks = checked((int)Math.Ceiling(_services.TerrainDomain.RadiusMeters / grid.ChunkSizeMeters));
        UnderworldUniqueLocationAnchor? winner = null;
        ulong winnerScore = ulong.MaxValue;

        for (var z = -radiusChunks; z <= radiusChunks; z++)
        for (var x = -radiusChunks; x <= radiusChunks; x++)
        {
            var key = new UnderworldInstanceChunkKey(x, z);
            if (!grid.IntersectsPlayableDomain(key)) continue;

            var bounds = grid.Bounds(key);
            var localX = (bounds.MinimumX + bounds.MaximumX) * 0.5d;
            var localZ = (bounds.MinimumZ + bounds.MaximumZ) * 0.5d;
            var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(localX, 0d, localZ);
            if (!terrain.Admitted || terrain.Biome != biome) continue;

            var score = Score(identity.DerivedSeed32, normalized, key);
            if (winner is not null && score >= winnerScore) continue;

            winnerScore = score;
            var engine = UnderworldInstanceLayer.ToEngine(new Vector3((float)localX, (float)terrain.Height, (float)localZ));
            winner = new UnderworldUniqueLocationAnchor(normalized, biome, key, localX, terrain.Height, localZ, engine);
        }

        if (winner is null)
            throw new InvalidOperationException($"No admitted '{biome}' terrain exists for unique Underworld location '{normalized}'.");

        _cache.Add(cacheKey, winner.Value);
        return winner.Value;
    }

    internal void Clear() => _cache.Clear();

    private static ulong Score(int seed, string locationId, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            // Stable FNV-1a style mixing. Never use string.GetHashCode here: runtime-specific hash
            // randomization would move a boss location between server processes.
            ulong hash = 14695981039346656037UL;
            hash = (hash ^ (uint)seed) * 1099511628211UL;
            for (var i = 0; i < locationId.Length; i++) hash = (hash ^ locationId[i]) * 1099511628211UL;
            hash = (hash ^ (uint)key.X) * 1099511628211UL;
            hash = (hash ^ (uint)key.Z) * 1099511628211UL;
            hash ^= hash >> 32;
            hash *= 0xd6e8feb86659fd93UL;
            hash ^= hash >> 32;
            return hash;
        }
    }

    private readonly record struct CacheKey(string DerivedWorldId, int Seed, string LocationId, UnderworldTerrainBiome Biome);
}

internal readonly record struct UnderworldUniqueLocationAnchor(
    string LocationId,
    UnderworldTerrainBiome Biome,
    UnderworldInstanceChunkKey Chunk,
    double LocalX,
    double LocalY,
    double LocalZ,
    Vector3 EnginePosition);
