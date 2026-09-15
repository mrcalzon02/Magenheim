using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Magenheim.Runtime;

/// <summary>Procedural water-filled cultivation basin used by each alignment-specific Crystal Bed.</summary>
internal static class CrystalBedVisuals
{
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, ElementalAlignment element)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"Crystal Bed host '{prefab.name}' exposes no material source.");

        var root = new GameObject("magenheim.crystal-bed." + element.ToString().ToLowerInvariant() + ".visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);

        var tint = ElementVisualPalette.Tint(element);
        var stone = Material(source, "stone", new Color(.10f, .12f, .14f, 1f), .14f, .18f);
        var iron = Material(source, "iron", new Color(.25f, .29f, .32f, 1f), .68f, .32f);
        var crystal = Material(source, "growth", new Color(Mathf.Min(1f, tint.r + .14f), Mathf.Min(1f, tint.g + .14f), Mathf.Min(1f, tint.b + .14f), 1f), .02f, .90f, .48f);
        var water = WaterMaterial(source, tint);

        Box(root, "floor", new Vector3(0f, .18f, 0f), new Vector3(3.10f, .36f, 2.20f), stone);
        Box(root, "north-wall", new Vector3(0f, .50f, .98f), new Vector3(3.10f, .62f, .22f), stone);
        Box(root, "south-wall", new Vector3(0f, .50f, -.98f), new Vector3(3.10f, .62f, .22f), stone);
        Box(root, "west-wall", new Vector3(-1.44f, .50f, 0f), new Vector3(.22f, .62f, 1.78f), stone);
        Box(root, "east-wall", new Vector3(1.44f, .50f, 0f), new Vector3(.22f, .62f, 1.78f), stone);
        Box(root, "north-band", new Vector3(0f, .77f, 1.00f), new Vector3(3.18f, .08f, .10f), iron);
        Box(root, "south-band", new Vector3(0f, .77f, -1.00f), new Vector3(3.18f, .08f, .10f), iron);
        Box(root, "west-band", new Vector3(-1.48f, .77f, 0f), new Vector3(.10f, .08f, 1.92f), iron);
        Box(root, "east-band", new Vector3(1.48f, .77f, 0f), new Vector3(.10f, .08f, 1.92f), iron);
        Box(root, "mineral-water", new Vector3(0f, .59f, 0f), new Vector3(2.62f, .10f, 1.55f), water);

        var growths = new[]
        {
            new Vector3(-.85f, .76f, -.38f), new Vector3(-.32f, .74f, .42f), new Vector3(.22f, .78f, -.30f),
            new Vector3(.78f, .75f, .36f), new Vector3(-.08f, .84f, .10f), new Vector3(.52f, .73f, -.62f),
        };
        for (var i = 0; i < growths.Length; i++)
        {
            var height = .42f + (i % 3) * .12f;
            Prism(root, "growth-" + i, growths[i], .12f + (i % 2) * .035f, height, 6, crystal,
                new Vector3((i % 2 == 0 ? -8f : 9f), i * 31f, (i % 3 - 1) * 7f));
        }

        var light = root.AddComponent<Light>();
        light.color = tint;
        light.range = 3.3f;
        light.intensity = .65f;
        light.transform.localPosition = new Vector3(0f, .82f, 0f);

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static Material Material(Material source, string suffix, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.crystal-bed." + suffix };
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
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

    private static Material WaterMaterial(Material source, Color tint)
    {
        var color = new Color(tint.r * .72f + .12f, tint.g * .72f + .12f, tint.b * .72f + .12f, .58f);
        var material = Material(source, "mineral-water", color, 0f, .96f, .14f);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        return material;
    }

    private static void Box(GameObject root, string name, Vector3 position, Vector3 size, Material material) =>
        AddPart(root, name, BoxMesh, position, size, Quaternion.identity, material);

    private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3 rotation)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh)) { mesh = CreatePrismMesh(sides); PrismMeshes.Add(sides, mesh); }
        AddPart(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.Euler(rotation), material);
    }

    private static void AddPart(GameObject root, string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var part = new GameObject(name) { layer = root.layer };
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale = scale;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Mesh CreateBoxMesh()
    {
        var mesh = new Mesh { name = "magenheim.crystal-bed.box" };
        mesh.vertices = new[]
        {
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f),
        };
        mesh.triangles = new[] { 0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2 };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh CreatePrismMesh(int sides)
    {
        var vertices = new List<Vector3>(sides * 2 + 2);
        var triangles = new List<int>(sides * 12);
        for (var i = 0; i < sides; i++)
        {
            var angle = 2f * Mathf.PI * i / sides;
            vertices.Add(new Vector3(.36f * Mathf.Cos(angle), -.45f, .36f * Mathf.Sin(angle)));
        }
        for (var i = 0; i < sides; i++)
        {
            var angle = 2f * Mathf.PI * i / sides;
            vertices.Add(new Vector3(.5f * Mathf.Cos(angle), .20f, .5f * Mathf.Sin(angle)));
        }
        var top = vertices.Count; vertices.Add(new Vector3(0f, .68f, 0f));
        var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.50f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[] { bottom, i, next, i, sides + i, next, next, sides + i, sides + next, top, sides + next, sides + i });
        }
        var mesh = new Mesh { name = "magenheim.crystal-bed.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
