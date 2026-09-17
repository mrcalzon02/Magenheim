using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Pure lifecycle authority for player-owned Underworld thermal pressure.
/// Hazard adapters own accumulation; this policy owns passive recovery and readable severity.
/// </summary>
public static class UnderworldThermalState
{
    public const float WarmThreshold = 35f;
    public const float HotThreshold = 65f;
    public const float CriticalThreshold = 85f;
    public const float DefaultCoolingPerSecond = 6f;
    public const float MaximumCoolingPerSecond = 20f;
    public const float MaximumCoolingSampleSeconds = 1f;

    public static float Cool(float currentHeat, float deltaSeconds, float coolingPerSecond = DefaultCoolingPerSecond)
    {
        if (!IsFinite(currentHeat) || currentHeat <= 0f) return 0f;
        if (!IsFinite(deltaSeconds) || deltaSeconds <= 0f) return currentHeat;
        if (!IsFinite(coolingPerSecond) || coolingPerSecond < 0f) coolingPerSecond = DefaultCoolingPerSecond;

        var seconds = Math.Min(deltaSeconds, MaximumCoolingSampleSeconds);
        var rate = Math.Min(coolingPerSecond, MaximumCoolingPerSecond);
        return Math.Max(0f, currentHeat - rate * seconds);
    }

    public static UnderworldThermalSeverity Severity(float currentHeat, float maximumHeat)
    {
        if (!IsFinite(currentHeat) || !IsFinite(maximumHeat) || maximumHeat <= 0f || currentHeat <= 0f)
            return UnderworldThermalSeverity.Normal;

        var percent = Math.Max(0f, Math.Min(100f, currentHeat / maximumHeat * 100f));
        if (percent >= CriticalThreshold) return UnderworldThermalSeverity.Critical;
        if (percent >= HotThreshold) return UnderworldThermalSeverity.Hot;
        if (percent >= WarmThreshold) return UnderworldThermalSeverity.Warm;
        return UnderworldThermalSeverity.Normal;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

public enum UnderworldThermalSeverity
{
    Normal = 0,
    Warm = 1,
    Hot = 2,
    Critical = 3
}
