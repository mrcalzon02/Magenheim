using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Loads original checked-in assets and derives elemental color variants at runtime.</summary>
internal static class EarthAssets
{
    private static readonly Dictionary<string, Mesh> Meshes = new();
    private static readonly Dictionary<string, Sprite> Icons = new();
    private static readonly Dictionary<string, Material> Materials = new();
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static string DirectoryPath => Path.Combine(
        Path.GetDirectoryName(typeof(EarthAssets).Assembly.Location)!, "assets", "earth");

    internal static Sprite Icon(string name)
    {
        if (Icons.TryGetValue(name, out var icon)) return icon;
        var texture = Texture(name + ".icon.png");
        icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 128f);
        icon.name = "magenheim." + name + ".icon";
        Icons.Add(name, icon);
        return icon;
    }

    internal static Sprite Icon(string name, string variant, Color tint)
    {
        if (string.IsNullOrWhiteSpace(variant)) throw new ArgumentException("Variant is required.", nameof(variant));
        var key = name + "|" + variant;
        if (Icons.TryGetValue(key, out var icon)) return icon;

        var texture = TintedTexture(name + ".icon.png", variant, tint);
        icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 128f);
        icon.name = "magenheim." + name + "." + variant + ".icon";
        Icons.Add(key, icon);
        return icon;
    }

    internal static GameObject ReplaceVisual(
        GameObject prefab,
        string asset,
        float scale = 1f,
        bool worldObject = false,
        bool buildingPiece = false,
        string? variant = null,
        Color? tint = null)
    {
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
            throw new ArgumentOutOfRangeException(nameof(scale), "Visual scale must be finite and greater than zero.");

        var wear = prefab.GetComponent<WearNTear>();
        var variants = buildingPiece && wear ? new[] { wear.m_new, wear.m_worn, wear.m_broken }.Where(obj => obj).Distinct().ToArray() : Array.Empty<GameObject>();
        // Keep workbench radius markers, connection effects, and trigger visuals intact.
        var originalRenderers = variants.Length > 0
            ? variants.SelectMany(obj => obj.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray()
            : prefab.GetComponentsInChildren<Renderer>(true);
        var sourceMaterial = originalRenderers.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material);
        if (!sourceMaterial) throw new InvalidOperationException($"No material source on {prefab.name}.");

        var materialKey = string.IsNullOrWhiteSpace(variant) ? asset : asset + "|" + variant;
        if (!Materials.TryGetValue(materialKey, out var material))
        {
            material = new Material(sourceMaterial) { name = "magenheim." + materialKey + ".material" };
            material.mainTexture = tint.HasValue
                ? TintedTexture(asset + ".png", variant ?? "tint", tint.Value)
                : Texture(asset + ".png");
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .15f);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_EMISSION");
            ConfigureOpaqueMaterial(material);
            Materials.Add(materialKey, material);
        }

        var mesh = LoadMesh(asset);
        var worldOffset = worldObject
            ? new Vector3(0f, -mesh.bounds.min.y * scale, 0f)
            : Vector3.zero;
        var visual = new GameObject("magenheim." + materialKey + ".visual");
        // Preserve the inventory attachment hierarchy used by item stands and dropped items.
        visual.transform.SetParent(worldObject || buildingPiece ? prefab.transform : prefab.transform.Find("attach") ?? prefab.transform, false);
        visual.layer = prefab.layer;
        visual.transform.localScale = Vector3.one * scale;
        visual.transform.localPosition = worldOffset;
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.AddComponent<MeshRenderer>().sharedMaterial = material;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        foreach (var renderer in originalRenderers) renderer.enabled = false;

        if (worldObject)
        {
            // Match the collision envelope to the actual custom mesh and requested scale instead
            // of keeping Rock_4's collision or a hard-coded collider from an older visual size.
            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
                if (!collider.isTrigger) collider.enabled = false;
            var noduleCollider = prefab.AddComponent<SphereCollider>();
            noduleCollider.center = worldOffset + (mesh.bounds.center * scale);
            var extents = mesh.bounds.extents * scale;
            noduleCollider.radius = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)) * 1.05f;
        }
        return visual;
    }

    private static void ConfigureOpaqueMaterial(Material material)
    {
        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 0f);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", 1f);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", 0f);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
        if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 2000;
    }

    private static Texture2D Texture(string filename)
    {
        if (Textures.TryGetValue(filename, out var existing)) return existing;

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "magenheim." + filename };
        var loadImage = typeof(ImageConversion).GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) })
            ?? throw new MissingMethodException("Unity ImageConversion.LoadImage(Texture2D, byte[], bool)");
        if (!(bool)loadImage.Invoke(null, new object[] { texture, File.ReadAllBytes(Path.Combine(DirectoryPath, filename)), false }))
            throw new InvalidDataException($"Cannot decode Earth asset {filename}.");
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Textures.Add(filename, texture);
        return texture;
    }

    private static Texture2D TintedTexture(string filename, string variant, Color tint)
    {
        var key = filename + "|" + variant;
        if (Textures.TryGetValue(key, out var existing)) return existing;

        var source = Texture(filename);
        var pixels = source.GetPixels();
        for (var index = 0; index < pixels.Length; index++)
        {
            var sourcePixel = pixels[index];
            var luminance = sourcePixel.grayscale;
            var value = Mathf.Clamp01(0.24f + luminance * 1.08f);
            pixels[index] = new Color(
                Mathf.Clamp01(tint.r * value),
                Mathf.Clamp01(tint.g * value),
                Mathf.Clamp01(tint.b * value),
                sourcePixel.a);
        }

        var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        {
            name = "magenheim." + filename + "." + variant
        };
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Textures.Add(key, texture);
        return texture;
    }

    private static Mesh LoadMesh(string asset)
    {
        if (Meshes.TryGetValue(asset, out var mesh)) return mesh;
        var data = JsonConvert.DeserializeObject<MeshDocument>(File.ReadAllText(Path.Combine(DirectoryPath, asset + ".mesh.json")))
            ?? throw new InvalidDataException($"Empty mesh {asset}.");
        if (data.Vertices.Length == 0 || data.Vertices.Length != data.Uv.Length || data.Triangles.Length % 3 != 0
            || data.Vertices.Any(v => v.Length != 3 || v.Any(x => float.IsNaN(x) || float.IsInfinity(x)))
            || data.Uv.Any(v => v.Length != 2 || v.Any(x => float.IsNaN(x) || float.IsInfinity(x)))
            || data.Triangles.Any(i => i < 0 || i >= data.Vertices.Length))
            throw new InvalidDataException($"Invalid mesh {asset}.");
        mesh = new Mesh
        {
            name = "magenheim." + asset + ".mesh",
            vertices = data.Vertices.Select(v => new Vector3(v[0], v[1], v[2])).ToArray(),
            uv = data.Uv.Select(v => new Vector2(v[0], v[1])).ToArray(),
            triangles = data.Triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Meshes.Add(asset, mesh);
        return mesh;
    }

    private sealed class MeshDocument
    {
        public float[][] Vertices { get; set; } = Array.Empty<float[]>();
        public float[][] Uv { get; set; } = Array.Empty<float[]>();
        public int[] Triangles { get; set; } = Array.Empty<int>();
    }
}
