using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Presentation-only placement grammar for native Underworld ecology communities.
/// It deliberately owns no terrain, lifecycle, networking, progression, or spawn authority.
/// Callers must still terrain-sample and biome-admit every proposed point.
/// </summary>
internal static class UnderworldBiomeEcologyComposition
{
    internal static Vector2 Compose(
        UnderworldTerrainBiome biome,
        System.Random random,
        float radius,
        int member,
        float heading)
    {
        if (member == 0) return Landmark(random, radius);

        return biome switch
        {
            UnderworldTerrainBiome.FungalForest => FungalGrove(random, radius, member, heading),
            UnderworldTerrainBiome.FrozenCaverns => FrozenShardFan(random, radius, member, heading),
            UnderworldTerrainBiome.GreatDecay => DecayRootCorridor(random, radius, member, heading),
            UnderworldTerrainBiome.SulfurousWastes => SulfurOutcropChain(random, radius, member, heading),
            UnderworldTerrainBiome.FractureZones => FractureFaultLine(random, radius, member, heading),
            UnderworldTerrainBiome.BlackwaterDeep => BlackwaterSparsePocket(random, radius, member, heading),
            _ => GenericArc(random, radius, member, heading),
        };
    }

    private static Vector2 Landmark(System.Random random, float radius)
    {
        var angle = Tau(random);
        var distance = Unit(random) * radius * 0.10f;
        return Polar(angle, distance);
    }

    // Broad canopy/grove: supports wrap most of a circle, fill gathers beneath one side.
    private static Vector2 FungalGrove(System.Random random, float radius, int member, float heading)
    {
        if (member <= 4)
        {
            var slot = member - 1;
            var angle = heading + 0.35f + slot * 1.38f + Jitter(random, 0.24f);
            return Polar(angle, radius * (0.42f + Unit(random) * 0.43f));
        }
        var fillAngle = heading + 2.55f + Jitter(random, 0.38f);
        return Polar(fillAngle, radius * (0.28f + Unit(random) * 0.28f)) + CartesianJitter(random, radius * 0.08f);
    }

    // Narrow directional fan: shards step outward and spread from a common origin.
    private static Vector2 FrozenShardFan(System.Random random, float radius, int member, float heading)
    {
        if (member <= 4)
        {
            var slot = member - 1;
            var angle = heading + (slot - 1.5f) * 0.31f + Jitter(random, 0.10f);
            var distance = radius * (0.36f + slot * 0.13f + Unit(random) * 0.10f);
            return Polar(angle, distance);
        }
        return Polar(heading + Jitter(random, 0.22f), radius * (0.20f + Unit(random) * 0.22f)) + CartesianJitter(random, radius * 0.05f);
    }

    // Two-sided root corridor: large forms flank a traversable central seam instead of closing a ring.
    private static Vector2 DecayRootCorridor(System.Random random, float radius, int member, float heading)
    {
        var forward = new Vector2(Mathf.Cos(heading), Mathf.Sin(heading));
        var side = new Vector2(-forward.y, forward.x);
        if (member <= 4)
        {
            var slot = member - 1;
            var along = radius * (-0.48f + slot * 0.32f + Jitter(random, 0.07f));
            var flank = radius * ((slot & 1) == 0 ? 0.43f : -0.43f) * (0.86f + Unit(random) * 0.20f);
            return forward * along + side * flank;
        }
        var fillAlong = radius * (-0.30f + (member - 5) * 0.58f + Jitter(random, 0.06f));
        return forward * fillAlong + side * radius * Jitter(random, 0.16f);
    }

    // Broken mineral/rock chain with unequal spacing and a small scree pocket.
    private static Vector2 SulfurOutcropChain(System.Random random, float radius, int member, float heading)
    {
        var forward = new Vector2(Mathf.Cos(heading), Mathf.Sin(heading));
        var side = new Vector2(-forward.y, forward.x);
        if (member <= 4)
        {
            var slot = member - 1;
            var along = radius * (-0.64f + slot * 0.39f + Jitter(random, 0.08f));
            return forward * along + side * radius * Jitter(random, 0.20f);
        }
        return forward * radius * 0.42f + side * radius * (0.24f + Jitter(random, 0.12f)) + CartesianJitter(random, radius * 0.06f);
    }

    // Strong fault-line read: almost linear, with alternating offsets suggesting displaced strata.
    private static Vector2 FractureFaultLine(System.Random random, float radius, int member, float heading)
    {
        var forward = new Vector2(Mathf.Cos(heading), Mathf.Sin(heading));
        var normal = new Vector2(-forward.y, forward.x);
        if (member <= 4)
        {
            var slot = member - 1;
            var along = radius * (-0.72f + slot * 0.47f + Jitter(random, 0.05f));
            var offset = radius * (((slot & 1) == 0 ? 1f : -1f) * (0.09f + Unit(random) * 0.08f));
            return forward * along + normal * offset;
        }
        var end = member == 5 ? -0.54f : 0.58f;
        return forward * radius * end + normal * radius * Jitter(random, 0.10f);
    }

    // Blackwater stays deliberately sparse: supports occupy one bank/depression edge, fill hugs it.
    private static Vector2 BlackwaterSparsePocket(System.Random random, float radius, int member, float heading)
    {
        if (member <= 4)
        {
            var slot = member - 1;
            var angle = heading + 0.78f + slot * 0.52f + Jitter(random, 0.16f);
            var distance = radius * (0.52f + Unit(random) * 0.40f);
            return Polar(angle, distance);
        }
        return Polar(heading + 1.55f + Jitter(random, 0.20f), radius * (0.62f + Unit(random) * 0.22f));
    }

    private static Vector2 GenericArc(System.Random random, float radius, int member, float heading)
    {
        if (member <= 4)
        {
            var slot = member - 1;
            return Polar(heading + 0.65f + slot * 1.22f + Jitter(random, 0.24f), radius * (0.38f + Unit(random) * 0.47f));
        }
        return Polar(heading + Jitter(random, 0.28f), radius * (0.48f + Unit(random) * 0.34f)) + CartesianJitter(random, radius * 0.07f);
    }

    private static float Unit(System.Random random) => (float)random.NextDouble();
    private static float Tau(System.Random random) => Unit(random) * Mathf.PI * 2f;
    private static float Jitter(System.Random random, float halfRange) => (Unit(random) * 2f - 1f) * halfRange;
    private static Vector2 Polar(float angle, float distance) => new(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    private static Vector2 CartesianJitter(System.Random random, float extent) => new(Jitter(random, extent), Jitter(random, extent));
}
