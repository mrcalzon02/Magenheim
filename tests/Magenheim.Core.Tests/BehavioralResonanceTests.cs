using System;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.Socketing;

internal static class BehavioralResonanceTests
{
    internal static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            assertions++;
        }

        var plan = BehavioralResonance.Plan(
            new[]
            {
                new Crystal(ElementalAlignment.Storm, CrystalTier.Master),
                new Crystal(ElementalAlignment.Storm, CrystalTier.Advanced),
                new Crystal(ElementalAlignment.Storm, CrystalTier.Simple),
                new Crystal(ElementalAlignment.Venom, CrystalTier.Crystal)
            },
            new[] { 1f, 0.5f, 0.25f });

        Assert(plan.Count == 2,
            "Four physical sockets across two elements must produce exactly two behavioral processors.");

        var storm = plan.Single(entry => entry.Element == ElementalAlignment.Storm);
        Assert(storm.ContributingCrystalCount == 3,
            "Repeated Storm sockets must strengthen one Storm processor rather than create three proc sources.");
        Assert(storm.HighestTier == CrystalTier.Master,
            "Behavioral tier gates must use the strongest contributing crystal in the elemental family.");
        Assert(Math.Abs(storm.EffectiveTierPower - 4.875d) < 0.0000001d,
            "Storm behavioral power must use the same diminishing resonance authority as numerical effects.");

        var venom = plan.Single(entry => entry.Element == ElementalAlignment.Venom);
        Assert(venom.ContributingCrystalCount == 1 && venom.HighestTier == CrystalTier.Crystal,
            "A distinct element must receive its own independent processor and tier gate.");
        Assert(Math.Abs(venom.EffectiveTierPower - 1.5d) < 0.0000001d,
            "A single behavioral crystal must retain ordinary Magenheim tier power.");

        var single = BehavioralResonance.Plan(
            new[] { new Crystal(ElementalAlignment.Storm, CrystalTier.Master) },
            new[] { 1f, 0.5f, 0.25f });
        Assert(single.Count == 1 &&
               single[0].ContributingCrystalCount == 1 &&
               Math.Abs(single[0].EffectiveTierPower - 3.5d) < 0.0000001d,
            "Standalone Magenheim equipment must still resolve to one unchanged behavioral processor.");

        return assertions;
    }
}
