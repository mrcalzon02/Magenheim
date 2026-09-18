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
            new UnderworldTerrainSample(0d, 0d, 0d, 30d, 8d, 0.5d), 12345);
        Assert(center.Admitted, "Logical origin must be admitted to Underworld terrain generation.");
        Assert(center.Biome == UnderworldTerrainBiome.FungalForest,
            "The central admission basin must remain Fungal Forest regardless of seed.");
        Assert(center.Hazard01 == 0d, "First-biome terrain must not introduce a damage hazard.");

        var outside = UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(domain.RadiusMeters + 1d, 0d, 0d, 30d, 0d, 0.5d), 1);
        Assert(!outside.Admitted, "Terrain outside the reserved Underworld radius must fail closed.");

        var malformed = UnderworldTerrainLifecycle.Evaluate(domain,
            new UnderworldTerrainSample(0d, 0d, 0d, double.NaN, 0d, 0.5d), 1);
        Assert(!malformed.Admitted, "Malformed terrain samples must fail closed.");

        var sample = new UnderworldTerrainSample(domain.RadiusMeters * 0.6d, 0d, 0d, 30d, 20d, 0.9d);
        var first = UnderworldTerrainLifecycle.Evaluate(domain, sample, 777);
        var repeat = UnderworldTerrainLifecycle.Evaluate(domain, sample, 777);
        Assert(first == repeat, "Terrain evaluation must be deterministic for the same seed and sample.");
        Assert(Math.Abs(first.Height - UnderworldTerrainLifecycle.BaseElevationMeters) <= UnderworldTerrainLifecycle.MaximumTerrainDelta + 0.0001d,
            "Terrain shaping must remain inside the bounded height delta around the region's base elevation.");
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
            new UnderworldTerrainSample(0d, 0d, 0d, 0d, 10d, 0.4d), 1);
        Assert(wet.WaterDepth == 0d, "Ground generated above the water line must report no water depth.");

        // The protected center should transition into hostile provinces instead of stepping
        // immediately from safe terrain to the full outer biome displacement/hazard.
        var transitionRadius = domain.RadiusMeters *
            ((UnderworldTerrainLifecycle.CentralFungalRadiusFraction + UnderworldTerrainLifecycle.FungalTransitionRadiusFraction) * 0.5d);
        var transitionSample = new UnderworldTerrainSample(transitionRadius, 0d, 0d, 30d, 18d, 0.9d);
        var transition = UnderworldTerrainLifecycle.Evaluate(domain, transitionSample, 777);
        Assert(transition.Admitted && transition.Biome != UnderworldTerrainBiome.FungalForest,
            "Transition-band samples must retain their outer province identity.");
        Assert(transition.Hazard01 >= 0d && transition.Hazard01 < 0.8d,
            "Transition band must attenuate hostile biome hazard near the protected center.");

        var innerEdge = UnderworldTerrainLifecycle.Evaluate(domain,
            transitionSample with { X = domain.RadiusMeters * (UnderworldTerrainLifecycle.CentralFungalRadiusFraction + 0.0001d) }, 777);
        Assert(Math.Abs(innerEdge.Height - UnderworldTerrainLifecycle.BaseElevationMeters) < UnderworldTerrainLifecycle.MaximumTerrainDelta,
            "Crossing the central biome edge must not create a maximum-delta terrain cliff.");
        Assert(innerEdge.Hazard01 < transition.Hazard01 + 0.0001d,
            "Hazard must ramp outward rather than spike at the central biome boundary.");

        return assertions;
    }
}
