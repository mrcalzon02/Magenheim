using System;

namespace Magenheim.Core.Underworld;

public enum UnderworldLandformKind { Spire, Plateau }

/// <summary>A seeded, faceted terrain footprint, not a separately placed scenery prop.</summary>
public readonly record struct UnderworldMonumentalLandform(
    double X, double Z, double Radius, double Height, double Rotation, UnderworldLandformKind Kind)
{
    public double ExtentMeters => (Radius + 45d) * 1.15d / Math.Cos(Math.PI / 6d);

    // Unequal support planes give straight cliff faces without a regular hexagonal column.
    public double HeightAt(double x, double z)
    {
        var distance = 0d;
        for (var face = 0; face < 6; face++)
        {
            var angle = Rotation + face * Math.PI / 3d;
            var reach = 1d + .15d * UnderworldTerrainNoise.Lattice((int)(Rotation * 100000d), face, 17);
            distance = Math.Max(distance, ((x - X) * Math.Cos(angle) + (z - Z) * Math.Sin(angle)) / reach);
        }
        // Erosion cuts the plan-view boundary, not the vertical height profile. The broad
        // walls stay sheer while buttresses/recesses break up an otherwise extruded column.
        var fracture = UnderworldTerrainNoise.Value(37, x / 180d, z / 180d) * 35d +
                       UnderworldTerrainNoise.Value(71, x / 55d, z / 55d) * 10d;
        distance += fracture * Math.Min(1d, distance / Radius);
        var t = distance / Radius;
        if (t >= 1d) return 0d;
        // Plateaus rise across a 64m horizontal wall band; spires converge to a single apex.
        // No smoothstep: smoothing turns these silhouettes back into rounded mountains.
        return Height * (Kind == UnderworldLandformKind.Plateau
            ? Math.Min(1d, (1d - t) * Radius / 64d)
            : 1d - t);
    }
}

public static class UnderworldMonumentalLandforms
{
    public const string AlgorithmId = "faceted-monuments-v3-local-relief";
    public const double CellSizeMeters = 3072d;
    public const double MaximumHeightMeters = UnderworldSkyLighting.MaximumTerrainHeightMeters;

    public static bool TryGet(UnderworldInstanceTerrainDomain domain, int seed, int cellX, int cellZ,
        out UnderworldMonumentalLandform landform)
    {
        landform = default;
        var hash = UnderworldTerrainNoise.Mix(unchecked((uint)seed ^ (uint)cellX * 374761393u ^ (uint)cellZ * 668265263u));
        if (hash % 100u >= 48u) return false;
        var x = (cellX + .5d) * CellSizeMeters + Unit(hash, 11u) * 320d - 160d;
        var z = (cellZ + .5d) * CellSizeMeters + Unit(hash, 23u) * 320d - 160d;
        var plateau = (hash & 256u) != 0u;
        var radius = plateau ? 650d + Unit(hash, 37u) * 250d : 300d + Unit(hash, 37u) * 180d;
        var distance = Math.Sqrt(x * x + z * z);
        // Circumradius is larger than apothem. Keep the complete cliff footprint out of
        // the arrival/transition basin and away from the circular instance boundary.
        var extent = (radius + 45d) * 1.15d / Math.Cos(Math.PI / 6d);
        if (distance - extent < domain.RadiusMeters * .27d || distance + extent > domain.RadiusMeters - 64d)
            return false;
        // Custom short domains must never generate an unreachable summit above their ceiling.
        var available = domain.MaximumY - 128d;
        if (available < 3200d) return false;
        var height = Math.Min(available, 3200d + Unit(hash, 53u) * (MaximumHeightMeters - 3200d));
        landform = new UnderworldMonumentalLandform(x, z, radius, height,
            Unit(hash, 71u) * Math.PI / 3d,
            plateau ? UnderworldLandformKind.Plateau : UnderworldLandformKind.Spire);
        return true;
    }

    public static double HeightAt(UnderworldInstanceTerrainDomain domain, int seed, double x, double z)
    {
        // Every footprint fits inside its own cell, so sampling is independent of residency
        // and needs neither a registry nor a neighbour search.
        var cellX = checked((int)Math.Floor(x / CellSizeMeters));
        var cellZ = checked((int)Math.Floor(z / CellSizeMeters));
        return TryGet(domain, seed, cellX, cellZ, out var landform) ? landform.HeightAt(x, z) : 0d;
    }

    public static double ApplyToTerrain(UnderworldInstanceTerrainDomain domain, int seed, double x, double z, double terrainHeight)
    {
        var cellX = checked((int)Math.Floor(x / CellSizeMeters));
        var cellZ = checked((int)Math.Floor(z / CellSizeMeters));
        if (!TryGet(domain, seed, cellX, cellZ, out var landform)) return terrainHeight;
        var influence = landform.HeightAt(x, z) / landform.Height;
        return terrainHeight + (landform.Height - terrainHeight) * influence;
    }

    private static double Unit(uint hash, uint salt) =>
        UnderworldTerrainNoise.Mix(hash ^ salt * 2654435761u) / ((double)uint.MaxValue + 1d);
}
