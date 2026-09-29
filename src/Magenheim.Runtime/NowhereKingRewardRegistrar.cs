using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the Dark Throne's recoverable equipment and trophy identities.</summary>
internal sealed class NowhereKingRewardRegistrar : IDisposable
{
    internal const string NullMantlePrefabName = "Magenheim_NullMantle";
    internal const string TrophyPrefabName = "Magenheim_TrophyNowhereKing";
    internal const string FirmamentPrefabName = "Magenheim_LastArgument_Firmament";
    internal const string NullGatePrefabName = "Magenheim_LastArgument_NullGate";
    internal const string PairedLastArgumentPrefabName = "Magenheim_LastArgument_Paired";
    internal const string FirmamentModelId = "nowhere-king-sword-firmament";
    internal const string NullGateModelId = "nowhere-king-sword-null-gate";
    internal const string VanillaCrownPrefabName = "HelmetCrownofValheim";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal NowhereKingRewardRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

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
            RegisterNullMantle();
            RegisterTrophy();
            RegisterRoyalSword(
                FirmamentPrefabName,
                FirmamentModelId,
                "Firmament",
                "One half of the Last Argument. Its blade contains an impossible night of stars rather than ordinary steel.");
            RegisterRoyalSword(
                NullGatePrefabName,
                NullGateModelId,
                "Null Gate",
                "One half of the Last Argument. The framed blade is less a surface than an aperture into nowhere.");
            RegisterPairedLastArgument();

            _registered = true;
            _log.LogInfo(
                $"Registered Nowhere King rewards '{NullMantlePrefabName}', '{TrophyPrefabName}', " +
                $"'{FirmamentPrefabName}', '{NullGatePrefabName}', and craftable paired weapon '{PairedLastArgumentPrefabName}'.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Nowhere King reward registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static void RegisterNullMantle()
    {
        if (PrefabManager.Instance.GetPrefab(NullMantlePrefabName) != null)
            throw new InvalidOperationException($"Reward identity '{NullMantlePrefabName}' is occupied.");
        if (PrefabManager.Instance.GetPrefab(VanillaCrownPrefabName) == null)
            throw new InvalidOperationException($"Null Mantle requires vanilla Crown of Valheim prefab '{VanillaCrownPrefabName}'.");

        var item = new CustomItem(NullMantlePrefabName, VanillaCrownPrefabName);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Null Mantle";
        shared.m_description = "The broken crown of the Nowhere King. The void behind it remembers its sovereign.";
        shared.m_maxStackSize = 1;
        shared.m_weight = 2f;
        shared.m_value = 0;
        Darken(item.ItemPrefab, new Color(.012f, .008f, .015f, 1f), "magenheim.null-mantle");
        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused reward '{NullMantlePrefabName}'.");
    }

    private static void RegisterTrophy()
    {
        if (PrefabManager.Instance.GetPrefab(TrophyPrefabName) != null)
            throw new InvalidOperationException($"Reward identity '{TrophyPrefabName}' is occupied.");

        var item = new CustomItem(TrophyPrefabName, "TrophyDvergr");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Trophy: The Nowhere King";
        shared.m_description = "A blackened royal remnant from the throne at the end of nowhere.";
        shared.m_maxStackSize = 20;
        shared.m_weight = 2f;
        shared.m_value = 0;
        Darken(item.ItemPrefab, new Color(.02f, .006f, .01f, 1f), "magenheim.nowhere-king-trophy");

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused reward '{TrophyPrefabName}'.");
    }

    private static void RegisterRoyalSword(string prefabName, string modelId, string displayName, string description)
    {
        if (PrefabManager.Instance.GetPrefab(prefabName) != null || CustomItem.IsCustomItem(prefabName))
            throw new InvalidOperationException($"Reward identity '{prefabName}' is occupied.");
        if (PrefabManager.Instance.GetPrefab("SwordBlackmetal") == null)
            throw new InvalidOperationException($"{displayName} requires vanilla sword donor 'SwordBlackmetal'.");

        var item = new CustomItem(prefabName, "SwordBlackmetal");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = displayName;
        shared.m_description = description + " Recovered from the paired royal armament called the Last Argument.";
        shared.m_maxStackSize = 1;
        shared.m_weight = 2.4f;
        shared.m_value = 0;
        shared.m_maxQuality = 4;
        shared.m_useDurability = true;
        shared.m_maxDurability = 2200f;
        shared.m_durabilityPerLevel = 300f;
        shared.m_icons = new[] { EarthAssets.Icon(modelId) };
        ModelAssets.Load(item.ItemPrefab, modelId, item: true);

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused royal sword reward '{prefabName}'.");
    }

    private static void RegisterPairedLastArgument()
    {
        if (PrefabManager.Instance.GetPrefab(PairedLastArgumentPrefabName) != null ||
            CustomItem.IsCustomItem(PairedLastArgumentPrefabName))
            throw new InvalidOperationException($"Weapon identity '{PairedLastArgumentPrefabName}' is occupied.");

        var knifeDonor = PrefabManager.Instance.GetPrefab("KnifeBlackMetal")
            ?? PrefabManager.Instance.GetPrefab("KnifeSilver")
            ?? PrefabManager.Instance.GetPrefab("KnifeChitin")
            ?? throw new InvalidOperationException("The Last Argument requires a vanilla knife donor.");
        var donorName = knifeDonor.name;
        var item = new CustomItem(PairedLastArgumentPrefabName, donorName);
        var shared = item.ItemDrop.m_itemData.m_shared;

        // Keep a knife's compact reach/handling, but occupy both hands. If Flesh Rippers are
        // available, borrow their native alternating-hand attack choreography while retaining
        // Knives skill progression and this item's own damage/durability authority.
        var rippers = PrefabManager.Instance.GetPrefab("FistFenrirClaw")?.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
        if (rippers is not null)
        {
            shared.m_attack = rippers.m_attack;
            shared.m_secondaryAttack = rippers.m_secondaryAttack;
        }

        shared.m_name = "The Last Argument";
        shared.m_description =
            "Firmament and Null Gate rebound for mortal hands. The King's enormous paired swords are " +
            "carried as two compact void-edged knives: Firmament in the right hand, Null Gate in the left.";
        shared.m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon;
        shared.m_skillType = Skills.SkillType.Knives;
        shared.m_maxStackSize = 1;
        shared.m_weight = 1.6f;
        shared.m_value = 0;
        shared.m_maxQuality = 4;
        shared.m_useDurability = true;
        shared.m_maxDurability = 2400f;
        shared.m_durabilityPerLevel = 325f;
        shared.m_icons = new[] { EarthAssets.Icon(FirmamentModelId) };

        // The boss assets are ~1.57m at player scale because the King enlarges them to ~2.8m.
        // 0.43 produces ~0.68m blades: large knives rather than player-scale boss swords.
        ModelAssets.Load(item.ItemPrefab, FirmamentModelId, item: true, scale: LastArgumentPlayerVisualRuntime.PlayerBladeScale);

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException($"Jotunn refused paired weapon '{PairedLastArgumentPrefabName}'.");
    }

    private static void Darken(GameObject prefab, Color tint, string materialPrefix)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var originals = renderer.sharedMaterials;
            if (originals is null || originals.Length == 0) continue;

            var replacements = new Material[originals.Length];
            for (var i = 0; i < originals.Length; i++)
            {
                var source = originals[i];
                if (!source) continue;
                var material = new Material(source) { name = $"{materialPrefix}.{renderer.name}.{i}" };
                if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .78f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .34f);
                replacements[i] = material;
            }
            renderer.sharedMaterials = replacements;
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }
}
