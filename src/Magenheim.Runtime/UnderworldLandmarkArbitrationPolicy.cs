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
    /// at its anchor. Only biome-valid competing anchors whose reservation reaches
    /// the candidate participate. The lower unsigned family salt wins; Kind is a
    /// stable final tie-breaker so registration order can never affect placement.
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

        if (!candidate.Eligible(identity, candidateAnchor) ||
            !anchorMatchesBiome(candidateAnchor, candidate.Biome))
            return false;

        foreach (var family in families)
        {
            if (family is not IUnderworldLandmarkStructureFamily competitor ||
                ReferenceEquals(competitor, candidate))
                continue;

            // A competitor matters only when one of its real, biome-valid anchors
            // reserves the candidate anchor. IsReserved enumerates the competitor's
            // deterministic cells, so differently sized landmark grids still
            // arbitrate through the same authority.
            var overlaps = UnderworldLandmarkReservationPolicy.IsReserved(
                identity,
                candidateAnchor,
                competitor.ReservationProfile,
                anchor => competitor.Eligible(identity, anchor) &&
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
