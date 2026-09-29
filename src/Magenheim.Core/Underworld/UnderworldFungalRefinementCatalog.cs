using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldFungalRefinementCost(string Prefab, int Amount);

public sealed record UnderworldFungalRefinementDefinition(
    string Prefab,
    string Name,
    string DonorPrefab,
    int OutputAmount,
    IReadOnlyList<UnderworldFungalRefinementCost> Costs);

/// <summary>
/// The first Underworld refining rung. These identities sit between Fungal Forest raw ecology and
/// finished equipment so the biome has a real gather -> station -> refine -> craft dependency tree.
/// </summary>
public static class UnderworldFungalRefinementCatalog
{
    public const string WorldrootPlank = "Magenheim_Underworld_Refined_WorldrootPlank";
    public const string SpireCord = "Magenheim_Underworld_Refined_SpireCord";
    public const string CuredGlowcap = "Magenheim_Underworld_Refined_CuredGlowcap";

    public static IReadOnlyList<UnderworldFungalRefinementDefinition> All { get; } =
        Array.AsReadOnly(new[]
        {
            Definition(
                WorldrootPlank, "Worldroot Plank", "FineWood", 2,
                Cost("Magenheim_Underworld_Resource_WorldrootTimber", 1)),
            Definition(
                SpireCord, "Spire Cord", "LinenThread", 1,
                Cost("Magenheim_Underworld_Resource_SpireFibre", 3)),
            Definition(
                CuredGlowcap, "Cured Glowcap", "MushroomMagecap", 1,
                Cost("Magenheim_Underworld_Resource_GlowcapFlesh", 2),
                Cost("Magenheim_Underworld_Resource_SpireFibre", 1)),
        });

    private static UnderworldFungalRefinementDefinition Definition(
        string prefab,
        string name,
        string donorPrefab,
        int outputAmount,
        params UnderworldFungalRefinementCost[] costs) =>
        new(prefab, name, donorPrefab, outputAmount, Array.AsReadOnly(costs));

    private static UnderworldFungalRefinementCost Cost(string prefab, int amount) =>
        new(prefab, amount);
}
