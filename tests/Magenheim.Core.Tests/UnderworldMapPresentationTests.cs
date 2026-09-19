using System;
using Magenheim.Core.Underworld;

internal static class UnderworldMapPresentationTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld map presentation assertion {assertions} failed: {message}");
        }

        var domain = UnderworldSpatialDomain.CreateDefault();
        // CreateUnderworldViewport(UnderworldSpatialDomainDefinition, ...) is obsolete; bridge the
        // same way its own (obsolete) implementation does internally. domain.RadiusMeters below is
        // unaffected -- both domain types expose it.
        var instanceDomain = UnderworldInstanceTerrainDomain.ValidateAndFreeze(
            domain.RadiusMeters, domain.LogicalMinY, domain.LogicalMaxY);
        var view = UnderworldMapPresentation.CreateUnderworldViewport(instanceDomain, 512, 512);
        Assert(view.Layer == MagenheimMapLayer.Underworld, "viewport must identify the logical Underworld tab");
        Assert(view.CenterX == 0d && view.CenterZ == 0d, "host offset must never enter map-space presentation");
        Assert(UnderworldMapPresentation.TryLogicalToCell(view, 0d, 0d, out var cx, out var cy) && cx == 256 && cy == 256,
            "logical origin must map to the exploration texture center");
        Assert(!UnderworldMapPresentation.TryLogicalToCell(view, domain.RadiusMeters + 1d, 0d, out _, out _),
            "points outside the playable circular map must be rejected");
        Assert(UnderworldMapPresentation.TryLogicalToCell(view, domain.RadiusMeters, 0d, out cx, out cy) && cx == 511,
            "positive map edge must clamp to the final texture cell");
        var logical = UnderworldMapPresentation.CellCenterToLogical(view, 256, 256);
        Assert(Math.Abs(logical.X) <= domain.RadiusMeters / 512d && Math.Abs(logical.Z) <= domain.RadiusMeters / 512d,
            "center cell must invert near logical origin");
        return assertions;
    }
}
