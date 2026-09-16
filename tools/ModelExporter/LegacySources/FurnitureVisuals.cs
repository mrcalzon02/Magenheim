using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Original procedural geometry for Magenheim's geology/crystal furniture collection.</summary>
internal static class FurnitureVisuals
{
    internal const string GeodeTable = "furniture-geode-table";
    internal const string GeodeChair = "furniture-geode-chair";
    internal const string CrystalBench = "furniture-crystal-bench";
    internal const string CrystalBed = "furniture-crystal-bed";
    internal const string MineralShelf = "furniture-mineral-shelf";
    internal const string LapidaryCabinet = "furniture-lapidary-cabinet";
    internal const string GeoDesk = "furniture-geo-desk";
    internal const string GeodePedestal = "furniture-geode-pedestal";
    internal const string CrystalDivider = "furniture-crystal-divider";
    internal const string CrystalThrone = "furniture-crystal-throne";

    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, string modelId)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Furniture model id is required.", nameof(modelId));

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on furniture host '{prefab.name}'.");

        var root = new GameObject("magenheim." + modelId + ".visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);
        if (modelId == GeodeChair || modelId == CrystalThrone)
            root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var wood = Material(source, modelId + ".wood", new Color(.24f, .15f, .085f, 1f), 0f, .10f);
        var darkWood = Material(source, modelId + ".dark-wood", new Color(.13f, .075f, .045f, 1f), 0f, .08f);
        var stone = Material(source, modelId + ".stone", new Color(.33f, .32f, .30f, 1f), .02f, .09f);
        var darkStone = Material(source, modelId + ".dark-stone", new Color(.16f, .17f, .18f, 1f), .02f, .08f);
        var iron = Material(source, modelId + ".iron", new Color(.27f, .30f, .31f, 1f), .58f, .24f);
        var bronze = Material(source, modelId + ".bronze", new Color(.44f, .29f, .14f, 1f), .48f, .28f);
        var hide = Material(source, modelId + ".hide", new Color(.31f, .22f, .15f, 1f), 0f, .06f);
        var accent = AccentFor(modelId);
        var crystal = Material(source, modelId + ".crystal", accent, .02f, .72f, .32f);
        var bright = Material(source, modelId + ".crystal-bright", Color.Lerp(accent, Color.white, .30f), .01f, .82f, .52f);

        switch (modelId)
        {
            case GeodeTable: BuildTable(root, wood, darkStone, iron, crystal, bright); break;
            case GeodeChair: BuildChair(root, wood, stone, iron, crystal); break;
            case CrystalBench: BuildBench(root, wood, darkStone, iron, crystal); break;
            case CrystalBed: BuildBed(root, wood, darkWood, stone, iron, hide, crystal); break;
            case MineralShelf: BuildShelf(root, darkWood, stone, iron, crystal, bright); break;
            case LapidaryCabinet: BuildCabinet(root, wood, darkWood, stone, iron, bronze, crystal); break;
            case GeoDesk: BuildDesk(root, wood, darkStone, iron, bronze, crystal, bright); break;
            case GeodePedestal: BuildPedestal(root, stone, darkStone, iron, crystal, bright); break;
            case CrystalDivider: BuildDivider(root, wood, stone, iron, crystal, bright); break;
            case CrystalThrone: BuildThrone(root, darkWood, stone, darkStone, iron, bronze, crystal, bright); break;
            default: throw new InvalidOperationException($"Unknown Magenheim furniture model '{modelId}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static Color AccentFor(string modelId) => modelId switch
    {
        GeodeTable => new Color(.72f, .48f, .20f, 1f),
        GeodeChair => new Color(.44f, .76f, .88f, 1f),
        CrystalBench => new Color(.50f, .82f, .90f, 1f),
        CrystalBed => new Color(.54f, .72f, .92f, 1f),
        MineralShelf => new Color(.62f, .86f, .72f, 1f),
        LapidaryCabinet => new Color(.74f, .55f, .25f, 1f),
        GeoDesk => new Color(.64f, .46f, .80f, 1f),
        GeodePedestal => new Color(.78f, .52f, .23f, 1f),
        CrystalDivider => new Color(.48f, .82f, .88f, 1f),
        CrystalThrone => new Color(.82f, .68f, .31f, 1f),
        _ => new Color(.50f, .82f, .92f, 1f),
    };

    private static void BuildTable(GameObject root, Material wood, Material stone, Material iron, Material crystal, Material bright)
    {
        Box(root, "slab", new Vector3(0f, .86f, 0f), new Vector3(1.85f, .16f, .92f), stone);
        Box(root, "front-apron", new Vector3(0f, .71f, -.37f), new Vector3(1.58f, .13f, .10f), wood);
        Box(root, "rear-apron", new Vector3(0f, .71f, .37f), new Vector3(1.58f, .13f, .10f), wood);
        foreach (var x in new[] { -.72f, .72f })
            foreach (var z in new[] { -.31f, .31f })
                Box(root, "leg-" + x + "-" + z, new Vector3(x, .42f, z), new Vector3(.15f, .78f, .15f), wood);
        Box(root, "iron-inlay-a", new Vector3(0f, .955f, -.20f), new Vector3(1.38f, .024f, .035f), iron);
        Box(root, "iron-inlay-b", new Vector3(0f, .955f, .20f), new Vector3(1.38f, .024f, .035f), iron);
        MiniGeode(root, new Vector3(0f, .99f, 0f), .34f, stone, crystal, bright);
    }

    private static void BuildChair(GameObject root, Material wood, Material stone, Material iron, Material crystal)
    {
        Box(root, "seat", new Vector3(0f, .51f, 0f), new Vector3(.62f, .15f, .62f), stone);
        foreach (var x in new[] { -.23f, .23f })
            foreach (var z in new[] { -.23f, .23f })
                Box(root, "leg-" + x + "-" + z, new Vector3(x, .26f, z), new Vector3(.11f, .52f, .11f), wood);
        Box(root, "left-back", new Vector3(-.23f, 1.06f, .25f), new Vector3(.10f, 1.05f, .10f), wood);
        Box(root, "right-back", new Vector3(.23f, 1.06f, .25f), new Vector3(.10f, 1.05f, .10f), wood);
        Box(root, "back-rail", new Vector3(0f, 1.28f, .25f), new Vector3(.54f, .11f, .12f), iron);
        for (var i = -1; i <= 1; i++)
            Prism(root, "back-crystal-" + i, new Vector3(i * .16f, 1.38f + .04f * Math.Abs(i), .23f), new Vector3(.10f, .34f, .10f), 5, crystal);
    }

    private static void BuildBench(GameObject root, Material wood, Material stone, Material iron, Material crystal)
    {
        Box(root, "seat", new Vector3(0f, .48f, 0f), new Vector3(1.55f, .17f, .48f), stone);
        foreach (var x in new[] { -.61f, .61f })
        {
            Box(root, "leg-" + x, new Vector3(x, .25f, 0f), new Vector3(.17f, .50f, .40f), wood);
            Box(root, "end-band-" + x, new Vector3(x, .59f, 0f), new Vector3(.10f, .08f, .52f), iron);
            Prism(root, "end-crystal-" + x, new Vector3(x, .77f, 0f), new Vector3(.10f, .34f, .10f), 6, crystal);
        }
        Box(root, "stretcher", new Vector3(0f, .20f, 0f), new Vector3(1.22f, .11f, .12f), wood);
    }

    private static void BuildBed(GameObject root, Material wood, Material darkWood, Material stone, Material iron, Material hide, Material crystal)
    {
        Box(root, "frame", new Vector3(0f, .34f, 0f), new Vector3(1.02f, .18f, 2.05f), wood);
        Box(root, "mattress", new Vector3(0f, .49f, -.05f), new Vector3(.88f, .18f, 1.78f), hide);
        Box(root, "headboard", new Vector3(0f, .93f, .90f), new Vector3(1.02f, .92f, .14f), darkWood);
        Box(root, "head-stone", new Vector3(0f, .95f, .82f), new Vector3(.74f, .56f, .10f), stone);
        Box(root, "head-band", new Vector3(0f, 1.20f, .79f), new Vector3(.86f, .07f, .08f), iron);
        foreach (var x in new[] { -.42f, .42f })
        {
            Box(root, "post-" + x, new Vector3(x, .65f, .86f), new Vector3(.11f, 1.18f, .11f), wood);
            Prism(root, "finial-" + x, new Vector3(x, 1.33f, .86f), new Vector3(.12f, .34f, .12f), 5, crystal);
        }
    }

    private static void BuildShelf(GameObject root, Material wood, Material stone, Material iron, Material crystal, Material bright)
    {
        foreach (var x in new[] { -.56f, .56f })
            Box(root, "post-" + x, new Vector3(x, .96f, 0f), new Vector3(.12f, 1.90f, .18f), wood);
        foreach (var y in new[] { .25f, .86f, 1.47f })
        {
            Box(root, "shelf-" + y, new Vector3(0f, y, 0f), new Vector3(1.26f, .11f, .48f), stone);
            Box(root, "shelf-band-" + y, new Vector3(0f, y + .07f, -.20f), new Vector3(1.12f, .035f, .035f), iron);
        }
        Prism(root, "specimen-a", new Vector3(-.30f, .48f, 0f), new Vector3(.12f, .36f, .12f), 5, crystal, new Vector3(5f, 0f, -8f));
        Prism(root, "specimen-b", new Vector3(.23f, 1.08f, 0f), new Vector3(.10f, .28f, .10f), 6, bright, new Vector3(-4f, 15f, 6f));
        MiniGeode(root, new Vector3(.30f, 1.68f, 0f), .22f, stone, crystal, bright);
    }

    private static void BuildCabinet(GameObject root, Material wood, Material darkWood, Material stone, Material iron, Material bronze, Material crystal)
    {
        Box(root, "body", new Vector3(0f, .58f, 0f), new Vector3(1.26f, 1.08f, .62f), darkWood);
        Box(root, "top", new Vector3(0f, 1.16f, 0f), new Vector3(1.40f, .13f, .70f), stone);
        Box(root, "left-door", new Vector3(-.31f, .61f, -.326f), new Vector3(.55f, .82f, .055f), wood);
        Box(root, "right-door", new Vector3(.31f, .61f, -.326f), new Vector3(.55f, .82f, .055f), wood);
        Box(root, "door-band-l", new Vector3(-.31f, .61f, -.36f), new Vector3(.035f, .72f, .035f), iron);
        Box(root, "door-band-r", new Vector3(.31f, .61f, -.36f), new Vector3(.035f, .72f, .035f), iron);
        Cylinder(root, "handle-l", new Vector3(-.08f, .61f, -.39f), new Vector3(.055f, .10f, .055f), 10, bronze, new Vector3(90f, 0f, 0f));
        Cylinder(root, "handle-r", new Vector3(.08f, .61f, -.39f), new Vector3(.055f, .10f, .055f), 10, bronze, new Vector3(90f, 0f, 0f));
        Prism(root, "cabinet-crystal", new Vector3(0f, 1.34f, 0f), new Vector3(.13f, .32f, .13f), 6, crystal);
    }

    private static void BuildDesk(GameObject root, Material wood, Material stone, Material iron, Material bronze, Material crystal, Material bright)
    {
        Box(root, "top", new Vector3(0f, .79f, 0f), new Vector3(1.55f, .14f, .70f), stone);
        foreach (var x in new[] { -.61f, .61f })
            Box(root, "leg-" + x, new Vector3(x, .38f, .22f), new Vector3(.15f, .76f, .15f), wood);
        Box(root, "rear-rail", new Vector3(0f, .47f, .28f), new Vector3(1.20f, .13f, .10f), wood);
        Box(root, "drawer", new Vector3(.40f, .67f, -.31f), new Vector3(.50f, .20f, .14f), wood);
        Cylinder(root, "drawer-pull", new Vector3(.40f, .67f, -.40f), new Vector3(.045f, .09f, .045f), 10, bronze, new Vector3(90f, 0f, 0f));
        Box(root, "inlay", new Vector3(-.28f, .872f, 0f), new Vector3(.55f, .018f, .32f), iron);
        Prism(root, "scribe-crystal", new Vector3(-.28f, .99f, 0f), new Vector3(.08f, .26f, .08f), 5, crystal, new Vector3(0f, 0f, -12f));
        Prism(root, "scribe-tip", new Vector3(-.28f, 1.12f, 0f), new Vector3(.045f, .12f, .045f), 5, bright, new Vector3(0f, 0f, -12f));
    }

    private static void BuildPedestal(GameObject root, Material stone, Material darkStone, Material iron, Material crystal, Material bright)
    {
        Cylinder(root, "base", new Vector3(0f, .12f, 0f), new Vector3(.70f, .24f, .70f), 8, darkStone);
        Cylinder(root, "column", new Vector3(0f, .55f, 0f), new Vector3(.34f, .76f, .34f), 7, stone);
        Cylinder(root, "cap", new Vector3(0f, .96f, 0f), new Vector3(.58f, .14f, .58f), 8, iron);
        MiniGeode(root, new Vector3(0f, 1.18f, 0f), .42f, stone, crystal, bright);
    }

    private static void BuildDivider(GameObject root, Material wood, Material stone, Material iron, Material crystal, Material bright)
    {
        foreach (var x in new[] { -.78f, -.26f, .26f, .78f })
        {
            Box(root, "post-" + x, new Vector3(x, .96f, 0f), new Vector3(.10f, 1.92f, .12f), wood);
            Prism(root, "post-crystal-" + x, new Vector3(x, 1.66f, 0f), new Vector3(.12f, .42f, .12f), 5, crystal, new Vector3(0f, 0f, x * 6f));
        }
        Box(root, "base", new Vector3(0f, .10f, 0f), new Vector3(1.72f, .20f, .28f), stone);
        Box(root, "top", new Vector3(0f, 1.92f, 0f), new Vector3(1.72f, .12f, .18f), iron);
        Box(root, "mid", new Vector3(0f, .92f, 0f), new Vector3(1.55f, .06f, .10f), iron);
        Prism(root, "center-gem", new Vector3(0f, 1.18f, 0f), new Vector3(.15f, .48f, .15f), 6, bright);
    }

    private static void BuildThrone(GameObject root, Material wood, Material stone, Material darkStone, Material iron, Material bronze, Material crystal, Material bright)
    {
        Box(root, "seat", new Vector3(0f, .56f, 0f), new Vector3(.88f, .19f, .78f), stone);
        Box(root, "back", new Vector3(0f, 1.34f, .31f), new Vector3(.86f, 1.55f, .18f), darkStone);
        foreach (var x in new[] { -.42f, .42f })
        {
            Box(root, "arm-" + x, new Vector3(x, .84f, 0f), new Vector3(.15f, .20f, .74f), wood);
            Box(root, "pillar-" + x, new Vector3(x, 1.34f, .31f), new Vector3(.14f, 1.68f, .14f), wood);
            Prism(root, "crown-crystal-" + x, new Vector3(x, 2.17f, .31f), new Vector3(.15f, .44f, .15f), 5, crystal, new Vector3(0f, 0f, x * 12f));
        }
        Box(root, "back-band-a", new Vector3(0f, 1.18f, .20f), new Vector3(.62f, .06f, .06f), iron);
        Box(root, "back-band-b", new Vector3(0f, 1.68f, .20f), new Vector3(.62f, .06f, .06f), bronze);
        Prism(root, "heart", new Vector3(0f, 1.52f, .18f), new Vector3(.18f, .52f, .14f), 6, bright);
        Cylinder(root, "base", new Vector3(0f, .14f, 0f), new Vector3(1.05f, .28f, .95f), 8, darkStone);
    }

    private static void MiniGeode(GameObject root, Vector3 position, float size, Material stone, Material crystal, Material bright)
    {
        Prism(root, "geode-shell", position, new Vector3(size, size * .72f, size), 10, stone, new Vector3(0f, 18f, 0f));
        for (var i = 0; i < 5; i++)
        {
            var angle = i * Mathf.PI * 2f / 5f;
            var offset = new Vector3(Mathf.Cos(angle) * size * .20f, size * .22f, Mathf.Sin(angle) * size * .20f);
            Prism(root, "geode-crystal-" + i, position + offset, new Vector3(size * .15f, size * .52f, size * .15f), 5, i == 0 ? bright : crystal,
                new Vector3(8f * Mathf.Sin(angle), i * 17f, -8f * Mathf.Cos(angle)));
        }
    }

    private static void Box(GameObject root, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null) =>
        Add(root, name, BoxMesh, position, scale, Quaternion.Euler(euler ?? Vector3.zero), material);

    private static void Cylinder(GameObject root, string name, Vector3 position, Vector3 scale, int sides, Material material, Vector3? euler = null)
    {
        if (!CylinderMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = CreateCylinderMesh(sides);
            CylinderMeshes.Add(sides, mesh);
        }
        Add(root, name, mesh, position, scale, Quaternion.Euler(euler ?? Vector3.zero), material);
    }

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
        var material = new Material(source) { name = "magenheim.furniture." + suffix };
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

    private static Mesh CreateBoxMesh()
    {
        var mesh = new Mesh { name = "magenheim.furniture.box" };
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

    private static Mesh CreateCylinderMesh(int sides)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (var ring = 0; ring < 2; ring++)
        {
            var y = ring == 0 ? -.5f : .5f;
            for (var i = 0; i < sides; i++)
            {
                var angle = Mathf.PI * 2f * i / sides;
                vertices.Add(new Vector3(.5f * Mathf.Cos(angle), y, .5f * Mathf.Sin(angle)));
            }
        }
        var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.5f, 0f));
        var top = vertices.Count; vertices.Add(new Vector3(0f, .5f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[]
            {
                i, sides + i, next,
                next, sides + i, sides + next,
                bottom, i, next,
                top, sides + next, sides + i
            });
        }
        var mesh = new Mesh { name = "magenheim.furniture.cylinder." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh CreatePrismMesh(int sides)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (var i = 0; i < sides; i++)
        {
            var angle = Mathf.PI * 2f * i / sides;
            vertices.Add(new Vector3(.40f * Mathf.Cos(angle), -.48f, .40f * Mathf.Sin(angle)));
        }
        for (var i = 0; i < sides; i++)
        {
            var angle = Mathf.PI * 2f * i / sides;
            vertices.Add(new Vector3(.50f * Mathf.Cos(angle), .20f, .50f * Mathf.Sin(angle)));
        }
        var top = vertices.Count; vertices.Add(new Vector3(0f, .66f, 0f));
        var bottom = vertices.Count; vertices.Add(new Vector3(0f, -.50f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[]
            {
                bottom, i, next,
                i, sides + i, next,
                next, sides + i, sides + next,
                top, sides + next, sides + i
            });
        }
        var mesh = new Mesh { name = "magenheim.furniture.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
