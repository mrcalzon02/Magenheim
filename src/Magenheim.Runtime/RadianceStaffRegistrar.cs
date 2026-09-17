using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class RadianceStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal RadianceStaffRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

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
                throw new InvalidOperationException("Geologist's Workstation must exist before Radiance staff recipes are registered.");

            var flash = StaffEffectPayloads.CreateField(
                "Magenheim_Radiance_Flash", new HitData.DamageTypes { m_spirit = 2f },
                2.2f, .20f, .20f, 28f, new Color(1f, .93f, .52f, 1f), 1.65f);
            var sanctuary = StaffEffectPayloads.CreateField(
                "Magenheim_Radiance_Sanctuary", new HitData.DamageTypes { m_spirit = 6f },
                5.5f, 10f, 1f, 0f, new Color(1f, .98f, .78f, 1f), 1.85f);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Radiance_Projectile", new Color(1f, .90f, .38f, 1f), 1.45f, sourceStaffPrefab: BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Radiance_FlashProjectile", new Color(1f, .96f, .64f, 1f), 1.70f, flash, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Radiance_DaybreakProjectile", new Color(1f, .99f, .84f, 1f), 1.95f, sanctuary, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Radiance abilities with owned Radiance staff bodies and stamina-only casting.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Radiance staff content registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static void RegisterStaff(Definition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Radiance staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_pierce = definition.PierceDamage, m_spirit = definition.SpiritDamage };
        shared.m_damagesPerLevel = new HitData.DamageTypes();
        shared.m_icons = new[] { EarthAssets.Icon(definition.AssetName) };

        // StaffIceShards spends Eitr rather than durability, so a clone inherits none and
        // never degrades. The Eitr economy was removed from Magenheim staves, so durability
        // is what keeps them in the ordinary wear-and-repair loop with every other weapon.
        if (!CrystalStaffDurability.TryResolveTier(definition.PrefabName, out var staffTier))
            throw new InvalidOperationException($"Staff identity '{definition.PrefabName}' does not resolve a canonical crystal tier.");
        shared.m_useDurability = true;
        shared.m_maxDurability = CrystalStaffDurability.ForTier(staffTier);
        shared.m_durabilityPerLevel = 0f;
        shared.m_durabilityDrain = 0f;
        shared.m_useDurabilityDrain = 0f;

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

        RadianceVisuals.Apply(item.ItemPrefab, definition.AssetName);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Radiance staff item '{definition.PrefabName}'.");
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
        foreach (var requirement in definition.Requirements) config.AddRequirement(requirement.PrefabName, requirement.Amount);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException($"Jotunn refused Radiance staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<Definition> Definitions() => new[]
    {
        new Definition("Magenheim_Staff_Radiance_Simple", "staff-radiance-simple", "Simple Staff of Radiance",
            "Prism Spark: fires a needle of white-gold hard light. Pierce harms ordinary flesh while Spirit damage naturally punishes undead and other spirit-vulnerable corruption.",
            1, 20f, 8f, 10f, 1f, .35f, .85f, 45f, .35f, 1, 1, 0f, PayloadKind.Direct, new Color(.96f,.82f,.32f,1f),
            new Requirement("FineWood",8), new Requirement("Bronze",2), new Requirement("Magenheim_Crystal_Radiance_Simple",1)),
        new Definition("Magenheim_Staff_Radiance_Crystal", "staff-radiance-crystal", "Crystal Staff of Radiance",
            "Sun Lance: compresses Radiance into a nearly dispersionless line. High Pierce, heavy Spirit pressure, extreme velocity, and focused stagger reward deliberate aim.",
            2, 30f, 18f, 30f, 1f, 1.10f, 1.55f, 72f, .10f, 1, 1, 0f, PayloadKind.Direct, new Color(1f,.90f,.48f,1f),
            new Requirement("ElderBark",10), new Requirement("Silver",3), new Requirement("Magenheim_Crystal_Radiance_Crystal",1)),
        new Definition("Magenheim_Staff_Radiance_Advanced", "staff-radiance-advanced", "Advanced Staff of Radiance",
            "Corona Flash: fractures one cast into seven rays. Each impact blooms into a brief radiant flash, adding local Spirit pressure and violent interruption around clustered targets.",
            3, 30f, 4f, 8f, .55f, .55f, 1.35f, 48f, 9f, 7, 1, 0f, PayloadKind.Flash, new Color(1f,.95f,.68f,1f),
            new Requirement("YggdrasilWood",10), new Requirement("Silver",4), new Requirement("BlackMetal",2), new Requirement("Magenheim_Crystal_Radiance_Advanced",1)),
        new Definition("Magenheim_Staff_Radiance_Master", "staff-radiance-master", "Master Staff of Radiance",
            "Daybreak Sanctuary: drives one sun-bright lance into the target point and leaves a wide sanctified field for ten seconds. The field deals pure Spirit damage, making corrupted ground lethal to spirit-vulnerable enemies rather than becoming another artillery barrage.",
            4, 50f, 18f, 38f, 1f, 1.0f, 1.80f, 66f, .12f, 1, 1, 0f, PayloadKind.Sanctuary, new Color(1f,.99f,.84f,1f),
            new Requirement("YggdrasilWood",15), new Requirement("BlackMetal",4), new Requirement("Magenheim_Crystal_Radiance_Master",1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Direct, Flash, Sanctuary }

    private sealed class PayloadSet
    {
        private readonly GameObject _direct, _flash, _sanctuary;
        internal PayloadSet(GameObject direct, GameObject flash, GameObject sanctuary) { _direct=direct; _flash=flash; _sanctuary=sanctuary; }
        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Direct => _direct, PayloadKind.Flash => _flash, PayloadKind.Sanctuary => _sanctuary,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct Requirement
    {
        internal Requirement(string prefabName, int amount) { PrefabName=prefabName; Amount=amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class Definition
    {
        internal Definition(string prefabName, string assetName, string displayName, string description, int minimumStationLevel,
            float staminaCost, float pierceDamage, float spiritDamage, float damageMultiplier, float forceMultiplier,
            float staggerMultiplier, float projectileVelocity, float projectileAccuracy, int projectiles, int bursts, float burstInterval,
            PayloadKind payload, Color tint, params Requirement[] requirements)
        {
            PrefabName=prefabName; AssetName=assetName; DisplayName=displayName; Description=description; MinimumStationLevel=minimumStationLevel;
            StaminaCost=staminaCost; PierceDamage=pierceDamage; SpiritDamage=spiritDamage; DamageMultiplier=damageMultiplier;
            ForceMultiplier=forceMultiplier; StaggerMultiplier=staggerMultiplier; ProjectileVelocity=projectileVelocity; ProjectileAccuracy=projectileAccuracy;
            Projectiles=projectiles; Bursts=bursts; BurstInterval=burstInterval; Payload=payload; Tint=tint; Requirements=requirements;
        }
        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float PierceDamage { get; }
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
        internal Color Tint { get; }
        internal IReadOnlyList<Requirement> Requirements { get; }
    }
}

