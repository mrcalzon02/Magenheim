using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Original geometry for the floating iron-banded Crystal Sentinel and its unified munition indicator.</summary>
internal static class CrystalSentinelVisuals
{
    private static readonly Color MunitionTint = new(.72f, .91f, 1f, 1f);
    private static readonly Mesh BoxMesh = CreateBoxMesh();
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static GameObject Apply(GameObject prefab)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        var turret = prefab.GetComponent<Turret>()
            ?? throw new InvalidOperationException($"Crystal Sentinel host '{prefab.name}' has no Turret component.");

        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"Crystal Sentinel host '{prefab.name}' exposes no material source.");

        var root = new GameObject("magenheim.crystal-sentinel.visual") { layer = prefab.layer };
        root.transform.SetParent(prefab.transform, false);

        var stone = Material(source, "black-marble", new Color(.10f, .12f, .14f, 1f), .18f, .20f);
        var iron = Material(source, "iron", new Color(.25f, .29f, .32f, 1f), .72f, .34f);
        var crystal = Material(source, "crystal", new Color(.58f, .86f, .98f, 1f), .03f, .88f, .38f);
        var crystalCore = Material(source, "crystal-core", new Color(.90f, .98f, 1f, 1f), .01f, .96f, .68f);

        Cylinder(root, "base-plinth", new Vector3(0f, .14f, 0f), .78f, .28f, 12, stone);
        Cylinder(root, "lower-ring", new Vector3(0f, .34f, 0f), .66f, .08f, 12, iron);
        Cylinder(root, "upper-ring", new Vector3(0f, .52f, 0f), .49f, .065f, 12, iron);

        var body = new GameObject("floating-body") { layer = prefab.layer };
        body.transform.SetParent(turret.m_turretBody ? turret.m_turretBody.transform : root.transform, false);
        body.transform.localPosition = new Vector3(0f, .85f, 0f);

        Prism(body, "sentinel-crystal", Vector3.zero, .44f, 1.90f, 8, crystal);
        Prism(body, "sentinel-core", new Vector3(0f, .05f, 0f), .12f, 1.55f, 8, crystalCore);
        Band(body, "band-lower", -.48f, .48f, 10, iron);
        Band(body, "band-middle", 0f, .50f, 10, iron);
        Band(body, "band-upper", .48f, .46f, 10, iron);
        for (var i = 0; i < 4; i++)
        {
            var angle = Mathf.PI * .5f * i;
            var x = .37f * Mathf.Cos(angle);
            var z = .37f * Mathf.Sin(angle);
            Box(body, "iron-spine-" + i, new Vector3(x, 0f, z), new Vector3(.055f, 1.50f, .055f), iron,
                new Vector3(0f, -angle * Mathf.Rad2Deg, 0f));
        }

        // The vanilla turret body aims toward local -Z. Give the owned body an unmistakable
        // forward emitter so placement and tracking direction remain readable at a glance.
        Box(body, "forward-yoke", new Vector3(0f, .05f, -.38f), new Vector3(.52f, .09f, .09f), iron);
        Box(body, "forward-rail-left", new Vector3(-.18f, .05f, -.55f), new Vector3(.055f, .055f, .42f), iron);
        Box(body, "forward-rail-right", new Vector3(.18f, .05f, -.55f), new Vector3(.055f, .055f, .42f), iron);
        Cylinder(body, "forward-collar", new Vector3(0f, .05f, -.69f), .16f, .09f, 12, iron, new Vector3(90f, 0f, 0f));
        Prism(body, "forward-emitter", new Vector3(0f, .05f, -.78f), .105f, .42f, 7, crystalCore, new Vector3(-90f, 0f, 0f));

        var light = body.AddComponent<Light>();
        light.color = new Color(.55f, .82f, 1f, 1f);
        light.range = 4.5f;
        light.intensity = 1.25f;

        foreach (var renderer in original)
        {
            if (renderer.transform.IsChildOf(root.transform)) continue;
            renderer.enabled = false;
        }
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
        return root;
    }

    internal static GameObject CreateAmmoVisual(Turret turret, Material source)
    {
        if (turret is null) throw new ArgumentNullException(nameof(turret));
        var root = new GameObject("magenheim.sentinel.ammo.crystal") { layer = turret.gameObject.layer };
        root.transform.SetParent(turret.m_turretBody ? turret.m_turretBody.transform : turret.transform, false);
        root.transform.localPosition = new Vector3(0f, .90f, -.52f);
        var material = Material(source, "ammo-crystal", MunitionTint, .02f, .90f, .60f);
        Prism(root, "loaded-crystal-munition", Vector3.zero, .13f, .42f, 7, material, new Vector3(90f, 0f, 0f));
        root.SetActive(false);
        return root;
    }

    internal static void TintProjectile(GameObject projectile)
    {
        foreach (var renderer in projectile.GetComponentsInChildren<Renderer>(true))
        {
            var sources = renderer.sharedMaterials;
            var materials = new Material[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (!source) continue;
                var material = new Material(source) { name = $"magenheim.sentinel.projectile.crystal.{i}" };
                GeneratedSurfaceTextures.Apply(material, "crystal-projectile");
                if (material.HasProperty("_Color")) material.SetColor("_Color", MunitionTint);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", MunitionTint * 1.25f);
                    material.EnableKeyword("_EMISSION");
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
        foreach (var particles in projectile.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(MunitionTint);
        }
    }

    private static void Band(GameObject root, string name, float y, float radius, int segments, Material material)
    {
        for (var i = 0; i < segments; i++)
        {
            var angle = 2f * Mathf.PI * i / segments;
            var x = radius * Mathf.Cos(angle);
            var z = radius * Mathf.Sin(angle);
            Box(root, name + "-" + i, new Vector3(x, y, z), new Vector3(.32f, .075f, .065f), material,
                new Vector3(0f, -angle * Mathf.Rad2Deg, 0f));
        }
    }

    private static Material Material(Material source, string suffix, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.sentinel." + suffix };
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
        var mesh = new Mesh { name = "magenheim.sentinel.box" };
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
        var mesh = new Mesh { name = "magenheim.sentinel.cylinder." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
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
        var mesh = new Mesh { name = "magenheim.sentinel.prism." + sides, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
