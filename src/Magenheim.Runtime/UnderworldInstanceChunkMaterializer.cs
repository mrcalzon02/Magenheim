using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unity presentation adapter for authoritative native Underworld chunk samples.
/// It owns only presentation objects: terrain authority remains in Core and chunk
/// residency remains in UnderworldInstanceChunkStreamingRuntime.
/// </summary>
internal sealed class UnderworldInstanceChunkMaterializer
{
    private readonly UnderworldRuntimeServices _services;
    private readonly ManualLogSource _log;
    private readonly Dictionary<UnderworldInstanceChunkKey, GameObject> _materialized = new();
    private GameObject? _root;
    private Material? _terrainMaterial;

    internal UnderworldInstanceChunkMaterializer(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal int MaterializedCount => _materialized.Count;

    internal void Reconcile()
    {
        var resident = _services.ChunkStreaming.LoadedChunks;
        var stale = new List<UnderworldInstanceChunkKey>();
        foreach (var pair in _materialized)
            if (!resident.ContainsKey(pair.Key)) stale.Add(pair.Key);
        foreach (var key in stale) DestroyChunk(key);

        if (resident.Count == 0)
        {
            if (_root) UnityEngine.Object.Destroy(_root);
            _root = null;
            return;
        }

        EnsureRoot();
        foreach (var pair in resident)
            if (!_materialized.ContainsKey(pair.Key)) Materialize(pair.Value);
    }

    internal void Clear()
    {
        var keys = new List<UnderworldInstanceChunkKey>(_materialized.Keys);
        foreach (var key in keys) DestroyChunk(key);
        if (_root) UnityEngine.Object.Destroy(_root);
        _root = null;
        if (_terrainMaterial) UnityEngine.Object.Destroy(_terrainMaterial);
        _terrainMaterial = null;
    }

    private void EnsureRoot()
    {
        if (_root) return;
        _root = new GameObject("Magenheim_Underworld_InstanceTerrain");
    }

    private void Materialize(UnderworldInstanceChunkSample sample)
    {
        var edge = sample.VerticesPerEdge;
        var expected = checked(edge * edge);
        if (sample.Heights.Length != expected || sample.Admitted.Length != expected)
            throw new InvalidOperationException($"Underworld chunk {sample.Key} has inconsistent terrain payload dimensions.");

        var grid = _services.ChunkStreaming.Grid;
        var bounds = grid.Bounds(sample.Key);
        var vertices = new Vector3[expected];
        var uv = new Vector2[expected];
        for (var z = 0; z < edge; z++)
        for (var x = 0; x < edge; x++)
        {
            var index = z * edge + x;
            vertices[index] = new Vector3(x * grid.VertexSpacingMeters, (float)sample.Heights[index], z * grid.VertexSpacingMeters);
            uv[index] = new Vector2(x / (float)(edge - 1), z / (float)(edge - 1));
        }

        var triangles = new List<int>((edge - 1) * (edge - 1) * 6);
        for (var z = 0; z < edge - 1; z++)
        for (var x = 0; x < edge - 1; x++)
        {
            var a = z * edge + x;
            var b = a + 1;
            var c = a + edge;
            var d = c + 1;
            if (!sample.Admitted[a] || !sample.Admitted[b] || !sample.Admitted[c] || !sample.Admitted[d]) continue;
            triangles.Add(a); triangles.Add(c); triangles.Add(b);
            triangles.Add(b); triangles.Add(c); triangles.Add(d);
        }

        var mesh = new Mesh { name = $"Magenheim_Underworld_Chunk_{sample.Key.X}_{sample.Key.Z}" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.SetTriangles(triangles, 0, true);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var node = new GameObject(mesh.name);
        node.transform.SetParent(_root!.transform, false);
        node.transform.localPosition = new Vector3((float)bounds.MinimumX, 0f, (float)bounds.MinimumZ);
        node.AddComponent<MeshFilter>().sharedMesh = mesh;
        node.AddComponent<MeshRenderer>().sharedMaterial = GetTerrainMaterial();
        node.AddComponent<MeshCollider>().sharedMesh = mesh;
        _materialized.Add(sample.Key, node);
        _log.LogDebug($"Materialized native Underworld chunk {sample.Key.X},{sample.Key.Z} with {triangles.Count / 3} terrain triangles.");
    }

    private Material GetTerrainMaterial()
    {
        if (_terrainMaterial) return _terrainMaterial;
        var shader = Shader.Find("Standard");
        if (shader is null) throw new InvalidOperationException("Unity Standard shader is unavailable for Underworld terrain materialization.");
        _terrainMaterial = new Material(shader) { name = "Magenheim_Underworld_Terrain_Runtime" };
        return _terrainMaterial;
    }

    private void DestroyChunk(UnderworldInstanceChunkKey key)
    {
        if (!_materialized.TryGetValue(key, out var node)) return;
        _materialized.Remove(key);
        if (!node) return;
        var filter = node.GetComponent<MeshFilter>();
        if (filter && filter.sharedMesh) UnityEngine.Object.Destroy(filter.sharedMesh);
        UnityEngine.Object.Destroy(node);
    }
}
