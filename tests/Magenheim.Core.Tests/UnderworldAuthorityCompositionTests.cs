using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldAuthorityCompositionTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException(
                    $"Underworld authority composition assertion {assertions} failed: {message}");
        }

        var content = CreateContent();
        var architecture = UnderworldArchitectureCatalog.CreateInitialStructuralSlice();
        var authority = UnderworldAuthorityComposer.Compose(content, architecture);

        Assert(authority.SchemaVersion == UnderworldAuthorityComposer.CurrentSchemaVersion,
            "Composite authority must expose its schema version.");
        Assert(authority.Fingerprint.Length == 64,
            "Composite authority fingerprint must be SHA-256 hex.");
        Assert(ReferenceEquals(authority.Content, content),
            "Composite authority must retain the validated content snapshot.");
        Assert(ReferenceEquals(authority.Architecture, architecture),
            "Composite authority must retain the validated architecture snapshot.");

        var repeat = UnderworldAuthorityComposer.Compose(content, architecture);
        Assert(repeat.Fingerprint == authority.Fingerprint,
            "Identical validated component authorities must compose deterministically.");

        var changedPieces = architecture.Pieces
            .Select(value => value.Id.EndsWith("worldroot_beam_8m", StringComparison.Ordinal)
                ? value with
                {
                    Costs = Array.AsReadOnly(new[]
                    {
                        new UnderworldBuildCost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 9),
                    }),
                }
                : value)
            .ToArray();
        var changedArchitecture = UnderworldArchitectureValidator.ValidateAndFreeze(
            UnderworldArchitectureValidator.CurrentSchemaVersion,
            changedPieces);
        var changedAuthority = UnderworldAuthorityComposer.Compose(content, changedArchitecture);
        Assert(changedAuthority.Fingerprint != authority.Fingerprint,
            "Gameplay-significant architecture changes must alter canonical Underworld authority.");

        var changedContent = CreateContent("magenheim.underworld.boon.spore_communion_variant");
        var changedContentAuthority = UnderworldAuthorityComposer.Compose(changedContent, architecture);
        Assert(changedContentAuthority.Fingerprint != authority.Fingerprint,
            "Gameplay-significant biome/boss/Deepstone changes must alter canonical Underworld authority.");

        AssertThrows(Assert,
            () => UnderworldAuthorityComposer.Compose(null!, architecture),
            "Null content authority must fail closed.");
        AssertThrows(Assert,
            () => UnderworldAuthorityComposer.Compose(content, null!),
            "Null architecture authority must fail closed.");

        return assertions;
    }

    private static UnderworldDefinitionSet CreateContent(string firstBoon = "magenheim.underworld.boon.spore_communion")
    {
        var biome = new UnderworldBiomeDefinition(
            "magenheim.underworld.biome.fungal_forest",
            "Fungal Forest",
            "magenheim.underworld.boss.first_bloom");
        var boss = new UnderworldBossDefinition(
            "magenheim.underworld.boss.first_bloom",
            biome.Id,
            "magenheim.underworld.location.first_bloom",
            "magenheim.underworld.deepstone.bloom",
            "Magenheim_Underworld_Trophy_FirstBloom",
            firstBoon,
            Array.Empty<string>());
        var stone = new UnderworldDeepstoneDefinition(
            boss.DeepstoneSlotId,
            boss.Id,
            firstBoon);

        return UnderworldDefinitionValidator.ValidateAndFreeze(
            UnderworldDefinitionValidator.CurrentSchemaVersion,
            new[] { biome },
            new[] { boss },
            new[] { stone });
    }

    private static void AssertThrows(Action<bool, string> assert, Action action, string message)
    {
        var threw = false;
        try
        {
            action();
        }
        catch (ArgumentNullException)
        {
            threw = true;
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        assert(threw, message);
    }
}
