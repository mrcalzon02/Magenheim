using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Magenheim.Runtime;

/// <summary>
/// Playable Storm staff family built on Valheim's native StaffLightning / Dundr electrical attack.
/// Storm's combat language is discharge geometry: cheap knockback, focused charge, sequential
/// forked arcs, and a broad repeated thunderhead burst rather than fire-style damage scaling.
/// </summary>
internal sealed class StormStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffLightning";

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
                throw new InvalidOperationException($"Required vanilla lightning staff prefab '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Storm staff recipes are registered.");

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered the four-tier Storm staff family: Static Snap, Thunderbolt, Arc Chain, and Thunderhead.");
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

    private static void RegisterStaff(StormStaffDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Storm staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        // Preserve Dundr's native lightning projectile and hit effects. Storm differentiation
        // comes from how electrical packets are emitted and how much force each packet carries.
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
            "Static Snap: discharges a short, violent electrical packet. Damage is modest, but the sudden impulse throws enemies off their footing and creates breathing room without Eitr.",
            minimumStationLevel: 1,
            staminaCost: 20f,
            eitrCost: 0f,
            damageMultiplier: 0.34f,
            forceMultiplier: 1.55f,
            staggerMultiplier: 1.20f,
            projectileVelocity: 34f,
            projectileAccuracy: 2.0f,
            projectiles: 1,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("FineWood", 8),
            new StaffRequirement("Bronze", 2),
            new StaffRequirement("Magenheim_Crystal_Storm_Simple", 1)),

        new StormStaffDefinition(
            "Magenheim_Staff_Storm_Crystal",
            "Crystal Staff of Storm",
            "Thunderbolt: holds charge for one dense discharge and drives it down a narrow line. The bolt is fast, forceful, and built to punch one target backward rather than scatter damage.",
            minimumStationLevel: 2,
            staminaCost: 30f,
            eitrCost: 0f,
            damageMultiplier: 0.92f,
            forceMultiplier: 2.10f,
            staggerMultiplier: 1.55f,
            projectileVelocity: 58f,
            projectileAccuracy: 0.35f,
            projectiles: 1,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("ElderBark", 10),
            new StaffRequirement("Iron", 2),
            new StaffRequirement("Magenheim_Crystal_Storm_Crystal", 1)),

        new StormStaffDefinition(
            "Magenheim_Staff_Storm_Advanced",
            "Advanced Staff of Storm",
            "Arc Chain: releases three closely spaced forked discharges. Each pulse throws two electrical branches across a narrow cone, creating chain-like pressure through clustered enemies instead of one oversized hit.",
            minimumStationLevel: 3,
            staminaCost: 12f,
            eitrCost: 20f,
            damageMultiplier: 0.33f,
            forceMultiplier: 1.10f,
            staggerMultiplier: 1.00f,
            projectileVelocity: 42f,
            projectileAccuracy: 6f,
            projectiles: 2,
            bursts: 3,
            burstInterval: 0.10f,
            new StaffRequirement("YggdrasilWood", 10),
            new StaffRequirement("Silver", 3),
            new StaffRequirement("BlackMetal", 2),
            new StaffRequirement("Magenheim_Crystal_Storm_Advanced", 1)),

        new StormStaffDefinition(
            "Magenheim_Staff_Storm_Master",
            "Master Staff of Storm",
            "Thunderhead: tears open a broad electrical front. Four rapid pulses each throw five lightning branches across a wide cone, battering a group with repeated knockback and overlapping discharge paths.",
            minimumStationLevel: 4,
            staminaCost: 0f,
            eitrCost: 50f,
            damageMultiplier: 0.19f,
            forceMultiplier: 1.35f,
            staggerMultiplier: 1.15f,
            projectileVelocity: 38f,
            projectileAccuracy: 15f,
            projectiles: 5,
            bursts: 4,
            burstInterval: 0.12f,
            new StaffRequirement("YggdrasilWood", 15),
            new StaffRequirement("BlackMetal", 5),
            new StaffRequirement("Eitr", 12),
            new StaffRequirement("Magenheim_Crystal_Storm_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
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
            float eitrCost,
            float damageMultiplier,
            float forceMultiplier,
            float staggerMultiplier,
            float projectileVelocity,
            float projectileAccuracy,
            int projectiles,
            int bursts,
            float burstInterval,
            params StaffRequirement[] requirements)
        {
            PrefabName = prefabName;
            DisplayName = displayName;
            Description = description;
            MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost;
            EitrCost = eitrCost;
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
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float EitrCost { get; }
        internal float DamageMultiplier { get; }
        internal float ForceMultiplier { get; }
        internal float StaggerMultiplier { get; }
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal int Projectiles { get; }
        internal int Bursts { get; }
        internal float BurstInterval { get; }
        internal IReadOnlyList<StaffRequirement> Requirements { get; }
    }
}
