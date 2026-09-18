using System;

namespace Magenheim.Core.Underworld;

/// <summary>
/// The single deterministic noise authority for Underworld terrain.
/// </summary>
/// <remarks>
/// Integer-lattice value noise, seeded through an avalanche mix, sampled at double precision and
/// interpolated with a smoothstep. It is coordinate-robust: the lattice cell is an integer derived
/// from the coordinate, so the region can sit anywhere in the world without losing detail.
///
/// This replaces <c>Mathf.PerlinNoise</c> on the terrain path. The runtime previously folded the
/// world seed into the sample coordinate as <c>(x + seed * 0.0137f) * 0.00115f</c>. With an ordinary
/// seed that lands the input around 2.3e7, where a float carries roughly 0.002 of precision, while a
/// one-metre terrain step moves the input by 0.00115. Every column in the region therefore rounded
/// to the same input and the whole Underworld generated as one flat plane.
///
/// It is also the one place the avalanche mix lives, instead of a copy per consumer.
/// </remarks>
public static class UnderworldTerrainNoise
{
    /// <summary>Default feature size of the largest octave, in metres.</summary>
    public const double DefaultFeatureMetres = 1200d;

    /// <summary>Default octave count: macro basins down to local relief.</summary>
    public const int DefaultOctaves = 4;

    /// <summary>Lowbias32 finalizer. Neighbouring inputs produce unrelated outputs.</summary>
    public static uint Mix(uint value)
    {
        unchecked
        {
            value ^= value >> 16; value *= 0x7feb352du;
            value ^= value >> 15; value *= 0x846ca68bu;
            value ^= value >> 16;
            return value;
        }
    }

    /// <summary>Hashes one integer lattice cell to [-1, 1].</summary>
    public static double Lattice(int seed, int x, int z)
    {
        unchecked
        {
            var n = (uint)seed ^ ((uint)x * 0x9e3779b9u) ^ ((uint)z * 0x85ebca6bu);
            return Mix(n) / (double)uint.MaxValue * 2d - 1d;
        }
    }

    /// <summary>Smooth value noise in [-1, 1] at lattice scale.</summary>
    public static double Value(int seed, double x, double z)
    {
        var ix = (int)Math.Floor(x);
        var iz = (int)Math.Floor(z);
        var u = Smooth(x - ix);
        var v = Smooth(z - iz);
        var a = Lattice(seed, ix, iz);
        var b = Lattice(seed, ix + 1, iz);
        var c = Lattice(seed, ix, iz + 1);
        var d = Lattice(seed, ix + 1, iz + 1);
        return (a + (b - a) * u) * (1d - v) + (c + (d - c) * u) * v;
    }

    /// <summary>
    /// Fractal value noise normalised to [0, 1], for a coordinate in metres.
    /// </summary>
    /// <remarks>
    /// Each octave halves the feature size and halves its contribution, so the first octave carries
    /// the macro basins and walls the design calls for and the later ones supply traversable relief.
    /// Octaves are decorrelated by mixing the seed rather than by offsetting the coordinate, which
    /// keeps every octave in the same precision regime.
    /// </remarks>
    public static double Fractal01(int seed, double x, double z,
        int octaves = DefaultOctaves, double featureMetres = DefaultFeatureMetres)
    {
        if (octaves < 1) throw new ArgumentOutOfRangeException(nameof(octaves), "At least one octave is required.");
        if (!(featureMetres > 0d) || double.IsInfinity(featureMetres))
            throw new ArgumentOutOfRangeException(nameof(featureMetres), "Feature size must be a positive finite distance.");
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(z) || double.IsInfinity(z)) return 0.5d;

        var frequency = 1d / featureMetres;
        var amplitude = 1d;
        var total = 0d;
        var range = 0d;
        var octaveSeed = seed;

        for (var octave = 0; octave < octaves; octave++)
        {
            total += Value(octaveSeed, x * frequency, z * frequency) * amplitude;
            range += amplitude;
            frequency *= 2d;
            amplitude *= 0.5d;
            octaveSeed = unchecked((int)Mix((uint)octaveSeed));
        }

        var normalized = (total / range + 1d) * 0.5d;
        return normalized < 0d ? 0d : normalized > 1d ? 1d : normalized;
    }

    /// <summary>
    /// Terrain relief in metres, summed over octaves that each carry their own metre amplitude.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because normalising fractal noise to [0, 1] and then multiplying by a single
    /// amplitude produces terrain nobody can see. Measured from the shipped model: a Fungal Forest
    /// screen 50m across varied by 0.02m, and the steepest one-metre step over 400m was 0.015m, or
    /// 0.85 degrees. The fine octaves were present but each was a fraction of an already small total,
    /// so the Underworld read as a flat plane however many octaves were added.
    /// </para>
    /// <para>
    /// Giving every octave an amplitude in metres fixes the scale directly. The table spans macro
    /// basins and wall masses down to footstep-scale relief, so the ground reads as ground while the
    /// long wavelengths still carry the regional geography the design asks for.
    /// </para>
    /// </remarks>
    /// <param name="regionalWeight">
    /// How much of the large-wavelength geography this column receives, 0 to 1. The coarse octaves
    /// carry mountain barriers and deep basins; at full strength they swing well over a hundred
    /// metres, which is correct for the outer provinces and wrong for the arrival basin, where it
    /// would flood the Fungal Forest and strand the player. Attenuating them toward the centre is
    /// what keeps the first biome dry and walkable while the rest of the realm stays enormous.
    /// </param>
    public static double ReliefMetres(int seed, double x, double z, double regionalWeight)
    {
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(z) || double.IsInfinity(z)) return 0d;
        if (double.IsNaN(regionalWeight)) regionalWeight = 0d;
        regionalWeight = regionalWeight < 0d ? 0d : regionalWeight > 1d ? 1d : regionalWeight;

        var total = 0d;
        var octaveSeed = seed;
        for (var octave = 0; octave < ReliefFeatureMetres.Length; octave++)
        {
            var frequency = 1d / ReliefFeatureMetres[octave];
            var amplitude = ReliefAmplitudeMetres[octave] * (ReliefIsRegional[octave] ? regionalWeight : 1d);
            total += Value(octaveSeed, x * frequency, z * frequency) * amplitude;
            octaveSeed = unchecked((int)Mix((uint)octaveSeed));
        }
        return total;
    }

    /// <summary>Largest relief this table can produce, in metres, for bounds checking.</summary>
    public static double MaximumReliefMetres
    {
        get
        {
            var total = 0d;
            foreach (var amplitude in ReliefAmplitudeMetres) total += amplitude;
            return total;
        }
    }

    // Wavelength in metres, coarsest first: regional basins and walls, ridges, hills, hummocks,
    // then footstep-scale break-up. The first three are regional and fade out toward the arrival
    // basin; the last three are local and always present, so no part of the realm is ever a plane.
    private static readonly double[] ReliefFeatureMetres = { 2048d, 768d, 256d, 96d, 32d, 11d };
    private static readonly double[] ReliefAmplitudeMetres = { 72d, 40d, 20d, 10d, 5d, 2d };
    private static readonly bool[] ReliefIsRegional = { true, true, true, false, false, false };

    private static double Smooth(double value) => value * value * (3d - 2d * value);
}
