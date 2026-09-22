using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Deterministic conflict authority for landmark families that can reserve
/// overlapping native-instance territory. This policy owns no world state and
/// does not inspect player presence or load order: callers supply the currently
/// registered landmark families and native-terrain anchor validation.
/// </summary>
internal static class UnderworldLandmarkArbitrationPolicy
{
    /// <summary>
    /// Returns true when the candidate landmark remains the deterministic winner
    /// at its anchor. A competitor participates when the two reservation territories
    /// overlap, even when neither anchor falls inside the other's smaller radius.
    /// The lower unsigned family salt wins; Kind is the stable family tie-breaker.
    /// Anchors from the same family also arbitrate against one another, using native
    /// X then Z as a final deterministic tie-breaker. This prevents adjacent sparse
    /// reservation cells from materializing overlapping copies of one landmark.
    /// </summary>
    internal static bool IsWinner(
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey candidateAnchor,
        IUnderworldLandmarkStructureFamily candidate,
        IReadOnlyList<IUnderworldBiomeStructureFamily> families,
        Func<UnderworldInstanceChunkKey, UnderworldTerrainBiome, bool> anchorMatchesBiome)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (candidate is null) throw new ArgumentNullException(nameof(candidate));
        if (families is null) throw new ArgumentNullException(nameof(families));
        if (anchorMatchesBiome is null) throw new ArgumentNullException(nameof(anchorMatchesBiome));

        if (!UnderworldLandmarkReservationPolicy.IsAnchor(identity, candidateAnchor, candidate.ReservationProfile) ||
            !candidate.Eligible(identity, candidateAnchor) ||
            !anchorMatchesBiome(candidateAnchor, candidate.Biome))
            return false;

        foreach (var family in families)
        {
            if (family is not IUnderworldLandmarkStructureFamily competitor)
                continue;

            var combinedRadiusLong = (long)candidate.ReservationProfile.ExclusionRadiusChunks +
                                     competitor.ReservationProfile.ExclusionRadiusChunks;
            if (combinedRadiusLong > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(families), "Combined landmark exclusion radius exceeds supported native chunk range.");
            var combinedRadius = (int)combinedRadiusLong;

            var blocked = UnderworldLandmarkReservationPolicy.IsReserved(
                identity,
                candidateAnchor,
                competitor.ReservationProfile,
                combinedRadius,
                anchor =>
                {
                    if (!UnderworldLandmarkReservationPolicy.IsAnchor(identity, anchor, competitor.ReservationProfile) ||
                        !competitor.Eligible(identity, anchor) ||
                        !anchorMatchesBiome(anchor, competitor.Biome))
                        return false;

                    // A family's search necessarily rediscovers the candidate itself.
                    // It is not a conflict, but another anchor from that same family is.
                    if (ReferenceEquals(competitor, candidate) && SameAnchor(anchor, candidateAnchor))
                        return false;

                    return HasPriority(competitor, anchor, candidate, candidateAnchor);
                });
            if (blocked) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true when a native chunk lies inside the exclusion territory of any
    /// landmark that actually wins arbitration. Reservation is intentionally not
    /// filtered by the queried chunk's biome: a valid landmark anchor owns its full
    /// exclusion footprint even when that footprint crosses a terrain-biome edge.
    /// </summary>
    internal static bool IsReservedByWinner(
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey key,
        IReadOnlyList<IUnderworldBiomeStructureFamily> families,
        Func<UnderworldInstanceChunkKey, UnderworldTerrainBiome, bool> anchorMatchesBiome)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (families is null) throw new ArgumentNullException(nameof(families));
        if (anchorMatchesBiome is null) throw new ArgumentNullException(nameof(anchorMatchesBiome));

        foreach (var family in families)
        {
            if (family is not IUnderworldLandmarkStructureFamily landmark) continue;
            if (UnderworldLandmarkReservationPolicy.IsReserved(
                    identity,
                    key,
                    landmark.ReservationProfile,
                    anchor => IsWinner(identity, anchor, landmark, families, anchorMatchesBiome)))
                return true;
        }

        return false;
    }

    private static bool HasPriority(
        IUnderworldLandmarkStructureFamily left,
        UnderworldInstanceChunkKey leftAnchor,
        IUnderworldLandmarkStructureFamily right,
        UnderworldInstanceChunkKey rightAnchor)
    {
        var leftSalt = unchecked((uint)left.ReservationProfile.Salt);
        var rightSalt = unchecked((uint)right.ReservationProfile.Salt);
        if (leftSalt != rightSalt) return leftSalt < rightSalt;

        var kindOrder = string.CompareOrdinal(left.Kind, right.Kind);
        if (kindOrder != 0) return kindOrder < 0;

        if (leftAnchor.X != rightAnchor.X) return leftAnchor.X < rightAnchor.X;
        return leftAnchor.Z < rightAnchor.Z;
    }

    private static bool SameAnchor(UnderworldInstanceChunkKey left, UnderworldInstanceChunkKey right) =>
        left.X == right.X && left.Z == right.Z;
}
