using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Magenheim.Runtime;

/// <summary>
/// Playable Earth staff family built on Valheim's radial sledge attacks. Earth magic is
/// intentionally short-ranged and positional: impact rings, posture breaking, knockback,
/// and seismic crowd control rather than another projectile family.
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

            foreach (var definition in Definitions())
            {
                if (PrefabManager.Instance.GetPrefab(definition.BasePrefab) is null)
                    throw new InvalidOperationException($"Required seismic carrier prefab '{definition.BasePrefab}' is unavailable.");
                RegisterStaff(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo("Registered the four-tier Earth staff family: Stone Pulse, Fault Breaker, Seismic Ring, and Worldshaker.");
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

        // Preserve the carrier's native radial ground-impact attack and dust/shockwave path.
        // Earth differentiation is physical: force and stagger rise faster than raw damage,
        // turning the family into local space control rather than a ranged damage substitute.
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
            "SledgeStagbreaker",
            minimumStationLevel: 1,
            staminaCost: 24f,
            eitrCost: 0f,
            damageMultiplier: 0.42f,
            forceMultiplier: 1.45f,
            staggerMultiplier: 1.35f,
            new StaffRequirement("CoreWood", 10),
            new StaffRequirement("Stone", 12),
            new StaffRequirement("Magenheim_Crystal_Earth_Simple", 1)),

        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Crystal",
            "Crystal Staff of Earth",
            "Fault Breaker: focuses the impact downward instead of outward. The rupture is still short-ranged, but its heavy stagger is intended to break posture and open armored enemies to follow-up attacks.",
            "SledgeIron",
            minimumStationLevel: 2,
            staminaCost: 34f,
            eitrCost: 0f,
            damageMultiplier: 0.72f,
            forceMultiplier: 1.35f,
            staggerMultiplier: 2.35f,
            new StaffRequirement("ElderBark", 10),
            new StaffRequirement("Iron", 4),
            new StaffRequirement("Magenheim_Crystal_Earth_Crystal", 1)),

        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Advanced",
            "Advanced Staff of Earth",
            "Seismic Ring: releases stored force as a broad local quake. It sacrifices direct damage for violent radial displacement and repeated opportunities to stagger a surrounding pack.",
            "SledgeDemolisher",
            minimumStationLevel: 3,
            staminaCost: 14f,
            eitrCost: 18f,
            damageMultiplier: 0.38f,
            forceMultiplier: 2.80f,
            staggerMultiplier: 2.10f,
            new StaffRequirement("YggdrasilWood", 10),
            new StaffRequirement("BlackMarble", 8),
            new StaffRequirement("Iron", 4),
            new StaffRequirement("Magenheim_Crystal_Earth_Advanced", 1)),

        new EarthStaffDefinition(
            "Magenheim_Staff_Earth_Master",
            "Master Staff of Earth",
            "Worldshaker: turns the caster's immediate ground into a crushing seismic weapon. The Master pulse combines heavy blunt impact, extreme stagger, and enough radial force to reset the shape of a melee engagement.",
            "SledgeDemolisher",
            minimumStationLevel: 4,
            staminaCost: 0f,
            eitrCost: 48f,
            damageMultiplier: 0.92f,
            forceMultiplier: 3.40f,
            staggerMultiplier: 2.85f,
            new StaffRequirement("YggdrasilWood", 15),
            new StaffRequirement("BlackMarble", 12),
            new StaffRequirement("BlackMetal", 5),
            new StaffRequirement("Eitr", 10),
            new StaffRequirement("Magenheim_Crystal_Earth_Master", 1)),
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

    private sealed class EarthStaffDefinition
    {
        internal EarthStaffDefinition(
            string prefabName,
            string displayName,
            string description,
            string basePrefab,
            int minimumStationLevel,
            float staminaCost,
            float eitrCost,
            float damageMultiplier,
            float forceMultiplier,
            float staggerMultiplier,
            params StaffRequirement[] requirements)
        {
            PrefabName = prefabName;
            DisplayName = displayName;
            Description = description;
            BasePrefab = basePrefab;
            MinimumStationLevel = minimumStationLevel;
            StaminaCost = staminaCost;
            EitrCost = eitrCost;
            DamageMultiplier = damageMultiplier;
            ForceMultiplier = forceMultiplier;
            StaggerMultiplier = staggerMultiplier;
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
