using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Concrete Deep Current adaptation. Selection authority remains in DeepBoonRuntime;
/// this adapter only modifies the stamina charge that Valheim is already about to apply.
/// </summary>
internal static class DeepCurrentRuntime
{
    internal const string DeepBoonId = "deep_current";
    private const float DefaultSwimmingStaminaReduction = 0.25f;
    private static ConfigEntry<float>? _swimmingStaminaReduction;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _swimmingStaminaReduction = config.Bind(
            "Balance.Underworld.DeepBoons.DeepCurrent",
            "SwimmingStaminaReduction",
            DefaultSwimmingStaminaReduction,
            "Fraction of swimming stamina cost removed while Deep Current is the player's active Deep Boon. Clamped to 0..0.60 so swimming still has a meaningful cost.");
    }

    internal static float AdjustSwimmingStaminaCost(Player player, float requestedStamina)
    {
        if (player is null || requestedStamina <= 0f || !player.IsSwimming() || !DeepBoonRuntime.IsActive(player, DeepBoonId))
            return requestedStamina;

        var configured = _swimmingStaminaReduction?.Value ?? DefaultSwimmingStaminaReduction;
        var reduction = Mathf.Clamp(configured, 0f, 0.60f);
        return requestedStamina * (1f - reduction);
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
