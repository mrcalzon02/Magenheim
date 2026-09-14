using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Magenheim.Runtime;

/// <summary>
/// Playable Venom staff family built around Valheim's native Ooze Bomb poison-field projectile.
/// Each tier carries original Magenheim geometry/iconography while escalating from a single toxic
/// glob to broad persistent contamination rather than direct burst damage.
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
            _log.LogInfo("Registered the four-tier Venom staff family with original geometry/icons: Toxic Glob, Caustic Pool, Miasma Bloom, and Plaguefield.");
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
        shared.m_icons = new[] { EarthAssets.Icon(definition.AssetName) };

        // Keep a proper staff animation/handling surface, but replace the fireball payload with
        // BombOoze's native poison-field projectile. That carrier creates the persistent field;
        // each Magenheim tier changes how many zones are deployed and where.
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

        VenomStaffVisuals.Apply(item.ItemPrefab, definition.AssetName);

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
            "Magenheim_Staff_Venom_Simple", "staff-venom-simple", "Simple Staff of Venom",
            "Toxic Glob: hurls one unstable mass that ruptures into a lingering poison field. The forked green crystal is crude, cheap, and built to deny a doorway rather than burst a target down.",
            1, 20f, 0f, 16f, .55f, .25f, .30f, 22f, 2f, 1, 1, 0f,
            new StaffRequirement("FineWood", 8), new StaffRequirement("Ooze", 4), new StaffRequirement("Magenheim_Crystal_Venom_Simple", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Crystal", "staff-venom-crystal", "Crystal Staff of Venom",
            "Caustic Pool: a silver-bound Venom crystal fires two closely timed globules down one lane, refreshing poison pressure and keeping contested ground dangerous longer.",
            2, 30f, 0f, 24f, .72f, .20f, .25f, 28f, 1.2f, 1, 2, .28f,
            new StaffRequirement("ElderBark", 10), new StaffRequirement("Guck", 4), new StaffRequirement("Ooze", 6), new StaffRequirement("Magenheim_Crystal_Venom_Crystal", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Advanced", "staff-venom-advanced", "Advanced Staff of Venom",
            "Miasma Bloom: a black-metal thorn crown throws three contamination globes across a broad fan, turning clustered ground into a corrosive attrition zone.",
            3, 12f, 18f, 30f, .46f, .15f, .20f, 24f, 11f, 3, 1, 0f,
            new StaffRequirement("YggdrasilWood", 10), new StaffRequirement("Guck", 6), new StaffRequirement("Bilebag", 2), new StaffRequirement("Magenheim_Crystal_Venom_Advanced", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Master", "staff-venom-master", "Master Staff of Venom",
            "Plaguefield: six black-metal ribs cage a Master Venom crystal and release three waves of three poison globes. Nine lingering fields convert open ground into hostile terrain.",
            4, 0f, 48f, 34f, .30f, .10f, .15f, 26f, 16f, 3, 3, .22f,
            new StaffRequirement("YggdrasilWood", 15), new StaffRequirement("Guck", 10), new StaffRequirement("Bilebag", 4), new StaffRequirement("Eitr", 10), new StaffRequirement("Magenheim_Crystal_Venom_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private readonly struct StaffRequirement
    {
        internal StaffRequirement(string prefabName, int amount) { PrefabName = prefabName; Amount = amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class VenomStaffDefinition
    {
        internal VenomStaffDefinition(
            string prefabName, string assetName, string displayName, string description,
            int minimumStationLevel, float staminaCost, float eitrCost, float poisonDamage,
            float damageMultiplier, float forceMultiplier, float staggerMultiplier,
            float projectileVelocity, float projectileAccuracy, int projectiles, int bursts,
            float burstInterval, params StaffRequirement[] requirements)
        {
            PrefabName = prefabName; AssetName = assetName; DisplayName = displayName; Description = description;
            MinimumStationLevel = minimumStationLevel; StaminaCost = staminaCost; EitrCost = eitrCost; PoisonDamage = poisonDamage;
            DamageMultiplier = damageMultiplier; ForceMultiplier = forceMultiplier; StaggerMultiplier = staggerMultiplier;
            ProjectileVelocity = projectileVelocity; ProjectileAccuracy = projectileAccuracy;
            Projectiles = projectiles; Bursts = bursts; BurstInterval = burstInterval; Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string AssetName { get; }
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
