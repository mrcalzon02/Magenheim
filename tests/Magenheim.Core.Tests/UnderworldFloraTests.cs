using System;
using System.IO;
using System.Linq;
using Magenheim.Core.Definitions;
using Magenheim.Core.Underworld;
using Magenheim.Runtime.Definitions;
using Newtonsoft.Json.Linq;

internal static class UnderworldFloraTests
{
    internal static int Run()
    {
        var count = 0;
        void Check(bool ok) { count++; if (!ok) throw new InvalidOperationException($"Flora assertion {count} failed."); }
        void Reject(Action action)
        {
            var rejected = false;
            try { action(); } catch (InvalidOperationException) { rejected = true; }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected);
        }
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "foundation.json"));
        var baseline = MagenheimDefinitionLoader.LoadFromJson(json);
        var flora = baseline.UnderworldFlora!;
        Check(flora.Species.Count == 3);
        var glow = flora.Species.Single(x => x.Id.EndsWith("glowcap", StringComparison.Ordinal));
        var shelf = flora.Species.Single(x => x.Id.EndsWith("shelfwood", StringComparison.Ordinal));
        var sample = new UnderworldFloraTerrainSample(UnderworldLayer.Underworld, glow.BiomeId, true, 1, 0, false, false);
        bool Place(UnderworldFloraTerrainSample s) => UnderworldFloraPlacement.CanPlace(flora, glow.Id, s);
        Check(Place(sample));
        Check(!Place(sample with { Layer = UnderworldLayer.Surface }));
        Check(!Place(sample with { Layer = (UnderworldLayer)99 }));
        Check(!Place(sample with { BiomeId = "Meadows" }));
        Check(!Place(sample with { HasGround = false }));
        Check(!Place(sample with { IsObstructed = true }));
        Check(!Place(sample with { HeightAboveWaterMeters = 0 }));
        Check(!Place(sample with { HeightAboveWaterMeters = -1 }));
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Check(!Place(sample with { HeightAboveWaterMeters = invalid }));
            Check(!Place(sample with { SlopeDegrees = invalid }));
        }
        Check(Place(sample with { SlopeDegrees = glow.MaxSlopeDegrees }));
        Check(!Place(sample with { SlopeDegrees = glow.MaxSlopeDegrees + .01 }));
        Check(!Place(sample with { SlopeDegrees = -1 }));
        Check(!UnderworldFloraPlacement.CanPlace(flora, "unknown", sample));
        Check(!UnderworldFloraPlacement.CanPlace(flora, shelf.Id, sample));
        Check(UnderworldFloraPlacement.CanPlace(flora, shelf.Id, sample with { HasExposedRockFace = true }));
        Check(new UnderworldFloraDefinitionSet(1, flora.Species.Reverse()).Fingerprint == flora.Fingerprint);
        var mutable = flora.Species.ToArray();
        var snapshot = new UnderworldFloraDefinitionSet(1, mutable);
        mutable[0] = mutable[0] with { MaxHeightMeters = 99 };
        Check(snapshot.Species[0].MaxHeightMeters != 99);
        Check(new UnderworldFloraDefinitionSet(1, mutable).Fingerprint != flora.Fingerprint);
        Reject(() => new UnderworldFloraDefinitionSet(2, flora.Species));
        Reject(() => new UnderworldFloraDefinitionSet(1, flora.Species.Concat(new[] { glow })));
        Reject(() => new UnderworldFloraDefinitionSet(1, new[] { glow with { Id = "foreign.tree" } }));
        Reject(() => new UnderworldFloraDefinitionSet(1, new[] { glow with { MaxHeightMeters = 1 } }));
        Reject(() => new UnderworldFloraDefinitionSet(1, new[] { glow with { MaxSlopeDegrees = 91 } }));
        var root = JObject.Parse(json);
        root["underworldFlora"]!["species"]![0]!["MaxHeightMeters"] = 9;
        Check(MagenheimDefinitionLoader.LoadFromJson(root.ToString()).Fingerprint != baseline.Fingerprint);
        root["underworldFlora"]!["species"]![0]!["BiomeId"] = "magenheim.underworld.biome.unknown";
        Reject(() => MagenheimDefinitionLoader.LoadFromJson(root.ToString()));
        root = JObject.Parse(json);
        ((JObject)root["underworldFlora"]!["species"]![0]!).Remove("RequiresRockFace");
        Reject(() => MagenheimDefinitionLoader.LoadFromJson(root.ToString()));
        return count;
    }
}
