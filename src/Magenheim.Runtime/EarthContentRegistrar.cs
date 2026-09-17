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

    private static Magenheim.Core.Socketing.SocketEffectDefinitionSet? _socketEffects;

    internal EarthContentRegistrar(ManualLogSource log) => _log = log;

    internal EarthContentRegistrar(ManualLogSource log, Magenheim.Core.Socketing.SocketEffectDefinitionSet socketEffects)
        : this(log) => _socketEffects = socketEffects;

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
            var crystalCount = 0;
            var shardCount = 0;
            foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            {
                foreach (CrystalTier tier in Enum.GetValues(typeof(CrystalTier)))
                {
                    AddCrystal(element, tier);
                    crystalCount++;
                }

                AddShard(element);
                shardCount++;
            }

            _registered = true;
            _log.LogInfo(
                $"Registered {crystalCount} elemental crystal items across {Enum.GetValues(typeof(ElementalAlignment)).Length} alignments " +
                $"plus {shardCount} matching shard items using the Magenheim crystal geometry family.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Elemental crystal content registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static void AddCrystal(ElementalAlignment element, CrystalTier tier)
    {
        var prefab = $"Magenheim_Crystal_{element}_{tier}";
        var asset = tier.ToString().ToLowerInvariant();
        var displayName = tier == CrystalTier.Crystal
            ? $"{element} Crystal"
            : $"{tier} {element} Crystal";
        var description = $"A {element}-aligned {tier.ToString().ToLowerInvariant()} crystal. {ElementVisualPalette.Essence(element)}";
        // Reported from play: a crystal did not say what it does in each kind of slot, so the
        // player could not tell before committing the socket. The lines are generated from the
        // same validated rules the runtime calculates with, so they cannot drift from balance.
        if (_socketEffects is not null)
            description += System.Environment.NewLine + System.Environment.NewLine
                + Magenheim.Core.Socketing.SocketEffectDescription.Tooltip(element, tier, _socketEffects);
        AddItem(prefab, asset, displayName, description, .3f, element);
    }

    private static void AddShard(ElementalAlignment element)
    {
        AddItem(
            $"Magenheim_Shard_{element}",
            "shards",
            $"{element} Crystal Shards",
            $"Fragments of {element}-aligned crystal recovered from a failed shaping attempt. {ElementVisualPalette.Essence(element)}",
            .1f,
            element);
    }

    private static void AddItem(
        string prefab,
        string asset,
        string name,
        string description,
        float weight,
        ElementalAlignment element)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) || CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException($"Cannot replace occupied elemental item identity '{prefab}'.");

        var item = new CustomItem(prefab, "Stone");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = name;
        shared.m_description = description;
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_maxStackSize = 50;
        shared.m_weight = weight;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        if (element == ElementalAlignment.Earth)
        {
            shared.m_icons = new[] { EarthAssets.Icon(asset) };
            EarthAssets.ReplaceVisual(item.ItemPrefab, asset);
        }
        else
        {
            var variant = ElementVisualPalette.Variant(element);
            var tint = ElementVisualPalette.Tint(element);
            shared.m_icons = new[] { EarthAssets.Icon(asset, variant, tint) };
            EarthAssets.ReplaceVisual(item.ItemPrefab, asset, variant: variant, tint: tint);
        }

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused elemental item '{prefab}'.");
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterItems;
        _subscribed = false;
    }
}
