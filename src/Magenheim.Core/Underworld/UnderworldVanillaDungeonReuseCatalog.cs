using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Authority for the five ordinary Underworld dungeons. These are intentionally expanded vanilla
/// dungeons, not Magenheim-authored room kits. Deep Fracture remains the bespoke architecture lane.
/// </summary>
public sealed record UnderworldVanillaDungeonReuseDefinition(
    string DungeonId,
    UnderworldTerrainBiome Biome,
    string DonorDisplayName,
    string DonorGeneratorPrefab,
    string DonorThemeName,
    IReadOnlyList<string> DonorEntrancePrefabs,
    double LinearRoomScale,
    double RoomCountMultiplier,
    bool ReplaceVanillaEnemies,
    bool ReplaceVanillaLoot,
    bool UseBiomeResources,
    bool AllowMagenheimAuthoredRoomInjection)
{
    public int ExpandedMinimumRooms(int vanillaMinimum) =>
        Math.Max(vanillaMinimum + 1, (int)Math.Ceiling(vanillaMinimum * RoomCountMultiplier));

    public int ExpandedMaximumRooms(int vanillaMaximum) =>
        Math.Max(vanillaMaximum + 1, (int)Math.Ceiling(vanillaMaximum * RoomCountMultiplier));

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DungeonId) ||
            !DungeonId.StartsWith("magenheim.underworld.dungeon.", StringComparison.Ordinal))
            throw new InvalidOperationException("Vanilla-reuse dungeon id is invalid.");
        if (!Enum.IsDefined(typeof(UnderworldTerrainBiome), Biome))
            throw new InvalidOperationException($"Unknown vanilla-reuse biome '{Biome}'.");
        if (string.IsNullOrWhiteSpace(DonorDisplayName) ||
            string.IsNullOrWhiteSpace(DonorGeneratorPrefab) ||
            string.IsNullOrWhiteSpace(DonorThemeName))
            throw new InvalidOperationException("Vanilla-reuse donor identity is incomplete.");
        if (DonorEntrancePrefabs is null ||
            DonorEntrancePrefabs.Count == 0 ||
            DonorEntrancePrefabs.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Vanilla-reuse dungeon requires at least one donor entrance prefab.");
        if (DonorEntrancePrefabs.Distinct(StringComparer.Ordinal).Count() != DonorEntrancePrefabs.Count)
            throw new InvalidOperationException("Vanilla-reuse entrance prefab identities must be unique.");
        if (double.IsNaN(LinearRoomScale) || double.IsInfinity(LinearRoomScale) || LinearRoomScale < 1.5d)
            throw new InvalidOperationException("Ordinary Underworld donor rooms must be scaled to at least 1.5x linear size.");
        if (double.IsNaN(RoomCountMultiplier) || double.IsInfinity(RoomCountMultiplier) || RoomCountMultiplier < 3.5d)
            throw new InvalidOperationException("Ordinary Underworld dungeons must target at least 3.5x the donor room count.");
        if (!ReplaceVanillaEnemies || !ReplaceVanillaLoot || !UseBiomeResources)
            throw new InvalidOperationException(
                "Ordinary Underworld dungeons must replace vanilla encounters/loot with Magenheim biome ecology.");
        if (AllowMagenheimAuthoredRoomInjection)
            throw new InvalidOperationException(
                "Ordinary Underworld dungeons may not mix bespoke Magenheim room architecture into the vanilla donor tileset.");
    }
}

public static class UnderworldVanillaDungeonReuseCatalog
{
    public const double MinimumLinearRoomScale = 1.5d;
    public const double MinimumRoomCountMultiplier = 3.5d;

    public static UnderworldVanillaDungeonReuseDefinition FungalForest { get; } = Definition(
        UnderworldDungeonCatalog.FungalForest,
        "Burial Chambers",
        "DG_ForestCrypt",
        "ForestCrypt",
        "Crypt2", "Crypt3", "Crypt4");

    public static UnderworldVanillaDungeonReuseDefinition BlackwaterDeep { get; } = Definition(
        UnderworldDungeonCatalog.BlackwaterDeep,
        "Sunken Crypts",
        "DG_SunkenCrypt",
        "SunkenCrypt",
        "SunkenCrypt4");

    public static UnderworldVanillaDungeonReuseDefinition SulfurousWastes { get; } = Definition(
        UnderworldDungeonCatalog.SulfurousWastes,
        "Infested Mines",
        "DG_DvergrTown",
        "DvergerTown",
        "Mistlands_DvergrTownEntrance1",
        "Mistlands_DvergrTownEntrance2");

    public static UnderworldVanillaDungeonReuseDefinition FrozenCaverns { get; } = Definition(
        UnderworldDungeonCatalog.FrozenCaverns,
        "Frost Caves",
        "DG_Cave",
        "Cave",
        "MountainCave02");

    public static UnderworldVanillaDungeonReuseDefinition GreatDecay { get; } = Definition(
        UnderworldDungeonCatalog.GreatDecay,
        "Winding Tunnels",
        "DG_Hole",
        "Hole",
        "TheHole01");

    public static IReadOnlyList<UnderworldVanillaDungeonReuseDefinition> All { get; } =
        Array.AsReadOnly(new[]
        {
            FungalForest,
            BlackwaterDeep,
            SulfurousWastes,
            FrozenCaverns,
            GreatDecay,
        });

    public static void Validate()
    {
        foreach (var definition in All) definition.Validate();

        if (All.Count != 5)
            throw new InvalidOperationException("Exactly five ordinary Underworld vanilla-reuse dungeon definitions are required.");
        if (All.Select(value => value.DungeonId).Distinct(StringComparer.Ordinal).Count() != All.Count)
            throw new InvalidOperationException("Vanilla-reuse dungeon ids must be unique.");
        if (All.Select(value => value.Biome).Distinct().Count() != All.Count)
            throw new InvalidOperationException("Vanilla-reuse dungeon biomes must be unique.");
        if (All.Select(value => value.DonorGeneratorPrefab).Distinct(StringComparer.Ordinal).Count() != All.Count)
            throw new InvalidOperationException(
                "Each ordinary Underworld biome must currently reuse a distinct vanilla dungeon generator family.");

        var ordinaryIds = UnderworldDungeonCatalog.All
            .Where(value => !string.Equals(
                value.Id,
                UnderworldDungeonCatalog.DeepFracture.Id,
                StringComparison.Ordinal))
            .Select(value => value.Id)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var reuseIds = All
            .Select(value => value.DungeonId)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (!ordinaryIds.SequenceEqual(reuseIds, StringComparer.Ordinal))
            throw new InvalidOperationException(
                "Every ordinary Underworld dungeon must map exactly once to the vanilla-reuse authority.");
    }

    public static UnderworldVanillaDungeonReuseDefinition Require(string dungeonId)
    {
        var match = All.SingleOrDefault(value =>
            string.Equals(value.DungeonId, dungeonId, StringComparison.Ordinal));
        return match ?? throw new InvalidOperationException(
            $"Underworld dungeon '{dungeonId}' has no vanilla-reuse definition.");
    }

    private static UnderworldVanillaDungeonReuseDefinition Definition(
        UnderworldDungeonDefinition dungeon,
        string donorDisplayName,
        string generator,
        string theme,
        params string[] entrances) =>
        new(
            dungeon.Id,
            dungeon.Biome,
            donorDisplayName,
            generator,
            theme,
            Array.AsReadOnly(entrances),
            MinimumLinearRoomScale,
            MinimumRoomCountMultiplier,
            ReplaceVanillaEnemies: true,
            ReplaceVanillaLoot: true,
            UseBiomeResources: true,
            AllowMagenheimAuthoredRoomInjection: false);
}
