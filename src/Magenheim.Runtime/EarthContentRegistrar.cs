using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;

namespace Magenheim.Runtime;

internal sealed class EarthContentRegistrar : IDisposable
{
    internal const string SkillId = "magenheim.crystal_shaping";
    internal static Skills.SkillType CrystalShapingSkill { get; private set; }
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal EarthContentRegistrar(ManualLogSource log) => _log = log;

    internal void Register()
    {
        if (_subscribed || _registered) return;
        var skill = SkillManager.Instance.AddSkill(new SkillConfig
        {
            Identifier = SkillId,
            Name = "Crystal Shaping",
            Description = "Knowledge of elemental minerals and the craft of shaping crystals.",
            Icon = EarthAssets.Icon("crystal-shaping"),
            IncreaseStep = 1f
        });
        CrystalShapingSkill = skill;
        _log.LogInfo($"Registered Crystal Shaping skill '{SkillId}' ({skill}).");
        PrefabManager.OnVanillaPrefabsAvailable += RegisterItems;
        _subscribed = true;
    }

    private void RegisterItems()
    {
        if (_registered) return;
        try
        {
            foreach (CrystalTier tier in Enum.GetValues(typeof(CrystalTier)))
            {
                AddItem("Magenheim_Crystal_Earth_" + tier, tier.ToString().ToLowerInvariant(),
                    tier == CrystalTier.Crystal ? "Earth Crystal" : tier + " Earth Crystal",
                    "An Earth-aligned crystal. Its warm ochre facets hold the quiet strength of stone.", .3f);
            }
            AddItem("Magenheim_Shard_Earth", "shards", "Earth Crystal Shards",
                "Small fragments of Earth crystal, recovered when a larger mineral fractures.", .1f);
            _registered = true;
            _log.LogInfo("Registered 5 Earth crystal tiers and Earth Crystal Shards with original meshes, textures, and icons.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Earth content registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static void AddItem(string prefab, string asset, string name, string description, float weight)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) || CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException($"Cannot replace occupied Earth item identity '{prefab}'.");
        var item = new CustomItem(prefab, "Stone");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = name;
        shared.m_description = description;
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_maxStackSize = 50;
        shared.m_weight = weight;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_icons = new[] { EarthAssets.Icon(asset) };
        EarthAssets.ReplaceVisual(item.ItemPrefab, asset);
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused Earth item '{prefab}'.");
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterItems;
        _subscribed = false;
    }
}
