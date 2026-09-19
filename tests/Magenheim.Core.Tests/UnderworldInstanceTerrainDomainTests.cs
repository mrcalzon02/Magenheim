using System;
using Magenheim.Core.Underworld;

internal static class UnderworldInstanceTerrainDomainTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld instance-terrain assertion {assertions} failed: {message}");
        }

        var domain = UnderworldInstanceTerrainDomain.CreateDefault();
        Assert(domain.RadiusMeters == 8000d, "Default instance radius is canonical.");
        Assert(domain.Contains(0d, 0d, 0d), "Instance origin belongs to native terrain domain.");
        Assert(!domain.Contains(domain.RadiusMeters + 1d, 0d, 0d), "Coordinates outside native radius are rejected.");
        Assert(!domain.Contains(0d, domain.MaximumY + 1d, 0d), "Coordinates above native vertical domain are rejected.");

        var center = UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(0d, 0d, 0d, 30d, 0d, 0.5d), 12345);
        Assert(center.Admitted, "Native instance terrain sample is admitted without Surface host coordinates.");
        Assert(center.Biome == UnderworldTerrainBiome.FungalForest, "Native instance origin remains protected Fungal Forest.");

        var type = typeof(UnderworldInstanceTerrainDomain);
        Assert(type.GetProperty("HostCenterX") is null && type.GetProperty("HostCenterZ") is null && type.GetProperty("HostBaseY") is null,
            "Native instance terrain contract cannot express Surface host placement.");
        return assertions;
    }
}
