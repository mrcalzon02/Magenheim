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
/// Four-tier Spirit staff family. Spirit magic is built around echoes and repeated spectral
/// pressure. Every tier has its own original assembled model rather than reusing a vanilla mesh.
/// </summary>
internal sealed class SpiritStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffFireball";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal SpiritStaffRegistrar(ManualLogSource log) =>
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
                throw new InvalidOperationException("Geologist's Workstation must exist before Spirit staff recipes are registered.");

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Spirit staffs: Whisper Rod, Wraith Lantern, Soul Chorus, and Reliquary of Echoes.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Spirit staff content registration failed: {exception}");
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
            throw new InvalidOperationException($"Cannot replace occupied Spirit staff identity '{definition.PrefabName}'.");

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
            EarthAssets.Icon("crystal", definition.AssetName, new Color(.26f, .92f, .78f, 1f))
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

        SpiritVisuals.Apply(item.ItemPrefab, definition.AssetName);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Spirit staff item '{definition.PrefabName}'.");
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
            throw new InvalidOperationException($"Jotunn refused Spirit staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<Definition> Definitions() => new[]
    {
        new Definition(
            "Magenheim_Staff_Spirit_Simple", "staff-spirit-simple", "Simple Staff of Spirit",
            "Whisper Rod: releases one quiet spirit bolt with little force but clean supernatural damage. It is a listening instrument turned into a weapon.",
            1, 17f, 0f, 24f, .55f, .22f, .35f, 36f, 1.1f, 1, 1, 0f,
            new Requirement("FineWood", 8), new Requirement("BoneFragments", 8), new Requirement("Magenheim_Crystal_Spirit_Simple", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Crystal", "staff-spirit-crystal", "Crystal Staff of Spirit",
            "Wraith Lantern: suspends a shaped Spirit crystal inside a binding cage and drives a dense spectral lance down one line.",
            2, 27f, 0f, 48f, 1.08f, .62f, .80f, 48f, .45f, 1, 1, 0f,
            new Requirement("ElderBark", 10), new Requirement("Chain", 2), new Requirement("Silver", 2), new Requirement("Magenheim_Crystal_Spirit_Crystal", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Advanced", "staff-spirit-advanced", "Advanced Staff of Spirit",
            "Soul Chorus: four spectral voices answer the same cast, then echo once more. The pattern trades single-hit weight for overlapping spirit pressure.",
            3, 10f, 18f, 30f, .40f, .38f, .50f, 39f, 7.5f, 4, 2, .17f,
            new Requirement("YggdrasilWood", 10), new Requirement("BlackCore", 2), new Requirement("Eitr", 6), new Requirement("Magenheim_Crystal_Spirit_Advanced", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Master", "staff-spirit-master", "Master Staff of Spirit",
            "Reliquary of Echoes: a caged master crystal releases four spectral lines through four successive responses, filling a broad lane with returning shades.",
            4, 0f, 48f, 36f, .43f, .42f, .58f, 42f, 10f, 4, 4, .12f,
            new Requirement("YggdrasilWood", 15), new Requirement("BlackCore", 4), new Requirement("Eitr", 12), new Requirement("Magenheim_Crystal_Spirit_Master", 1)),
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

internal static class SpiritVisuals
{
    internal static void Apply(GameObject prefab, string assetName)
    {
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on Spirit staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);

        var bone = MakeMaterial(source, "bone", new Color(.66f, .70f, .65f, 1f), .02f, .18f);
        var dark = MakeMaterial(source, "dark", new Color(.08f, .12f, .13f, 1f), .18f, .16f);
        var silver = MakeMaterial(source, "silver", new Color(.42f, .55f, .55f, 1f), .56f, .38f);
        var spirit = MakeMaterial(source, "spirit", new Color(.24f, .88f, .76f, 1f), .01f, .74f, .58f);
        var pale = MakeMaterial(source, "pale", new Color(.72f, 1f, .92f, 1f), .01f, .84f, .78f);

        switch (assetName)
        {
            case "staff-spirit-simple":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.065f, 1.48f, .065f), dark);
                Cylinder(root, "bone-cap", new Vector3(0f, .57f, 0f), new Vector3(.10f, .08f, .10f), bone);
                Cube(root, "hook", new Vector3(.07f, .73f, 0f), new Vector3(.035f, .34f, .035f), Quaternion.Euler(0f, 0f, 24f), bone);
                Sphere(root, "whisper", new Vector3(.13f, .90f, 0f), new Vector3(.13f, .18f, .13f), spirit);
                break;

            case "staff-spirit-crystal":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.072f, 1.48f, .072f), dark);
                Cylinder(root, "collar", new Vector3(0f, .55f, 0f), new Vector3(.13f, .07f, .13f), silver);
                for (var i = 0; i < 4; i++)
                {
                    var angle = i * Mathf.PI / 2f;
                    var x = .14f * Mathf.Cos(angle);
                    var z = .14f * Mathf.Sin(angle);
                    Cube(root, "cage-" + i, new Vector3(x, .82f, z), new Vector3(.030f, .46f, .030f), Quaternion.Euler(10f * Mathf.Sin(angle), 0f, 16f * Mathf.Cos(angle)), silver);
                }
                Sphere(root, "lantern", new Vector3(0f, .90f, 0f), new Vector3(.20f, .28f, .20f), spirit);
                Sphere(root, "heart", new Vector3(0f, .97f, 0f), new Vector3(.095f, .13f, .095f), pale);
                break;

            case "staff-spirit-advanced":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.078f, 1.48f, .078f), dark);
                Cylinder(root, "collar", new Vector3(0f, .54f, 0f), new Vector3(.15f, .08f, .15f), silver);
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * Mathf.PI / 3f;
                    var x = .19f * Mathf.Cos(angle);
                    var z = .19f * Mathf.Sin(angle);
                    Sphere(root, "voice-" + i, new Vector3(x, .88f, z), new Vector3(.075f, .10f, .075f), i % 2 == 0 ? spirit : pale);
                    Cube(root, "rib-" + i, new Vector3(x * .70f, .80f, z * .70f), new Vector3(.026f, .42f, .026f), Quaternion.Euler(14f * Mathf.Sin(angle), 0f, 18f * Mathf.Cos(angle)), bone);
                }
                Sphere(root, "chorus-core", new Vector3(0f, .91f, 0f), new Vector3(.22f, .30f, .22f), spirit);
                break;

            case "staff-spirit-master":
                Cylinder(root, "shaft", new Vector3(0f, -.08f, 0f), new Vector3(.084f, 1.48f, .084f), dark);
                foreach (var y in new[] { -.44f, -.12f, .24f, .51f })
                    Cylinder(root, "band-" + y, new Vector3(0f, y, 0f), new Vector3(.13f, .045f, .13f), silver);
                for (var ring = 0; ring < 2; ring++)
                {
                    var radius = ring == 0 ? .21f : .29f;
                    var y = ring == 0 ? .88f : .94f;
                    var count = ring == 0 ? 6 : 8;
                    for (var i = 0; i < count; i++)
                    {
                        var angle = i * Mathf.PI * 2f / count + (ring == 0 ? 0f : Mathf.PI / 8f);
                        var x = radius * Mathf.Cos(angle);
                        var z = radius * Mathf.Sin(angle);
                        Sphere(root, "echo-" + ring + "-" + i, new Vector3(x, y, z), new Vector3(.065f, .085f, .065f), ring == 0 ? spirit : pale);
                    }
                }
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Mathf.PI / 4f;
                    var x = .20f * Mathf.Cos(angle);
                    var z = .20f * Mathf.Sin(angle);
                    Cube(root, "reliquary-rib-" + i, new Vector3(x * .70f, .83f, z * .70f), new Vector3(.025f, .48f, .025f), Quaternion.Euler(17f * Mathf.Sin(angle), 0f, 20f * Mathf.Cos(angle)), silver);
                }
                Sphere(root, "reliquary", new Vector3(0f, .95f, 0f), new Vector3(.25f, .38f, .25f), spirit);
                Sphere(root, "soul-heart", new Vector3(0f, 1.07f, 0f), new Vector3(.11f, .16f, .11f), pale);
                break;

            default:
                throw new InvalidOperationException($"Unknown Spirit geometry '{assetName}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void Cube(GameObject root, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material) =>
        Primitive(root, name, PrimitiveType.Cube, position, scale, rotation, material);

    private static void Cylinder(GameObject root, string name, Vector3 position, Vector3 scale, Material material) =>
        Primitive(root, name, PrimitiveType.Cylinder, position, scale, Quaternion.identity, material);

    private static void Sphere(GameObject root, string name, Vector3 position, Vector3 scale, Material material) =>
        Primitive(root, name, PrimitiveType.Sphere, position, scale, Quaternion.identity, material);

    private static void Primitive(GameObject root, string name, PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.layer = root.layer;
        gameObject.transform.SetParent(root.transform, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localRotation = rotation;
        gameObject.transform.localScale = scale;
        var collider = gameObject.GetComponent<Collider>();
        if (collider) collider.enabled = false;
        var renderer = gameObject.GetComponent<Renderer>();
        if (!renderer) throw new InvalidOperationException($"Spirit visual primitive '{name}' has no renderer.");
        renderer.sharedMaterial = material;
    }

    private static Material MakeMaterial(Material source, string name, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.spirit." + name };
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
}
