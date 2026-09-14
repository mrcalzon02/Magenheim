using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Four-tier Seidr staff family. Seidr is deliberate sorcery: precise hexes, rune pressure,
/// spreading weave patterns, and repeated fate-line barrages. Every tier has original
/// procedural geometry rather than a recolored vanilla staff.
/// </summary>
internal sealed class SeidrStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffFireball";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal SeidrStaffRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private void RegisterContent()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab(BaseStaffPrefab) is null)
                throw new InvalidOperationException($"Required vanilla staff prefab '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Seidr staff recipes are registered.");

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Seidr staffs: Hex Needle, Rune Spear, Witchweave, and Fate Loom.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Seidr staff content registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterStaff(Definition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Seidr staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_spirit = definition.SpiritDamage };
        shared.m_icons = new[]
        {
            EarthAssets.Icon("crystal", definition.AssetName, new Color(.67f, .30f, .96f, 1f))
        };

        var attack = shared.m_attack;
        attack.m_attackStamina = definition.StaminaCost;
        attack.m_attackEitr = definition.EitrCost;
        attack.m_damageMultiplier = definition.DamageMultiplier;
        attack.m_forceMultiplier = definition.ForceMultiplier;
        attack.m_staggerMultiplier = definition.StaggerMultiplier;
        attack.m_projectileVel = definition.ProjectileVelocity;
        attack.m_projectileVelMin = definition.ProjectileVelocity;
        attack.m_projectileAccuracy = definition.ProjectileAccuracy;
        attack.m_projectileAccuracyMin = definition.ProjectileAccuracy;
        attack.m_projectiles = definition.Projectiles;
        attack.m_projectileBursts = definition.Bursts;
        attack.m_burstInterval = definition.BurstInterval;
        attack.m_perBurstResourceUsage = false;

        SeidrVisuals.Apply(item.ItemPrefab, definition.AssetName);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Seidr staff item '{definition.PrefabName}'.");
    }

    private static void RegisterRecipe(Definition definition)
    {
        var config = new RecipeConfig
        {
            Name = "Magenheim_Recipe_" + definition.PrefabName,
            Item = definition.PrefabName,
            Amount = 1,
            CraftingStation = WorkshopRegistrar.StationPrefab,
            MinStationLevel = definition.MinimumStationLevel,
            Enabled = true,
        };
        foreach (var requirement in definition.Requirements)
            config.AddRequirement(requirement.PrefabName, requirement.Amount);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException($"Jotunn refused Seidr staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<Definition> Definitions() => new[]
    {
        new Definition(
            "Magenheim_Staff_Seidr_Simple", "staff-seidr-simple", "Simple Staff of Seidr",
            "Hex Needle: drives a narrow omen-laced bolt through one chosen line. Cheap, exact, and intentionally unforgiving of poor aim.",
            1, 18f, 0f, 22f, .58f, .42f, .55f, 42f, .75f, 1, 1, 0f,
            new Requirement("FineWood", 8), new Requirement("GreydwarfEye", 6), new Requirement("Magenheim_Crystal_Seidr_Simple", 1)),
        new Definition(
            "Magenheim_Staff_Seidr_Crystal", "staff-seidr-crystal", "Crystal Staff of Seidr",
            "Rune Spear: channels a fully shaped crystal through a rigid fork of binding runes, producing one heavy, straight sorcerous strike.",
            2, 28f, 0f, 42f, 1.05f, 1.10f, 1.15f, 50f, .35f, 1, 1, 0f,
            new Requirement("ElderBark", 10), new Requirement("AncientSeed", 3), new Requirement("Silver", 2), new Requirement("Magenheim_Crystal_Seidr_Crystal", 1)),
        new Definition(
            "Magenheim_Staff_Seidr_Advanced", "staff-seidr-advanced", "Advanced Staff of Seidr",
            "Witchweave: fractures a single command into five crossing omen-lines. The fan is less violent per bolt but blankets evasive targets with sorcerous pressure.",
            3, 10f, 18f, 28f, .42f, .68f, .72f, 38f, 9.0f, 5, 1, 0f,
            new Requirement("YggdrasilWood", 10), new Requirement("BlackCore", 2), new Requirement("Eitr", 6), new Requirement("Magenheim_Crystal_Seidr_Advanced", 1)),
        new Definition(
            "Magenheim_Staff_Seidr_Master", "staff-seidr-master", "Master Staff of Seidr",
            "Fate Loom: holds the master crystal inside intersecting rune halos and releases repeated three-line volleys, stitching the target zone with controlled sorcery.",
            4, 0f, 46f, 34f, .46f, .76f, .80f, 42f, 7.0f, 3, 5, .13f,
            new Requirement("YggdrasilWood", 15), new Requirement("BlackCore", 4), new Requirement("Eitr", 12), new Requirement("Magenheim_Crystal_Seidr_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private readonly struct Requirement
    {
        internal Requirement(string prefabName, int amount) { PrefabName = prefabName; Amount = amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class Definition
    {
        internal Definition(
            string prefabName, string assetName, string displayName, string description, int minimumStationLevel,
            float staminaCost, float eitrCost, float spiritDamage, float damageMultiplier, float forceMultiplier,
            float staggerMultiplier, float projectileVelocity, float projectileAccuracy, int projectiles, int bursts,
            float burstInterval, params Requirement[] requirements)
        {
            PrefabName = prefabName;
            AssetName = assetName;
            DisplayName = displayName;
            Description = description;
            MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost;
            EitrCost = eitrCost;
            SpiritDamage = spiritDamage;
            DamageMultiplier = damageMultiplier;
            ForceMultiplier = forceMultiplier;
            StaggerMultiplier = staggerMultiplier;
            ProjectileVelocity = projectileVelocity;
            ProjectileAccuracy = projectileAccuracy;
            Projectiles = projectiles;
            Bursts = bursts;
            BurstInterval = burstInterval;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float EitrCost { get; }
        internal float SpiritDamage { get; }
        internal float DamageMultiplier { get; }
        internal float ForceMultiplier { get; }
        internal float StaggerMultiplier { get; }
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal int Projectiles { get; }
        internal int Bursts { get; }
        internal float BurstInterval { get; }
        internal IReadOnlyList<Requirement> Requirements { get; }
    }
}

internal static class SeidrVisuals
{
    private static readonly Mesh Box = MakeBox();
    private static readonly Dictionary<int, Mesh> Cylinders = new();
    private static readonly Dictionary<int, Mesh> Prisms = new();

    internal static void Apply(GameObject prefab, string assetName)
    {
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on Seidr staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);

        var blackWood = Material(source, "blackwood", new Color(.10f, .07f, .12f, 1f), 0f, .12f);
        var iron = Material(source, "blackiron", new Color(.16f, .13f, .20f, 1f), .62f, .30f);
        var silver = Material(source, "runesilver", new Color(.54f, .48f, .62f, 1f), .55f, .38f);
        var violet = Material(source, "violet", new Color(.62f, .20f, .95f, 1f), .02f, .74f, .42f);
        var pale = Material(source, "paleeitr", new Color(.83f, .62f, 1f, 1f), .01f, .86f, .70f);

        switch (assetName)
        {
            case "staff-seidr-simple":
                Shaft(root, blackWood, iron, .034f);
                Part(root, "crook-left", Box, new Vector3(-.085f, .68f, 0f), new Vector3(.035f, .34f, .035f), Quaternion.Euler(0f, 0f, -20f), iron);
                Part(root, "crook-right", Box, new Vector3(.055f, .72f, 0f), new Vector3(.030f, .25f, .030f), Quaternion.Euler(0f, 0f, 18f), silver);
                Prism(root, "hex", new Vector3(0f, .91f, 0f), .075f, .23f, 6, violet);
                break;

            case "staff-seidr-crystal":
                Shaft(root, blackWood, silver, .038f);
                Cylinder(root, "binding", new Vector3(0f, .57f, 0f), .082f, .055f, 12, silver);
                Part(root, "fork-left", Box, new Vector3(-.12f, .77f, 0f), new Vector3(.036f, .42f, .038f), Quaternion.Euler(0f, 0f, -16f), silver);
                Part(root, "fork-right", Box, new Vector3(.12f, .77f, 0f), new Vector3(.036f, .42f, .038f), Quaternion.Euler(0f, 0f, 16f), silver);
                Prism(root, "spear", new Vector3(0f, .97f, 0f), .095f, .46f, 6, violet);
                Prism(root, "tip", new Vector3(0f, 1.20f, 0f), .052f, .16f, 5, pale);
                break;

            case "staff-seidr-advanced":
                Shaft(root, blackWood, silver, .040f);
                Cylinder(root, "collar", new Vector3(0f, .56f, 0f), .095f, .060f, 14, silver);
                for (var i = 0; i < 3; i++)
                {
                    var angle = i * Mathf.PI * 2f / 3f;
                    var x = .14f * Mathf.Cos(angle);
                    var z = .14f * Mathf.Sin(angle);
                    Part(root, "tine-" + i, Box, new Vector3(x, .79f, z), new Vector3(.032f, .42f, .032f), Quaternion.Euler(10f * Mathf.Sin(angle), 0f, 18f * Mathf.Cos(angle)), silver);
                }
                SegmentedRing(root, "witch-ring", .89f, .22f, 12, .045f, violet, 0f);
                Prism(root, "weave-core", new Vector3(0f, .94f, 0f), .105f, .34f, 8, pale);
                break;

            case "staff-seidr-master":
                Shaft(root, blackWood, iron, .044f);
                foreach (var y in new[] { -.44f, -.12f, .24f, .50f })
                    Cylinder(root, "band-" + y, new Vector3(0f, y, 0f), .064f, .040f, 14, silver);
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * Mathf.PI / 3f;
                    var x = .18f * Mathf.Cos(angle);
                    var z = .18f * Mathf.Sin(angle);
                    Part(root, "loom-arm-" + i, Box, new Vector3(x, .80f, z), new Vector3(.028f, .38f, .028f), Quaternion.Euler(15f * Mathf.Sin(angle), 0f, 20f * Mathf.Cos(angle)), i % 2 == 0 ? silver : iron);
                    Prism(root, "node-" + i, new Vector3(x, 1.00f, z), .035f, .12f, 5, violet);
                }
                SegmentedRing(root, "outer-fate", .88f, .27f, 16, .038f, silver, 0f);
                SegmentedRing(root, "inner-fate", .93f, .18f, 12, .035f, violet, Mathf.PI / 12f);
                Prism(root, "master-core", new Vector3(0f, 1.00f, 0f), .125f, .50f, 8, pale);
                Prism(root, "omen-heart", new Vector3(0f, 1.19f, 0f), .062f, .18f, 6, violet);
                break;

            default:
                throw new InvalidOperationException($"Unknown Seidr geometry '{assetName}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void Shaft(GameObject root, Material wood, Material metal, float radius)
    {
        Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), radius, 1.48f, 10, wood);
        Cylinder(root, "pommel", new Vector3(0f, -.77f, 0f), radius * 1.38f, .10f, 12, metal);
        Cylinder(root, "neck", new Vector3(0f, .53f, 0f), radius * 1.42f, .06f, 12, metal);
    }

    private static void SegmentedRing(GameObject root, string name, float y, float radius, int segments, float size, Material material, float phase)
    {
        for (var i = 0; i < segments; i++)
        {
            var angle = phase + i * Mathf.PI * 2f / segments;
            var position = new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle));
            var tangentDegrees = -angle * Mathf.Rad2Deg;
            Part(root, name + "-" + i, Box, position, new Vector3(size * 1.8f, size, size), Quaternion.Euler(0f, tangentDegrees, 0f), material);
        }
    }

    private static void Cylinder(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material)
    {
        if (!Cylinders.TryGetValue(sides, out var mesh))
        {
            mesh = MakeCylinder(sides);
            Cylinders.Add(sides, mesh);
        }
        Part(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);
    }

    private static void Prism(GameObject root, string name, Vector3 position, float radius, float height, int sides, Material material)
    {
        if (!Prisms.TryGetValue(sides, out var mesh))
        {
            mesh = MakePrism(sides);
            Prisms.Add(sides, mesh);
        }
        Part(root, name, mesh, position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);
    }

    private static void Part(GameObject root, string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var gameObject = new GameObject(name) { layer = root.layer };
        gameObject.transform.SetParent(root.transform, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localRotation = rotation;
        gameObject.transform.localScale = scale;
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material Material(Material source, string name, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.seidr." + name };
        material.mainTexture = Texture2D.whiteTexture;
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

    private static Mesh MakeBox()
    {
        var mesh = new Mesh { name = "magenheim.seidr.box" };
        mesh.vertices = new[]
        {
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f),
        };
        mesh.triangles = new[]
        {
            0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4,
            3,7,6, 3,6,2, 1,2,6, 1,6,5, 0,4,7, 0,7,3,
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh MakeCylinder(int sides)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (var i = 0; i < sides; i++)
        {
            var angle = i * Mathf.PI * 2f / sides;
            var x = .5f * Mathf.Cos(angle);
            var z = .5f * Mathf.Sin(angle);
            vertices.Add(new Vector3(x, -.5f, z));
            vertices.Add(new Vector3(x, .5f, z));
        }
        var bottom = vertices.Count;
        vertices.Add(new Vector3(0f, -.5f, 0f));
        var top = vertices.Count;
        vertices.Add(new Vector3(0f, .5f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            var a = i * 2;
            var b = next * 2;
            triangles.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b, bottom, b, a, top, a + 1, b + 1 });
        }
        var mesh = new Mesh { name = "magenheim.seidr.cylinder." + sides };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh MakePrism(int sides)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (var i = 0; i < sides; i++)
        {
            var angle = i * Mathf.PI * 2f / sides;
            var x = .5f * Mathf.Cos(angle);
            var z = .5f * Mathf.Sin(angle);
            vertices.Add(new Vector3(x, -.5f, z));
            vertices.Add(new Vector3(x, .18f, z));
        }
        var bottom = vertices.Count;
        vertices.Add(new Vector3(0f, -.5f, 0f));
        var tip = vertices.Count;
        vertices.Add(new Vector3(0f, .5f, 0f));
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            var a = i * 2;
            var b = next * 2;
            triangles.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b, bottom, b, a, tip, a + 1, b + 1 });
        }
        var mesh = new Mesh { name = "magenheim.seidr.prism." + sides };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
