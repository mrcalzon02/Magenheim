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
        Assert(state.Length == 2 && state.All(value => !value.TrophyMounted && !value.BoonUnlocked), "Initial Conclave state must be inactive.");

        var tide = "magenheim.underworld.deepstone.tide";
        var bloom = "magenheim.underworld.deepstone.bloom";
        var blocked = UnderworldDeepstoneProgression.TryMountTrophy(definitions, state, tide, "Magenheim_Underworld_Trophy_BlackwaterMaw");
        Assert(blocked.Status == UnderworldDeepstoneMountStatus.PrerequisiteMissing && !blocked.Changed, "Dependent stones must fail closed before prerequisite stones are mounted.");

        var wrong = UnderworldDeepstoneProgression.TryMountTrophy(definitions, state, bloom, "Magenheim_Underworld_Trophy_BlackwaterMaw");
        Assert(wrong.Status == UnderworldDeepstoneMountStatus.WrongTrophy && !wrong.Changed, "A Deepstone must reject another boss trophy.");

        var bloomMounted = UnderworldDeepstoneProgression.TryMountTrophy(definitions, state, bloom, "Magenheim_Underworld_Trophy_FirstBloom");
        Assert(bloomMounted.Changed && bloomMounted.State!.TrophyMounted && bloomMounted.State.BoonUnlocked, "Correct trophy must atomically activate the stone and unlock its boon.");
        state[Array.FindIndex(state, value => value.DeepstoneId == bloom)] = bloomMounted.State!;

        var duplicate = UnderworldDeepstoneProgression.TryMountTrophy(definitions, state, bloom, "Magenheim_Underworld_Trophy_FirstBloom");
        Assert(duplicate.Status == UnderworldDeepstoneMountStatus.AlreadyMounted && !duplicate.Changed, "Mounted trophies must be idempotent.");

        var tideMounted = UnderworldDeepstoneProgression.TryMountTrophy(definitions, state, tide, "Magenheim_Underworld_Trophy_BlackwaterMaw");
        Assert(tideMounted.Changed && tideMounted.State!.DeepBoonId == "magenheim.underworld.boon.deep_current", "Completing prerequisites must admit the next canonical trophy and boon.");

        return assertions;
    }

    private static UnderworldDefinitionSet CreateDefinitions()
    {
        var bloomBoss = new UnderworldBossDefinition(
            "magenheim.underworld.boss.first_bloom", "magenheim.underworld.biome.fungal_forest",
            "magenheim.underworld.location.first_bloom", "magenheim.underworld.deepstone.bloom",
            "Magenheim_Underworld_Trophy_FirstBloom", "magenheim.underworld.boon.spore_communion", Array.Empty<string>());
        var tideBoss = new UnderworldBossDefinition(
            "magenheim.underworld.boss.blackwater_maw", "magenheim.underworld.biome.blackwater_deep",
            "magenheim.underworld.location.blackwater_maw", "magenheim.underworld.deepstone.tide",
            "Magenheim_Underworld_Trophy_BlackwaterMaw", "magenheim.underworld.boon.deep_current", new[] { bloomBoss.Id });
        return UnderworldDefinitionValidator.ValidateAndFreeze(
            UnderworldDefinitionValidator.CurrentSchemaVersion,
            new[] {
                new UnderworldBiomeDefinition(bloomBoss.BiomeId, "Fungal Forest", bloomBoss.Id),
                new UnderworldBiomeDefinition(tideBoss.BiomeId, "Blackwater Deep", tideBoss.Id),
            },
            new[] { bloomBoss, tideBoss },
            new[] {
                new UnderworldDeepstoneDefinition(bloomBoss.DeepstoneSlotId, bloomBoss.Id, bloomBoss.DeepBoonId),
                new UnderworldDeepstoneDefinition(tideBoss.DeepstoneSlotId, tideBoss.Id, tideBoss.DeepBoonId),
            });
    }
}
