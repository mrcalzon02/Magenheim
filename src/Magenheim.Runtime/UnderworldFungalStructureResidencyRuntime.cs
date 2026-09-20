using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// First repeatable biome-structure consumer of the native Underworld admission framework.
/// Residency follows native chunk residency; durable generation identity remains in
/// UnderworldGeneratedObjectStateStore and never falls back to Surface ZoneSystem/ZDO placement.
/// </summary>
internal sealed class UnderworldFungalStructureResidencyRuntime
{
    private const string StructureKind = "fungal-spore-cairn";
    private const int PlacementSlot = 0;
    private readonly UnderworldRuntimeServices _services;
    private readonly Dictionary<UnderworldInstanceChunkKey, GameObject> _resident = new();

    internal UnderworldFungalStructureResidencyRuntime(UnderworldRuntimeServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal int ResidentCount => _resident.Count;

    internal void Reconcile()
    {
        var identity = _services.InstanceLifecycle.Identity;
        if (identity is null || _services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active)
        {
            Clear();
            return;
        }

        var chunks = _services.ChunkStreaming.LoadedChunks;
        var stale = new List<UnderworldInstanceChunkKey>();
        foreach (var pair in _resident)
            if (!chunks.ContainsKey(pair.Key)) stale.Add(pair.Key);
        foreach (var key in stale) Release(key);

        var authoritative = ZNet.instance is not null && ZNet.instance.IsServer();
        foreach (var pair in chunks)
        {
            if (_resident.ContainsKey(pair.Key) || !Eligible(identity, pair.Key)) continue;
            var bounds = _services.ChunkStreaming.Grid.Bounds(pair.Key);
            var x = (bounds.MinimumX + bounds.MaximumX) * 0.5d;
            var z = (bounds.MinimumZ + bounds.MaximumZ) * 0.5d;
            var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(x, 0d, z);
            if (!terrain.Admitted || terrain.Biome != UnderworldTerrainBiome.FungalForest) continue;

            var root = _services.StructureAdmission.Admit(
                identity, StructureKind, pair.Key, PlacementSlot, authoritative,
                () => ComposeCairn(new Vector3((float)x, (float)terrain.Height, (float)z), identity, pair.Key),
                out _);
            _resident.Add(pair.Key, root);
        }
    }

    internal void Clear()
    {
        foreach (var root in _resident.Values) if (root) UnityEngine.Object.Destroy(root);
        _resident.Clear();
    }

    private void Release(UnderworldInstanceChunkKey key)
    {
        if (!_resident.TryGetValue(key, out var root)) return;
        if (root) UnityEngine.Object.Destroy(root);
        _resident.Remove(key);
    }

    private static bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            // Sparse enough to remain a landmark family rather than ecology clutter.
            return (hash & 3) == 0;
        }
    }

    private static GameObject ComposeCairn(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FungalSporeCairn_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663;
        var random = new System.Random(seed);
        try
        {
            // A low stone/root-like pedestal with three oversized fungal caps. This is intentionally
            // self-contained geometry: later art passes may replace the visual grammar without changing
            // placement identity, persistence, or residency semantics proven by this first family.
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(root.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            pedestal.transform.localScale = new Vector3(2.8f, 0.7f, 2.8f);

            for (var i = 0; i < 3; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2d);
                var radius = 1.1f + (float)random.NextDouble() * 1.4f;
                var height = 2.8f + (float)random.NextDouble() * 2.6f;
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = $"Stem_{i}";
                stem.transform.SetParent(root.transform, false);
                stem.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f + 1.1f, Mathf.Sin(angle) * radius);
                stem.transform.localScale = new Vector3(0.45f + i * 0.08f, height * 0.5f, 0.45f + i * 0.08f);

                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = $"Cap_{i}";
                cap.transform.SetParent(root.transform, false);
                cap.transform.localPosition = stem.transform.localPosition + Vector3.up * (height * 0.55f);
                var capScale = 1.8f + (float)random.NextDouble() * 1.3f;
                cap.transform.localScale = new Vector3(capScale, capScale * 0.38f, capScale);
            }
            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }
}
