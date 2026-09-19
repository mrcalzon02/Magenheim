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
    private const float TerrainUvMetersPerTile = 4f;
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
            var localX = x * grid.VertexSpacingMeters;
            var localZ = z * grid.VertexSpacingMeters;
            vertices[index] = new Vector3(localX, (float)sample.Heights[index], localZ);

            // UVs live in absolute instance space rather than restarting at 0..1 on every chunk.
            // Adjacent chunks therefore share identical coordinates on their common edge and keep
            // a stable four-metre texel rhythm as streaming changes the resident chunk set.
            uv[index] = new Vector2(
                (float)((bounds.MinimumX + localX) / TerrainUvMetersPerTile),
                (float)((bounds.MinimumZ + localZ) / TerrainUvMetersPerTile));
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
        GameObject? node = null;
        try
        {
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.SetTriangles(triangles, 0, true);
            mesh.normals = BuildAuthoritativeNormals(sample, bounds.MinimumX, bounds.MinimumZ, grid.VertexSpacingMeters);
            mesh.RecalculateBounds();

            node = new GameObject(mesh.name);
            node.transform.SetParent(_root!.transform, false);
            node.transform.localPosition = new Vector3((float)bounds.MinimumX, 0f, (float)bounds.MinimumZ);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.AddComponent<MeshRenderer>().sharedMaterial = GetTerrainMaterial();
            node.AddComponent<MeshCollider>().sharedMesh = mesh;
            _materialized.Add(sample.Key, node);
            _log.LogDebug($"Materialized native Underworld chunk {sample.Key.X},{sample.Key.Z} with {triangles.Count / 3} terrain triangles.");
        }
        catch
        {
            // Materialization is transactional: a failed Unity component/shader/collider operation must
            // not leave an untracked node or mesh behind for every subsequent reconciliation attempt.
            if (node) UnityEngine.Object.Destroy(node);
            UnityEngine.Object.Destroy(mesh);
            throw;
        }
    }

    /// <summary>
    /// Computes normals from deterministic terrain samples in absolute instance space rather than
    /// from each mesh's private triangle set. Shared border vertices therefore receive identical
    /// normals regardless of chunk load order, eliminating the lighting seam produced by
    /// Mesh.RecalculateNormals at independently materialized chunk boundaries.
    /// </summary>
    private static Vector3[] BuildAuthoritativeNormals(
        UnderworldInstanceChunkSample sample,
        double minimumX,
        double minimumZ,
        double spacing)
    {
        var edge = sample.VerticesPerEdge;
        var normals = new Vector3[checked(edge * edge)];
        for (var z = 0; z < edge; z++)
        for (var x = 0; x < edge; x++)
        {
            var index = z * edge + x;
            if (!sample.Admitted[index])
            {
                normals[index] = Vector3.up;
                continue;
            }

            var worldX = minimumX + x * spacing;
            var worldZ = minimumZ + z * spacing;
            var center = sample.Heights[index];
            var left = SampleHeightOrFallback(worldX - spacing, worldZ, center);
            var right = SampleHeightOrFallback(worldX + spacing, worldZ, center);
            var down = SampleHeightOrFallback(worldX, worldZ - spacing, center);
            var up = SampleHeightOrFallback(worldX, worldZ + spacing, center);

            // Tangents span two sample intervals. Their cross product points upward and remains
            // identical for the same absolute vertex when it is represented by adjacent chunks.
            var tangentX = new Vector3((float)(spacing * 2d), (float)(right - left), 0f);
            var tangentZ = new Vector3(0f, (float)(up - down), (float)(spacing * 2d));
            normals[index] = Vector3.Cross(tangentZ, tangentX).normalized;
        }
        return normals;
    }

    private static double SampleHeightOrFallback(double x, double z, double fallback)
    {
        var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(x, 0d, z);
        return terrain.Admitted ? terrain.Height : fallback;
    }

    private Material GetTerrainMaterial()
    {
        if (_terrainMaterial) return _terrainMaterial;
        // Valheim does not ship Unity's built-in Standard shader in the player. Resolve a loaded
        // game surface shader through the same runtime authority used by Magenheim model assets so
        // native Underworld terrain can actually materialize in a live Valheim process.
        _terrainMaterial = new Material(ModelAssets.ResolveSurfaceShader())
        {
            name = "Magenheim_Underworld_Terrain_Runtime"
        };
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
