using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Four-tier Spirit staff family. Spirit magic uses spectral projectiles and echo fields to haunt
/// enemies, suppress outgoing attacks, and keep pressure lingering after the initial hit.
/// </summary>
internal sealed class SpiritStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal SpiritStaffRegistrar(ManualLogSource log) =>
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
                throw new InvalidOperationException("Geologist's Workstation must exist before Spirit staff recipes are registered.");

            var haunted = RegisterSuppressionEffect(
                "Magenheim_SE_Haunted",
                "Haunted",
                "A spectral presence clings to the target, reducing all outgoing attack damage by twelve percent.",
                3.5f,
                .88f,
                new Color(.30f, .90f, .80f, 1f));
            var dissonance = RegisterSuppressionEffect(
                "Magenheim_SE_SpiritDissonance",
                "Spirit Dissonance",
                "Overlapping voices disrupt the target's intent, reducing all outgoing attack damage by twenty percent.",
                3f,
                .80f,
                new Color(.52f, 1f, .91f, 1f));
            var soulSuppression = RegisterSuppressionEffect(
                "Magenheim_SE_SoulSuppression",
                "Soul Suppression",
                "The reliquary smothers hostile intent, reducing all outgoing attack damage by thirty percent.",
                5f,
                .70f,
                new Color(.78f, 1f, .95f, 1f));

            var lanternEcho = StaffEffectPayloads.CreateField(
                "Magenheim_Spirit_LanternEcho",
                new HitData.DamageTypes { m_spirit = 3f },
                2.4f,
                3f,
                .75f,
                4f,
                new Color(.28f, .92f, .80f, 1f),
                1.35f,
                haunted);
            var chorusEcho = StaffEffectPayloads.CreateField(
                "Magenheim_Spirit_ChorusEcho",
                new HitData.DamageTypes { m_spirit = 2f },
                1.8f,
                1.4f,
                .40f,
                2f,
                new Color(.50f, 1f, .90f, 1f),
                1.55f,
                dissonance);
            var reliquaryEcho = StaffEffectPayloads.CreateField(
                "Magenheim_Spirit_ReliquaryEcho",
                new HitData.DamageTypes { m_spirit = 3.5f },
                2.8f,
                2.2f,
                .45f,
                3f,
                new Color(.74f, 1f, .94f, 1f),
                1.75f,
                soulSuppression);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_Projectile", new Color(.24f, .88f, .76f, 1f), 1.35f, null, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_LanternProjectile", new Color(.34f, .96f, .84f, 1f), 1.50f, lanternEcho, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_ChorusProjectile", new Color(.55f, 1f, .91f, 1f), 1.65f, chorusEcho, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Spirit_ReliquaryProjectile", new Color(.78f, 1f, .95f, 1f), 1.85f, reliquaryEcho, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Spirit abilities with Valheim-scale authored Spirit staff bodies, non-fire projectile carriers, and stamina-only casting.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Spirit staff content registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static StatusEffect RegisterSuppressionEffect(
        string identity,
        string displayName,
        string tooltip,
        float ttl,
        float damageMultiplier,
        Color tint)
    {
        if (damageMultiplier <= 0f || damageMultiplier > 1f)
            throw new ArgumentOutOfRangeException(nameof(damageMultiplier), damageMultiplier, "Spirit suppression must reduce outgoing damage without inverting it.");

        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = identity;
        effect.m_name = displayName;
        effect.m_tooltip = tooltip;
        effect.m_ttl = ttl;
        effect.m_icon = EarthAssets.Icon("crystal", identity, tint);
        effect.m_modifyAttackSkill = Skills.SkillType.All;
        effect.m_damageModifier = damageMultiplier;

        var custom = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(custom))
            throw new InvalidOperationException($"Jotunn refused Spirit status effect '{identity}'.");
        return custom.StatusEffect;
    }

    private static void RegisterStaff(Definition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Spirit staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_spirit = definition.SpiritDamage };
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

        SpiritVisuals.Apply(item.ItemPrefab, definition.AssetName);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Spirit staff item '{definition.PrefabName}'.");
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
        foreach (var requirement in definition.Requirements)
            config.AddRequirement(requirement.PrefabName, requirement.Amount);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException($"Jotunn refused Spirit staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<Definition> Definitions() => new[]
    {
        new Definition(
            "Magenheim_Staff_Spirit_Simple", "staff-spirit-simple", "Simple Staff of Spirit",
            "Whisper Bolt: releases one quiet spectral shot. It carries only Spirit damage and leaves no borrowed flame, suppression field, or explosion behind.",
            1, 17f, 22f, .95f, .18f, .30f, 40f, .85f, 1, 1, 0f, PayloadKind.Direct,
            new Requirement("FineWood", 8), new Requirement("BoneFragments", 8), new Requirement("Magenheim_Crystal_Spirit_Simple", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Crystal", "staff-spirit-crystal", "Crystal Staff of Spirit",
            "Wraith Lantern: drives a dense spectral lance into one target and leaves a three-second haunting echo. Enemies touched by the echo become Haunted, reducing outgoing attack damage by twelve percent for three and a half seconds.",
            2, 27f, 34f, 1f, .42f, .65f, 50f, .35f, 1, 1, 0f, PayloadKind.Lantern,
            new Requirement("ElderBark", 10), new Requirement("Chain", 2), new Requirement("Silver", 2), new Requirement("Magenheim_Crystal_Spirit_Crystal", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Advanced", "staff-spirit-advanced", "Advanced Staff of Spirit",
            "Soul Chorus: four spectral voices answer the cast twice. Their overlapping echo fields inflict Spirit Dissonance, cutting outgoing attack damage by twenty percent while the chorus continues to haunt a clustered group.",
            3, 28f, 14f, .45f, .28f, .42f, 42f, 7.5f, 4, 2, .17f, PayloadKind.Chorus,
            new Requirement("YggdrasilWood", 10), new Requirement("BlackCore", 2), new Requirement("Magenheim_Crystal_Spirit_Advanced", 1)),
        new Definition(
            "Magenheim_Staff_Spirit_Master", "staff-spirit-master", "Master Staff of Spirit",
            "Reliquary of Echoes: releases four spectral lines through three successive responses. Each impact opens a larger echo field that inflicts Soul Suppression, reducing outgoing attack damage by thirty percent for five seconds after exposure.",
            4, 52f, 16f, .40f, .32f, .50f, 44f, 10f, 4, 3, .13f, PayloadKind.Reliquary,
            new Requirement("YggdrasilWood", 15), new Requirement("BlackCore", 4), new Requirement("Magenheim_Crystal_Spirit_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Direct, Lantern, Chorus, Reliquary }

    private sealed class PayloadSet
    {
        private readonly GameObject _direct;
        private readonly GameObject _lantern;
        private readonly GameObject _chorus;
        private readonly GameObject _reliquary;

        internal PayloadSet(GameObject direct, GameObject lantern, GameObject chorus, GameObject reliquary)
        {
            _direct = direct;
            _lantern = lantern;
            _chorus = chorus;
            _reliquary = reliquary;
        }

        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Direct => _direct,
            PayloadKind.Lantern => _lantern,
            PayloadKind.Chorus => _chorus,
            PayloadKind.Reliquary => _reliquary,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct Requirement
    {
        internal Requirement(string prefabName, int amount)
        {
            PrefabName = prefabName;
            Amount = amount;
        }

        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class Definition
    {
        internal Definition(
            string prefabName,
            string assetName,
            string displayName,
            string description,
            int minimumStationLevel,
            float staminaCost,
            float spiritDamage,
            float damageMultiplier,
            float forceMultiplier,
            float staggerMultiplier,
            float projectileVelocity,
            float projectileAccuracy,
            int projectiles,
            int bursts,
            float burstInterval,
            PayloadKind payload,
            params Requirement[] requirements)
        {
            PrefabName = prefabName;
            AssetName = assetName;
            DisplayName = displayName;
            Description = description;
            MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost;
            SpiritDamage = spiritDamage;
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
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
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
        internal IReadOnlyList<Requirement> Requirements { get; }
    }
}

