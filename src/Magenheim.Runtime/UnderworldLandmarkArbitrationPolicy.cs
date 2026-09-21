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
    /// The lower unsigned family salt wins; Kind is the stable final tie-breaker.
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
            if (family is not IUnderworldLandmarkStructureFamily competitor ||
                ReferenceEquals(competitor, candidate))
                continue;

            var combinedRadiusLong = (long)candidate.ReservationProfile.ExclusionRadiusChunks +
                                     competitor.ReservationProfile.ExclusionRadiusChunks;
            if (combinedRadiusLong > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(families), "Combined landmark exclusion radius exceeds supported native chunk range.");
            var combinedRadius = (int)combinedRadiusLong;

            var overlaps = UnderworldLandmarkReservationPolicy.IsReserved(
                identity,
                candidateAnchor,
                competitor.ReservationProfile,
                combinedRadius,
                anchor => UnderworldLandmarkReservationPolicy.IsAnchor(identity, anchor, competitor.ReservationProfile) &&
                          competitor.Eligible(identity, anchor) &&
                          anchorMatchesBiome(anchor, competitor.Biome));
            if (!overlaps) continue;

            if (HasPriority(competitor, candidate)) return false;
        }

        return true;
    }

    private static bool HasPriority(
        IUnderworldLandmarkStructureFamily left,
        IUnderworldLandmarkStructureFamily right)
    {
        var leftSalt = unchecked((uint)left.ReservationProfile.Salt);
        var rightSalt = unchecked((uint)right.ReservationProfile.Salt);
        if (leftSalt != rightSalt) return leftSalt < rightSalt;
        return string.CompareOrdinal(left.Kind, right.Kind) < 0;
    }
}
