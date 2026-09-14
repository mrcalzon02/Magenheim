using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Original procedural geometry for Magenheim's compact geology/crystal decor collection.
/// These models deliberately reuse the same stone, dark timber, iron/bronze binding, geode,
/// and faceted-crystal language as the workstation and primary furniture family.
/// </summary>
internal static class GeologyDecorVisuals
{
    internal const string GeodeBowl = "decor-geode-bowl";
    internal const string CutGeodePlaque = "decor-cut-geode-plaque";
    internal const string CrystalEndTable = "decor-crystal-end-table";
    internal const string GeologistStool = "decor-geologist-stool";
    internal const string MineralDisplayCase = "decor-mineral-display-case";
    internal const string CrystalWallSconce = "decor-crystal-wall-sconce";
    internal const string StrataMapTable = "decor-strata-map-table";
    internal const string SpecimenSideboard = "decor-specimen-sideboard";
    internal const string CrystalCoatRack = "decor-crystal-coat-rack";
    internal const string GeodeHearthMantel = "decor-geode-hearth-mantel";

    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, string modelId)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Decor model id is required.", nameof(modelId));

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on decor host '{prefab.name}'.");

        var root = new GameObject("magenheim." + modelId + ".visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);

        var wood = MakeMaterial(source, modelId + ".wood", new Color(.25f, .15f, .08f, 1f), 0f, .10f);
        var darkWood = MakeMaterial(source, modelId + ".darkwood", new Color(.12f, .07f, .04f, 1f), 0f, .08f);
        var stone = MakeMaterial(source, modelId + ".stone", new Color(.34f, .33f, .31f, 1f), .02f, .09f);
        var darkStone = MakeMaterial(source, modelId + ".darkstone", new Color(.15f, .16f, .17f, 1f), .02f, .07f);
        var warmStone = MakeMaterial(source, modelId + ".warmstone", new Color(.43f, .35f, .27f, 1f), .02f, .08f);
        var iron = MakeMaterial(source, modelId + ".iron", new Color(.27f, .30f, .31f, 1f), .58f, .24f);
        var bronze = MakeMaterial(source, modelId + ".bronze", new Color(.45f, .29f, .14f, 1f), .48f, .28f);
        var accent = AccentFor(modelId);
        var crystal = MakeMaterial(source, modelId + ".crystal", accent, .02f, .72f, .34f);
        var bright = MakeMaterial(source, modelId + ".crystalbright", Color.Lerp(accent, Color.white, .30f), .01f, .84f, .58f);
        var secondary = MakeMaterial(source, modelId + ".secondary", SecondaryFor(modelId), .01f, .60f, .18f);

        switch (modelId)
        {
            case GeodeBowl:
                BuildGeodeBowl(root, stone, darkStone, iron, crystal, bright);
                break;
            case CutGeodePlaque:
                BuildCutGeodePlaque(root, darkWood, stone, darkStone, iron, crystal, bright);
                break;
            case CrystalEndTable:
                BuildEndTable(root, wood, stone, darkStone, iron, crystal);
                break;
            case GeologistStool:
                BuildStool(root, wood, stone, iron, crystal);
                break;
            case MineralDisplayCase:
                BuildDisplayCase(root, darkWood, stone, iron, bronze, crystal, bright, secondary);
                break;
            case CrystalWallSconce:
                BuildWallSconce(root, darkStone, iron, bronze, crystal, bright);
                break;
            case StrataMapTable:
                BuildStrataMapTable(root, wood, stone, warmStone, darkStone, iron, bronze, crystal, secondary);
                break;
            case SpecimenSideboard:
                BuildSideboard(root, wood, darkWood, stone, iron, bronze, crystal, bright, secondary);
                break;
            case CrystalCoatRack:
                BuildCoatRack(root, wood, darkStone, iron, bronze, crystal);
                break;
            case GeodeHearthMantel:
                BuildHearthMantel(root, stone, darkStone, warmStone, iron, bronze, crystal, bright);
                break;
            default:
                throw new InvalidOperationException($"Unknown Magenheim geology decor model '{modelId}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static Color AccentFor(string modelId) => modelId switch
    {
        GeodeBowl => new Color(.70f, .48f, .24f, 1f),
        CutGeodePlaque => new Color(.53f, .82f, .90f, 1f),
        CrystalEndTable => new Color(.66f, .50f, .84f, 1f),
        GeologistStool => new Color(.56f, .80f, .70f, 1f),
        MineralDisplayCase => new Color(.54f, .82f, .88f, 1f),
        CrystalWallSconce => new Color(.78f, .88f, .96f, 1f),
        StrataMapTable => new Color(.78f, .58f, .29f, 1f),
        SpecimenSideboard => new Color(.62f, .84f, .73f, 1f),
        CrystalCoatRack => new Color(.48f, .75f, .88f, 1f),
        GeodeHearthMantel => new Color(.86f, .52f, .22f, 1f),
        _ => new Color(.52f, .82f, .90f, 1f),
    };

    private static Color SecondaryFor(string modelId) => modelId switch
    {
        MineralDisplayCase => new Color(.76f, .47f, .25f, 1f),
        StrataMapTable => new Color(.46f, .68f, .58f, 1f),
        SpecimenSideboard => new Color(.74f, .52f, .25f, 1f),
        _ => new Color(.62f, .48f, .72f, 1f),
    };

    private static void BuildGeodeBowl(GameObject root, Material stone, Material darkStone, Material iron, Material crystal, Material bright)
    {
        Cylinder(root, "foot", new Vector3(0f, .07f, 0f), new Vector3(.52f, .14f, .52f), 10, darkStone);
        Cylinder(root, "bowl-base", new Vector3(0f, .18f, 0f), new Vector3(.72f, .16f, .72f), 10, stone);
        for (var i = 0; i < 10; i++)
        {
            var a = Mathf.PI * 2f * i / 10f;
            var x = .34f * Mathf.Cos(a);
            var z = .34f * Mathf.Sin(a);
            Box(root, "rim-stone-" + i, new Vector3(x, .30f, z), new Vector3(.24f, .18f, .18f), stone,
                new Vector3(0f, -a * Mathf.Rad2Deg, 12f * Mathf.Sin(a)));
        }
        Cylinder(root, "iron-rim", new Vector3(0f, .34f, 0f), new Vector3(.78f, .045f, .78f), 12, iron);
        Prism(root, "cluster-center", new Vector3(0f, .42f, 0f), new Vector3(.18f, .44f, .18f), 6, bright);
        Prism(root, "cluster-left", new Vector3(-.16f, .38f, .06f), new Vector3(.12f, .31f, .12f), 5, crystal, new Vector3(4f, 0f, -13f));
        Prism(root, "cluster-right", new Vector3(.15f, .37f, -.05f), new Vector3(.11f, .28f, .11f), 5, crystal, new Vector3(-5f, 18f, 11f));
    }

    private static void BuildCutGeodePlaque(GameObject root, Material wood, Material stone, Material darkStone, Material iron, Material crystal, Material bright)
    {
        Box(root, "backing", new Vector3(0f, .72f, .02f), new Vector3(.92f, 1.18f, .10f), wood);
        Box(root, "top-band", new Vector3(0f, 1.27f, -.045f), new Vector3(.98f, .055f, .055f), iron);
        Box(root, "bottom-band", new Vector3(0f, .17f, -.045f), new Vector3(.98f, .055f, .055f), iron);
        for (var i = 0; i < 9; i++)
        {
            var angle = 140f + i * 32.5f;
            var rad = angle * Mathf.Deg2Rad;
            var x = .31f * Mathf.Cos(rad);
            var y = .74f + .31f * Mathf.Sin(rad);
            Box(root, "shell-" + i, new Vector3(x, y, -.10f), new Vector3(.22f, .18f, .14f), stone,
                new Vector3(0f, 0f, angle + 90f));
        }
        Cylinder(root, "geode-shadow", new Vector3(0f, .74f, -.075f), new Vector3(.51f, .045f, .51f), 12, darkStone, new Vector3(90f, 0f, 0f));
        for (var i = -2; i <= 2; i++)
            Prism(root, "interior-" + i, new Vector3(i * .10f, .75f + .025f * Math.Abs(i), -.18f),
                new Vector3(.10f, .30f - .025f * Math.Abs(i), .10f), 5, i == 0 ? bright : crystal,
                new Vector3(90f, 0f, i * 8f));
    }

    private static void BuildEndTable(GameObject root, Material wood, Material stone, Material darkStone, Material iron, Material crystal)
    {
        Cylinder(root, "top", new Vector3(0f, .66f, 0f), new Vector3(.78f, .14f, .78f), 8, stone);
        Cylinder(root, "top-band", new Vector3(0f, .61f, 0f), new Vector3(.82f, .055f, .82f), 8, iron);
        Cylinder(root, "pedestal", new Vector3(0f, .34f, 0f), new Vector3(.22f, .55f, .22f), 8, wood);
        Cylinder(root, "base", new Vector3(0f, .08f, 0f), new Vector3(.58f, .16f, .58f), 8, darkStone);
        for (var i = 0; i < 4; i++)
        {
            var angle = Mathf.PI * 2f * i / 4f;
            Prism(root, "under-crystal-" + i,
                new Vector3(.24f * Mathf.Cos(angle), .49f, .24f * Mathf.Sin(angle)),
                new Vector3(.08f, .22f, .08f), 5, crystal, new Vector3(0f, -angle * Mathf.Rad2Deg, 18f));
        }
    }

    private static void BuildStool(GameObject root, Material wood, Material stone, Material iron, Material crystal)
    {
        Cylinder(root, "seat", new Vector3(0f, .58f, 0f), new Vector3(.62f, .16f, .62f), 10, stone);
        Cylinder(root, "seat-band", new Vector3(0f, .52f, 0f), new Vector3(.66f, .05f, .66f), 10, iron);
        for (var i = 0; i < 3; i++)
        {
            var angle = Mathf.PI * 2f * i / 3f;
            Box(root, "leg-" + i,
                new Vector3(.19f * Mathf.Cos(angle), .27f, .19f * Mathf.Sin(angle)),
                new Vector3(.11f, .52f, .11f), wood,
                new Vector3(5f * Mathf.Sin(angle), -angle * Mathf.Rad2Deg, -5f * Mathf.Cos(angle)));
        }
        Prism(root, "underslung-crystal", new Vector3(0f, .38f, 0f), new Vector3(.11f, .25f, .11f), 6, crystal, new Vector3(180f, 0f, 0f));
    }

    private static void BuildDisplayCase(GameObject root, Material wood, Material stone, Material iron, Material bronze, Material crystal, Material bright, Material secondary)
    {
        Box(root, "base", new Vector3(0f, .11f, 0f), new Vector3(1.32f, .22f, .62f), stone);
        Box(root, "top", new Vector3(0f, 1.46f, 0f), new Vector3(1.32f, .13f, .62f), stone);
        foreach (var x in new[] { -.57f, .57f })
            foreach (var z in new[] { -.25f, .25f })
                Box(root, "post-" + x + "-" + z, new Vector3(x, .80f, z), new Vector3(.075f, 1.28f, .075f), iron);
        foreach (var y in new[] { .49f, .95f })
            Box(root, "shelf-" + y, new Vector3(0f, y, 0f), new Vector3(1.12f, .055f, .50f), wood);
        Box(root, "lower-trim", new Vector3(0f, .26f, -.30f), new Vector3(1.18f, .055f, .055f), bronze);
        Box(root, "upper-trim", new Vector3(0f, 1.31f, -.30f), new Vector3(1.18f, .055f, .055f), bronze);
        Prism(root, "specimen-a", new Vector3(-.33f, .69f, 0f), new Vector3(.12f, .33f, .12f), 5, crystal, new Vector3(4f, 10f, -8f));
        Prism(root, "specimen-b", new Vector3(.12f, .68f, 0f), new Vector3(.10f, .29f, .10f), 6, secondary, new Vector3(-5f, -12f, 7f));
        Prism(root, "specimen-c", new Vector3(.34f, 1.17f, 0f), new Vector3(.13f, .35f, .13f), 5, bright, new Vector3(3f, 7f, 10f));
        MiniGeode(root, new Vector3(-.25f, 1.15f, 0f), .22f, stone, crystal, bright);
    }

    private static void BuildWallSconce(GameObject root, Material stone, Material iron, Material bronze, Material crystal, Material bright)
    {
        Box(root, "wall-plate", new Vector3(0f, .68f, .03f), new Vector3(.38f, .72f, .10f), stone);
        Box(root, "vertical-band", new Vector3(0f, .68f, -.045f), new Vector3(.075f, .62f, .055f), iron);
        Box(root, "bracket", new Vector3(0f, .56f, -.24f), new Vector3(.09f, .09f, .42f), bronze, new Vector3(-12f, 0f, 0f));
        Cylinder(root, "socket", new Vector3(0f, .55f, -.43f), new Vector3(.19f, .10f, .19f), 10, iron, new Vector3(90f, 0f, 0f));
        Prism(root, "lamp-crystal", new Vector3(0f, .72f, -.48f), new Vector3(.19f, .47f, .19f), 6, crystal, new Vector3(7f, 0f, 0f));
        Prism(root, "lamp-tip", new Vector3(0f, .94f, -.50f), new Vector3(.11f, .24f, .11f), 5, bright, new Vector3(5f, 14f, 0f));
    }

    private static void BuildStrataMapTable(GameObject root, Material wood, Material stone, Material warmStone, Material darkStone, Material iron, Material bronze, Material crystal, Material secondary)
    {
        Box(root, "top", new Vector3(0f, .84f, 0f), new Vector3(1.72f, .15f, 1.00f), stone);
        foreach (var x in new[] { -.68f, .68f })
            foreach (var z in new[] { -.34f, .34f })
                Box(root, "leg-" + x + "-" + z, new Vector3(x, .40f, z), new Vector3(.14f, .80f, .14f), wood);
        Box(root, "edge-band-front", new Vector3(0f, .78f, -.47f), new Vector3(1.55f, .075f, .055f), iron);
        Box(root, "edge-band-back", new Vector3(0f, .78f, .47f), new Vector3(1.55f, .075f, .055f), iron);
        var layers = new[] { darkStone, warmStone, secondary, warmStone, darkStone };
        for (var i = 0; i < layers.Length; i++)
            Box(root, "strata-" + i, new Vector3(-.28f + i * .14f, .923f, -.05f + .035f * (i % 2)), new Vector3(.11f, .018f, .66f), layers[i], new Vector3(0f, 8f * (i - 2), 0f));
        Cylinder(root, "compass-ring", new Vector3(.52f, .94f, .20f), new Vector3(.28f, .025f, .28f), 12, bronze);
        Prism(root, "compass-point", new Vector3(.52f, .985f, .20f), new Vector3(.07f, .19f, .07f), 5, crystal, new Vector3(0f, 0f, 90f));
    }

    private static void BuildSideboard(GameObject root, Material wood, Material darkWood, Material stone, Material iron, Material bronze, Material crystal, Material bright, Material secondary)
    {
        Box(root, "body", new Vector3(0f, .49f, 0f), new Vector3(1.70f, .82f, .62f), darkWood);
        Box(root, "top", new Vector3(0f, .95f, 0f), new Vector3(1.82f, .13f, .70f), stone);
        for (var i = -1; i <= 1; i++)
        {
            Box(root, "door-" + i, new Vector3(i * .50f, .50f, -.33f), new Vector3(.45f, .60f, .055f), wood);
            Box(root, "door-band-" + i, new Vector3(i * .50f, .50f, -.37f), new Vector3(.035f, .52f, .035f), iron);
            Cylinder(root, "pull-" + i, new Vector3(i * .50f + .13f, .50f, -.41f), new Vector3(.045f, .08f, .045f), 8, bronze, new Vector3(90f, 0f, 0f));
        }
        Prism(root, "top-specimen-left", new Vector3(-.48f, 1.17f, 0f), new Vector3(.11f, .34f, .11f), 5, crystal, new Vector3(4f, 0f, -7f));
        Prism(root, "top-specimen-mid", new Vector3(0f, 1.14f, 0f), new Vector3(.10f, .28f, .10f), 6, secondary, new Vector3(-3f, 12f, 4f));
        MiniGeode(root, new Vector3(.46f, 1.08f, 0f), .24f, stone, bright, crystal);
    }

    private static void BuildCoatRack(GameObject root, Material wood, Material stone, Material iron, Material bronze, Material crystal)
    {
        Cylinder(root, "base", new Vector3(0f, .10f, 0f), new Vector3(.62f, .20f, .62f), 10, stone);
        Cylinder(root, "shaft", new Vector3(0f, .94f, 0f), new Vector3(.15f, 1.78f, .15f), 8, wood);
        Cylinder(root, "lower-collar", new Vector3(0f, .45f, 0f), new Vector3(.22f, .075f, .22f), 10, iron);
        Cylinder(root, "upper-collar", new Vector3(0f, 1.50f, 0f), new Vector3(.23f, .075f, .23f), 10, bronze);
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.PI * 2f * i / 6f;
            var x = .27f * Mathf.Cos(angle);
            var z = .27f * Mathf.Sin(angle);
            Box(root, "hook-" + i, new Vector3(x, 1.56f, z), new Vector3(.075f, .075f, .52f), iron,
                new Vector3(0f, -angle * Mathf.Rad2Deg + 90f, -18f));
            Prism(root, "hook-crystal-" + i, new Vector3(.44f * Mathf.Cos(angle), 1.66f, .44f * Mathf.Sin(angle)),
                new Vector3(.07f, .20f, .07f), 5, crystal, new Vector3(0f, -angle * Mathf.Rad2Deg, 16f));
        }
        Prism(root, "crown", new Vector3(0f, 1.92f, 0f), new Vector3(.12f, .36f, .12f), 6, crystal);
    }

    private static void BuildHearthMantel(GameObject root, Material stone, Material darkStone, Material warmStone, Material iron, Material bronze, Material crystal, Material bright)
    {
        foreach (var x in new[] { -.72f, .72f })
        {
            Box(root, "pillar-" + x, new Vector3(x, .72f, 0f), new Vector3(.36f, 1.44f, .42f), stone);
            Box(root, "pillar-foot-" + x, new Vector3(x, .11f, 0f), new Vector3(.52f, .22f, .54f), darkStone);
            Box(root, "pillar-band-" + x, new Vector3(x, .78f, -.235f), new Vector3(.28f, .075f, .055f), iron);
            MiniGeode(root, new Vector3(x, 1.16f, -.24f), .18f, stone, crystal, bright);
        }
        Box(root, "lintel", new Vector3(0f, 1.46f, 0f), new Vector3(1.82f, .32f, .46f), warmStone);
        Box(root, "mantel-shelf", new Vector3(0f, 1.72f, -.02f), new Vector3(2.02f, .16f, .58f), stone);
        Box(root, "lintel-band", new Vector3(0f, 1.50f, -.25f), new Vector3(1.45f, .065f, .055f), bronze);
        for (var i = -2; i <= 2; i++)
            Prism(root, "seam-crystal-" + i, new Vector3(i * .19f, 1.58f, -.27f), new Vector3(.07f, .20f + .025f * (2 - Math.Abs(i)), .07f), 5,
                i == 0 ? bright : crystal, new Vector3(90f, 0f, i * 7f));
    }

    private static void MiniGeode(GameObject root, Vector3 position, float radius, Material shell, Material crystal, Material bright)
    {
        for (var i = 0; i < 7; i++)
        {
            var angle = Mathf.PI * 2f * i / 7f;
            Box(root, "geode-shell-" + position + "-" + i,
                position + new Vector3(radius * .70f * Mathf.Cos(angle), 0f, radius * .70f * Mathf.Sin(angle)),
                new Vector3(radius * .62f, radius * .46f, radius * .50f), shell,
                new Vector3(0f, -angle * Mathf.Rad2Deg, 7f * Mathf.Sin(angle)));
        }
        Prism(root, "geode-heart-" + position, position + new Vector3(0f, radius * .22f, 0f),
            new Vector3(radius * .55f, radius * 1.10f, radius * .55f), 6, bright);
        Prism(root, "geode-heart-side-" + position, position + new Vector3(radius * .28f, radius * .12f, -.03f),
            new Vector3(radius * .35f, radius * .72f, radius * .35f), 5, crystal, new Vector3(5f, 12f, 8f));
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

    private static Material MakeMaterial(Material source, string suffix, Color color, float metallic, float glossiness, float emission = 0f)
    {
        var material = new Material(source)
        {
            name = "magenheim.geology-decor." + suffix,
            mainTexture = Texture2D.whiteTexture,
        };
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
        var mesh = new Mesh { name = "magenheim.geology-decor.box" };
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
            triangles.AddRange(new[] { i,next,sides+i, next,sides+next,sides+i, bottom,next,i, top,sides+i,sides+next });
        }
        var mesh = new Mesh
        {
            name = "magenheim.geology-decor.cylinder." + sides,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };
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
            triangles.AddRange(new[] { bottom,next,i, i,next,sides+i, next,sides+next,sides+i, sides+i,sides+next,top });
        }
        var mesh = new Mesh
        {
            name = "magenheim.geology-decor.prism." + sides,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
