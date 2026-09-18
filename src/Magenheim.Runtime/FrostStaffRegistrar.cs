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
/// Four-tier Frost staff family. Frost owns explicit Magenheim projectile payloads, with
/// precision shatter setup at Crystal tier and persistent Rime control at Advanced/Master tiers.
/// </summary>
internal sealed class FrostStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal FrostStaffRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

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
                throw new InvalidOperationException($"Required hidden frost staff carrier '{BaseStaffPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Frost staff recipes are registered.");

            var brittle = RegisterFrostEffect(
                "Magenheim_SE_Brittle", "Brittle",
                "The target's frozen surface fractures under blunt impact. Blunt damage now strikes as Weak.",
                3.5f, true, 0f, new Color(.66f, .90f, 1f, 1f));
            var deepRime = RegisterFrostEffect(
                "Magenheim_SE_DeepRime", "Deep Rime",
                "Rime has accumulated around the target, reducing movement speed by twenty percent.",
                2.5f, false, -.20f, new Color(.78f, .96f, 1f, 1f));

            var lanceShatter = StaffEffectPayloads.CreateField(
                "Magenheim_Frost_LanceShatter", new HitData.DamageTypes { m_frost = 2f },
                .85f, .15f, .15f, 2f, new Color(.56f, .86f, 1f, 1f), 1.50f, brittle);
            var rimePatch = StaffEffectPayloads.CreateField(
                "Magenheim_Frost_RimePatch", new HitData.DamageTypes { m_frost = 2.5f },
                2.2f, 2.5f, .75f, 0f, new Color(.68f, .92f, 1f, 1f), 1.60f, deepRime);
            var torrentPatch = StaffEffectPayloads.CreateField(
                "Magenheim_Frost_TorrentRime", new HitData.DamageTypes { m_frost = 2f },
                1.8f, 1.8f, .60f, 0f, new Color(.82f, .98f, 1f, 1f), 1.85f, deepRime);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Frost_WaterProjectile", new Color(.40f, .78f, 1f, 1f), 1.30f, sourceStaffPrefab: BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Frost_LanceProjectile", new Color(.58f, .88f, 1f, 1f), 1.55f, lanceShatter, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Frost_VolleyProjectile", new Color(.68f, .93f, 1f, 1f), 1.68f, rimePatch, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Frost_TorrentProjectile", new Color(.86f, .99f, 1f, 1f), 1.95f, torrentPatch, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Frost abilities with owned Frost staff bodies and stamina-only casting.");
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

    private static StatusEffect RegisterFrostEffect(string identity, string displayName, string tooltip, float ttl, bool bluntWeakness, float speedModifier, Color tint)
    {
        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = identity;
        effect.m_name = displayName;
        effect.m_tooltip = tooltip;
        effect.m_ttl = ttl;
        effect.m_icon = EarthAssets.Icon("crystal", identity, tint);

        if (bluntWeakness)
        {
            effect.m_mods = new List<HitData.DamageModPair>
            {
                new HitData.DamageModPair { m_type = HitData.DamageType.Blunt, m_modifier = HitData.DamageModifier.Weak },
            };
        }

        if (speedModifier != 0f)
        {
            var speedField = typeof(SE_Stats).GetField("m_speedModifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Current Valheim SE_Stats no longer exposes m_speedModifier required by Frost Rime effects.");
            if (speedField.FieldType != typeof(float))
                throw new InvalidOperationException($"SE_Stats.m_speedModifier has unexpected type '{speedField.FieldType.FullName}'.");
            speedField.SetValue(effect, speedModifier);
        }

        var custom = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(custom))
            throw new InvalidOperationException($"Jotunn refused Frost status effect '{identity}'.");
        return custom.StatusEffect;
    }

    private static void RegisterStaff(FrostStaffDefinition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Frost staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, BaseStaffPrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_damages = new HitData.DamageTypes { m_frost = definition.FrostDamage };
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

        FrostStaffVisuals.Apply(item.ItemPrefab, definition.AssetName);
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
        foreach (var requirement in definition.Requirements) config.AddRequirement(requirement.PrefabName, requirement.Amount);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException($"Jotunn refused Frost staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<FrostStaffDefinition> Definitions() => new[]
    {
        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Simple", "staff-frost-simple", "Simple Staff of Frost",
            "Water Dart: snaps one cold-heavy shard forward. It deals clean Frost damage without leaving additional terrain or debuffs beyond the element's ordinary chill pressure.",
            1, 18f, 16f, 1f, .35f, .50f, 24f, 2.5f, 1, 1, 0f, PayloadKind.Water,
            new StaffRequirement("FineWood", 8), new StaffRequirement("FreezeGland", 2), new StaffRequirement("Magenheim_Crystal_Frost_Simple", 1)),
        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Crystal", "staff-frost-crystal", "Crystal Staff of Frost",
            "Frost Lance: compresses the cast into one precise spike. Its impact makes the target Brittle for three and a half seconds, causing Blunt damage to strike it as Weak.",
            2, 28f, 35f, 1f, 1.60f, 1.35f, 55f, .4f, 1, 1, 0f, PayloadKind.Lance,
            new StaffRequirement("ElderBark", 10), new StaffRequirement("Silver", 3), new StaffRequirement("Magenheim_Crystal_Frost_Crystal", 1)),
        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Advanced", "staff-frost-advanced", "Advanced Staff of Frost",
            "Ice Volley: throws five lighter shards across a fan. Each impact leaves a short Rime patch that deals Frost damage and applies Deep Rime, reducing movement by twenty percent.",
            3, 28f, 8f, .85f, .80f, .80f, 36f, 12f, 5, 1, 0f, PayloadKind.Volley,
            new StaffRequirement("YggdrasilWood", 10), new StaffRequirement("Silver", 4), new StaffRequirement("FreezeGland", 4), new StaffRequirement("Magenheim_Crystal_Frost_Advanced", 1)),
        new FrostStaffDefinition(
            "Magenheim_Staff_Frost_Master", "staff-frost-master", "Master Staff of Frost",
            "Rime Torrent: pours paired shards through six rapid pulses. Every impact leaves a compact Rime field, creating a moving carpet of repeated Frost pressure and Deep Rime rather than only sixteen isolated projectiles.",
            4, 44f, 5f, .75f, .65f, .70f, 40f, 7f, 2, 6, .09f, PayloadKind.Torrent,
            new StaffRequirement("YggdrasilWood", 15), new StaffRequirement("BlackMetal", 4), new StaffRequirement("FreezeGland", 6), new StaffRequirement("Magenheim_Crystal_Frost_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Water, Lance, Volley, Torrent }

    private sealed class PayloadSet
    {
        private readonly GameObject _water;
        private readonly GameObject _lance;
        private readonly GameObject _volley;
        private readonly GameObject _torrent;
        internal PayloadSet(GameObject water, GameObject lance, GameObject volley, GameObject torrent)
        { _water = water; _lance = lance; _volley = volley; _torrent = torrent; }
        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Water => _water, PayloadKind.Lance => _lance, PayloadKind.Volley => _volley, PayloadKind.Torrent => _torrent,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct StaffRequirement
    {
        internal StaffRequirement(string prefabName, int amount) { PrefabName = prefabName; Amount = amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class FrostStaffDefinition
    {
        internal FrostStaffDefinition(
            string prefabName, string assetName, string displayName, string description,
            int minimumStationLevel, float staminaCost, float frostDamage,
            float damageMultiplier, float forceMultiplier, float staggerMultiplier,
            float projectileVelocity, float projectileAccuracy, int projectiles, int bursts,
            float burstInterval, PayloadKind payload, params StaffRequirement[] requirements)
        {
            PrefabName = prefabName; AssetName = assetName; DisplayName = displayName; Description = description;
            MinimumStationLevel = minimumStationLevel; StaminaCost = staminaCost; FrostDamage = frostDamage;
            DamageMultiplier = damageMultiplier; ForceMultiplier = forceMultiplier; StaggerMultiplier = staggerMultiplier;
            ProjectileVelocity = projectileVelocity; ProjectileAccuracy = projectileAccuracy; Projectiles = projectiles;
            Bursts = bursts; BurstInterval = burstInterval; Payload = payload; Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string AssetName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float FrostDamage { get; }
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
