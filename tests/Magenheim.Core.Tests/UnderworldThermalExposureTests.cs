using System;
using Magenheim.Core.Underworld;

internal static class UnderworldThermalExposureTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld thermal exposure assertion {assertions} failed: {message}");
        }

        Assert(Math.Abs(UnderworldThermalExposure.ApplyBuildup(20f, 10f, 100f, 0f) - 30f) < 0.0001f, "Unmodified exposure must accumulate normally.");
        Assert(Math.Abs(UnderworldThermalExposure.ApplyBuildup(20f, 10f, 100f, 0.35f) - 26.5f) < 0.0001f, "Furnace Blood's default reduction must preserve partial buildup.");
        Assert(Math.Abs(UnderworldThermalExposure.ApplyBuildup(20f, 10f, 100f, 4f) - 22.5f) < 0.0001f, "Reduction must clamp at 75% rather than grant immunity.");
        Assert(Math.Abs(UnderworldThermalExposure.ApplyBuildup(95f, 20f, 100f, 0f) - 100f) < 0.0001f, "Accumulation must not exceed the hazard maximum.");
        Assert(Math.Abs(UnderworldThermalExposure.ApplyBuildup(20f, -10f, 100f, 0.35f) - 20f) < 0.0001f, "Negative exposure must not cool through the buildup path.");
        Assert(Math.Abs(UnderworldThermalExposure.ApplyBuildup(20f, 10f, 100f, float.NaN) - 30f) < 0.0001f, "Invalid reduction must fail neutral.");
        Assert(UnderworldThermalExposure.ApplyBuildup(float.NaN, 10f, 100f, 0.35f) == 0f, "Invalid thermal state must fail closed.");
        Assert(UnderworldThermalExposure.ApplyBuildup(20f, 10f, 0f, 0.35f) == 0f, "Invalid capacity must fail closed.");

        return assertions;
    }
}
