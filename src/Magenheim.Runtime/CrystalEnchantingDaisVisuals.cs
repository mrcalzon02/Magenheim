using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Procedural ritual geometry for the Crystal Enchanting Dais. It is a broad, low working
/// platform with inset elemental channels rather than a crystal monument occupying the workspace.
/// </summary>
internal static class CrystalEnchantingDaisVisuals
{
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"Crystal Enchanting Dais host '{prefab.name}' exposes no material source.");

        var root = new GameObject("magenheim.crystal-enchanting-dais.visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);

        var darkStone = Material(source, "dark-stone", new Color(.12f, .13f, .15f, 1f), .08f, .19f);
        var faceStone = Material(source, "face-stone", new Color(.25f, .27f, .29f, 1f), .10f, .25f);
        var iron = Material(source, "iron", new Color(.24f, .27f, .31f, 1f), .76f, .36f);
        var paleCrystal = Material(source, "central-crystal", new Color(.82f, .91f, 1f, 1f), .03f, .95f, .86f);

        // Broad stepped stone courses create a usable raised floor. Nothing at the centre rises
        // high enough to read as a monument or obstruct the player's view across the platform.
        Cylinder(root, "lower-course", new Vector3(0f, .10f, 0f), 1.82f, .20f, 16, darkStone);
        Cylinder(root, "middle-course", new Vector3(0f, .24f, 0f), 1.58f, .12f, 16, faceStone);
        Cylinder(root, "working-face", new Vector3(0f, .35f, 0f), 1.36f, .10f, 16, darkStone);
        Ring(root, "outer-band", 1.55f, .32f, 16, .34f, .070f, iron);
        Ring(root, "inner-band", .91f, .425f, 12, .35f, .050f, iron);

        var elements = (ElementalAlignment[])Enum.GetValues(typeof(ElementalAlignment));
        for (var i = 0; i < elements.Length; i++)
        {
            var angle = i * 360f / elements.Length;
            var radians = angle * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            AddPart(root, "channel-" + elements[i].ToString().ToLowerInvariant(), BoxMesh,
                direction * .68f + Vector3.up * .425f,
                new Vector3(.095f, .035f, .76f), Quaternion.Euler(0f, angle, 0f), iron);

            var tint = ElementVisualPalette.Tint(elements[i]);
            var crystalMaterial = Material(source, "node-crystal-" + elements[i].ToString().ToLowerInvariant(),
                new Color(Mathf.Min(1f, tint.r + .10f), Mathf.Min(1f, tint.g + .10f), Mathf.Min(1f, tint.b + .10f), 1f),
                .02f, .93f, .62f);

            // Low perimeter markers identify each alignment without turning the dais into a forest
            // of spikes. Their tips sit only slightly above the working surface.
            Prism(root, "node-" + elements[i].ToString().ToLowerInvariant(),
                direction * 1.13f + Vector3.up * .49f, .095f, .22f, 6, crystalMaterial,
                new Vector3(i % 2 == 0 ? -4f : 4f, angle, 0f));
        }

        // Recessed central focus: a shallow metal socket and luminous crystal disk, not an apex.
        Cylinder(root, "focus-plinth", new Vector3(0f, .435f, 0f), .47f, .07f, 12, faceStone);
        Cylinder(root, "focus-collar", new Vector3(0f, .475f, 0f), .39f, .045f, 16, iron);
        Cylinder(root, "central-focus", new Vector3(0f, .505f, 0f), .30f, .035f, 16, paleCrystal);
        Ring(root, "focus-runes", .34f, .505f, 12, .17f, .035f, iron);

        var lightHolder = new GameObject("magenheim.crystal-enchanting-dais.light") { layer = prefab.layer };
        lightHolder.transform.SetParent(root.transform, false);
        lightHolder.transform.localPosition = new Vector3(0f, .62f, 0f);
        var light = lightHolder.AddComponent<Light>();
        light.color = new Color(.68f, .82f, 1f, 1f);
        light.range = 3.8f;
        light.intensity = .68f;

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static void Ring(GameObject root, string prefix, float radius, float y, int segments, float segmentLength, float thickness, Material material)
    {
        for (var i = 0; i < segments; i++)
        {
            var angle = i * 360f / segments;
            var radians = angle * Mathf.Deg2Rad;
            var position = new Vector3(Mathf.Sin(radians) * radius, y, Mathf.Cos(radians) * radius);
            AddPart(root, prefix + "-" + i, BoxMesh, position,
                new Vector3(segmentLength, thickness, thickness), Quaternion.Euler(0f, angle, 0f), material);
        }
    }

    private static Material Material(Material source, string suffix, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.crystal-enchanting-dais." + suffix };
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

    private static void Cylinder(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material)
    {
        if (!CylinderMeshes.TryGetValue(sides, out var mesh)) { mesh = CreateCylinderMesh(sides); CylinderMeshes.Add(sides, mesh); }
        AddPart(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);
    }

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
        var mesh = new Mesh { name = "magenheim.crystal-enchanting-dais.box" };
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
        for (var i = 0; i < sides; i++)
        {
            var angle = 2f * Mathf.PI * i / sides;
            vertices.Add(new Vector3(.5f * Mathf.Cos(angle), -.5f, .5f * Mathf.Sin(angle)));
        }
        for (var i = 0; i < sides; i++)
        {
            var angle = 2f * Mathf.PI * i / sides;
            vertices.Add(new Vector3(.5f * Mathf.Cos(angle), .5f, .5f * Mathf.Sin(angle)));
        }
        var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.5f, 0f));
        var top = vertices.Count; vertices.Add(new Vector3(0f, .5f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[]
            {
                bottom, i, next,
                i, sides + i, next,
                next, sides + i, sides + next,
                top, sides + next, sides + i,
            });
        }
        var mesh = new Mesh { name = "magenheim.crystal-enchanting-dais.cylinder." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
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
            triangles.AddRange(new[]
            {
                bottom, i, next,
                i, sides + i, next,
                next, sides + i, sides + next,
                top, sides + next, sides + i,
            });
        }
        var mesh = new Mesh { name = "magenheim.crystal-enchanting-dais.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
