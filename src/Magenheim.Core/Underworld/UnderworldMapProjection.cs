using System;

namespace Magenheim.Core.Underworld;

public enum MagenheimMapLayer
{
    Surface = 0,
    Underworld = 1,
}

public readonly struct MagenheimMapPoint
{
    public MagenheimMapPoint(double x, double z) { X = x; Z = z; }
    public double X { get; }
    public double Z { get; }
}

/// <summary>
/// Pure authority for presenting multiple logical maps while all simulation remains in one Valheim
/// world/save. The Underworld's 40km host offset is storage/streaming space, never map-space truth.
/// </summary>
public static class UnderworldMapProjection
{
    public static MagenheimMapLayer LayerAtWorldColumn(
        UnderworldSpatialDomainDefinition domain, double worldX, double worldZ) =>
        UnderworldSpatialDomain.ContainsHostColumn(domain, worldX, worldZ)
            ? MagenheimMapLayer.Underworld
            : MagenheimMapLayer.Surface;

    public static MagenheimMapPoint WorldToLayer(
        UnderworldSpatialDomainDefinition domain, MagenheimMapLayer layer, double worldX, double worldZ)
    {
        RequireFinite(worldX, nameof(worldX));
        RequireFinite(worldZ, nameof(worldZ));
        if (layer == MagenheimMapLayer.Surface) return new MagenheimMapPoint(worldX, worldZ);
        if (!UnderworldSpatialDomain.ContainsHostColumn(domain, worldX, worldZ))
            throw new InvalidOperationException("World column is outside the reserved Underworld region.");
        var local = UnderworldSpatialDomain.ToLogicalColumn(domain, worldX, worldZ);
        return new MagenheimMapPoint(local.X, local.Z);
    }

    public static MagenheimMapPoint LayerToWorld(
        UnderworldSpatialDomainDefinition domain, MagenheimMapLayer layer, double mapX, double mapZ)
    {
        RequireFinite(mapX, nameof(mapX));
        RequireFinite(mapZ, nameof(mapZ));
        if (layer == MagenheimMapLayer.Surface) return new MagenheimMapPoint(mapX, mapZ);
        if (mapX * mapX + mapZ * mapZ > domain.RadiusMeters * domain.RadiusMeters)
            throw new InvalidOperationException("Underworld map point lies outside the playable radius.");
        return new MagenheimMapPoint(domain.HostCenterX + mapX, domain.HostCenterZ + mapZ);
    }

    /// <summary>
    /// Logical biome lookup for map/HUD/environment/ecology consumers. This deliberately does not
    /// consult WorldGenerator.GetBiomeSector: the host region can be outside vanilla's finite biome
    /// map and Ocean is therefore only an engine compatibility answer, not Underworld authority.
    /// </summary>
    public static UnderworldTerrainBiome UnderworldBiomeAt(
        UnderworldSpatialDomainDefinition domain, int surfaceSeed, double logicalX, double logicalZ)
    {
        if (logicalX * logicalX + logicalZ * logicalZ > domain.RadiusMeters * domain.RadiusMeters)
            throw new InvalidOperationException("Underworld map point lies outside the playable radius.");
        // UnderworldTerrainLifecycle.Evaluate(UnderworldSpatialDomainDefinition, ...) is obsolete;
        // this is the same bridge its own (obsolete) implementation used internally, inlined so this
        // caller compiles against the native instance-domain overload instead of the deprecated one.
        var instanceDomain = UnderworldInstanceTerrainDomain.ValidateAndFreeze(
            domain.RadiusMeters, domain.LogicalMinY, domain.LogicalMaxY);
        var result = UnderworldTerrainLifecycle.Evaluate(
            instanceDomain,
            new UnderworldTerrainSample(logicalX, 0d, logicalZ, UnderworldTerrainLifecycle.BaseElevationMeters, 0d,
                UnderworldTerrainNoise.Fractal01(surfaceSeed, logicalX, logicalZ)),
            surfaceSeed);
        if (!result.Admitted) throw new InvalidOperationException("Underworld biome authority rejected an admitted map point.");
        return result.Biome;
    }

    public static int CellIndex(int width, int height, int x, int y)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (x < 0 || x >= width || y < 0 || y >= height) throw new ArgumentOutOfRangeException(nameof(x));
        return checked(y * width + x);
    }

    private static void RequireFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
    }
}
