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
/// Owns deliberate crystal grinding and the late-game Prismatic Eitrwine fermentation chain:
/// Rough crystal -> matching shards -> generic Crystal Dust -> fermentable base -> finished alcohol.
/// </summary>
internal sealed class CrystalAlchemyRegistrar : IDisposable
{
    internal const string CrystalDustPrefab = "Magenheim_CrystalDust";
    internal const string EitrwineBasePrefab = "Magenheim_MeadBase_PrismaticEitr";
    internal const string EitrwinePrefab = "Magenheim_Mead_PrismaticEitr";
    internal const int ShardsPerRoughCrystal = 2;
    internal const int ShardsPerDust = 2;
    internal const int FermenterYield = 6;

    private const float EitrOverTime = 300f;
    private const float EitrOverTimeDuration = 12f;
    private const float EitrRegenMultiplier = 1.20f;
    private const float PotionCooldown = 120f;

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal CrystalAlchemyRegistrar(ManualLogSource log) =>
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
                throw new InvalidOperationException("Geologist's Workstation must exist before crystal grinding recipes are registered.");

            RegisterDust();
            RegisterEitrwineBase();
            RegisterEitrwine();
            RegisterGrindingRecipes();
            RegisterEitrwineBaseRecipe();
            RegisterFermentation();

            _registered = true;
            _log.LogInfo(
                "Registered crystal alchemy chain: 1 Rough crystal -> 2 matching shards; 2 shards -> 1 Crystal Dust; " +
                $"Prismatic Eitrwine base -> {FermenterYield} bottles; finished alcohol restores {EitrOverTime:0} Eitr over {EitrOverTimeDuration:0}s " +
                $"and grants x{EitrRegenMultiplier:0.00} Eitr regeneration during its {PotionCooldown:0}s potion window.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Crystal alchemy registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterDust()
    {
        if (PrefabManager.Instance.GetPrefab(CrystalDustPrefab) || CustomItem.IsCustomItem(CrystalDustPrefab))
            throw new InvalidOperationException($"Cannot replace occupied crystal dust identity '{CrystalDustPrefab}'.");
        if (PrefabManager.Instance.GetPrefab("Coal") is null)
            throw new InvalidOperationException("Vanilla Coal prefab is unavailable for Crystal Dust cloning.");

        var item = new CustomItem(CrystalDustPrefab, "Coal");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Crystal Dust";
        shared.m_description =
            "A fine, sparkling powder ground from elemental crystal shards. The grinding destroys the shard's individual alignment, " +
            "leaving a volatile mineral matrix suited to alchemy and fermentation.";
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_maxStackSize = 100;
        shared.m_weight = .05f;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_icons = new[] { CrystalAlchemyIcons.Icon(CrystalAlchemyIcons.Dust) };

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException("Jotunn refused Crystal Dust item registration.");
    }

    private static void RegisterEitrwineBase()
    {
        if (PrefabManager.Instance.GetPrefab(EitrwineBasePrefab) || CustomItem.IsCustomItem(EitrwineBasePrefab))
            throw new InvalidOperationException($"Cannot replace occupied Eitrwine base identity '{EitrwineBasePrefab}'.");
        if (PrefabManager.Instance.GetPrefab("MeadBaseEitrMinor") is null)
            throw new InvalidOperationException("Vanilla MeadBaseEitrMinor prefab is unavailable for Prismatic Eitrwine base cloning.");

        var item = new CustomItem(EitrwineBasePrefab, "MeadBaseEitrMinor");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Prismatic Eitrwine Base";
        shared.m_description =
            "An unstable sweet must carrying suspended Crystal Dust, sap, and royal jelly. It must be fermented before drinking.";
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_icons = new[] { CrystalAlchemyIcons.Icon(CrystalAlchemyIcons.Base) };

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException("Jotunn refused Prismatic Eitrwine base registration.");
    }

    private static void RegisterEitrwine()
    {
        if (PrefabManager.Instance.GetPrefab(EitrwinePrefab) || CustomItem.IsCustomItem(EitrwinePrefab))
            throw new InvalidOperationException($"Cannot replace occupied Eitrwine identity '{EitrwinePrefab}'.");
        if (PrefabManager.Instance.GetPrefab("MeadEitrMinor") is null)
            throw new InvalidOperationException("Vanilla MeadEitrMinor prefab is unavailable for Prismatic Eitrwine cloning.");

        var item = new CustomItem(EitrwinePrefab, "MeadEitrMinor");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Prismatic Eitrwine";
        shared.m_description =
            "A potent crystal-fermented alcohol. The suspended mineral matrix floods the body with Eitr, then keeps the flow elevated. " +
            "Extremely bright, faintly metallic, and inadvisable as ordinary table wine.";
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_icons = new[] { CrystalAlchemyIcons.Icon(CrystalAlchemyIcons.Wine) };

        var sourceEffect = shared.m_consumeStatusEffect as SE_Stats
            ?? throw new InvalidOperationException("Vanilla MeadEitrMinor no longer exposes an SE_Stats consume effect.");
        var effect = UnityEngine.Object.Instantiate(sourceEffect);
        effect.name = "Magenheim_SE_PrismaticEitrwine";
        effect.m_name = "Prismatic Eitrwine";
        effect.m_tooltip =
            $"Restores {EitrOverTime:0} Eitr over {EitrOverTimeDuration:0} seconds and increases Eitr regeneration by " +
            $"{(EitrRegenMultiplier - 1f) * 100f:0}% while the effect remains active.";
        effect.m_ttl = PotionCooldown;
        SetFloat(effect, "m_eitrOverTime", EitrOverTime);
        SetFloat(effect, "m_eitrOverTimeDuration", EitrOverTimeDuration);
        SetFloat(effect, "m_eitrRegenMultiplier", EitrRegenMultiplier);
        effect.m_icon = shared.m_icons[0];

        var customEffect = new CustomStatusEffect(effect, fixReference: false);
        if (!ItemManager.Instance.AddStatusEffect(customEffect))
            throw new InvalidOperationException("Jotunn refused Prismatic Eitrwine status effect registration.");
        shared.m_consumeStatusEffect = customEffect.StatusEffect;

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException("Jotunn refused Prismatic Eitrwine item registration.");
    }

    private static void SetFloat(object target, string fieldName, float value)
    {
        var field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, fieldName);
        if (field.FieldType != typeof(float))
            throw new InvalidOperationException($"{target.GetType().Name}.{fieldName} is not a float.");
        field.SetValue(target, value);
    }

    private static void RegisterGrindingRecipes()
    {
        foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
        {
            var rough = $"Magenheim_Crystal_{element}_Rough";
            var shards = $"Magenheim_Shard_{element}";

            RequirePrefab(rough);
            RequirePrefab(shards);

            var roughConfig = new RecipeConfig
            {
                Name = $"Magenheim_Recipe_{element}_Rough_To_Shards",
                Item = shards,
                Amount = ShardsPerRoughCrystal,
                CraftingStation = WorkshopRegistrar.StationPrefab,
                MinStationLevel = 1,
                Enabled = true,
            };
            roughConfig.AddRequirement(rough, 1);
            if (!ItemManager.Instance.AddRecipe(new CustomRecipe(roughConfig)))
                throw new InvalidOperationException($"Jotunn refused rough-crystal grinding recipe '{roughConfig.Name}'.");

            var dustConfig = new RecipeConfig
            {
                Name = $"Magenheim_Recipe_{element}_Shards_To_Dust",
                Item = CrystalDustPrefab,
                Amount = 1,
                CraftingStation = WorkshopRegistrar.StationPrefab,
                MinStationLevel = 2,
                Enabled = true,
            };
            dustConfig.AddRequirement(shards, ShardsPerDust);
            if (!ItemManager.Instance.AddRecipe(new CustomRecipe(dustConfig)))
                throw new InvalidOperationException($"Jotunn refused shard grinding recipe '{dustConfig.Name}'.");
        }
    }

    private static void RegisterEitrwineBaseRecipe()
    {
        var station = ResolveMeadStation();
        foreach (var prefab in new[] { CrystalDustPrefab, "Honey", "Sap", "RoyalJelly" })
            RequirePrefab(prefab);

        var config = new RecipeConfig
        {
            Name = "Magenheim_Recipe_PrismaticEitrwineBase",
            Item = EitrwineBasePrefab,
            Amount = 1,
            CraftingStation = station,
            MinStationLevel = 1,
            Enabled = true,
        };
        config.AddRequirement(CrystalDustPrefab, 10);
        config.AddRequirement("Honey", 10);
        config.AddRequirement("Sap", 5);
        config.AddRequirement("RoyalJelly", 5);

        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException("Jotunn refused Prismatic Eitrwine base recipe.");
    }

    private static void RegisterFermentation()
    {
        var config = new FermenterConversionConfig
        {
            FromItem = EitrwineBasePrefab,
            ToItem = EitrwinePrefab,
            ProducedItems = FermenterYield,
        };

        if (!ItemManager.Instance.AddItemConversion(new CustomItemConversion(config)))
            throw new InvalidOperationException("Jotunn refused Prismatic Eitrwine fermenter conversion.");
    }

    private static string ResolveMeadStation()
    {
        foreach (var candidate in new[] { "piece_meadcauldron", "piece_cauldron" })
            if (PrefabManager.Instance.GetPrefab(candidate))
                return candidate;

        throw new InvalidOperationException("Neither Mead Ketill nor cauldron crafting station is available for Eitrwine base production.");
    }

    private static void RequirePrefab(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null && !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException($"Required crystal alchemy prefab '{prefab}' is unavailable.");
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }
}
