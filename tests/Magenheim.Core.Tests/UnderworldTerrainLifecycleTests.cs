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

        var instanceDomain = UnderworldInstanceTerrainDomain.CreateDefault();
        var center = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            new UnderworldTerrainSample(0d, 0d, 0d, 30d, 8d, 0.5d), 12345);
        Assert(center.Admitted, "Logical origin must be admitted to Underworld terrain generation.");
        Assert(center.Biome == UnderworldTerrainBiome.FungalForest,
            "The central admission basin must remain Fungal Forest regardless of seed.");
        Assert(center.Hazard01 == 0d, "First-biome terrain must not introduce a damage hazard.");

        var outside = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            new UnderworldTerrainSample(instanceDomain.RadiusMeters + 1d, 0d, 0d, 30d, 0d, 0.5d), 1);
        Assert(!outside.Admitted, "Terrain outside the native Underworld radius must fail closed.");

        var malformed = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            new UnderworldTerrainSample(0d, 0d, 0d, double.NaN, 0d, 0.5d), 1);
        Assert(!malformed.Admitted, "Malformed terrain samples must fail closed.");

        var sample = new UnderworldTerrainSample(instanceDomain.RadiusMeters * 0.6d, 0d, 0d, 30d, 20d, 0.9d);
        var first = UnderworldTerrainLifecycle.Evaluate(instanceDomain, sample, 777);
        var repeat = UnderworldTerrainLifecycle.Evaluate(instanceDomain, sample, 777);
        Assert(first == repeat, "Terrain evaluation must be deterministic for the same seed and sample.");
        Assert(Math.Abs(first.Height - UnderworldTerrainLifecycle.BaseElevationMeters) <= UnderworldTerrainLifecycle.MaximumTerrainDelta + 0.0001d,
            "Terrain shaping must remain inside the bounded height delta around the region's base elevation.");
        Assert(first.Cover01 >= 0d && first.Cover01 <= 1d && first.Hazard01 >= 0d && first.Hazard01 <= 1d,
            "Terrain ecology outputs must remain normalized.");

        var foundDifferentBiome = false;
        for (var seed = 778; seed < 810; seed++)
        {
            var candidate = UnderworldTerrainLifecycle.Evaluate(instanceDomain, sample, seed);
            if (candidate.Biome != first.Biome)
            {
                foundDifferentBiome = true;
                break;
            }
        }
        Assert(foundDifferentBiome, "Derived seed must rotate outer biome provinces.");

        var wet = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            new UnderworldTerrainSample(0d, 0d, 0d, 0d, 10d, 0.4d), 1);
        Assert(wet.WaterDepth == 0d, "Ground generated above the water line must report no water depth.");

        var transitionRadius = instanceDomain.RadiusMeters *
            ((UnderworldTerrainLifecycle.CentralFungalRadiusFraction + UnderworldTerrainLifecycle.FungalTransitionRadiusFraction) * 0.5d);
        var transitionSample = new UnderworldTerrainSample(transitionRadius, 0d, 0d, 30d, 18d, 0.9d);
        var transition = UnderworldTerrainLifecycle.Evaluate(instanceDomain, transitionSample, 777);
        Assert(transition.Admitted && transition.Biome != UnderworldTerrainBiome.FungalForest,
            "Transition-band samples must retain their outer province identity.");
        Assert(transition.Hazard01 >= 0d && transition.Hazard01 < 0.8d,
            "Transition band must attenuate hostile biome hazard near the protected center.");

        var innerEdge = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            transitionSample with { X = instanceDomain.RadiusMeters * (UnderworldTerrainLifecycle.CentralFungalRadiusFraction + 0.0001d) }, 777);
        Assert(Math.Abs(innerEdge.Height - UnderworldTerrainLifecycle.BaseElevationMeters) < UnderworldTerrainLifecycle.MaximumTerrainDelta,
            "Crossing the central biome edge must not create a maximum-delta terrain cliff.");
        Assert(innerEdge.Hazard01 < transition.Hazard01 + 0.0001d,
            "Hazard must ramp outward rather than spike at the central biome boundary.");

        const int precisionSeed = 1675883973;
        var nearOrigin = UnderworldTerrainNoise.Fractal01(precisionSeed, 0d, 0d);
        var offsetA = UnderworldTerrainNoise.Fractal01(precisionSeed, 40000d, 0d);
        var offsetB = UnderworldTerrainNoise.Fractal01(precisionSeed, 40001d, 0d);
        Assert(Math.Abs(offsetA - offsetB) > 1e-9d && nearOrigin >= 0d && nearOrigin <= 1d,
            "Terrain noise must still vary metre to metre at large coordinates.");

        var lowest = double.MaxValue;
        var highest = double.MinValue;
        var relief = 0;
        for (var step = 0; step < 96; step++)
        {
            var x = -instanceDomain.RadiusMeters * 0.5d + step * (instanceDomain.RadiusMeters / 96d);
            var probe = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
                new UnderworldTerrainSample(x, 0d, 512d, 30d, 0d,
                    UnderworldTerrainNoise.Fractal01(precisionSeed, x, 512d)), precisionSeed);
            if (!probe.Admitted) continue;
            relief++;
            if (probe.Height < lowest) lowest = probe.Height;
            if (probe.Height > highest) highest = probe.Height;
        }
        Assert(relief > 48, "A transect across the instance must admit terrain to measure.");
        Assert(highest - lowest > 8d,
            "Underworld terrain must have real relief across a transect, not generate as a flat plane.");

        var screenLow = double.MaxValue;
        var screenHigh = double.MinValue;
        for (var step = 0; step <= 50; step++)
        {
            var probe = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
                new UnderworldTerrainSample(step, 0d, 0d, 30d, 0d,
                    UnderworldTerrainNoise.Fractal01(precisionSeed, step, 0d)), precisionSeed);
            if (!probe.Admitted) continue;
            if (probe.Height < screenLow) screenLow = probe.Height;
            if (probe.Height > screenHigh) screenHigh = probe.Height;
        }
        Assert(screenHigh - screenLow > 0.3d,
            "Underworld terrain must show relief across a 50m screen, not only across kilometres.");

        var steepest = 0d;
        var previous = double.NaN;
        for (var step = 0; step <= 400; step++)
        {
            var probe = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
                new UnderworldTerrainSample(step, 0d, 96d, 30d, 0d,
                    UnderworldTerrainNoise.Fractal01(precisionSeed, step, 96d)), precisionSeed);
            if (!probe.Admitted) continue;
            if (!double.IsNaN(previous)) steepest = Math.Max(steepest, Math.Abs(probe.Height - previous));
            previous = probe.Height;
        }
        Assert(steepest > 0.02d && steepest < 1d,
            "Central-basin relief must be visible but walkable metre to metre.");

        return assertions;
    }
}
