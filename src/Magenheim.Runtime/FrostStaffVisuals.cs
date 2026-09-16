using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Original runtime geometry for the four Frost staff tiers. The models are assembled from
/// small authored meshes rather than retaining StaffIceShards' vanilla visual hierarchy.
/// </summary>
internal static class FrostStaffVisuals
{
    private static readonly Mesh BoxMesh = RuntimeMeshPrimitives.Box("magenheim.staff.box");
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, string assetName)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(assetName)) throw new ArgumentException("Asset name is required.", nameof(assetName));

        var originalRenderers = prefab.GetComponentsInChildren<Renderer>(true);
        var sourceMaterial = originalRenderers
            .Select(renderer => renderer.sharedMaterial)
            .FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on Frost staff prefab '{prefab.name}'.");

        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var wood = Material(sourceMaterial, "wood", new Color(.29f, .21f, .16f, 1f), 0f, .14f);
        var paleWood = Material(sourceMaterial, "pale-wood", new Color(.46f, .36f, .27f, 1f), 0f, .16f);
        var iron = Material(sourceMaterial, "iron", new Color(.36f, .40f, .43f, 1f), .42f, .22f);
        var silver = Material(sourceMaterial, "silver", new Color(.67f, .75f, .80f, 1f), .62f, .34f);
        var frost = Material(sourceMaterial, "frost-crystal", new Color(.44f, .78f, .92f, 1f), .08f, .58f, .16f);
        var frostBright = Material(sourceMaterial, "frost-bright-crystal", new Color(.73f, .93f, .98f, 1f), .05f, .72f, .28f);
        var frostDeep = Material(sourceMaterial, "frost-deep-crystal", new Color(.22f, .51f, .72f, 1f), .10f, .48f, .12f);

        switch (assetName)
        {
            case "staff-frost-simple": BuildSimple(root, wood, paleWood, iron, frost); break;
            case "staff-frost-crystal": BuildCrystal(root, paleWood, silver, frostBright); break;
            case "staff-frost-advanced": BuildAdvanced(root, wood, silver, frostBright, frostDeep); break;
            case "staff-frost-master": BuildMaster(root, wood, silver, frost, frostBright, frostDeep); break;
            default: throw new InvalidOperationException($"Unknown Frost staff geometry '{assetName}'.");
        }

        foreach (var renderer in originalRenderers) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static void BuildSimple(GameObject root, Material wood, Material paleWood, Material iron, Material frost)
    {
        Shaft(root, wood, iron, .034f);
        Box(root, "left-fork", new Vector3(-.065f, .69f, 0), new Vector3(.052f, .38f, .052f), paleWood, -13.75f);
        Box(root, "right-fork", new Vector3(.065f, .69f, 0), new Vector3(.052f, .38f, .052f), paleWood, 13.75f);
        Cylinder(root, "fork-band", new Vector3(0, .59f, 0), .052f, .055f, 10, iron);
        Prism(root, "simple-crystal", new Vector3(0, .80f, 0), .070f, .28f, 6, frost);
    }

    private static void BuildCrystal(GameObject root, Material wood, Material silver, Material frost)
    {
        Shaft(root, wood, silver, .038f);
        Cylinder(root, "crown-band", new Vector3(0, .59f, 0), .066f, .075f, 12, silver);
        Box(root, "crossbar", new Vector3(0, .69f, 0), new Vector3(.31f, .040f, .045f), silver);
        Box(root, "left-tine", new Vector3(-.12f, .77f, 0), new Vector3(.040f, .25f, .040f), silver, -20.05f);
        Box(root, "right-tine", new Vector3(.12f, .77f, 0), new Vector3(.040f, .25f, .040f), silver, 20.05f);
        Prism(root, "crystal-focus", new Vector3(0, .82f, 0), .090f, .37f, 6, frost);
    }

    private static void BuildAdvanced(GameObject root, Material wood, Material silver, Material frost, Material frostDeep)
    {
        Shaft(root, wood, silver, .041f);
        Cylinder(root, "lower-band", new Vector3(0, .28f, 0), .058f, .050f, 12, silver);
        Cylinder(root, "upper-band", new Vector3(0, .58f, 0), .073f, .075f, 12, silver);
        const float radius = .22f;
        for (var index = 0; index < 6; index++)
        {
            var angle = Mathf.PI / 3f * index;
            Box(root, "crown-" + index,
                new Vector3(radius * .62f * Mathf.Cos(angle), .81f + radius * .62f * Mathf.Sin(angle), 0),
                new Vector3(.20f, .034f, .044f), silver, angle * Mathf.Rad2Deg + 90f);
        }
        Prism(root, "advanced-focus", new Vector3(0, .84f, 0), .105f, .45f, 6, frost);
        Prism(root, "left-satellite", new Vector3(-.16f, .72f, .02f), .036f, .17f, 6, frostDeep);
        Prism(root, "right-satellite", new Vector3(.16f, .72f, -.02f), .036f, .17f, 6, frostDeep);
    }

    private static void BuildMaster(GameObject root, Material wood, Material silver, Material frost, Material frostBright, Material frostDeep)
    {
        Shaft(root, wood, silver, .043f);
        foreach (var y in new[] { -.48f, -.05f, .37f, .58f })
            Cylinder(root, "shaft-band-" + y, new Vector3(0, y, 0), .062f, .045f, 12, silver);
        for (var index = 0; index < 6; index++)
        {
            var angle = Mathf.PI / 3f * index;
            var x = .155f * Mathf.Cos(angle);
            var z = .155f * Mathf.Sin(angle);
            Box(root, "master-tine-" + index, new Vector3(x, .83f, z), new Vector3(.035f, .34f, .035f), silver, 10.31f * Mathf.Cos(angle));
            Prism(root, "master-satellite-" + index, new Vector3(x, .99f, z), .030f, .15f, 6, index % 2 == 0 ? frost : frostDeep);
        }
        Cylinder(root, "master-halo", new Vector3(0, .70f, 0), .18f, .045f, 14, silver);
        Prism(root, "master-focus", new Vector3(0, .91f, 0), .125f, .54f, 6, frostBright);
    }

    private static void Shaft(GameObject root, Material wood, Material metal, float radius)
    {
        Cylinder(root, "shaft", new Vector3(0, -.08f, 0), radius, 1.48f, 10, wood);
        Cylinder(root, "pommel", new Vector3(0, -.77f, 0), radius * 1.35f, .10f, 10, metal);
        Cylinder(root, "neck-band", new Vector3(0, .50f, 0), radius * 1.30f, .055f, 10, metal);
    }

    private static void Box(GameObject root, string name, Vector3 position, Vector3 size, Material material, float zDegrees = 0f) =>
        AddPart(root, name, BoxMesh, position, size, Quaternion.Euler(0, 0, zDegrees), material);

    private static void Cylinder(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material)
    {
        if (!CylinderMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = RuntimeMeshPrimitives.Cylinder(sides, "magenheim.staff.cylinder." + sides);
            CylinderMeshes.Add(sides, mesh);
        }
        AddPart(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);
    }

    private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = RuntimeMeshPrimitives.Prism(
                sides,
                "magenheim.staff.prism." + sides,
                lowerRadius: .36f,
                lowerY: -.45f,
                upperRadius: .50f,
                upperY: .20f,
                apexY: .68f,
                baseY: -.50f);
            PrismMeshes.Add(sides, mesh);
        }
        AddPart(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);
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

    private static Material Material(Material source, string suffix, Color color, float metallic, float glossiness, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.frost-staff." + suffix };
        GeneratedSurfaceTextures.Apply(material, suffix);
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
}
