using System;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Deterministic reservation authority for sparse native-instance landmarks.
/// Reservation geometry is derived only from the Underworld identity and native
/// chunk coordinates. Callers may additionally validate anchors (for example by
/// biome) before allowing them to exclude ordinary structure families.
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

    internal static bool IsReserved(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key, Profile profile) =>
        IsReserved(identity, key, profile, _ => true);

    /// <summary>
    /// Returns true only when a nearby deterministic anchor is both inside the
    /// exclusion radius and accepted by the caller. This is the gate that lets
    /// biome-specific landmarks reserve space without creating empty holes around
    /// anchors that landed in some other Underworld biome.
    /// </summary>
    internal static bool IsReserved(
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey key,
        Profile profile,
        Func<UnderworldInstanceChunkKey, bool> anchorAccepted)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchorAccepted is null) throw new ArgumentNullException(nameof(anchorAccepted));
        var cellX = FloorDiv(key.X, profile.CellSizeChunks);
        var cellZ = FloorDiv(key.Z, profile.CellSizeChunks);

        // A reservation can extend beyond an immediately adjacent landmark cell.
        // Search enough cells to cover the configured chunk radius rather than
        // silently assuming every future profile will fit inside a 3x3 cell window.
        // Compute in long space so a valid extreme profile cannot overflow while
        // deriving its search window.
        var cellRadiusLong = 1L + (long)profile.ExclusionRadiusChunks / profile.CellSizeChunks;
        if (cellRadiusLong > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(profile), "Landmark reservation search radius exceeds supported native chunk range.");
        var cellRadius = (int)cellRadiusLong;

        for (var dz = -cellRadius; dz <= cellRadius; dz++)
        for (var dx = -cellRadius; dx <= cellRadius; dx++)
        {
            var anchor = AnchorForCell(identity, CheckedCellOffset(cellX, dx), CheckedCellOffset(cellZ, dz), profile);
            if (ChebyshevDistance(anchor, key) <= profile.ExclusionRadiusChunks && anchorAccepted(anchor)) return true;
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
            return new UnderworldInstanceChunkKey(CheckedAnchorCoordinate(cellX, profile.CellSizeChunks, xOffset), CheckedAnchorCoordinate(cellZ, profile.CellSizeChunks, zOffset));
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

    private static int CheckedCellOffset(int cell, int offset)
    {
        var value = (long)cell + offset;
        if (value < int.MinValue || value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(offset), "Landmark reservation search exceeded native cell coordinate range.");
        return (int)value;
    }

    private static int CheckedAnchorCoordinate(int cell, int cellSize, int offset)
    {
        var value = (long)cell * cellSize + offset;
        if (value < int.MinValue || value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(cell), "Landmark anchor exceeded native chunk coordinate range.");
        return (int)value;
    }

    private static long ChebyshevDistance(UnderworldInstanceChunkKey left, UnderworldInstanceChunkKey right)
    {
        var dx = Math.Abs((long)left.X - right.X);
        var dz = Math.Abs((long)left.Z - right.Z);
        return Math.Max(dx, dz);
    }
}
