using System;
using Magenheim.Core.Underworld;

internal static class UnderworldDeepSigilDiscoveryTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Deep Sigil discovery assertion {assertions} failed: {message}");
        }

        const string biomeId = "magenheim.underworld.biome.fracture";
        const string bossId = "magenheim.underworld.boss.fracture";
        const string locationId = "magenheim.underworld.location.fracture";
        const string deepstoneId = "magenheim.underworld.deepstone.fracture";
        const string boonId = "magenheim.underworld.boon.fracture";

        var definitions = UnderworldDefinitionValidator.ValidateAndFreeze(
            UnderworldDefinitionValidator.CurrentSchemaVersion,
            new[] { new UnderworldBiomeDefinition(biomeId, "Fracture Zones", bossId) },
            new[] { new UnderworldBossDefinition(bossId, biomeId, locationId, deepstoneId, "Magenheim_Underworld_Trophy_Fracture", boonId, Array.Empty<string>()) },
            new[] { new UnderworldDeepstoneDefinition(deepstoneId, bossId, boonId) });

        var discovery = UnderworldDeepSigilDiscoveryResolver.Resolve(definitions, biomeId);
        Assert(discovery.BiomeId == biomeId, "Discovery must preserve canonical biome identity.");
        Assert(discovery.BossId == bossId, "Discovery must resolve the biome-owned boss identity.");
        Assert(discovery.UniqueLocationId == locationId, "Discovery must reveal the stable unique location identity, not manufacture coordinates.");

        var rejectedUnknown = false;
        try { UnderworldDeepSigilDiscoveryResolver.Resolve(definitions, "magenheim.underworld.biome.missing"); }
        catch (InvalidOperationException) { rejectedUnknown = true; }
        Assert(rejectedUnknown, "Unknown Sigil biomes must fail closed.");

        return assertions;
    }
}
