using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime ownership/streaming authority for native Underworld chunks. Terrain authority remains
/// materialization-neutral; after residency changes, the downstream Unity materializer is reconciled.
/// Surface WorldGenerator is not part of this pipeline.
/// </summary>
internal sealed class UnderworldInstanceChunkStreamingRuntime
{
    private const int DefaultStreamingRadiusChunks = 2;

    private readonly UnderworldRuntimeServices _services;
    private readonly UnderworldInstanceChunkGrid _grid;
    private readonly ManualLogSource _log;
    private readonly Dictionary<UnderworldInstanceChunkKey, UnderworldInstanceChunkSample> _loaded = new();
    private string? _loadedInstanceId;

    internal UnderworldInstanceChunkStreamingRuntime(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _grid = new UnderworldInstanceChunkGrid(services.TerrainDomain);
    }

    internal UnderworldInstanceChunkGrid Grid => _grid;
    internal IReadOnlyDictionary<UnderworldInstanceChunkKey, UnderworldInstanceChunkSample> LoadedChunks => _loaded;

    /// <summary>
    /// Reconciles the resident chunk set around every authoritative native instance-space focus point.
    /// Residency is the union of all focus neighborhoods so one player cannot evict terrain required by
    /// another. Returns false and clears residency whenever no authoritative Underworld instance is active.
    /// </summary>
    internal bool Reconcile(IEnumerable<(double X, double Z)> focuses, int radiusChunks = DefaultStreamingRadiusChunks)
    {
        if (focuses is null) throw new ArgumentNullException(nameof(focuses));
        if (radiusChunks < 0) throw new ArgumentOutOfRangeException(nameof(radiusChunks));
        var identity = _services.InstanceLifecycle.Identity;
        var phase = _services.InstanceLifecycle.Phase;
        if (identity is null || (phase != UnderworldInstancePhase.Admitting && phase != UnderworldInstancePhase.Active))
        {
            Clear();
            return false;
        }

        if (_loadedInstanceId is not null && !string.Equals(_loadedInstanceId, identity.DerivedWorldId, StringComparison.Ordinal))
            Clear();
        _loadedInstanceId = identity.DerivedWorldId;

        var requiredSet = new HashSet<UnderworldInstanceChunkKey>();
        foreach (var focus in focuses)
        {
            var center = _grid.KeyAt(focus.X, focus.Z);
            foreach (var key in _grid.EnumerateSquare(center, radiusChunks))
                requiredSet.Add(key);
        }

        if (requiredSet.Count == 0)
        {
            Clear();
            return true;
        }

        var stale = new List<UnderworldInstanceChunkKey>();
        foreach (var key in _loaded.Keys)
            if (!requiredSet.Contains(key)) stale.Add(key);
        foreach (var key in stale) _loaded.Remove(key);

        var waterLevel = ZoneSystem.instance is null ? 30d : ZoneSystem.instance.m_waterLevel;
        foreach (var key in requiredSet)
            if (!_loaded.ContainsKey(key))
                _loaded.Add(key, UnderworldInstanceChunkSampler.Sample(_grid, key, identity.DerivedSeed32, waterLevel));

        _services.ChunkMaterializer.Reconcile();
        return true;
    }

    internal bool TryGetChunk(UnderworldInstanceChunkKey key, out UnderworldInstanceChunkSample? sample) =>
        _loaded.TryGetValue(key, out sample);

    internal void Clear()
    {
        if (_loaded.Count > 0) _log.LogDebug($"Released {_loaded.Count} native Underworld chunk payload(s).");
        _loaded.Clear();
        _loadedInstanceId = null;
        _services.ChunkMaterializer.Reconcile();
    }
}
