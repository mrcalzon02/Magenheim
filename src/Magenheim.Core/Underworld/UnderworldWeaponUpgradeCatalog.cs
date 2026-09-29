using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldWeaponAccent
{
    Worldroot,
    Flowstone,
    Emberiron,
    Rime,
    Fracture,
    Decay,
}

public sealed record UnderworldWeaponIngredient(string Prefab, int Amount);

public sealed record UnderworldWeaponUpgradeDefinition(
    string Prefab,
    string Name,
    string ModelId,
    string BaseModelId,
    UnderworldTerrainBiome Biome,
    string BasePrefab,
    string StationPrefab,
    UnderworldWeaponAccent Accent,
    IReadOnlyList<UnderworldWeaponIngredient> Ingredients);

/// <summary>
/// Canonical Underworld weapon ancestry. Every Underworld weapon is an upgrade of a
/// Magenheim crystal-grade weapon or crystal-grade staff; no tier may bypass the
/// pre-Underworld crystal progression by cloning a vanilla weapon directly.
/// </summary>
public static class UnderworldWeaponUpgradeCatalog
{
    public const string MycelialBenchPrefab = UnderworldStationCatalog.MycelialBenchPrefab;
    public const string TidalBasinPrefab = UnderworldStationCatalog.TidalBasinPrefab;
    public const string FurnaceHeartForgePrefab = UnderworldStationCatalog.FurnaceHeartForgePrefab;
    public const string SilenceTablePrefab = UnderworldStationCatalog.SilenceTablePrefab;
    public const string AnchorForgePrefab = UnderworldStationCatalog.AnchorForgePrefab;
    public const string CrownReliquaryPrefab = UnderworldStationCatalog.CrownReliquaryPrefab;

    public static IReadOnlyList<UnderworldWeaponUpgradeDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        Upgrade("Magenheim_Underworld_Weapon_WorldrootClub", "Worldroot Club", "underworld-weapon-worldroot-club", "crystal-weapon-mace",
            UnderworldTerrainBiome.FungalForest, "Magenheim_Weapon_CrystalMace", MycelialBenchPrefab,
            UnderworldWeaponAccent.Worldroot,
            Ingredient(UnderworldFungalRefinementCatalog.WorldrootPlank, 8),
            Ingredient("Magenheim_Underworld_Resource_Understone", 4),
            Ingredient(UnderworldFungalRefinementCatalog.SpireCord, 2)),

        Upgrade("Magenheim_Underworld_Weapon_WorldrootBow", "Worldroot Bow", "underworld-weapon-worldroot-bow", "crystal-weapon-bow",
            UnderworldTerrainBiome.FungalForest, "Magenheim_Weapon_CrystalBow", MycelialBenchPrefab,
            UnderworldWeaponAccent.Worldroot,
            Ingredient(UnderworldFungalRefinementCatalog.WorldrootPlank, 6),
            Ingredient(UnderworldFungalRefinementCatalog.SpireCord, 6),
            Ingredient(UnderworldFungalRefinementCatalog.CuredGlowcap, 1)),

        Upgrade("Magenheim_Underworld_Weapon_FlowstoneMaul", "Flowstone Maul", "underworld-weapon-flowstone-maul", "crystal-weapon-mace",
            UnderworldTerrainBiome.BlackwaterDeep, "Magenheim_Weapon_CrystalMace", TidalBasinPrefab,
            UnderworldWeaponAccent.Flowstone,
            Ingredient("Magenheim_Underworld_Resource_BlackwaterFlowstone", 14),
            Ingredient("Magenheim_Underworld_Resource_BlackwaterPearl", 4),
            Ingredient("Magenheim_Underworld_Resource_PaleFibre", 6)),

        Upgrade("Magenheim_Underworld_Weapon_BlackwaterHarpoon", "Blackwater Harpoon", "underworld-weapon-blackwater-harpoon", "crystal-weapon-spear",
            UnderworldTerrainBiome.BlackwaterDeep, "Magenheim_Weapon_CrystalSpear", TidalBasinPrefab,
            UnderworldWeaponAccent.Flowstone,
            Ingredient("Magenheim_Underworld_Resource_BlackwaterFlowstone", 8),
            Ingredient("Magenheim_Underworld_Resource_BlackwaterPearl", 2),
            Ingredient("Magenheim_Underworld_Resource_PaleFibre", 8),
            Ingredient("Magenheim_Underworld_Resource_DeepSalt", 2)),

        Upgrade("Magenheim_Underworld_Weapon_EmberironAxe", "Emberiron Axe", "underworld-weapon-emberiron-axe", "crystal-weapon-axe",
            UnderworldTerrainBiome.SulfurousWastes, "Magenheim_Weapon_CrystalAxe", FurnaceHeartForgePrefab,
            UnderworldWeaponAccent.Emberiron,
            Ingredient("Magenheim_Underworld_Resource_Emberiron", 12),
            Ingredient("Magenheim_Underworld_Resource_Slagstone", 8),
            Ingredient("Magenheim_Underworld_Resource_CharredTimber", 4),
            Ingredient("Magenheim_Underworld_Resource_Sulfur", 2)),

        Upgrade("Magenheim_Underworld_Weapon_EmberironGreatsword", "Emberiron Greatsword", "underworld-weapon-emberiron-greatsword", "crystal-weapon-greatsword",
            UnderworldTerrainBiome.SulfurousWastes, "Magenheim_Weapon_CrystalGreatsword", FurnaceHeartForgePrefab,
            UnderworldWeaponAccent.Emberiron,
            Ingredient("Magenheim_Underworld_Resource_Emberiron", 18),
            Ingredient("Magenheim_Underworld_Resource_Slagstone", 10),
            Ingredient("Magenheim_Underworld_Resource_CharredTimber", 6),
            Ingredient("Magenheim_Underworld_Resource_Sulfur", 3)),

        Upgrade("Magenheim_Underworld_Weapon_RimesilverSpear", "Rimesilver Spear", "underworld-weapon-rimesilver-spear", "crystal-weapon-spear",
            UnderworldTerrainBiome.FrozenCaverns, "Magenheim_Weapon_CrystalSpear", SilenceTablePrefab,
            UnderworldWeaponAccent.Rime,
            Ingredient("Magenheim_Underworld_Resource_Rimesilver", 12),
            Ingredient("Magenheim_Underworld_Resource_ClearIce", 8),
            Ingredient("Magenheim_Underworld_Resource_Rimewood", 6)),

        Upgrade("Magenheim_Underworld_Weapon_IcebindStaff", "Icebind Staff", "underworld-weapon-icebind-staff", "staff-frost-crystal",
            UnderworldTerrainBiome.FrozenCaverns, "Magenheim_Staff_Frost_Crystal", SilenceTablePrefab,
            UnderworldWeaponAccent.Rime,
            Ingredient("Magenheim_Underworld_Resource_Rimesilver", 10),
            Ingredient("Magenheim_Underworld_Resource_ClearIce", 12),
            Ingredient("Magenheim_Underworld_Resource_Rimewood", 6)),

        Upgrade("Magenheim_Underworld_Weapon_TitanboneAtgeir", "Titanbone Atgeir", "underworld-weapon-titanbone-atgeir", "crystal-weapon-atgeir",
            UnderworldTerrainBiome.FractureZones, "Magenheim_Weapon_CrystalAtgeir", AnchorForgePrefab,
            UnderworldWeaponAccent.Fracture,
            Ingredient("Magenheim_Underworld_Resource_Titanbone", 14),
            Ingredient("Magenheim_Underworld_Resource_FractureCrystal", 10),
            Ingredient("Magenheim_Underworld_Resource_Shardstone", 8)),

        Upgrade("Magenheim_Underworld_Weapon_ShardstoneCrossbow", "Shardstone Crossbow", "underworld-weapon-shardstone-crossbow", "crystal-weapon-crossbow",
            UnderworldTerrainBiome.FractureZones, "Magenheim_Weapon_CrystalCrossbow", AnchorForgePrefab,
            UnderworldWeaponAccent.Fracture,
            Ingredient("Magenheim_Underworld_Resource_Shardstone", 10),
            Ingredient("Magenheim_Underworld_Resource_FractureCrystal", 8),
            Ingredient("Magenheim_Underworld_Resource_Titanbone", 6)),

        Upgrade("Magenheim_Underworld_Weapon_AmberBlade", "Amber Blade", "underworld-weapon-amber-blade", "crystal-weapon-sword",
            UnderworldTerrainBiome.GreatDecay, "Magenheim_Weapon_CrystalSword", CrownReliquaryPrefab,
            UnderworldWeaponAccent.Decay,
            Ingredient("Magenheim_Underworld_Resource_CarrionAmber", 12),
            Ingredient("Magenheim_Underworld_Resource_Rotwood", 6),
            Ingredient("Magenheim_Underworld_Resource_DecaySpore", 4),
            Ingredient("Magenheim_Underworld_Resource_BoneGravel", 4)),

        Upgrade("Magenheim_Underworld_Weapon_CrownSceptre", "Crown Sceptre", "underworld-weapon-crown-sceptre", "crystal-weapon-mace",
            UnderworldTerrainBiome.GreatDecay, "Magenheim_Weapon_CrystalMace", CrownReliquaryPrefab,
            UnderworldWeaponAccent.Decay,
            Ingredient("Magenheim_Underworld_Resource_CarrionAmber", 14),
            Ingredient("Magenheim_Underworld_Resource_BoneGravel", 8),
            Ingredient("Magenheim_Underworld_Resource_DecaySpore", 6),
            Ingredient("Magenheim_Underworld_Resource_Rotwood", 4)),
    });

    /// <summary>
    /// The first playable economy slice. Later definitions are deliberately present now so
    /// their dependency and visual ancestry cannot drift while their stations/refining loops
    /// are still being built.
    /// </summary>
    public static IReadOnlyList<UnderworldWeaponUpgradeDefinition> FungalForestSlice { get; } =
        Array.AsReadOnly(All.Where(value => value.Biome == UnderworldTerrainBiome.FungalForest).ToArray());

    private static UnderworldWeaponUpgradeDefinition Upgrade(
        string prefab,
        string name,
        string modelId,
        string baseModelId,
        UnderworldTerrainBiome biome,
        string basePrefab,
        string stationPrefab,
        UnderworldWeaponAccent accent,
        params UnderworldWeaponIngredient[] ingredients) =>
        new(prefab, name, modelId, baseModelId, biome, basePrefab, stationPrefab, accent, Array.AsReadOnly(ingredients));

    private static UnderworldWeaponIngredient Ingredient(string prefab, int amount) => new(prefab, amount);
}
