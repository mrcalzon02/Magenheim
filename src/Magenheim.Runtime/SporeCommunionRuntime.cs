using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// First concrete Deep Boon behavior. Spore Communion is projected only through
/// DeepBoonRuntime, so durable selection and Conclave unlock authority remain upstream.
/// </summary>
internal static class SporeCommunionRuntime
{
    internal const string DeepBoonId = "spore_communion";
    private const float DefaultPoisonDamageReduction = 0.35f;
    private const float DefaultFungalProvisionEfficiencyBonus = 0.20f;
    private static readonly FieldInfo? PlayerFoodsField = AccessTools.Field(typeof(Player), "m_foods");
    private static ConfigEntry<float>? _poisonDamageReduction;
    private static ConfigEntry<float>? _fungalProvisionEfficiencyBonus;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _poisonDamageReduction = config.Bind(
            "Balance.Underworld.DeepBoons.SporeCommunion",
            "PoisonDamageReduction",
            DefaultPoisonDamageReduction,
            "Fraction of incoming poison damage removed while Spore Communion is the player's active Deep Boon. Clamped to 0..0.90; this is resistance, never immunity.");
        _fungalProvisionEfficiencyBonus = config.Bind(
            "Balance.Underworld.DeepBoons.SporeCommunion",
            "FungalProvisionEfficiencyBonus",
            DefaultFungalProvisionEfficiencyBonus,
            "Fractional efficiency bonus applied only to canonical Magenheim Underworld fungal provisions while Spore Communion is active. Core clamps this to 0..0.50.");
    }

    internal static void MitigatePoison(Player player, HitData hit)
    {
        if (player is null || hit is null || !DeepBoonRuntime.IsActive(player, DeepBoonId)) return;
        var configured = _poisonDamageReduction?.Value ?? DefaultPoisonDamageReduction;
        var reduction = Mathf.Clamp(configured, 0f, 0.90f);
        if (reduction <= 0f || hit.m_damage.m_poison <= 0f) return;
        hit.m_damage.m_poison *= 1f - reduction;
    }

    /// <summary>
    /// Narrow Runtime adapter for provision consumption. It deliberately resolves only
    /// the consumed item's prefab identity and delegates eligibility/bounds to Core.
    /// No ItemDrop.SharedData or prefab state is mutated here.
    /// </summary>
    internal static float ProvisionEfficiencyMultiplier(Player player, ItemDrop.ItemData? item)
    {
        if (player is null || item is null || !DeepBoonRuntime.IsActive(player, DeepBoonId)) return 1f;

        var prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        var configured = _fungalProvisionEfficiencyBonus?.Value ?? DefaultFungalProvisionEfficiencyBonus;
        return UnderworldFungalProvisionCatalog.EfficiencyMultiplier(prefabName, configured);
    }

    /// <summary>
    /// Applies Spore Communion only after Valheim has successfully created/replaced the
    /// consuming player's Food entry. The entry is player-owned state; shared item data
    /// and prefabs remain untouched, preventing cross-player amplification.
    /// </summary>
    internal static void ApplyConsumedProvision(Player player, ItemDrop.ItemData? consumedItem)
    {
        var multiplier = ProvisionEfficiencyMultiplier(player, consumedItem);
        if (multiplier <= 1f || consumedItem is null || PlayerFoodsField is null) return;
        if (PlayerFoodsField.GetValue(player) is not List<Food> foods) return;

        for (var index = foods.Count - 1; index >= 0; index--)
        {
            var food = foods[index];
            if (food is null || !ReferenceEquals(food.m_item, consumedItem)) continue;

            food.m_health *= multiplier;
            food.m_stamina *= multiplier;
            food.m_eitr *= multiplier;
            return;
        }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class SporeCommunionDamagePatch
{
    private static void Prefix(Character __instance, HitData hit)
    {
        if (__instance is Player player)
            SporeCommunionRuntime.MitigatePoison(player, hit);
    }
}

[HarmonyPatch(typeof(Player), "EatFood", new Type[] { typeof(ItemDrop.ItemData) })]
internal static class SporeCommunionFoodPatch
{
    private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result)
    {
        if (__result)
            SporeCommunionRuntime.ApplyConsumedProvision(__instance, item);
    }
}
