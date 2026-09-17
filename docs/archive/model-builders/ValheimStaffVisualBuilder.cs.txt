using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Shared low-poly construction boundary for authored Magenheim staff silhouettes.
/// It centralizes canonical mesh reuse, crooked segmented shafts, grips, cages, shards,
/// owned surfaces and concentrated focus lighting so every elemental family can meet the
/// Valheim-derived staff visual standard without duplicating private mesh builders.
/// </summary>
internal static class ValheimStaffVisualBuilder
{
    internal sealed class Context
    {
        internal Context(GameObject prefab, GameObject root, Renderer[] originalRenderers, Material sourceMaterial)
        {
            Prefab = prefab;
            Root = root;
            OriginalRenderers = originalRenderers;
            SourceMaterial = sourceMaterial;
        }

        internal GameObject Prefab { get; }
        internal GameObject Root { get; }
        internal Renderer[] OriginalRenderers { get; }
        internal Material SourceMaterial { get; }
    }

    private static readonly Mesh BoxMesh = RuntimeMeshPrimitives.Box("magenheim.staff-standard.box");
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static Context Begin(GameObject prefab, string visualIdentity)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(visualIdentity)) throw new ArgumentException("Visual identity is required.", nameof(visualIdentity));

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + visualIdentity + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return new Context(prefab, root, original, source);
    }

    internal static void Finish(Context context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        foreach (var renderer in context.OriginalRenderers)
            if (renderer) renderer.enabled = false;
        foreach (var lod in context.Prefab.GetComponentsInChildren<LODGroup>(true))
            if (lod) lod.enabled = false;
    }

    internal static Material Surface(
        Material source,
        string family,
        string semantic,
        Color color,
        float metallic,
        float gloss,
        float emission = 0f)
    {
        if (!source) throw new ArgumentNullException(nameof(source));
        var material = new Material(source) { name = $"magenheim.{family}.{semantic}" };
        GeneratedSurfaceTextures.Apply(material, semantic);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
        material.DisableKeyword("_NORMALMAP");
        if (material.HasProperty("_EmissionColor") && emission > 0f)
        {
            material.SetColor("_EmissionColor", color * emission);
            material.EnableKeyword("_EMISSION");
        }
        else material.DisableKeyword("_EMISSION");
        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        material.renderQueue = 2000;
        return material;
    }

    internal static void OrganicShaft(
        GameObject root,
        string prefix,
        Material wood,
        Material wrap,
        Material metal,
        float radius,
        float lateralBend,
        bool reinforced)
    {
        var points = new[]
        {
            new Vector3(-.014f * lateralBend, -.84f, .010f),
            new Vector3(-.032f * lateralBend, -.48f, .002f),
            new Vector3(.012f * lateralBend, -.13f, -.014f),
            new Vector3(-.016f * lateralBend, .21f, .014f),
            new Vector3(.020f * lateralBend, .53f, .006f),
        };

        for (var i = 0; i < points.Length - 1; i++)
        {
            var segmentRadius = radius * (1f - i * .07f);
            Segment(root, prefix + "-shaft-" + i, points[i], points[i + 1], segmentRadius, wood, i < 2 ? 9 : 8);
        }

        Band(root, prefix + "-grip-lower", new Vector3(-.023f * lateralBend, -.54f, .006f), radius * 1.43f, .055f, wrap, 9);
        Band(root, prefix + "-grip-mid", new Vector3(-.004f * lateralBend, -.39f, .001f), radius * 1.47f, .060f, wrap, 9);
        Band(root, prefix + "-grip-upper", new Vector3(.006f * lateralBend, -.24f, -.007f), radius * 1.41f, .052f, wrap, 9);
        Band(root, prefix + "-neck", new Vector3(.018f * lateralBend, .51f, .005f), radius * 1.55f, .055f, metal, 10);
        Band(root, prefix + "-pommel", new Vector3(-.014f * lateralBend, -.84f, .010f), radius * 1.58f, .075f, metal, 10);

        if (!reinforced) return;
        Band(root, prefix + "-reinforcement-a", new Vector3(-.012f * lateralBend, .04f, -.002f), radius * 1.40f, .040f, metal, 10);
        Band(root, prefix + "-reinforcement-b", new Vector3(.005f * lateralBend, .30f, .009f), radius * 1.44f, .040f, metal, 10);
    }

    internal static void Segment(GameObject root, string name, Vector3 start, Vector3 end, float radius, Material material, int sides = 8)
    {
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        var delta = end - start;
        var length = delta.magnitude;
        if (length <= .0001f) throw new InvalidOperationException($"Staff segment '{name}' has no length.");
        var rotation = Quaternion.FromToRotation(Vector3.up, delta / length);
        Add(root, name, Cylinder(sides), (start + end) * .5f, new Vector3(radius * 2f, length, radius * 2f), rotation, material);
    }

    internal static void Band(GameObject root, string name, Vector3 position, float radius, float height, Material material, int sides = 10) =>
        Add(root, name, Cylinder(sides), position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);

    internal static void Shard(GameObject root, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, int sides = 6) =>
        Add(root, name, Prism(sides), position, scale, rotation, material);

    internal static void Plate(GameObject root, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material) =>
        Add(root, name, BoxMesh, position, scale, rotation, material);

    internal static void FocusLight(GameObject root, string name, Vector3 position, Color color, float range, float intensity)
    {
        var host = new GameObject(name) { layer = root.layer };
        host.transform.SetParent(root.transform, false);
        host.transform.localPosition = position;
        var light = host.AddComponent<Light>();
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
    }

    private static Mesh Cylinder(int sides)
    {
        if (CylinderMeshes.TryGetValue(sides, out var mesh)) return mesh;
        mesh = RuntimeMeshPrimitives.Cylinder(sides, "magenheim.staff-standard.cylinder." + sides);
        CylinderMeshes.Add(sides, mesh);
        return mesh;
    }

    private static Mesh Prism(int sides)
    {
        if (PrismMeshes.TryGetValue(sides, out var mesh)) return mesh;
        mesh = RuntimeMeshPrimitives.Prism(
            sides,
            "magenheim.staff-standard.prism." + sides,
            lowerRadius: .36f,
            lowerY: -.45f,
            upperRadius: .50f,
            upperY: .20f,
            apexY: .68f,
            baseY: -.50f);
        PrismMeshes.Add(sides, mesh);
        return mesh;
    }

    private static void Add(GameObject root, string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        if (!root) throw new ArgumentNullException(nameof(root));
        if (!mesh) throw new ArgumentNullException(nameof(mesh));
        if (!material) throw new ArgumentNullException(nameof(material));
        var part = new GameObject(name) { layer = root.layer };
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = rotation;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
    }
}
