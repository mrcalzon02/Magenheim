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
        Assert(instanceDomain.Contains(sample.X, first.Height, sample.Z),
            "Generated terrain must fit the admitted instance, without a shared small height clamp.");
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
        Assert(foundDifferentBiome, "Derived seed must rearrange the organic outer biome field.");

        var wet = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            new UnderworldTerrainSample(0d, 0d, 0d, 0d, 10d, 0.4d), 1);
        Assert(wet.WaterDepth == 0d, "Ground generated above the water line must report no water depth.");

        var guaranteedRadius = instanceDomain.RadiusMeters *
            UnderworldTerrainLifecycle.GuaranteedFungalRadiusFraction;
        for (var angleStep = 0; angleStep < 24; angleStep++)
        {
            var angle = angleStep * Math.PI * 2d / 24d;
            var guaranteed = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
                new UnderworldTerrainSample(
                    guaranteedRadius * .98d * Math.Cos(angle),
                    0d,
                    guaranteedRadius * .98d * Math.Sin(angle),
                    30d,
                    12d,
                    .5d),
                777);
            Assert(guaranteed.Biome == UnderworldTerrainBiome.FungalForest,
                "The guaranteed arrival core must remain Fungal Forest in every direction.");
        }

        var transitionRadius = instanceDomain.RadiusMeters *
            ((UnderworldTerrainLifecycle.GuaranteedFungalRadiusFraction +
              UnderworldTerrainLifecycle.FungalTransitionRadiusFraction) * 0.5d);
        var foundHostileTransition = false;
        UnderworldTerrainResult transition = default;
        for (var angleStep = 0; angleStep < 72; angleStep++)
        {
            var angle = angleStep * Math.PI * 2d / 72d;
            var candidate = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
                new UnderworldTerrainSample(
                    transitionRadius * Math.Cos(angle),
                    0d,
                    transitionRadius * Math.Sin(angle),
                    30d,
                    18d,
                    .9d),
                777);
            if (candidate.Biome == UnderworldTerrainBiome.FungalForest) continue;
            transition = candidate;
            foundHostileTransition = true;
            break;
        }
        Assert(foundHostileTransition,
            "The organic Fungal shoulder must eventually give way to an outer biome rather than forming a perfect ring.");
        Assert(transition.Hazard01 >= 0d && transition.Hazard01 < 0.8d,
            "The arrival shoulder must attenuate hostile-biome hazard while its edge meanders.");

        var safeInside = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            new UnderworldTerrainSample(guaranteedRadius - 1d, 0d, 0d, 30d, 12d, .5d), 777);
        var safeOutside = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
            new UnderworldTerrainSample(guaranteedRadius + 1d, 0d, 0d, 30d, 12d, .5d), 777);
        Assert(Math.Abs(safeInside.Height - safeOutside.Height) < 4d,
            "Leaving the guaranteed core must not create a radial terrain step.");

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
        for (var step = 0; step <= 60; step++)
        {
            var probe = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
                new UnderworldTerrainSample(step, 0d, 0d, 30d, 0d,
                    UnderworldTerrainNoise.Fractal01(precisionSeed, step, 0d)), precisionSeed);
            if (!probe.Admitted) continue;
            if (!double.IsNaN(previous)) steepest = Math.Max(steepest, Math.Abs(probe.Height - previous));
            previous = probe.Height;
        }
        Assert(steepest > 0.02d && steepest < 1d,
            "Protected gate relief must be visible but walkable metre to metre.");

        foreach (var seed in new[] { 12345, 777, precisionSeed })
        {
            var lows = new double[6];
            var highs = new double[6];
            Array.Fill(lows, double.MaxValue);
            Array.Fill(highs, double.MinValue);
            for (var z = -7840; z <= 7840; z += 160)
            for (var x = -7840; x <= 7840; x += 160)
            {
                var point = new UnderworldTerrainSample(x, 0, z, 30, 0, .5);
                var terrain = UnderworldTerrainLifecycle.Evaluate(instanceDomain, point, seed);
                if (!terrain.Admitted) continue;
                Assert(instanceDomain.Contains(x, terrain.Height, z) &&
                    instanceDomain.Contains(x, terrain.Height + 2, z), "Terrain and standing players fit the native instance.");
                Assert(terrain == UnderworldTerrainLifecycle.Evaluate(instanceDomain, point, seed),
                    "Wide relief remains deterministic.");
                if (UnderworldTerrainLifecycle.RareCellMassifLiftAt(instanceDomain, seed, x, z) > 1d) continue;
                if (Math.Sqrt(x * x + z * z) > instanceDomain.RadiusMeters * .82d) continue;
                var index = (int)terrain.Biome;
                lows[index] = Math.Min(lows[index], terrain.Height);
                highs[index] = Math.Max(highs[index], terrain.Height);
            }
            var minimumSpans = new[] { 110d, 170d, 400d, 550d, 800d, 300d };
            for (var biome = 0; biome < 6; biome++)
                Assert(highs[biome] - lows[biome] > minimumSpans[biome],
                    $"{(UnderworldTerrainBiome)biome} must have substantial ordinary relief for seed {seed}.");
            var fractureLift = UnderworldBiomeTerrain.NominalRegionalLift(UnderworldTerrainBiome.FractureZones);
            var blackwaterLift = UnderworldBiomeTerrain.NominalRegionalLift(UnderworldTerrainBiome.BlackwaterDeep);
            Assert(lows[1] - blackwaterLift < -100 && lows[4] - fractureLift < -255 && highs[4] - fractureLift > 345,
                $"Basins and ranges retain unclipped relief around their progression elevation: Blackwater low={lows[1]}, Fracture low={lows[4]}, high={highs[4]}, lift={fractureLift}.");
            var gateHalfDiagonal = Math.Sqrt(17.89d * 17.89d + 10.04d * 10.04d);
            Assert(UnderworldTerrainLifecycle.GateFoundationRadiusMeters > gateHalfDiagonal,
                "The flat Deep Gate foundation must contain the measured Aesir gate footprint.");

            var foundation = UnderworldTerrainLifecycle.Evaluate(
                instanceDomain,
                new UnderworldTerrainSample(
                    UnderworldTerrainLifecycle.GateFoundationCenterXMeters,
                    0d,
                    UnderworldTerrainLifecycle.GateFoundationCenterZMeters,
                    30d,
                    0d,
                    .5d),
                seed);
            for (var z = -10; z <= 10; z += 5)
            for (var x = 18; x <= 54; x += 6)
            {
                var gate = UnderworldTerrainLifecycle.Evaluate(instanceDomain, new(x, 0, z, 30, 0, .5), seed);
                Assert(Math.Abs(gate.Height - foundation.Height) < 1e-9,
                    "Every sampled point beneath the measured Deep Gate footprint must share one level foundation.");
                Assert(gate.WaterDepth == 0, "The Deep Gate foundation must stay dry.");
            }

            for (var x = -80; x <= 80; x += 5)
            {
                var gate = UnderworldTerrainLifecycle.Evaluate(instanceDomain, new(x, 0, 0, 30, 0, .5), seed);
                var gateDistance = Math.Abs(x - UnderworldTerrainLifecycle.GateFoundationCenterXMeters);
                if (gateDistance >= UnderworldTerrainLifecycle.GateFoundationBlendRadiusMeters)
                {
                    Assert(Math.Abs(gate.Height - (51 + .55 * UnderworldTerrainNoise.ReliefMetres(seed, x, 0, 0))) < 1e-9,
                        "The protected approach must recover its original fine relief beyond the Deep Gate foundation blend.");
                }
                Assert(gate.WaterDepth == 0, "The protected approach stays dry.");
            }
            var ringBiomes = new UnderworldTerrainBiome[360];
            var ringSeen = new bool[6];
            var arcStarts = new int[6];
            var transitions = 0;
            for (var degree = 0; degree < ringBiomes.Length; degree++)
            {
                var angle = degree * Math.PI / 180d;
                var ring = UnderworldTerrainLifecycle.Evaluate(instanceDomain,
                    new UnderworldTerrainSample(
                        6000d * Math.Cos(angle),
                        0d,
                        6000d * Math.Sin(angle),
                        30d,
                        0d,
                        .5d),
                    seed);
                ringBiomes[degree] = ring.Biome;
                ringSeen[(int)ring.Biome] = true;
                if (degree > 0 && ringBiomes[degree - 1] != ring.Biome)
                {
                    transitions++;
                    arcStarts[(int)ring.Biome]++;
                }
            }
            if (ringBiomes[ringBiomes.Length - 1] != ringBiomes[0])
            {
                transitions++;
                arcStarts[(int)ringBiomes[0]]++;
            }
            var unique = 0;
            var repeatedArcBiome = false;
            for (var biome = 0; biome < ringSeen.Length; biome++)
            {
                if (ringSeen[biome]) unique++;
                if (arcStarts[biome] >= 2) repeatedArcBiome = true;
            }
            Assert(unique >= 4,
                "A 6km ring must encounter at least four biome identities; worldgen cannot collapse to fixed slices.");
            Assert(transitions >= 10,
                "A 6km ring must cross many organic biome boundaries rather than exactly five province spokes.");
            Assert(repeatedArcBiome,
                "At least one biome must reappear in disconnected arcs on the same ring.");
        }
        // The hex/Voronoi boundary network is not decorative map ink: its strong seams are
        // physically submerged Blackwater corridors distributed throughout the outer realm.
        var riverQuadrants = new bool[4];
        var deepBlackwater = 0;
        var hydroSamples = 0;
        for (var z = -7200; z <= 7200; z += 160)
        for (var x = -7200; x <= 7200; x += 160)
        {
            if (x * x + z * z > 7200 * 7200 || x * x + z * z < 1800 * 1800) continue;
            var hydro = UnderworldTerrainLifecycle.Evaluate(
                instanceDomain,
                new UnderworldTerrainSample(x, 0, z, 30, 0, .5),
                12345);
            if (!hydro.Admitted) continue;
            hydroSamples++;
            if (hydro.Biome != UnderworldTerrainBiome.BlackwaterDeep || hydro.WaterDepth < 2d) continue;
            deepBlackwater++;
            var quadrant = (x >= 0 ? 1 : 0) + (z >= 0 ? 2 : 0);
            riverQuadrants[quadrant] = true;
        }
        Assert(hydroSamples > 2000, "Hydrology gate must sample a broad outer-world area.");
        Assert(deepBlackwater > hydroSamples * .08 && deepBlackwater < hydroSamples * .45,
            "Voronoi river/ocean-depth corridors must be common without drowning the majority of the realm.");
        Assert(riverQuadrants[0] && riverQuadrants[1] && riverQuadrants[2] && riverQuadrants[3],
            "Deep Blackwater corridors must cross all four world quadrants rather than pooling in one province.");

        // The physical terrain plate must dissolve into a deep Blackwater ring before the hard
        // domain boundary. The outer ring is deterministic ocean in every direction, while the
        // inner shoreline is allowed to wander through the plasma edge warp.
        foreach (var seed in new[] { 12345, 777 })
        {
            var outerDepthLow = double.MaxValue;
            var outerDepthHigh = double.MinValue;
            for (var angleStep = 0; angleStep < 72; angleStep++)
            {
                var angle = angleStep * Math.PI * 2d / 72d;
                var radius = instanceDomain.RadiusMeters * .985d;
                var edge = UnderworldTerrainLifecycle.Evaluate(
                    instanceDomain,
                    new UnderworldTerrainSample(
                        radius * Math.Cos(angle),
                        0d,
                        radius * Math.Sin(angle),
                        30d,
                        0d,
                        .5d),
                    seed);
                Assert(edge.Admitted, "The edge-ocean ring must still lie inside the native terrain domain.");
                Assert(edge.Biome == UnderworldTerrainBiome.BlackwaterDeep,
                    "The outer edge ring must be Blackwater rather than exposed land.");
                Assert(edge.WaterDepth > 100d,
                    "The outer edge ring must be genuinely deep water before the hard world boundary.");
                outerDepthLow = Math.Min(outerDepthLow, edge.WaterDepth);
                outerDepthHigh = Math.Max(outerDepthHigh, edge.WaterDepth);
            }
            Assert(outerDepthLow > 100d && outerDepthHigh <= 150d,
                "The edge-ocean depth must remain inside the intended deep-water envelope.");

            var shorelineDepthLow = double.MaxValue;
            var shorelineDepthHigh = double.MinValue;
            for (var angleStep = 0; angleStep < 72; angleStep++)
            {
                var angle = angleStep * Math.PI * 2d / 72d;
                var radius = instanceDomain.RadiusMeters * .89d;
                var shore = UnderworldTerrainLifecycle.Evaluate(
                    instanceDomain,
                    new UnderworldTerrainSample(
                        radius * Math.Cos(angle),
                        0d,
                        radius * Math.Sin(angle),
                        30d,
                        0d,
                        .5d),
                    seed);
                if (!shore.Admitted) continue;
                shorelineDepthLow = Math.Min(shorelineDepthLow, shore.WaterDepth);
                shorelineDepthHigh = Math.Max(shorelineDepthHigh, shore.WaterDepth);
            }
            Assert(shorelineDepthHigh - shorelineDepthLow > 12d,
                "Plasma shoreline warp must keep the edge-ocean transition from becoming a perfect circular bathtub.");
        }

        var compared = 0;
        var changed = 0;
        for (var z = -7000; z <= 7000; z += 400)
        for (var x = -7000; x <= 7000; x += 400)
        {
            if (x * x + z * z > 7000 * 7000) continue;
            compared++;
            var a = UnderworldTerrainLifecycle.Evaluate(instanceDomain, new(x, 0, z, 30, 0, .5), 12345);
            var b = UnderworldTerrainLifecycle.Evaluate(instanceDomain, new(x, 0, z, 30, 0, .5), 777);
            if (a.Biome != b.Biome) changed++;
        }
        Assert(compared > 500 && changed > compared * .35,
            "Changing the derived seed must materially rearrange biome geography, not merely rotate one fixed layout.");

        // Nominal regional elevation climbs in progression order. Individual noise/basins may
        // locally cross, but the biome baselines themselves are deliberately ordered.
        var orderedBiomes = new[]
        {
            UnderworldTerrainBiome.FungalForest,
            UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldTerrainBiome.SulfurousWastes,
            UnderworldTerrainBiome.FrozenCaverns,
            UnderworldTerrainBiome.FractureZones,
            UnderworldTerrainBiome.GreatDecay,
        };
        for (var index = 1; index < orderedBiomes.Length; index++)
            Assert(
                UnderworldBiomeTerrain.NominalRegionalLift(orderedBiomes[index]) >
                UnderworldBiomeTerrain.NominalRegionalLift(orderedBiomes[index - 1]),
                "Biome nominal regional lift must rise with progression order.");

        // The progression preference is graded, not ring-locked: outer samples should trend toward
        // later biome ranks over a broad seed ensemble while still containing earlier enclaves.
        double MeanRank(double radius)
        {
            var sum = 0d;
            var count = 0;
            for (var seed = 120; seed < 152; seed++)
            for (var step = 0; step < 72; step++)
            {
                var angle = step * Math.PI * 2d / 72d;
                var sampleAtRadius = UnderworldTerrainLifecycle.Evaluate(
                    instanceDomain,
                    new UnderworldTerrainSample(
                        radius * Math.Cos(angle),
                        0d,
                        radius * Math.Sin(angle),
                        30d,
                        0d,
                        .5d),
                    seed);
                if (!sampleAtRadius.Admitted || sampleAtRadius.WaterDepth > 2d) continue;
                sum += (int)sampleAtRadius.Biome;
                count++;
            }
            return count == 0 ? 0d : sum / count;
        }
        var innerRank = MeanRank(instanceDomain.RadiusMeters * .34d);
        var outerRank = MeanRank(instanceDomain.RadiusMeters * .70d);
        Assert(outerRank > innerRank + .25d,
            "Later-biome preference must increase with distance without becoming hard radial rings.");

        return assertions;
    }
}
