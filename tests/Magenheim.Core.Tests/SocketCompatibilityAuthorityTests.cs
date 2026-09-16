using System;
using System.Runtime.CompilerServices;
using Magenheim.Core.Socketing;

internal static class SocketCompatibilityAuthorityTests
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var assertions = Run();
        Console.WriteLine($"SocketCompatibilityAuthorityTests: {assertions} assertions passed.");
    }

    private static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Socket compatibility authority assertion {assertions} failed: {message}");
        }

        var caseInsensitive = new SocketEligibilityPolicy(
            explicitIncludeMaxSlots: 2,
            identityComparison: SocketIdentityComparison.CaseInsensitive,
            includedItemNames: new[] { "$item_foreign", "$ITEM_FOREIGN" });
        Assert(caseInsensitive.IncludedItemNames.Count == 1,
            "Case-insensitive policy should canonicalize duplicate item identities.");

        var includedItem = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("ForeignPrefab", "foreign.mod", EquipmentCategory.Unknown)
            {
                ItemName = "$ITEM_FOREIGN",
            },
            caseInsensitive);
        Assert(includedItem.IsEligible && includedItem.MaximumSlots == 2,
            "Item-name inclusion should opt otherwise unknown equipment into socketing.");

        var excludedItemPolicy = new SocketEligibilityPolicy(
            includedItemNames: new[] { "$item_foreign" },
            excludedItemNames: new[] { "$item_foreign" });
        var excludedItem = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("ForeignPrefab", "foreign.mod", EquipmentCategory.Weapon)
            {
                ItemName = "$item_foreign",
            },
            excludedItemPolicy);
        Assert(!excludedItem.IsEligible && excludedItem.Outcome == SocketEligibilityOutcome.ExcludedItem,
            "Explicit item exclusion must win over category eligibility and inclusion.");

        var invalidComparisonRejected = false;
        try
        {
            _ = new SocketEligibilityPolicy(identityComparison: (SocketIdentityComparison)99);
        }
        catch (InvalidOperationException)
        {
            invalidComparisonRejected = true;
        }
        Assert(invalidComparisonRejected,
            "Unknown identity comparison values must fail closed instead of degrading to exact matching.");

        var definitionFingerprint = new string('a', GameplayAuthorityFingerprint.Sha256HexLength);
        var baseline = GameplayAuthorityFingerprint.Compute(definitionFingerprint, new SocketEligibilityPolicy());
        var changedLimit = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(weaponMaxSlots: 2));
        Assert(!string.Equals(baseline, changedLimit, StringComparison.Ordinal),
            "Changing socket limits must change gameplay authority.");

        var changedExclusion = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(excludedItemNames: new[] { "$item_sword_iron" }));
        Assert(!string.Equals(baseline, changedExclusion, StringComparison.Ordinal),
            "Changing item compatibility exclusions must change gameplay authority.");

        var spatialA = new string('b', GameplayAuthorityFingerprint.Sha256HexLength);
        var spatialB = new string('c', GameplayAuthorityFingerprint.Sha256HexLength);
        var spatialAuthorityA = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(),
            spatialA);
        var spatialAuthorityB = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(),
            spatialB);
        Assert(!string.Equals(spatialAuthorityA, spatialAuthorityB, StringComparison.Ordinal),
            "Changing the Underworld spatial-domain fingerprint must change gameplay peer authority.");
        Assert(!string.Equals(baseline, spatialAuthorityA, StringComparison.Ordinal),
            "Presence versus absence of Underworld spatial authority must be fingerprint-significant.");

        var invalidSpatialRejected = false;
        try
        {
            _ = GameplayAuthorityFingerprint.Compute(
                definitionFingerprint,
                new SocketEligibilityPolicy(),
                "not-a-sha256");
        }
        catch (ArgumentException)
        {
            invalidSpatialRejected = true;
        }
        Assert(invalidSpatialRejected,
            "Malformed Underworld spatial authority must fail closed before peer synchronization.");

        var caseA = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(
                identityComparison: SocketIdentityComparison.CaseInsensitive,
                excludedModOrigins: new[] { "Example.Mod" }));
        var caseB = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(
                identityComparison: SocketIdentityComparison.CaseInsensitive,
                excludedModOrigins: new[] { "example.mod" }));
        Assert(string.Equals(caseA, caseB, StringComparison.Ordinal),
            "Case-insensitive policy must hash semantically equivalent identity casing identically.");

        var exactA = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(
                identityComparison: SocketIdentityComparison.Exact,
                excludedModOrigins: new[] { "Example.Mod" }));
        var exactB = GameplayAuthorityFingerprint.Compute(
            definitionFingerprint,
            new SocketEligibilityPolicy(
                identityComparison: SocketIdentityComparison.Exact,
                excludedModOrigins: new[] { "example.mod" }));
        Assert(!string.Equals(exactA, exactB, StringComparison.Ordinal),
            "Exact identity policy must preserve casing significance in gameplay authority.");

        return assertions;
    }
}
