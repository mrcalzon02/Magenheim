using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Short-lived per-player local atmosphere override used by authored interior spaces. This never
/// chooses progression or persists state. Trigger volumes refresh it while occupied; expiry returns
/// the player to the normal synchronized biome weather automatically.
/// </summary>
internal static class UnderworldLocalWeatherOverride
{
    internal readonly record struct State(
        UnderworldTerrainBiome Biome,
        UnderworldAtmosphereEvent Event,
        double Intensity01,
        double HazardFloor01,
        float ExpiresAt);

    private static readonly Dictionary<long, State> Active = new();

    internal static void Refresh(
        Player player,
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent,
        double intensity01,
        double hazardFloor01,
        float holdSeconds)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));
        if (!UnderworldAtmosphere.EventApplies(biome, atmosphereEvent))
            throw new InvalidOperationException(
                $"Local atmosphere event {atmosphereEvent} does not apply to {biome}.");
        if (!Finite(intensity01) || !Finite(hazardFloor01) ||
            intensity01 < 0d || intensity01 > 1d ||
            hazardFloor01 < 0d || hazardFloor01 > 1d)
            throw new ArgumentOutOfRangeException(nameof(intensity01));
        if (float.IsNaN(holdSeconds) || float.IsInfinity(holdSeconds) || holdSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(holdSeconds));

        Active[player.GetPlayerID()] = new State(
            biome,
            atmosphereEvent,
            intensity01,
            hazardFloor01,
            Time.unscaledTime + holdSeconds);
    }

    internal static bool TryResolve(
        Player player,
        UnderworldTerrainBiome biome,
        out State state)
    {
        state = default;
        if (player is null) return false;
        var id = player.GetPlayerID();
        if (!Active.TryGetValue(id, out var candidate))
            return false;
        if (Time.unscaledTime > candidate.ExpiresAt)
        {
            Active.Remove(id);
            return false;
        }
        if (candidate.Biome != biome) return false;
        state = candidate;
        return true;
    }

    internal static void Clear(Player player)
    {
        if (player is not null) Active.Remove(player.GetPlayerID());
    }

    internal static void ClearAll() => Active.Clear();

    private static bool Finite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}
