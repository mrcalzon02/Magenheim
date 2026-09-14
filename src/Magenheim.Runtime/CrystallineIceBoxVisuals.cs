using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Magenheim.Runtime;

/// <summary>Procedural black-stone, iron-banded and Frost-crystal geometry for the Crystalline Ice Box.</summary>
internal static class CrystallineIceBoxVisuals
{
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"Crystalline Ice Box host '{prefab.name}' exposes no material source.");

        var root = new GameObject("magenheim.crystalline-ice-box.visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);

        var frost = ElementVisualPalette.Tint(Magenheim.Core.ElementalAlignment.Frost);
        var blackStone = Material(source, "black-stone", new Color(.075f, .095f, .12f, 1f), .08f, .20f);
        var iron = Material(source, "iron", new Color(.25f, .29f, .34f, 1f), .76f, .38f);
        var silver = Material(source, "silver", new Color(.58f, .68f, .76f, 1f), .78f, .58f);
        var crystal = Material(
            source,
            "frost-crystal",
            new Color(Mathf.Min(1f, frost.r + .14f), Mathf.Min(1f, frost.g + .12f), 1f, 1f),
            .04f,
            .92f,
            .72f);
        var ice = IceMaterial(source, frost);

        // The body is built as an actual shell rather than a recolored vanilla beehive.
        Box(root, "floor", new Vector3(0f, .17f, 0f), new Vector3(2.45f, .28f, 1.54f), blackStone);
        Box(root, "back-wall", new Vector3(0f, .72f, .68f), new Vector3(2.45f, 1.06f, .18f), blackStone);
        Box(root, "left-wall", new Vector3(-1.13f, .72f, 0f), new Vector3(.20f, 1.06f, 1.28f), blackStone);
        Box(root, "right-wall", new Vector3(1.13f, .72f, 0f), new Vector3(.20f, 1.06f, 1.28f), blackStone);
        Box(root, "front-plinth", new Vector3(0f, .36f, -.68f), new Vector3(2.45f, .36f, .18f), blackStone);
        Box(root, "lid", new Vector3(0f, 1.30f, 0f), new Vector3(2.55f, .22f, 1.62f), blackStone);

        // A translucent reservoir window makes the box visibly cold rather than just dark furniture.
        Box(root, "ice-window", new Vector3(0f, .82f, -.705f), new Vector3(1.82f, .58f, .055f), ice);
        Box(root, "ice-mass-left", new Vector3(-.53f, .72f, -.48f), new Vector3(.58f, .52f, .72f), ice);
        Box(root, "ice-mass-right", new Vector3(.45f, .66f, -.46f), new Vector3(.72f, .42f, .68f), ice);
        Box(root, "ice-mass-high", new Vector3(.05f, .93f, -.42f), new Vector3(.70f, .32f, .62f), ice);

        // Heavy iron framing keeps the design tied to the Magenheim workstation and Sentinel language.
        Box(root, "front-top-band", new Vector3(0f, 1.16f, -.72f), new Vector3(2.48f, .09f, .10f), iron);
        Box(root, "front-bottom-band", new Vector3(0f, .50f, -.72f), new Vector3(2.48f, .09f, .10f), iron);
        Box(root, "back-top-band", new Vector3(0f, 1.16f, .72f), new Vector3(2.48f, .09f, .10f), iron);
        Box(root, "left-post", new Vector3(-1.19f, .78f, -.02f), new Vector3(.10f, 1.18f, 1.50f), iron);
        Box(root, "right-post", new Vector3(1.19f, .78f, -.02f), new Vector3(.10f, 1.18f, 1.50f), iron);
        Box(root, "lid-band-front", new Vector3(0f, 1.39f, -.67f), new Vector3(2.62f, .08f, .09f), silver);
        Box(root, "lid-band-back", new Vector3(0f, 1.39f, .67f), new Vector3(2.62f, .08f, .09f), silver);
        Box(root, "handle", new Vector3(0f, 1.48f, -.38f), new Vector3(.68f, .09f, .10f), iron);
        Box(root, "handle-left", new Vector3(-.30f, 1.43f, -.38f), new Vector3(.08f, .16f, .10f), iron);
        Box(root, "handle-right", new Vector3(.30f, 1.43f, -.38f), new Vector3(.08f, .16f, .10f), iron);

        var crown = new[]
        {
            new Vector3(-.78f, 1.48f, .26f),
            new Vector3(-.36f, 1.50f, .08f),
            new Vector3(.06f, 1.52f, .30f),
            new Vector3(.48f, 1.48f, .06f),
            new Vector3(.83f, 1.47f, .25f),
        };
        for (var i = 0; i < crown.Length; i++)
        {
            var height = .42f + (i % 3) * .10f;
            Prism(
                root,
                "frost-crown-" + i,
                crown[i],
                .11f + (i % 2) * .025f,
                height,
                6,
                crystal,
                new Vector3((i % 2 == 0 ? -8f : 7f), i * 37f, (i % 3 - 1) * 6f));
        }

        // Small corner growths sell the idea that the Frost lattice is actively freezing the housing.
        Prism(root, "corner-growth-left", new Vector3(-1.05f, .88f, -.66f), .10f, .38f, 6, crystal, new Vector3(-15f, 18f, 7f));
        Prism(root, "corner-growth-right", new Vector3(1.05f, .84f, -.66f), .10f, .34f, 6, crystal, new Vector3(14f, -22f, -6f));

        var lightObject = new GameObject("cold-light") { layer = root.layer };
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, .86f, -.20f);
        var light = lightObject.AddComponent<Light>();
        light.color = new Color(.48f, .84f, 1f, 1f);
        light.range = 3.6f;
        light.intensity = .72f;

        foreach (var renderer in original)
            renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true))
            lod.enabled = false;

        return root;
    }

    private static Material Material(
        Material source,
        string suffix,
        Color color,
        float metallic,
        float gloss,
        float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.crystalline-ice-box." + suffix };
        material.mainTexture = Texture2D.whiteTexture;
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
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
        else
        {
            material.DisableKeyword("_EMISSION");
        }
        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        material.renderQueue = 2000;
        return material;
    }

    private static Material IceMaterial(Material source, Color frost)
    {
        var color = new Color(
            frost.r * .56f + .24f,
            frost.g * .66f + .24f,
            Mathf.Min(1f, frost.b * .82f + .22f),
            .62f);
        var material = Material(source, "ice", color, .02f, .97f, .22f);
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

    private static void Prism(
        GameObject root,
        string name,
        Vector3 position,
        float radius,
        float height,
        int sides,
        Material material,
        Vector3 rotation)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = CreatePrismMesh(sides);
            PrismMeshes.Add(sides, mesh);
        }

        AddPart(
            root,
            name,
            mesh,
            position,
            new Vector3(radius * 2f, height, radius * 2f),
            Quaternion.Euler(rotation),
            material);
    }

    private static void AddPart(
        GameObject root,
        string name,
        Mesh mesh,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
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
        var mesh = new Mesh { name = "magenheim.crystalline-ice-box.box" };
        mesh.vertices = new[]
        {
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f),
        };
        mesh.triangles = new[]
        {
            0,3,2, 0,2,1,
            4,5,6, 4,6,7,
            0,4,7, 0,7,3,
            1,2,6, 1,6,5,
            0,1,5, 0,5,4,
            3,7,6, 3,6,2,
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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

        var top = vertices.Count;
        vertices.Add(new Vector3(0f, .68f, 0f));
        var bottom = vertices.Count;
        vertices.Add(new Vector3(0f, -.50f, 0f));

        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            triangles.AddRange(new[]
            {
                bottom, next, i,
                i, next, sides + i,
                next, sides + next, sides + i,
                sides + i, sides + next, top,
            });
        }

        var mesh = new Mesh
        {
            name = "magenheim.crystalline-ice-box.prism." + sides,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
