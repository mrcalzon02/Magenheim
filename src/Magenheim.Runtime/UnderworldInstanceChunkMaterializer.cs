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
    private const int TerrainTextureSize = 32;
    private readonly UnderworldRuntimeServices _services;
    private readonly ManualLogSource _log;
    private readonly Dictionary<UnderworldInstanceChunkKey, GameObject> _materialized = new();
    private readonly Dictionary<UnderworldTerrainBiome, Material> _biomeMaterials = new();
    private readonly Dictionary<UnderworldTerrainBiome, Texture2D> _biomeTextures = new();
    private readonly Dictionary<UnderworldTerrainBiome, Texture2D> _biomeNormalTextures = new();
    private GameObject? _root;
    // Coarse presentation covers the realm; collision/ecology still use the native 2m chunks.
    private readonly Dictionary<UnderworldInstanceChunkKey, GameObject> _horizon = new();
    private readonly Dictionary<UnderworldInstanceChunkKey, UnderworldInstanceChunkSample> _horizonSamples = new();
    private readonly HashSet<UnderworldInstanceChunkKey> _previousResidents = new();
    private Queue<UnderworldInstanceChunkKey>? _horizonPending;
    private UnderworldInstanceChunkGrid HorizonGrid => new(_services.TerrainDomain, 1024, 16);

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
            ClearHorizon();
            if (_root) UnityEngine.Object.Destroy(_root);
            _root = null;
            return;
        }

        EnsureRoot();
        foreach (var pair in resident)
            if (!_materialized.ContainsKey(pair.Key)) Materialize(pair.Value);
        ReconcileHorizon();
    }

    internal void Clear()
    {
        ClearHorizon();
        var keys = new List<UnderworldInstanceChunkKey>(_materialized.Keys);
        foreach (var key in keys) DestroyChunk(key);
        if (_root) UnityEngine.Object.Destroy(_root);
        _root = null;
        foreach (var material in _biomeMaterials.Values)
            if (material) UnityEngine.Object.Destroy(material);
        _biomeMaterials.Clear();
        foreach (var texture in _biomeTextures.Values)
            if (texture) UnityEngine.Object.Destroy(texture);
        _biomeTextures.Clear();
        foreach (var texture in _biomeNormalTextures.Values)
            if (texture) UnityEngine.Object.Destroy(texture);
        _biomeNormalTextures.Clear();
    }

    private void EnsureRoot()
    {
        if (_root) return;
        _root = new GameObject("Magenheim_Underworld_InstanceTerrain");
        _root.transform.position = new Vector3(0f, UnderworldInstanceLayer.EngineBaseY, 0f);
    }

    private void Materialize(UnderworldInstanceChunkSample sample, bool distant = false)
    {
        var edge = sample.VerticesPerEdge;
        var expected = checked(edge * edge);
        if (sample.Heights.Length != expected || sample.Biomes.Length != expected || sample.Admitted.Length != expected)
            throw new InvalidOperationException($"Underworld chunk {sample.Key} has inconsistent terrain payload dimensions.");

        var grid = distant ? HorizonGrid : _services.ChunkStreaming.Grid;
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

        var distantVertices = distant ? new List<Vector3>(vertices) : null;
        var distantUv = distant ? new List<Vector2>(uv) : null;
        var trianglesByBiome = new Dictionary<UnderworldTerrainBiome, List<int>>();
        for (var z = 0; z < edge - 1; z++)
        for (var x = 0; x < edge - 1; x++)
        {
            if (distant && IsResident(bounds.MinimumX + (x + .5d) * grid.VertexSpacingMeters,
                bounds.MinimumZ + (z + .5d) * grid.VertexSpacingMeters)) continue;
            var a = z * edge + x;
            var b = a + 1;
            var c = a + edge;
            var d = c + 1;
            if (!sample.Admitted[a] || !sample.Admitted[b] || !sample.Admitted[c] || !sample.Admitted[d]) continue;
            var biome = SelectCellBiome(sample.Biomes[a], sample.Biomes[b], sample.Biomes[c], sample.Biomes[d]);
            if (!trianglesByBiome.TryGetValue(biome, out var triangles))
                trianglesByBiome.Add(biome, triangles = new List<int>());
            if (distant && StitchResidentBoundary(bounds.MinimumX, bounds.MinimumZ, x, z,
                vertices, edge, distantVertices!, distantUv!, triangles)) continue;
            triangles.Add(a); triangles.Add(c); triangles.Add(b);
            triangles.Add(b); triangles.Add(c); triangles.Add(d);
        }

        var mesh = new Mesh { name = $"Magenheim_Underworld_Chunk_{sample.Key.X}_{sample.Key.Z}" };
        GameObject? node = null;
        try
        {
            mesh.vertices = distant ? distantVertices!.ToArray() : vertices;
            mesh.uv = distant ? distantUv!.ToArray() : uv;
            if (!distant)
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
            if (distant) mesh.RecalculateNormals();
            ApplyCliffUvs(mesh, bounds.MinimumX, bounds.MinimumZ);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            node = new GameObject(mesh.name);
            node.transform.SetParent(_root!.transform, false);
            node.transform.localPosition = new Vector3((float)bounds.MinimumX, 0f, (float)bounds.MinimumZ);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.AddComponent<MeshRenderer>().sharedMaterials = materials;
            if (!distant) node.AddComponent<MeshCollider>().sharedMesh = mesh;
            else node.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            (distant ? _horizon : _materialized).Add(sample.Key, node);
            _log.LogDebug($"Materialized native Underworld chunk {sample.Key.X},{sample.Key.Z} with {triangleCount} terrain triangles across {trianglesByBiome.Count} biome surfaces.");
        }
        catch
        {
            if (node) UnityEngine.Object.Destroy(node);
            UnityEngine.Object.Destroy(mesh);
            throw;
        }
    }

    // Horizontal UV projection stretches a few metres of texture up a kilometre-high
    // wall. Split only steep triangles and project those onto their dominant vertical plane.
    private static void ApplyCliffUvs(Mesh mesh, double minX, double minZ)
    {
        var vertices = new List<Vector3>(mesh.vertices);
        var normals = new List<Vector3>(mesh.normals);
        var uv = new List<Vector2>(mesh.uv);
        var submeshes = new List<int[]>();
        for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            var triangles = mesh.GetTriangles(submesh);
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]],
                    vertices[triangles[i + 2]] - vertices[triangles[i]]).normalized;
                if (Mathf.Abs(normal.y) >= .45f) continue;
                var projectZ = Mathf.Abs(normal.x) > Mathf.Abs(normal.z);
                for (var corner = 0; corner < 3; corner++)
                {
                    var original = triangles[i + corner];
                    var position = vertices[original];
                    triangles[i + corner] = vertices.Count;
                    vertices.Add(position);
                    normals.Add(normals[original]);
                    uv.Add(new Vector2((float)(projectZ ? minZ + position.z : minX + position.x) / TerrainUvMetersPerTile,
                        position.y / TerrainUvMetersPerTile));
                }
            }
            submeshes.Add(triangles);
        }
        if (vertices.Count > ushort.MaxValue) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = submeshes.Count;
        for (var i = 0; i < submeshes.Count; i++) mesh.SetTriangles(submeshes[i], i, false);
    }

    // A boundary cell is a fan with 2m vertices on its resident-facing edges. Its other
    // edges keep the coarse endpoints, so neither side has a T-junction or needs a skirt.
    private bool StitchResidentBoundary(double minX, double minZ, int x, int z,
        Vector3[] coarse, int edge, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
    {
        const int spacing = 16;
        var worldX = minX + x * spacing;
        var worldZ = minZ + z * spacing;
        var refined = new[] {
            IsResident(worldX - 1d, worldZ + 8d),
            IsResident(worldX + 8d, worldZ + 17d),
            IsResident(worldX + 17d, worldZ + 8d),
            IsResident(worldX + 8d, worldZ - 1d) };
        if (!refined[0] && !refined[1] && !refined[2] && !refined[3]) return false;
        var a = z * edge + x;
        var corners = new[] { coarse[a], coarse[a + edge], coarse[a + edge + 1], coarse[a + 1] };
        int Add(Vector3 position)
        {
            var index = vertices.Count;
            vertices.Add(position);
            uv.Add(new Vector2((float)(minX + position.x) / TerrainUvMetersPerTile,
                (float)(minZ + position.z) / TerrainUvMetersPerTile));
            return index;
        }
        var center = Add(new Vector3(x * spacing + 8f,
            (float)SampleHeightOrFallback(worldX + 8d, worldZ + 8d, corners[0].y), z * spacing + 8f));
        var ring = new List<int>();
        for (var side = 0; side < 4; side++)
        {
            var steps = refined[side] ? 8 : 1;
            for (var step = 0; step < steps; step++)
            {
                var position = Vector3.Lerp(corners[side], corners[(side + 1) % 4], step / (float)steps);
                if (step > 0)
                    position.y = (float)SampleHeightOrFallback(minX + position.x, minZ + position.z, position.y);
                ring.Add(Add(position));
            }
        }
        for (var i = 0; i < ring.Count; i++)
        { triangles.Add(center); triangles.Add(ring[i]); triangles.Add(ring[(i + 1) % ring.Count]); }
        return true;
    }

    private bool IsResident(double x, double z) =>
        _services.ChunkStreaming.LoadedChunks.ContainsKey(_services.ChunkStreaming.Grid.KeyAt(x, z));

    private void ReconcileHorizon()
    {
        var identity = _services.InstanceLifecycle.Identity;
        if (identity is null) return;
        var grid = HorizonGrid;
        var resident = _services.ChunkStreaming.LoadedChunks;
        var dirty = new HashSet<UnderworldInstanceChunkKey>();
        var nativeSize = _services.ChunkStreaming.Grid.ChunkSizeMeters;
        void DirtyBoundary(UnderworldInstanceChunkKey key)
        {
            // A resident edge can also change a fan in the neighbouring horizon tile.
            var minX = key.X * (double)nativeSize - 1d;
            var minZ = key.Z * (double)nativeSize - 1d;
            var maxX = minX + nativeSize + 2d;
            var maxZ = minZ + nativeSize + 2d;
            dirty.Add(grid.KeyAt(minX, minZ)); dirty.Add(grid.KeyAt(maxX, minZ));
            dirty.Add(grid.KeyAt(minX, maxZ)); dirty.Add(grid.KeyAt(maxX, maxZ));
        }
        foreach (var key in _previousResidents)
            if (!resident.ContainsKey(key)) DirtyBoundary(key);
        foreach (var key in resident.Keys)
            if (!_previousResidents.Contains(key)) DirtyBoundary(key);
        _previousResidents.Clear();
        foreach (var key in resident.Keys) _previousResidents.Add(key);
        foreach (var key in dirty)
        {
            if (!_horizonSamples.TryGetValue(key, out var sample)) continue;
            DestroyNode(_horizon, key);
            Materialize(sample, true);
        }
        foreach (var pair in _horizonSamples)
            if (!_horizon.ContainsKey(pair.Key)) Materialize(pair.Value, true);
        if (_horizonPending is null)
        {
            var keys = new List<UnderworldInstanceChunkKey>(grid.EnumerateSquare(new(0, 0),
                (int)Math.Ceiling(grid.Domain.RadiusMeters / grid.ChunkSizeMeters)));
            // Near terrain appears first; progressively fill the skyline without a realm-sized stall.
            var focusX = 0d;
            var focusZ = 0d;
            foreach (var key in resident.Keys) { focusX += key.X * (double)nativeSize; focusZ += key.Z * (double)nativeSize; }
            focusX /= resident.Count; focusZ /= resident.Count;
            double Distance(UnderworldInstanceChunkKey key) =>
                Math.Pow((key.X + .5d) * 1024d - focusX, 2d) + Math.Pow((key.Z + .5d) * 1024d - focusZ, 2d);
            keys.Sort((a, b) => Distance(a).CompareTo(Distance(b)));
            _horizonPending = new Queue<UnderworldInstanceChunkKey>(keys);
        }
        for (var budget = 0; budget < 4 && _horizonPending.Count > 0; budget++)
        {
            var key = _horizonPending.Peek();
            var water = ZoneSystem.instance is null ? 30d : ZoneSystem.instance.m_waterLevel;
            if (!_horizonSamples.TryGetValue(key, out var sample))
            {
                sample = UnderworldInstanceChunkSampler.Sample(grid, key, identity.DerivedSeed32, water);
                _horizonSamples.Add(key, sample);
            }
            if (!_horizon.ContainsKey(key)) Materialize(sample, true);
            _horizonPending.Dequeue();
        }
    }

    private void ClearHorizon()
    {
        foreach (var key in new List<UnderworldInstanceChunkKey>(_horizon.Keys)) DestroyNode(_horizon, key);
        _horizonSamples.Clear();
        _previousResidents.Clear();
        _horizonPending = null;
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
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        var texture = GetBiomeTexture(biome, color);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        var normal = GetBiomeNormalTexture(biome);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
        if (material.HasProperty("_NormalMap")) material.SetTexture("_NormalMap", normal);
        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", BiomeNormalStrength(biome));
        material.EnableKeyword("_NORMALMAP");
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", BiomeSmoothness(biome));
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", BiomeSmoothness(biome));
        _biomeMaterials[biome] = material;
        return material;
    }

    private Texture2D GetBiomeTexture(UnderworldTerrainBiome biome, Color baseColor)
    {
        if (_biomeTextures.TryGetValue(biome, out var existing) && existing) return existing;
        var texture = NewTerrainTexture($"Magenheim_Underworld_Terrain_{biome}_Detail", false);
        var pixels = new Color32[TerrainTextureSize * TerrainTextureSize];
        for (var y = 0; y < TerrainTextureSize; y++)
        for (var x = 0; x < TerrainTextureSize; x++)
        {
            var shade = TerrainDetailHeight(biome, x, y);
            pixels[y * TerrainTextureSize + x] = (Color32)new Color(
                Mathf.Clamp01(baseColor.r * shade),
                Mathf.Clamp01(baseColor.g * shade),
                Mathf.Clamp01(baseColor.b * shade), 1f);
        }
        texture.SetPixels32(pixels);
        texture.Apply(true, false);
        _biomeTextures[biome] = texture;
        return texture;
    }

    private Texture2D GetBiomeNormalTexture(UnderworldTerrainBiome biome)
    {
        if (_biomeNormalTextures.TryGetValue(biome, out var existing) && existing) return existing;
        var texture = NewTerrainTexture($"Magenheim_Underworld_Terrain_{biome}_Normal", true);
        var pixels = new Color32[TerrainTextureSize * TerrainTextureSize];
        var slope = BiomeNormalSlope(biome);
        for (var y = 0; y < TerrainTextureSize; y++)
        for (var x = 0; x < TerrainTextureSize; x++)
        {
            var left = TerrainDetailHeight(biome, WrapTexel(x - 1), y);
            var right = TerrainDetailHeight(biome, WrapTexel(x + 1), y);
            var down = TerrainDetailHeight(biome, x, WrapTexel(y - 1));
            var up = TerrainDetailHeight(biome, x, WrapTexel(y + 1));
            var normal = new Vector3((left - right) * slope, (down - up) * slope, 1f).normalized;
            pixels[y * TerrainTextureSize + x] = new Color32(
                (byte)Mathf.RoundToInt((normal.x * .5f + .5f) * 255f),
                (byte)Mathf.RoundToInt((normal.y * .5f + .5f) * 255f),
                (byte)Mathf.RoundToInt((normal.z * .5f + .5f) * 255f), 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply(true, false);
        _biomeNormalTextures[biome] = texture;
        return texture;
    }

    private static Texture2D NewTerrainTexture(string name, bool linear)
    {
        return new Texture2D(TerrainTextureSize, TerrainTextureSize, TextureFormat.RGBA32, true, linear)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 4
        };
    }

    private static int WrapTexel(int value) => (value + TerrainTextureSize) % TerrainTextureSize;

    private static float TerrainDetailHeight(UnderworldTerrainBiome biome, int x, int y)
    {
        var coarse = Hash01(x / 4, y / 4, (int)biome * 97);
        var fine = Hash01(x, y, (int)biome * 193);
        var structure = BiomeStructure(biome, x, y);
        return Mathf.Clamp(0.72f + coarse * 0.22f + fine * 0.10f + structure, 0.48f, 1.18f);
    }

    private static float Hash01(int x, int y, int seed)
    {
        unchecked
        {
            var n = x * 374761393 + y * 668265263 + seed * 1442695041;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7fffffff) / 2147483647f;
        }
    }

    private static float BiomeStructure(UnderworldTerrainBiome biome, int x, int y) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => ((x + y * 2) % 11 == 0) ? 0.14f : 0f,
        UnderworldTerrainBiome.BlackwaterDeep => (y % 8 < 2) ? -0.10f : 0.02f,
        UnderworldTerrainBiome.SulfurousWastes => ((x * 3 + y) % 13 < 2) ? 0.16f : -0.02f,
        UnderworldTerrainBiome.FrozenCaverns => (Math.Abs(x - y) % 9 == 0) ? 0.18f : 0f,
        UnderworldTerrainBiome.FractureZones => (Math.Abs(x * 2 - y) % 13 < 2) ? -0.18f : 0.02f,
        UnderworldTerrainBiome.GreatDecay => ((x + y) % 7 == 0) ? -0.12f : 0.01f,
        _ => 0f,
    };

    private static float BiomeSmoothness(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.BlackwaterDeep => 0.52f,
        UnderworldTerrainBiome.FrozenCaverns => 0.44f,
        UnderworldTerrainBiome.FungalForest => 0.18f,
        UnderworldTerrainBiome.GreatDecay => 0.12f,
        _ => 0.22f,
    };

    private static float BiomeNormalStrength(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.BlackwaterDeep => 0.45f,
        UnderworldTerrainBiome.FrozenCaverns => 0.65f,
        UnderworldTerrainBiome.SulfurousWastes => 1.05f,
        UnderworldTerrainBiome.FractureZones => 1.15f,
        UnderworldTerrainBiome.GreatDecay => 0.9f,
        _ => 0.8f,
    };

    private static float BiomeNormalSlope(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.BlackwaterDeep => 1.8f,
        UnderworldTerrainBiome.FrozenCaverns => 2.5f,
        UnderworldTerrainBiome.SulfurousWastes => 4.2f,
        UnderworldTerrainBiome.FractureZones => 4.8f,
        UnderworldTerrainBiome.GreatDecay => 3.6f,
        _ => 3.2f,
    };

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
        => DestroyNode(_materialized, key);

    private static void DestroyNode(Dictionary<UnderworldInstanceChunkKey, GameObject> nodes, UnderworldInstanceChunkKey key)
    {
        if (!nodes.TryGetValue(key, out var node)) return;
        nodes.Remove(key);
        if (!node) return;
        var filter = node.GetComponent<MeshFilter>();
        if (filter && filter.sharedMesh) UnityEngine.Object.Destroy(filter.sharedMesh);
        UnityEngine.Object.Destroy(node);
    }
}
