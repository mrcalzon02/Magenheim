using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldFungalRefinementCatalogTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException("Underworld fungal refinement assertion " + assertions + " failed: " + message);
        }

        var all = UnderworldFungalRefinementCatalog.All;
        Assert(all.Count == 3, "Fungal Forest must expose the three planned refined materials.");
        Assert(all.Select(value => value.Prefab).Distinct(StringComparer.Ordinal).Count() == all.Count,
            "Refined material prefab identities must be unique.");
        Assert(all.All(value => value.OutputAmount > 0 && value.Costs.Count > 0),
            "Every refinement must have a positive output and consume at least one raw material.");
        Assert(all.SelectMany(value => value.Costs)
                .All(cost => UnderworldResourceCatalog.All.Any(resource =>
                    string.Equals(resource.Prefab, cost.Prefab, StringComparison.Ordinal))),
            "Fungal refinements may only consume canonical raw Underworld resources.");
        Assert(all.Single(value => value.Prefab == UnderworldFungalRefinementCatalog.WorldrootPlank)
                .Costs.Single().Prefab == "Magenheim_Underworld_Resource_WorldrootTimber",
            "Worldroot Plank must be refined from Worldroot Timber.");
        Assert(all.Single(value => value.Prefab == UnderworldFungalRefinementCatalog.SpireCord)
                .Costs.Single().Prefab == "Magenheim_Underworld_Resource_SpireFibre",
            "Spire Cord must be refined from Spire Fibre.");
        Assert(all.Single(value => value.Prefab == UnderworldFungalRefinementCatalog.CuredGlowcap)
                .Costs.Any(cost => cost.Prefab == "Magenheim_Underworld_Resource_GlowcapFlesh"),
            "Cured Glowcap must consume Glowcap Flesh.");

        return assertions;
    }
}
