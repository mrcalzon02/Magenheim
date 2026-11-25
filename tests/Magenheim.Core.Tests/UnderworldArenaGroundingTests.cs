using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;

internal static class UnderworldArenaGroundingTests
{
    public static int Run()
    {
        var checks = 0;
        void Verify(bool result, string message)
        {
            checks++;
            if (!result) throw new InvalidOperationException(message);
        }

        bool Resolve(double center, bool drowned, Func<double, double, double> height, out double origin) =>
            UnderworldArenaGrounding.TryResolveOriginHeight(800d, -450d, center, 30d, drowned, height, out origin);
        Verify(Resolve(45d, false, (_, _) => 45d, out var flat) &&
            Math.Abs(flat + UnderworldArenaGrounding.FloorHeightFromOrigin - 45.1d) < 1e-9,
            "A flat arena floor should clear the ground by 10 cm.");
        Verify(Resolve(45d, false, (x, _) => 45d + (x - 800d) * .04d, out var slope) &&
            slope + UnderworldArenaGrounding.FloorHeightFromOrigin > 45d + 13d * .04d,
            "A suitable gentle slope may not break through the combat floor.");
        Verify(!Resolve(45d, false, (x, _) => 45d + (x - 800d) * .14d, out _),
            "A steep site should be rejected.");
        Verify(Resolve(27.85d, true, (_, _) => 27.85d, out var drowned) &&
            Math.Abs(drowned + UnderworldArenaGrounding.FloorHeightFromOrigin - 29.4d) < 1e-9,
            "The Drowned Ring floor must be in wading water with room for a swimming boss in the center.");
        Verify(!Resolve(14d, true, (_, _) => 14d, out _), "A deep bed cannot host the Drowned Ring.");
        Verify(!Resolve(28.8d, true, (_, _) => 28.8d, out _), "The boss needs room to swim in the pool.");
        Verify(!UnderworldArenaGrounding.IsOutsideArrival(750d, 0d) &&
            UnderworldArenaGrounding.IsOutsideArrival(768d, 0d), "Arrival buffer must be radial and inclusive at its edge.");
        Verify(!UnderworldArenaGrounding.HasFeatureClearance(75d, 0d, 24d, 44d) &&
            UnderworldArenaGrounding.HasFeatureClearance(76d, 0d, 24d, 44d),
            "Clearance must include both features' footprints and the intervening margin.");

        var domain = UnderworldInstanceTerrainDomain.CreateDefault();
        foreach (var seed in new[] { 777, 12345, -1304777429 })
        {
            var eligible = new HashSet<UnderworldTerrainBiome>();
            UnderworldTerrainResult Sample(double x, double z) => UnderworldTerrainLifecycle.Evaluate(
                domain, new UnderworldTerrainSample(x, 0d, z, 30d, 0d, .5d), seed);

            for (var x = -10000; x <= 10000 && eligible.Count < 6; x += 160)
            for (var z = -10000; z <= 10000 && eligible.Count < 6; z += 160)
            {
                var radial = x * x + z * z;
                if (!UnderworldArenaGrounding.IsOutsideArrival(x, z) ||
                    radial > (domain.RadiusMeters - 24d) * (domain.RadiusMeters - 24d))
                    continue;
                var point = Sample(x, z);
                if (!point.Admitted || eligible.Contains(point.Biome)) continue;
                var isDrowned = point.Biome == UnderworldTerrainBiome.BlackwaterDeep;
                if (UnderworldArenaGrounding.TryResolveOriginHeight(x, z, point.Height, 30d, isDrowned,
                        (px, pz) => Sample(px, pz) is { Admitted: true } ground ? ground.Height : double.NaN,
                        out _))
                    eligible.Add(point.Biome);
            }

            Verify(eligible.Count == 6,
                $"Seed {seed} has suitable arena terrain in {eligible.Count}/6 biomes: {string.Join(", ", eligible)}.");
        }
        Console.WriteLine("Underworld arena grounding: clearance, water depth and slope cases passed, with qualifying sites in six biomes per seed.");
        return checks;
    }
}
