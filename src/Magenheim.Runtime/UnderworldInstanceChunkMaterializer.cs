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
    private readonly Dictionary<UnderworldTerrainBiome, Material> _biomeMaterials = new();
    private GameObject? _root;

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
        foreach (var material in _biomeMaterials.Values)
            if (material) UnityEngine.Object.Destroy(material);
        _biomeMaterials.Clear();
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
        if (sample.Heights.Length != expected || sample.Biomes.Length != expected || sample.Admitted.Length != expected)
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
            uv[index] = new Vector2(
                (float)((bounds.MinimumX + localX) / TerrainUvMetersPerTile),
                (float)((bounds.MinimumZ + localZ) / TerrainUvMetersPerTile));
        }

        // Presentation consumes the authoritative biome payload instead of reclassifying terrain.
        // A cell is assigned from its four corners by deterministic majority vote (ties prefer the
        // north-west corner), giving biome boundaries stable geometry regardless of chunk load order.
        var trianglesByBiome = new Dictionary<UnderworldTerrainBiome, List<int>>();
        for (var z = 0; z < edge - 1; z++)
        for (var x = 0; x < edge - 1; x++)
        {
            var a = z * edge + x;
            var b = a + 1;
            var c = a + edge;
            var d = c + 1;
            if (!sample.Admitted[a] || !sample.Admitted[b] || !sample.Admitted[c] || !sample.Admitted[d]) continue;
            var biome = SelectCellBiome(sample.Biomes[a], sample.Biomes[b], sample.Biomes[c], sample.Biomes[d]);
            if (!trianglesByBiome.TryGetValue(biome, out var triangles))
                trianglesByBiome.Add(biome, triangles = new List<int>());
            triangles.Add(a); triangles.Add(c); triangles.Add(b);
            triangles.Add(b); triangles.Add(c); triangles.Add(d);
        }

        var mesh = new Mesh { name = $"Magenheim_Underworld_Chunk_{sample.Key.X}_{sample.Key.Z}" };
        GameObject? node = null;
        try
        {
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.normals = BuildAuthoritativeNormals(sample, bounds.MinimumX, bounds.MinimumZ, grid.VertexSpacingMeters);
            mesh.subMeshCount = trianglesByBiome.Count;
            var materials = new Material[trianglesByBiome.Count];
            var submesh = 0;
            var triangleCount = 0;
            foreach (var pair in trianglesByBiome)
            {
                mesh.SetTriangles(pair.Value, submesh, false);
                materials[submesh] = GetBiomeMaterial(pair.Key);
                triangleCount += pair.Value.Count / 3;
                submesh++;
            }
            mesh.RecalculateBounds();

            node = new GameObject(mesh.name);
            node.transform.SetParent(_root!.transform, false);
            node.transform.localPosition = new Vector3((float)bounds.MinimumX, 0f, (float)bounds.MinimumZ);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.AddComponent<MeshRenderer>().sharedMaterials = materials;
            node.AddComponent<MeshCollider>().sharedMesh = mesh;
            _materialized.Add(sample.Key, node);
            _log.LogDebug($"Materialized native Underworld chunk {sample.Key.X},{sample.Key.Z} with {triangleCount} terrain triangles across {trianglesByBiome.Count} biome surfaces.");
        }
        catch
        {
            if (node) UnityEngine.Object.Destroy(node);
            UnityEngine.Object.Destroy(mesh);
            throw;
        }
    }

    private static UnderworldTerrainBiome SelectCellBiome(UnderworldTerrainBiome a, UnderworldTerrainBiome b, UnderworldTerrainBiome c, UnderworldTerrainBiome d)
    {
        var candidates = new[] { a, b, c, d };
        var selected = a;
        var bestCount = 0;
        for (var i = 0; i < candidates.Length; i++)
        {
            var count = 0;
            for (var j = 0; j < candidates.Length; j++) if (candidates[j] == candidates[i]) count++;
            if (count > bestCount) { selected = candidates[i]; bestCount = count; }
        }
        return selected;
    }

    private static Vector3[] BuildAuthoritativeNormals(UnderworldInstanceChunkSample sample, double minimumX, double minimumZ, double spacing)
    {
        var edge = sample.VerticesPerEdge;
        var normals = new Vector3[checked(edge * edge)];
        for (var z = 0; z < edge; z++)
        for (var x = 0; x < edge; x++)
        {
            var index = z * edge + x;
            if (!sample.Admitted[index]) { normals[index] = Vector3.up; continue; }
            var worldX = minimumX + x * spacing;
            var worldZ = minimumZ + z * spacing;
            var center = sample.Heights[index];
            var left = SampleHeightOrFallback(worldX - spacing, worldZ, center);
            var right = SampleHeightOrFallback(worldX + spacing, worldZ, center);
            var down = SampleHeightOrFallback(worldX, worldZ - spacing, center);
            var up = SampleHeightOrFallback(worldX, worldZ + spacing, center);
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

    private Material GetBiomeMaterial(UnderworldTerrainBiome biome)
    {
        if (_biomeMaterials.TryGetValue(biome, out var material) && material) return material;
        material = new Material(ModelAssets.ResolveSurfaceShader()) { name = $"Magenheim_Underworld_Terrain_{biome}" };
        var color = BiomeColor(biome);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        _biomeMaterials[biome] = material;
        return material;
    }

    private static Color BiomeColor(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => new Color32(48, 82, 69, 255),
        UnderworldTerrainBiome.BlackwaterDeep => new Color32(18, 32, 48, 255),
        UnderworldTerrainBiome.SulfurousWastes => new Color32(105, 78, 38, 255),
        UnderworldTerrainBiome.FrozenCaverns => new Color32(91, 124, 137, 255),
        UnderworldTerrainBiome.FractureZones => new Color32(82, 57, 67, 255),
        UnderworldTerrainBiome.GreatDecay => new Color32(65, 72, 45, 255),
        _ => new Color32(32, 32, 32, 255),
    };

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
