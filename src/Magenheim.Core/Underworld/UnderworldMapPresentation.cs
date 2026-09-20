using System;

namespace Magenheim.Core.Underworld;

public readonly record struct UnderworldMapViewport(
    MagenheimMapLayer Layer,
    double CenterX,
    double CenterZ,
    double RadiusMeters,
    int ExplorationWidth,
    int ExplorationHeight);

/// <summary>Pure presentation policy for the dedicated Underworld instance map.</summary>
public static class UnderworldMapPresentation
{
    public static UnderworldMapViewport CreateUnderworldViewport(
        UnderworldInstanceTerrainDomain domain,
        int explorationWidth,
        int explorationHeight)
    {
        if (domain is null) throw new ArgumentNullException(nameof(domain));
        if (explorationWidth <= 0) throw new ArgumentOutOfRangeException(nameof(explorationWidth));
        if (explorationHeight <= 0) throw new ArgumentOutOfRangeException(nameof(explorationHeight));
        return new UnderworldMapViewport(
            MagenheimMapLayer.Underworld,
            0d,
            0d,
            domain.RadiusMeters,
            explorationWidth,
            explorationHeight);
    }

    public static bool TryLogicalToCell(
        UnderworldMapViewport viewport,
        double logicalX,
        double logicalZ,
        out int cellX,
        out int cellY)
    {
        cellX = cellY = -1;
        if (viewport.Layer != MagenheimMapLayer.Underworld || viewport.RadiusMeters <= 0d ||
            viewport.ExplorationWidth <= 0 || viewport.ExplorationHeight <= 0 ||
            !Finite(logicalX) || !Finite(logicalZ)) return false;

        var dx = logicalX - viewport.CenterX;
        var dz = logicalZ - viewport.CenterZ;
        if (dx * dx + dz * dz > viewport.RadiusMeters * viewport.RadiusMeters) return false;

        var u = (dx / viewport.RadiusMeters + 1d) * 0.5d;
        var v = (dz / viewport.RadiusMeters + 1d) * 0.5d;
        cellX = ClampCell((int)Math.Floor(u * viewport.ExplorationWidth), viewport.ExplorationWidth);
        cellY = ClampCell((int)Math.Floor(v * viewport.ExplorationHeight), viewport.ExplorationHeight);
        return true;
    }

    public static MagenheimMapPoint CellCenterToLogical(UnderworldMapViewport viewport, int cellX, int cellY)
    {
        if (viewport.Layer != MagenheimMapLayer.Underworld) throw new InvalidOperationException("Viewport is not the Underworld layer.");
        if (cellX < 0 || cellX >= viewport.ExplorationWidth) throw new ArgumentOutOfRangeException(nameof(cellX));
        if (cellY < 0 || cellY >= viewport.ExplorationHeight) throw new ArgumentOutOfRangeException(nameof(cellY));
        var x = (((cellX + 0.5d) / viewport.ExplorationWidth) * 2d - 1d) * viewport.RadiusMeters + viewport.CenterX;
        var z = (((cellY + 0.5d) / viewport.ExplorationHeight) * 2d - 1d) * viewport.RadiusMeters + viewport.CenterZ;
        return new MagenheimMapPoint(x, z);
    }

    private static int ClampCell(int value, int size) => value < 0 ? 0 : value >= size ? size - 1 : value;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
