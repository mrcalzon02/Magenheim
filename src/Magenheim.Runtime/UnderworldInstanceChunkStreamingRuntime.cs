using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime ownership/streaming authority for native Underworld chunks. This class deliberately
/// materializes no Unity or Valheim terrain objects; renderer/materializer adapters consume the
/// immutable chunk samples exposed here. Surface WorldGenerator is not part of this pipeline.
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
    /// Reconciles the resident chunk set around a native instance-space focus point. Returns false
    /// and clears residency whenever no authoritative Underworld instance is active.
    /// </summary>
    internal bool Reconcile(double focusX, double focusZ, int radiusChunks = DefaultStreamingRadiusChunks)
    {
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

        var center = _grid.KeyAt(focusX, focusZ);
        var required = _grid.EnumerateSquare(center, radiusChunks);
        var requiredSet = new HashSet<UnderworldInstanceChunkKey>(required);

        var stale = new List<UnderworldInstanceChunkKey>();
        foreach (var key in _loaded.Keys)
            if (!requiredSet.Contains(key)) stale.Add(key);
        foreach (var key in stale) _loaded.Remove(key);

        var waterLevel = ZoneSystem.instance is null ? 30d : ZoneSystem.instance.m_waterLevel;
        foreach (var key in required)
            if (!_loaded.ContainsKey(key))
                _loaded.Add(key, UnderworldInstanceChunkSampler.Sample(_grid, key, identity.DerivedSeed32, waterLevel));

        return true;
    }

    internal bool TryGetChunk(UnderworldInstanceChunkKey key, out UnderworldInstanceChunkSample? sample) =>
        _loaded.TryGetValue(key, out sample);

    internal void Clear()
    {
        if (_loaded.Count > 0) _log.LogDebug($"Released {_loaded.Count} native Underworld chunk payload(s).");
        _loaded.Clear();
        _loadedInstanceId = null;
    }
}
