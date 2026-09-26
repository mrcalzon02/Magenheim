using System;

namespace Magenheim.Core.Underworld;

/// <summary>Metre-scale geography per biome. Amplitudes bound the noise naturally; heights are never clipped.</summary>
public static class UnderworldBiomeTerrain
{
    public const string AlgorithmId = "biome-relief-v2-unclipped-provinces";
    private enum Form { Rolling, Ridges, Faults, Basins }
    private sealed record Profile(double Offset, double RegionalLift, Form Form,
        double[] Wavelengths, double[] Amplitudes);

    // First three octaves describe regional geography; last three describe local ground.
    // Forest local relief is unchanged at the gate. All larger geography fades in outside it.
    private static readonly Profile Forest = new(6, 180, Form.Rolling,
        new[] { 2048d, 768d, 256d, 96d, 32d, 11d }, new[] { 180d, 90d, 40d, 9.9d, 4.4d, 1.65d });
    private static readonly Profile Blackwater = new(0, -180, Form.Rolling,
        new[] { 1800d, 600d, 200d, 110d, 40d, 13d }, new[] { 180d, 110d, 45d, 16d, 7d, 2d });
    private static readonly Profile Sulfur = new(0, 220, Form.Ridges,
        new[] { 1800d, 600d, 220d, 90d, 30d, 10d }, new[] { 380d, 180d, 70d, 35d, 14d, 5d });
    private static readonly Profile Frozen = new(0, 340, Form.Ridges,
        new[] { 2300d, 900d, 320d, 120d, 40d, 14d }, new[] { 560d, 220d, 90d, 28d, 10d, 3d });
    private static readonly Profile Fracture = new(0, 280, Form.Faults,
        new[] { 1600d, 550d, 180d, 70d, 24d, 8d }, new[] { 700d, 300d, 120d, 60d, 18d, 5d });
    private static readonly Profile Decay = new(0, -15, Form.Basins,
        new[] { 1800d, 700d, 220d, 100d, 35d, 12d }, new[] { 240d, 140d, 55d, 22d, 10d, 3d });

    public static double HeightDelta(UnderworldTerrainBiome biome, int seed, double x, double z, double regionalWeight)
    {
        var profile = biome switch
        {
            UnderworldTerrainBiome.FungalForest => Forest,
            UnderworldTerrainBiome.BlackwaterDeep => Blackwater,
            UnderworldTerrainBiome.SulfurousWastes => Sulfur,
            UnderworldTerrainBiome.FrozenCaverns => Frozen,
            UnderworldTerrainBiome.FractureZones => Fracture,
            UnderworldTerrainBiome.GreatDecay => Decay,
            _ => throw new ArgumentOutOfRangeException(nameof(biome)),
        };
        var total = profile.Offset + profile.RegionalLift * regionalWeight;
        // Preserve the arrival forest's existing fine-noise seed sequence.
        var octaveSeed = biome == UnderworldTerrainBiome.FungalForest ? seed
            : unchecked((int)UnderworldTerrainNoise.Mix((uint)seed ^ ((uint)biome * 0x9e3779b9u)));
        for (var octave = 0; octave < profile.Wavelengths.Length; octave++)
        {
            var value = UnderworldTerrainNoise.Value(octaveSeed, x / profile.Wavelengths[octave], z / profile.Wavelengths[octave]);
            if (octave < 2)
                value = profile.Form switch
                {
                    Form.Ridges => 1d - 2d * Math.Abs(value),
                    Form.Faults => Math.Tanh(value * 3d),
                    Form.Basins => 1d - 2d * Math.Pow(1d - Math.Abs(value), 2d),
                    _ => value,
                };
            total += value * profile.Amplitudes[octave] * (octave < 3 ? regionalWeight : 1d);
            octaveSeed = unchecked((int)UnderworldTerrainNoise.Mix((uint)octaveSeed));
        }
        return total;
    }
}
