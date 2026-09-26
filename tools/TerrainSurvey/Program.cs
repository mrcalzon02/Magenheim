using System.Globalization;
using System.Text.Json;
using Magenheim.Core.Underworld;

var output = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts/terrain-survey.json");
var seed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 12345;
var domain = UnderworldInstanceTerrainDomain.CreateDefault();
var samples = new List<object>();
var heights = Enum.GetValues<UnderworldTerrainBiome>().ToDictionary(b => b, _ => new List<double>());
for (var z = -8000; z <= 8000; z += 80)
for (var x = -8000; x <= 8000; x += 80)
{
    var terrain = UnderworldTerrainLifecycle.Evaluate(domain,
        new(x, 0, z, 30, 0, UnderworldTerrainNoise.Fractal01(seed, x, z)), seed);
    if (!terrain.Admitted) continue;
    var monument = UnderworldMonumentalLandforms.HeightAt(domain, seed, x, z) > 0;
    samples.Add(new { x, z, biome = terrain.Biome.ToString(), height = terrain.Height, monument });
    if (!monument) heights[terrain.Biome].Add(terrain.Height);
}
var summary = heights.Select(pair =>
{
    pair.Value.Sort();
    return new { biome = pair.Key.ToString(), count = pair.Value.Count,
        min = pair.Value[0], max = pair.Value[^1],
        p05 = pair.Value[(int)((pair.Value.Count-1)*.05)],
        p95 = pair.Value[(int)((pair.Value.Count-1)*.95)] };
}).ToArray();
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new { seed, spacing = 80, summary, samples }));
Console.WriteLine(JsonSerializer.Serialize(summary));
