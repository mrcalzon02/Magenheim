using System;
using BepInEx.Configuration;
using HarmonyLib;
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
    private static ConfigEntry<float>? _poisonDamageReduction;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _poisonDamageReduction = config.Bind(
            "Balance.Underworld.DeepBoons.SporeCommunion",
            "PoisonDamageReduction",
            DefaultPoisonDamageReduction,
            "Fraction of incoming poison damage removed while Spore Communion is the player's active Deep Boon. Clamped to 0..0.90; this is resistance, never immunity.");
    }

    internal static void MitigatePoison(Player player, HitData hit)
    {
        if (player is null || hit is null || !DeepBoonRuntime.IsActive(player, DeepBoonId)) return;
        var configured = _poisonDamageReduction?.Value ?? DefaultPoisonDamageReduction;
        var reduction = Mathf.Clamp(configured, 0f, 0.90f);
        if (reduction <= 0f || hit.m_damage.m_poison <= 0f) return;
        hit.m_damage.m_poison *= 1f - reduction;
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
