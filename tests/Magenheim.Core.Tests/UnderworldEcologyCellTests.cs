using System;
using Magenheim.Core.Underworld;
internal static class UnderworldEcologyCellTests
{
    public static int Run()
    {
        var checks = 0;
        void Assert(bool value) { checks++; if (!value) throw new InvalidOperationException("Ecology residency moved or evicted nearby scenery."); }
        foreach (var seed in new[] { 0, -123, int.MaxValue })
        for (var x = -220; x <= 220; x += 11)
        {
            var support = UnderworldEcologyCells.KeyAt(x, -x);
            var originalSeed = UnderworldEcologyCells.Seed(seed, support);
            // Cross the former 90m rebuild threshold in either direction while still standing
            // within a large rock's footprint. Its cell and placement sequence must survive.
            foreach (var movement in new[] { -95, -64, -1, 0, 1, 64, 95 })
            {
                var focus = UnderworldEcologyCells.KeyAt(x + movement, -x);
                Assert(UnderworldEcologyCells.Retain(support, focus));
                Assert(UnderworldEcologyCells.Seed(seed, support) == originalSeed);
            }
            Assert(!UnderworldEcologyCells.Retain(support, new(support.X + 5, support.Z)));
        }
        Assert(UnderworldEcologyCells.KeyAt(-.1, -.1) == new UnderworldInstanceChunkKey(-1,-1));
        Assert(UnderworldEcologyCells.KeyAt(64, 64) == new UnderworldInstanceChunkKey(1,1));
        return checks;
    }
}
