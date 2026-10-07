using System;
using System.IO;
using System.Linq;
using Magenheim.Core.TrueBlacksmithing;
using Magenheim.Runtime.TrueBlacksmithing;
using Newtonsoft.Json;

internal static class TrueBlacksmithingCatalogTests
{
    internal static int Run()
    {
        var checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
        void Reject(Action action, string message)
        {
            var rejected = false;
            try { action(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, message);
        }
        var directory = Path.Combine(AppContext.BaseDirectory, "true-blacksmithing");
        var catalog = ManufacturingDataLoader.Read<ManufacturingCatalog>(Path.Combine(directory, "vanilla-catalog.json"));
        var grammar = ManufacturingDataLoader.Read<ManufacturingGrammar>(Path.Combine(directory, "manufacturing-grammar.json"));
        Check(catalog.Recipes.Count > 300 && catalog.Stations.Count >= 10, "Authoritative extraction must span current vanilla progression.");
        Check(catalog.SourceHashes.Count >= 3 && catalog.SourceHashes.All(x => x.Value.Length == 64), "Capture must identify installed source assets.");
        Check(catalog.Recipes.Any(x => !x.Enabled && x.Disposition == ManufacturingDisposition.Exempt), "Disabled recipes must remain disabled/exempt.");
        var iron = catalog.Recipes.Single(x => x.Id == "Recipe_SwordIron");
        var ironCost = iron.Costs.Single(x => x.Prefab == "Iron");
        Check(ironCost.Amount == 20 && ironCost.QualityCosts.SequenceEqual(new[] { 20, 10, 20, 40 }), "Installed iron sword material costs and native quality-four boundary must remain exact.");
        Check(iron.CraftingStation == "forge" && iron.MinStationLevel == 2, "Native station requirements must be preserved.");
        var budget = new MaterialBudget { Amount = 1, AmountPerLevel = 3, UpgraderResource = true };
        Check(budget.GetNativeAmount(1) == 1 && budget.GetNativeAmount(4) == 13 && budget.GetNativeAmount(5) == 14 && budget.GetNativeAmount(6) == 16,
            "Special upgrader resources must retain their base amount and use native floor rounding.");
        var previous = ironCost.QualityCosts[3];
        ironCost.QualityCosts[3]++;
        Reject(catalog.Validate, "Incorrect quality budgets must reject the catalog.");
        ironCost.QualityCosts[3] = previous;
        catalog.Recipes.Add(iron);
        Reject(catalog.Validate, "Duplicate recipe identities must reject the catalog.");
        catalog.Recipes.RemoveAt(catalog.Recipes.Count - 1);
        var station = iron.CraftingStation;
        iron.CraftingStation = "missing-station";
        Reject(catalog.Validate, "Unresolved progression stations must reject the catalog.");
        iron.CraftingStation = station;
        var disabled = catalog.Recipes.First(x => !x.Enabled);
        disabled.Disposition = ManufacturingDisposition.Replace;
        Reject(catalog.Validate, "A disabled native asset must never be enabled by a classification error.");
        disabled.Disposition = ManufacturingDisposition.Exempt;
        var quench = grammar.Workshops.Single(x => x.Id == "quench");
        Check(quench.Interaction == ProcessInteraction.DirectInteraction, "Deterministic quenching should use direct interaction.");
        Check(!grammar.Routes.Single(x => x.Id == "wood-composite").Operations.Contains(WorkshopOperation.Quenching), "Wood composites must not inherit a metal treatment chain.");
        Check(!grammar.Routes.Single(x => x.Id == "cast-nonferrous").Operations.Contains(WorkshopOperation.Tempering), "Casting must not force ferrous tempering onto bronze.");
        quench.Operations.Clear();
        Reject(grammar.Validate, "Every physical transformation must have an owner.");
        quench.Operations.Add(WorkshopOperation.Quenching);
        grammar.Routes[0].Components.Add("unknown-component");
        Reject(grammar.Validate, "A route must not depend on an undefined component family.");
        grammar.Routes[0].Components.Remove("unknown-component");
        catalog.Validate(); grammar.Validate();
        var temporary = Path.GetTempFileName();
        try
        {
            var json = File.ReadAllText(Path.Combine(directory, "manufacturing-grammar.json"));
            foreach (var malformed in new[]
            {
                json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"schemaVersion\": 1"),
                json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"unknownField\": true"),
                json.Replace("\"DirectInteraction\"", "\"UnknownInteraction\""),
                json.Replace("\"DirectInteraction\"", "1"),
                json.Replace("\"interaction\":\"DirectInteraction\",", "")
            })
            {
                File.WriteAllText(temporary, malformed);
                var rejected = false;
                try { ManufacturingDataLoader.Read<ManufacturingGrammar>(temporary); }
                catch (JsonException) { rejected = true; }
                Check(rejected, "Malformed/ambiguous or incomplete authored manufacturing data must be rejected.");
            }
        }
        finally { File.Delete(temporary); }
        Console.WriteLine($"TrueBlacksmithingCatalogTests: {checks} checks; {catalog.Recipes.Count} native recipes and all workshop/family contracts passed.");
        return checks;
    }
}
