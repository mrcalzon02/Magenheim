using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers Magenheim's physical crystal weapon family. These weapons preserve ordinary
/// class damage/attack behavior from compatible vanilla donors while trading material cost
/// for extraordinary durability and original crystal geometry.
/// </summary>
internal sealed class CrystalWeaponRegistrar : IDisposable
{
    private const float DurabilityPerQuality = 250f;

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal CrystalWeaponRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterWeapons;
        _subscribed = true;
    }

    private void RegisterWeapons()
    {
        if (_registered) return;

        try
        {
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException(
                    "Geologist's Workstation must exist before crystal weapon recipes are registered.");

            foreach (var definition in Definitions())
            {
                RegisterWeapon(definition);
                RegisterRecipe(definition);
            }

            _registered = true;
            _log.LogInfo(
                "Registered 10-piece Magenheim crystal weapon set with 1200-1900 base durability " +
                "and +250 durability per quality level.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Crystal weapon registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterWeapon(CrystalWeaponDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName) ||
            CustomItem.IsCustomItem(definition.PrefabName))
            throw new InvalidOperationException(
                $"Cannot replace occupied crystal weapon identity '{definition.PrefabName}'.");

        var source = ResolveSource(definition.SourceCandidates);
        var item = new CustomItem(definition.PrefabName, source);
        var shared = item.ItemDrop.m_itemData.m_shared;

        var sourceDamage = shared.m_damages;
        var sourceDamagePerLevel = shared.m_damagesPerLevel;

        shared.m_name = definition.DisplayName;
        shared.m_description =
            definition.Description + " Its defining advantage is service life: " +
            $"{definition.BaseDurability:0} base durability and +{DurabilityPerQuality:0} per quality level.";
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_maxQuality = 4;
        shared.m_useDurability = true;
        shared.m_maxDurability = definition.BaseDurability;
        shared.m_durabilityPerLevel = DurabilityPerQuality;
        shared.m_damages = PhysicalOnly(sourceDamage);
        shared.m_damagesPerLevel = PhysicalOnly(sourceDamagePerLevel);
        // Rendered from the same .blend the runtime mesh is exported from, the way the staff
        // family already works. CrystalWeaponIcons.cs drew these procedurally in C#, so they
        // described the weapons only as well as code could draw them and drifted the moment a
        // model was re-authored. tools/render-weapon-icons.py owns them now.
        shared.m_icons = new[] { EarthAssets.Icon(definition.ModelId) };

        CrystalWeaponVisuals.Apply(item.ItemPrefab, definition.ModelId);

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException(
                $"Jotunn refused crystal weapon item '{definition.PrefabName}'.");
    }

    private static HitData.DamageTypes PhysicalOnly(HitData.DamageTypes source) =>
        new()
        {
            m_blunt = source.m_blunt,
            m_slash = source.m_slash,
            m_pierce = source.m_pierce,
            m_chop = source.m_chop,
        };

    private static void RegisterRecipe(CrystalWeaponDefinition definition)
    {
        var config = new RecipeConfig
        {
            Name = "Magenheim_Recipe_" + definition.PrefabName,
            Item = definition.PrefabName,
            Amount = 1,
            CraftingStation = WorkshopRegistrar.StationPrefab,
            RepairStation = WorkshopRegistrar.StationPrefab,
            MinStationLevel = 4,
            Enabled = true,
            Requirements = definition.Requirements,
        };

        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException(
                $"Jotunn refused crystal weapon recipe '{config.Name}'.");
    }

    private static string ResolveSource(IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (PrefabManager.Instance.GetPrefab(candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            "None of the compatible vanilla weapon donors are available: " +
            string.Join(", ", candidates));
    }

    private static RequirementConfig Cost(string item, int amount, int amountPerLevel) =>
        new(item, amount, amountPerLevel, false);

    private static IReadOnlyList<CrystalWeaponDefinition> Definitions() => new[]
    {
        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalSword", CrystalWeaponVisuals.Sword,
            "Crystal Sword",
            "A one-handed sword built around a black-metal tang and a fused faceted crystal blade. " +
            "Its physical damage remains in the ordinary sword envelope rather than gaining an elemental payload.",
            1500f,
            new[] { "SwordBlackmetal", "SwordSilver", "SwordIron" },
            Cost(StructuralCrystalRegistrar.PrefabName, 9, 4), Cost("BlackMetal", 12, 6), Cost("FineWood", 2, 1)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalGreatsword", CrystalWeaponVisuals.Greatsword,
            "Crystal Greatsword",
            "A two-handed crystal slab reinforced by a dark metal spine. The weapon is deliberately conventional in damage, " +
            "but the fused lattice tolerates an absurd amount of campaigning.",
            1800f,
            new[] { "THSwordKrom", "THSwordSlayer", "THSwordGold" },
            Cost(StructuralCrystalRegistrar.PrefabName, 15, 6), Cost("BlackMetal", 18, 8), Cost("Iron", 10, 5), Cost("LinenThread", 4, 2)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalAxe", CrystalWeaponVisuals.Axe,
            "Crystal Axe",
            "A compact black-metal haft carrying a thick translucent crystal wedge. It remains a practical chopping weapon " +
            "whose exceptional value is how slowly its edge and body wear.",
            1600f,
            new[] { "AxeBlackMetal", "AxeJotunBane", "AxeIron" },
            Cost(StructuralCrystalRegistrar.PrefabName, 10, 4), Cost("BlackMetal", 14, 6), Cost("FineWood", 4, 2)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalBattleaxe", CrystalWeaponVisuals.Battleaxe,
            "Crystal Battleaxe",
            "A broad double-headed battleaxe grown around an iron-black spine. Its immense crystal heads favor endurance " +
            "over exotic damage and can survive long expeditions without constant repair.",
            1900f,
            new[] { "BattleaxeBlackmetal", "BattleaxeCrystal", "Battleaxe" },
            Cost(StructuralCrystalRegistrar.PrefabName, 17, 7), Cost("BlackMetal", 20, 10), Cost("ElderBark", 10, 4)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalMace", CrystalWeaponVisuals.Mace,
            "Crystal Mace",
            "A dark metal core surrounded by interlocked crystal striking facets. It gains no hidden elemental damage; " +
            "its advantage is a head that simply refuses to wear out.",
            1700f,
            new[] { "MaceSilver", "MaceIron", "MaceBronze" },
            Cost(StructuralCrystalRegistrar.PrefabName, 11, 5), Cost("BlackMetal", 12, 6), Cost("Iron", 6, 3), Cost("FineWood", 2, 1)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalSpear", CrystalWeaponVisuals.Spear,
            "Crystal Spear",
            "A long dark shaft capped by a deeply socketed crystal spearhead. Its ordinary piercing performance is paired " +
            "with a reinforced socket designed to survive repeated thrusts and throws.",
            1500f,
            new[] { "SpearCarapace", "SpearWolfFang", "SpearBronze" },
            Cost(StructuralCrystalRegistrar.PrefabName, 8, 4), Cost("BlackMetal", 8, 4), Cost("YggdrasilWood", 8, 4)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalKnife", CrystalWeaponVisuals.Knife,
            "Crystal Knife",
            "A short monolithic crystal blade clamped into a black-metal grip. It keeps the speed and modest reach of a knife " +
            "while surviving far more use than its size suggests.",
            1200f,
            new[] { "KnifeBlackMetal", "KnifeSilver", "KnifeChitin" },
            Cost(StructuralCrystalRegistrar.PrefabName, 5, 3), Cost("BlackMetal", 8, 4), Cost("FineWood", 2, 1)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalAtgeir", CrystalWeaponVisuals.Atgeir,
            "Crystal Atgeir",
            "A polearm with a long crystal spear point and two reinforced side blades. It retains the familiar atgeir attack set " +
            "while its crystal-metal spine provides extreme structural endurance.",
            1800f,
            new[] { "AtgeirBlackmetal", "AtgeirHimminAfl", "AtgeirIron" },
            Cost(StructuralCrystalRegistrar.PrefabName, 14, 6), Cost("BlackMetal", 18, 8), Cost("FineWood", 8, 4), Cost("LinenThread", 4, 2)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalBow", CrystalWeaponVisuals.Bow,
            "Crystal Bow",
            "A composite bow whose dark central riser carries segmented crystal limbs. Its draw characteristics remain conventional; " +
            "the crystal laminate is built to endure thousands of shots.",
            1400f,
            new[] { "BowSpineSnap", "BowDraugrFang", "BowHuntsman" },
            Cost(StructuralCrystalRegistrar.PrefabName, 10, 4), Cost("FineWood", 12, 5), Cost("LinenThread", 8, 4), Cost("BlackMetal", 4, 2)),

        new CrystalWeaponDefinition(
            "Magenheim_Weapon_CrystalCrossbow", CrystalWeaponVisuals.Crossbow,
            "Crystal Crossbow",
            "A heavy crossbow framed with crystal limbs around a reinforced black-metal stock. It does not out-damage its class donor; " +
            "it is built to remain fieldworthy for an exceptionally long time.",
            1700f,
            new[] { "CrossbowArbalest" },
            Cost(StructuralCrystalRegistrar.PrefabName, 12, 5), Cost("BlackMetal", 14, 7), Cost("YggdrasilWood", 10, 5), Cost("Iron", 6, 3)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterWeapons;
        _subscribed = false;
    }

    private sealed class CrystalWeaponDefinition
    {
        internal CrystalWeaponDefinition(
            string prefabName,
            string modelId,
            string displayName,
            string description,
            float baseDurability,
            IReadOnlyList<string> sourceCandidates,
            params RequirementConfig[] requirements)
        {
            PrefabName = prefabName;
            ModelId = modelId;
            DisplayName = displayName;
            Description = description;
            BaseDurability = baseDurability;
            SourceCandidates = sourceCandidates;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string ModelId { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal float BaseDurability { get; }
        internal IReadOnlyList<string> SourceCandidates { get; }
        internal RequirementConfig[] Requirements { get; }
    }
}
