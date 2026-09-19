using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Native spatial contract for terrain inside the dedicated Underworld instance.
/// Contains only instance-local dimensions. Surface host coordinates are deliberately impossible
/// to express through this type.
/// </summary>
public sealed record UnderworldInstanceTerrainDomain(
    double RadiusMeters,
    double MinimumY,
    double MaximumY)
{
    public static UnderworldInstanceTerrainDomain CreateDefault() =>
        ValidateAndFreeze(8000d, -256d, 1792d);

    public static UnderworldInstanceTerrainDomain ValidateAndFreeze(
        double radiusMeters,
        double minimumY,
        double maximumY)
    {
        if (!Finite(radiusMeters) || radiusMeters <= 0d)
            throw new InvalidOperationException("Underworld instance terrain radius must be positive and finite.");
        if (!Finite(minimumY) || !Finite(maximumY) || maximumY <= minimumY)
            throw new InvalidOperationException("Underworld instance terrain vertical domain is invalid.");
        return new UnderworldInstanceTerrainDomain(radiusMeters, minimumY, maximumY);
    }

    public bool Contains(double x, double y, double z)
    {
        if (!Finite(x) || !Finite(y) || !Finite(z)) return false;
        return x * x + z * z <= RadiusMeters * RadiusMeters && y >= MinimumY && y <= MaximumY;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
