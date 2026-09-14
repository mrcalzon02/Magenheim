using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Magenheim.Core
{
    public sealed class ElementWeight
    {
        public string Element { get; private set; }
        public int Weight { get; private set; }
        internal ElementWeight(string element, int weight) { Element = element; Weight = weight; }
    }

    public sealed class GeodeDefinition
    {
        public string Id { get; private set; }
        public double SecondChance { get; private set; }
        public double ThirdChance { get; private set; }
        public double Experience { get; private set; }
        public ReadOnlyCollection<ElementWeight> Weights { get; private set; }
        private GeodeDefinition(string id, double second, double third, double xp, ElementWeight[] weights)
        { Id = id; SecondChance = second; ThirdChance = third; Experience = xp; Weights = Array.AsReadOnly(weights); }

        internal static GeodeDefinition[] Parse(JToken token, ICollection<string> elements)
        {
            var rows = token as JArray;
            if (rows == null || rows.Count == 0 || rows.Count > 8)
                throw new InvalidDataException("geodes: one to eight definitions required");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<GeodeDefinition>();
            var knownIds = new[] { "meadows", "black_forest", "swamp", "mountain", "plains", "mistlands", "ashlands", "deep_north" };
            foreach (var entry in rows)
            {
                var row = entry as JObject;
                if (row == null) throw new InvalidDataException(entry.Path + ": object required");
                DefinitionLoader.Keys(row, "id", "secondCrystalChance", "thirdCrystalChance", "experience", "weights");
                if (row["id"].Type != JTokenType.String || !knownIds.Contains((string)row["id"]))
                    throw new InvalidDataException(row.Path + ".id: unknown geode ID");
                string id = (string)row["id"];
                if (!ids.Add(id)) throw new InvalidDataException(row.Path + ".id: duplicate geode ID");
                var weights = row["weights"] as JArray;
                if (weights == null || weights.Count == 0 || weights.Count > elements.Count)
                    throw new InvalidDataException(row.Path + ".weights: nonempty array bounded by element count required");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var parsedWeights = new List<ElementWeight>();
                foreach (var weightToken in weights)
                {
                    var weight = weightToken as JObject;
                    if (weight == null) throw new InvalidDataException(weightToken.Path + ": object required");
                    DefinitionLoader.Keys(weight, "element", "weight");
                    if (weight["element"].Type != JTokenType.String || !elements.Contains((string)weight["element"]))
                        throw new InvalidDataException(weight.Path + ".element: undefined element");
                    string element = (string)weight["element"];
                    if (!seen.Add(element)) throw new InvalidDataException(weight.Path + ".element: duplicate weight entry");
                    // Integer weights bound the total and prevent positive entries vanishing through floating point addition.
                    parsedWeights.Add(new ElementWeight(element, DefinitionLoader.Integer(weight, "weight", 0, 1000000)));
                }
                if (parsedWeights.All(w => w.Weight == 0)) throw new InvalidDataException(row.Path + ".weights: positive total required");
                result.Add(new GeodeDefinition(id, DefinitionLoader.Number(row, "secondCrystalChance", 0, 1),
                    DefinitionLoader.Number(row, "thirdCrystalChance", 0, 1), DefinitionLoader.Number(row, "experience", 0, 1000), parsedWeights.ToArray()));
            }
            return result.ToArray();
        }
    }

    public static class WeightedRoll
    {
        public static string Select(GeodeDefinition geode, double roll)
        {
            if (geode == null) throw new ArgumentNullException("geode");
            Validate(roll);
            int total = geode.Weights.Sum(w => w.Weight);
            double threshold = roll * total;
            int cumulative = 0;
            foreach (var entry in geode.Weights)
            {
                cumulative += entry.Weight;
                if (entry.Weight > 0 && threshold < cumulative) return entry.Element;
            }
            // Multiplication may round the largest valid sample up to the total.
            return geode.Weights.Last(w => w.Weight > 0).Element;
        }
        internal static void Validate(double roll)
        {
            if (double.IsNaN(roll) || double.IsInfinity(roll) || roll < 0 || roll >= 1)
                throw new ArgumentOutOfRangeException("roll", "Random samples must be finite and in [0, 1)");
        }
    }

    public sealed class CrackingOutcome
    {
        public string ConsumedGeode { get; private set; }
        public int ConsumedCount { get { return 1; } }
        public string OutputTier { get { return "rough"; } }
        public ReadOnlyCollection<string> Elements { get; private set; }
        public double Experience { get; private set; }
        internal CrackingOutcome(string id, string[] elements, double xp)
        { ConsumedGeode = id; Elements = Array.AsReadOnly(elements); Experience = xp; }
    }

    public static class CrystalCracking
    {
        // The future authoritative transaction handler supplies fresh RNG, never a client-supplied roll.
        // This method only plans results; it cannot modify inventory or award experience.
        public static CrackingOutcome Evaluate(DefinitionSnapshot data, string geodeId, int stationLevel, Func<double> nextRoll)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (nextRoll == null) throw new ArgumentNullException("nextRoll");
            if (stationLevel < 1) throw new InvalidOperationException("Geologist's Workstation required");
            var geode = data.Geodes.FirstOrDefault(g => g.Id == geodeId);
            if (geode == null) throw new ArgumentException("Unknown geode: " + geodeId);
            double second = nextRoll(); WeightedRoll.Validate(second);
            double third = nextRoll(); WeightedRoll.Validate(third);
            var result = new List<string> { WeightedRoll.Select(geode, nextRoll()) };
            if (second < geode.SecondChance) result.Add(WeightedRoll.Select(geode, nextRoll()));
            // Independent of the second bonus: third may succeed even when second fails.
            if (third < geode.ThirdChance) result.Add(WeightedRoll.Select(geode, nextRoll()));
            return new CrackingOutcome(geode.Id, result.ToArray(), geode.Experience);
        }
    }

    public sealed class RecombinationOutcome
    {
        public string Element { get; private set; }
        public int ConsumedShards { get; private set; }
        public int OutputCount { get { return 1; } }
        public string OutputTier { get { return "simple"; } }
        internal RecombinationOutcome(string element, int cost) { Element = element; ConsumedShards = cost; }
    }

    public static class ShardRecovery
    {
        // availableMatchingShards must be counted for this element, not across all shard types.
        public static RecombinationOutcome Evaluate(DefinitionSnapshot data, string element, int stationLevel, int availableMatchingShards)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (!data.Elements.Contains(element)) throw new ArgumentException("Unknown element: " + element);
            if (stationLevel < 1) throw new InvalidOperationException("Geologist's Workstation required");
            if (availableMatchingShards < 0) throw new ArgumentOutOfRangeException("availableMatchingShards");
            if (availableMatchingShards < data.ShardsRequired) throw new InvalidOperationException("Insufficient matching shards");
            return new RecombinationOutcome(element, data.ShardsRequired);
        }
    }
}
