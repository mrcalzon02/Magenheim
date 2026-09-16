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
/// Four-tier Spirit staff family. Spirit magic uses spectral projectiles and echo fields to haunt
/// enemies, suppress outgoing attacks, and keep pressure lingering after the initial hit.
/// </summary>
internal sealed class SpiritStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";
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
                throw new InvalidOperationException($"Required hidden staff carrier '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Spirit staff recipes are registered.");

            var haunted = RegisterSuppressionEffect(
                "Magenheim_SE_Haunted",
                "Haunted",
                "A spectral presence clings to the target, reducing all outgoing attack damage by twelve percent.",
                3.5f,
                .88f,
                new Color(.30f, .90f, .80f, 1f));
            var dissonance = RegisterSuppressionEffect(
                "Magenheim_SE_SpiritDissonance",
                "Spirit Dissonance",
                "Overlapping voices disrupt the target's intent, reducing all outgoing attack damage by twenty percent.",
                3f,
                .80f,
                new Color(.52f, 1f, .91f, 1f));
            var soulSuppression = RegisterSuppressionEffect(
                "Magenheim_SE_SoulSuppression",
                "Soul Suppression",
                "The reliquary smothers hostile intent, reducing all outgoing attack damage by thirty percent.",
                5f,
                .70f,
                new Color(.78f, 1f, .95f, 1f));

            var lanternEcho = StaffEffectPayloads.CreateField(
                "Magenheim_Spirit_LanternEcho",
                new HitData.DamageTypes { m_spirit = 3f },
                2.4f,
                3f,
                .75f,
                4f,
                new Color(.28f, .92f, .80f, 1f),
                1.35f,
                haunted);
            var chorusEcho = StaffEffectPayloads.CreateField(
                "Magenheim_Spirit_ChorusEcho",
                new HitData.DamageTypes { m_spirit = 2f },
                1.8f,
                1.4f,
                .40f,
                2f,
                new Color(.50f, 1f, .90f, 1f),
                1.55f,
                dissonance);
            var reliquaryEcho = StaffEffectPayloads.CreateField(
                "Magenheim_Spirit_ReliquaryEcho",
                new HitData.DamageTypes { m_spirit = 3.5f },
                2.8f,
                2.2f,
                .45f,
                3f,
                new Color(.74f, 1f, .94f, 1f),
                1.75f,
                soulSuppression);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_Projectile", new Color(.24f, .88f, .76f, 1f), 1.35f, null, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_LanternProjectile", new Color(.34f, .96f, .84f, 1f), 1.50f, lanternEcho, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_ChorusProjectile", new Color(.55f, 1f, .91f, 1f), 1.65f, chorusEcho, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_ReliquaryProjectile", new Color(.78f, 1f, .95f, 1f), 1.85f, reliquaryEcho, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Spirit abilities with Valheim-scale authored Spirit staff bodies, non-fire projectile carriers, and stamina-only casting.");
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

    private static StatusEffect RegisterSuppressionEffect(
        string identity,
        string displayName,
        string tooltip,
        float ttl,
        float damageMultiplier,
        Color tint)
    {
        if (damageMultiplier <= 0f || damageMultiplier > 1f)
            throw new ArgumentOutOfRangeException(nameof(damageMultiplier), damageMultiplier, "Spirit suppression must reduce outgoing damage without inverting it.");

        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = identity;
        effect.m_name = displayName;
        effect.m_tooltip = tooltip;
        effect.m_ttl = ttl;
        effect.m_icon = EarthAssets.Icon("crystal", identity, tint);
        effect.m_modifyAttackSkill = Skills.SkillType.All;
        effect.m_damageModifier = damageMultiplier;

        var custom = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(custom))
            throw new InvalidOperationException($"Jotunn refused Spirit status effect '{identity}'.");
        return custom.StatusEffect;
    }

    private static void RegisterStaff(Definition definition, PayloadSet payloads)
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
        shared.m_damagesPerLevel = new HitData.DamageTypes();
        shared.m_icons = new[] { EarthAssets.Icon("crystal", definition.AssetName, new Color(.26f, .92f, .78f, 1f)) };

        var attack = shared.m_attack;
        attack.m_attackProjectile = payloads.Resolve(definition.Payload);
        attack.m_attackStamina = definition.StaminaCost;
        attack.m_attackEitr = 0f;
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
            "Whisper Bolt: releases one quiet spectral shot. It carries only Spirit damage and leaves no borrowed flame, suppression field, or explosion behind.",
            1, 17f, 22f, .95f, .18f, .30f, 40f, .85f, 1, 1, 0f, PayloadKind.Direct,
            new Requirement("FineWood", 8), new Requirement("BoneFragments", 8), new Requirement("Magenheim_Crystal_Spirit_Simple", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Crystal", "staff-spirit-crystal", "Crystal Staff of Spirit",
            "Wraith Lantern: drives a dense spectral lance into one target and leaves a three-second haunting echo. Enemies touched by the echo become Haunted, reducing outgoing attack damage by twelve percent for three and a half seconds.",
            2, 27f, 34f, 1f, .42f, .65f, 50f, .35f, 1, 1, 0f, PayloadKind.Lantern,
            new Requirement("ElderBark", 10), new Requirement("Chain", 2), new Requirement("Silver", 2), new Requirement("Magenheim_Crystal_Spirit_Crystal", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Advanced", "staff-spirit-advanced", "Advanced Staff of Spirit",
            "Soul Chorus: four spectral voices answer the cast twice. Their overlapping echo fields inflict Spirit Dissonance, cutting outgoing attack damage by twenty percent while the chorus continues to haunt a clustered group.",
            3, 28f, 14f, .45f, .28f, .42f, 42f, 7.5f, 4, 2, .17f, PayloadKind.Chorus,
            new Requirement("YggdrasilWood", 10), new Requirement("BlackCore", 2), new Requirement("Magenheim_Crystal_Spirit_Advanced", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Master", "staff-spirit-master", "Master Staff of Spirit",
            "Reliquary of Echoes: releases four spectral lines through three successive responses. Each impact opens a larger echo field that inflicts Soul Suppression, reducing outgoing attack damage by thirty percent for five seconds after exposure.",
            4, 52f, 16f, .40f, .32f, .50f, 44f, 10f, 4, 3, .13f, PayloadKind.Reliquary,
            new Requirement("YggdrasilWood", 15), new Requirement("BlackCore", 4), new Requirement("Magenheim_Crystal_Spirit_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Direct, Lantern, Chorus, Reliquary }

    private sealed class PayloadSet
    {
        private readonly GameObject _direct;
        private readonly GameObject _lantern;
        private readonly GameObject _chorus;
        private readonly GameObject _reliquary;

        internal PayloadSet(GameObject direct, GameObject lantern, GameObject chorus, GameObject reliquary)
        {
            _direct = direct;
            _lantern = lantern;
            _chorus = chorus;
            _reliquary = reliquary;
        }

        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Direct => _direct,
            PayloadKind.Lantern => _lantern,
            PayloadKind.Chorus => _chorus,
            PayloadKind.Reliquary => _reliquary,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct Requirement
    {
        internal Requirement(string prefabName, int amount)
        {
            PrefabName = prefabName;
            Amount = amount;
        }

        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class Definition
    {
        internal Definition(
            string prefabName,
            string assetName,
            string displayName,
            string description,
            int minimumStationLevel,
            float staminaCost,
            float spiritDamage,
            float damageMultiplier,
            float forceMultiplier,
            float staggerMultiplier,
            float projectileVelocity,
            float projectileAccuracy,
            int projectiles,
            int bursts,
            float burstInterval,
            PayloadKind payload,
            params Requirement[] requirements)
        {
            PrefabName = prefabName;
            AssetName = assetName;
            DisplayName = displayName;
            Description = description;
            MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost;
            SpiritDamage = spiritDamage;
            DamageMultiplier = damageMultiplier;
            ForceMultiplier = forceMultiplier;
            StaggerMultiplier = staggerMultiplier;
            ProjectileVelocity = projectileVelocity;
            ProjectileAccuracy = projectileAccuracy;
            Projectiles = projectiles;
            Bursts = bursts;
            BurstInterval = burstInterval;
            Payload = payload;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float SpiritDamage { get; }
        internal float DamageMultiplier { get; }
        internal float ForceMultiplier { get; }
        internal float StaggerMultiplier { get; }
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal int Projectiles { get; }
        internal int Bursts { get; }
        internal float BurstInterval { get; }
        internal PayloadKind Payload { get; }
        internal IReadOnlyList<Requirement> Requirements { get; }
    }
}

internal static class SpiritVisuals
{
    private static readonly Mesh BoxMesh = RuntimeMeshPrimitives.Box("magenheim.spirit-staff.box");
    private static readonly Dictionary<int, Mesh> CylinderMeshes = new();
    private static readonly Dictionary<int, Mesh> PrismMeshes = new();

    internal static void Apply(GameObject prefab, string assetName)
    {
        var original = prefab.GetComponentsInChildren<Renderer>(true);
        var source = original.Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material)
            ?? throw new InvalidOperationException($"No material source exists on Spirit staff prefab '{prefab.name}'.");
        var attach = prefab.transform.Find("attach") ?? prefab.transform;
        var root = new GameObject("magenheim." + assetName + ".visual") { layer = prefab.layer };
        root.transform.SetParent(attach, false);

        var bone = MakeMaterial(source, "bone", new Color(.56f, .61f, .57f, 1f), .02f, .16f);
        var dark = MakeMaterial(source, "dark-wood", new Color(.065f, .09f, .085f, 1f), .08f, .13f);
        var leather = MakeMaterial(source, "leather", new Color(.13f, .09f, .075f, 1f), .01f, .10f);
        var silver = MakeMaterial(source, "silver", new Color(.38f, .48f, .47f, 1f), .62f, .34f);
        var spirit = MakeMaterial(source, "spirit-crystal", new Color(.20f, .80f, .68f, 1f), .01f, .72f, .58f);
        var pale = MakeMaterial(source, "spirit-pale-crystal", new Color(.64f, .96f, .84f, 1f), .01f, .82f, .82f);

        switch (assetName)
        {
            case "staff-spirit-simple":
                BuildSimple(root, dark, leather, bone, spirit, pale);
                break;
            case "staff-spirit-crystal":
                BuildCrystal(root, dark, leather, bone, silver, spirit, pale);
                break;
            case "staff-spirit-advanced":
                BuildAdvanced(root, dark, leather, bone, silver, spirit, pale);
                break;
            case "staff-spirit-master":
                BuildMaster(root, dark, leather, bone, silver, spirit, pale);
                break;
            default:
                throw new InvalidOperationException($"Unknown Spirit geometry '{assetName}'.");
        }

        foreach (var renderer in original) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) lod.enabled = false;
    }

    private static void BuildSimple(GameObject root, Material dark, Material leather, Material bone, Material spirit, Material pale)
    {
        BuildOrganicShaft(root, dark, leather, .040f, false);
        Segment(root, "left-fork", new Vector3(.015f, .48f, .005f), new Vector3(-.13f, .92f, .015f), .029f, bone, 8);
        Segment(root, "right-fork", new Vector3(.025f, .50f, -.005f), new Vector3(.18f, .82f, -.025f), .025f, bone, 8);
        Segment(root, "broken-tine", new Vector3(-.13f, .92f, .015f), new Vector3(-.07f, 1.04f, .025f), .020f, bone, 7);
        Shard(root, "whisper-core", new Vector3(.045f, .87f, .01f), new Vector3(.16f, .30f, .16f), Quaternion.Euler(4f, 0f, -9f), spirit, 6);
        Shard(root, "whisper-splinter", new Vector3(.17f, .91f, -.025f), new Vector3(.055f, .16f, .055f), Quaternion.Euler(12f, 20f, 19f), pale, 5);
        AddFocusLight(root, new Vector3(.045f, .89f, .01f), new Color(.22f, .88f, .74f), 1.35f, .34f);
    }

    private static void BuildCrystal(GameObject root, Material dark, Material leather, Material bone, Material silver, Material spirit, Material pale)
    {
        BuildOrganicShaft(root, dark, leather, .043f, true);
        Band(root, "silver-collar", new Vector3(.018f, .49f, .005f), .068f, .075f, silver, 10);
        var basePoint = new Vector3(.015f, .53f, .005f);
        var tips = new[]
        {
            new Vector3(-.20f, .96f, .035f),
            new Vector3(.18f, 1.02f, -.025f),
            new Vector3(-.035f, .91f, -.20f),
            new Vector3(.06f, .88f, .18f),
        };
        for (var i = 0; i < tips.Length; i++)
        {
            var material = i == 1 ? bone : silver;
            Segment(root, "lantern-rib-" + i, basePoint + new Vector3((i - 1.5f) * .012f, 0f, 0f), tips[i], .021f, material, 8);
            Segment(root, "lantern-return-" + i, tips[i], new Vector3(.02f, .76f, .005f), .015f, material, 7);
        }
        Shard(root, "lantern-core", new Vector3(.015f, .90f, .005f), new Vector3(.20f, .42f, .20f), Quaternion.Euler(-4f, 11f, 3f), spirit, 7);
        Shard(root, "lantern-heart", new Vector3(.035f, .98f, -.015f), new Vector3(.085f, .20f, .085f), Quaternion.Euler(10f, 24f, -6f), pale, 6);
        Shard(root, "hanging-splinter", new Vector3(-.17f, .76f, .035f), new Vector3(.045f, .14f, .045f), Quaternion.Euler(18f, 0f, 23f), pale, 5);
        AddFocusLight(root, new Vector3(.02f, .92f, .005f), new Color(.28f, .96f, .80f), 1.65f, .42f);
    }

    private static void BuildAdvanced(GameObject root, Material dark, Material leather, Material bone, Material silver, Material spirit, Material pale)
    {
        BuildOrganicShaft(root, dark, leather, .046f, true);
        Band(root, "lower-silver-band", new Vector3(-.008f, .04f, -.002f), .060f, .055f, silver, 10);
        Band(root, "upper-silver-band", new Vector3(.015f, .45f, .004f), .074f, .070f, silver, 10);
        var hub = new Vector3(.015f, .57f, .005f);
        for (var i = 0; i < 6; i++)
        {
            var angle = i * Mathf.PI * 2f / 6f + .17f;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var shoulder = hub + radial * .10f + Vector3.up * .12f;
            var tip = hub + radial * (i % 2 == 0 ? .27f : .23f) + Vector3.up * (i % 2 == 0 ? .48f : .42f);
            Segment(root, "chorus-rib-a-" + i, hub, shoulder, .020f, i % 2 == 0 ? bone : silver, 8);
            Segment(root, "chorus-rib-b-" + i, shoulder, tip, .016f, i % 2 == 0 ? bone : silver, 7);
            Shard(root, "voice-" + i, tip + Vector3.up * .035f, new Vector3(.060f, .15f, .060f), Quaternion.Euler(10f * Mathf.Sin(angle), angle * Mathf.Rad2Deg, 13f * Mathf.Cos(angle)), i % 2 == 0 ? spirit : pale, 5);
        }
        Shard(root, "chorus-core", new Vector3(.015f, .88f, .005f), new Vector3(.23f, .48f, .23f), Quaternion.Euler(0f, 17f, -3f), spirit, 7);
        Shard(root, "chorus-heart", new Vector3(.025f, .99f, -.015f), new Vector3(.085f, .22f, .085f), Quaternion.Euler(8f, -12f, 4f), pale, 6);
        Segment(root, "asymmetric-bone-hook", new Vector3(-.15f, .76f, .03f), new Vector3(-.30f, .90f, .06f), .018f, bone, 7);
        Shard(root, "hook-chime", new Vector3(-.31f, .94f, .06f), new Vector3(.045f, .13f, .045f), Quaternion.Euler(16f, 9f, -17f), pale, 5);
        AddFocusLight(root, new Vector3(.015f, .91f, .005f), new Color(.42f, 1f, .86f), 1.95f, .50f);
    }

    private static void BuildMaster(GameObject root, Material dark, Material leather, Material bone, Material silver, Material spirit, Material pale)
    {
        BuildOrganicShaft(root, dark, leather, .050f, true);
        foreach (var band in new[]
        {
            new Vector3(-.012f, -.34f, 0f), new Vector3(.004f, -.08f, -.003f),
            new Vector3(-.006f, .20f, .004f), new Vector3(.018f, .47f, .005f)
        })
            Band(root, "reliquary-band-" + band.y.ToString("0.00"), band, .075f, .050f, silver, 10);

        var forkBase = new Vector3(.015f, .51f, .005f);
        var leftJoint = new Vector3(-.16f, .72f, .025f);
        var rightJoint = new Vector3(.17f, .70f, -.015f);
        Segment(root, "left-main-fork", forkBase, leftJoint, .030f, bone, 9);
        Segment(root, "right-main-fork", forkBase, rightJoint, .029f, bone, 9);
        Segment(root, "left-spire", leftJoint, new Vector3(-.28f, 1.17f, .055f), .021f, bone, 8);
        Segment(root, "right-spire", rightJoint, new Vector3(.31f, 1.10f, -.035f), .020f, silver, 8);
        Segment(root, "rear-spire", new Vector3(.015f, .58f, -.025f), new Vector3(-.03f, 1.20f, -.18f), .018f, silver, 8);

        var centre = new Vector3(.015f, .91f, .005f);
        for (var i = 0; i < 8; i++)
        {
            var angle = i * Mathf.PI * 2f / 8f + .22f;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var lower = centre + radial * .16f + Vector3.down * .18f;
            var upper = centre + radial * .26f + Vector3.up * .18f;
            Segment(root, "reliquary-rib-" + i, lower, upper, .014f, i % 3 == 0 ? bone : silver, 7);
            if (i % 2 == 0)
                Segment(root, "reliquary-brace-" + i, upper, centre + radial * .11f + Vector3.up * .29f, .011f, silver, 7);
        }

        Shard(root, "reliquary-core", centre, new Vector3(.27f, .55f, .27f), Quaternion.Euler(-3f, 21f, 2f), spirit, 8);
        Shard(root, "soul-heart", centre + new Vector3(.025f, .12f, -.015f), new Vector3(.10f, .25f, .10f), Quaternion.Euler(9f, -17f, -4f), pale, 6);
        for (var i = 0; i < 6; i++)
        {
            var angle = i * Mathf.PI * 2f / 6f + .52f;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Shard(root, "echo-shard-" + i, centre + radial * .32f + Vector3.up * (.03f + .025f * (i % 2)), new Vector3(.050f, .15f, .050f), Quaternion.Euler(14f * Mathf.Sin(angle), angle * Mathf.Rad2Deg, 18f * Mathf.Cos(angle)), i % 2 == 0 ? pale : spirit, 5);
        }
        Segment(root, "left-hanging-chain", new Vector3(-.23f, .91f, .05f), new Vector3(-.31f, .73f, .08f), .010f, silver, 6);
        Shard(root, "left-chime", new Vector3(-.315f, .69f, .08f), new Vector3(.040f, .13f, .040f), Quaternion.Euler(13f, 0f, -8f), pale, 5);
        AddFocusLight(root, centre + Vector3.up * .06f, new Color(.58f, 1f, .90f), 2.25f, .58f);
    }

    private static void BuildOrganicShaft(GameObject root, Material wood, Material leather, float radius, bool reinforced)
    {
        var points = new[]
        {
            new Vector3(-.015f, -.84f, .010f),
            new Vector3(-.030f, -.48f, .004f),
            new Vector3(.010f, -.12f, -.012f),
            new Vector3(-.012f, .22f, .013f),
            new Vector3(.018f, .53f, .005f),
        };
        for (var i = 0; i < points.Length - 1; i++)
        {
            var segmentRadius = radius * (1f - i * .075f);
            Segment(root, "shaft-" + i, points[i], points[i + 1], segmentRadius, wood, i < 2 ? 9 : 8);
        }

        Band(root, "grip-lower", new Vector3(-.022f, -.53f, .006f), radius * 1.42f, .055f, leather, 9);
        Band(root, "grip-middle", new Vector3(-.005f, -.39f, .002f), radius * 1.46f, .060f, leather, 9);
        Band(root, "grip-upper", new Vector3(.006f, -.25f, -.006f), radius * 1.40f, .050f, leather, 9);
        if (reinforced)
        {
            Segment(root, "shaft-root-spur", new Vector3(-.006f, .23f, .010f), new Vector3(-.105f, .39f, .032f), radius * .52f, wood, 7);
            Segment(root, "shaft-root-return", new Vector3(-.105f, .39f, .032f), new Vector3(.015f, .50f, .005f), radius * .40f, wood, 7);
        }
    }

    private static void Segment(GameObject root, string name, Vector3 from, Vector3 to, float radius, Material material, int sides)
    {
        var delta = to - from;
        var length = delta.magnitude;
        if (length <= .0001f) throw new InvalidOperationException($"Spirit visual segment '{name}' has zero length.");
        var rotation = Quaternion.FromToRotation(Vector3.up, delta / length);
        AddPart(root, name, CylinderMesh(sides), (from + to) * .5f, new Vector3(radius * 2f, length, radius * 2f), rotation, material);
    }

    private static void Band(GameObject root, string name, Vector3 position, float radius, float height, Material material, int sides) =>
        AddPart(root, name, CylinderMesh(sides), position, new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, material);

    private static void Shard(GameObject root, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, int sides) =>
        AddPart(root, name, PrismMesh(sides), position, scale, rotation, material);

    private static GameObject AddPart(GameObject root, string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = root.layer };
        gameObject.transform.SetParent(root.transform, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localRotation = rotation;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        return gameObject;
    }

    private static Mesh CylinderMesh(int sides)
    {
        if (CylinderMeshes.TryGetValue(sides, out var mesh)) return mesh;
        mesh = RuntimeMeshPrimitives.Cylinder(sides, $"magenheim.spirit-staff.cylinder.{sides}");
        CylinderMeshes.Add(sides, mesh);
        return mesh;
    }

    private static Mesh PrismMesh(int sides)
    {
        if (PrismMeshes.TryGetValue(sides, out var mesh)) return mesh;
        mesh = RuntimeMeshPrimitives.Prism(sides, $"magenheim.spirit-staff.prism.{sides}");
        PrismMeshes.Add(sides, mesh);
        return mesh;
    }

    private static void AddFocusLight(GameObject root, Vector3 position, Color color, float range, float intensity)
    {
        var lightObject = new GameObject("spirit-focus-light") { layer = root.layer };
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = position;
        var light = lightObject.AddComponent<Light>();
        light.color = color;
        light.range = range;
        light.intensity = intensity;
    }

    private static Material MakeMaterial(Material source, string name, Color color, float metallic, float gloss, float emission = 0f)
    {
        var material = new Material(source) { name = "magenheim.spirit." + name };
        GeneratedSurfaceTextures.Apply(material, name);
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
