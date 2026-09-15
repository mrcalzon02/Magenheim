using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Four-tier Storm staff family with owned Magenheim staff bodies. Advanced tiers create real
/// secondary discharge areas at impact so Storm remains about force, chaining, and burst geometry.
/// </summary>
internal sealed class StormStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";
    private const string LightningProjectileSource = "StaffLightning";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal StormStaffRegistrar(ManualLogSource log) =>
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
                throw new InvalidOperationException($"Required hidden staff carrier prefab '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(LightningProjectileSource) is null)
                throw new InvalidOperationException($"Required lightning projectile source '{LightningProjectileSource}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Storm staff recipes are registered.");

            var arcDischarge = StaffEffectPayloads.CreateField(
                "Magenheim_Storm_ArcDischarge",
                new HitData.DamageTypes { m_lightning = 9f },
                radius: 4.5f,
                ttl: .12f,
                hitInterval: .12f,
                attackForce: 22f,
                tint: new Color(.46f, .74f, 1f, 1f),
                emission: 1.75f);
            var thunderheadPulse = StaffEffectPayloads.CreateField(
                "Magenheim_Storm_ThunderheadPulse",
                new HitData.DamageTypes { m_lightning = 4f },
                radius: 3.2f,
                ttl: .35f,
                hitInterval: .17f,
                attackForce: 14f,
                tint: new Color(.70f, .88f, 1f, 1f),
                emission: 1.95f);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Storm_StaticProjectile", new Color(.28f, .62f, 1f, 1f), 1.45f, sourceStaffPrefab: LightningProjectileSource),
                StaffEffectPayloads.CreateProjectile("Magenheim_Storm_ThunderboltProjectile", new Color(.50f, .78f, 1f, 1f), 1.65f, sourceStaffPrefab: LightningProjectileSource),
                StaffEffectPayloads.CreateProjectile("Magenheim_Storm_ArcProjectile", new Color(.58f, .82f, 1f, 1f), 1.80f, arcDischarge, LightningProjectileSource),
                StaffEffectPayloads.CreateProjectile("Magenheim_Storm_ThunderheadProjectile", new Color(.78f, .92f, 1f, 1f), 2.05f, thunderheadPulse, LightningProjectileSource));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Storm abilities and four owned Magenheim Storm staff bodies.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Storm staff content registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterStaff(StormStaffDefinition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Storm staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_lightning = definition.LightningDamage };
        shared.m_damagesPerLevel = new HitData.DamageTypes();

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

        StormStaffVisuals.Apply(item.ItemPrefab, definition.PrefabName);

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Storm staff item '{definition.PrefabName}'.");
    }

    private static void RegisterRecipe(StormStaffDefinition definition)
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
            throw new InvalidOperationException($"Jotunn refused Storm staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<StormStaffDefinition> Definitions() => new[]
    {
        new StormStaffDefinition(
            "Magenheim_Staff_Storm_Simple",
            "Simple Staff of Storm",
            "Static Snap: discharges one short electrical packet. Its direct lightning damage is modest, but the violent impulse is built to interrupt and knock enemies away without spending Eitr.",
            1, 20f, 18f, 1f, 1.55f, 1.20f, 34f, 2f, 1, 1, 0f, PayloadKind.Static,
            new StaffRequirement("FineWood", 8), new StaffRequirement("Bronze", 2), new StaffRequirement("Magenheim_Crystal_Storm_Simple", 1)),

        new StormStaffDefinition(
            "Magenheim_Staff_Storm_Crystal",
            "Crystal Staff of Storm",
            "Thunderbolt: compresses the discharge into one nearly straight, high-velocity bolt with heavy force and stagger. It remains a deliberate single-target strike rather than a spread spell.",
            2, 30f, 42f, 1f, 2.10f, 1.55f, 58f, .35f, 1, 1, 0f, PayloadKind.Thunderbolt,
            new StaffRequirement("ElderBark", 10), new StaffRequirement("Iron", 2), new StaffRequirement("Magenheim_Crystal_Storm_Crystal", 1)),

        new StormStaffDefinition(
            "Magenheim_Staff_Storm_Advanced",
            "Advanced Staff of Storm",
            "Arc Chain: fires two successive electrical leaders. Every impact erupts into a 4.5-meter secondary discharge, so the struck target becomes the origin of real lightning pressure against nearby clustered enemies.",
            3, 32f, 16f, .85f, 1.05f, 1.00f, 44f, 2.5f, 1, 2, .13f, PayloadKind.Arc,
            new StaffRequirement("YggdrasilWood", 10), new StaffRequirement("Silver", 3), new StaffRequirement("BlackMetal", 2), new StaffRequirement("Magenheim_Crystal_Storm_Advanced", 1)),

        new StormStaffDefinition(
            "Magenheim_Staff_Storm_Master",
            "Master Staff of Storm",
            "Thunderhead: throws three branches through three rapid waves. Each impact seeds a short two-pulse lightning burst around itself, creating overlapping electrical pressure and knockback across a broad front.",
            4, 50f, 10f, .60f, 1.25f, 1.10f, 40f, 13f, 3, 3, .13f, PayloadKind.Thunderhead,
            new StaffRequirement("YggdrasilWood", 15), new StaffRequirement("BlackMetal", 5), new StaffRequirement("Magenheim_Crystal_Storm_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Static, Thunderbolt, Arc, Thunderhead }

    private sealed class PayloadSet
    {
        private readonly GameObject _static;
        private readonly GameObject _thunderbolt;
        private readonly GameObject _arc;
        private readonly GameObject _thunderhead;

        internal PayloadSet(GameObject staticPayload, GameObject thunderbolt, GameObject arc, GameObject thunderhead)
        {
            _static = staticPayload;
            _thunderbolt = thunderbolt;
            _arc = arc;
            _thunderhead = thunderhead;
        }

        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Static => _static,
            PayloadKind.Thunderbolt => _thunderbolt,
            PayloadKind.Arc => _arc,
            PayloadKind.Thunderhead => _thunderhead,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct StaffRequirement
    {
        internal StaffRequirement(string prefabName, int amount)
        {
            PrefabName = prefabName;
            Amount = amount;
        }

        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class StormStaffDefinition
    {
        internal StormStaffDefinition(
            string prefabName,
            string displayName,
            string description,
            int minimumStationLevel,
            float staminaCost,
            float lightningDamage,
            float damageMultiplier,
            float forceMultiplier,
            float staggerMultiplier,
            float projectileVelocity,
            float projectileAccuracy,
            int projectiles,
            int bursts,
            float burstInterval,
            PayloadKind payload,
            params StaffRequirement[] requirements)
        {
            PrefabName = prefabName;
            DisplayName = displayName;
            Description = description;
            MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost;
            LightningDamage = lightningDamage;
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
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float LightningDamage { get; }
        internal float DamageMultiplier { get; }
        internal float ForceMultiplier { get; }
        internal float StaggerMultiplier { get; }
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal int Projectiles { get; }
        internal int Bursts { get; }
        internal float BurstInterval { get; }
        internal PayloadKind Payload { get; }
        internal IReadOnlyList<StaffRequirement> Requirements { get; }
    }
}
