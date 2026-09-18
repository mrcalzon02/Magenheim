using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Four-tier Venom staff family. Venom owns persistent toxic terrain and, above the simple tier,
/// a separate corrosive attrition effect that continues briefly after a target escapes the pool.
/// </summary>
internal sealed class VenomStaffRegistrar : IDisposable
{
    // StaffIceShards is used only as the hidden animation/attachment carrier. Its visible hierarchy
    // is disabled by VenomStaffVisuals, and all Magenheim combat/resource values are replaced below.
    private const string BaseStaffPrefab = "StaffIceShards";

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
            if (PrefabManager.Instance.GetPrefab(BaseStaffPrefab) is null)
                throw new InvalidOperationException($"Required hidden staff carrier prefab '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Venom staff recipes are registered.");

            var corrosion = RegisterCorrosionEffect(
                "Magenheim_SE_Corrosion",
                "Corrosion",
                "Caustic crystal residue continues eating into the victim after it leaves the contaminated ground.",
                ttl: 4f,
                healthPerTick: -1.5f,
                tickInterval: 1f,
                new Color(.42f, .88f, .12f, 1f));
            var deepCorrosion = RegisterCorrosionEffect(
                "Magenheim_SE_DeepCorrosion",
                "Deep Corrosion",
                "Master Venom has soaked into the target and continues to burn through it after exposure ends.",
                ttl: 6f,
                healthPerTick: -2.5f,
                tickInterval: 1f,
                new Color(.68f, 1f, .12f, 1f));

            var toxicField = StaffEffectPayloads.CreateField(
                "Magenheim_Venom_ToxicField",
                new HitData.DamageTypes { m_poison = 3f },
                radius: 3.6f,
                ttl: 8f,
                hitInterval: 1f,
                attackForce: 0f,
                tint: new Color(.25f, .72f, .10f, 1f),
                emission: 1.15f);
            var causticField = StaffEffectPayloads.CreateField(
                "Magenheim_Venom_CausticField",
                new HitData.DamageTypes { m_poison = 4f },
                radius: 4.0f,
                ttl: 10f,
                hitInterval: 1f,
                attackForce: 0f,
                tint: new Color(.38f, .84f, .10f, 1f),
                emission: 1.25f,
                statusEffect: corrosion);
            var miasmaField = StaffEffectPayloads.CreateField(
                "Magenheim_Venom_MiasmaField",
                new HitData.DamageTypes { m_poison = 3.5f },
                radius: 3.4f,
                ttl: 8f,
                hitInterval: .8f,
                attackForce: 0f,
                tint: new Color(.47f, .92f, .12f, 1f),
                emission: 1.35f,
                statusEffect: corrosion);
            var plagueField = StaffEffectPayloads.CreateField(
                "Magenheim_Venom_PlagueField",
                new HitData.DamageTypes { m_poison = 3f },
                radius: 3.8f,
                ttl: 10f,
                hitInterval: .75f,
                attackForce: 0f,
                tint: new Color(.62f, 1f, .10f, 1f),
                emission: 1.55f,
                statusEffect: deepCorrosion);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Venom_ToxicProjectile", new Color(.24f, .76f, .08f, 1f), 1.15f, toxicField),
                StaffEffectPayloads.CreateProjectile("Magenheim_Venom_CausticProjectile", new Color(.38f, .86f, .08f, 1f), 1.28f, causticField),
                StaffEffectPayloads.CreateProjectile("Magenheim_Venom_MiasmaProjectile", new Color(.48f, .94f, .10f, 1f), 1.40f, miasmaField),
                StaffEffectPayloads.CreateProjectile("Magenheim_Venom_PlagueProjectile", new Color(.65f, 1f, .10f, 1f), 1.65f, plagueField));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Venom abilities: Toxic Glob, Caustic Pool, Miasma Bloom, and Plaguefield with persistent Corrosion.");
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

    private static StatusEffect RegisterCorrosionEffect(
        string identity,
        string displayName,
        string tooltip,
        float ttl,
        float healthPerTick,
        float tickInterval,
        Color tint)
    {
        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = identity;
        effect.m_name = displayName;
        effect.m_tooltip = tooltip;
        effect.m_ttl = ttl;
        effect.m_icon = EarthAssets.Icon("crystal", identity, tint);

        SetRequiredStatsField(effect, "m_healthPerTick", healthPerTick, identity);
        SetRequiredStatsField(effect, "m_tickInterval", tickInterval, identity);

        var custom = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(custom))
            throw new InvalidOperationException($"Jotunn refused Venom status effect '{identity}'.");
        return custom.StatusEffect;
    }

    private static void SetRequiredStatsField(SE_Stats effect, string fieldName, float value, string identity)
    {
        var field = typeof(SE_Stats).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Current Valheim SE_Stats no longer exposes '{fieldName}' required by Venom effect '{identity}'.");
        if (field.FieldType != typeof(float))
            throw new InvalidOperationException($"SE_Stats.{fieldName} has unexpected type '{field.FieldType.FullName}' for Venom effect '{identity}'.");
        field.SetValue(effect, value);
    }

    private static void RegisterStaff(VenomStaffDefinition definition, PayloadSet payloads)
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
        StaffBurstContract.Apply(attack, definition.PrefabName);

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
            "Toxic Glob: hurls one unstable mass that ruptures into an eight-second poison field. This tier teaches contamination and area denial without adding a second debuff.",
            1, 20f, 10f, .70f, .15f, .20f, 23f, 2f, 1, 1, 0f, PayloadKind.Toxic,
            new StaffRequirement("FineWood", 8), new StaffRequirement("Ooze", 4), new StaffRequirement("Magenheim_Crystal_Venom_Simple", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Crystal", "staff-venom-crystal", "Crystal Staff of Venom",
            "Caustic Pool: fires two closely timed globules into the same lane. Its ten-second pools poison exposed targets and apply Corrosion, which continues eating health for four seconds after they escape.",
            2, 30f, 12f, .60f, .12f, .18f, 28f, 1.2f, 1, 2, .28f, PayloadKind.Caustic,
            new StaffRequirement("ElderBark", 10), new StaffRequirement("Guck", 4), new StaffRequirement("Ooze", 6), new StaffRequirement("Magenheim_Crystal_Venom_Crystal", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Advanced", "staff-venom-advanced", "Advanced Staff of Venom",
            "Miasma Bloom: throws three contamination globes across a broad fan. Every field repeatedly reapplies poison and Corrosion, creating overlapping attrition zones rather than chasing burst damage.",
            3, 30f, 10f, .48f, .08f, .12f, 24f, 11f, 3, 1, 0f, PayloadKind.Miasma,
            new StaffRequirement("YggdrasilWood", 10), new StaffRequirement("Guck", 6), new StaffRequirement("Bilebag", 2), new StaffRequirement("Magenheim_Crystal_Venom_Advanced", 1)),

        new VenomStaffDefinition(
            "Magenheim_Staff_Venom_Master", "staff-venom-master", "Master Staff of Venom",
            "Plaguefield: releases three waves of three globes. Nine ten-second fields saturate the front with poison while Deep Corrosion persists for six seconds after exposure, making retreat necessary but not immediately sufficient.",
            4, 42f, 8f, .35f, .05f, .10f, 26f, 16f, 3, 3, .22f, PayloadKind.Plague,
            new StaffRequirement("YggdrasilWood", 15), new StaffRequirement("Guck", 10), new StaffRequirement("Bilebag", 4), new StaffRequirement("Magenheim_Crystal_Venom_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Toxic, Caustic, Miasma, Plague }

    private sealed class PayloadSet
    {
        private readonly GameObject _toxic;
        private readonly GameObject _caustic;
        private readonly GameObject _miasma;
        private readonly GameObject _plague;

        internal PayloadSet(GameObject toxic, GameObject caustic, GameObject miasma, GameObject plague)
        {
            _toxic = toxic;
            _caustic = caustic;
            _miasma = miasma;
            _plague = plague;
        }

        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Toxic => _toxic,
            PayloadKind.Caustic => _caustic,
            PayloadKind.Miasma => _miasma,
            PayloadKind.Plague => _plague,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
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
            int minimumStationLevel, float staminaCost, float poisonDamage,
            float damageMultiplier, float forceMultiplier, float staggerMultiplier,
            float projectileVelocity, float projectileAccuracy, int projectiles, int bursts,
            float burstInterval, PayloadKind payload, params StaffRequirement[] requirements)
        {
            PrefabName = prefabName; AssetName = assetName; DisplayName = displayName; Description = description;
            MinimumStationLevel = minimumStationLevel; StaminaCost = staminaCost; PoisonDamage = poisonDamage;
            DamageMultiplier = damageMultiplier; ForceMultiplier = forceMultiplier; StaggerMultiplier = staggerMultiplier;
            ProjectileVelocity = projectileVelocity; ProjectileAccuracy = projectileAccuracy;
            Projectiles = projectiles; Bursts = bursts; BurstInterval = burstInterval; Payload = payload; Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float PoisonDamage { get; }
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
