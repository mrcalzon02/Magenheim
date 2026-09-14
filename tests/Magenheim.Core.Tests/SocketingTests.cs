using System;
using System.Collections.Generic;
using Magenheim.Core.Socketing;

internal static class SocketingTests
{
    internal static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            assertions++;
        }

        var state = new SocketState(2, new[]
        {
            new Crystal(ElementalAlignment.Earth, CrystalTier.Simple),
            new Crystal(ElementalAlignment.Fire, CrystalTier.Advanced)
        });
        var encoded = SocketMetadataCodec.Encode(state);
        Assert(SocketMetadataCodec.TryDecode(encoded, out var decoded, out var diagnostic),
            "Valid socket metadata should round-trip: " + diagnostic);
        Assert(decoded.UnlockedSlots == 2, "Round-tripped socket count changed.");
        Assert(decoded.InstalledCrystals.Count == 2, "Round-tripped installed crystal count changed.");
        Assert(decoded.InstalledCrystals[0].Element == ElementalAlignment.Earth &&
               decoded.InstalledCrystals[1].Tier == CrystalTier.Advanced,
            "Round-tripped crystal identities changed.");

        Assert(!SocketMetadataCodec.TryDecode("1|1|Earth:Simple,Fire:Simple", out _, out _),
            "Metadata with more crystals than slots must fail closed.");
        Assert(!SocketMetadataCodec.TryDecode("2|1|Earth:Simple", out _, out _),
            "Unknown socket metadata versions must fail closed.");

        var foreignData = new Dictionary<string, string>
        {
            ["foreign.mod.key"] = "preserve-me"
        };
        SocketMetadataCodec.Write(foreignData, state);
        Assert(foreignData["foreign.mod.key"] == "preserve-me",
            "Writing Magenheim socket state must preserve foreign custom data.");
        Assert(foreignData.ContainsKey(SocketMetadataCodec.CustomDataKey),
            "Socket write must create only the namespaced Magenheim key.");
        SocketMetadataCodec.Write(foreignData, SocketState.Empty);
        Assert(!foreignData.ContainsKey(SocketMetadataCodec.CustomDataKey) &&
               foreignData["foreign.mod.key"] == "preserve-me",
            "Clearing empty Magenheim socket state must not remove foreign metadata.");

        var addOne = SocketingService.AddSlot(SocketState.Empty, 2);
        Assert(addOne.Changed && addOne.State.UnlockedSlots == 1,
            "Adding a first socket should create one unlocked slot.");
        var addTwo = SocketingService.AddSlot(addOne.State, 2);
        Assert(addTwo.Changed && addTwo.State.UnlockedSlots == 2,
            "Adding a second socket should reach configured capacity.");
        var atCapacity = SocketingService.AddSlot(addTwo.State, 2);
        Assert(!atCapacity.Changed && atCapacity.Outcome == SocketMutationOutcome.AtCapacity,
            "Adding beyond configured capacity must not mutate socket state.");

        var installed = SocketingService.InstallCrystal(addTwo.State,
            new Crystal(ElementalAlignment.Storm, CrystalTier.Master));
        Assert(installed.Changed && installed.State.InstalledCrystals.Count == 1,
            "Installing into a free socket should persist the crystal.");
        var removed = SocketingService.RemoveCrystal(installed.State, 0);
        Assert(removed.Changed && removed.RemovedCrystal is { Element: ElementalAlignment.Storm, Tier: CrystalTier.Master },
            "Removing a crystal should return the exact installed crystal.");

        var policy = new SocketEligibilityPolicy();
        var weapon = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("SwordIron", "vanilla", EquipmentCategory.Weapon), policy);
        Assert(weapon.IsEligible && weapon.MaximumSlots == 3,
            "Default adaptive weapon classification should permit three sockets.");

        var unknown = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("ForeignThing", "SomeMod", EquipmentCategory.Unknown), policy);
        Assert(!unknown.IsEligible && unknown.Outcome == SocketEligibilityOutcome.UnknownCategory,
            "Unknown equipment must fail safely without an explicit compatibility include.");

        var explicitPolicy = new SocketEligibilityPolicy(
            includedPrefabNames: new[] { "ForeignThing" },
            explicitIncludeMaxSlots: 2);
        var explicitUnknown = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("ForeignThing", "SomeMod", EquipmentCategory.Unknown), explicitPolicy);
        Assert(explicitUnknown.IsEligible && explicitUnknown.MaximumSlots == 2,
            "Explicit prefab inclusion should opt an otherwise unknown item into socketing.");

        var exclusionWins = new SocketEligibilityPolicy(
            includedPrefabNames: new[] { "ForeignThing" },
            excludedPrefabNames: new[] { "foreignthing" },
            identityComparison: SocketIdentityComparison.CaseInsensitive);
        var excluded = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("ForeignThing", "SomeMod", EquipmentCategory.Unknown), exclusionWins);
        Assert(!excluded.IsEligible && excluded.Outcome == SocketEligibilityOutcome.ExcludedPrefab,
            "Explicit exclusion must win over inclusion under the configured identity comparer.");

        return assertions;
    }
}
