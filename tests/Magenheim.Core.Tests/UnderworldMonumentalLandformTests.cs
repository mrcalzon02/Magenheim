using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Magenheim.Core.Underworld;

internal static class UnderworldMonumentalLandformTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("Monumental terrain: " + message);
        }
        var domain = UnderworldInstanceTerrainDomain.CreateDefault();
        var kinds = new HashSet<UnderworldLandformKind>();
        var count = 0;
        foreach (var seed in new[] { 0, 1, -1, 12345, 1675883973 })
        {
            var perSeed = 0;
            for (var z = -4; z < 4; z++)
            for (var x = -4; x < 4; x++)
            {
                if (!UnderworldMonumentalLandforms.TryGet(domain, seed, x, z, out var form)) continue;
                count++; perSeed++; kinds.Add(form.Kind);
                Assert(UnderworldMonumentalLandforms.TryGet(domain, seed, x, z, out var repeat) && repeat == form,
                    "seeded shape must be stable");
                Assert(form.Height >= 3200d && form.Height <= UnderworldSkyLighting.MaximumTerrainHeightMeters, "landmarks must rise for miles");
                Assert(Math.Sqrt(form.X * form.X + form.Z * form.Z) - form.ExtentMeters
                    >= domain.RadiusMeters * .27d, "complete footprint must avoid arrival basin");
                var summit = Sample(form.X, form.Z, seed);
                Assert(domain.Contains(form.X, summit.Height + 2d, form.Z), "summit and standing player must fit domain");
                Assert(summit.Height <= UnderworldSkyLighting.MaximumTerrainHeightMeters, "summits must end just above cavern haze");
                Assert(summit.Height > 3000d, "full lifecycle must not clamp monuments back to hills");
                var dx = Math.Cos(form.Rotation); var dz = Math.Sin(form.Rotation);
                var rise = 0d;
                for (var distance = 8d; distance < form.ExtentMeters; distance += 8d)
                    rise = Math.Max(rise, Math.Abs(
                        form.HeightAt(form.X + dx * (distance - 1d), form.Z + dz * (distance - 1d)) -
                        form.HeightAt(form.X + dx * (distance + 1d), form.Z + dz * (distance + 1d))));
                Assert(Math.Atan(rise / 2d) * 180d / Math.PI > (form.Kind == UnderworldLandformKind.Plateau ? 88d : 79d),
                    "cliff faces must read as walls, not mountain slopes");
                if (form.Kind == UnderworldLandformKind.Plateau)
                    Assert(form.HeightAt(form.X + dx * form.Radius * .6d, form.Z + dz * form.Radius * .6d) == form.Height,
                        "plateau must have a broad level crown");
                else
                    Assert(form.HeightAt(form.X + dx * 8d, form.Z + dz * 8d) < form.Height - 40d,
                        "spire must converge to a sharp apex");
                Assert(form.HeightAt(form.X + dx * (form.ExtentMeters + 1d), form.Z + dz * (form.ExtentMeters + 1d)) == 0d,
                    "cliff support must be compact");
                var grid = new UnderworldInstanceChunkGrid(domain);
                var key = grid.KeyAt(form.X, form.Z);
                var left = UnderworldInstanceChunkSampler.Sample(grid, key, seed, 30d);
                var right = UnderworldInstanceChunkSampler.Sample(grid, new(key.X + 1, key.Z), seed, 30d);
                for (var row = 0; row < left.VerticesPerEdge; row++)
                    Assert(left.Heights[row * left.VerticesPerEdge + left.VerticesPerEdge - 1] == right.Heights[row * right.VerticesPerEdge],
                        "monument chunks must share exact border heights");
            }
            Assert(perSeed >= 2 && perSeed <= 30, "landmarks must be present but occasional");
            var occupied = 0; var probes = 0;
            for (var z = -7900d; z < 8000d; z += 200d)
            for (var x = -7900d; x < 8000d; x += 200d)
            {
                if (!domain.Contains(x, 0, z)) continue;
                probes++;
                if (UnderworldMonumentalLandforms.HeightAt(domain, seed, x, z) > 0d) occupied++;
            }
            Assert(occupied > 0 && occupied < probes * .08d, "over 92% of the realm must retain ordinary biome relief");
        }
        Assert(count > 20 && kinds.Count == 2, "test seeds must exercise both landmark forms");
        var shortDomain = UnderworldInstanceTerrainDomain.ValidateAndFreeze(8000d, -256d, 1792d);
        Assert(UnderworldMonumentalLandforms.HeightAt(shortDomain, 12345, 5000, 5000) == 0d,
            "custom low ceilings must not receive unreachable peaks");
        var output = Environment.GetEnvironmentVariable("MAGENHEIM_TERRAIN_REVIEW");
        if (!string.IsNullOrWhiteSpace(output)) ExportReview(output!, domain);
        return assertions;
    }

    private static UnderworldTerrainResult Sample(double x, double z, int seed) =>
        UnderworldTerrainLifecycle.Evaluate(UnderworldInstanceTerrainDomain.CreateDefault(),
            new(x, 0, z, 30, 0, UnderworldTerrainNoise.Fractal01(seed, x, z)), seed);

    private static void ExportReview(string output, UnderworldInstanceTerrainDomain domain)
    {
        var scenes = new List<object>();
        var seen = new HashSet<UnderworldLandformKind>();
        for (var seed = 0; seed < 64 && seen.Count < 2; seed++)
        for (var z = -4; z < 4; z++)
        for (var x = -4; x < 4; x++)
        {
            if (!UnderworldMonumentalLandforms.TryGet(domain, seed, x, z, out var form) || Math.Sqrt(form.X * form.X + form.Z * form.Z) > 6200d || !seen.Add(form.Kind)) continue;
            var heights = new List<double>();
            var coarse = new List<double>();
            const int edge = 257;
            var minX = Math.Floor((form.X - 1024d) / 32d) * 32d;
            var minZ = Math.Floor((form.Z - 1024d) / 32d) * 32d;
            for (var iz = 0; iz < edge; iz++)
            for (var ix = 0; ix < edge; ix++) heights.Add(Sample(minX + ix * 8d, minZ + iz * 8d, seed).Height);
            for (var iz = 0; iz < 129; iz++)
            for (var ix = 0; ix < 129; ix++) coarse.Add(Sample(minX + ix * 16d, minZ + iz * 16d, seed).Height);
            scenes.Add(new { seed, kind = form.Kind.ToString(), form.Height, minX, minZ, heights, coarse, edge, spacing = 8 });
        }
        if (scenes.Count != 2) throw new InvalidOperationException("Review must include both landform kinds.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, JsonSerializer.Serialize(new { scenes }));
    }
}
