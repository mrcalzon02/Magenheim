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
        IsReserved(identity, key, profile, profile.ExclusionRadiusChunks, _ => true);

    internal static bool IsReserved(
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey key,
        Profile profile,
        Func<UnderworldInstanceChunkKey, bool> anchorAccepted) =>
        IsReserved(identity, key, profile, profile.ExclusionRadiusChunks, anchorAccepted);

    /// <summary>
    /// Searches deterministic anchors out to an explicit chunk radius. Normal
    /// reservations pass the profile exclusion radius; landmark conflict
    /// arbitration may pass a larger radius (for example the sum of two landmark
    /// radii) without changing either landmark's actual exclusion territory.
    /// Search cells beyond the representable native-instance coordinate domain are
    /// ignored rather than allowed to wrap or invalidate an otherwise valid edge key.
    /// </summary>
    internal static bool IsReserved(
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey key,
        Profile profile,
        int searchRadiusChunks,
        Func<UnderworldInstanceChunkKey, bool> anchorAccepted)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (anchorAccepted is null) throw new ArgumentNullException(nameof(anchorAccepted));
        if (searchRadiusChunks < 0) throw new ArgumentOutOfRangeException(nameof(searchRadiusChunks));
        var cellX = FloorDiv(key.X, profile.CellSizeChunks);
        var cellZ = FloorDiv(key.Z, profile.CellSizeChunks);

        var cellRadiusLong = 1L + (long)searchRadiusChunks / profile.CellSizeChunks;
        if (cellRadiusLong > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(searchRadiusChunks), "Landmark reservation search radius exceeds supported native chunk range.");
        var cellRadius = (int)cellRadiusLong;

        for (long dz = -cellRadius; dz <= cellRadius; dz++)
        for (long dx = -cellRadius; dx <= cellRadius; dx++)
        {
            if (!TryCellOffset(cellX, dx, out var searchCellX) ||
                !TryCellOffset(cellZ, dz, out var searchCellZ))
                continue;

            if (!TryAnchorForCell(identity, searchCellX, searchCellZ, profile, out var anchor))
                continue;

            if (ChebyshevDistance(anchor, key) <= searchRadiusChunks && anchorAccepted(anchor)) return true;
        }
        return false;
    }

    private static UnderworldInstanceChunkKey AnchorForCell(UnderworldWorldIdentity identity, int cellX, int cellZ, Profile profile)
    {
        if (!TryAnchorForCell(identity, cellX, cellZ, profile, out var anchor))
            throw new ArgumentOutOfRangeException(nameof(cellX), "Landmark anchor exceeded native chunk coordinate range.");
        return anchor;
    }

    private static bool TryAnchorForCell(UnderworldWorldIdentity identity, int cellX, int cellZ, Profile profile, out UnderworldInstanceChunkKey anchor)
    {
        unchecked
        {
            var hash = (uint)(identity.DerivedSeed32 ^ profile.Salt);
            hash = Mix(hash, (uint)cellX);
            hash = Mix(hash, (uint)cellZ);
            var xOffset = (int)(hash % (uint)profile.CellSizeChunks);
            hash = Mix(hash, 0x9E3779B9u);
            var zOffset = (int)(hash % (uint)profile.CellSizeChunks);
            if (!TryAnchorCoordinate(cellX, profile.CellSizeChunks, xOffset, out var x) ||
                !TryAnchorCoordinate(cellZ, profile.CellSizeChunks, zOffset, out var z))
            {
                anchor = default;
                return false;
            }
            anchor = new UnderworldInstanceChunkKey(x, z);
            return true;
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

    private static bool TryCellOffset(int cell, long offset, out int result)
    {
        var value = (long)cell + offset;
        if (value < int.MinValue || value > int.MaxValue)
        {
            result = default;
            return false;
        }
        result = (int)value;
        return true;
    }

    private static bool TryAnchorCoordinate(int cell, int cellSize, int offset, out int result)
    {
        var value = (long)cell * cellSize + offset;
        if (value < int.MinValue || value > int.MaxValue)
        {
            result = default;
            return false;
        }
        result = (int)value;
        return true;
    }

    private static long ChebyshevDistance(UnderworldInstanceChunkKey left, UnderworldInstanceChunkKey right)
    {
        var dx = Math.Abs((long)left.X - right.X);
        var dz = Math.Abs((long)left.Z - right.Z);
        return Math.Max(dx, dz);
    }
}
