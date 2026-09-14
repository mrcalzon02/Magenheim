using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Original reusable Magenheim geometry for later physical systems. This file deliberately
/// contains models only: no ward, passage, ritual, ship, summon, or totem gameplay is created here.
/// </summary>
internal static class WorldArtifactVisuals
{
    internal const string CrystalWardstone = "crystal-wardstone";
    internal const string PassageStone = "passage-stone";
    internal const string RunedTotem = "runed-totem";
    internal const string RunicKeelstone = "runic-keelstone";
    internal const string CrystalLantern = "crystal-lantern";
    internal const string CrystalBrazier = "crystal-brazier";
    internal const string RuneEngraver = "rune-engraver";
    internal const string SeidrRitualFocus = "seidr-ritual-focus";
    internal const string SpiritFetishWolf = "spirit-fetish-wolf";
    internal const string SpiritFetishRaven = "spirit-fetish-raven";
    internal const string SpiritFetishWarrior = "spirit-fetish-warrior";

    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab, string modelId, bool itemModel = false, float scale = 1f, Color? accentOverride = null)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Model id is required.", nameof(modelId));
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale));

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on model host '{prefab.name}'.");
        var parent = itemModel ? prefab.transform.Find("attach") ?? prefab.transform : prefab.transform;
        var root = new GameObject("magenheim." + modelId + ".visual") { layer = prefab.layer };
        root.transform.SetParent(parent, false);
        root.transform.localScale = Vector3.one * scale;

        var stone = Material(source, modelId + ".stone", new Color(.31f, .30f, .285f, 1f), .02f, .10f);
        var darkStone = Material(source, modelId + ".dark-stone", new Color(.16f, .17f, .17f, 1f), .02f, .08f);
        var wood = Material(source, modelId + ".wood", new Color(.26f, .17f, .095f, 1f), 0f, .10f);
        var darkWood = Material(source, modelId + ".dark-wood", new Color(.15f, .095f, .055f, 1f), 0f, .08f);
        var iron = Material(source, modelId + ".iron", new Color(.27f, .30f, .31f, 1f), .58f, .24f);
        var bronze = Material(source, modelId + ".bronze", new Color(.42f, .29f, .15f, 1f), .48f, .28f);
        var bone = Material(source, modelId + ".bone", new Color(.70f, .68f, .57f, 1f), .02f, .16f);
        var accentColor = accentOverride ?? new Color(.50f, .82f, .92f, 1f);
        var crystal = Material(source, modelId + ".crystal", accentColor, .02f, .72f, .42f);
        var crystalBright = Material(source, modelId + ".crystal-bright", Color.Lerp(accentColor, Color.white, .28f), .01f, .82f, .62f);

        switch (modelId)
        {
            case CrystalWardstone: BuildWardstone(root, stone, darkStone, iron, crystal, crystalBright); break;
            case PassageStone: BuildPassageStone(root, stone, darkStone, iron, crystal); break;
            case RunedTotem: BuildRunedTotem(root, wood, darkWood, iron, crystal); break;
            case RunicKeelstone: BuildKeelstone(root, darkWood, iron, bronze, crystal); break;
            case CrystalLantern: BuildLantern(root, iron, bronze, crystalBright); break;
            case CrystalBrazier: BuildBrazier(root, stone, iron, bronze, crystal, crystalBright); break;
            case RuneEngraver: BuildRuneEngraver(root, wood, stone, iron, bronze, crystal); break;
            case SeidrRitualFocus: BuildRitualFocus(root, stone, darkStone, iron, crystal, crystalBright); break;
            case SpiritFetishWolf: BuildWolfFetish(root, darkWood, bone, iron, crystal); break;
            case SpiritFetishRaven: BuildRavenFetish(root, darkWood, bone, iron, crystal); break;
            case SpiritFetishWarrior: BuildWarriorFetish(root, darkWood, bone, iron, crystal); break;
            default: throw new InvalidOperationException($"Unknown Magenheim world-artifact model '{modelId}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    private static void BuildWardstone(GameObject root, Material stone, Material darkStone, Material iron, Material crystal, Material bright)
    {
        Cylinder(root, "base", new Vector3(0f, .14f, 0f), new Vector3(.94f, .28f, .94f), 8, stone);
        Cylinder(root, "plinth", new Vector3(0f, .38f, 0f), new Vector3(.70f, .24f, .70f), 8, darkStone);
        Cylinder(root, "monolith", new Vector3(0f, 1.18f, 0f), new Vector3(.52f, 1.48f, .52f), 7, stone);
        for (var y = .70f; y <= 1.48f; y += .39f)
            RuneBand(root, "rune-band-" + y, new Vector3(0f, y, 0f), .30f, .035f, 10, iron, horizontal: true);
        RuneBand(root, "crown", new Vector3(0f, 1.86f, 0f), .34f, .055f, 12, iron, horizontal: true);
        Prism(root, "socket", new Vector3(0f, 1.91f, 0f), new Vector3(.25f, .18f, .25f), 8, darkStone);
        Prism(root, "ward-crystal", new Vector3(0f, 2.14f, 0f), new Vector3(.19f, .56f, .19f), 6, crystal);
        Prism(root, "ward-tip", new Vector3(.015f, 2.40f, -.01f), new Vector3(.11f, .22f, .11f), 5, bright, new Vector3(7f, 13f, -4f));
    }

    private static void BuildPassageStone(GameObject root, Material stone, Material darkStone, Material iron, Material crystal)
    {
        Cylinder(root, "left-stone", new Vector3(-.72f, .94f, 0f), new Vector3(.43f, 1.88f, .43f), 7, stone, new Vector3(0f, 0f, -4f));
        Cylinder(root, "right-stone", new Vector3(.72f, .94f, 0f), new Vector3(.43f, 1.88f, .43f), 7, stone, new Vector3(0f, 0f, 4f));
        Box(root, "lintel", new Vector3(0f, 1.92f, 0f), new Vector3(1.80f, .34f, .48f), stone);
        Box(root, "left-foot", new Vector3(-.72f, .13f, 0f), new Vector3(.64f, .26f, .62f), darkStone, new Vector3(0f, 7f, 0f));
        Box(root, "right-foot", new Vector3(.72f, .13f, 0f), new Vector3(.64f, .26f, .62f), darkStone, new Vector3(0f, -7f, 0f));
        RuneBand(root, "passage-ring", new Vector3(0f, 1.05f, .02f), .55f, .055f, 16, iron, horizontal: false);
        Prism(root, "left-key", new Vector3(-.50f, 1.54f, .12f), new Vector3(.11f, .31f, .11f), 5, crystal, new Vector3(10f, 0f, -14f));
        Prism(root, "right-key", new Vector3(.50f, 1.54f, .12f), new Vector3(.11f, .31f, .11f), 5, crystal, new Vector3(-10f, 0f, 14f));
        for (var i = 0; i < 6; i++)
        {
            var angle = 30f + i * 60f;
            var rad = angle * Mathf.Deg2Rad;
            Box(root, "rune-notch-" + i, new Vector3(.56f * Mathf.Cos(rad), 1.05f + .56f * Mathf.Sin(rad), .075f), new Vector3(.05f, .14f, .08f), iron, new Vector3(0f, 0f, angle));
        }
    }

    private static void BuildRunedTotem(GameObject root, Material wood, Material darkWood, Material iron, Material crystal)
    {
        Cylinder(root, "shaft", new Vector3(0f, .72f, 0f), new Vector3(.22f, 1.42f, .22f), 7, darkWood);
        Cylinder(root, "base", new Vector3(0f, .13f, 0f), new Vector3(.52f, .26f, .52f), 8, wood);
        Box(root, "left-fork", new Vector3(-.13f, 1.44f, 0f), new Vector3(.10f, .60f, .10f), wood, new Vector3(0f, 0f, -16f));
        Box(root, "right-fork", new Vector3(.13f, 1.44f, 0f), new Vector3(.10f, .60f, .10f), wood, new Vector3(0f, 0f, 16f));
        RuneBand(root, "binding-low", new Vector3(0f, .55f, 0f), .16f, .035f, 10, iron, horizontal: true);
        RuneBand(root, "binding-high", new Vector3(0f, 1.15f, 0f), .17f, .035f, 10, iron, horizontal: true);
        RuneBand(root, "socket-ring", new Vector3(0f, 1.52f, 0f), .24f, .045f, 10, iron, horizontal: false);
        Prism(root, "totem-crystal", new Vector3(0f, 1.58f, 0f), new Vector3(.16f, .44f, .16f), 6, crystal);
    }

    private static void BuildKeelstone(GameObject root, Material wood, Material iron, Material bronze, Material crystal)
    {
        Box(root, "keel-block", new Vector3(0f, .16f, 0f), new Vector3(1.36f, .28f, .46f), wood);
        Box(root, "iron-foot", new Vector3(0f, .04f, 0f), new Vector3(1.58f, .10f, .56f), iron);
        Box(root, "left-clamp", new Vector3(-.56f, .43f, 0f), new Vector3(.16f, .62f, .55f), iron, new Vector3(0f, 0f, -7f));
        Box(root, "right-clamp", new Vector3(.56f, .43f, 0f), new Vector3(.16f, .62f, .55f), iron, new Vector3(0f, 0f, 7f));
        RuneBand(root, "keel-ring", new Vector3(0f, .50f, 0f), .31f, .050f, 12, bronze, horizontal: false);
        Prism(root, "keel-crystal", new Vector3(0f, .52f, 0f), new Vector3(.20f, .50f, .20f), 6, crystal, new Vector3(3f, 0f, -4f));
        Box(root, "fore-rune", new Vector3(0f, .26f, -.25f), new Vector3(.48f, .06f, .05f), bronze);
    }

    private static void BuildLantern(GameObject root, Material iron, Material bronze, Material crystal)
    {
        Box(root, "base", new Vector3(0f, .07f, 0f), new Vector3(.48f, .14f, .48f), iron);
        Box(root, "top", new Vector3(0f, .86f, 0f), new Vector3(.48f, .14f, .48f), iron);
        foreach (var x in new[] { -.19f, .19f })
            foreach (var z in new[] { -.19f, .19f })
                Cylinder(root, "cage-" + x + "-" + z, new Vector3(x, .47f, z), new Vector3(.045f, .72f, .045f), 6, bronze);
        Prism(root, "lantern-crystal", new Vector3(0f, .47f, 0f), new Vector3(.22f, .66f, .22f), 6, crystal);
        RuneBand(root, "handle", new Vector3(0f, 1.02f, 0f), .22f, .028f, 12, iron, horizontal: false);
        Cylinder(root, "handle-pin", new Vector3(0f, .92f, 0f), new Vector3(.08f, .08f, .08f), 8, bronze);
    }

    private static void BuildBrazier(GameObject root, Material stone, Material iron, Material bronze, Material crystal, Material bright)
    {
        Cylinder(root, "foot", new Vector3(0f, .13f, 0f), new Vector3(.72f, .26f, .72f), 8, stone);
        Cylinder(root, "stem", new Vector3(0f, .55f, 0f), new Vector3(.22f, .72f, .22f), 8, iron);
        Cylinder(root, "bowl", new Vector3(0f, .98f, 0f), new Vector3(.86f, .22f, .86f), 10, bronze);
        RuneBand(root, "bowl-rim", new Vector3(0f, 1.08f, 0f), .45f, .045f, 14, iron, horizontal: true);
        Prism(root, "center-crystal", new Vector3(0f, 1.28f, 0f), new Vector3(.18f, .54f, .18f), 6, bright);
        Prism(root, "left-crystal", new Vector3(-.18f, 1.20f, .04f), new Vector3(.11f, .38f, .11f), 5, crystal, new Vector3(0f, 0f, -12f));
        Prism(root, "right-crystal", new Vector3(.18f, 1.18f, -.04f), new Vector3(.10f, .34f, .10f), 5, crystal, new Vector3(0f, 0f, 13f));
    }

    private static void BuildRuneEngraver(GameObject root, Material wood, Material stone, Material iron, Material bronze, Material crystal)
    {
        Box(root, "bench", new Vector3(0f, .58f, 0f), new Vector3(1.34f, .16f, .72f), wood);
        foreach (var x in new[] { -.52f, .52f })
            foreach (var z in new[] { -.24f, .24f })
                Box(root, "leg-" + x + "-" + z, new Vector3(x, .27f, z), new Vector3(.12f, .56f, .12f), wood);
        Box(root, "tablet", new Vector3(-.22f, .75f, 0f), new Vector3(.56f, .10f, .46f), stone, new Vector3(-7f, 0f, 0f));
        Cylinder(root, "engraving-wheel", new Vector3(.36f, .82f, 0f), new Vector3(.42f, .11f, .42f), 12, bronze, new Vector3(90f, 0f, 0f));
        Cylinder(root, "wheel-axle", new Vector3(.36f, .82f, 0f), new Vector3(.08f, .44f, .08f), 8, iron, new Vector3(90f, 0f, 0f));
        Box(root, "stylus-arm", new Vector3(.20f, 1.02f, -.18f), new Vector3(.06f, .46f, .06f), iron, new Vector3(0f, 0f, -18f));
        Prism(root, "focus-chip", new Vector3(-.22f, .84f, 0f), new Vector3(.11f, .22f, .11f), 5, crystal);
    }

    private static void BuildRitualFocus(GameObject root, Material stone, Material darkStone, Material iron, Material crystal, Material bright)
    {
        Cylinder(root, "ritual-base", new Vector3(0f, .11f, 0f), new Vector3(1.04f, .22f, 1.04f), 12, darkStone);
        Cylinder(root, "rune-disk", new Vector3(0f, .25f, 0f), new Vector3(.80f, .12f, .80f), 12, stone);
        RuneBand(root, "outer-runes", new Vector3(0f, .32f, 0f), .44f, .035f, 12, iron, horizontal: true);
        for (var i = 0; i < 3; i++)
        {
            var angle = i * Mathf.PI * 2f / 3f;
            var x = .38f * Mathf.Cos(angle);
            var z = .38f * Mathf.Sin(angle);
            Cylinder(root, "ritual-pillar-" + i, new Vector3(x, .62f, z), new Vector3(.10f, .64f, .10f), 7, darkStone);
            Prism(root, "ritual-node-" + i, new Vector3(x, .98f, z), new Vector3(.11f, .30f, .11f), 5, crystal, new Vector3(4f * Mathf.Sin(angle), 0f, -4f * Mathf.Cos(angle)));
        }
        Prism(root, "ritual-heart", new Vector3(0f, .61f, 0f), new Vector3(.20f, .54f, .20f), 6, bright);
    }

    private static void BuildWolfFetish(GameObject root, Material wood, Material bone, Material iron, Material crystal)
    {
        Cylinder(root, "spine", new Vector3(0f, .34f, 0f), new Vector3(.11f, .66f, .11f), 7, wood, new Vector3(0f, 0f, 7f));
        Box(root, "left-ear", new Vector3(-.10f, .74f, 0f), new Vector3(.07f, .30f, .07f), bone, new Vector3(0f, 0f, -18f));
        Box(root, "right-ear", new Vector3(.10f, .74f, 0f), new Vector3(.07f, .30f, .07f), bone, new Vector3(0f, 0f, 18f));
        RuneBand(root, "binding", new Vector3(0f, .50f, 0f), .12f, .025f, 8, iron, horizontal: true);
        Prism(root, "spirit-chip", new Vector3(.02f, .31f, .06f), new Vector3(.09f, .26f, .09f), 5, crystal, new Vector3(6f, 0f, -8f));
    }

    private static void BuildRavenFetish(GameObject root, Material wood, Material bone, Material iron, Material crystal)
    {
        Cylinder(root, "spine", new Vector3(0f, .36f, 0f), new Vector3(.10f, .70f, .10f), 7, wood);
        Box(root, "left-wing", new Vector3(-.17f, .48f, 0f), new Vector3(.32f, .07f, .08f), bone, new Vector3(0f, 0f, -18f));
        Box(root, "right-wing", new Vector3(.17f, .48f, 0f), new Vector3(.32f, .07f, .08f), bone, new Vector3(0f, 0f, 18f));
        Box(root, "beak", new Vector3(.14f, .78f, 0f), new Vector3(.34f, .07f, .07f), bone, new Vector3(0f, 0f, -8f));
        RuneBand(root, "binding", new Vector3(0f, .57f, 0f), .11f, .025f, 8, iron, horizontal: true);
        Prism(root, "spirit-chip", new Vector3(-.02f, .28f, .05f), new Vector3(.085f, .24f, .085f), 5, crystal);
    }

    private static void BuildWarriorFetish(GameObject root, Material wood, Material bone, Material iron, Material crystal)
    {
        Cylinder(root, "spine", new Vector3(0f, .36f, 0f), new Vector3(.10f, .72f, .10f), 7, wood);
        Cylinder(root, "shield", new Vector3(0f, .48f, .04f), new Vector3(.38f, .08f, .38f), 10, iron, new Vector3(90f, 0f, 0f));
        Box(root, "sword", new Vector3(.22f, .45f, -.03f), new Vector3(.06f, .66f, .06f), bone, new Vector3(0f, 0f, -15f));
        Box(root, "helm-left", new Vector3(-.08f, .80f, 0f), new Vector3(.06f, .24f, .06f), bone, new Vector3(0f, 0f, -13f));
        Box(root, "helm-right", new Vector3(.08f, .80f, 0f), new Vector3(.06f, .24f, .06f), bone, new Vector3(0f, 0f, 13f));
        Prism(root, "spirit-chip", new Vector3(0f, .32f, .08f), new Vector3(.09f, .25f, .09f), 5, crystal);
    }

    private static void RuneBand(GameObject root, string name, Vector3 center, float radius, float thickness, int segments, Material material, bool horizontal)
    {
        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var degrees = angle * Mathf.Rad2Deg;
            if (horizontal)
            {
                var position = center + new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));
                Box(root, name + "-" + i, position, new Vector3(thickness * 1.3f, thickness, radius * Mathf.PI * 2f / segments * .92f), material, new Vector3(0f, -degrees, 0f));
            }
            else
            {
                var position = center + new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f);
                Box(root, name + "-" + i, position, new Vector3(radius * Mathf.PI * 2f / segments * .92f, thickness, thickness * 1.3f), material, new Vector3(0f, 0f, degrees + 90f));
            }
        }
    }

    private static void Box(GameObject root, string name, Vector3 position, Vector3 size, Material material, Vector3? euler = null) =>
        AddPart(root, name, BoxMesh, position, size, Quaternion.Euler(euler ?? Vector3.zero), material);

    private static void Cylinder(GameObject root, string name, Vector3 position, Vector3 size, int sides, Material material, Vector3? euler = null)
    {
        if (!CylinderMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = CreateCylinderMesh(sides);
            CylinderMeshes.Add(sides, mesh);
        }
        AddPart(root, name, mesh, position, size, Quaternion.Euler(euler ?? Vector3.zero), material);
    }

    private static void Prism(GameObject root, string name, Vector3 position, Vector3 size, int sides, Material material, Vector3? euler = null)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = CreatePrismMesh(sides);
            PrismMeshes.Add(sides, mesh);
        }
        AddPart(root, name, mesh, position, size, Quaternion.Euler(euler ?? Vector3.zero), material);
    }

    private static void AddPart(GameObject root, string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
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
        var material = new Material(source) { name = "magenheim.world-artifact." + suffix };
        material.mainTexture = Texture2D.whiteTexture;
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
        var mesh = new Mesh { name = "magenheim.world-artifact.box" };
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
                var a = Mathf.PI * 2f * i / sides;
                vertices.Add(new Vector3(.5f * Mathf.Cos(a), y, .5f * Mathf.Sin(a)));
            }
        }
        var bottom = vertices.Count; vertices.Add(new Vector3(0f,-.5f,0f));
        var top = vertices.Count; vertices.Add(new Vector3(0f,.5f,0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[] { i,next,sides+i, next,sides+next,sides+i, bottom,next,i, top,sides+i,sides+next });
        }
        var mesh = new Mesh { name = "magenheim.world-artifact.cylinder." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
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
            var a = Mathf.PI * 2f * i / sides;
            vertices.Add(new Vector3(.42f * Mathf.Cos(a), -.5f, .42f * Mathf.Sin(a)));
        }
        for (var i = 0; i < sides; i++)
        {
            var a = Mathf.PI * 2f * i / sides;
            vertices.Add(new Vector3(.50f * Mathf.Cos(a), .18f, .50f * Mathf.Sin(a)));
        }
        var top = vertices.Count; vertices.Add(new Vector3(0f,.70f,0f));
        var bottom = vertices.Count; vertices.Add(new Vector3(0f,-.50f,0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[] { bottom,next,i, i,next,sides+i, next,sides+next,sides+i, sides+i,sides+next,top });
        }
        var mesh = new Mesh { name = "magenheim.world-artifact.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
