using System;
namespace Magenheim.Core.Underworld;

/// <summary>Fixed ecology coordinates; residency can change without moving surviving scenery.</summary>
public static class UnderworldEcologyCells
{
    public const int SizeMeters = 64;
    public const int LoadRadius = 3;
    public const int RetainRadius = 4;
    public static UnderworldInstanceChunkKey KeyAt(double x, double z) =>
        new((int)Math.Floor(x / SizeMeters), (int)Math.Floor(z / SizeMeters));
    public static int Seed(int worldSeed, UnderworldInstanceChunkKey key) =>
        unchecked(worldSeed ^ key.X * 73856093 ^ key.Z * 19349663);
    public static bool Retain(UnderworldInstanceChunkKey key, UnderworldInstanceChunkKey focus) =>
        Math.Abs(key.X - focus.X) <= RetainRadius && Math.Abs(key.Z - focus.Z) <= RetainRadius;
}
