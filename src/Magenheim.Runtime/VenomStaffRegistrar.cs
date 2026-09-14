using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Magenheim.Runtime;

/// <summary>
/// Playable Venom staff family built around Valheim's native Ooze Bomb poison-field projectile.
/// Venom's combat language is contamination: low impact, repeated poison exposure, overlapping
/// lingering fields, and increasingly aggressive area denial rather than direct burst damage.
/// </summary>
internal sealed class VenomStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffFireball";
    private const string PoisonCarrierPrefab = "BombOoze";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal VenomStaffRegistrar(ManualLogSource log) =>
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
            var poisonCarrier = PrefabManager.Instance.GetPrefab(PoisonCarrierPrefab)
                ?? throw new InvalidOperationException($"Required poison carrier prefab '{PoisonCarrierPrefab}' is unavailable.");
            var poisonDrop = poisonCarrier.GetComponent<ItemDrop>()
                ?? throw new InvalidOperationException($"Poison carrier prefab '{PoisonCarrierPrefab}' has no ItemDrop component.");
            var poisonAttack = poisonDrop.m_itemData.m_shared.m_attack;

            if (PrefabManager.Instance.GetPrefab(BaseStaffPrefab) is null)
                throw new InvalidOperationException($"Required staff carrier prefab '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Venom staff recipes are registered.");

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, poisonAttack);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered the four-tier Venom staff family: Toxic Glob, Caustic Pool, Miasma Bloom, and Plaguefield.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Venom staff content registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterStaff(VenomStaffDefinition definition, Attack poisonAttack)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Venom staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_poison = definition.PoisonDamage };

        // Keep a proper staff animation/handling surface, but replace the fireball payload with
        // BombOoze's native poison-field projectile. That carrier creates the real persistent
        // contamination zone; this family changes how many zones are deployed and where.
        var attack = shared.m_attack;
        attack.m_attackProjectile = poisonAttack.m_attackProjectile;
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
            throw new InvalidOperationException($"Jotunn refused Venom staff item '{definition.PrefabName}'.");
    }

    private static void RegisterRecipe(VenomStaffDefinition definition)
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
            throw new InvalidOperationException($"Jotunn refused Venom staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<VenomStaffDefinition> Definitions() => new[]
    {
        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Simple",
            "Simple Staff of Venom",
            "Toxic Glob: hurls one unstable mass that ruptures into a lingering poison field. The impact itself is weak; controlling a doorway or forcing movement is the real weapon.",
            minimumStationLevel: 1,
            staminaCost: 20f,
            eitrCost: 0f,
            poisonDamage: 16f,
            damageMultiplier: 0.55f,
            forceMultiplier: 0.25f,
            staggerMultiplier: 0.30f,
            projectileVelocity: 22f,
            projectileAccuracy: 2f,
            projectiles: 1,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("FineWood", 8),
            new StaffRequirement("Ooze", 4),
            new StaffRequirement("Magenheim_Crystal_Venom_Simple", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Crystal",
            "Crystal Staff of Venom",
            "Caustic Pool: fires two closely timed globules down the same lane. Repeated contamination does not invent a new poison stack; it refreshes pressure and keeps the target area dangerous longer.",
            minimumStationLevel: 2,
            staminaCost: 30f,
            eitrCost: 0f,
            poisonDamage: 24f,
            damageMultiplier: 0.72f,
            forceMultiplier: 0.20f,
            staggerMultiplier: 0.25f,
            projectileVelocity: 28f,
            projectileAccuracy: 1.2f,
            projectiles: 1,
            bursts: 2,
            burstInterval: 0.28f,
            new StaffRequirement("ElderBark", 10),
            new StaffRequirement("Guck", 4),
            new StaffRequirement("Ooze", 6),
            new StaffRequirement("Magenheim_Crystal_Venom_Crystal", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Advanced",
            "Advanced Staff of Venom",
            "Miasma Bloom: throws three contamination globes across a broad fan. Each impact seeds a separate poison field, turning clustered ground into a corrosive attrition zone instead of chasing immediate burst damage.",
            minimumStationLevel: 3,
            staminaCost: 12f,
            eitrCost: 18f,
            poisonDamage: 30f,
            damageMultiplier: 0.46f,
            forceMultiplier: 0.15f,
            staggerMultiplier: 0.20f,
            projectileVelocity: 24f,
            projectileAccuracy: 11f,
            projectiles: 3,
            bursts: 1,
            burstInterval: 0f,
            new StaffRequirement("YggdrasilWood", 10),
            new StaffRequirement("Guck", 6),
            new StaffRequirement("Bilebag", 2),
            new StaffRequirement("Magenheim_Crystal_Venom_Advanced", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Master",
            "Master Staff of Venom",
            "Plaguefield: saturates a wide front with three waves of three poison globes. Nine lingering fields overlap into hostile terrain that punishes anything attempting to hold or cross the contaminated space.",
            minimumStationLevel: 4,
            staminaCost: 0f,
            eitrCost: 48f,
            poisonDamage: 34f,
            damageMultiplier: 0.30f,
            forceMultiplier: 0.10f,
            staggerMultiplier: 0.15f,
            projectileVelocity: 26f,
            projectileAccuracy: 16f,
            projectiles: 3,
            bursts: 3,
            burstInterval: 0.22f,
            new StaffRequirement("YggdrasilWood", 15),
            new StaffRequirement("Guck", 10),
            new StaffRequirement("Bilebag", 4),
            new StaffRequirement("Eitr", 10),
            new StaffRequirement("Magenheim_Crystal_Venom_Master", 1)),
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

    private sealed class VenomStaffDefinition
    {
        internal VenomStaffDefinition(
            string prefabName,
            string displayName,
            string description,
            int minimumStationLevel,
            float staminaCost,
            float eitrCost,
            float poisonDamage,
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
            PoisonDamage = poisonDamage;
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
        internal float PoisonDamage { get; }
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
