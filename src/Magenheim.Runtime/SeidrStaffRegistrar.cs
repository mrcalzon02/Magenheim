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

/// <summary>Four-tier Seidr staff family built around binding sorcery and owned Magenheim staff geometry.</summary>
internal sealed class SeidrStaffRegistrar : IDisposable
{
    private const string BaseStaffPrefab = "StaffIceShards";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal SeidrStaffRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

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
                throw new InvalidOperationException("Geologist's Workstation must exist before Seidr staff recipes are registered.");

            var snare = RegisterBindingEffect("Magenheim_SE_SeidrSnare", "Seidr Snare", "Binding runes drag against every step.", 2.2f, -.20f, new Color(.63f, .27f, .94f, 1f));
            var fateBind = RegisterBindingEffect("Magenheim_SE_SeidrFateBind", "Fate Bind", "A woven fate-line constrains movement until the omen releases.", 4f, -.35f, new Color(.84f, .53f, 1f, 1f));

            var hexMark = StaffEffectPayloads.CreateField("Magenheim_Seidr_HexMark", new HitData.DamageTypes { m_spirit = .5f }, .70f, .15f, .15f, 0f, new Color(.57f, .19f, .88f, 1f), 1.35f, snare);
            var runeSeal = StaffEffectPayloads.CreateField("Magenheim_Seidr_RuneSeal", new HitData.DamageTypes { m_spirit = 3f }, 2.4f, .22f, .22f, 8f, new Color(.67f, .28f, .96f, 1f), 1.50f, snare);
            var witchweave = StaffEffectPayloads.CreateField("Magenheim_Seidr_WitchweaveSeal", new HitData.DamageTypes { m_spirit = 1.5f }, 1.35f, .26f, .26f, 3f, new Color(.75f, .39f, 1f, 1f), 1.60f, snare);
            var fateKnot = StaffEffectPayloads.CreateField("Magenheim_Seidr_FateKnot", new HitData.DamageTypes { m_spirit = 2f }, 1.65f, .36f, .36f, 4f, new Color(.88f, .64f, 1f, 1f), 1.80f, fateBind);

            var payloads = new PayloadSet(
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_HexProjectile", new Color(.52f, .16f, .85f, 1f), 1.35f, hexMark, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_RuneProjectile", new Color(.64f, .25f, .95f, 1f), 1.50f, runeSeal, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_WeaveProjectile", new Color(.74f, .38f, 1f, 1f), 1.62f, witchweave, BaseStaffPrefab),
                StaffEffectPayloads.CreateProjectile("Magenheim_Seidr_FateProjectile", new Color(.88f, .62f, 1f, 1f), 1.85f, fateKnot, BaseStaffPrefab));

            foreach (var definition in Definitions())
            {
                RegisterStaff(definition, payloads);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Seidr abilities with owned Seidr staff bodies and stamina-only casting.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Seidr staff content registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static StatusEffect RegisterBindingEffect(string identity, string displayName, string tooltip, float ttl, float speedModifier, Color tint)
    {
        var effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = identity;
        effect.m_name = displayName;
        effect.m_tooltip = tooltip;
        effect.m_ttl = ttl;
        effect.m_icon = EarthAssets.Icon("crystal", identity, tint);
        var speedField = typeof(SE_Stats).GetField("m_speedModifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Current Valheim SE_Stats no longer exposes the m_speedModifier field required by Seidr binding.");
        speedField.SetValue(effect, speedModifier);
        var custom = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(custom)) throw new InvalidOperationException($"Jotunn refused Seidr status effect '{identity}'.");
        return custom.StatusEffect;
    }

    private static void RegisterStaff(Definition definition, PayloadSet payloads)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Seidr staff identity '{definition.PrefabName}'.");
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

        SeidrVisuals.Apply(item.ItemPrefab, definition.AssetName);
        if (!ItemManager.Instance.AddItem(item)) throw new InvalidOperationException($"Jotunn refused Seidr staff item '{definition.PrefabName}'.");
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
        foreach (var requirement in definition.Requirements) config.AddRequirement(requirement.PrefabName, requirement.Amount);
        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config))) throw new InvalidOperationException($"Jotunn refused Seidr staff recipe '{config.Name}'.");
    }

    private static IReadOnlyList<Definition> Definitions() => new[]
    {
        new Definition("Magenheim_Staff_Seidr_Simple", "staff-seidr-simple", "Simple Staff of Seidr",
            "Hex Needle: drives one omen-laced bolt into the chosen target. The impact inscribes a brief Seidr Snare, dragging twenty percent from movement instead of pretending the hex is only damage.",
            1, 18f, 15f, 1f, .30f, .40f, 44f, .60f, 1, 1, 0f, PayloadKind.Hex,
            new Requirement("FineWood", 8), new Requirement("GreydwarfEye", 6), new Requirement("Magenheim_Crystal_Seidr_Simple", 1)),
        new Definition("Magenheim_Staff_Seidr_Crystal", "staff-seidr-crystal", "Crystal Staff of Seidr",
            "Rune Spear: hurls one heavy sorcerous lance. Its impact opens a binding seal around the struck point, damaging nearby spirits and catching movement in the same rune.",
            2, 28f, 34f, 1f, .70f, .90f, 54f, .25f, 1, 1, 0f, PayloadKind.Rune,
            new Requirement("ElderBark", 10), new Requirement("AncientSeed", 3), new Requirement("Silver", 2), new Requirement("Magenheim_Crystal_Seidr_Crystal", 1)),
        new Definition("Magenheim_Staff_Seidr_Advanced", "staff-seidr-advanced", "Advanced Staff of Seidr",
            "Witchweave: casts five crossing omen-lines. Every line leaves a small binding seal, trading raw impact for a fan of overlapping snares that catches evasive or clustered enemies.",
            3, 28f, 9f, .50f, .35f, .55f, 42f, 9f, 5, 1, 0f, PayloadKind.Weave,
            new Requirement("YggdrasilWood", 10), new Requirement("BlackCore", 2), new Requirement("Magenheim_Crystal_Seidr_Advanced", 1)),
        new Definition("Magenheim_Staff_Seidr_Master", "staff-seidr-master", "Master Staff of Seidr",
            "Fate Loom: releases three omen-lines through four successive responses. Every impact knots a stronger Fate Bind into the ground, reducing movement by thirty-five percent for four seconds and turning the target lane into controlled sorcery.",
            4, 48f, 8f, .40f, .40f, .65f, 45f, 7f, 3, 4, .13f, PayloadKind.Fate,
            new Requirement("YggdrasilWood", 15), new Requirement("BlackCore", 4), new Requirement("Magenheim_Crystal_Seidr_Master", 1)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }

    private enum PayloadKind { Hex, Rune, Weave, Fate }

    private sealed class PayloadSet
    {
        private readonly GameObject _hex, _rune, _weave, _fate;
        internal PayloadSet(GameObject hex, GameObject rune, GameObject weave, GameObject fate) { _hex=hex; _rune=rune; _weave=weave; _fate=fate; }
        internal GameObject Resolve(PayloadKind kind) => kind switch
        {
            PayloadKind.Hex => _hex, PayloadKind.Rune => _rune, PayloadKind.Weave => _weave, PayloadKind.Fate => _fate,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private readonly struct Requirement
    {
        internal Requirement(string prefabName, int amount) { PrefabName=prefabName; Amount=amount; }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private sealed class Definition
    {
        internal Definition(string prefabName, string assetName, string displayName, string description, int minimumStationLevel,
            float staminaCost, float spiritDamage, float damageMultiplier, float forceMultiplier, float staggerMultiplier,
            float projectileVelocity, float projectileAccuracy, int projectiles, int bursts, float burstInterval, PayloadKind payload,
            params Requirement[] requirements)
        {
            PrefabName=prefabName; AssetName=assetName; DisplayName=displayName; Description=description; MinimumStationLevel=minimumStationLevel;
            StaminaCost=staminaCost; SpiritDamage=spiritDamage; DamageMultiplier=damageMultiplier; ForceMultiplier=forceMultiplier;
            StaggerMultiplier=staggerMultiplier; ProjectileVelocity=projectileVelocity; ProjectileAccuracy=projectileAccuracy; Projectiles=projectiles;
            Bursts=bursts; BurstInterval=burstInterval; Payload=payload; Requirements=requirements;
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

