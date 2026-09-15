using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Procedural creature presentation owned by Magenheim rather than foreign model assets.</summary>
internal static class DeepFractureCreatureVisuals
{
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject ApplyAnnoyanceWisp(GameObject prefab, ElementalAlignment alignment)
    {
        var context = Begin(prefab, "annoyance-wisp", alignment);
        Prism(context.Root, "motion-core", Vector3.zero, .22f, .58f, 7, context.Core);
        Prism(context.Root, "crystal-shell", Vector3.zero, .38f, .82f, 7, context.Shell);
        for (var i = 0; i < 5; i++)
        {
            var angle = (Mathf.PI * 2f * i / 5f) + .28f;
            Prism(context.Root, "orbit-shard-" + i,
                new Vector3(Mathf.Cos(angle) * .48f, -.03f + ((i & 1) == 0 ? .08f : -.05f), Mathf.Sin(angle) * .48f),
                .075f, .30f, 5, context.Shard, new Vector3(22f, -angle * Mathf.Rad2Deg, 34f));
        }
        AddLight(context.Root, context.Tint, 3.2f, .85f);
        HideOriginal(prefab, context.Root, context.Original);
        return context.Root;
    }

    internal static GameObject ApplyGeodeCrawler(GameObject prefab, ElementalAlignment alignment)
    {
        var context = Begin(prefab, "geode-crawler", alignment);
        context.Root.transform.localPosition = new Vector3(0f, .08f, 0f);
        Prism(context.Root, "stone-abdomen", new Vector3(0f, .12f, -.18f), .43f, .72f, 8, context.Shell, new Vector3(90f, 0f, 0f));
        Prism(context.Root, "stone-thorax", new Vector3(0f, .12f, .34f), .32f, .48f, 7, context.Shell, new Vector3(90f, 0f, 0f));
        Prism(context.Root, "carapace-core", new Vector3(0f, .39f, -.12f), .23f, .38f, 6, context.Core, new Vector3(8f, 0f, 0f));
        for (var i = 0; i < 3; i++)
        {
            var z = -.27f + i * .30f;
            Prism(context.Root, "left-leg-" + i, new Vector3(-.43f, -.04f, z), .075f, .58f, 5, context.Shard, new Vector3(0f, 0f, 62f));
            Prism(context.Root, "right-leg-" + i, new Vector3(.43f, -.04f, z), .075f, .58f, 5, context.Shard, new Vector3(0f, 0f, -62f));
        }
        Prism(context.Root, "left-mandible", new Vector3(-.17f, .06f, .63f), .065f, .30f, 5, context.Core, new Vector3(70f, 0f, 18f));
        Prism(context.Root, "right-mandible", new Vector3(.17f, .06f, .63f), .065f, .30f, 5, context.Core, new Vector3(70f, 0f, -18f));
        AddLight(context.Root, context.Tint, 2.4f, .55f);
        HideOriginal(prefab, context.Root, context.Original);
        return context.Root;
    }

    private sealed record VisualContext(GameObject Root, Renderer[] Original, Color Tint, Material Shell, Material Core, Material Shard);

    private static VisualContext Begin(GameObject prefab, string family, ElementalAlignment alignment)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"Creature host '{prefab.name}' exposes no material source.");
        var root = new GameObject("magenheim.fracture.creature." + family + ".visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);
        root.transform.localPosition = new Vector3(0f, .18f, 0f);
        var tint = ElementVisualPalette.Tint(alignment);
        return new VisualContext(root, original, tint,
            Material(source, family + ".shell", Color.Lerp(tint, new Color(.28f, .25f, .22f), .62f), .18f, .58f, .18f),
            Material(source, family + ".core", Color.Lerp(tint, Color.white, .68f), .02f, .96f, 1.35f),
            Material(source, family + ".shard", tint, .05f, .78f, .82f));
    }

    private static void HideOriginal(GameObject prefab, GameObject root, IEnumerable<Renderer> original)
    {
        foreach (var renderer in original) if (!renderer.transform.IsChildOf(root.transform)) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void AddLight(GameObject root, Color tint, float range, float intensity)
    {
        var light = root.AddComponent<Light>(); light.color = tint; light.range = range; light.intensity = intensity;
    }

    private static Material Material(Material source, string suffix, Color color, float metallic, float gloss, float emission)
    {
        var material = new Material(source) { name = "magenheim.fracture.creature." + suffix };
        material.mainTexture = Texture2D.whiteTexture;
        material.mainTextureScale = Vector2.one; material.mainTextureOffset = Vector2.zero;
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
        material.DisableKeyword("_NORMALMAP");
        if (material.HasProperty("_EmissionColor")) { material.SetColor("_EmissionColor", color * emission); material.EnableKeyword("_EMISSION"); }
        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        material.renderQueue = 2000;
        return material;
    }

    private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3? rotation = null)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh)) { mesh = CreatePrismMesh(sides); PrismMeshes.Add(sides, mesh); }
        var part = new GameObject(name) { layer = root.layer };
        part.transform.SetParent(root.transform, false); part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero); part.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
        part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Mesh CreatePrismMesh(int sides)
    {
        var vertices = new List<Vector3>(sides * 2 + 2); var triangles = new List<int>(sides * 12);
        for (var i = 0; i < sides; i++) { var a = 2f * Mathf.PI * i / sides; vertices.Add(new Vector3(.36f * Mathf.Cos(a), -.45f, .36f * Mathf.Sin(a))); }
        for (var i = 0; i < sides; i++) { var a = 2f * Mathf.PI * i / sides; vertices.Add(new Vector3(.5f * Mathf.Cos(a), .20f, .5f * Mathf.Sin(a))); }
        var top = vertices.Count; vertices.Add(new Vector3(0f, .68f, 0f)); var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.50f, 0f));
        for (var i = 0; i < sides; i++) { var next = (i + 1) % sides; triangles.AddRange(new[] { bottom, next, i, i, next, sides + i, next, sides + next, sides + i, sides + i, sides + next, top }); }
        var mesh = new Mesh { name = "magenheim.fracture.creature.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
