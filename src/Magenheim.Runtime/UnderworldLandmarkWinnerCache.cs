using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Reconciliation-scoped memoization for deterministic landmark arbitration.
/// A fresh instance is created for each residency pass; no winner state survives
/// world/instance lifecycle changes and no player/load-order state participates.
/// </summary>
internal sealed class UnderworldLandmarkWinnerCache
{
    private readonly UnderworldWorldIdentity _identity;
    private readonly IReadOnlyList<IUnderworldBiomeStructureFamily> _families;
    private readonly Func<UnderworldInstanceChunkKey, UnderworldTerrainBiome, bool> _anchorMatchesBiome;
    private readonly Dictionary<WinnerKey, bool> _winners = new();

    internal UnderworldLandmarkWinnerCache(
        UnderworldWorldIdentity identity,
        IReadOnlyList<IUnderworldBiomeStructureFamily> families,
        Func<UnderworldInstanceChunkKey, UnderworldTerrainBiome, bool> anchorMatchesBiome)
    {
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _families = families ?? throw new ArgumentNullException(nameof(families));
        _anchorMatchesBiome = anchorMatchesBiome ?? throw new ArgumentNullException(nameof(anchorMatchesBiome));
    }

    internal bool IsWinner(IUnderworldLandmarkStructureFamily landmark, UnderworldInstanceChunkKey anchor)
    {
        if (landmark is null) throw new ArgumentNullException(nameof(landmark));

        var key = new WinnerKey(landmark.Kind, anchor.X, anchor.Z);
        if (_winners.TryGetValue(key, out var winner)) return winner;

        winner = UnderworldLandmarkArbitrationPolicy.IsWinner(
            _identity,
            anchor,
            landmark,
            _families,
            _anchorMatchesBiome);
        _winners.Add(key, winner);
        return winner;
    }

    private readonly struct WinnerKey : IEquatable<WinnerKey>
    {
        private readonly string _kind;
        private readonly int _x;
        private readonly int _z;

        internal WinnerKey(string kind, int x, int z)
        {
            _kind = kind ?? throw new ArgumentNullException(nameof(kind));
            _x = x;
            _z = z;
        }

        public bool Equals(WinnerKey other) =>
            _x == other._x && _z == other._z && string.Equals(_kind, other._kind, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is WinnerKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(_kind);
                hash = (hash * 397) ^ _x;
                hash = (hash * 397) ^ _z;
                return hash;
            }
        }
    }
}
