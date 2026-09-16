using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldDeepBoonSelectionTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message) { assertions++; if (!condition) throw new InvalidOperationException($"Deep Boon selection assertion {assertions} failed: {message}"); }
        var definitions = CreateDefinitions();
        var world = UnderworldDeepstoneProgression.CreateInitialState(definitions).ToArray();
        var initial = UnderworldDeepBoonSelection.CreateInitialState();
        Assert(initial.SelectedDeepBoonId is null, "Players must begin without a selected Deep Boon.");

        var bloomBoon = Boon("spore_communion");
        var tideBoon = Boon("deep_current");
        var locked = UnderworldDeepBoonSelection.Select(definitions, world, initial, bloomBoon);
        Assert(locked.Status == UnderworldDeepBoonSelectionStatus.Locked && !locked.Changed, "A world-locked boon must not be selectable.");
        var unknown = UnderworldDeepBoonSelection.Select(definitions, world, initial, Boon("unknown"));
        Assert(unknown.Status == UnderworldDeepBoonSelectionStatus.UnknownBoon && !unknown.Changed, "Unknown boon identities must fail closed.");

        Unlock(world, Stone("bloom"));
        var selected = UnderworldDeepBoonSelection.Select(definitions, world, initial, bloomBoon);
        Assert(selected.Status == UnderworldDeepBoonSelectionStatus.Selected && selected.State.SelectedDeepBoonId == bloomBoon, "An unlocked boon must become the player's sole selected boon.");
        var duplicate = UnderworldDeepBoonSelection.Select(definitions, world, selected.State, bloomBoon);
        Assert(duplicate.Status == UnderworldDeepBoonSelectionStatus.AlreadySelected && !duplicate.Changed, "Selecting the active boon again must be idempotent.");

        Unlock(world, Stone("tide"));
        var switched = UnderworldDeepBoonSelection.Select(definitions, world, selected.State, tideBoon);
        Assert(switched.Changed && switched.State.SelectedDeepBoonId == tideBoon, "Selecting another unlocked boon must replace rather than stack with the previous selection.");
        var reconstructed = UnderworldDeepBoonSelection.Reconstruct(definitions, world, tideBoon);
        Assert(reconstructed == switched.State, "A valid per-player selection must reconstruct across reconnect.");

        var clear = UnderworldDeepBoonSelection.Clear(switched.State);
        Assert(clear.Status == UnderworldDeepBoonSelectionStatus.Cleared && clear.State.SelectedDeepBoonId is null, "Clearing must return the player to no active Deep Boon.");

        var rejectedLockedPersistence = false;
        try { _ = UnderworldDeepBoonSelection.Reconstruct(definitions, world, Boon("furnace_blood")); } catch (InvalidOperationException) { rejectedLockedPersistence = true; }
        Assert(rejectedLockedPersistence, "Persisted selection must fail closed when its world boon is not unlocked.");
        return assertions;
    }

    private static void Unlock(UnderworldDeepstoneState[] world, string stoneId)
    {
        var index = Array.FindIndex(world, value => value.DeepstoneId == stoneId);
        world[index] = world[index] with { TrophyMounted = true, BoonUnlocked = true };
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
        return UnderworldDefinitionValidator.ValidateAndFreeze(UnderworldDefinitionValidator.CurrentSchemaVersion,
            bosses.Select(value => new UnderworldBiomeDefinition(value.BiomeId, value.Id, value.Id)).ToArray(), bosses,
            bosses.Select(value => new UnderworldDeepstoneDefinition(value.DeepstoneSlotId, value.Id, value.DeepBoonId)).ToArray());
    }

    private static UnderworldBossDefinition BossDefinition(string id, string biome, string stone, string trophy, string boon, params string[] prerequisites)
        => new(Boss(id), Biome(biome), "magenheim.underworld.location." + id, Stone(stone), "Magenheim_Underworld_Trophy_" + trophy, Boon(boon), prerequisites);
    private static string Biome(string id) => "magenheim.underworld.biome." + id;
    private static string Boss(string id) => "magenheim.underworld.boss." + id;
    private static string Stone(string id) => "magenheim.underworld.deepstone." + id;
    private static string Boon(string id) => "magenheim.underworld.boon." + id;
}
