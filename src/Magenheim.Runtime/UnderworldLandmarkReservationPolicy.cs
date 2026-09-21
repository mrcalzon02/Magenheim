using System;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Deterministic reservation authority for sparse native-instance landmarks.
/// This does not place landmarks by itself: a landmark family must opt into the
/// same profile before ordinary structures are made to yield to it, preventing
/// empty reservation holes while landmark content is still being authored.
/// </summary>
internal static class UnderworldLandmarkReservationPolicy
{
    internal readonly struct Profile
    {
        internal Profile(int salt, int cellSizeChunks, int exclusionRadiusChunks)
        {
            if (cellSizeChunks < 1) throw new ArgumentOutOfRangeException(nameof(cellSizeChunks));
            if (exclusionRadiusChunks < 0) throw new ArgumentOutOfRangeException(nameof(exclusionRadiusChunks));
            Salt = salt;
            CellSizeChunks = cellSizeChunks;
            ExclusionRadiusChunks = exclusionRadiusChunks;
        }

        internal int Salt { get; }
        internal int CellSizeChunks { get; }
        internal int ExclusionRadiusChunks { get; }
    }

    internal static UnderworldInstanceChunkKey Anchor(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key, Profile profile)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var cellX = FloorDiv(key.X, profile.CellSizeChunks);
        var cellZ = FloorDiv(key.Z, profile.CellSizeChunks);
        return AnchorForCell(identity, cellX, cellZ, profile);
    }

    internal static bool IsAnchor(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key, Profile profile)
    {
        var anchor = Anchor(identity, key, profile);
        return anchor.X == key.X && anchor.Z == key.Z;
    }

    internal static bool IsReserved(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key, Profile profile)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var cellX = FloorDiv(key.X, profile.CellSizeChunks);
        var cellZ = FloorDiv(key.Z, profile.CellSizeChunks);

        // A reservation radius may cross a cell boundary, so inspect the local
        // 3x3 cells rather than assuming the current cell owns the nearest anchor.
        for (var dz = -1; dz <= 1; dz++)
        for (var dx = -1; dx <= 1; dx++)
        {
            var anchor = AnchorForCell(identity, cellX + dx, cellZ + dz, profile);
            if (ChebyshevDistance(anchor, key) <= profile.ExclusionRadiusChunks) return true;
        }
        return false;
    }

    private static UnderworldInstanceChunkKey AnchorForCell(UnderworldWorldIdentity identity, int cellX, int cellZ, Profile profile)
    {
        unchecked
        {
            var hash = (uint)(identity.DerivedSeed32 ^ profile.Salt);
            hash = Mix(hash, (uint)cellX);
            hash = Mix(hash, (uint)cellZ);
            var xOffset = (int)(hash % (uint)profile.CellSizeChunks);
            hash = Mix(hash, 0x9E3779B9u);
            var zOffset = (int)(hash % (uint)profile.CellSizeChunks);
            return new UnderworldInstanceChunkKey(cellX * profile.CellSizeChunks + xOffset, cellZ * profile.CellSizeChunks + zOffset);
        }
    }

    private static uint Mix(uint hash, uint value)
    {
        unchecked
        {
            hash = hash * 397u ^ value;
            hash ^= hash >> 16;
            hash *= 0x85EBCA6Bu;
            hash ^= hash >> 13;
            return hash;
        }
    }

    private static int FloorDiv(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private static int ChebyshevDistance(UnderworldInstanceChunkKey left, UnderworldInstanceChunkKey right) =>
        Math.Max(Math.Abs(left.X - right.X), Math.Abs(left.Z - right.Z));
}
