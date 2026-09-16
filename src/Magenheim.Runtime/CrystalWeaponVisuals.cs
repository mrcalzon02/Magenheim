using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Original procedural geometry for the ten-piece physical crystal weapon family.</summary>
internal static class CrystalWeaponVisuals
{
    internal const string Sword = "crystal-weapon-sword";
    internal const string Greatsword = "crystal-weapon-greatsword";
    internal const string Axe = "crystal-weapon-axe";
    internal const string Battleaxe = "crystal-weapon-battleaxe";
    internal const string Mace = "crystal-weapon-mace";
    internal const string Spear = "crystal-weapon-spear";
    internal const string Knife = "crystal-weapon-knife";
    internal const string Atgeir = "crystal-weapon-atgeir";
    internal const string Bow = "crystal-weapon-bow";
    internal const string Crossbow = "crystal-weapon-crossbow";

    private static readonly Mesh BoxMesh = RuntimeMeshPrimitives.Box("magenheim.crystal-weapon.box");
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, string modelId)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Weapon model id is required.", nameof(modelId));

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on crystal weapon host '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + modelId + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);

        var grip = Material(source, modelId + ".grip", new Color(.13f, .08f, .045f, 1f), 0f, .10f);
        var blackMetal = Material(source, modelId + ".blackmetal", new Color(.16f, .19f, .21f, 1f), .68f, .34f);
        var silver = Material(source, modelId + ".silver", new Color(.56f, .65f, .72f, 1f), .58f, .42f);
        var crystal = Material(source, modelId + ".crystal", new Color(.55f, .86f, .94f, 1f), .03f, .82f, .30f);
        var crystalBright = Material(source, modelId + ".crystal-bright", new Color(.84f, .97f, 1f, 1f), .02f, .92f, .50f);
        var rainbow = RainbowMaterials(source, modelId);

        switch (modelId)
        {
            case Sword: BuildSword(root, grip, blackMetal, crystal, crystalBright, rainbow); break;
            case Greatsword: BuildGreatsword(root, grip, blackMetal, silver, crystal, crystalBright, rainbow); break;
            case Axe: BuildAxe(root, grip, blackMetal, crystal, crystalBright, rainbow); break;
            case Battleaxe: BuildBattleaxe(root, grip, blackMetal, silver, crystal, crystalBright, rainbow); break;
            case Mace: BuildMace(root, grip, blackMetal, crystal, crystalBright, rainbow); break;
            case Spear: BuildSpear(root, grip, blackMetal, crystal, crystalBright, rainbow); break;
            case Knife: BuildKnife(root, grip, blackMetal, crystal, crystalBright, rainbow); break;
            case Atgeir: BuildAtgeir(root, grip, blackMetal, silver, crystal, crystalBright, rainbow); break;
            case Bow: BuildBow(root, grip, blackMetal, crystal, crystalBright, rainbow); break;
            case Crossbow: BuildCrossbow(root, grip, blackMetal, silver, crystal, crystalBright, rainbow); break;
            default: throw new InvalidOperationException($"Unknown crystal weapon model '{modelId}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static void BuildSword(GameObject root, Material grip, Material metal, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "grip", new Vector3(0f, -.38f, 0f), .045f, .55f, 10, grip);
        Box(root, "guard", new Vector3(0f, -.08f, 0f), new Vector3(.42f, .055f, .075f), metal);
        Cylinder(root, "pommel", new Vector3(0f, -.69f, 0f), .07f, .12f, 8, metal);
        Prism(root, "blade", new Vector3(0f, .52f, 0f), .105f, 1.26f, 6, crystal);
        Prism(root, "blade-core", new Vector3(0f, .48f, 0f), .035f, 1.05f, 6, bright);
        Box(root, "rainbow-inlay", new Vector3(0f, .05f, -.07f), new Vector3(.055f, .24f, .025f), rainbow[4]);
    }

    private static void BuildGreatsword(GameObject root, Material grip, Material metal, Material silver, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "twohand-grip", new Vector3(0f, -.50f, 0f), .055f, .82f, 10, grip);
        Box(root, "guard", new Vector3(0f, -.06f, 0f), new Vector3(.72f, .075f, .10f), metal);
        Box(root, "guard-silver", new Vector3(0f, -.02f, 0f), new Vector3(.48f, .035f, .12f), silver);
        Prism(root, "great-blade", new Vector3(0f, .77f, 0f), .18f, 1.68f, 6, crystal);
        Box(root, "spine", new Vector3(0f, .72f, .02f), new Vector3(.065f, 1.40f, .07f), metal);
        Prism(root, "tip-core", new Vector3(0f, 1.49f, 0f), .07f, .32f, 6, bright);
        Box(root, "left-prismatic-rib", new Vector3(-.13f, .66f, -.10f), new Vector3(.035f, 1.15f, .025f), rainbow[1]);
        Box(root, "right-prismatic-rib", new Vector3(.13f, .66f, -.10f), new Vector3(.035f, 1.15f, .025f), rainbow[6]);
    }

    private static void BuildAxe(GameObject root, Material grip, Material metal, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "haft", new Vector3(0f, -.05f, 0f), .042f, 1.35f, 10, grip);
        Cylinder(root, "lower-band", new Vector3(0f, -.58f, 0f), .055f, .10f, 10, metal);
        Cylinder(root, "head-band", new Vector3(0f, .52f, 0f), .07f, .15f, 10, metal);
        Box(root, "axe-head", new Vector3(.18f, .58f, 0f), new Vector3(.42f, .46f, .13f), crystal, new Vector3(0f, 0f, -8f));
        Prism(root, "edge", new Vector3(.36f, .60f, 0f), .08f, .52f, 5, bright, new Vector3(0f, 0f, -90f));
        Box(root, "head-inlay", new Vector3(.05f, .57f, -.08f), new Vector3(.18f, .055f, .025f), rainbow[2]);
    }

    private static void BuildBattleaxe(GameObject root, Material grip, Material metal, Material silver, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "long-haft", new Vector3(0f, -.15f, 0f), .052f, 1.85f, 10, grip);
        Cylinder(root, "band-low", new Vector3(0f, -.87f, 0f), .068f, .09f, 10, metal);
        Cylinder(root, "band-mid", new Vector3(0f, .48f, 0f), .068f, .09f, 10, metal);
        Cylinder(root, "band-high", new Vector3(0f, .65f, 0f), .068f, .09f, 10, metal);
        Box(root, "head-core", new Vector3(0f, .72f, 0f), new Vector3(.50f, .24f, .15f), silver);
        Box(root, "left-head", new Vector3(-.30f, .75f, 0f), new Vector3(.48f, .62f, .15f), crystal, new Vector3(0f, 0f, 11f));
        Box(root, "right-head", new Vector3(.30f, .75f, 0f), new Vector3(.48f, .62f, .15f), crystal, new Vector3(0f, 0f, -11f));
        Prism(root, "left-edge", new Vector3(-.52f, .76f, 0f), .085f, .66f, 5, bright, new Vector3(0f, 0f, 90f));
        Prism(root, "right-edge", new Vector3(.52f, .76f, 0f), .085f, .66f, 5, bright, new Vector3(0f, 0f, -90f));
        Box(root, "prismatic-eye", new Vector3(0f, .72f, -.10f), new Vector3(.22f, .08f, .035f), rainbow[5]);
    }

    private static void BuildMace(GameObject root, Material grip, Material metal, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "haft", new Vector3(0f, -.12f, 0f), .045f, 1.20f, 10, grip);
        Cylinder(root, "neck", new Vector3(0f, .45f, 0f), .075f, .18f, 10, metal);
        Prism(root, "head-core", new Vector3(0f, .68f, 0f), .17f, .40f, 8, crystal);
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.PI * 2f * i / 6f;
            var x = .21f * Mathf.Cos(angle);
            var z = .21f * Mathf.Sin(angle);
            Prism(root, "striker-" + i, new Vector3(x, .70f, z), .065f, .30f, 5,
                i % 2 == 0 ? bright : rainbow[i], new Vector3(70f, -angle * Mathf.Rad2Deg, 0f));
        }
        Cylinder(root, "pommel", new Vector3(0f, -.75f, 0f), .065f, .12f, 8, metal);
    }

    private static void BuildSpear(GameObject root, Material grip, Material metal, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "shaft", new Vector3(0f, -.20f, 0f), .032f, 1.85f, 10, grip);
        Cylinder(root, "socket", new Vector3(0f, .70f, 0f), .060f, .22f, 10, metal);
        Prism(root, "spearhead", new Vector3(0f, 1.04f, 0f), .105f, .70f, 6, crystal);
        Prism(root, "spear-tip", new Vector3(0f, 1.34f, 0f), .045f, .28f, 6, bright);
        Box(root, "prism-bind", new Vector3(0f, .76f, -.07f), new Vector3(.055f, .18f, .025f), rainbow[3]);
    }

    private static void BuildKnife(GameObject root, Material grip, Material metal, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "grip", new Vector3(0f, -.28f, 0f), .050f, .48f, 8, grip);
        Box(root, "guard", new Vector3(0f, -.02f, 0f), new Vector3(.24f, .045f, .065f), metal);
        Prism(root, "blade", new Vector3(.03f, .30f, 0f), .095f, .66f, 5, crystal, new Vector3(0f, 0f, -5f));
        Prism(root, "tip", new Vector3(.06f, .57f, 0f), .040f, .22f, 5, bright, new Vector3(0f, 0f, -5f));
        Box(root, "inlay", new Vector3(0f, -.26f, -.06f), new Vector3(.035f, .22f, .025f), rainbow[6]);
    }

    private static void BuildAtgeir(GameObject root, Material grip, Material metal, Material silver, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Cylinder(root, "pole", new Vector3(0f, -.18f, 0f), .038f, 2.15f, 10, grip);
        Cylinder(root, "socket", new Vector3(0f, .85f, 0f), .07f, .24f, 10, metal);
        Prism(root, "main-point", new Vector3(0f, 1.25f, 0f), .12f, .78f, 6, crystal);
        Prism(root, "tip", new Vector3(0f, 1.58f, 0f), .05f, .28f, 6, bright);
        Box(root, "left-wing", new Vector3(-.19f, .99f, 0f), new Vector3(.40f, .11f, .11f), crystal, new Vector3(0f, 0f, -25f));
        Box(root, "right-wing", new Vector3(.19f, .99f, 0f), new Vector3(.40f, .11f, .11f), crystal, new Vector3(0f, 0f, 25f));
        Box(root, "wing-core", new Vector3(0f, .98f, 0f), new Vector3(.48f, .055f, .13f), silver);
        Box(root, "rainbow-bind", new Vector3(0f, .88f, -.08f), new Vector3(.18f, .045f, .025f), rainbow[4]);
    }

    private static void BuildBow(GameObject root, Material grip, Material metal, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Box(root, "riser", new Vector3(0f, 0f, 0f), new Vector3(.10f, .52f, .10f), grip);
        Box(root, "grip-band", new Vector3(0f, 0f, -.07f), new Vector3(.12f, .18f, .035f), metal);
        BowLimb(root, "upper-inner", new Vector3(-.08f, .38f, 0f), -18f, crystal);
        BowLimb(root, "upper-mid", new Vector3(-.19f, .69f, 0f), -24f, rainbow[4]);
        BowLimb(root, "upper-outer", new Vector3(-.31f, .97f, 0f), -30f, rainbow[6]);
        BowLimb(root, "lower-inner", new Vector3(.08f, -.38f, 0f), 18f, crystal);
        BowLimb(root, "lower-mid", new Vector3(.19f, -.69f, 0f), 24f, rainbow[1]);
        BowLimb(root, "lower-outer", new Vector3(.31f, -.97f, 0f), 30f, rainbow[3]);
        Prism(root, "upper-cap", new Vector3(-.43f, 1.16f, 0f), .045f, .18f, 5, bright, new Vector3(0f, 0f, 35f));
        Prism(root, "lower-cap", new Vector3(.43f, -1.16f, 0f), .045f, .18f, 5, bright, new Vector3(0f, 0f, -35f));
        Box(root, "string-upper", new Vector3(-.22f, .58f, -.07f), new Vector3(.018f, 1.22f, .018f), metal, new Vector3(0f, 0f, -20f));
        Box(root, "string-lower", new Vector3(.22f, -.58f, -.07f), new Vector3(.018f, 1.22f, .018f), metal, new Vector3(0f, 0f, -20f));
    }

    private static void BowLimb(GameObject root, string name, Vector3 position, float angle, Material material) =>
        Box(root, name, position, new Vector3(.105f, .38f, .075f), material, new Vector3(0f, 0f, angle));

    private static void BuildCrossbow(GameObject root, Material grip, Material metal, Material silver, Material crystal, Material bright, IReadOnlyList<Material> rainbow)
    {
        Box(root, "stock", new Vector3(0f, -.05f, 0f), new Vector3(.18f, 1.20f, .18f), grip);
        Box(root, "stock-spine", new Vector3(0f, .02f, -.11f), new Vector3(.075f, 1.05f, .055f), metal);
        Box(root, "prod-core", new Vector3(0f, .46f, 0f), new Vector3(.95f, .10f, .12f), silver);
        Box(root, "left-limb", new Vector3(-.48f, .48f, 0f), new Vector3(.62f, .13f, .11f), crystal, new Vector3(0f, 0f, -8f));
        Box(root, "right-limb", new Vector3(.48f, .48f, 0f), new Vector3(.62f, .13f, .11f), crystal, new Vector3(0f, 0f, 8f));
        Prism(root, "left-tip", new Vector3(-.78f, .53f, 0f), .045f, .18f, 5, bright, new Vector3(0f, 0f, 90f));
        Prism(root, "right-tip", new Vector3(.78f, .53f, 0f), .045f, .18f, 5, bright, new Vector3(0f, 0f, 90f));
        Box(root, "bowstring", new Vector3(0f, .54f, -.08f), new Vector3(1.50f, .018f, .018f), metal);
        Box(root, "prism-track", new Vector3(0f, .18f, -.13f), new Vector3(.055f, .52f, .025f), rainbow[5]);
    }

    private static Material[] RainbowMaterials(Material source, string modelId)
    {
        var colors = new[]
        {
            new Color(1f,.18f,.12f,1f), new Color(1f,.56f,.10f,1f), new Color(1f,.90f,.18f,1f),
            new Color(.30f,.95f,.30f,1f), new Color(.14f,.88f,1f,1f), new Color(.30f,.40f,1f,1f),
            new Color(.78f,.22f,1f,1f)
        };
        var materials = new Material[colors.Length];
        for (var i = 0; i < colors.Length; i++)
            materials[i] = Material(source, modelId + ".rainbow." + i, colors[i], .02f, .82f, .34f);
        return materials;
    }

    private static void Box(GameObject root, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null) =>
        Add(root, name, BoxMesh, position, scale, Quaternion.Euler(euler ?? Vector3.zero), material);

    private static void Cylinder(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material)
    {
        if (!CylinderMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = RuntimeMeshPrimitives.Cylinder(sides, "magenheim.crystal-weapon.cylinder." + sides);
            CylinderMeshes.Add(sides, mesh);
        }
        Add(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);
    }

    private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3? euler = null)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = RuntimeMeshPrimitives.Prism(
                sides,
                "magenheim.crystal-weapon.prism." + sides,
                lowerRadius: .36f,
                lowerY: -.45f,
                upperRadius: .50f,
                upperY: .20f,
                apexY: .68f,
                baseY: -.50f);
            PrismMeshes.Add(sides, mesh);
        }
        Add(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.Euler(euler ?? Vector3.zero), material);
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
        var material = new Material(source) { name = "magenheim.crystal-weapon." + suffix };
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
