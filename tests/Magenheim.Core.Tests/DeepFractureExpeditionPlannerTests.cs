using System;
using System.Linq;
using Magenheim.Core.DeepFractures;

internal static class DeepFractureExpeditionPlannerTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Deep Fracture expedition assertion {assertions} failed: {message}");
        }

        var policy = DeepFractureExpeditionScalePolicy.Default;
        policy.Validate();
        Assert(policy.SmallWeight + policy.FullWeight + policy.GrandWeight == 100, "Default scale weights should remain human-readable percentages.");

        for (var seed = -32; seed <= 32; seed++)
        {
            var scaleA = DeepFractureExpeditionPlanner.SelectScale(seed, policy);
            var scaleB = DeepFractureExpeditionPlanner.SelectScale(seed, policy);
            Assert(scaleA == scaleB, $"Scale selection must be deterministic for seed {seed}.");

            var expedition = DeepFractureExpeditionPlanner.Build(seed, policy);
            Assert(expedition.Seed == seed, "Expedition seed must preserve the Valheim generator seed exactly.");
            Assert(expedition.Scale == scaleA, "Expedition scale must match deterministic scale authority.");
            Assert(expedition.Dungeon.Seed == seed, "Dungeon authority must preserve expedition seed.");
            Assert(expedition.Encounters.DungeonSeed == seed, "Encounter authority must preserve expedition seed.");
            Assert(expedition.Interior.Seed == seed, "Interior authority must preserve expedition seed.");
            Assert(expedition.Interior.Scale == expedition.Scale, "Interior scale must preserve expedition scale authority.");
            Assert(expedition.Interior.Modules.Count == expedition.Dungeon.Modules.Count, "Interior projection must cover every planned district.");
        }

        foreach (var scale in new[] { DeepFractureScale.Small, DeepFractureScale.Full, DeepFractureScale.Grand })
        {
            var forced = DeepFractureExpeditionPlanner.BuildForScale(90210, scale);
            Assert(forced.Scale == scale && forced.Dungeon.Scale == scale && forced.Interior.Scale == scale, $"Forced {scale} planning must preserve the requested scale end to end.");
        }

        var maxRadius = DeepFractureExpeditionPlanner.MaximumSupportedInteriorRadius();
        Assert(maxRadius > 0d && !double.IsNaN(maxRadius) && !double.IsInfinity(maxRadius), "Maximum supported interior radius must be finite and positive.");

        for (var seed = 1; seed <= 8; seed++)
        {
            foreach (var scale in new[] { DeepFractureScale.Small, DeepFractureScale.Full, DeepFractureScale.Grand })
            {
                var expedition = DeepFractureExpeditionPlanner.BuildForScale(seed, scale);
                Assert(expedition.Interior.InteriorRadius <= maxRadius, "Conservative runtime interior radius must contain every supported generated expedition.");
            }
        }

        var invalidWeightsRejected = false;
        try
        {
            DeepFractureExpeditionPlanner.SelectScale(1, new DeepFractureExpeditionScalePolicy(0, 0, 0));
        }
        catch (InvalidOperationException)
        {
            invalidWeightsRejected = true;
        }
        Assert(invalidWeightsRejected, "Scale selection must fail closed when all weights are zero.");

        var seen = Enumerable.Range(0, 256)
            .Select(seed => DeepFractureExpeditionPlanner.SelectScale(seed))
            .Distinct()
            .ToArray();
        Assert(seen.Length == 3, "Default scale policy should actually produce Small, Full and Grand expeditions across a representative seed range.");

        return assertions;
    }
}
