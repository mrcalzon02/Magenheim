using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Shared runtime surface authority for Magenheim-owned procedural geometry.
/// Generated models receive repeatable 256px material surfaces instead of flat placeholder color.
/// Reconciliation is strictly owned-only and never replaces authored/file-backed textures.
/// </summary>
internal static class GeneratedSurfaceTextures
{
    private const int TextureSize = 256;
    private const float ProjectionScale = 1.75f;
    private static readonly Dictionary<SurfaceKind, Texture2D> Cache = new();

    static GeneratedSurfaceTextures() => PrefabManager.OnPrefabsRegistered += RepairOwnedVisuals;

    internal static void Apply(Material material, string semantic)
    {
        if (material is null) throw new ArgumentNullException(nameof(material));
        material.mainTexture = ForSemantic(semantic);
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
    }

    internal static Texture2D ForSemantic(string semantic)
    {
        var kind = Classify(semantic);
        if (Cache.TryGetValue(kind, out var existing) && existing) return existing;
        var texture = Build(kind);
        Cache[kind] = texture;
        return texture;
    }

    private static SurfaceKind Classify(string semantic)
    {
        var key = (semantic ?? string.Empty).ToLowerInvariant();
        if (ContainsAny(key, "water", "liquid", "solution")) return SurfaceKind.Liquid;
        if (ContainsAny(key, "hide", "leather", "pelt")) return SurfaceKind.Leather;
        if (ContainsAny(key, "stone", "marble", "rock", "earth", "strata", "slate", "basalt")) return SurfaceKind.Stone;
        if (ContainsAny(key, "wood", "timber", "root", "shaft", "bark")) return SurfaceKind.Timber;
        if (ContainsAny(key, "iron", "bronze", "silver", "gold", "metal", "band", "collar", "brace", "rail", "rim")) return SurfaceKind.Metal;
        if (ContainsAny(key, "cloth", "banner", "fabric")) return SurfaceKind.Cloth;
        if (ContainsAny(key, "bone", "ivory", "antler")) return SurfaceKind.Bone;
        if (ContainsAny(key, "crystal", "frost", "rime", "ice", "spirit", "radiance", "venom", "seidr", "fate", "eitr", "gem", "shard", "growth", "focus", "core", "light")) return SurfaceKind.Crystal;
        return SurfaceKind.Generic;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        foreach (var needle in needles)
            if (value.IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    private static Texture2D Build(SurfaceKind kind)
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: true)
        {
            name = "magenheim.surface." + kind.ToString().ToLowerInvariant(),
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4,
            hideFlags = HideFlags.HideAndDontSave,
        };
        var pixels = new Color[TextureSize * TextureSize];
        for (var y = 0; y < TextureSize; y++)
        for (var x = 0; x < TextureSize; x++)
        {
            var value = kind switch
            {
                SurfaceKind.Crystal => Crystal(x, y), SurfaceKind.Stone => Stone(x, y),
                SurfaceKind.Timber => Timber(x, y), SurfaceKind.Metal => Metal(x, y),
                SurfaceKind.Cloth => Cloth(x, y), SurfaceKind.Bone => Bone(x, y),
                SurfaceKind.Liquid => Liquid(x, y), SurfaceKind.Leather => Leather(x, y),
                _ => Generic(x, y),
            };
            pixels[y * TextureSize + x] = new Color(value, value, value, 1f);
        }
        texture.SetPixels(pixels);
        texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        return texture;
    }

    private static float Crystal(int x, int y)
    {
        const int cell = 32; var cx = x / cell; var cy = y / cell; var lx = x % cell; var ly = y % cell;
        var seed = Hash(cx, cy, 13); var facet = .54f + .27f * seed;
        var edge = lx <= 1 || ly <= 1 || lx >= cell - 2 || ly >= cell - 2 || Math.Abs(lx - ly) <= 2 || Math.Abs((cell - 1 - lx) - ly) <= 2;
        var band = .035f * Mathf.Sin((x + y) * .18f + seed * 5.7f); var fine = (Hash(x / 2, y / 2, 19) - .5f) * .055f;
        var glint = ((x * 3 + y * 5 + cx * 11 + cy * 17) & 63) == 0 ? .16f : 0f;
        return Mathf.Clamp01(facet + band + fine + (edge ? .15f : 0f) + glint);
    }

    private static float Stone(int x, int y)
    {
        var coarse = Hash(x / 18, y / 18, 29); var medium = Hash(x / 6, y / 6, 31); var fine = Hash(x, y, 47);
        var strata = .055f * Mathf.Sin((y + 10f * Mathf.Sin(x * .043f)) * .13f);
        var fracture = Mathf.Abs(Mathf.Sin(x * .061f + y * .029f + coarse * 5f)); var vein = fracture > .965f ? -.16f : fracture > .93f ? -.08f : 0f;
        return Mathf.Clamp01(.54f + coarse * .16f + medium * .10f + fine * .035f + strata + vein);
    }

    private static float Timber(int x, int y)
    {
        var wobble = 11f * Mathf.Sin(y * .031f) + 4f * Mathf.Sin(y * .087f); var grain = Mathf.Abs(Mathf.Sin((x + wobble) * .087f));
        var secondary = Mathf.Abs(Mathf.Sin((x * .41f + wobble * .65f) * .11f)); var kx = RepeatDistance(x, 158f, TextureSize); var ky = RepeatDistance(y, 93f, TextureSize);
        var kd = Mathf.Sqrt(kx * kx + ky * ky); var knot = Mathf.Abs(Mathf.Sin(kd * .13f)); var weight = Mathf.Clamp01(1f - kd / 54f);
        return Mathf.Clamp01(.48f + grain * .23f + secondary * .08f + knot * weight * .19f + (Hash(x / 3, y / 3, 59) - .5f) * .045f);
    }

    private static float Metal(int x, int y) => Mathf.Clamp01(.54f + Hash(x / 10, y / 10, 71) * .16f + Hash(x / 2, y / 2, 73) * .045f + ((x + y * 3) % 71 == 0 ? .16f : 0f) + ((x * 5 + y) % 97 == 0 ? .12f : 0f) + .035f * Mathf.Sin(y * .195f) + .045f * Mathf.Abs(Mathf.Sin((x - y) * .047f)));
    private static float Cloth(int x, int y) => Mathf.Clamp01(.51f + (x % 8 <= 1 ? .14f : 0f) + (y % 8 <= 1 ? .11f : 0f) + (x % 8 <= 1 && y % 8 <= 1 ? .07f : 0f) + Hash(x / 24, y / 24, 83) * .055f + (Hash(x, y, 89) - .5f) * .035f);
    private static float Bone(int x, int y) => Mathf.Clamp01(.72f + .075f * Mathf.Sin(y * .12f + Mathf.Sin(x * .035f) * 2.5f) + .035f * Mathf.Sin(y * .036f + x * .011f) + (Hash(x / 2, y / 2, 97) > .94f ? -.14f : 0f) + (Hash(x, y, 101) - .5f) * .035f);
    private static float Liquid(int x, int y)
    {
        var rx = RepeatDistance(x, 124f, TextureSize); var ry = RepeatDistance(y, 116f, TextureSize);
        return Mathf.Clamp01(.64f + .075f * Mathf.Sin(x * .078f + y * .020f) + .055f * Mathf.Sin(x * .031f - y * .105f) + .045f * Mathf.Sin(Mathf.Sqrt(rx * rx + ry * ry) * .14f) + (Hash(x / 3, y / 3, 103) > .975f ? .09f : 0f));
    }
    private static float Leather(int x, int y) => Mathf.Clamp01(.49f + Hash(x / 5, y / 5, 107) * .15f + (Hash(x, y, 109) - .5f) * .04f + .065f * Mathf.Sin(y * .067f + Mathf.Sin(x * .028f) * 2.4f) + (Hash(x / 2, y / 2, 113) > .955f ? -.12f : 0f));
    private static float Generic(int x, int y) => Mathf.Clamp01(.57f + Hash(x / 8, y / 8, 127) * .14f + Hash(x, y, 131) * .045f + .04f * Mathf.Sin((x + y) * .105f));

    private static float RepeatDistance(float value, float center, float period) { var delta = Mathf.Abs(value - center); return Mathf.Min(delta, period - delta); }
    private static float Hash(int x, int y, int salt) { unchecked { var value = x * 374761393 + y * 668265263 + salt * 1442695041; value = (value ^ (value >> 13)) * 1274126177; value ^= value >> 16; return (value & 0x7fffffff) / (float)int.MaxValue; } }

    private static void RepairOwnedVisuals()
    {
        var repairedMaterials = 0;
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (!material || !IsOwnedName(material.name) || !NeedsGeneratedSurface(material.mainTexture)) continue;
            Apply(material, material.name); repairedMaterials++;
        }
        var repairedMeshes = 0;
        foreach (var mesh in Resources.FindObjectsOfTypeAll<Mesh>())
        {
            if (!mesh || !mesh.isReadable || !IsOwnedName(mesh.name) || HasUsableUv(mesh) || mesh.vertexCount == 0 || mesh.subMeshCount == 0 || mesh.boneWeights.Length != 0) continue;
            try { ProjectTriangleUvs(mesh); repairedMeshes++; }
            catch (Exception exception) { Debug.LogWarning($"Magenheim could not generate UV0 for owned mesh '{mesh.name}': {exception.Message}"); }
        }
        if (repairedMaterials > 0 || repairedMeshes > 0) Debug.Log($"Magenheim visual-quality reconciliation upgraded {repairedMaterials} placeholder material(s) and {repairedMeshes} UV-less owned mesh(es).");
    }

    private static bool IsOwnedName(string value) => !string.IsNullOrEmpty(value) && value.StartsWith("magenheim", StringComparison.OrdinalIgnoreCase);
    private static bool NeedsGeneratedSurface(Texture? texture)
    {
        if (!texture || ReferenceEquals(texture, Texture2D.whiteTexture)) return true;
        // A generated Magenheim surface is already semantically classified. Re-running it through
        // the material's namespaced object name can misclassify components (for example a
        // crystal-bed iron frame as Crystal). Preserve the existing generated surface.
        if (texture.name.StartsWith("magenheim.surface.", StringComparison.OrdinalIgnoreCase)) return false;
        return texture.width <= 4 && texture.height <= 4;
    }

    private static bool HasUsableUv(Mesh mesh)
    {
        var uv = mesh.uv; if (uv is null || uv.Length != mesh.vertexCount || uv.Length == 0) return false;
        var min = uv[0]; var max = uv[0];
        for (var i = 1; i < uv.Length; i++) { min = Vector2.Min(min, uv[i]); max = Vector2.Max(max, uv[i]); }
        return (max - min).sqrMagnitude > .000001f;
    }

    private static void ProjectTriangleUvs(Mesh mesh)
    {
        var sourceVertices = mesh.vertices; var sourceNormals = mesh.normals; var sourceTangents = mesh.tangents; var sourceColors = mesh.colors;
        var hasNormals = sourceNormals is not null && sourceNormals.Length == sourceVertices.Length;
        var hasTangents = sourceTangents is not null && sourceTangents.Length == sourceVertices.Length;
        var hasColors = sourceColors is not null && sourceColors.Length == sourceVertices.Length;
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var tangents = hasTangents ? new List<Vector4>() : null; var colors = hasColors ? new List<Color>() : null; var uv = new List<Vector2>(); var submeshes = new List<int[]>(mesh.subMeshCount);
        for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            var sourceTriangles = mesh.GetTriangles(submesh); var projectedTriangles = new int[sourceTriangles.Length];
            for (var triangle = 0; triangle < sourceTriangles.Length; triangle += 3)
            {
                if (triangle + 2 >= sourceTriangles.Length) throw new InvalidOperationException("Triangle index buffer is not divisible by three.");
                var i0 = sourceTriangles[triangle]; var i1 = sourceTriangles[triangle + 1]; var i2 = sourceTriangles[triangle + 2];
                ValidateVertexIndex(i0, sourceVertices.Length); ValidateVertexIndex(i1, sourceVertices.Length); ValidateVertexIndex(i2, sourceVertices.Length);
                var a = sourceVertices[i0]; var b = sourceVertices[i1]; var c = sourceVertices[i2]; var faceNormal = Vector3.Cross(b - a, c - a).normalized; if (faceNormal.sqrMagnitude <= .000001f) faceNormal = Vector3.up;
                projectedTriangles[triangle] = AppendProjectedVertex(i0, a, faceNormal, sourceNormals, sourceTangents, sourceColors, hasNormals, hasTangents, hasColors, vertices, normals, tangents, colors, uv);
                projectedTriangles[triangle + 1] = AppendProjectedVertex(i1, b, faceNormal, sourceNormals, sourceTangents, sourceColors, hasNormals, hasTangents, hasColors, vertices, normals, tangents, colors, uv);
                projectedTriangles[triangle + 2] = AppendProjectedVertex(i2, c, faceNormal, sourceNormals, sourceTangents, sourceColors, hasNormals, hasTangents, hasColors, vertices, normals, tangents, colors, uv);
            }
            submeshes.Add(projectedTriangles);
        }
        mesh.Clear(); mesh.vertices = vertices.ToArray(); mesh.normals = normals.ToArray(); mesh.uv = uv.ToArray();
        if (tangents is not null) mesh.tangents = tangents.ToArray(); if (colors is not null) mesh.colors = colors.ToArray();
        mesh.subMeshCount = submeshes.Count; for (var i = 0; i < submeshes.Count; i++) mesh.SetTriangles(submeshes[i], i, calculateBounds: false); mesh.RecalculateBounds();
    }

    private static int AppendProjectedVertex(int sourceIndex, Vector3 vertex, Vector3 faceNormal, Vector3[]? sourceNormals, Vector4[]? sourceTangents, Color[]? sourceColors, bool hasNormals, bool hasTangents, bool hasColors, List<Vector3> vertices, List<Vector3> normals, List<Vector4>? tangents, List<Color>? colors, List<Vector2> uv)
    {
        var destinationIndex = vertices.Count; vertices.Add(vertex); normals.Add(hasNormals ? sourceNormals![sourceIndex] : faceNormal);
        if (hasTangents && tangents is not null) tangents.Add(sourceTangents![sourceIndex]); if (hasColors && colors is not null) colors.Add(sourceColors![sourceIndex]);
        uv.Add(Project(vertex, faceNormal) * ProjectionScale); return destinationIndex;
    }

    private static Vector2 Project(Vector3 point, Vector3 normal)
    {
        var absolute = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        if (absolute.x >= absolute.y && absolute.x >= absolute.z) return normal.x >= 0f ? new Vector2(-point.z, point.y) : new Vector2(point.z, point.y);
        if (absolute.y >= absolute.x && absolute.y >= absolute.z) return normal.y >= 0f ? new Vector2(point.x, point.z) : new Vector2(point.x, -point.z);
        return normal.z >= 0f ? new Vector2(point.x, point.y) : new Vector2(-point.x, point.y);
    }

    private static void ValidateVertexIndex(int index, int vertexCount) { if (index < 0 || index >= vertexCount) throw new InvalidOperationException($"Triangle references vertex {index} outside the mesh's {vertexCount} vertices."); }

    private enum SurfaceKind { Crystal, Stone, Timber, Metal, Cloth, Bone, Liquid, Leather, Generic }
}
