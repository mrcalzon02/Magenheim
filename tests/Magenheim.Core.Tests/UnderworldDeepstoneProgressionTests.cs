using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldDeepstoneProgressionTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld Deepstone assertion {assertions} failed: {message}");
        }

        var definitions = CreateDefinitions();
        var state = UnderworldDeepstoneProgression.CreateInitialState(definitions).ToArray();
        Assert(state.Length == 6 && state.All(value => !value.TrophyMounted && !value.BoonUnlocked), "Initial Conclave state must contain all six inactive Deepstones.");

        var bloom = Stone("bloom");
        var tide = Stone("tide");
        var cinder = Stone("cinder");
        var rime = Stone("rime");
        var fracture = Stone("fracture");
        var decay = Stone("decay");

        var blocked = UnderworldDeepstoneProgression.TryMountTrophy(definitions, state, tide, Trophy("BlackwaterMaw"));
        Assert(blocked.Status == UnderworldDeepstoneMountStatus.PrerequisiteMissing && !blocked.Changed, "Dependent stones must fail closed before prerequisite stones are mounted.");

        var wrong = UnderworldDeepstoneProgression.TryMountTrophy(definitions, state, bloom, Trophy("BlackwaterMaw"));
        Assert(wrong.Status == UnderworldDeepstoneMountStatus.WrongTrophy && !wrong.Changed, "A Deepstone must reject another boss trophy.");

        var missingInventory = UnderworldDeepstoneProgression.PlanMountTrophy(definitions, state, bloom, Trophy("FirstBloom"), 0);
        Assert(missingInventory.Status == UnderworldDeepstoneTransactionStatus.MissingTrophy && !missingInventory.Ready && missingInventory.TrophyConsumeCount == 0, "A valid trophy identity without authoritative inventory possession must not produce a consumable transaction.");
        Assert(missingInventory.ResultingState is { TrophyMounted: true, BoonUnlocked: true }, "Rejected inventory execution may describe the prospective canonical state but must not authorize its persistence.");

        MountReady(Assert, definitions, state, bloom, Trophy("FirstBloom"), Boon("spore_communion"));

        var duplicatePlan = UnderworldDeepstoneProgression.PlanMountTrophy(definitions, state, bloom, Trophy("FirstBloom"), 1);
        Assert(duplicatePlan.Status == UnderworldDeepstoneTransactionStatus.AlreadyMounted && duplicatePlan.TrophyConsumeCount == 0, "Duplicate runtime requests must not consume another trophy.");

        MountReady(Assert, definitions, state, tide, Trophy("BlackwaterMaw"), Boon("deep_current"));
        MountReady(Assert, definitions, state, cinder, Trophy("FurnaceHeart"), Boon("furnace_blood"));

        var fractureBlocked = UnderworldDeepstoneProgression.PlanMountTrophy(definitions, state, fracture, Trophy("RiftTitan"), 1);
        Assert(fractureBlocked.Status == UnderworldDeepstoneTransactionStatus.PrerequisiteMissing && fractureBlocked.TrophyConsumeCount == 0, "Fracture must remain blocked until both Cinder and Rime branches are mounted.");

        MountReady(Assert, definitions, state, rime, Trophy("WhiteSilence"), Boon("rimebound"));
        MountReady(Assert, definitions, state, fracture, Trophy("RiftTitan"), Boon("stone_anchor"));
        MountReady(Assert, definitions, state, decay, Trophy("CarrionCrown"), Boon("defiant_flesh"));

        Assert(state.All(value => value.TrophyMounted && value.BoonUnlocked), "Completing the canonical progression must leave all six Deepstones mounted with all six Deep Boons unlocked.");
        Assert(state.Select(value => value.DeepBoonId).Distinct(StringComparer.Ordinal).Count() == 6, "Each completed Deepstone must retain its own canonical Deep Boon identity.");

        return assertions;
    }

    private static void MountReady(
        Action<bool, string> assert,
        UnderworldDefinitionSet definitions,
        UnderworldDeepstoneState[] state,
        string stoneId,
        string trophyPrefab,
        string expectedBoon)
    {
        var plan = UnderworldDeepstoneProgression.PlanMountTrophy(definitions, state, stoneId, trophyPrefab, 1);
        assert(plan.Ready && plan.TrophyConsumeCount == 1, $"{stoneId} must produce exactly one consumable trophy transaction when prerequisites are satisfied.");
        assert(plan.ResultingState is { TrophyMounted: true, BoonUnlocked: true }, $"{stoneId} must atomically couple trophy mount and boon unlock.");
        assert(plan.ResultingState!.DeepBoonId == expectedBoon, $"{stoneId} must unlock its canonical Deep Boon.");
        state[Array.FindIndex(state, value => value.DeepstoneId == stoneId)] = plan.ResultingState;
    }

    private static UnderworldDefinitionSet CreateDefinitions()
    {
        var bloom = BossDefinition("first_bloom", "fungal_forest", "bloom", "FirstBloom", "spore_communion");
        var tide = BossDefinition("blackwater_maw", "blackwater_deep", "tide", "BlackwaterMaw", "deep_current", bloom.Id);
        var cinder = BossDefinition("furnace_heart", "sulfurous_wastes", "cinder", "FurnaceHeart", "furnace_blood", tide.Id);
        var rime = BossDefinition("white_silence", "frozen_caverns", "rime", "WhiteSilence", "rimebound", tide.Id);
        var fracture = BossDefinition("rift_titan", "fracture_zones", "fracture", "RiftTitan", "stone_anchor", cinder.Id, rime.Id);
        var decay = BossDefinition("carrion_crown", "great_decay", "decay", "CarrionCrown", "defiant_flesh", fracture.Id);
        var bosses = new[] { bloom, tide, cinder, rime, fracture, decay };

        return UnderworldDefinitionValidator.ValidateAndFreeze(
            UnderworldDefinitionValidator.CurrentSchemaVersion,
            new[] {
                new UnderworldBiomeDefinition(bloom.BiomeId, "Fungal Forest", bloom.Id),
                new UnderworldBiomeDefinition(tide.BiomeId, "Blackwater Deep", tide.Id),
                new UnderworldBiomeDefinition(cinder.BiomeId, "Sulfurous Wastes", cinder.Id),
                new UnderworldBiomeDefinition(rime.BiomeId, "Frozen Caverns", rime.Id),
                new UnderworldBiomeDefinition(fracture.BiomeId, "Fracture Zones", fracture.Id),
                new UnderworldBiomeDefinition(decay.BiomeId, "The Great Decay", decay.Id),
            },
            bosses,
            bosses.Select(value => new UnderworldDeepstoneDefinition(value.DeepstoneSlotId, value.Id, value.DeepBoonId)).ToArray());
    }

    private static UnderworldBossDefinition BossDefinition(
        string id,
        string biome,
        string stone,
        string trophySuffix,
        string boon,
        params string[] prerequisites)
        => new(
            Boss(id),
            Biome(biome),
            "magenheim.underworld.location." + id,
            Stone(stone),
            Trophy(trophySuffix),
            Boon(boon),
            prerequisites);

    private static string Biome(string id) => "magenheim.underworld.biome." + id;
    private static string Boss(string id) => "magenheim.underworld.boss." + id;
    private static string Stone(string id) => "magenheim.underworld.deepstone." + id;
    private static string Boon(string id) => "magenheim.underworld.boon." + id;
    private static string Trophy(string suffix) => "Magenheim_Underworld_Trophy_" + suffix;
}
