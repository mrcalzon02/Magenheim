using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Magenheim.Core.Underworld;

/// <summary>Heightfield parameters only. The shared cavern skybox has no terrain geometry.</summary>
public sealed record UnderworldTerrainProfile(
    string BiomeId, int RollingHeightMeters, int BasinDepthMeters,
    int LandformScaleMeters, int DetailScaleMeters, int DetailHeightMeters);

public sealed class UnderworldTerrainDefinition
{
    public const string Algorithm = "fungal-heightfield-v1";
    public UnderworldTerrainProfile Profile { get; }
    public string Fingerprint { get; }

    public UnderworldTerrainDefinition(UnderworldTerrainProfile profile)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (profile.BiomeId != "magenheim.underworld.biome.fungal_forest")
            throw new InvalidOperationException("The initial terrain profile supports Fungal Forest only.");
        if (profile.RollingHeightMeters < 0 || profile.RollingHeightMeters > 32 ||
            profile.BasinDepthMeters < 0 || profile.BasinDepthMeters > 32 ||
            profile.DetailHeightMeters < 0 || profile.DetailHeightMeters > 8 ||
            profile.LandformScaleMeters < 32 || profile.LandformScaleMeters > 1024 ||
            profile.DetailScaleMeters < 8 || profile.DetailScaleMeters > profile.LandformScaleMeters)
            throw new InvalidOperationException("Terrain amplitudes or wavelengths exceed supported bounds.");
        Profile = profile;
        var text = string.Join("|", Algorithm, profile.BiomeId,
            profile.RollingHeightMeters.ToString(CultureInfo.InvariantCulture),
            profile.BasinDepthMeters.ToString(CultureInfo.InvariantCulture),
            profile.LandformScaleMeters.ToString(CultureInfo.InvariantCulture),
            profile.DetailScaleMeters.ToString(CultureInfo.InvariantCulture),
            profile.DetailHeightMeters.ToString(CultureInfo.InvariantCulture));
        using var sha = SHA256.Create();
        Fingerprint = string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text))
            .Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
    }
}

public sealed record UnderworldTerrainPoint(double HeightMeters, double SlopeDegrees, bool IsSubmerged);

public static class UnderworldTerrain
{
    /// <summary>
    /// Alters a supplied surface height only for an admitted Underworld biome sample.
    /// Biome strength must come from the realm's biome authority, not vanilla biome names.
    /// Coordinates are global realm-local meters, independent of zone evaluation order.
    /// </summary>
    public static double HeightAt(UnderworldTerrainDefinition definition, UnderworldLayer layer,
        int seed, double x, double z, double baseHeight, double biomeStrength)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        ValidateCoordinate(x); ValidateCoordinate(z);
        if (!Finite(baseHeight) || !Finite(biomeStrength) || biomeStrength < 0 || biomeStrength > 1)
            throw new ArgumentOutOfRangeException(nameof(biomeStrength), "Height and biome strength must be finite; strength must be 0..1.");
        if (layer != UnderworldLayer.Surface && layer != UnderworldLayer.Underworld)
            throw new ArgumentOutOfRangeException(nameof(layer));
        if (layer == UnderworldLayer.Surface || biomeStrength == 0) return baseHeight;
        var p = definition.Profile;
        var rolling = Noise(seed, x / p.LandformScaleMeters, z / p.LandformScaleMeters);
        var basin = Noise(unchecked(seed + 7919), x / p.LandformScaleMeters, z / p.LandformScaleMeters);
        var detail = Noise(unchecked(seed + 104729), x / p.DetailScaleMeters, z / p.DetailScaleMeters);
        var depression = Smooth(Math.Max(0, basin));
        return baseHeight + biomeStrength * (rolling * p.RollingHeightMeters -
            depression * p.BasinDepthMeters + detail * p.DetailHeightMeters);
    }

    /// <summary>Derivatives include the base heightfield and biome blend, avoiding seam-local slopes.</summary>
    public static UnderworldTerrainPoint Sample(UnderworldTerrainDefinition definition, UnderworldLayer layer,
        int seed, double x, double z, double waterHeight,
        Func<double, double, double> baseHeight, Func<double, double, double> biomeStrength)
    {
        if (baseHeight is null) throw new ArgumentNullException(nameof(baseHeight));
        if (biomeStrength is null) throw new ArgumentNullException(nameof(biomeStrength));
        if (!Finite(waterHeight)) throw new ArgumentOutOfRangeException(nameof(waterHeight));
        double Height(double a, double b) => HeightAt(definition, layer, seed, a, b, baseHeight(a, b), biomeStrength(a, b));
        var height = Height(x, z);
        var dx = (Height(x + 1, z) - Height(x - 1, z)) * .5;
        var dz = (Height(x, z + 1) - Height(x, z - 1)) * .5;
        return new UnderworldTerrainPoint(height, Math.Atan(Math.Sqrt(dx * dx + dz * dz)) * 180 / Math.PI, height <= waterHeight);
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static void ValidateCoordinate(double value)
    {
        if (!Finite(value) || Math.Abs(value) > 1000000)
            throw new ArgumentOutOfRangeException(nameof(value), "Terrain coordinates must be finite and within the supported realm bounds.");
    }
    private static double Smooth(double value) => value * value * (3 - 2 * value);
    private static double Noise(int seed, double x, double z)
    {
        var ix = (int)Math.Floor(x); var iz = (int)Math.Floor(z);
        var u = Smooth(x - ix); var v = Smooth(z - iz);
        var a = Hash(seed, ix, iz); var b = Hash(seed, ix + 1, iz);
        var c = Hash(seed, ix, iz + 1); var d = Hash(seed, ix + 1, iz + 1);
        return (a + (b - a) * u) * (1 - v) + (c + (d - c) * u) * v;
    }
    private static double Hash(int seed, int x, int z)
    {
        unchecked
        {
            var n = (uint)seed ^ ((uint)x * 0x9e3779b9u) ^ ((uint)z * 0x85ebca6bu);
            n ^= n >> 16; n *= 0x7feb352du; n ^= n >> 15; n *= 0x846ca68bu; n ^= n >> 16;
            return n / (double)uint.MaxValue * 2 - 1;
        }
    }
}
