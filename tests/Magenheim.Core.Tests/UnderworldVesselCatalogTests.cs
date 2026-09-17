using System;
using Magenheim.Core.Underworld;

internal static class UnderworldVesselCatalogTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld vessel assertion {assertions} failed: {message}");
        }

        Assert(UnderworldVesselCatalog.PrefabNames.Count == 1, "Initial vessel package must expose exactly one canonical vessel.");
        Assert(UnderworldVesselCatalog.PrefabNames[0] == UnderworldVesselCatalog.BlackwaterSkiff, "Blackwater Skiff must be the initial canonical vessel.");
        Assert(UnderworldVesselCatalog.IsCanonical(UnderworldVesselCatalog.BlackwaterSkiff), "Exact Blackwater Skiff identity must be admitted.");
        Assert(!UnderworldVesselCatalog.IsCanonical("Raft"), "Vanilla raft must not be admitted.");
        Assert(!UnderworldVesselCatalog.IsCanonical("VikingShip"), "Vanilla longship must not be admitted.");
        Assert(!UnderworldVesselCatalog.IsCanonical("magenheim_underworld_vessel_blackwaterskiff"), "Admission must remain case-sensitive.");
        Assert(!UnderworldVesselCatalog.IsCanonical(null), "Null identity must fail closed.");
        Assert(Math.Abs(UnderworldVesselCatalog.HandlingMultiplier(UnderworldVesselCatalog.BlackwaterSkiff, 0.20f) - 1.20f) < 0.0001f, "Configured canonical handling bonus must apply.");
        Assert(Math.Abs(UnderworldVesselCatalog.HandlingMultiplier("Raft", 0.20f) - 1f) < 0.0001f, "Foreign vessels must remain neutral.");
        Assert(Math.Abs(UnderworldVesselCatalog.HandlingMultiplier(UnderworldVesselCatalog.BlackwaterSkiff, 4f) - 1.50f) < 0.0001f, "Handling bonus must clamp at +50%.");
        Assert(Math.Abs(UnderworldVesselCatalog.HandlingMultiplier(UnderworldVesselCatalog.BlackwaterSkiff, -1f) - 1f) < 0.0001f, "Negative configuration must fail neutral.");
        Assert(Math.Abs(UnderworldVesselCatalog.HandlingMultiplier(UnderworldVesselCatalog.BlackwaterSkiff, float.NaN) - 1f) < 0.0001f, "Non-finite configuration must fail neutral.");

        return assertions;
    }
}
