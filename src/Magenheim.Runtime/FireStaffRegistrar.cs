using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;

namespace Magenheim.Runtime;

/// <summary>
/// First playable staff content family. Fire intentionally starts from Valheim's proven
/// StaffFireball attack/projectile so these are immediately usable weapons rather than
/// inventory-only placeholders. Each tier changes resource economy and cast behavior.
/// </summary>
internal sealed class FireStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffFireball";

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
                throw new InvalidOperationException($"Required vanilla staff prefab '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim staff recipes are registered.");

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered the four-tier Fire staff family: Ember Dart, Firebolt, Flameburst, and Meteorfall barrage.");
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

    private static void RegisterStaff(FireStaffDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        // StaffFireball already supplies the animation, projectile, impact behavior, skill
        // channel and VFX. Alter only the cloned attack instance for this Magenheim tier.
        var attack = shared.m_attack;
        attack.m_attackStamina = definition.StaminaCost;
        attack.m_attackEitr = definition.EitrCost;
        attack.m_damageMultiplier = definition.DamageMultiplier;
        attack.m_projectileVel = definition.ProjectileVelocity;
        attack.m_projectileVelMin = definition.ProjectileVelocity;
        attack.m_projectileAccuracy = definition.ProjectileAccuracy;
        attack.m_projectileAccuracyMin = definition.ProjectileAccuracy;
        attack.m_projectiles = definition.Projectiles;
        attack.m_projectileBursts = definition.Bursts;
        attack.m_burstInterval = definition.BurstInterval;
        attack.m_perBurstResourceUsage = false;

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
            "Magenheim_Staff_Fire_Simple",
            "Simple Staff of Fire",
            "Ember Dart: a quick, single ember cast powered entirely by stamina. The first deliberate weaponization of a shaped Fire crystal.",
            minimumStationLevel: 1,
            staminaCost: 24f,
            eitrCost: 0f,
            damageMultiplier: 0.45f,
            projectileVelocity: 28f,
            projectileAccuracy: 1.5f,
            projectiles: 1,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("FineWood", 10),
            new StaffRequirement("Bronze", 2),
            new StaffRequirement("Magenheim_Crystal_Fire_Simple", 1)),

        new FireStaffDefinition(
            "Magenheim_Staff_Fire_Crystal",
            "Crystal Staff of Fire",
            "Firebolt: a denser and faster bolt driven by a fully shaped Fire crystal. It still relies on stamina rather than Eitr.",
            minimumStationLevel: 2,
            staminaCost: 34f,
            eitrCost: 0f,
            damageMultiplier: 0.78f,
            projectileVelocity: 36f,
            projectileAccuracy: 1f,
            projectiles: 1,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("ElderBark", 10),
            new StaffRequirement("Iron", 2),
            new StaffRequirement("Magenheim_Crystal_Fire_Crystal", 1)),

        new FireStaffDefinition(
            "Magenheim_Staff_Fire_Advanced",
            "Advanced Staff of Fire",
            "Flameburst: an unstable three-bolt fan. Advanced shaping begins bridging mortal stamina and magical Eitr.",
            minimumStationLevel: 3,
            staminaCost: 14f,
            eitrCost: 18f,
            damageMultiplier: 0.48f,
            projectileVelocity: 31f,
            projectileAccuracy: 7f,
            projectiles: 3,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("FineWood", 12),
            new StaffRequirement("Silver", 3),
            new StaffRequirement("BlackMetal", 2),
            new StaffRequirement("Magenheim_Crystal_Fire_Advanced", 1)),

        new FireStaffDefinition(
            "Magenheim_Staff_Fire_Master",
            "Master Staff of Fire",
            "Meteorfall: a sustained barrage of incandescent projectiles. Master shaping abandons mundane exertion and feeds the cast directly with Eitr.",
            minimumStationLevel: 4,
            staminaCost: 0f,
            eitrCost: 46f,
            damageMultiplier: 0.58f,
            projectileVelocity: 34f,
            projectileAccuracy: 9f,
            projectiles: 3,
            bursts: 3,
            burstInterval: 0.18f,
            new StaffRequirement("YggdrasilWood", 15),
            new StaffRequirement("BlackMetal", 4),
            new StaffRequirement("Eitr", 10),
            new StaffRequirement("Magenheim_Crystal_Fire_Master", 1)),
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

    private sealed class FireStaffDefinition
    {
        internal FireStaffDefinition(
            string prefabName,
            string displayName,
            string description,
            int minimumStationLevel,
            float staminaCost,
            float eitrCost,
            float damageMultiplier,
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
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal int Projectiles { get; }
        internal int Bursts { get; }
        internal float BurstInterval { get; }
        internal IReadOnlyList<StaffRequirement> Requirements { get; }
    }
}
