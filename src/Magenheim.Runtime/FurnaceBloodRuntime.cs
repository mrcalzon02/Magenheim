using System;
using BepInEx.Configuration;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime projection for the Furnace Blood Deep Boon. Durable selection and unlock
/// authority remain in DeepBoonRuntime; this adapter alters only player-owned hazard state.
/// </summary>
internal static class FurnaceBloodRuntime
{
    internal const string DeepBoonId = "furnace_blood";
    private const float DefaultFireDamageReduction = 0.30f;
    private const float DefaultThermalBuildupReduction = 0.35f;
    private static ConfigEntry<float>? _fireDamageReduction;
    private static ConfigEntry<float>? _thermalBuildupReduction;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _fireDamageReduction = config.Bind(
            "Balance.Underworld.DeepBoons.FurnaceBlood",
            "FireDamageReduction",
            DefaultFireDamageReduction,
            "Fraction of incoming fire damage removed while Furnace Blood is the player's active Deep Boon. Clamped to 0..0.75; Furnace Blood provides resistance, never immunity.");
        _thermalBuildupReduction = config.Bind(
            "Balance.Underworld.DeepBoons.FurnaceBlood",
            "ThermalBuildupReduction",
            DefaultThermalBuildupReduction,
            "Fraction of admitted Underworld thermal buildup removed while Furnace Blood is active. Clamped by Core to 0..0.75; hazards continue accumulating heat.");
    }

    internal static void MitigateFire(Player player, HitData hit)
    {
        if (player is null || hit is null || !DeepBoonRuntime.IsActive(player, DeepBoonId)) return;
        if (hit.m_damage.m_fire <= 0f) return;

        var configured = _fireDamageReduction?.Value ?? DefaultFireDamageReduction;
        var reduction = Mathf.Clamp(configured, 0f, 0.75f);
        if (reduction <= 0f) return;

        hit.m_damage.m_fire *= 1f - reduction;
    }

    internal static float ApplyThermalBuildup(Player player, float currentHeat, float incomingHeat, float maximumHeat)
    {
        var reduction = player is not null && DeepBoonRuntime.IsActive(player, DeepBoonId)
            ? _thermalBuildupReduction?.Value ?? DefaultThermalBuildupReduction
            : 0f;
        return UnderworldThermalExposure.ApplyBuildup(currentHeat, incomingHeat, maximumHeat, reduction);
    }
}
