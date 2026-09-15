using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Procedural creature presentation owned by Magenheim rather than foreign model assets.</summary>
internal static class DeepFractureCreatureVisuals
{
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject ApplyAnnoyanceWisp(GameObject prefab, ElementalAlignment alignment)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"Annoyance Wisp host '{prefab.name}' exposes no material source.");

        var root = new GameObject("magenheim.fracture.creature.annoyance-wisp.visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);
        root.transform.localPosition = new Vector3(0f, .18f, 0f);

        var tint = ElementVisualPalette.Tint(alignment);
        var shell = Material(source, "wisp-shell", Color.Lerp(tint, Color.white, .18f), .02f, .82f, .52f);
        var core = Material(source, "wisp-core", Color.Lerp(tint, Color.white, .72f), .01f, .96f, 1.35f);
        var shard = Material(source, "wisp-shard", tint, .04f, .78f, .88f);

        Prism(root, "motion-core", Vector3.zero, .22f, .58f, 7, core);
        Prism(root, "crystal-shell", Vector3.zero, .38f, .82f, 7, shell);
        for (var i = 0; i < 5; i++)
        {
            var angle = (Mathf.PI * 2f * i / 5f) + .28f;
            var position = new Vector3(Mathf.Cos(angle) * .48f, -.03f + ((i & 1) == 0 ? .08f : -.05f), Mathf.Sin(angle) * .48f);
            Prism(root, "orbit-shard-" + i, position, .075f, .30f, 5, shard,
                new Vector3(22f, -angle * Mathf.Rad2Deg, 34f));
        }

        var light = root.AddComponent<Light>();
        light.color = tint;
        light.range = 3.2f;
        light.intensity = .85f;

        foreach (var renderer in original)
            if (!renderer.transform.IsChildOf(root.transform)) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true))
            lod.enabled = false;

        return root;
    }

    private static Material Material(Material source, string suffix, Color color, float metallic, float gloss, float emission)
    {
        var material = new Material(source) { name = "magenheim.fracture.annoyance-wisp." + suffix };
        material.mainTexture = Texture2D.whiteTexture;
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
        material.DisableKeyword("_NORMALMAP");
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * emission);
            material.EnableKeyword("_EMISSION");
        }
        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        material.renderQueue = 2000;
        return material;
    }

    private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material, Vector3? rotation = null)
    {
        if (!PrismMeshes.TryGetValue(sides, out var mesh))
        {
            mesh = CreatePrismMesh(sides);
            PrismMeshes.Add(sides, mesh);
        }
        var part = new GameObject(name) { layer = root.layer };
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
        part.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
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
            triangles.AddRange(new[] { bottom, next, i, i, next, sides + i, next, sides + next, sides + i, sides + i, sides + next, top });
        }
        var mesh = new Mesh { name = "magenheim.fracture.creature.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
