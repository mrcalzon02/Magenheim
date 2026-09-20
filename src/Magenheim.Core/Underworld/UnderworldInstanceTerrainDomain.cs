using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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

    public string Fingerprint
    {
        get
        {
            var payload = new StringBuilder()
                .Append("underworld-instance-terrain-domain-v1\n")
                .Append("radius=").Append(Canonical(RadiusMeters)).Append('\n')
                .Append("minimum-y=").Append(Canonical(MinimumY)).Append('\n')
                .Append("maximum-y=").Append(Canonical(MaximumY)).Append('\n')
                .ToString();
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var result = new StringBuilder(hash.Length * 2);
            foreach (var value in hash)
                result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }
    }

    public bool Contains(double x, double y, double z)
    {
        if (!Finite(x) || !Finite(y) || !Finite(z)) return false;
        return x * x + z * z <= RadiusMeters * RadiusMeters && y >= MinimumY && y <= MaximumY;
    }

    private static string Canonical(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
