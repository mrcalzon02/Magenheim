using System;
using Magenheim.Core;
using Magenheim.Core.Definitions;
using Magenheim.Core.Socketing;
using Magenheim.Core.Worldgen;

internal static class DefinitionCompatibilityTests
{
    internal static int Run()
    {
        var assertions = 0;
        CaseInsensitivePolicyRejectsCaseOnlyGeodeIdentityCollision(ref assertions);
        CaseInsensitivePolicyRejectsCaseOnlyPrefabCollision(ref assertions);
        CaseInsensitivePolicyRejectsCaseOnlyExclusionDuplicate(ref assertions);
        ExactPolicyAllowsCaseDistinctOwnedExclusions(ref assertions);
        CaseInsensitiveFingerprintCanonicalizesExclusionCase(ref assertions);
        ExactFingerprintPreservesExclusionCase(ref assertions);
        SocketEffectFingerprintChangesWithMagnitude(ref assertions);
        OverridesPreserveSocketEffects(ref assertions);
        return assertions;
    }

    private static void CaseInsensitivePolicyRejectsCaseOnlyGeodeIdentityCollision(ref int assertions)
    {
        var geodes = new[]
        {
            CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth"),
            CreateGeode("magenheim.geode.meadows.EARTH", "Magenheim_Geode_Meadows_Earth_Alt"),
        };

        var threw = false;
        try
        {
            CreateSnapshot(geodes, WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
            });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert(threw, ref assertions, "Case-insensitive policy must reject geode ids that differ only by case.");
    }

    private static void CaseInsensitivePolicyRejectsCaseOnlyPrefabCollision(ref int assertions)
    {
        var geodes = new[]
        {
            CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth"),
            CreateGeode("magenheim.geode.meadows.earth-alt", "Magenheim_Geode_Meadows_EARTH"),
        };

        var threw = false;
        try
        {
            CreateSnapshot(geodes, WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
            });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert(threw, ref assertions, "Case-insensitive policy must reject prefab identities that differ only by case.");
    }

    private static void CaseInsensitivePolicyRejectsCaseOnlyExclusionDuplicate(ref int assertions)
    {
        var threw = false;
        try
        {
            CreateSnapshot(
                new[] { CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth") },
                WorldgenCompatibilityPolicy.Conservative with
                {
                    IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
                    ExcludedRegistrationKeys = new[]
                    {
                        "magenheim.geode.compatibility.foo",
                        "magenheim.geode.compatibility.FOO",
                    },
                });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert(threw, ref assertions, "Case-insensitive policy must reject exclusion entries that collapse to the same identity.");
    }

    private static void ExactPolicyAllowsCaseDistinctOwnedExclusions(ref int assertions)
    {
        var snapshot = CreateSnapshot(
            new[] { CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth") },
            WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.Exact,
                ExcludedRegistrationKeys = new[]
                {
                    "magenheim.geode.compatibility.foo",
                    "magenheim.geode.compatibility.FOO",
                },
            });

        Assert(snapshot.WorldgenCompatibility.ExcludedRegistrationKeys.Count == 2, ref assertions,
            "Exact identity policy should preserve case-distinct owned exclusions.");
    }

    private static void CaseInsensitiveFingerprintCanonicalizesExclusionCase(ref int assertions)
    {
        var geodes = new[] { CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth") };
        var lower = CreateSnapshot(
            geodes,
            WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
                ExcludedRegistrationKeys = new[] { "magenheim.compatibility.foo" },
                ExcludedPrefabNames = new[] { "Magenheim_Compatibility_Foo" },
            });
        var mixed = CreateSnapshot(
            geodes,
            WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.CaseInsensitive,
                ExcludedRegistrationKeys = new[] { "magenheim.compatibility.FOO" },
                ExcludedPrefabNames = new[] { "Magenheim_Compatibility_FOO" },
            });

        Assert(string.Equals(lower.Fingerprint, mixed.Fingerprint, StringComparison.Ordinal), ref assertions,
            "Case-insensitive exclusion identity must produce one canonical authority fingerprint regardless of configured suffix casing.");
    }

    private static void ExactFingerprintPreservesExclusionCase(ref int assertions)
    {
        var geodes = new[] { CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth") };
        var lower = CreateSnapshot(
            geodes,
            WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.Exact,
                ExcludedRegistrationKeys = new[] { "magenheim.compatibility.foo" },
            });
        var upper = CreateSnapshot(
            geodes,
            WorldgenCompatibilityPolicy.Conservative with
            {
                IdentityComparison = RegistrationIdentityComparison.Exact,
                ExcludedRegistrationKeys = new[] { "magenheim.compatibility.FOO" },
            });

        Assert(!string.Equals(lower.Fingerprint, upper.Fingerprint, StringComparison.Ordinal), ref assertions,
            "Exact exclusion identity must retain casing as part of definition authority.");
    }

    private static void SocketEffectFingerprintChangesWithMagnitude(ref int assertions)
    {
        var geodes = new[] { CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth") };
        var firstEffects = new SocketEffectDefinitionSet(new[]
        {
            new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Weapon, SocketEffectKind.BluntDamage, 2d),
        });
        var secondEffects = new SocketEffectDefinitionSet(new[]
        {
            new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Weapon, SocketEffectKind.BluntDamage, 2.5d),
        });

        var first = CreateSnapshot(geodes, WorldgenCompatibilityPolicy.Conservative, firstEffects);
        var second = CreateSnapshot(geodes, WorldgenCompatibilityPolicy.Conservative, secondEffects);

        Assert(!string.Equals(first.Fingerprint, second.Fingerprint, StringComparison.Ordinal), ref assertions,
            "Socket-effect magnitude must participate in synchronized definition authority fingerprinting.");
    }

    private static void OverridesPreserveSocketEffects(ref int assertions)
    {
        var effects = new SocketEffectDefinitionSet(new[]
        {
            new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Utility, SocketEffectKind.CarryWeight, 10d),
        });
        var baseline = CreateSnapshot(
            new[] { CreateGeode("magenheim.geode.meadows.earth", "Magenheim_Geode_Meadows_Earth") },
            WorldgenCompatibilityPolicy.Conservative,
            effects);

        var effective = MagenheimDefinitionOverrideApplier.Apply(
            baseline,
            Array.Empty<RefinementBalanceOverride>(),
            Array.Empty<GeodeBalanceOverride>());

        Assert(effective.SocketEffects.Rules.Count == 1 &&
               effective.SocketEffects.Rules[0].Effect == SocketEffectKind.CarryWeight &&
               Math.Abs(effective.SocketEffects.Rules[0].SimpleMagnitude - 10d) < 0.0000001d,
            ref assertions,
            "Balance/worldgen override application must preserve socket-effect authority instead of silently erasing it.");
        Assert(string.Equals(effective.Fingerprint, baseline.Fingerprint, StringComparison.Ordinal), ref assertions,
            "A no-op override pass must preserve the full schema-four authority fingerprint including socket effects.");
    }

    private static GeodeDefinition CreateGeode(string id, string prefabName) =>
        new(
            id,
            "Meadows",
            prefabName,
            SpawnArea.All,
            1,
            0.35d,
            0.10d,
            new[] { new ElementWeight(ElementalAlignment.Earth, 100d) });

    private static MagenheimDefinitionSet CreateSnapshot(
        GeodeDefinition[] geodes,
        WorldgenCompatibilityPolicy policy,
        SocketEffectDefinitionSet? socketEffects = null) =>
        MagenheimDefinitionValidator.ValidateAndFreeze(
            MagenheimDefinitionValidator.CurrentSchemaVersion,
            CrystalRefinementService.CreateCanonicalDefaults(),
            geodes,
            policy,
            socketEffects);

    private static void Assert(bool condition, ref int assertions, string message)
    {
        assertions++;
        if (!condition)
            throw new InvalidOperationException($"Definition compatibility assertion {assertions} failed: {message}");
    }
}
