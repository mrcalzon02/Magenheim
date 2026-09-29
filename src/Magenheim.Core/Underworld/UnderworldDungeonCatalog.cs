using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldDungeonStatus
{
    Planned,
    RuntimeReady,
}

/// <summary>
/// Durable biome-dungeon placement/production authority. A catalog entry is not permission to
/// register a dead entrance: runtime admission still requires a real interior binder.
/// </summary>
public sealed record UnderworldDungeonDefinition(
    string Id,
    string DisplayName,
    string PrefabName,
    UnderworldTerrainBiome Biome,
    UnderworldDungeonStatus Status,
    int Quantity,
    double ExteriorRadiusMeters,
    double MinDistanceFromSimilarMeters,
    double MaxTerrainDeltaMeters,
    int TargetRoomFamilyMinimum,
    int TargetRoomFamilyMaximum,
    int MinimumRoomFamilyUses,
    int MaximumRoomFamilyUses)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) ||
            !Id.StartsWith("magenheim.underworld.dungeon.", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Underworld dungeon ids must use the 'magenheim.underworld.dungeon.' namespace.");
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new InvalidOperationException("Underworld dungeon display name is required.");
        if (string.IsNullOrWhiteSpace(PrefabName) ||
            !PrefabName.StartsWith("Magenheim_Underworld_Dungeon_", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Underworld dungeon prefab identities must use the Magenheim_Underworld_Dungeon_ namespace.");
        if (!Enum.IsDefined(typeof(UnderworldTerrainBiome), Biome))
            throw new InvalidOperationException($"Unknown Underworld dungeon biome '{Biome}'.");
        if (!Enum.IsDefined(typeof(UnderworldDungeonStatus), Status))
            throw new InvalidOperationException($"Unknown Underworld dungeon status '{Status}'.");
        if (Quantity < 1 || Quantity > 32)
            throw new InvalidOperationException("Underworld dungeon quantity must be between one and 32.");
        FinitePositive(ExteriorRadiusMeters, nameof(ExteriorRadiusMeters));
        FiniteNonNegative(MinDistanceFromSimilarMeters, nameof(MinDistanceFromSimilarMeters));
        FiniteNonNegative(MaxTerrainDeltaMeters, nameof(MaxTerrainDeltaMeters));

        if (TargetRoomFamilyMinimum < 1 ||
            TargetRoomFamilyMaximum < TargetRoomFamilyMinimum)
            throw new InvalidOperationException("Underworld dungeon room-family target is invalid.");
        if (MinimumRoomFamilyUses < 1 ||
            MaximumRoomFamilyUses < MinimumRoomFamilyUses)
            throw new InvalidOperationException("Underworld dungeon room-family reuse target is invalid.");
    }

    private static void FinitePositive(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            throw new InvalidOperationException(name + " must be finite and greater than zero.");
    }

    private static void FiniteNonNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            throw new InvalidOperationException(name + " must be finite and non-negative.");
    }
}

public static class UnderworldDungeonCatalog
{
    // Working identities for the five new biome dungeon programs. Their names may be refined with
    // the authored room kits; ids/prefabs are the stable code-facing identities.
    public static UnderworldDungeonDefinition FungalForest { get; } = Planned(
        "fungal_forest", "Fungal Forest Dungeon",
        UnderworldTerrainBiome.FungalForest, quantity: 8, spacing: 900d);

    public static UnderworldDungeonDefinition BlackwaterDeep { get; } = Planned(
        "blackwater_deep", "Blackwater Deep Dungeon",
        UnderworldTerrainBiome.BlackwaterDeep, quantity: 7, spacing: 1050d);

    public static UnderworldDungeonDefinition SulfurousWastes { get; } = Planned(
        "sulfurous_wastes", "Sulfurous Wastes Dungeon",
        UnderworldTerrainBiome.SulfurousWastes, quantity: 7, spacing: 1050d);

    public static UnderworldDungeonDefinition FrozenCaverns { get; } = Planned(
        "frozen_caverns", "Frozen Caverns Dungeon",
        UnderworldTerrainBiome.FrozenCaverns, quantity: 7, spacing: 1100d);

    /// <summary>
    /// The established Deep Fracture is the first admitted Underworld dungeon. It keeps its own
    /// exact-plan 20-district expedition rather than being forced into the reusable-biome-kit rule.
    /// </summary>
    public static UnderworldDungeonDefinition DeepFracture { get; } = new(
        Id: "magenheim.underworld.dungeon.deep_fracture",
        DisplayName: "Deep Fracture",
        PrefabName: "Magenheim_Underworld_Dungeon_DeepFracture",
        Biome: UnderworldTerrainBiome.FractureZones,
        Status: UnderworldDungeonStatus.RuntimeReady,
        Quantity: 6,
        ExteriorRadiusMeters: 18d,
        MinDistanceFromSimilarMeters: 1400d,
        MaxTerrainDeltaMeters: 42d,
        TargetRoomFamilyMinimum: 20,
        TargetRoomFamilyMaximum: 20,
        MinimumRoomFamilyUses: 1,
        MaximumRoomFamilyUses: 1);

    public static UnderworldDungeonDefinition GreatDecay { get; } = Planned(
        "great_decay", "Great Decay Dungeon",
        UnderworldTerrainBiome.GreatDecay, quantity: 6, spacing: 1200d);

    public static IReadOnlyList<UnderworldDungeonDefinition> All { get; } =
        Array.AsReadOnly(new[]
        {
            FungalForest,
            BlackwaterDeep,
            SulfurousWastes,
            FrozenCaverns,
            DeepFracture,
            GreatDecay,
        });

    public static void Validate()
    {
        foreach (var dungeon in All) dungeon.Validate();
        if (All.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != All.Count)
            throw new InvalidOperationException("Underworld dungeon ids must be unique.");
        if (All.Select(value => value.PrefabName).Distinct(StringComparer.Ordinal).Count() != All.Count)
            throw new InvalidOperationException("Underworld dungeon prefab identities must be unique.");
        if (All.Select(value => value.Biome).Distinct().Count() != 6)
            throw new InvalidOperationException("Underworld dungeon catalog must cover all six canonical biomes exactly once.");
        if (All.Count(value => value.Status == UnderworldDungeonStatus.RuntimeReady) != 1 ||
            DeepFracture.Status != UnderworldDungeonStatus.RuntimeReady)
            throw new InvalidOperationException(
                "Deep Fracture must remain the sole runtime-ready dungeon until another real interior binder is admitted.");
    }

    private static UnderworldDungeonDefinition Planned(
        string suffix,
        string displayName,
        UnderworldTerrainBiome biome,
        int quantity,
        double spacing) =>
        new(
            Id: "magenheim.underworld.dungeon." + suffix,
            DisplayName: displayName,
            PrefabName: "Magenheim_Underworld_Dungeon_" + Pascal(suffix),
            Biome: biome,
            Status: UnderworldDungeonStatus.Planned,
            Quantity: quantity,
            ExteriorRadiusMeters: 18d,
            MinDistanceFromSimilarMeters: spacing,
            MaxTerrainDeltaMeters: 36d,
            TargetRoomFamilyMinimum: 15,
            TargetRoomFamilyMaximum: 20,
            MinimumRoomFamilyUses: 2,
            MaximumRoomFamilyUses: 3);

    private static string Pascal(string value) =>
        string.Concat(value.Split('_')
            .Where(part => part.Length > 0)
            .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
}
