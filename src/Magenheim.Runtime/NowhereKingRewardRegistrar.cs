using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the persistent one-time rewards released by the Dark Throne encounter.</summary>
internal sealed class NowhereKingRewardRegistrar : IDisposable
{
    internal const string NullMantlePrefabName = "Magenheim_NullMantle";
    internal const string TrophyPrefabName = "Magenheim_TrophyNowhereKing";
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
            _registered = true;
            _log.LogInfo($"Registered Nowhere King rewards '{NullMantlePrefabName}' and '{TrophyPrefabName}'.");
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

        // The 1.0 Crown of Valheim is the authoritative visual/equipment donor. Do not replace it
        // with a procedural cylinder or another hand-built approximation.
        var item = new CustomItem(NullMantlePrefabName, VanillaCrownPrefabName);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Null Mantle";
        shared.m_description = "The broken crown of the Nowhere King. The void behind it remembers its sovereign.";
        shared.m_maxStackSize = 1;
        shared.m_weight = 2f;
        shared.m_value = 0;
        Darken(item.ItemPrefab, new Color(.012f, .008f, .015f, 1f), "magenheim.null-mantle");
        if (!ItemManager.Instance.AddItem(item)) throw new InvalidOperationException($"Jotunn refused reward '{NullMantlePrefabName}'.");
    }

    private static void RegisterTrophy()
    {
        if (PrefabManager.Instance.GetPrefab(TrophyPrefabName) != null) throw new InvalidOperationException($"Reward identity '{TrophyPrefabName}' is occupied.");
        var item = new CustomItem(TrophyPrefabName, "TrophyDvergr");
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = "Trophy: The Nowhere King";
        shared.m_description = "A blackened royal remnant from the throne at the end of nowhere.";
        shared.m_maxStackSize = 20;
        shared.m_weight = 2f;
        shared.m_value = 0;
        Darken(item.ItemPrefab, new Color(.02f, .006f, .01f, 1f), "magenheim.nowhere-king-trophy");
        if (!ItemManager.Instance.AddItem(item)) throw new InvalidOperationException($"Jotunn refused reward '{TrophyPrefabName}'.");
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
                // Preserve Iron Gate's authored Crown texture/normal maps; only change the palette
                // and material response needed for the Null Mantle identity.
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
