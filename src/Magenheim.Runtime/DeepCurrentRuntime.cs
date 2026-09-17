using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DeepCurrentRuntime
{
    internal const string DeepBoonId = "deep_current";
    private const float DefaultSwimmingStaminaReduction = 0.25f;
    private const float DefaultWetDurationReduction = 0.35f;
    private const float DefaultVesselHandlingBonus = 0.20f;
    private static readonly FieldInfo? StatusEffectCharacterField = AccessTools.Field(typeof(StatusEffect), "m_character");
    private static readonly FieldInfo? StatusEffectTimeField = AccessTools.Field(typeof(StatusEffect), "m_time");
    private static ConfigEntry<float>? _swimmingStaminaReduction;
    private static ConfigEntry<float>? _wetDurationReduction;
    private static ConfigEntry<float>? _vesselHandlingBonus;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _swimmingStaminaReduction = config.Bind("Balance.Underworld.DeepBoons.DeepCurrent", "SwimmingStaminaReduction", DefaultSwimmingStaminaReduction, "Fraction of swimming stamina cost removed while Deep Current is active. Clamped to 0..0.60.");
        _wetDurationReduction = config.Bind("Balance.Underworld.DeepBoons.DeepCurrent", "WetDurationReduction", DefaultWetDurationReduction, "Fraction of Wet recovery time removed while Deep Current is active. Clamped to 0..0.60.");
        _vesselHandlingBonus = config.Bind("Balance.Underworld.DeepBoons.DeepCurrent", "VesselHandlingBonus", DefaultVesselHandlingBonus, "Handling bonus for canonical Magenheim Underworld vessels while Deep Current is active. Core clamps this to +50%.");
    }

    internal static float AdjustSwimmingStaminaCost(Player player, float requestedStamina)
    {
        if (player is null || requestedStamina <= 0f || !player.IsSwimming() || !DeepBoonRuntime.IsActive(player, DeepBoonId)) return requestedStamina;
        var reduction = Mathf.Clamp(_swimmingStaminaReduction?.Value ?? DefaultSwimmingStaminaReduction, 0f, 0.60f);
        return requestedStamina * (1f - reduction);
    }

    internal static void AdvanceWetRecovery(SE_Wet wet, float deltaTime)
    {
        if (wet is null || deltaTime <= 0f || StatusEffectCharacterField is null || StatusEffectTimeField is null) return;
        if (StatusEffectCharacterField.GetValue(wet) is not Player player || !DeepBoonRuntime.IsActive(player, DeepBoonId)) return;
        var reduction = Mathf.Clamp(_wetDurationReduction?.Value ?? DefaultWetDurationReduction, 0f, 0.60f);
        if (reduction <= 0f || StatusEffectTimeField.GetValue(wet) is not float elapsed) return;
        StatusEffectTimeField.SetValue(wet, elapsed + deltaTime * (reduction / (1f - reduction)));
    }

    internal static float VesselHandlingMultiplier(Ship ship, Player controllingPlayer)
    {
        if (ship is null || controllingPlayer is null || !DeepBoonRuntime.IsActive(controllingPlayer, DeepBoonId)) return 1f;
        return UnderworldVesselCatalog.HandlingMultiplier(Utils.GetPrefabName(ship.gameObject), _vesselHandlingBonus?.Value ?? DefaultVesselHandlingBonus);
    }

    internal static Vector3 AdjustVesselSteeringInput(Ship ship, Vector3 input)
    {
        if (ship is null || ship.m_shipControlls is null || Mathf.Approximately(input.x, 0f)) return input;
        var userId = ship.m_shipControlls.GetUser();
        if (userId == 0L) return input;
        var controllingPlayer = Player.GetPlayer(userId);
        if (controllingPlayer is null) return input;
        var multiplier = VesselHandlingMultiplier(ship, controllingPlayer);
        if (multiplier <= 1f) return input;
        input.x = Mathf.Clamp(input.x * multiplier, -1f, 1f);
        return input;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
internal static class DeepCurrentSwimmingStaminaPatch
{
    private static void Prefix(Player __instance, ref float v) => v = DeepCurrentRuntime.AdjustSwimmingStaminaCost(__instance, v);
}

[HarmonyPatch(typeof(SE_Wet), nameof(SE_Wet.UpdateStatusEffect))]
internal static class DeepCurrentWetRecoveryPatch
{
    private static void Postfix(SE_Wet __instance, float dt) => DeepCurrentRuntime.AdvanceWetRecovery(__instance, dt);
}

[HarmonyPatch(typeof(Ship), nameof(Ship.ApplyControlls))]
internal static class DeepCurrentVesselSteeringPatch
{
    private static void Prefix(Ship __instance, ref Vector3 dir) => dir = DeepCurrentRuntime.AdjustVesselSteeringInput(__instance, dir);
}
