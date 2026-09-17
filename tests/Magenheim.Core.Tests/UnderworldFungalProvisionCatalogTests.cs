using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldFungalProvisionCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld fungal provision assertion {assertions} failed: {message}");
        }

        Assert(UnderworldFungalProvisionCatalog.PrefabNames.Count == 3, "The first Fungal Forest food package must expose three canonical provision identities.");
        Assert(UnderworldFungalProvisionCatalog.PrefabNames.Distinct(StringComparer.Ordinal).Count() == 3, "Canonical provision identities must be unique.");
        foreach (var prefab in UnderworldFungalProvisionCatalog.PrefabNames)
        {
            Assert(prefab.StartsWith("Magenheim_Underworld_Food_", StringComparison.Ordinal), "Eligible provisions must remain Magenheim-owned Underworld food identities.");
            Assert(UnderworldFungalProvisionCatalog.IsEligible(prefab), "Every canonical provision must be admitted.");
            Assert(Math.Abs(UnderworldFungalProvisionCatalog.EfficiencyMultiplier(prefab, 0.20f) - 1.20f) < 0.0001f, "Canonical provisions receive the configured efficiency bonus.");
        }

        Assert(!UnderworldFungalProvisionCatalog.IsEligible("Mushroom"), "Vanilla food must not be admitted by resemblance.");
        Assert(!UnderworldFungalProvisionCatalog.IsEligible("OtherMod_GlowcapStew"), "Third-party food must not be admitted by naming similarity.");
        Assert(!UnderworldFungalProvisionCatalog.IsEligible(null), "Missing identities fail closed.");
        Assert(UnderworldFungalProvisionCatalog.EfficiencyMultiplier("Mushroom", 0.50f) == 1f, "Foreign food always receives a neutral multiplier.");
        Assert(UnderworldFungalProvisionCatalog.EfficiencyMultiplier(UnderworldFungalProvisionCatalog.GlowcapStew, -1f) == 1f, "Negative configuration cannot penalize provisions.");
        Assert(UnderworldFungalProvisionCatalog.EfficiencyMultiplier(UnderworldFungalProvisionCatalog.GlowcapStew, 5f) == 1.50f, "Provision efficiency is capped at +50 percent.");
        Assert(UnderworldFungalProvisionCatalog.EfficiencyMultiplier(UnderworldFungalProvisionCatalog.GlowcapStew, float.NaN) == 1f, "Non-finite configuration fails neutral.");

        return assertions;
    }
}
