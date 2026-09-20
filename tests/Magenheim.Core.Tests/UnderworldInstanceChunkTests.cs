using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldInstanceChunkTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld chunk assertion {assertions} failed: {message}");
        }

        var domain = UnderworldInstanceTerrainDomain.CreateDefault();
        var grid = new UnderworldInstanceChunkGrid(domain);
        Assert(grid.KeyAt(0d, 0d) == new UnderworldInstanceChunkKey(0, 0), "Origin belongs to native chunk 0,0.");
        Assert(grid.KeyAt(-0.01d, -0.01d) == new UnderworldInstanceChunkKey(-1, -1), "Negative native coordinates floor into the adjacent chunk.");
        Assert(grid.VerticesPerEdge == 33, "Default 64m chunk at 2m spacing has 33 vertices per edge.");
        Assert(grid.IntersectsPlayableDomain(new UnderworldInstanceChunkKey(0, 0)), "Origin chunk intersects playable domain.");
        Assert(!grid.IntersectsPlayableDomain(new UnderworldInstanceChunkKey(1000, 1000)), "Remote chunk is rejected without Surface-world semantics.");

        var neighborhood = grid.EnumerateSquare(new UnderworldInstanceChunkKey(0, 0), 1);
        Assert(neighborhood.Count == 9, "Interior streaming neighborhood contains nine chunks at radius one.");

        var first = UnderworldInstanceChunkSampler.Sample(grid, new UnderworldInstanceChunkKey(0, 0), 12345, 30d);
        var repeat = UnderworldInstanceChunkSampler.Sample(grid, new UnderworldInstanceChunkKey(0, 0), 12345, 30d);
        Assert(first.Heights.Length == 1089 && first.Biomes.Length == 1089 && first.Admitted.Length == 1089, "Chunk payload dimensions match grid geometry.");
        Assert(first.Heights.SequenceEqual(repeat.Heights), "Chunk terrain is deterministic for instance seed and chunk key.");
        Assert(first.Biomes.SequenceEqual(repeat.Biomes), "Chunk biome materialization is deterministic.");
        Assert(typeof(UnderworldInstanceChunkKey).GetProperty("HostCenterX") is null, "Chunk keys cannot encode host placement.");
        Assert(typeof(UnderworldInstanceChunkGrid).GetProperty("SpatialDomain") is null, "Native chunk grid cannot depend on legacy spatial domain.");

        var placement = UnderworldStructurePlacementKey.For(new UnderworldInstanceChunkKey(-2, 7), 3);
        Assert(placement == "chunk:-2,7/slot:3", "Structure placement keys are canonical native chunk/slot identities.");
        Assert(placement == UnderworldStructurePlacementKey.For(new UnderworldInstanceChunkKey(-2, 7), 3), "Structure placement keys are deterministic.");
        Assert(placement != UnderworldStructurePlacementKey.For(new UnderworldInstanceChunkKey(-2, 7), 4), "Different slots cannot alias one generated-object record.");
        Assert(placement != UnderworldStructurePlacementKey.For(new UnderworldInstanceChunkKey(-1, 7), 3), "Different native chunks cannot alias one generated-object record.");
        try
        {
            UnderworldStructurePlacementKey.For(new UnderworldInstanceChunkKey(0, 0), -1);
            Assert(false, "Negative structure slots must be rejected.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert(true, "Negative structure slots are rejected.");
        }
        return assertions;
    }
}
