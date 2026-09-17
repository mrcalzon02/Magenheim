using System;
using Magenheim.Core.Underworld;

internal static class UnderworldThermalStateTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld thermal state assertion {assertions} failed: {message}");
        }

        Assert(Math.Abs(UnderworldThermalState.Cool(50f, 1f) - 44f) < 0.0001f, "Default recovery must cool at six heat per second.");
        Assert(UnderworldThermalState.Cool(4f, 1f) == 0f, "Cooling must floor at zero.");
        Assert(Math.Abs(UnderworldThermalState.Cool(50f, 10f) - 44f) < 0.0001f, "A long frame must not dump multiple seconds of heat at once.");
        Assert(Math.Abs(UnderworldThermalState.Cool(50f, 1f, 100f) - 30f) < 0.0001f, "Cooling configuration must have a hard ceiling.");
        Assert(Math.Abs(UnderworldThermalState.Cool(50f, 1f, float.NaN) - 44f) < 0.0001f, "Malformed cooling configuration must fall back to the canonical rate.");
        Assert(UnderworldThermalState.Cool(float.NaN, 1f) == 0f, "Malformed thermal state must fail closed.");

        Assert(UnderworldThermalState.Severity(0f, 100f) == UnderworldThermalSeverity.Normal, "Zero heat must be normal.");
        Assert(UnderworldThermalState.Severity(35f, 100f) == UnderworldThermalSeverity.Warm, "35 percent heat must enter Warm.");
        Assert(UnderworldThermalState.Severity(65f, 100f) == UnderworldThermalSeverity.Hot, "65 percent heat must enter Hot.");
        Assert(UnderworldThermalState.Severity(85f, 100f) == UnderworldThermalSeverity.Critical, "85 percent heat must enter Critical.");
        Assert(UnderworldThermalState.Severity(200f, 100f) == UnderworldThermalSeverity.Critical, "Over-capacity state must remain Critical.");
        Assert(UnderworldThermalState.Severity(50f, 0f) == UnderworldThermalSeverity.Normal, "Invalid capacity must fail closed.");

        return assertions;
    }
}
