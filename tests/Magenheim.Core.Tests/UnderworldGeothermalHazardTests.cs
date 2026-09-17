using System;
using Magenheim.Core.Underworld;

internal static class UnderworldGeothermalHazardTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld geothermal hazard assertion {assertions} failed: {message}");
        }

        Assert(UnderworldGeothermalHazard.IsCanonical(UnderworldGeothermalHazard.VentField), "Vent fields must be canonical geothermal sources.");
        Assert(UnderworldGeothermalHazard.IsCanonical(UnderworldGeothermalHazard.LavaChannel), "Lava channels must be canonical geothermal sources.");
        Assert(UnderworldGeothermalHazard.IsCanonical(UnderworldGeothermalHazard.ThermalSurge), "Thermal Surge must be a canonical geothermal source.");
        Assert(!UnderworldGeothermalHazard.IsCanonical("Vent_Field"), "Hazard identity must remain exact and case-sensitive.");
        Assert(!UnderworldGeothermalHazard.IsCanonical("foreign_mod_lava"), "Foreign hazards must not inherit Underworld thermal behavior by naming convention.");

        Assert(Math.Abs(UnderworldGeothermalHazard.Exposure(UnderworldGeothermalHazard.VentField, 1f, 0.5f) - 4f) < 0.0001f, "Vent exposure must scale by intensity and elapsed time.");
        Assert(Math.Abs(UnderworldGeothermalHazard.Exposure(UnderworldGeothermalHazard.LavaChannel, 1f, 1f) - 14f) < 0.0001f, "Lava channels must exert stronger thermal pressure than vent fields.");
        Assert(Math.Abs(UnderworldGeothermalHazard.Exposure(UnderworldGeothermalHazard.ThermalSurge, 1f, 1f) - 20f) < 0.0001f, "Thermal Surge must remain the strongest canonical ambient source.");
        Assert(Math.Abs(UnderworldGeothermalHazard.Exposure(UnderworldGeothermalHazard.VentField, 99f, 99f) - 16f) < 0.0001f, "Intensity and sample duration must be bounded against runaway heat spikes.");
        Assert(UnderworldGeothermalHazard.Exposure("foreign_mod_lava", 1f, 1f) == 0f, "Unknown hazard identities must fail neutral.");
        Assert(UnderworldGeothermalHazard.Exposure(UnderworldGeothermalHazard.VentField, -1f, 1f) == 0f, "Negative intensity must fail neutral.");
        Assert(UnderworldGeothermalHazard.Exposure(UnderworldGeothermalHazard.VentField, 1f, float.NaN) == 0f, "Malformed sample time must fail neutral.");

        var raw = UnderworldGeothermalHazard.Exposure(UnderworldGeothermalHazard.LavaChannel, 1f, 1f);
        Assert(Math.Abs(UnderworldThermalExposure.ApplyBuildup(20f, raw, 100f, 0.35f) - 29.1f) < 0.0001f, "Canonical geothermal exposure must compose with Furnace Blood thermal reduction without granting immunity.");

        return assertions;
    }
}
