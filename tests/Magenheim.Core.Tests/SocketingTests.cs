using System;
using System.Collections.Generic;
using Magenheim.Core.Networking;
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
        Assert(!SocketMetadataCodec.TryDecode("1|4|", out _, out _),
            "Socket metadata above the supported 0..3 range must fail closed.");

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

        var roughRejected = SocketingService.InstallCrystal(addTwo.State,
            new Crystal(ElementalAlignment.Earth, CrystalTier.Rough));
        Assert(!roughRejected.Changed && roughRejected.Outcome == SocketMutationOutcome.RoughCrystalNotSocketable,
            "Rough crystals must never be socketable.");

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
        Assert(weapon.IsEligible && weapon.MaximumSlots == 1,
            "Version-one default weapon classification should permit one socket, not silently enable multi-socket balance.");
        var shield = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("ShieldIron", "vanilla", EquipmentCategory.Shield), policy);
        Assert(shield.IsEligible && shield.MaximumSlots == 1,
            "Adaptive shield classification should default to one socket.");
        var tool = SocketEligibilityService.Evaluate(
            new EquipmentDescriptor("PickaxeIron", "vanilla", EquipmentCategory.Tool), policy);
        Assert(tool.IsEligible && tool.MaximumSlots == 1,
            "Adaptive tool classification should default to one socket.");

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

        var authority = new DefinitionAuthorityResult(
            DefinitionAuthorityStatus.Compatible, true, "test authority");
        var sword = new EquipmentDescriptor("SwordIron", "vanilla", EquipmentCategory.Weapon);
        var addSlotPlan = SocketTransactionPlanner.Plan(new SocketTransactionRequest(
            authority, sword, policy, SocketState.Empty, SocketTransactionKind.AddSlot, null, 0));
        Assert(addSlotPlan.IsReady && addSlotPlan.ResultState.UnlockedSlots == 1 &&
               addSlotPlan.ConsumeCrystalCount == 0,
            "Eligible equipment should prepare a per-instance AddSlot mutation without consuming a crystal.");

        var secondSlotDenied = SocketTransactionPlanner.Plan(new SocketTransactionRequest(
            authority, sword, policy, addSlotPlan.ResultState, SocketTransactionKind.AddSlot, null, 0));
        Assert(!secondSlotDenied.IsReady && secondSlotDenied.Outcome == SocketTransactionOutcome.MutationRejected,
            "Default one-socket policy must deny a second unlocked socket.");

        var installPlan = SocketTransactionPlanner.Plan(new SocketTransactionRequest(
            authority,
            sword,
            policy,
            addSlotPlan.ResultState,
            SocketTransactionKind.InstallCrystal,
            new Crystal(ElementalAlignment.Earth, CrystalTier.Simple),
            1));
        Assert(installPlan.IsReady && installPlan.ConsumeCrystalCount == 1 &&
               installPlan.ResultState.InstalledCrystals.Count == 1 &&
               Math.Abs(installPlan.CrystalShapingExperience - 0.25d) < 0.0000001d,
            "Installing a Simple crystal should consume exactly one crystal and award the configured initial socket-install XP weight.");

        var missingCrystal = SocketTransactionPlanner.Plan(new SocketTransactionRequest(
            authority,
            sword,
            policy,
            addSlotPlan.ResultState,
            SocketTransactionKind.InstallCrystal,
            new Crystal(ElementalAlignment.Earth, CrystalTier.Simple),
            0));
        Assert(!missingCrystal.IsReady && missingCrystal.Outcome == SocketTransactionOutcome.MissingCrystal,
            "Socket install must fail without the exact source crystal.");

        var deniedAuthority = SocketTransactionPlanner.Plan(new SocketTransactionRequest(
            DefinitionAuthorityResult.Pending,
            sword,
            policy,
            SocketState.Empty,
            SocketTransactionKind.AddSlot,
            null,
            0));
        Assert(!deniedAuthority.IsReady && deniedAuthority.Outcome == SocketTransactionOutcome.AuthorityDenied,
            "Unsynchronized authority must deny socket metadata mutation.");

        var guard = new SocketOperationGuard();
        var key = new SocketOperationKey(21, 1, "socket-1");
        var transaction = new SocketTransactionRequest(
            authority, sword, policy, SocketState.Empty, SocketTransactionKind.AddSlot, null, 0);
        var fresh = guard.Begin(key, transaction);
        Assert(fresh.MutationAuthorized,
            "Fresh socket operation should reserve exactly one metadata mutation attempt.");
        var duplicatePrepared = guard.Begin(key, transaction);
        Assert(duplicatePrepared.Outcome == SocketOperationOutcome.DuplicatePrepared &&
               !duplicatePrepared.MutationAuthorized,
            "Prepared socket replay must not authorize duplicate mutation.");
        Assert(guard.MarkApplied(key), "Prepared socket operation should mark applied exactly once.");
        Assert(!guard.MarkApplied(key), "Applied socket operation must not mark applied twice.");
        var duplicateApplied = guard.Begin(key, transaction);
        Assert(duplicateApplied.Outcome == SocketOperationOutcome.DuplicateApplied &&
               !duplicateApplied.MutationAuthorized,
            "Applied socket replay must not authorize a second metadata write.");

        var stale = new SocketOperationKey(33, 4, "socket-old");
        Assert(guard.Begin(stale, transaction).MutationAuthorized,
            "Old-session setup socket operation should prepare.");
        Assert(guard.RetirePeerSessions(33, 5) == 1,
            "New peer session should retire stale socket replay reservations.");

        var extractionState = new SocketState(1, new[]
        {
            new Crystal(ElementalAlignment.Earth, CrystalTier.Master)
        });
        var extractionSuccess = SocketExtractionService.Plan(new SocketExtractionRequest(
            authority,
            extractionState,
            0,
            0,
            SocketExtractionService.RequiredStationId,
            0.90d));
        Assert(extractionSuccess.MutationAuthorized && extractionSuccess.ReturnsCrystal &&
               extractionSuccess.ReturnedCrystal is { Element: ElementalAlignment.Earth, Tier: CrystalTier.Master } &&
               extractionSuccess.ResultState.InstalledCrystals.Count == 0 &&
               Math.Abs(extractionSuccess.EffectiveFailureChance - 0.40d) < 0.0000001d,
            "A Master extraction roll above the skill-zero 40% break chance should return the exact crystal and clear its socket.");
        Assert(Math.Abs(extractionSuccess.CrystalShapingExperience - CrystalShapingExperience.CrystalExtraction) < 0.0000001d,
            "Valid extraction should award the canonical Crystal Shaping extraction experience weight.");

        var extractionFailure = SocketExtractionService.Plan(new SocketExtractionRequest(
            authority,
            extractionState,
            0,
            0,
            SocketExtractionService.RequiredStationId,
            0.10d));
        Assert(extractionFailure.MutationAuthorized &&
               extractionFailure.Outcome == SocketExtractionOutcome.FailedShatteredCrystal &&
               extractionFailure.ReturnedCrystal is null &&
               extractionFailure.ShardElement == ElementalAlignment.Earth &&
               extractionFailure.ShardReturnCount == 5 &&
               extractionFailure.ResultState.InstalledCrystals.Count == 0,
            "A failed Master extraction should clear the socket, destroy the crystal, and return five matching shards.");

        var skilledExtraction = SocketExtractionService.Plan(new SocketExtractionRequest(
            authority,
            extractionState,
            0,
            100,
            SocketExtractionService.RequiredStationId,
            0.11d));
        Assert(skilledExtraction.Outcome == SocketExtractionOutcome.SuccessReturnedCrystal &&
               Math.Abs(skilledExtraction.EffectiveFailureChance - 0.10d) < 0.0000001d,
            "Skill 100 with the default 75% maximum reduction should lower Master extraction break chance from 40% to 10%.");

        var wrongStationExtraction = SocketExtractionService.Plan(new SocketExtractionRequest(
            authority,
            extractionState,
            0,
            0,
            "Magenheim_GeologistWorkstation",
            0.90d));
        Assert(!wrongStationExtraction.MutationAuthorized &&
               wrongStationExtraction.Outcome == SocketExtractionOutcome.InvalidStation &&
               wrongStationExtraction.ResultState.InstalledCrystals.Count == 1,
            "Extraction without the Faceting Wheel must fail without changing per-item metadata.");

        var extractionAuthorityDenied = SocketExtractionService.Plan(new SocketExtractionRequest(
            DefinitionAuthorityResult.Pending,
            extractionState,
            0,
            0,
            SocketExtractionService.RequiredStationId,
            0.90d));
        Assert(!extractionAuthorityDenied.MutationAuthorized &&
               extractionAuthorityDenied.Outcome == SocketExtractionOutcome.AuthorityDenied,
            "Unsynchronized definition authority must deny crystal extraction.");

        var extractionGuard = new SocketExtractionOperationGuard();
        var extractionKey = new SocketExtractionOperationKey(44, 2, "extract-1");
        var extractionRequest = new SocketExtractionRequest(
            authority,
            extractionState,
            0,
            0,
            SocketExtractionService.RequiredStationId,
            0.90d);
        Assert(extractionGuard.Begin(extractionKey, extractionRequest).MutationAuthorized,
            "Fresh extraction should reserve exactly one metadata mutation attempt.");
        Assert(extractionGuard.Begin(extractionKey, extractionRequest).Outcome == SocketExtractionOperationOutcome.DuplicatePrepared,
            "Prepared extraction replay must not authorize a second mutation.");
        Assert(extractionGuard.MarkApplied(extractionKey),
            "Prepared extraction should mark applied exactly once.");
        Assert(extractionGuard.Begin(extractionKey, extractionRequest).Outcome == SocketExtractionOperationOutcome.DuplicateApplied,
            "Applied extraction replay must remain non-mutating.");

        var oldExtraction = new SocketExtractionOperationKey(55, 7, "extract-old");
        Assert(extractionGuard.Begin(oldExtraction, extractionRequest).MutationAuthorized,
            "Old-session extraction setup should prepare.");
        Assert(extractionGuard.RetirePeerSessions(55, 8) == 1,
            "A new peer session must retire stale extraction replay reservations.");

        var effectDefinitions = new SocketEffectDefinitionSet(new[]
        {
            new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Weapon, SocketEffectKind.BluntDamage, 2d),
            new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Weapon, SocketEffectKind.Knockback, 0.05d),
            new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Armor, SocketEffectKind.Armor, 1.5d),
            new SocketEffectRule(ElementalAlignment.Fire, EquipmentCategory.Weapon, SocketEffectKind.FireDamage, 3d)
        });
        var mixedEffectState = new SocketState(2, new[]
        {
            new Crystal(ElementalAlignment.Earth, CrystalTier.Master),
            new Crystal(ElementalAlignment.Fire, CrystalTier.Crystal)
        });
        var weaponEffects = SocketEffectService.Calculate(
            mixedEffectState,
            EquipmentCategory.Weapon,
            effectDefinitions);
        Assert(weaponEffects.IsSuccess &&
               Math.Abs(weaponEffects.Get(SocketEffectKind.BluntDamage) - 7d) < 0.0000001d &&
               Math.Abs(weaponEffects.Get(SocketEffectKind.Knockback) - 0.175d) < 0.0000001d &&
               Math.Abs(weaponEffects.Get(SocketEffectKind.FireDamage) - 4.5d) < 0.0000001d,
            "Socket effect calculation must scale each installed crystal by tier and add only matching element/category rules.");
        Assert(Math.Abs(weaponEffects.Get(SocketEffectKind.Armor)) < 0.0000001d,
            "Weapon effect calculation must not leak armor-category rules onto the same item.");

        var armorEffects = SocketEffectService.Calculate(
            mixedEffectState,
            EquipmentCategory.Armor,
            effectDefinitions);
        Assert(armorEffects.IsSuccess &&
               Math.Abs(armorEffects.Get(SocketEffectKind.Armor) - 5.25d) < 0.0000001d &&
               Math.Abs(armorEffects.Get(SocketEffectKind.FireDamage)) < 0.0000001d,
            "Armor calculation should apply the Master Earth tier scalar and ignore weapon-only Fire rules.");

        var unknownEffects = SocketEffectService.Calculate(
            mixedEffectState,
            EquipmentCategory.Unknown,
            effectDefinitions);
        Assert(!unknownEffects.IsSuccess &&
               unknownEffects.Outcome == SocketEffectCalculationOutcome.UnknownEquipmentCategory &&
               unknownEffects.Effects.Count == 0,
            "Unknown equipment classification must fail closed without applying guessed socket bonuses.");

        var malformedRoughState = new SocketState(1, new[]
        {
            new Crystal(ElementalAlignment.Earth, CrystalTier.Rough)
        });
        var roughEffects = SocketEffectService.Calculate(
            malformedRoughState,
            EquipmentCategory.Weapon,
            effectDefinitions);
        Assert(!roughEffects.IsSuccess &&
               roughEffects.Outcome == SocketEffectCalculationOutcome.InvalidSocketState &&
               roughEffects.Effects.Count == 0,
            "Malformed metadata containing a Rough installed crystal must not produce gameplay effects.");

        var duplicateRuleRejected = false;
        try
        {
            _ = new SocketEffectDefinitionSet(new[]
            {
                new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Weapon, SocketEffectKind.BluntDamage, 1d),
                new SocketEffectRule(ElementalAlignment.Earth, EquipmentCategory.Weapon, SocketEffectKind.BluntDamage, 2d)
            });
        }
        catch (InvalidOperationException)
        {
            duplicateRuleRejected = true;
        }
        Assert(duplicateRuleRejected,
            "Duplicate element/category/effect rules must reject instead of silently stacking ambiguous balance authority.");

        return assertions;
    }
}
