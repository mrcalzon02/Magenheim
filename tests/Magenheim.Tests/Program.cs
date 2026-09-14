using System;
using System.IO;
using System.Linq;
using Magenheim.Core;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static int assertions;
    private static void Check(bool condition, string message)
    { assertions++; if (!condition) throw new Exception(message); }
    private static void Near(double actual, double expected, string message)
    { Check(Math.Abs(actual - expected) < 0.00000001, message); }
    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; }
        catch (InvalidOperationException) { rejected = true; }
        catch (InvalidDataException) { rejected = true; }
        catch (Newtonsoft.Json.JsonException) { rejected = true; }
        Check(rejected, message);
    }
    private static int Main(string[] args)
    {
        try
        {
            string json = File.ReadAllText(args[0]);
            var data = DefinitionLoader.Parse(json);
            double[,] expected = { { .10, .08125, .0625, .04375, .025 },
                { .20, .1625, .125, .0875, .05 }, { .30, .24375, .1875, .13125, .075 },
                { .40, .325, .25, .175, .10 } };
            for (int tier = 0; tier < 4; tier++)
            {
                var step = data.Steps[tier];
                for (int skill = 0; skill < 5; skill++)
                    Near(Refinement.FailureChance(step.BaseFailure, skill * 25, .75), expected[tier, skill], "Spec formula table");
                var fail = Refinement.Evaluate(data, "earth", step.Source, 4, 0, 0);
                Check(!fail.Success && fail.Shards == new[] {1,2,3,5}[tier] && fail.OutputTier == null, "Failure recovery");
                Check(fail.Element == "earth" && fail.Experience == step.Experience, "Failure preserves element and awards attempt XP");
                var pass = Refinement.Evaluate(data, "earth", step.Source, 4, 0, step.BaseFailure);
                Check(pass.Success && pass.OutputTier == step.Target && pass.Shards == 0, "Exact roll boundary succeeds");
                Reject(() => Refinement.Evaluate(data, "earth", step.Source, tier, 0, .99), "Station progression enforced");
                Near(Refinement.FailureChance(step.BaseFailure, 100, 1), 0, "Maximum mastery guarantees success");
            }
            var guaranteed = DefinitionLoader.Parse(json.Replace("0.75", "1.0"));
            Check(Refinement.Evaluate(guaranteed, "earth", "advanced", 4, 100, 0).Success, "Zero probability boundary");
            Reject(() => Refinement.Evaluate(data, "fire", "rough", 1, 0, .5), "Undefined element");
            Reject(() => Refinement.Evaluate(data, "earth", "master", 4, 0, .5), "No master refinement");
            foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -1, 101 })
                Reject(() => Refinement.FailureChance(.1, bad, .75), "Invalid skill rejected");
            foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -0.1, 1.0 })
                Reject(() => Refinement.Evaluate(data, "earth", "rough", 1, 0, bad), "Invalid random sample rejected");
            Reject(() => DefinitionLoader.Parse(json.Replace("0.75", "0.49")), "Invalid config range");
            Reject(() => DefinitionLoader.Parse(json.Replace("\"earth\"", "\"earth\",\"earth\"")), "Duplicate elements");
            Reject(() => DefinitionLoader.Parse(json.Replace("\"rough\"", "\"master\"")), "Invalid ladder");
            Reject(() => DefinitionLoader.Parse(json.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 3")), "Future schema rejected");
            Reject(() => DefinitionLoader.Parse(json.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 2,\"schemaVersion\": 2")), "Duplicate JSON fields");
            Reject(() => DefinitionLoader.Parse(json.Replace("\"elements\"", "\"elementz\"")), "Unknown/missing fields");
            var root = JObject.Parse(json);
            var reordered = new JObject(root.Properties().Reverse().Select(p => new JProperty(p.Name, p.Value.DeepClone())));
            Check(data.Hash == DefinitionLoader.Parse(reordered.ToString()).Hash, "Object property ordering does not change hash");
            Check(data.Hash == DefinitionLoader.Parse(json.Replace("0.10", "0.1000")).Hash, "Equivalent numeric spelling");
            Check(data.Hash != guaranteed.Hash, "Balance changes change hash");
            Reject(() => DefinitionLoader.Parse(json.Replace("0.50}", "null}")), "Null numeric field");
            Check(data.ShardsRequired == 5, "Default recombination cost");
            GeologyTests(json, data);
            Console.WriteLine("PASS: " + assertions + " assertions. Data SHA256=" + data.Hash);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
    }
}
