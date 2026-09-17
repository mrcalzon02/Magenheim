using System;
using Magenheim.Core.Underworld;

internal static class UnderworldTerrainLifecycleTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld terrain lifecycle assertion {assertions} failed: {message}");
        }

        var domain = UnderworldSpatialDomain.CreateDefault();
        var center = UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(0d, 0d, 0d, 12d, 8d, 0d, 0.5d), 12345);
        Assert(center.Admitted, "Logical origin must be admitted to Underworld terrain generation.");
        Assert(center.Biome == UnderworldTerrainBiome.FungalForest,
            "The central admission basin must remain Fungal Forest regardless of seed.");
        Assert(center.Hazard01 == 0d, "First-biome terrain must not introduce a damage hazard.");

        var outside = UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(domain.RadiusMeters + 1d, 0d, 0d, 0d, 0d, 0d, 0.5d), 1);
        Assert(!outside.Admitted, "Terrain outside the reserved Underworld radius must fail closed.");

        var malformed = UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(0d, 0d, 0d, double.NaN, 0d, 0d, 0.5d), 1);
        Assert(!malformed.Admitted, "Malformed terrain samples must fail closed.");

        var sample = new UnderworldTerrainSample(domain.RadiusMeters * 0.6d, 0d, 0d, 20d, 20d, 0d, 0.9d);
        var first = UnderworldTerrainLifecycle.Evaluate(domain, sample, 777);
        var repeat = UnderworldTerrainLifecycle.Evaluate(domain, sample, 777);
        Assert(first == repeat, "Terrain evaluation must be deterministic for the same seed and sample.");
        Assert(Math.Abs(first.Height - sample.BaseHeight) <= UnderworldTerrainLifecycle.MaximumTerrainDelta + 0.0001d,
            "Terrain shaping must remain inside the bounded height delta.");
        Assert(first.Cover01 >= 0d && first.Cover01 <= 1d && first.Hazard01 >= 0d && first.Hazard01 <= 1d,
            "Terrain ecology outputs must remain normalized.");

        var foundDifferentBiome = false;
        for (var seed = 778; seed < 810; seed++)
        {
            var candidate = UnderworldTerrainLifecycle.Evaluate(domain, sample, seed);
            if (candidate.Biome != first.Biome)
            {
                foundDifferentBiome = true;
                break;
            }
        }
        Assert(foundDifferentBiome, "Derived seed must rotate outer biome provinces.");

        var wet = UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(0d, 0d, 0d, 0d, 10d, -8d, 0.4d), 1);
        Assert(wet.WaterDepth == 0d, "Negative water depth must normalize to dry terrain.");

        return assertions;
    }
}
