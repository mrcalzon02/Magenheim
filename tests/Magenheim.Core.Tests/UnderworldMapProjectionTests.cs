using System;
using Magenheim.Core.Underworld;

internal static class UnderworldMapProjectionTests
{
    public static int Run()
    {
        var n = 0;
        void A(bool ok, string message)
        {
            n++;
            if (!ok) throw new InvalidOperationException("Underworld map projection assertion " + n + " failed: " + message);
        }

        var domain = UnderworldInstanceTerrainDomain.CreateDefault();
        A(UnderworldMapProjection.UnderworldBiomeAt(domain, 12345, 0d, 0d) == UnderworldTerrainBiome.FungalForest,
            "native instance origin uses canonical Fungal biome");
        A(UnderworldMapProjection.CellIndex(8, 8, 3, 2) == 19,
            "exploration cells have deterministic row-major identity");

        var rejected = false;
        try
        {
            UnderworldMapProjection.UnderworldBiomeAt(domain, 12345, domain.RadiusMeters + 1d, 0d);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        A(rejected, "map biome authority must reject coordinates outside the native playable radius");
        return n;
    }
}
