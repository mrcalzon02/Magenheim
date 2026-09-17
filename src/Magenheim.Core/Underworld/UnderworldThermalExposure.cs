using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Pure authority for bounded Underworld thermal-pressure accumulation.
/// Runtime hazard adapters supply exposure; this policy owns clamping and boon reduction semantics.
/// </summary>
public static class UnderworldThermalExposure
{
    public const float MaximumBuildupReduction = 0.75f;

    public static float ApplyBuildup(float currentHeat, float incomingHeat, float maximumHeat, float buildupReduction)
    {
        if (!IsFinite(currentHeat) || !IsFinite(incomingHeat) || !IsFinite(maximumHeat) || maximumHeat <= 0f)
            return 0f;

        var current = Clamp(currentHeat, 0f, maximumHeat);
        if (incomingHeat <= 0f)
            return current;

        var reduction = IsFinite(buildupReduction)
            ? Clamp(buildupReduction, 0f, MaximumBuildupReduction)
            : 0f;
        var admitted = incomingHeat * (1f - reduction);
        return Clamp(current + admitted, 0f, maximumHeat);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static float Clamp(float value, float minimum, float maximum)
        => value < minimum ? minimum : value > maximum ? maximum : value;
}
