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
/// Four-tier Earth staff family. Earth is implemented as a true Magenheim staff line rather than
/// renamed Stagbreaker, Iron Sledge, or Demolisher items. Each cast launches a short-range seismic
/// focus that ruptures into blunt force, stagger, armor break, and ground-control effects.
/// </summary>
internal sealed class EarthStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal EarthStaffRegistrar(ManualLogSource log) =>
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
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Earth staff recipes are registered.");

            var fracturedArmor = RegisterEarthDebuff(
                "Magenheim_SE_FracturedArmor",
                "Fractured Armor",
                "Fault lines have opened through the target's defenses. Blunt, Slash, and Pierce damage now strike as Weak.",
                4f,
                HitData.DamageModifier.Weak,
                0f,
                new Color(.72f, .53f, .28f, 1f));
            var tremor = RegisterEarthDebuff(
                "Magenheim_SE_Tremor",
                "Tremor",
                "The ground is still moving under the target, reducing movement speed by fifteen percent.",
                2.5f,
                HitData.DamageModifier.Normal,
                -.15f,
                new Color(.58f, .42f, .24f, 1f));
            var shatteredArmor = RegisterEarthDebuff(
                "Magenheim_SE_ShatteredArmor",
                "Shattered Armor",
                "Worldshaker has broken the target's footing and defenses. Physical damage strikes as VeryWeak while movement is reduced by twenty percent.",
                4.5f,
                HitData.DamageModifier.VeryWeak,
                -.20f,
                new Color(.92f, .66f, .30f, 1f));

            var stonePulse = StaffEffectPayloads.CreateField(
                "Magenheim_Earth_StonePulse",
                new HitData.DamageTypes { m_blunt = 12f },
                2.2f, .16f, .16f, 34f,
                new Color(.58f,.42f,.24f,1f), .75f);
            var faultBreaker = StaffEffectPayloads.CreateField(
                "Magenheim_Earth_FaultBreaker",
                new HitData.DamageTypes { m_blunt = 18f },
                2.6f, .18f, .18f, 32f,
                new Color(.72f,.53f,.28f,1f), .90f,
                fracturedArmor);
            var seismicRing = StaffEffectPayloads.CreateField(
                "Magenheim_Earth_SeismicRing",
                new HitData.DamageTypes { m_blunt = 10f },
                3.4f, .28f, .14f, 50f,
                new Color(.68f,.50f,.27f,1f), 1.0f,
                tremor);
            var worldshaker = StaffEffectPayloads.CreateField(
                "Magenheim_Earth_Worldshaker",
                new HitData.DamageTypes { m_blunt = 20f },
                4.1f, .34f, .17f, 60f,
                new Color(.92f,.66f,.30f,1f), 1.2f,
                shatteredArmor);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Earth_StonePulseProjectile", new Color(.58f,.42f,.24f,1f), .90f, stonePulse, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Earth_FaultBreakerProjectile", new Color(.72f,.53f,.28f,1f), 1.00f, faultBreaker, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Earth_SeismicRingProjectile", new Color(.74f,.56f,.30f,1f), 1.10f, seismicRing, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Earth_WorldshakerProjectile", new Color(.92f,.66f,.30f,1f), 1.30f, worldshaker, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Earth staffs as owned Magenheim staff items: Stone Pulse, Fault Breaker, Seismic Ring, and Worldshaker.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Earth staff content registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static StatusEffect RegisterEarthDebuff(
        string identity,
        string displayName,
        string tooltip,
        float ttl,
        HitData.DamageModifier physicalModifier,
        float speedModifier,
        Color tint)
    {
        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = identity;
        effect.m_name = displayName;
        effect.m_tooltip = tooltip;
        effect.m_ttl = ttl;
        effect.m_icon = EarthAssets.Icon("crystal", identity, tint);

        if (physicalModifier != HitData.DamageModifier.Normal && physicalModifier != HitData.DamageModifier.Ignore)
        {
            effect.m_mods = new List<HitData.DamageModPair>
            {
                new HitData.DamageModPair { m_type = HitData.DamageType.Blunt, m_modifier = physicalModifier },
                new HitData.DamageModPair { m_type = HitData.DamageType.Slash, m_modifier = physicalModifier },
                new HitData.DamageModPair { m_type = HitData.DamageType.Pierce, m_modifier = physicalModifier },
            };
        }

        if (speedModifier != 0f)
        {
            var speedField = typeof(SE_Stats).GetField("m_speedModifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Current Valheim SE_Stats no longer exposes m_speedModifier required by Earth Tremor effects.");
            if (speedField.FieldType != typeof(float))
                throw new InvalidOperationException($"SE_Stats.m_speedModifier has unexpected type '{speedField.FieldType.FullName}'.");
            speedField.SetValue(effect, speedModifier);
        }

        var custom = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(custom))
            throw new InvalidOperationException($"Jotunn refused Earth status effect '{identity}'.");
        return custom.StatusEffect;
    }

    private static void RegisterStaff(EarthStaffDefinition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Earth staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_blunt = definition.BluntDamage };
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
        attack.m_projectiles = 1;
        attack.m_projectileBursts = 1;
        attack.m_burstInterval = 0f;
        attack.m_perBurstResourceUsage = false;

        EarthStaffVisuals.Apply(item.ItemPrefab, definition.PrefabName);

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Earth staff item '{definition.PrefabName}'.");
    }

    private static void RegisterRecipe(EarthStaffDefinition definition)
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
            throw new InvalidOperationException($"Jotunn refused Earth staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<EarthStaffDefinition> Definitions() => new[]
    {
        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Simple", "staff-earth-simple", "Simple Staff of Earth",
            "Stone Pulse: casts a compact seismic focus that ruptures into a blunt radial shockwave. It is a close-range interruption and knockback tool, not a disguised hammer.",
            1, 24f, 12f, .62f, 1.45f, 1.35f, 20f, 2f, PayloadKind.StonePulse,
            new StaffRequirement("CoreWood", 10), new StaffRequirement("Stone", 12), new StaffRequirement("Magenheim_Crystal_Earth_Simple", 1)),
        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Crystal", "staff-earth-crystal", "Crystal Staff of Earth",
            "Fault Breaker: bursts beneath the target and fractures physical defenses for four seconds while delivering a heavy stagger impulse.",
            2, 34f, 18f, .76f, 1.35f, 2.35f, 22f, 1.5f, PayloadKind.FaultBreaker,
            new StaffRequirement("ElderBark", 10), new StaffRequirement("Iron", 4), new StaffRequirement("Magenheim_Crystal_Earth_Crystal", 1)),
        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Advanced", "staff-earth-advanced", "Advanced Staff of Earth",
            "Seismic Ring: creates a wider rupture with violent radial displacement. Survivors remain slowed by Tremor while the ground settles.",
            3, 38f, 10f, .58f, 2.80f, 2.10f, 24f, 2.5f, PayloadKind.SeismicRing,
            new StaffRequirement("YggdrasilWood", 10), new StaffRequirement("BlackMarble", 8), new StaffRequirement("Iron", 4), new StaffRequirement("Magenheim_Crystal_Earth_Advanced", 1)),
        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Master", "staff-earth-master", "Master Staff of Earth",
            "Worldshaker: detonates a broad seismic rupture that crushes footing, hurls enemies outward, and leaves their armor shattered for the follow-up.",
            4, 52f, 20f, .92f, 3.40f, 2.85f, 26f, 3f, PayloadKind.Worldshaker,
            new StaffRequirement("YggdrasilWood", 15), new StaffRequirement("BlackMarble", 12), new StaffRequirement("BlackMetal", 5), new StaffRequirement("Magenheim_Crystal_Earth_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { StonePulse, FaultBreaker, SeismicRing, Worldshaker }

    private sealed class PayloadSet
    {
        private readonly GameObject _stonePulse;
        private readonly GameObject _faultBreaker;
        private readonly GameObject _seismicRing;
        private readonly GameObject _worldshaker;

        internal PayloadSet(GameObject stonePulse, GameObject faultBreaker, GameObject seismicRing, GameObject worldshaker)
        {
            _stonePulse = stonePulse;
            _faultBreaker = faultBreaker;
            _seismicRing = seismicRing;
            _worldshaker = worldshaker;
        }

        internal GameObject Resolve(PayloadKind payload) => payload switch
        {
            PayloadKind.StonePulse => _stonePulse,
            PayloadKind.FaultBreaker => _faultBreaker,
            PayloadKind.SeismicRing => _seismicRing,
            PayloadKind.Worldshaker => _worldshaker,
            _ => throw new ArgumentOutOfRangeException(nameof(payload), payload, null),
        };
    }

    private readonly struct StaffRequirement
    {
        internal StaffRequirement(string prefabName, int amount) { PrefabName = prefabName; Amount = amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class EarthStaffDefinition
    {
        internal EarthStaffDefinition(
            string prefabName, string assetName, string displayName, string description,
            int minimumStationLevel, float staminaCost, float bluntDamage,
            float damageMultiplier, float forceMultiplier, float staggerMultiplier,
            float projectileVelocity, float projectileAccuracy, PayloadKind payload,
            params StaffRequirement[] requirements)
        {
            PrefabName = prefabName;
            AssetName = assetName;
            DisplayName = displayName;
            Description = description;
            MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost;
            BluntDamage = bluntDamage;
            DamageMultiplier = damageMultiplier;
            ForceMultiplier = forceMultiplier;
            StaggerMultiplier = staggerMultiplier;
            ProjectileVelocity = projectileVelocity;
            ProjectileAccuracy = projectileAccuracy;
            Payload = payload;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float BluntDamage { get; }
        internal float DamageMultiplier { get; }
        internal float ForceMultiplier { get; }
        internal float StaggerMultiplier { get; }
        internal float ProjectileVelocity { get; }
        internal float ProjectileAccuracy { get; }
        internal PayloadKind Payload { get; }
        internal IReadOnlyList<StaffRequirement> Requirements { get; }
    }
}
