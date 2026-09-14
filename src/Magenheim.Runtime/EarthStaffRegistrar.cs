using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Playable Earth staff family built on Valheim's radial sledge attacks. Earth remains short-ranged
/// and positional, but the upper tiers now leave real seismic debuffs behind the impact instead of
/// describing armor breaking and instability only through stagger numbers.
/// </summary>
internal sealed class EarthStaffRegistrar : IDisposable
{
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
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before Magenheim Earth staff recipes are registered.");

            var fracturedArmor = RegisterEarthDebuff(
                "Magenheim_SE_FracturedArmor",
                "Fractured Armor",
                "Fault lines have opened through the target's defenses. Blunt, Slash, and Pierce damage now strike as Weak.",
                ttl: 4f,
                physicalModifier: HitData.DamageModifier.Weak,
                speedModifier: 0f,
                tint: new Color(.72f, .53f, .28f, 1f));
            var tremor = RegisterEarthDebuff(
                "Magenheim_SE_Tremor",
                "Tremor",
                "The ground is still moving under the target, reducing movement speed by fifteen percent.",
                ttl: 2.5f,
                physicalModifier: HitData.DamageModifier.Normal,
                speedModifier: -.15f,
                tint: new Color(.58f, .42f, .24f, 1f));
            var shatteredArmor = RegisterEarthDebuff(
                "Magenheim_SE_ShatteredArmor",
                "Shattered Armor",
                "Worldshaker has broken the target's footing and defenses. Physical damage strikes as VeryWeak while movement is reduced by twenty percent.",
                ttl: 4.5f,
                physicalModifier: HitData.DamageModifier.VeryWeak,
                speedModifier: -.20f,
                tint: new Color(.92f, .66f, .30f, 1f));

            EarthAbilityEffects.Configure(fracturedArmor, tremor, shatteredArmor);

            foreach (var definition in Definitions())
            {
                if (PrefabManager.Instance.GetPrefab(definition.BasePrefab) is null)
                    throw new InvalidOperationException($"Required seismic carrier prefab '{definition.BasePrefab}' is unavailable.");
                RegisterStaff(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered Earth abilities: Stone Pulse, Fault Breaker with Fractured Armor, Seismic Ring with Tremor, and Worldshaker with Shattered Armor.");
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
            var speedField = typeof(SE_Stats).GetField(
                "m_speedModifier",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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

    private static void RegisterStaff(EarthStaffDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) || CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied Earth staff identity '{definition.PrefabName}'.");

        var item = new CustomItem(definition.PrefabName, definition.BasePrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.DisplayName;
        shared.m_description = definition.Description;
        shared.m_skillType = Skills.SkillType.ElementalMagic;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        var attack = shared.m_attack;
        attack.m_attackStamina = definition.StaminaCost;
        attack.m_attackEitr = definition.EitrCost;
        attack.m_damageMultiplier = definition.DamageMultiplier;
        attack.m_forceMultiplier = definition.ForceMultiplier;
        attack.m_staggerMultiplier = definition.StaggerMultiplier;

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
            "Magenheim_Staff_Earth_Simple",
            "Simple Staff of Earth",
            "Stone Pulse: drives a compact impulse into the ground and releases a blunt radial shockwave. Damage is deliberately modest; the spell exists to interrupt and shove nearby enemies without Eitr.",
            "SledgeStagbreaker", 1, 24f, 0f, .42f, 1.45f, 1.35f,
            new StaffRequirement("CoreWood", 10), new StaffRequirement("Stone", 12), new StaffRequirement("Magenheim_Crystal_Earth_Simple", 1)),

        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Crystal",
            "Crystal Staff of Earth",
            "Fault Breaker: focuses the rupture through the target's defenses. Enemies struck gain Fractured Armor for four seconds, making Blunt, Slash, and Pierce damage strike them as Weak during the follow-up window.",
            "SledgeIron", 2, 34f, 0f, .72f, 1.35f, 2.35f,
            new StaffRequirement("ElderBark", 10), new StaffRequirement("Iron", 4), new StaffRequirement("Magenheim_Crystal_Earth_Crystal", 1)),

        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Advanced",
            "Advanced Staff of Earth",
            "Seismic Ring: sacrifices direct damage for violent radial displacement. Survivors remain caught in Tremor for two and a half seconds, losing fifteen percent movement while the ground settles.",
            "SledgeDemolisher", 3, 14f, 18f, .38f, 2.80f, 2.10f,
            new StaffRequirement("YggdrasilWood", 10), new StaffRequirement("BlackMarble", 8), new StaffRequirement("Iron", 4), new StaffRequirement("Magenheim_Crystal_Earth_Advanced", 1)),

        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Master",
            "Master Staff of Earth",
            "Worldshaker: turns the caster's immediate ground into a crushing seismic weapon. Enemies struck suffer Shattered Armor for four and a half seconds: physical damage treats them as VeryWeak and their movement is reduced by twenty percent.",
            "SledgeDemolisher", 4, 0f, 48f, .92f, 3.40f, 2.85f,
            new StaffRequirement("YggdrasilWood", 15), new StaffRequirement("BlackMarble", 12), new StaffRequirement("BlackMetal", 5), new StaffRequirement("Eitr", 10), new StaffRequirement("Magenheim_Crystal_Earth_Master", 1)),
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

    private sealed class EarthStaffDefinition
    {
        internal EarthStaffDefinition(
            string prefabName, string displayName, string description, string basePrefab,
            int minimumStationLevel, float staminaCost, float eitrCost, float damageMultiplier,
            float forceMultiplier, float staggerMultiplier, params StaffRequirement[] requirements)
        {
            PrefabName = prefabName; DisplayName = displayName; Description = description; BasePrefab = basePrefab;
            MinimumStationLevel = minimumStationLevel; StaminaCost = staminaCost; EitrCost = eitrCost;
            DamageMultiplier = damageMultiplier; ForceMultiplier = forceMultiplier; StaggerMultiplier = staggerMultiplier;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal string BasePrefab { get; }
        internal int MinimumStationLevel { get; }
        internal float StaminaCost { get; }
        internal float EitrCost { get; }
        internal float DamageMultiplier { get; }
        internal float ForceMultiplier { get; }
        internal float StaggerMultiplier { get; }
        internal IReadOnlyList<StaffRequirement> Requirements { get; }
    }
}

internal static class EarthAbilityEffects
{
    private static StatusEffect? _fracturedArmor;
    private static StatusEffect? _tremor;
    private static StatusEffect? _shatteredArmor;

    internal static void Configure(StatusEffect fracturedArmor, StatusEffect tremor, StatusEffect shatteredArmor)
    {
        _fracturedArmor = fracturedArmor ?? throw new ArgumentNullException(nameof(fracturedArmor));
        _tremor = tremor ?? throw new ArgumentNullException(nameof(tremor));
        _shatteredArmor = shatteredArmor ?? throw new ArgumentNullException(nameof(shatteredArmor));
    }

    internal static void ApplyOnHit(Character target, HitData hit)
    {
        if (target is null || hit is null || target.IsDead() || !hit.HaveAttacker()) return;

        var attacker = hit.GetAttacker() as Humanoid;
        if (attacker is null || ReferenceEquals(attacker, target)) return;

        var weapon = attacker.GetCurrentWeapon();
        if (weapon?.m_dropPrefab is null) return;

        var effect = weapon.m_dropPrefab.name switch
        {
            "Magenheim_Staff_Earth_Crystal" => _fracturedArmor,
            "Magenheim_Staff_Earth_Advanced" => _tremor,
            "Magenheim_Staff_Earth_Master" => _shatteredArmor,
            _ => null,
        };

        if (effect is not null)
            target.GetSEMan().AddStatusEffect(effect, true);
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class EarthAbilityHitPatch
{
    private static void Postfix(Character __instance, HitData hit) =>
        EarthAbilityEffects.ApplyOnHit(__instance, hit);
}
