using System;
using Magenheim.Core.Underworld;

internal static class UnderworldRareCellMassifTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Rare-cell massif terrain: " + message);
        }

        var domain = UnderworldInstanceTerrainDomain.CreateDefault();
        Assert(UnderworldTerrainLifecycle.RareCellMassifChance > 0d &&
               UnderworldTerrainLifecycle.RareCellMassifChance < .10d,
            "massif selection must stay rare rather than becoming ordinary relief");

        foreach (var seed in new[] { 0, 1, 12345, 1675883973 })
        {
            var occupied = 0;
            var probes = 0;
            var maxLift = 0d;
            var maxX = 0d;
            var maxZ = 0d;

            for (var z = -6400d; z <= 6400d; z += 160d)
            for (var x = -6400d; x <= 6400d; x += 160d)
            {
                var radius = Math.Sqrt(x * x + z * z);
                if (radius < domain.RadiusMeters * .26d ||
                    radius > domain.RadiusMeters * .82d)
                    continue;

                probes++;
                var lift = UnderworldTerrainLifecycle.RareCellMassifLiftAt(domain, seed, x, z);
                Assert(lift == UnderworldTerrainLifecycle.RareCellMassifLiftAt(domain, seed, x, z),
                    "massif selection/lift must be deterministic");

                if (lift <= 1d) continue;
                occupied++;
                if (lift > maxLift)
                {
                    maxLift = lift;
                    maxX = x;
                    maxZ = z;
                }
            }

            Assert(probes > 2000, "test must cover a broad portion of the eligible realm");
            Assert(occupied > 0, "representative seeds must contain at least one rare massif cell");
            Assert(occupied < probes * .12d,
                "rare massif coverage must stay sparse rather than replacing ordinary biome terrain");
            Assert(maxLift > 1200d,
                "rare massif interiors must create an inordinately tall terrain variant");

            // A selected high point must belong to a broad lifted cell, not a needle. Nearby probes
            // should still receive substantial lift from the same Voronoi-cell interior.
            var nearbyLifted = 0;
            for (var dz = -160d; dz <= 160d; dz += 80d)
            for (var dx = -160d; dx <= 160d; dx += 80d)
            {
                if (UnderworldTerrainLifecycle.RareCellMassifLiftAt(
                        domain, seed, maxX + dx, maxZ + dz) > 300d)
                    nearbyLifted++;
            }
            Assert(nearbyLifted >= 9,
                "massif elevation must occupy a broad cell interior rather than converge to a spire");
        }

        foreach (var seed in new[] { 12345, 777 })
        {
            for (var angleStep = 0; angleStep < 24; angleStep++)
            {
                var angle = angleStep * Math.PI * 2d / 24d;
                var innerRadius = domain.RadiusMeters * .20d;
                var edgeRadius = domain.RadiusMeters * .90d;
                Assert(UnderworldTerrainLifecycle.RareCellMassifLiftAt(
                           domain, seed,
                           innerRadius * Math.Cos(angle),
                           innerRadius * Math.Sin(angle)) == 0d,
                    "massifs must stay out of the arrival/progression shoulder");
                Assert(UnderworldTerrainLifecycle.RareCellMassifLiftAt(
                           domain, seed,
                           edgeRadius * Math.Cos(angle),
                           edgeRadius * Math.Sin(angle)) == 0d,
                    "massifs must stay out of the edge-ocean barrier");
            }
        }

        return assertions;
    }
}
