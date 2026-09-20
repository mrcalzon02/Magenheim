using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// One native instance-space point that chunk residency is kept loaded around.
///
/// Explicitly a struct and not a (double X, double Z) tuple: System.ValueTuple is a separate
/// facade assembly that Valheim and BepInEx do not ship. A tuple compiles here and then throws
/// TypeLoadException at registration, which silently removes every registrar behind it.
/// </summary>
internal readonly struct UnderworldChunkFocus
{
    internal UnderworldChunkFocus(double x, double z)
    {
        X = x;
        Z = z;
    }

    internal double X { get; }
    internal double Z { get; }
}

/// <summary>
/// Runtime ownership/streaming authority for native Underworld chunks. Terrain authority remains
/// materialization-neutral; after residency changes, downstream Unity presentation and native
/// biome-structure residency are reconciled. Surface WorldGenerator is not part of this pipeline.
/// </summary>
internal sealed class UnderworldInstanceChunkStreamingRuntime
{
    private const int DefaultStreamingRadiusChunks = 2;

    private readonly UnderworldRuntimeServices _services;
    private readonly UnderworldInstanceChunkGrid _grid;
    private readonly ManualLogSource _log;
    private readonly Dictionary<UnderworldInstanceChunkKey, UnderworldInstanceChunkSample> _loaded = new();
    private string? _loadedInstanceKey;

    internal UnderworldInstanceChunkStreamingRuntime(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _grid = new UnderworldInstanceChunkGrid(services.TerrainDomain);
    }

    internal UnderworldInstanceChunkGrid Grid => _grid;
    internal IReadOnlyDictionary<UnderworldInstanceChunkKey, UnderworldInstanceChunkSample> LoadedChunks => _loaded;

    internal bool Reconcile(IEnumerable<UnderworldChunkFocus> focuses, int radiusChunks = DefaultStreamingRadiusChunks)
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

        var instanceKey = InstanceKey(identity);
        if (_loadedInstanceKey is not null && !string.Equals(_loadedInstanceKey, instanceKey, StringComparison.Ordinal))
            Clear();
        _loadedInstanceKey = instanceKey;

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
        // Structures consume the exact same resident native chunk set as terrain. They are admitted
        // only after terrain payload/materialization exists, so placement height and biome authority
        // cannot race a chunk that has not yet been admitted into the derived instance.
        _services.FungalStructureResidency.Reconcile();
        return true;
    }

    internal bool TryGetChunk(UnderworldInstanceChunkKey key, out UnderworldInstanceChunkSample? sample) =>
        _loaded.TryGetValue(key, out sample);

    internal void Clear()
    {
        _services.FungalStructureResidency.Clear();
        if (_loaded.Count > 0) _log.LogDebug($"Released {_loaded.Count} native Underworld chunk payload(s).");
        _loaded.Clear();
        _loadedInstanceKey = null;
        _services.ChunkMaterializer.Reconcile();
    }

    private static string InstanceKey(UnderworldWorldIdentity identity) =>
        identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeedFingerprint;
}
