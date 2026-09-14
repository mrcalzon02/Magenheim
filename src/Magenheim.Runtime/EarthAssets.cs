using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Loads original checked-in assets. No Unity editor or asset bundle is required.</summary>
internal static class EarthAssets
{
    private static readonly Dictionary<string, Mesh> Meshes = new();
    private static readonly Dictionary<string, Sprite> Icons = new();
    private static readonly Dictionary<string, Material> Materials = new();
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

    internal static GameObject ReplaceVisual(GameObject prefab, string asset, float scale = 1f, bool worldObject = false, bool buildingPiece = false)
    {
        var wear = prefab.GetComponent<WearNTear>();
        var variants = buildingPiece && wear ? new[] { wear.m_new, wear.m_worn, wear.m_broken }.Where(obj => obj).Distinct().ToArray() : Array.Empty<GameObject>();
        // Keep workbench radius markers, connection effects, and trigger visuals intact.
        var originalRenderers = variants.Length > 0
            ? variants.SelectMany(obj => obj.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray()
            : prefab.GetComponentsInChildren<Renderer>(true);
        var sourceMaterial = originalRenderers.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material);
        if (!sourceMaterial) throw new InvalidOperationException($"No material source on {prefab.name}.");

        if (!Materials.TryGetValue(asset, out var material))
        {
            material = new Material(sourceMaterial) { name = "magenheim." + asset + ".material" };
            material.mainTexture = Texture(asset + ".png");
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
            Materials.Add(asset, material);
        }

        var mesh = LoadMesh(asset);
        var visual = new GameObject("magenheim." + asset + ".visual");
        // Preserve the inventory attachment hierarchy used by item stands and dropped items.
        visual.transform.SetParent(worldObject || buildingPiece ? prefab.transform : prefab.transform.Find("attach") ?? prefab.transform, false);
        visual.layer = prefab.layer;
        visual.transform.localScale = Vector3.one * scale;
        visual.transform.localPosition = worldObject ? new Vector3(0, .53f, 0) : Vector3.zero;
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.AddComponent<MeshRenderer>().sharedMaterial = material;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        foreach (var renderer in originalRenderers) renderer.enabled = false;

        if (worldObject)
        {
            // The new nodule is much smaller than Rock_4. Give only this clone a matching collider.
            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
                if (!collider.isTrigger) collider.enabled = false;
            var noduleCollider = prefab.AddComponent<SphereCollider>();
            noduleCollider.center = new Vector3(0, .53f, 0);
            noduleCollider.radius = .65f;
        }
        return visual;
    }

    private static void ConfigureOpaqueMaterial(Material material)
    {
        // Clone sources such as Rock_4 can carry shader/render-state settings that are
        // inappropriate for Magenheim's fully opaque RGB atlases. Normalize the cloned
        // material instead of inheriting transparency/blending from the source prefab.
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
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "magenheim." + filename };
        // Select the byte[] API explicitly: Unity 6 also exposes a Span overload whose
        // reference type is absent from net462. Do not ship a second runtime corlib.
        var loadImage = typeof(ImageConversion).GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) })
            ?? throw new MissingMethodException("Unity ImageConversion.LoadImage(Texture2D, byte[], bool)");
        if (!(bool)loadImage.Invoke(null, new object[] { texture, File.ReadAllBytes(Path.Combine(DirectoryPath, filename)), false }))
            throw new InvalidDataException($"Cannot decode Earth asset {filename}.");
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
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
