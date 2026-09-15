using System;
using System.Collections.Generic;

namespace Magenheim.Core.DarkThrone;

/// <summary>
/// Shared definition authority for crystal ecology population policy. Runtime registrars resolve
/// prefab prefixes against the live Magenheim creature registry instead of duplicating creature prefabs.
/// </summary>
public sealed record CrystalCreatureSpawnProfile(
    string Id,
    IReadOnlyList<string> PrefabPrefixes,
    int MaximumAlive,
    double SpawnRadius,
    double RespawnSeconds)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException("Crystal creature spawn profile requires an identity.");
        if (PrefabPrefixes is null || PrefabPrefixes.Count == 0)
            throw new InvalidOperationException($"Crystal creature spawn profile '{Id}' requires at least one prefab family.");
        foreach (var prefix in PrefabPrefixes)
            if (string.IsNullOrWhiteSpace(prefix))
                throw new InvalidOperationException($"Crystal creature spawn profile '{Id}' contains an empty prefab family.");
        if (MaximumAlive < 1)
            throw new InvalidOperationException($"Crystal creature spawn profile '{Id}' requires a positive population cap.");
        if (double.IsNaN(SpawnRadius) || double.IsInfinity(SpawnRadius) || SpawnRadius <= 0d)
            throw new InvalidOperationException($"Crystal creature spawn profile '{Id}' requires a finite positive spawn radius.");
        if (double.IsNaN(RespawnSeconds) || double.IsInfinity(RespawnSeconds) || RespawnSeconds < 1d)
            throw new InvalidOperationException($"Crystal creature spawn profile '{Id}' requires a finite respawn interval of at least one second.");
    }
}

public static class CrystalCreatureSpawnProfiles
{
    public static CrystalCreatureSpawnProfile DarkThroneLesser { get; } = new(
        "DarkThroneLesser",
        new[]
        {
            "Magenheim_AnnoyanceWisp_",
            "Magenheim_GeodeCrawler_",
            "Magenheim_Shardling_",
            "Magenheim_CrystalParasite_",
            "Magenheim_CrystalHound_",
        },
        MaximumAlive: 2,
        SpawnRadius: 8d,
        RespawnSeconds: 105d);

    public static CrystalCreatureSpawnProfile DarkThroneGuardian { get; } = new(
        "DarkThroneGuardian",
        new[]
        {
            "Magenheim_CrystalRevenant_",
            "Magenheim_FacetSentry_",
            "Magenheim_StoneSentinel_",
            "Magenheim_StoneGuardian_",
            "Magenheim_CrystalGolem_",
        },
        MaximumAlive: 1,
        SpawnRadius: 5d,
        RespawnSeconds: 150d);
}
