using System;
using System.IO;
using System.Linq;
using Magenheim.Core.Definitions;
using Magenheim.Core.Underworld;
using Magenheim.Runtime.Definitions;
using Newtonsoft.Json.Linq;

internal static class DefinitionLoaderTests
{
    internal static int Run()
    {
        var assertions = 0;
        void Assert(bool value, string message)
        {
            assertions++;
            if (!value) throw new InvalidOperationException(message);
        }
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "foundation.json"));
        var baseline = MagenheimDefinitionLoader.LoadFromJson(json);
        Assert(baseline.SchemaVersion == 5, "Shipped definition schema is current.");
        Assert(baseline.Underworld?.Bosses.Count == 6 && baseline.Underworld.Deepstones.Count == 6, "Shipped six-boss loop loads through canonical loader.");
        Assert(baseline.UnderworldArchitecture?.Pieces.Count == 11, "Shipped A0 architecture loads through canonical loader.");
        var overridden = MagenheimDefinitionOverrideApplier.Apply(baseline, Array.Empty<RefinementBalanceOverride>(), Array.Empty<GeodeBalanceOverride>());
        Assert(overridden.Fingerprint == baseline.Fingerprint, "Balance overrides preserve Underworld authority.");
        var architecture = baseline.UnderworldArchitecture!;
        var changed = MagenheimDefinitionValidator.ValidateAndFreeze(baseline.SchemaVersion, baseline.RefinementRules,
            baseline.Geodes, baseline.WorldgenCompatibility, baseline.SocketEffects, baseline.Underworld,
            architecture with { Pieces = architecture.Pieces.Select((piece, index) => index == 0
                ? piece with { Costs = piece.Costs.Select(cost => cost with { Amount = cost.Amount + 1 }).ToArray() } : piece).ToArray() }, baseline.UnderworldFlora);
        Assert(changed.Fingerprint != baseline.Fingerprint, "Changed build costs invalidate overall authority even with a stale component hash.");
        var forged = MagenheimDefinitionValidator.ValidateAndFreeze(baseline.SchemaVersion, baseline.RefinementRules,
            baseline.Geodes, baseline.WorldgenCompatibility, baseline.SocketEffects,
            baseline.Underworld! with { Fingerprint = "forged" }, architecture with { Fingerprint = "forged" }, baseline.UnderworldFlora);
        Assert(forged.Fingerprint == baseline.Fingerprint, "Supplied fingerprints cannot override actual data.");
        var reordered = JObject.Parse(json);
        foreach (var path in new[] { "underworld.biomes", "underworld.bosses", "underworld.deepstones", "underworldArchitecture.pieces", "underworldFlora.species" })
        {
            var array = (JArray)reordered.SelectToken(path)!;
            var reverse = array.Reverse().Select(value => value.DeepClone()).ToArray();
            array.Replace(new JArray(reverse));
        }
        Assert(MagenheimDefinitionLoader.LoadFromJson(reordered.ToString()).Fingerprint == baseline.Fingerprint, "Catalog ordering does not change peer authority.");
        void Reject(string invalid)
        {
            var rejected = false;
            try { MagenheimDefinitionLoader.LoadFromJson(invalid); }
            catch (Exception error) when (error is InvalidDataException || error is InvalidOperationException || error is ArgumentException) { rejected = true; }
            Assert(rejected, "Malformed definitions must fail before runtime mutation.");
        }
        Reject(""); Reject("{}"); Reject("{broken"); Reject(json.Replace("\"schemaVersion\":5", "\"schemaVersion\":4"));
        Reject("{\"schemaVersion\":5," + json.Substring(1));
        foreach (var path in new[] { "underworld", "underworld.biomes", "underworld.bosses[0].prerequisiteBossIds",
            "underworldArchitecture.pieces[0].Kind", "underworldArchitecture.pieces[0].Dimensions.WidthMeters" })
        {
            var invalid = JObject.Parse(json);
            invalid.SelectToken(path)!.Parent!.Remove();
            Reject(invalid.ToString());
        }
        var invalidReference = JObject.Parse(json);
        invalidReference["underworld"]!["bosses"]![0]!["biomeId"] = "magenheim.underworld.biome.missing";
        Reject(invalidReference.ToString());
        var unknown = JObject.Parse(json);
        unknown["underworld"]!["fingerprint"] = "untrusted";
        Reject(unknown.ToString());
        foreach (var path in new[] { "underworld.biomes[0].displayName", "underworldArchitecture.pieces[0].DisplayName" })
        {
            var invalid = JObject.Parse(json);
            invalid.SelectToken(path)!.Replace("name|injected\nrecord");
            Reject(invalid.ToString());
        }
        var changedBoon = JObject.Parse(json);
        changedBoon["underworld"]!["bosses"]![0]!["deepBoonId"] = "magenheim.underworld.boon.changed";
        changedBoon["underworld"]!["deepstones"]![0]!["deepBoonId"] = "magenheim.underworld.boon.changed";
        Assert(MagenheimDefinitionLoader.LoadFromJson(changedBoon.ToString()).Fingerprint != baseline.Fingerprint, "Deep Boon changes affect canonical peer authority.");
        var absent = JObject.Parse(json);
        absent["underworld"] = null; absent["underworldArchitecture"] = null; absent["underworldFlora"] = null;
        Assert(MagenheimDefinitionLoader.LoadFromJson(absent.ToString()).Fingerprint != baseline.Fingerprint, "Explicitly absent expansion authority differs from populated catalogs.");
        return assertions;
    }
}
