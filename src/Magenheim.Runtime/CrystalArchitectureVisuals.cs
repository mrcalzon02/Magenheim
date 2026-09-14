using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Procedural geometry for Magenheim's rainbow crystal construction family.
/// The beams and foundations use the same faceted structural language at different scales;
/// the hearth preserves the cloned vanilla fireplace behavior while replacing its visible shell.
/// </summary>
internal static class CrystalArchitectureVisuals
{
    internal const string CrystalHearth = "architecture-crystal-hearth";
    internal const string CrystalBeam2 = "architecture-crystal-beam-2m";
    internal const string CrystalBeam4 = "architecture-crystal-beam-4m";
    internal const string CrystalBeam8 = "architecture-crystal-beam-8m";
    internal const string CrystalFoundation2 = "architecture-crystal-foundation-2m";
    internal const string CrystalFoundation4 = "architecture-crystal-foundation-4m";
    internal const string CrystalFoundation8 = "architecture-crystal-foundation-8m";

    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, string modelId)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Architecture model id is required.", nameof(modelId));

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original
            .Where(renderer => !(renderer is ParticleSystemRenderer))
            .Select(renderer => renderer.sharedMaterial)
            .FirstOrDefault(material => material)
            ?? original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on architecture host '{prefab.name}'.");

        var root = new GameObject("magenheim." + modelId + ".visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);

        var darkStone = Material(source, modelId + ".darkstone", new Color(.13f, .14f, .15f, 1f), .02f, .10f);
        var stone = Material(source, modelId + ".stone", new Color(.31f, .31f, .32f, 1f), .02f, .12f);
        var iron = Material(source, modelId + ".iron", new Color(.25f, .28f, .30f, 1f), .62f, .30f);
        var rainbow = RainbowMaterials(source, modelId);

        switch (modelId)
        {
            case CrystalHearth:
                BuildHearth(root, darkStone, stone, iron, rainbow);
                TintVanillaFlame(prefab);
                break;
            case CrystalBeam2:
                BuildBeam(root, 2f, iron, rainbow);
                break;
            case CrystalBeam4:
                BuildBeam(root, 4f, iron, rainbow);
                break;
            case CrystalBeam8:
                BuildBeam(root, 8f, iron, rainbow);
                break;
            case CrystalFoundation2:
                BuildFoundation(root, 2f, iron, darkStone, rainbow);
                break;
            case CrystalFoundation4:
                BuildFoundation(root, 4f, iron, darkStone, rainbow);
                break;
            case CrystalFoundation8:
                BuildFoundation(root, 8f, iron, darkStone, rainbow);
                break;
            default:
                throw new InvalidOperationException($"Unknown Magenheim crystal architecture model '{modelId}'.");
        }

        foreach (var renderer in original)
        {
            if (renderer is ParticleSystemRenderer) continue;
            renderer.enabled = false;
        }
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static void BuildHearth(GameObject root, Material darkStone, Material stone, Material iron, IReadOnlyList<Material> rainbow)
    {
        Box(root, "base", new Vector3(0f, .13f, 0f), new Vector3(2.25f, .26f, 1.52f), stone);
        Box(root, "fire-bed", new Vector3(0f, .28f, .05f), new Vector3(1.58f, .18f, .92f), darkStone);
        Box(root, "back", new Vector3(0f, .72f, .57f), new Vector3(2.00f, 1.12f, .22f), stone);
        Box(root, "left-cheek", new Vector3(-.93f, .53f, .05f), new Vector3(.30f, .84f, 1.20f), stone);
        Box(root, "right-cheek", new Vector3(.93f, .53f, .05f), new Vector3(.30f, .84f, 1.20f), stone);
        Box(root, "front-band", new Vector3(0f, .43f, -.61f), new Vector3(1.88f, .09f, .09f), iron);
        Box(root, "rear-band", new Vector3(0f, .43f, .60f), new Vector3(1.88f, .09f, .09f), iron);

        for (var i = 0; i < rainbow.Count; i++)
        {
            var x = -0.72f + i * .24f;
            var height = .48f + .07f * (i % 3);
            Prism(root, "rainbow-crystal-" + i,
                new Vector3(x, .46f + height * .33f, .48f),
                new Vector3(.13f, height, .13f), 6, rainbow[i],
                new Vector3(3f * (i % 2 == 0 ? 1 : -1), i * 11f, (i - 3) * 2.4f));
        }

        for (var i = 0; i < rainbow.Count; i++)
        {
            var angle = Mathf.PI * 2f * i / rainbow.Count;
            Prism(root, "ember-crystal-" + i,
                new Vector3(.48f * Mathf.Cos(angle), .36f, .18f + .33f * Mathf.Sin(angle)),
                new Vector3(.085f, .22f, .085f), 5, rainbow[(i + 2) % rainbow.Count],
                new Vector3(12f, -angle * Mathf.Rad2Deg, 8f));
        }
    }

    private static void BuildBeam(GameObject root, float height, Material iron, IReadOnlyList<Material> rainbow)
    {
        var segmentHeight = height / rainbow.Count;
        for (var i = 0; i < rainbow.Count; i++)
        {
            var y = segmentHeight * (.5f + i);
            Prism(root, "segment-" + i, new Vector3(0f, y, 0f),
                new Vector3(.46f, segmentHeight * 1.06f, .46f), 8, rainbow[i],
                new Vector3(i % 2 == 0 ? 1.5f : -1.5f, i * 9f, i % 3 - 1));
        }

        Box(root, "base-collar", new Vector3(0f, .07f, 0f), new Vector3(.58f, .14f, .58f), iron);
        Box(root, "top-collar", new Vector3(0f, height - .07f, 0f), new Vector3(.58f, .14f, .58f), iron);
        for (var quarter = 1; quarter < 4; quarter++)
        {
            var y = height * quarter / 4f;
            Box(root, "brace-" + quarter, new Vector3(0f, y, 0f), new Vector3(.50f, .055f, .50f), iron,
                new Vector3(0f, quarter * 22.5f, 0f));
        }
    }

    private static void BuildFoundation(GameObject root, float size, Material iron, Material darkStone, IReadOnlyList<Material> rainbow)
    {
        var cells = Mathf.Max(1, Mathf.RoundToInt(size / 2f));
        var cell = size / cells;
        var start = -size * .5f + cell * .5f;

        Box(root, "shadow-bed", new Vector3(0f, .07f, 0f), new Vector3(size, .14f, size), darkStone);

        for (var x = 0; x < cells; x++)
        {
            for (var z = 0; z < cells; z++)
            {
                var px = start + x * cell;
                var pz = start + z * cell;
                var colorIndex = (x * 2 + z * 3) % rainbow.Count;
                Prism(root, $"facet-{x}-{z}", new Vector3(px, .19f, pz),
                    new Vector3(cell * .92f, .30f, cell * .92f), 8, rainbow[colorIndex],
                    new Vector3(0f, (x + z) * 11.25f, 0f));
            }
        }

        var edgeThickness = .10f;
        var edgeHeight = .16f;
        Box(root, "edge-north", new Vector3(0f, .20f, size * .5f - edgeThickness * .5f), new Vector3(size, edgeHeight, edgeThickness), iron);
        Box(root, "edge-south", new Vector3(0f, .20f, -size * .5f + edgeThickness * .5f), new Vector3(size, edgeHeight, edgeThickness), iron);
        Box(root, "edge-east", new Vector3(size * .5f - edgeThickness * .5f, .20f, 0f), new Vector3(edgeThickness, edgeHeight, size), iron);
        Box(root, "edge-west", new Vector3(-size * .5f + edgeThickness * .5f, .20f, 0f), new Vector3(edgeThickness, edgeHeight, size), iron);
    }

    private static void TintVanillaFlame(GameObject prefab)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, .12f, .08f), 0f),
                new GradientColorKey(new Color(1f, .78f, .08f), .16f),
                new GradientColorKey(new Color(.35f, 1f, .20f), .33f),
                new GradientColorKey(new Color(.10f, .90f, 1f), .50f),
                new GradientColorKey(new Color(.20f, .36f, 1f), .67f),
                new GradientColorKey(new Color(.72f, .18f, 1f), .84f),
                new GradientColorKey(new Color(1f, .16f, .62f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, .75f),
                new GradientAlphaKey(0f, 1f)
            });

        var tinted = 0;
        foreach (var system in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            var name = system.gameObject.name;
            if (name.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (name.IndexOf("sparks", StringComparison.OrdinalIgnoreCase) >= 0) continue;

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            var main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
            tinted++;
        }

        var lightColors = new[]
        {
            new Color(1f, .22f, .12f),
            new Color(.16f, .72f, 1f),
            new Color(.78f, .22f, 1f)
        };
        var lights = prefab.GetComponentsInChildren<Light>(true);
        for (var i = 0; i < lights.Length; i++)
        {
            lights[i].color = lightColors[i % lightColors.Length];
            lights[i].intensity = Mathf.Max(lights[i].intensity, 1.35f);
        }

        if (tinted == 0)
            throw new InvalidOperationException("Crystal Hearth source contains no non-smoke flame particle system to recolor.");
    }

    private static Material[] RainbowMaterials(Material source, string modelId)
    {
        var colors = new[]
        {
            new Color(1.00f, .16f, .12f, 1f),
            new Color(1.00f, .55f, .10f, 1f),
            new Color(1.00f, .90f, .16f, 1f),
            new Color(.30f, .95f, .28f, 1f),
            new Color(.14f, .88f, 1.00f, 1f),
            new Color(.28f, .38f, 1.00f, 1f),
            new Color(.78f, .22f, 1.00f, 1f)
        };
        var materials = new Material[colors.Length];
        for (var i = 0; i < colors.Length; i++)
            materials[i] = Material(source, modelId + ".rainbow." + i, colors[i], .02f, .78f, .48f);
        return materials;
    }

    private static void Box(GameObject root, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null) =>
        Add(root, name, BoxMesh, position, scale, Quaternion.Euler(euler ?? Vector3.zero), material);

    private static void Prism(GameObject root, string name, Vector3 position, Vector3 scale, int sides, Material material, Vector3? euler = null)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = CreatePrismMesh(sides);
            PrismMeshes.Add(sides, mesh);
        }
        Add(root, name, mesh, position, scale, Quaternion.Euler(euler ?? Vector3.zero), material);
    }

    private static void Add(GameObject root, string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var part = new GameObject(name) { layer = root.layer };
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = rotation;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material Material(Material source, string suffix, Color color, float metallic, float glossiness, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.architecture." + suffix, mainTexture = Texture2D.whiteTexture };
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", glossiness);
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

    private static Mesh CreateBoxMesh()
    {
        var mesh = new Mesh { name = "magenheim.architecture.box" };
        mesh.vertices = new[]
        {
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f)
        };
        mesh.triangles = new[] { 0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreatePrismMesh(int sides)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (var i = 0; i < sides; i++)
        {
            var angle = Mathf.PI * 2f * i / sides;
            vertices.Add(new Vector3(.46f * Mathf.Cos(angle), -.50f, .46f * Mathf.Sin(angle)));
        }
        for (var i = 0; i < sides; i++)
        {
            var angle = Mathf.PI * 2f * i / sides;
            vertices.Add(new Vector3(.50f * Mathf.Cos(angle), .34f, .50f * Mathf.Sin(angle)));
        }
        var top = vertices.Count;
        vertices.Add(new Vector3(0f, .58f, 0f));
        var bottom = vertices.Count;
        vertices.Add(new Vector3(0f, -.52f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[]
            {
                bottom, next, i,
                i, next, sides + i,
                next, sides + next, sides + i,
                sides + i, sides + next, top
            });
        }
        var mesh = new Mesh
        {
            name = "magenheim.architecture.prism." + sides,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
