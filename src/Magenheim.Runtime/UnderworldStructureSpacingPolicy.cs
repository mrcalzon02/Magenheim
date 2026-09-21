using System;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal static class UnderworldStructureSpacingPolicy
{
    internal static bool IsLocalWinner(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey candidate, int familySalt, int radiusChunks, Func<UnderworldInstanceChunkKey, bool> baseEligible)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (baseEligible is null) throw new ArgumentNullException(nameof(baseEligible));
        if (radiusChunks < 0) throw new ArgumentOutOfRangeException(nameof(radiusChunks));
        if (!baseEligible(candidate)) return false;

        var candidatePriority = Priority(identity.DerivedSeed32, candidate, familySalt);
        for (var dz = -radiusChunks; dz <= radiusChunks; dz++)
        for (var dx = -radiusChunks; dx <= radiusChunks; dx++)
        {
            if (dx == 0 && dz == 0) continue;
            var neighbor = new UnderworldInstanceChunkKey(candidate.X + dx, candidate.Z + dz);
            if (!baseEligible(neighbor)) continue;
            var neighborPriority = Priority(identity.DerivedSeed32, neighbor, familySalt);
            if (neighborPriority < candidatePriority || (neighborPriority == candidatePriority && ComesBefore(neighbor, candidate))) return false;
        }
        return true;
    }

    internal static bool IsLocalWinnerAcrossFamilies(
        UnderworldWorldIdentity identity,
        UnderworldInstanceChunkKey candidate,
        int candidateFamilySalt,
        int radiusChunks,
        int[] competingFamilySalts,
        Func<UnderworldInstanceChunkKey, int, bool> baseEligible)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (competingFamilySalts is null) throw new ArgumentNullException(nameof(competingFamilySalts));
        if (baseEligible is null) throw new ArgumentNullException(nameof(baseEligible));
        if (radiusChunks < 0) throw new ArgumentOutOfRangeException(nameof(radiusChunks));
        if (!baseEligible(candidate, candidateFamilySalt)) return false;

        var candidatePriority = Priority(identity.DerivedSeed32, candidate, candidateFamilySalt);
        foreach (var familySalt in competingFamilySalts)
        {
            for (var dz = -radiusChunks; dz <= radiusChunks; dz++)
            for (var dx = -radiusChunks; dx <= radiusChunks; dx++)
            {
                if (familySalt == candidateFamilySalt && dx == 0 && dz == 0) continue;
                var neighbor = new UnderworldInstanceChunkKey(candidate.X + dx, candidate.Z + dz);
                if (!baseEligible(neighbor, familySalt)) continue;
                var neighborPriority = Priority(identity.DerivedSeed32, neighbor, familySalt);
                if (neighborPriority < candidatePriority) return false;
                if (neighborPriority == candidatePriority)
                {
                    if (familySalt < candidateFamilySalt) return false;
                    if (familySalt == candidateFamilySalt && ComesBefore(neighbor, candidate)) return false;
                }
            }
        }
        return true;
    }

    private static uint Priority(int seed, UnderworldInstanceChunkKey key, int salt)
    {
        unchecked
        {
            uint hash = (uint)(seed ^ salt);
            hash = hash * 397u ^ (uint)key.X;
            hash = hash * 397u ^ (uint)key.Z;
            hash ^= hash >> 16;
            return hash;
        }
    }

    private static bool ComesBefore(UnderworldInstanceChunkKey left, UnderworldInstanceChunkKey right) => left.X < right.X || (left.X == right.X && left.Z < right.Z);
}
