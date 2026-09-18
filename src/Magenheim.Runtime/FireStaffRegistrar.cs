using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Four-tier Fire staff family. Fire owns explicit Magenheim projectile payloads and owned staff
/// bodies, escalating from direct ignition to impact burst, scorch terrain, and meteor burn zones.
/// </summary>
internal sealed class FireStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal FireStaffRegistrar(ManualLogSource log) =>
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
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Fire staff recipes are registered.");

            var fireboltBurst = StaffEffectPayloads.CreateField(
                "Magenheim_Fire_FireboltBurst",
                new HitData.DamageTypes { m_fire = 8f },
                radius: 1.8f,
                ttl: .18f,
                hitInterval: .18f,
                attackForce: 10f,
                tint: new Color(1f, .42f, .08f, 1f),
                emission: 1.55f);
            var scorchPatch = StaffEffectPayloads.CreateField(
                "Magenheim_Fire_ScorchPatch",
                new HitData.DamageTypes { m_fire = 3f },
                radius: 2.4f,
                ttl: 3f,
                hitInterval: 1f,
                attackForce: 0f,
                tint: new Color(1f, .26f, .04f, 1f),
                emission: 1.70f);
            var meteorBurn = StaffEffectPayloads.CreateField(
                "Magenheim_Fire_MeteorBurn",
                new HitData.DamageTypes { m_fire = 4f },
                radius: 3.0f,
                ttl: 4.5f,
                hitInterval: .75f,
                attackForce: 2f,
                tint: new Color(1f, .58f, .12f, 1f),
                emission: 2.0f);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Fire_EmberProjectile", new Color(1f, .34f, .06f, 1f), 1.40f),
                StaffEffectPayloads.CreateProjectile("Magenheim_Fire_FireboltProjectile", new Color(1f, .46f, .08f, 1f), 1.60f, fireboltBurst),
                StaffEffectPayloads.CreateProjectile("Magenheim_Fire_FlameburstProjectile", new Color(1f, .28f, .03f, 1f), 1.75f, scorchPatch),
                StaffEffectPayloads.CreateProjectile("Magenheim_Fire_MeteorProjectile", new Color(1f, .66f, .18f, 1f), 2.05f, meteorBurn));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Fire abilities and four owned Magenheim Fire staff bodies.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Fire staff content registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterStaff(FireStaffDefinition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_fire = definition.FireDamage };
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
        attack.m_projectileVel = definition.ProjectileVelocity;
        attack.m_projectileVelMin = definition.ProjectileVelocity;
        attack.m_projectileAccuracy = definition.ProjectileAccuracy;
        attack.m_projectileAccuracyMin = definition.ProjectileAccuracy;
        attack.m_projectiles = definition.Projectiles;
        attack.m_projectileBursts = definition.Bursts;
        attack.m_burstInterval = definition.BurstInterval;
        attack.m_perBurstResourceUsage = false;
        StaffBurstContract.Apply(attack, definition.PrefabName);

        FireStaffVisuals.Apply(item.ItemPrefab, definition.PrefabName);

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Fire staff item '{definition.PrefabName}'.");
    }

    private static void RegisterRecipe(FireStaffDefinition definition)
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
            throw new InvalidOperationException($"Jotunn refused Fire staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<FireStaffDefinition> Definitions() => new[]
    {
        new FireStaffDefinition(
            "Magenheim_Staff_Fire_Simple", "staff-fire-simple", "Simple Staff of Fire",
            "Ember Dart: fires one compact Magenheim ember with no secondary blast. It is the clean stamina-only introduction to direct Fire crystal damage.",
            1, 24f, 20f, 1f, 28f, 1.5f, 1, 1, 0f, PayloadKind.Ember,
            new StaffRequirement("FineWood", 10), new StaffRequirement("Bronze", 2), new StaffRequirement("Magenheim_Crystal_Fire_Simple", 1)),
        new FireStaffDefinition(
            "Magenheim_Staff_Fire_Crystal", "staff-fire-crystal", "Crystal Staff of Fire",
            "Firebolt: drives a dense bolt into one target and blooms into a compact 1.8-meter fireburst on impact, rewarding accurate shots against clustered enemies.",
            2, 34f, 36f, 1f, 38f, .8f, 1, 1, 0f, PayloadKind.Firebolt,
            new StaffRequirement("ElderBark", 10), new StaffRequirement("Iron", 2), new StaffRequirement("Magenheim_Crystal_Fire_Crystal", 1)),
        new FireStaffDefinition(
            "Magenheim_Staff_Fire_Advanced", "staff-fire-advanced", "Advanced Staff of Fire",
            "Flameburst: throws three unstable bolts across a fan. Every impact leaves three seconds of burning ground, so the spell pressures movement rather than ending at the initial hit.",
            3, 32f, 14f, .78f, 31f, 7f, 3, 1, 0f, PayloadKind.Flameburst,
            new StaffRequirement("FineWood", 12), new StaffRequirement("Silver", 3), new StaffRequirement("BlackMetal", 2), new StaffRequirement("Magenheim_Crystal_Fire_Advanced", 1)),
        new FireStaffDefinition(
            "Magenheim_Staff_Fire_Master", "staff-fire-master", "Master Staff of Fire",
            "Meteorfall: releases three waves of three meteoric bolts. Every impact leaves a wide four-and-a-half-second burn zone, turning the barrage into persistent incendiary terrain instead of nine disconnected fireballs.",
            4, 48f, 9f, .70f, 34f, 9f, 3, 3, .18f, PayloadKind.Meteor,
            new StaffRequirement("YggdrasilWood", 15), new StaffRequirement("BlackMetal", 4), new StaffRequirement("Magenheim_Crystal_Fire_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Ember, Firebolt, Flameburst, Meteor }

    private sealed class PayloadSet
    {
        private readonly GameObject _ember;
        private readonly GameObject _firebolt;
        private readonly GameObject _flameburst;
        private readonly GameObject _meteor;

        internal PayloadSet(GameObject ember, GameObject firebolt, GameObject flameburst, GameObject meteor)
        {
            _ember = ember; _firebolt = firebolt; _flameburst = flameburst; _meteor = meteor;
        }

        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Ember => _ember,
            PayloadKind.Firebolt => _firebolt,
            PayloadKind.Flameburst => _flameburst,
            PayloadKind.Meteor => _meteor,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct StaffRequirement
    {
        internal StaffRequirement(string prefabName, int amount) { PrefabName = prefabName; Amount = amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class FireStaffDefinition
    {
        internal FireStaffDefinition(
            string prefabName, string assetName, string displayName, string description, int minimumStationLevel,
            float staminaCost, float fireDamage, float damageMultiplier,
            float projectileVelocity, float projectileAccuracy, int projectiles, int bursts,
            float burstInterval, PayloadKind payload, params StaffRequirement[] requirements)
        {
            PrefabName = prefabName; AssetName = assetName; DisplayName = displayName; Description = description; MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost; FireDamage = fireDamage; DamageMultiplier = damageMultiplier;
            ProjectileVelocity = projectileVelocity; ProjectileAccuracy = projectileAccuracy; Projectiles = projectiles;
            Bursts = bursts; BurstInterval = burstInterval; Payload = payload; Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float FireDamage { get; }
        internal float DamageMultiplier { get; }
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal int Projectiles { get; }
        internal int Bursts { get; }
        internal float BurstInterval { get; }
        internal PayloadKind Payload { get; }
        internal IReadOnlyList<StaffRequirement> Requirements { get; }
    }
}
