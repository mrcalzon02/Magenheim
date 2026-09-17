using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Concrete Deep Current adaptations. Selection authority remains in DeepBoonRuntime;
/// this adapter only modifies player-owned costs/state already being processed by Valheim.
/// </summary>
internal static class DeepCurrentRuntime
{
    internal const string DeepBoonId = "deep_current";
    private const float DefaultSwimmingStaminaReduction = 0.25f;
    private const float DefaultWetDurationReduction = 0.35f;
    private static readonly FieldInfo? StatusEffectCharacterField = AccessTools.Field(typeof(StatusEffect), "m_character");
    private static readonly FieldInfo? StatusEffectTimeField = AccessTools.Field(typeof(StatusEffect), "m_time");
    private static ConfigEntry<float>? _swimmingStaminaReduction;
    private static ConfigEntry<float>? _wetDurationReduction;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _swimmingStaminaReduction = config.Bind(
            "Balance.Underworld.DeepBoons.DeepCurrent",
            "SwimmingStaminaReduction",
            DefaultSwimmingStaminaReduction,
            "Fraction of swimming stamina cost removed while Deep Current is the player's active Deep Boon. Clamped to 0..0.60 so swimming still has a meaningful cost.");
        _wetDurationReduction = config.Bind(
            "Balance.Underworld.DeepBoons.DeepCurrent",
            "WetDurationReduction",
            DefaultWetDurationReduction,
            "Fraction of Wet recovery time removed while Deep Current is the player's active Deep Boon. Clamped to 0..0.60; Wet is never suppressed and being exposed to water can still refresh it normally.");
    }

    internal static float AdjustSwimmingStaminaCost(Player player, float requestedStamina)
    {
        if (player is null || requestedStamina <= 0f || !player.IsSwimming() || !DeepBoonRuntime.IsActive(player, DeepBoonId))
            return requestedStamina;

        var configured = _swimmingStaminaReduction?.Value ?? DefaultSwimmingStaminaReduction;
        var reduction = Mathf.Clamp(configured, 0f, 0.60f);
        return requestedStamina * (1f - reduction);
    }

    internal static void AdvanceWetRecovery(SE_Wet wet, float deltaTime)
    {
        if (wet is null || deltaTime <= 0f || StatusEffectCharacterField is null || StatusEffectTimeField is null)
            return;

        if (StatusEffectCharacterField.GetValue(wet) is not Player player || !DeepBoonRuntime.IsActive(player, DeepBoonId))
            return;

        var configured = _wetDurationReduction?.Value ?? DefaultWetDurationReduction;
        var reduction = Mathf.Clamp(configured, 0f, 0.60f);
        if (reduction <= 0f)
            return;

        // UpdateStatusEffect has already advanced m_time by deltaTime. Add only the
        // extra elapsed time needed to make the remaining Wet lifetime (1-reduction)
        // of vanilla without touching the shared SE_Wet prefab or its TTL.
        var current = StatusEffectTimeField.GetValue(wet);
        if (current is not float elapsed)
            return;

        var extraElapsed = deltaTime * (reduction / (1f - reduction));
        StatusEffectTimeField.SetValue(wet, elapsed + extraElapsed);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
internal static class DeepCurrentSwimmingStaminaPatch
{
    private static void Prefix(Player __instance, ref float v)
    {
        v = DeepCurrentRuntime.AdjustSwimmingStaminaCost(__instance, v);
    }
}

[HarmonyPatch(typeof(SE_Wet), nameof(SE_Wet.UpdateStatusEffect))]
internal static class DeepCurrentWetRecoveryPatch
{
    private static void Postfix(SE_Wet __instance, float dt)
    {
        DeepCurrentRuntime.AdvanceWetRecovery(__instance, dt);
    }
}
