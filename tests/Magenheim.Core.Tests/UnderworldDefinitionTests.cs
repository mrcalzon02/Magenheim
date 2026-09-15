using System;
using System.Linq;
using System.Numerics;
using Magenheim.Core.DarkThrone;
using Magenheim.Core.Underworld;

internal static class UnderworldDefinitionTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException(
                    $"Underworld definition assertion {assertions} failed: {message}");
        }

        var foundation = CreateFoundation();
        Assert(foundation.Biomes.Count == 6, "Canonical fixture must contain six primary Underworld biomes.");
        Assert(foundation.Bosses.Count == 6, "Canonical fixture must contain six primary Underworld bosses.");
        Assert(foundation.Deepstones.Count == 6, "Canonical fixture must contain six Deepstones.");
        Assert(foundation.Fingerprint.Length == 64, "Underworld definition fingerprint must be SHA-256 hex.");

        var reversed = CreateFoundation(reverseInputs: true);
        Assert(
            foundation.Fingerprint == reversed.Fingerprint,
            "Definition fingerprint must be independent of input ordering.");

        var alteredBosses = foundation.Bosses
            .Select(value => value.Id == Boss("first_bloom")
                ? value with { DeepBoonId = Boon("different_bloom") }
                : value)
            .ToArray();
        var alteredDeepstones = foundation.Deepstones
            .Select(value => value.BossId == Boss("first_bloom")
                ? value with { DeepBoonId = Boon("different_bloom") }
                : value)
            .ToArray();
        var altered = UnderworldDefinitionValidator.ValidateAndFreeze(
            UnderworldDefinitionValidator.CurrentSchemaVersion,
            foundation.Biomes,
            alteredBosses,
            alteredDeepstones);
        Assert(
            foundation.Fingerprint != altered.Fingerprint,
            "Gameplay-significant Deep Boon changes must alter the Underworld fingerprint.");

        AssertThrows(
            Assert,
            () => UnderworldDefinitionValidator.ValidateAndFreeze(
                UnderworldDefinitionValidator.CurrentSchemaVersion,
                foundation.Biomes.Concat(new[] { foundation.Biomes[0] }),
                foundation.Bosses,
                foundation.Deepstones),
            "Duplicate biome identities must fail closed.");

        var foreignBiome = foundation.Biomes
            .Select((value, index) => index == 0 ? value with { Id = "foreign.biome.fungal" } : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldDefinitionValidator.ValidateAndFreeze(
                UnderworldDefinitionValidator.CurrentSchemaVersion,
                foreignBiome,
                foundation.Bosses,
                foundation.Deepstones),
            "Biome identities outside the Magenheim Underworld namespace must be rejected.");

        var missingBiomeBosses = foundation.Bosses
            .Select((value, index) => index == 0
                ? value with { BiomeId = Biome("missing") }
                : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldDefinitionValidator.ValidateAndFreeze(
                UnderworldDefinitionValidator.CurrentSchemaVersion,
                foundation.Biomes,
                missingBiomeBosses,
                foundation.Deepstones),
            "Bosses cannot reference unknown biomes.");

        var mismatchedStone = foundation.Deepstones
            .Select((value, index) => index == 0
                ? value with { BossId = Boss("blackwater_maw") }
                : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldDefinitionValidator.ValidateAndFreeze(
                UnderworldDefinitionValidator.CurrentSchemaVersion,
                foundation.Biomes,
                foundation.Bosses,
                mismatchedStone),
            "Deepstones must agree with their owning boss.");

        var cyclicBosses = foundation.Bosses
            .Select(value => value.Id == Boss("first_bloom")
                ? value with
                {
                    PrerequisiteBossIds = Array.AsReadOnly(new[] { Boss("carrion_crown") }),
                }
                : value)
            .ToArray();
        AssertThrows(
            Assert,
            () => UnderworldDefinitionValidator.ValidateAndFreeze(
                UnderworldDefinitionValidator.CurrentSchemaVersion,
                foundation.Biomes,
                cyclicBosses,
                foundation.Deepstones),
            "Boss progression cycles must be rejected.");

        var worldA = UnderworldWorldIdentityFactory.Derive("world-123", "SeedAlpha");
        var worldARepeat = UnderworldWorldIdentityFactory.Derive("world-123", "SeedAlpha");
        var worldB = UnderworldWorldIdentityFactory.Derive("world-123", "SeedBeta");
        Assert(worldA == worldARepeat, "Derived Underworld identity must be deterministic.");
        Assert(
            worldA.DerivedWorldId == "world-123:magenheim-underworld",
            "Derived world identity must remain visibly paired with its parent.");
        Assert(
            worldA.DerivedSeedFingerprint != worldB.DerivedSeedFingerprint,
            "Different parent seeds must derive different Underworld seeds.");
        Assert(
            worldA.DerivedSeedFingerprint.Length == 64,
            "Derived seed fingerprint must be canonical SHA-256 length.");

        var alive = new DarkThroneEncounterSnapshot(
            DarkThroneEncounterSnapshot.CurrentSchemaVersion,
            "magenheim.dark_throne",
            Vector3.Zero,
            DarkThroneEncounterLifecycle.Disengaged,
            1d,
            false);
        Assert(!UnderworldUnlockRules.IsUnlocked(alive), "The Underworld must remain locked before Nowhere King defeat.");

        var defeated = DarkThroneEncounterTransitions.Defeat(alive);
        Assert(UnderworldUnlockRules.IsUnlocked(defeated), "Nowhere King defeat must unlock Underworld eligibility.");

        return assertions;
    }

    private static void AssertThrows(
        Action<bool, string> assert,
        Action action,
        string message)
    {
        var threw = false;
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        assert(threw, message);
    }

    private static UnderworldDefinitionSet CreateFoundation(bool reverseInputs = false)
    {
        var biomes = new[]
        {
            new UnderworldBiomeDefinition(Biome("fungal_forest"), "Fungal Forest", Boss("first_bloom")),
            new UnderworldBiomeDefinition(Biome("blackwater_deep"), "Blackwater Deep", Boss("blackwater_maw")),
            new UnderworldBiomeDefinition(Biome("sulfurous_wastes"), "Sulfurous Wastes", Boss("furnace_heart")),
            new UnderworldBiomeDefinition(Biome("frozen_caverns"), "Frozen Caverns", Boss("white_silence")),
            new UnderworldBiomeDefinition(Biome("fracture_zones"), "Fracture Zones", Boss("rift_titan")),
            new UnderworldBiomeDefinition(Biome("great_decay"), "The Great Decay", Boss("carrion_crown")),
        };

        var bosses = new[]
        {
            BossDefinition("first_bloom", "fungal_forest", "bloom", "SporeBloom", "spore_communion"),
            BossDefinition("blackwater_maw", "blackwater_deep", "tide", "BlackwaterMaw", "deep_current", "first_bloom"),
            BossDefinition("furnace_heart", "sulfurous_wastes", "cinder", "FurnaceHeart", "furnace_blood", "blackwater_maw"),
            BossDefinition("white_silence", "frozen_caverns", "rime", "WhiteSilence", "rimebound", "blackwater_maw"),
            BossDefinition(
                "rift_titan",
                "fracture_zones",
                "fracture",
                "RiftTitan",
                "stone_anchor",
                "furnace_heart",
                "white_silence"),
            BossDefinition("carrion_crown", "great_decay", "decay", "CarrionCrown", "defiant_flesh", "rift_titan"),
        };

        var deepstones = new[]
        {
            Stone("bloom", "first_bloom", "spore_communion"),
            Stone("tide", "blackwater_maw", "deep_current"),
            Stone("cinder", "furnace_heart", "furnace_blood"),
            Stone("rime", "white_silence", "rimebound"),
            Stone("fracture", "rift_titan", "stone_anchor"),
            Stone("decay", "carrion_crown", "defiant_flesh"),
        };

        if (reverseInputs)
        {
            Array.Reverse(biomes);
            Array.Reverse(bosses);
            Array.Reverse(deepstones);

            var riftIndex = Array.FindIndex(bosses, value => value.Id == Boss("rift_titan"));
            var reversedPrerequisites = bosses[riftIndex].PrerequisiteBossIds.Reverse().ToArray();
            bosses[riftIndex] = bosses[riftIndex] with
            {
                PrerequisiteBossIds = Array.AsReadOnly(reversedPrerequisites),
            };
        }

        return UnderworldDefinitionValidator.ValidateAndFreeze(
            UnderworldDefinitionValidator.CurrentSchemaVersion,
            biomes,
            bosses,
            deepstones);
    }

    private static UnderworldBossDefinition BossDefinition(
        string id,
        string biome,
        string stone,
        string trophySuffix,
        string boon,
        params string[] prerequisites)
    {
        return new UnderworldBossDefinition(
            Boss(id),
            Biome(biome),
            "magenheim.underworld.location." + id,
            StoneId(stone),
            "Magenheim_Underworld_Trophy_" + trophySuffix,
            Boon(boon),
            Array.AsReadOnly(prerequisites.Select(Boss).ToArray()));
    }

    private static UnderworldDeepstoneDefinition Stone(string id, string boss, string boon)
        => new(StoneId(id), Boss(boss), Boon(boon));

    private static string Biome(string id) => "magenheim.underworld.biome." + id;
    private static string Boss(string id) => "magenheim.underworld.boss." + id;
    private static string StoneId(string id) => "magenheim.underworld.deepstone." + id;
    private static string Boon(string id) => "magenheim.underworld.boon." + id;
}
