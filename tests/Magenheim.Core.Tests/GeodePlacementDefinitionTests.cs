using System;
using System.Runtime.CompilerServices;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;

internal static class GeodePlacementDefinitionTests
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var assertions = Run();
        Console.WriteLine($"GeodePlacementDefinitionTests: {assertions} assertions passed.");
    }

    private static int Run()
    {
        var assertions = 0;

        static void Assert(ref int count, bool condition, string message)
        {
            count++;
            if (!condition)
                throw new InvalidOperationException($"GeodePlacementDefinition assertion {count} failed: {message}");
        }

        var baseline = CreateSnapshot(GeodePlacementDefinition.ConservativeMeadows);
        var placement = baseline.Geodes[0].Placement;
        Assert(ref assertions, Math.Abs(placement.MaxPerZone - 0.35d) < 0.0000001d,
            "Validated snapshot must preserve the authoritative Meadows placement chance.");
        Assert(ref assertions, placement.GroupSizeMin == 1 && placement.GroupSizeMax == 1,
            "Validated snapshot must preserve one-object geode groups.");

        var changed = CreateSnapshot(GeodePlacementDefinition.ConservativeMeadows with { MaxPerZone = 0.50d });
        Assert(ref assertions, !string.Equals(baseline.Fingerprint, changed.Fingerprint, StringComparison.Ordinal),
            "World-placement changes must alter the definition fingerprint.");

        var invalidTiltRejected = false;
        try
        {
            CreateSnapshot(GeodePlacementDefinition.ConservativeMeadows with { MaxTilt = 91d });
        }
        catch (InvalidOperationException)
        {
            invalidTiltRejected = true;
        }
        Assert(ref assertions, invalidTiltRejected,
            "Placement tilt outside the runtime 0..90 range must fail definition admission.");

        var invalidObjectTiltRejected = false;
        try
        {
            CreateSnapshot(GeodePlacementDefinition.ConservativeMeadows with { RandomTilt = 181d });
        }
        catch (InvalidOperationException)
        {
            invalidObjectTiltRejected = true;
        }
        Assert(ref assertions, invalidObjectTiltRejected,
            "Object lean beyond a half turn must fail definition admission.");

        var invalidGroundTiltChanceRejected = false;
        try
        {
            CreateSnapshot(GeodePlacementDefinition.ConservativeMeadows with { GroundTiltChance = 1.5d });
        }
        catch (InvalidOperationException)
        {
            invalidGroundTiltChanceRejected = true;
        }
        Assert(ref assertions, invalidGroundTiltChanceRejected,
            "Ground tilt chance outside 0..1 must fail definition admission.");

        var unrepresentableFloatRejected = false;
        try
        {
            CreateSnapshot(GeodePlacementDefinition.ConservativeMeadows with { MaxAltitude = double.MaxValue });
        }
        catch (InvalidOperationException)
        {
            unrepresentableFloatRejected = true;
        }
        Assert(ref assertions, unrepresentableFloatRejected,
            "Placement values that cannot be represented by the Jotunn float API must fail admission.");

        var overridden = MagenheimDefinitionOverrideApplier.Apply(
            baseline,
            Array.Empty<RefinementBalanceOverride>(),
            new[]
            {
                new GeodeBalanceOverride(
                    "magenheim.geode.meadows.earth",
                    0.35d,
                    0.10d,
                    new[] { new ElementWeight(ElementalAlignment.Earth, 100d) })
                {
                    Placement = placement with { GroundOffset = -0.20d },
                },
            });

        Assert(ref assertions, Math.Abs(overridden.Geodes[0].Placement.GroundOffset - (-0.20d)) < 0.0000001d,
            "Validated geode override path must carry world-placement configuration.");
        Assert(ref assertions, !string.Equals(baseline.Fingerprint, overridden.Fingerprint, StringComparison.Ordinal),
            "Server placement override must change effective definition authority.");

        return assertions;
    }

    private static MagenheimDefinitionSet CreateSnapshot(GeodePlacementDefinition placement)
    {
        var geode = new GeodeDefinition(
            "magenheim.geode.meadows.earth",
            "Meadows",
            "Magenheim_Geode_Meadows_Earth",
            SpawnArea.All,
            1,
            0.35d,
            0.10d,
            new[] { new ElementWeight(ElementalAlignment.Earth, 100d) })
        {
            Placement = placement,
        };

        return MagenheimDefinitionValidator.ValidateAndFreeze(
            MagenheimDefinitionValidator.CurrentSchemaVersion,
            CrystalRefinementService.CreateCanonicalDefaults(),
            new[] { geode },
            WorldgenCompatibilityPolicy.Conservative);
    }
}
