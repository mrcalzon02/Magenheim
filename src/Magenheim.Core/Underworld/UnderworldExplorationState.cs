using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Independent fog-of-war state for one logical map layer. Surface and Underworld exploration must
/// never share cells: they share a simulation/save, not a player-facing map.
/// </summary>
public sealed class UnderworldExplorationState
{
    private readonly bool[] _cells;

    public UnderworldExplorationState(MagenheimMapLayer layer, int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Layer = layer;
        Width = width;
        Height = height;
        _cells = new bool[checked(width * height)];
    }

    public MagenheimMapLayer Layer { get; }
    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// Revealed cells. Maintained incrementally so a presentation layer can decide whether its fog
    /// texture is stale without rescanning every cell each frame.
    /// </summary>
    public int ExploredCount { get; private set; }

    public bool IsExplored(int x, int y) => _cells[UnderworldMapProjection.CellIndex(Width, Height, x, y)];

    public bool Reveal(int x, int y)
    {
        var index = UnderworldMapProjection.CellIndex(Width, Height, x, y);
        if (_cells[index]) return false;
        _cells[index] = true;
        ExploredCount++;
        return true;
    }

    public int RevealCircle(int centerX, int centerY, int radiusCells)
    {
        if (radiusCells < 0) throw new ArgumentOutOfRangeException(nameof(radiusCells));
        var changed = 0;
        var radiusSquared = (long)radiusCells * radiusCells;
        var minX = Math.Max(0, centerX - radiusCells);
        var maxX = Math.Min(Width - 1, centerX + radiusCells);
        var minY = Math.Max(0, centerY - radiusCells);
        var maxY = Math.Min(Height - 1, centerY + radiusCells);
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var dx = (long)x - centerX;
            var dy = (long)y - centerY;
            if (dx * dx + dy * dy <= radiusSquared && Reveal(x, y)) changed++;
        }
        return changed;
    }

    public byte[] Pack()
    {
        var packed = new byte[(_cells.Length + 7) / 8];
        for (var i = 0; i < _cells.Length; i++)
            if (_cells[i]) packed[i >> 3] |= (byte)(1 << (i & 7));
        return packed;
    }

    public void Restore(byte[] packed)
    {
        if (packed is null) throw new ArgumentNullException(nameof(packed));
        if (packed.Length != (_cells.Length + 7) / 8)
            throw new InvalidOperationException("Exploration payload size does not match this logical map layer.");
        var explored = 0;
        for (var i = 0; i < _cells.Length; i++)
        {
            _cells[i] = (packed[i >> 3] & (1 << (i & 7))) != 0;
            if (_cells[i]) explored++;
        }
        ExploredCount = explored;
    }
}

/// <summary>Owns mutually isolated exploration state for the two player-facing maps.</summary>
public sealed class MagenheimExplorationLayers
{
    public MagenheimExplorationLayers(int surfaceWidth, int surfaceHeight, int underworldWidth, int underworldHeight)
    {
        Surface = new UnderworldExplorationState(MagenheimMapLayer.Surface, surfaceWidth, surfaceHeight);
        Underworld = new UnderworldExplorationState(MagenheimMapLayer.Underworld, underworldWidth, underworldHeight);
    }

    public UnderworldExplorationState Surface { get; }
    public UnderworldExplorationState Underworld { get; }

    public UnderworldExplorationState For(MagenheimMapLayer layer) =>
        layer == MagenheimMapLayer.Underworld ? Underworld : Surface;
}
