using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static Func<double> Rolls(params double[] samples)
    {
        var queue = new Queue<double>(samples);
        return () => queue.Dequeue();
    }
    private static string Edit(string json, string path, JToken value)
    {
        var root = JObject.Parse(json);
        root.SelectToken(path).Replace(value);
        return root.ToString();
    }
    private static void GeologyTests(string json, DefinitionSnapshot data)
    {
        var one = CrystalCracking.Evaluate(data, "meadows", 1, Rolls(.35, .10, 0));
        Check(one.Elements.Count == 1 && one.Elements[0] == "earth" && one.OutputTier == "rough", "Guaranteed Rough Earth at exact bonus boundaries");
        Check(one.ConsumedCount == 1 && one.ConsumedGeode == "meadows", "Exactly one intact geode cost");
        Near(one.Experience, .15, "Cracking XP from data");
        Check(CrystalCracking.Evaluate(data, "meadows", 1, Rolls(.34, .10, 0, 0)).Elements.Count == 2, "Second bonus only");
        Check(CrystalCracking.Evaluate(data, "meadows", 1, Rolls(.35, .09, 0, 0)).Elements.Count == 2, "Third bonus independent of second");
        Check(CrystalCracking.Evaluate(data, "meadows", 1, Rolls(.34, .09, 0, 0, 0)).Elements.Count == 3, "Both bonuses");
        int calls = 0;
        Func<double> tracked = () => { calls++; return .5; };
        Reject(() => CrystalCracking.Evaluate(data, "meadows", 0, tracked), "Cracking needs workstation");
        Reject(() => CrystalCracking.Evaluate(data, "missing", 1, tracked), "Unknown geode rejected");
        Check(calls == 0, "Invalid requests do not draw randomness");
        foreach (double bad in new[] { -0.01, 1.0, double.NaN, double.PositiveInfinity })
        {
            Reject(() => CrystalCracking.Evaluate(data, "meadows", 1, Rolls(bad)), "Invalid bonus roll rejected");
            Reject(() => WeightedRoll.Select(data.Geodes[0], bad), "Invalid weighted roll rejected");
        }
        // Two-element fixture exercises the generic sampler, without enabling extra shipped content.
        var mixedRoot = JObject.Parse(json);
        mixedRoot["elements"] = new JArray("earth", "frost", "fire");
        mixedRoot["geodes"][0]["weights"] = JArray.Parse("[{\"element\":\"fire\",\"weight\":0},{\"element\":\"earth\",\"weight\":60},{\"element\":\"frost\",\"weight\":40}]");
        var mixed = DefinitionLoader.Parse(mixedRoot.ToString());
        Check(WeightedRoll.Select(mixed.Geodes[0], 0) == "earth", "Zero-weight leading entry cannot win");
        Check(WeightedRoll.Select(mixed.Geodes[0], .599999) == "earth", "Below weight boundary");
        Check(WeightedRoll.Select(mixed.Geodes[0], .6) == "frost", "Exact weight boundary");
        Check(WeightedRoll.Select(mixed.Geodes[0], .9999999999999999) == "frost", "Last interval");
        var diverse = CrystalCracking.Evaluate(mixed, "meadows", 1, Rolls(0, 0, 0, .6, 0));
        Check(diverse.Elements.SequenceEqual(new[] { "earth", "frost", "earth" }), "Each crystal is rolled independently");
        var noBonus = DefinitionLoader.Parse(Edit(Edit(json, "geodes[0].secondCrystalChance", 0), "geodes[0].thirdCrystalChance", 0));
        Check(CrystalCracking.Evaluate(noBonus, "meadows", 1, Rolls(0, 0, 0)).Elements.Count == 1, "Zero bonus probability");
        var allBonus = DefinitionLoader.Parse(Edit(Edit(json, "geodes[0].secondCrystalChance", 1), "geodes[0].thirdCrystalChance", 1));
        Check(CrystalCracking.Evaluate(allBonus, "meadows", 1, Rolls(.999, .999, 0, 0, 0)).Elements.Count == 3, "Certain bonus probability");
        var rng = new Random(78231);
        int earth = 0;
        const int trials = 100000;
        for (int i = 0; i < trials; i++) if (WeightedRoll.Select(mixed.Geodes[0], rng.NextDouble()) == "earth") earth++;
        Check(Math.Abs((double)earth / trials - .6) < .01, "100,000 weighted rolls within one percentage point");
        var counts = new int[4];
        for (int i = 0; i < trials; i++) counts[CrystalCracking.Evaluate(data, "meadows", 1, rng.NextDouble).Elements.Count]++;
        // P(1)=.65*.90, P(3)=.35*.10; the remaining .38 gives two crystals.
        Check(Math.Abs((double)counts[1] / trials - .585) < .01, "One-crystal distribution");
        Check(Math.Abs((double)counts[2] / trials - .380) < .01, "Two-crystal distribution");
        Check(Math.Abs((double)counts[3] / trials - .035) < .005, "Three-crystal distribution");
        Console.WriteLine("Simulation: Earth=" + earth + "/100000; cracking yields 1/2/3=" + counts[1] + "/" + counts[2] + "/" + counts[3]);
        foreach (int available in new[] { 5, 6, int.MaxValue })
        {
            var result = ShardRecovery.Evaluate(data, "earth", 1, available);
            Check(result.Element == "earth" && result.ConsumedShards == 5 && result.OutputTier == "simple" && result.OutputCount == 1,
                "Recombination consumes exactly one configured batch");
        }
        Reject(() => ShardRecovery.Evaluate(data, "earth", 1, 4), "Insufficient shards");
        Reject(() => ShardRecovery.Evaluate(data, "earth", 1, -1), "Negative shard count");
        Reject(() => ShardRecovery.Evaluate(data, "earth", 0, 5), "Recombination station gate");
        Reject(() => ShardRecovery.Evaluate(data, "frost", 1, 5), "Unregistered shard family");
        var expensive = DefinitionLoader.Parse(Edit(json, "shardsRequiredForSimpleCrystal", 7));
        Reject(() => ShardRecovery.Evaluate(expensive, "earth", 1, 6), "Configured shard price enforced");
        Check(ShardRecovery.Evaluate(expensive, "earth", 1, 7).ConsumedShards == 7, "Configured shard price returned");
        foreach (var path in new[] { "geodes[0].secondCrystalChance", "geodes[0].thirdCrystalChance" })
        {
            Reject(() => DefinitionLoader.Parse(Edit(json, path, -0.01)), "Negative bonus probability");
            Reject(() => DefinitionLoader.Parse(Edit(json, path, 1.01)), "Excessive bonus probability");
            Check(DefinitionLoader.Parse(Edit(json, path, .2)).Hash != data.Hash, "Yield configuration included in hash");
        }
        foreach (var weight in new[] { -1.0, 0, .5, 1000001 })
            Reject(() => DefinitionLoader.Parse(Edit(json, "geodes[0].weights[0].weight", weight)), "Invalid weight/zero total");
        Reject(() => DefinitionLoader.Parse(Edit(json, "geodes[0].weights[0].element", "frost")), "Undefined weight reference");
        Reject(() => DefinitionLoader.Parse(Edit(json, "geodes[0].weights", new JArray())), "Empty weights");
        Reject(() => DefinitionLoader.Parse(Edit(json, "geodes", new JArray())), "Empty geodes");
        Reject(() => DefinitionLoader.Parse(Edit(json, "geodes[0].id", "ocean")), "Unknown biome");
        Reject(() => DefinitionLoader.Parse(Edit(json, "geodes[0].experience", JValue.CreateNull())), "Null geode XP");
        Reject(() => DefinitionLoader.Parse(Edit(json, "schemaVersion", 1)), "Previous development schema fails explicitly");
        var duplicateGeodes = JObject.Parse(json);
        ((JArray)duplicateGeodes["geodes"]).Add(duplicateGeodes["geodes"][0].DeepClone());
        Reject(() => DefinitionLoader.Parse(duplicateGeodes.ToString()), "Duplicate geode ID");
        mixedRoot["geodes"][0]["weights"][2]["element"] = "earth";
        Reject(() => DefinitionLoader.Parse(mixedRoot.ToString()), "Duplicate element weight");
        Check(DefinitionLoader.Parse(Edit(json, "geodes[0].experience", .25)).Hash != data.Hash, "XP included in hash");
    }
}
