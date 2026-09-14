using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Magenheim.Core
{
    public sealed class RefinementStep
    {
        public string Source { get; private set; }
        public string Target { get; private set; }
        public double BaseFailure { get; private set; }
        public int Shards { get; private set; }
        public int StationLevel { get; private set; }
        public double Experience { get; private set; }
        internal RefinementStep(string source, string target, double failure, int shards, int level, double xp)
        { Source = source; Target = target; BaseFailure = failure; Shards = shards; StationLevel = level; Experience = xp; }
    }

    // Immutable after validation: callers cannot change the balance behind a published hash.
    public sealed class DefinitionSnapshot
    {
        public string Hash { get; private set; }
        public double MaximumFailureReduction { get; private set; }
        public int ShardsRequired { get; private set; }
        public ReadOnlyCollection<string> Elements { get; private set; }
        public ReadOnlyCollection<RefinementStep> Steps { get; private set; }
        public ReadOnlyCollection<GeodeDefinition> Geodes { get; private set; }
        internal DefinitionSnapshot(string hash, double reduction, int shards, string[] elements, RefinementStep[] steps, GeodeDefinition[] geodes)
        { Hash = hash; MaximumFailureReduction = reduction; ShardsRequired = shards;
          Elements = Array.AsReadOnly(elements); Steps = Array.AsReadOnly(steps); Geodes = Array.AsReadOnly(geodes); }
        public RefinementStep Step(string source)
        {
            var step = Steps.FirstOrDefault(x => x.Source == source);
            if (step == null) throw new ArgumentException("Tier has no refinement transition: " + source);
            return step;
        }
    }

    public static class DefinitionLoader
    {
        private static readonly string[] TierOrder = { "rough", "simple", "refined", "advanced", "master" };
        private static readonly string[] ElementIds = { "earth", "fire", "frost", "storm", "venom", "radiance", "seidr", "spirit" };

        public static DefinitionSnapshot Load(string path)
        {
            try { return Parse(File.ReadAllText(path)); }
            catch (Exception ex) { throw new InvalidDataException(path + ": " + ex.Message, ex); }
        }

        public static DefinitionSnapshot Parse(string json)
        {
            var root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            Keys(root, "schemaVersion", "elements", "maximumFailureReduction", "shardsRequiredForSimpleCrystal", "refinement", "geodes");
            Integer(root, "schemaVersion", 2, 2);
            var reduction = Number(root, "maximumFailureReduction", 0.5, 1);
            var shards = Integer(root, "shardsRequiredForSimpleCrystal", 1, 1000);
            var elementArray = root["elements"] as JArray;
            if (elementArray == null || elementArray.Count == 0) throw new InvalidDataException("elements: nonempty array required");
            var elements = new List<string>();
            foreach (var token in elementArray)
            {
                if (token.Type != JTokenType.String || !ElementIds.Contains((string)token))
                    throw new InvalidDataException(token.Path + ": unknown element ID");
                elements.Add((string)token);
            }
            if (elements.Distinct().Count() != elements.Count) throw new InvalidDataException("elements: duplicate ID");
            var rows = root["refinement"] as JArray;
            if (rows == null || rows.Count != 4) throw new InvalidDataException("refinement: exactly four ordered transitions required");
            var steps = new List<RefinementStep>();
            for (int i = 0; i < 4; i++)
            {
                var row = rows[i] as JObject;
                if (row == null) throw new InvalidDataException("refinement[" + i + "]: object required");
                Keys(row, "source", "target", "baseFailure", "failureShards", "stationLevel", "experience");
                if ((string)row["source"] != TierOrder[i] || (string)row["target"] != TierOrder[i + 1])
                    throw new InvalidDataException(row.Path + ": invalid source/target progression");
                steps.Add(new RefinementStep(TierOrder[i], TierOrder[i + 1], Number(row, "baseFailure", 0, 1),
                    Integer(row, "failureShards", 1, 1000), Integer(row, "stationLevel", i + 1, i + 1), Number(row, "experience", 0, 1000)));
            }
            var geodes = GeodeDefinition.Parse(root["geodes"], elements);
            var canonical = Canonical(root).ToString(Formatting.None);
            string hash;
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", "").ToLowerInvariant();
            return new DefinitionSnapshot(hash, reduction, shards, elements.ToArray(), steps.ToArray(), geodes);
        }

        private static JToken Canonical(JToken token)
        {
            var obj = token as JObject;
            if (obj != null) return new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => new JProperty(p.Name, Canonical(p.Value))));
            var array = token as JArray;
            if (array != null) return new JArray(array.Select(Canonical));
            // Normalize equivalent numeric spellings (e.g. 1 and 1.0).
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                return new JValue(Convert.ToDecimal(((JValue)token).Value, CultureInfo.InvariantCulture));
            return token.DeepClone();
        }
        internal static void Keys(JObject obj, params string[] names)
        {
            foreach (var name in names)
                if (obj[name] == null) throw new InvalidDataException(obj.Path + "." + name + ": missing field");
            foreach (var prop in obj.Properties())
                if (!names.Contains(prop.Name)) throw new InvalidDataException(prop.Path + ": unknown field");
        }
        internal static double Number(JObject obj, string name, double min, double max)
        {
            var token = obj[name];
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
                throw new InvalidDataException(token.Path + ": number required");
            double value = (double)token;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                throw new InvalidDataException(token.Path + ": outside allowed range [" + min + ", " + max + "]");
            return value;
        }
        internal static int Integer(JObject obj, string name, int min, int max)
        {
            double value = Number(obj, name, min, max);
            if (value != Math.Truncate(value)) throw new InvalidDataException(obj[name].Path + ": integer required");
            return (int)value;
        }
    }
}
