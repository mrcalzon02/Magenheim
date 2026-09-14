using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Magenheim.Runtime;

/// <summary>
/// Playable Frost staff family built on Valheim's native StaffIceShards projectile/status path.
/// Each tier deliberately changes cast topology rather than only scaling damage.
/// </summary>
internal sealed class FrostStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal FrostStaffRegistrar(ManualLogSource log) =>
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
                throw new InvalidOperationException($"Required vanilla frost staff prefab '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Frost staff recipes are registered.");

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered the four-tier Frost staff family: Water Dart, Frost Lance, Ice Volley, and Rime Torrent.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Frost staff content registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterStaff(FrostStaffDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Frost staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        // Keep the native frost projectile and hit-status behavior, but reshape the cast itself.
        // This preserves real frost buildup while giving every Magenheim tier a distinct combat role.
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
            throw new InvalidOperationException($"Jotunn refused Frost staff item '{definition.PrefabName}'.");
    }

    private static void RegisterRecipe(FrostStaffDefinition definition)
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
            throw new InvalidOperationException($"Jotunn refused Frost staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<FrostStaffDefinition> Definitions() => new[]
    {
        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Simple",
            "Simple Staff of Frost",
            "Water Dart: condenses a water-heavy shard and snaps it forward. The impact carries a weak but genuine freezing bite and costs only stamina.",
            minimumStationLevel: 1,
            staminaCost: 18f,
            eitrCost: 0f,
            damageMultiplier: 0.30f,
            forceMultiplier: 0.35f,
            staggerMultiplier: 0.50f,
            projectileVelocity: 24f,
            projectileAccuracy: 2.5f,
            projectiles: 1,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("FineWood", 8),
            new StaffRequirement("FreezeGland", 2),
            new StaffRequirement("Magenheim_Crystal_Frost_Simple", 1)),

        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Crystal",
            "Crystal Staff of Frost",
            "Frost Lance: compresses the cast into one long, precise ice spike. It hits hard, travels exceptionally fast, and carries substantially more force and stagger.",
            minimumStationLevel: 2,
            staminaCost: 28f,
            eitrCost: 0f,
            damageMultiplier: 1.05f,
            forceMultiplier: 1.60f,
            staggerMultiplier: 1.35f,
            projectileVelocity: 55f,
            projectileAccuracy: 0.4f,
            projectiles: 1,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("ElderBark", 10),
            new StaffRequirement("Silver", 3),
            new StaffRequirement("Magenheim_Crystal_Frost_Crystal", 1)),

        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Advanced",
            "Advanced Staff of Frost",
            "Ice Volley: breaks one shaping impulse into a five-shard fan. Individual shards are lighter, but the spread can blanket a target or freeze a clustered advance.",
            minimumStationLevel: 3,
            staminaCost: 12f,
            eitrCost: 16f,
            damageMultiplier: 0.34f,
            forceMultiplier: 0.80f,
            staggerMultiplier: 0.80f,
            projectileVelocity: 36f,
            projectileAccuracy: 12f,
            projectiles: 5,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("YggdrasilWood", 10),
            new StaffRequirement("Silver", 4),
            new StaffRequirement("FreezeGland", 4),
            new StaffRequirement("Magenheim_Crystal_Frost_Advanced", 1)),

        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Master",
            "Master Staff of Frost",
            "Rime Torrent: opens the crystal continuously for a heartbeat, pouring paired ice shards through eight rapid pulses. The stream sacrifices per-hit force for relentless frost pressure.",
            minimumStationLevel: 4,
            staminaCost: 0f,
            eitrCost: 42f,
            damageMultiplier: 0.16f,
            forceMultiplier: 0.65f,
            staggerMultiplier: 0.70f,
            projectileVelocity: 40f,
            projectileAccuracy: 7f,
            projectiles: 2,
            bursts: 8,
            burstInterval: 0.09f,
            new StaffRequirement("YggdrasilWood", 15),
            new StaffRequirement("BlackMetal", 4),
            new StaffRequirement("Eitr", 10),
            new StaffRequirement("FreezeGland", 6),
            new StaffRequirement("Magenheim_Crystal_Frost_Master", 1)),
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

    private sealed class FrostStaffDefinition
    {
        internal FrostStaffDefinition(
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
