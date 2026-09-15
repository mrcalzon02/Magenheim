using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Original procedural geometry for Magenheim's elemental banner families.</summary>
internal static class CrystalBannerVisuals
{
    internal enum BannerStyle { Standard, Swallowtail, Pennant }

    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, BannerStyle style, ElementalAlignment element)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on banner host '{prefab.name}'.");

        var root = new GameObject($"magenheim.banner.{style.ToString().ToLowerInvariant()}.{element.ToString().ToLowerInvariant()}.visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);

        var tint = ElementVisualPalette.Tint(element);
        var cloth = Material(source, "cloth", new Color(tint.r * .72f, tint.g * .72f, tint.b * .72f, 1f), 0f, .08f);
        var clothBright = Material(source, "cloth-bright", tint, 0f, .12f, .06f);
        var wood = Material(source, "wood", new Color(.24f, .15f, .08f, 1f), 0f, .12f);
        var iron = Material(source, "iron", new Color(.28f, .31f, .34f, 1f), .62f, .28f);
        var crystal = Material(source, "crystal", new Color(Mathf.Min(1f, tint.r * 1.15f + .08f), Mathf.Min(1f, tint.g * 1.15f + .08f), Mathf.Min(1f, tint.b * 1.15f + .08f), 1f), .03f, .78f, .34f);

        Cylinder(root, "top-rod", new Vector3(0f, .72f, 0f), .035f, 1.12f, 10, wood, new Vector3(0f, 0f, 90f));
        Cylinder(root, "left-cap", new Vector3(-.59f, .72f, 0f), .055f, .10f, 8, iron, new Vector3(0f, 0f, 90f));
        Cylinder(root, "right-cap", new Vector3(.59f, .72f, 0f), .055f, .10f, 8, iron, new Vector3(0f, 0f, 90f));

        switch (style)
        {
            case BannerStyle.Standard: BuildStandard(root, cloth, clothBright, iron, crystal); break;
            case BannerStyle.Swallowtail: BuildSwallowtail(root, cloth, clothBright, iron, crystal); break;
            case BannerStyle.Pennant: BuildPennant(root, cloth, clothBright, iron, crystal); break;
            default: throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static void BuildStandard(GameObject root, Material cloth, Material accent, Material iron, Material crystal)
    {
        Box(root, "field", new Vector3(0f, .02f, .025f), new Vector3(.94f, 1.30f, .035f), cloth);
        Box(root, "left-fold", new Vector3(-.30f, .02f, .006f), new Vector3(.055f, 1.24f, .030f), accent);
        Box(root, "right-fold", new Vector3(.30f, .02f, .006f), new Vector3(.055f, 1.24f, .030f), accent);
        Box(root, "lower-rail", new Vector3(0f, -.65f, .012f), new Vector3(.96f, .055f, .055f), iron);
        Prism(root, "crystal-crest", new Vector3(0f, .03f, -.045f), .13f, .54f, 6, crystal);
    }

    private static void BuildSwallowtail(GameObject root, Material cloth, Material accent, Material iron, Material crystal)
    {
        Box(root, "upper-field", new Vector3(0f, .18f, .025f), new Vector3(.94f, .98f, .035f), cloth);
        Box(root, "left-tail", new Vector3(-.235f, -.58f, .025f), new Vector3(.45f, .70f, .035f), cloth, new Vector3(0f, 0f, -4f));
        Box(root, "right-tail", new Vector3(.235f, -.58f, .025f), new Vector3(.45f, .70f, .035f), cloth, new Vector3(0f, 0f, 4f));
        Box(root, "left-seam", new Vector3(-.22f, .10f, .006f), new Vector3(.045f, .92f, .030f), accent);
        Box(root, "right-seam", new Vector3(.22f, .10f, .006f), new Vector3(.045f, .92f, .030f), accent);
        Prism(root, "crystal-crest", new Vector3(0f, .20f, -.045f), .13f, .50f, 6, crystal);
        Cylinder(root, "tail-ring", new Vector3(0f, -.31f, -.020f), .10f, .05f, 10, iron, new Vector3(90f, 0f, 0f));
    }

    private static void BuildPennant(GameObject root, Material cloth, Material accent, Material iron, Material crystal)
    {
        var widths = new[] { .82f, .70f, .56f, .42f, .27f };
        for (var i = 0; i < widths.Length; i++)
        {
            var y = .50f - i * .28f;
            Box(root, "taper-" + i, new Vector3(0f, y, .025f), new Vector3(widths[i], .30f, .035f), i % 2 == 0 ? cloth : accent);
        }
        Prism(root, "lower-point", new Vector3(0f, -.76f, .025f), .13f, .42f, 4, cloth, new Vector3(0f, 0f, 45f));
        Box(root, "spine", new Vector3(0f, -.05f, -.005f), new Vector3(.045f, 1.35f, .045f), iron);
        Prism(root, "crystal-crest", new Vector3(0f, .25f, -.055f), .11f, .45f, 6, crystal);
    }

    private static Material Material(Material source, string suffix, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.banner." + suffix };
        GeneratedSurfaceTextures.Apply(material, suffix);
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

    private static void Box(GameObject root, string name, Vector3 position, Vector3 size, Material material, Vector3? rotation = null) =>
        AddPart(root, name, BoxMesh, position, size, Quaternion.Euler(rotation ?? Vector3.zero), material);

    private static void Cylinder(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3? rotation = null)
    {
        if (!CylinderMeshes.TryGetValue(sides, out var mesh)) { mesh = CreateCylinderMesh(sides); CylinderMeshes.Add(sides, mesh); }
        AddPart(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.Euler(rotation ?? Vector3.zero), material);
    }

    private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3? rotation = null)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh)) { mesh = CreatePrismMesh(sides); PrismMeshes.Add(sides, mesh); }
        AddPart(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.Euler(rotation ?? Vector3.zero), material);
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
        var mesh = new Mesh { name = "magenheim.banner.box" };
        mesh.vertices = new[]
        {
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f),
        };
        mesh.triangles = new[] { 0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2 };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh CreateCylinderMesh(int sides)
    {
        var vertices = new List<Vector3>(sides * 2 + 2);
        var triangles = new List<int>(sides * 12);
        for (var ring = 0; ring < 2; ring++)
        {
            var y = ring == 0 ? -.5f : .5f;
            for (var i = 0; i < sides; i++)
            {
                var angle = 2f * Mathf.PI * i / sides;
                vertices.Add(new Vector3(.5f * Mathf.Cos(angle), y, .5f * Mathf.Sin(angle)));
            }
        }
        var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.5f, 0f));
        var top = vertices.Count; vertices.Add(new Vector3(0f, .5f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[] { i, sides + i, next, next, sides + i, sides + next, bottom, i, next, top, sides + next, sides + i });
        }
        var mesh = new Mesh { name = "magenheim.banner.cylinder." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
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
        var mesh = new Mesh { name = "magenheim.banner.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
