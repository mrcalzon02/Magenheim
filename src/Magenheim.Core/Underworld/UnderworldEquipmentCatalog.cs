using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldEquipmentSlot
{
    Tool,
    Helmet,
    Chest,
    Legs,
    Cape,
}

public sealed record UnderworldEquipmentCost(string Prefab, int Amount);

public sealed record UnderworldEquipmentDefinition(
    string Prefab,
    string Name,
    string ModelId,
    UnderworldTerrainBiome Biome,
    UnderworldEquipmentSlot Slot,
    string StationPrefab,
    string GameplayRole,
    IReadOnlyList<UnderworldEquipmentCost> Costs);

/// <summary>
/// Stable equipment identities for the six complete Underworld biome tiers.
/// This is gameplay authority only: wearable runtime admission must not occur until an owned,
/// correctly skinned attach_skin asset exists for the armour piece.
/// </summary>
public static class UnderworldEquipmentCatalog
{
    public static IReadOnlyList<UnderworldEquipmentDefinition> Tools { get; } = Array.AsReadOnly(new[]
    {
        Tool("SporelightLantern", "Sporelight Lantern", "underworld-tool-sporelight-lantern",
            UnderworldTerrainBiome.FungalForest, UnderworldStationCatalog.MycelialBenchPrefab,
            "Fuel-free carried/placed light whose output is reduced by heavy spore density.",
            Cost(UnderworldFungalRefinementCatalog.WorldrootPlank, 4),
            Cost(UnderworldFungalRefinementCatalog.SpireCord, 4),
            Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 2),
            Cost("Magenheim_Underworld_Resource_Understone", 2)),

        WearableTool("DivingBellHood", "Diving Bell Hood", "underworld-tool-diving-bell-hood",
            UnderworldTerrainBiome.BlackwaterDeep, UnderworldStationCatalog.TidalBasinPrefab,
            "Traversal hood that extends underwater operating time in Blackwater Deep.",
            Cost("Magenheim_Underworld_Resource_BlackwaterFlowstone", 8),
            Cost("Magenheim_Underworld_Resource_BlackwaterPearl", 4),
            Cost("Magenheim_Underworld_Resource_PaleFibre", 10),
            Cost("Magenheim_Underworld_Resource_DeepSalt", 2)),

        Tool("SlagPick", "Slag Pick", "underworld-tool-slag-pick",
            UnderworldTerrainBiome.SulfurousWastes, UnderworldStationCatalog.FurnaceHeartForgePrefab,
            "Heat-cured mining tool intended to open Fracture Zone seams rather than merely mine faster.",
            Cost("Magenheim_Underworld_Resource_Emberiron", 14),
            Cost("Magenheim_Underworld_Resource_Slagstone", 8),
            Cost("Magenheim_Underworld_Resource_CharredTimber", 4),
            Cost("Magenheim_Underworld_Resource_Sulfur", 2)),

        Tool("RimeChisel", "Rime Chisel", "underworld-tool-rime-chisel",
            UnderworldTerrainBiome.FrozenCaverns, UnderworldStationCatalog.SilenceTablePrefab,
            "Precision harvesting tool intended to cut Clear Ice without shattering the deposit.",
            Cost("Magenheim_Underworld_Resource_Rimesilver", 8),
            Cost("Magenheim_Underworld_Resource_ClearIce", 6),
            Cost("Magenheim_Underworld_Resource_Rimewood", 4)),

        Tool("AnchorSpike", "Anchor Spike", "underworld-tool-anchor-spike",
            UnderworldTerrainBiome.FractureZones, UnderworldStationCatalog.AnchorForgePrefab,
            "Heavy deployable anchor intended to stabilize a local patch of tremor-prone ground.",
            Cost("Magenheim_Underworld_Resource_Titanbone", 8),
            Cost("Magenheim_Underworld_Resource_Shardstone", 12),
            Cost("Magenheim_Underworld_Resource_FractureCrystal", 6)),

        Tool("DefiantCenser", "Defiant Censer", "underworld-tool-defiant-censer",
            UnderworldTerrainBiome.GreatDecay, UnderworldStationCatalog.CrownReliquaryPrefab,
            "Carried reliquary intended to suppress contamination locally and make Great Decay habitation possible.",
            Cost("Magenheim_Underworld_Resource_CarrionAmber", 8),
            Cost("Magenheim_Underworld_Resource_Rotwood", 6),
            Cost("Magenheim_Underworld_Resource_DecaySpore", 4),
            Cost("Magenheim_Underworld_Resource_BoneGravel", 4)),
    });

    public static IReadOnlyList<UnderworldEquipmentDefinition> Armour { get; } = Array.AsReadOnly(new[]
    {
        ArmourPiece("Sporeweave", "Sporeweave", "sporeweave", UnderworldTerrainBiome.FungalForest,
            UnderworldStationCatalog.MycelialBenchPrefab, UnderworldEquipmentSlot.Helmet,
            "Light fungal veil; planned passive spore visibility.",
            Cost(UnderworldFungalRefinementCatalog.SpireCord, 8), Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 2)),
        ArmourPiece("Sporeweave", "Sporeweave", "sporeweave", UnderworldTerrainBiome.FungalForest,
            UnderworldStationCatalog.MycelialBenchPrefab, UnderworldEquipmentSlot.Chest,
            "Layered Worldroot/fungal cuirass; planned spore visibility contribution.",
            Cost(UnderworldFungalRefinementCatalog.SpireCord, 16), Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 4),
            Cost(UnderworldFungalRefinementCatalog.WorldrootPlank, 4)),
        ArmourPiece("Sporeweave", "Sporeweave", "sporeweave", UnderworldTerrainBiome.FungalForest,
            UnderworldStationCatalog.MycelialBenchPrefab, UnderworldEquipmentSlot.Legs,
            "Flexible fungal leg wraps; planned spore visibility contribution.",
            Cost(UnderworldFungalRefinementCatalog.SpireCord, 14), Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 3)),
        ArmourPiece("Sporeweave", "Sporeweave", "sporeweave", UnderworldTerrainBiome.FungalForest,
            UnderworldStationCatalog.MycelialBenchPrefab, UnderworldEquipmentSlot.Cape,
            "Spore-shedding mantle completing the light Fungal Forest set.",
            Cost(UnderworldFungalRefinementCatalog.SpireCord, 12), Cost(UnderworldFungalRefinementCatalog.CuredGlowcap, 4)),

        ArmourPieceRaw("Palewater", "Palewater", "palewater", UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldStationCatalog.TidalBasinPrefab, UnderworldEquipmentSlot.Helmet,
            "Sealed hood; planned breath and wet-resistance contribution.", 4, 8, 4, 2),
        ArmourPieceRaw("Palewater", "Palewater", "palewater", UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldStationCatalog.TidalBasinPrefab, UnderworldEquipmentSlot.Chest,
            "Flowstone-weighted diving coat; planned breath, wet-resistance and swim contribution.", 8, 18, 6, 3),
        ArmourPieceRaw("Palewater", "Palewater", "palewater", UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldStationCatalog.TidalBasinPrefab, UnderworldEquipmentSlot.Legs,
            "Pale-fibre diving leggings; planned swim contribution.", 6, 16, 4, 2),
        ArmourPieceRaw("Palewater", "Palewater", "palewater", UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldStationCatalog.TidalBasinPrefab, UnderworldEquipmentSlot.Cape,
            "Water-shedding mantle completing the Blackwater set.", 4, 14, 5, 2),

        SulfurArmour(UnderworldEquipmentSlot.Helmet, 8, 5, 2),
        SulfurArmour(UnderworldEquipmentSlot.Chest, 16, 10, 4),
        SulfurArmour(UnderworldEquipmentSlot.Legs, 14, 8, 3),
        SulfurArmour(UnderworldEquipmentSlot.Cape, 10, 6, 3),

        FrozenArmour(UnderworldEquipmentSlot.Helmet, 8, 6, 4),
        FrozenArmour(UnderworldEquipmentSlot.Chest, 16, 12, 8),
        FrozenArmour(UnderworldEquipmentSlot.Legs, 14, 10, 7),
        FrozenArmour(UnderworldEquipmentSlot.Cape, 10, 12, 5),

        FractureArmour(UnderworldEquipmentSlot.Helmet, 8, 5, 5),
        FractureArmour(UnderworldEquipmentSlot.Chest, 16, 10, 8),
        FractureArmour(UnderworldEquipmentSlot.Legs, 14, 8, 7),
        FractureArmour(UnderworldEquipmentSlot.Cape, 10, 7, 6),

        DecayArmour(UnderworldEquipmentSlot.Helmet, 8, 5, 4, 3),
        DecayArmour(UnderworldEquipmentSlot.Chest, 16, 10, 8, 5),
        DecayArmour(UnderworldEquipmentSlot.Legs, 14, 8, 7, 4),
        DecayArmour(UnderworldEquipmentSlot.Cape, 12, 8, 6, 4),
    });

    public static IReadOnlyList<UnderworldEquipmentDefinition> All { get; } =
        Array.AsReadOnly(Tools.Concat(Armour).ToArray());

    public static IReadOnlyList<UnderworldEquipmentDefinition> FungalForestSlice { get; } =
        Array.AsReadOnly(All.Where(value => value.Biome == UnderworldTerrainBiome.FungalForest).ToArray());

    static UnderworldEquipmentCatalog()
    {
        if (Tools.Count != 6) throw new InvalidOperationException("Exactly six Underworld biome tools are required.");
        if (Armour.Count != 24) throw new InvalidOperationException("Exactly six four-piece Underworld armour sets are required.");
        if (All.Select(value => value.Prefab).Distinct(StringComparer.Ordinal).Count() != All.Count)
            throw new InvalidOperationException("Underworld equipment prefab identities must be unique.");
        if (All.Select(value => value.ModelId).Distinct(StringComparer.Ordinal).Count() != All.Count)
            throw new InvalidOperationException("Underworld equipment model identities must be unique.");
        if (All.Any(value => value.Costs.Count < 2 || value.Costs.Any(cost => cost.Amount <= 0 || string.IsNullOrWhiteSpace(cost.Prefab))))
            throw new InvalidOperationException("Every Underworld equipment item requires a positive material package.");
        if (Tools.Select(value => value.Biome).Distinct().Count() != 6)
            throw new InvalidOperationException("Every Underworld biome must own exactly one progression tool.");
        foreach (UnderworldTerrainBiome biome in Enum.GetValues(typeof(UnderworldTerrainBiome)))
        {
            var pieces = Armour.Where(value => value.Biome == biome).ToArray();
            if (pieces.Length != 4 || pieces.Select(value => value.Slot).Distinct().Count() != 4)
                throw new InvalidOperationException(biome + " must own helmet, chest, legs and cape armour.");
        }
    }

    private static UnderworldEquipmentDefinition Tool(
        string suffix, string name, string modelId, UnderworldTerrainBiome biome, string station,
        string role, params UnderworldEquipmentCost[] costs) =>
        new("Magenheim_Underworld_Tool_" + suffix, name, modelId, biome, UnderworldEquipmentSlot.Tool,
            station, role, Array.AsReadOnly(costs));

    private static UnderworldEquipmentDefinition WearableTool(
        string suffix, string name, string modelId, UnderworldTerrainBiome biome, string station,
        string role, params UnderworldEquipmentCost[] costs) =>
        new("Magenheim_Underworld_Tool_" + suffix, name, modelId, biome, UnderworldEquipmentSlot.Helmet,
            station, role, Array.AsReadOnly(costs));

    private static UnderworldEquipmentDefinition ArmourPiece(
        string prefabSet, string displaySet, string modelSet, UnderworldTerrainBiome biome, string station,
        UnderworldEquipmentSlot slot, string role, params UnderworldEquipmentCost[] costs) =>
        new("Magenheim_Underworld_Armor_" + prefabSet + SlotSuffix(slot),
            displaySet + " " + DisplaySlot(slot),
            "underworld-armor-" + modelSet + "-" + ModelSlot(slot),
            biome, slot, station, role, Array.AsReadOnly(costs));

    private static UnderworldEquipmentDefinition ArmourPieceRaw(
        string prefabSet, string displaySet, string modelSet, UnderworldTerrainBiome biome, string station,
        UnderworldEquipmentSlot slot, string role, int flowstone, int fibre, int pearl, int salt) =>
        ArmourPiece(prefabSet, displaySet, modelSet, biome, station, slot, role,
            Cost("Magenheim_Underworld_Resource_BlackwaterFlowstone", flowstone),
            Cost("Magenheim_Underworld_Resource_PaleFibre", fibre),
            Cost("Magenheim_Underworld_Resource_BlackwaterPearl", pearl),
            Cost("Magenheim_Underworld_Resource_DeepSalt", salt));

    private static UnderworldEquipmentDefinition SulfurArmour(UnderworldEquipmentSlot slot, int iron, int slag, int sulfur) =>
        ArmourPiece("Emberiron", "Emberiron", "emberiron", UnderworldTerrainBiome.SulfurousWastes,
            UnderworldStationCatalog.FurnaceHeartForgePrefab, slot,
            "Heavy heat-resistant armour; planned to stack with Furnace Blood.",
            Cost("Magenheim_Underworld_Resource_Emberiron", iron),
            Cost("Magenheim_Underworld_Resource_Slagstone", slag),
            Cost("Magenheim_Underworld_Resource_Sulfur", sulfur));

    private static UnderworldEquipmentDefinition FrozenArmour(UnderworldEquipmentSlot slot, int silver, int ice, int wood) =>
        ArmourPiece("Rimeward", "Rimeward", "rimeward", UnderworldTerrainBiome.FrozenCaverns,
            UnderworldStationCatalog.SilenceTablePrefab, slot,
            "Cold-resistant precision armour; planned Rimebound stacking and quieter movement.",
            Cost("Magenheim_Underworld_Resource_Rimesilver", silver),
            Cost("Magenheim_Underworld_Resource_ClearIce", ice),
            Cost("Magenheim_Underworld_Resource_Rimewood", wood));

    private static UnderworldEquipmentDefinition FractureArmour(UnderworldEquipmentSlot slot, int titanbone, int shardstone, int crystal) =>
        ArmourPiece("Stoneanchor", "Stoneanchor", "stoneanchor", UnderworldTerrainBiome.FractureZones,
            UnderworldStationCatalog.AnchorForgePrefab, slot,
            "Heavy anchored armour; planned tremor immunity and knockback resistance.",
            Cost("Magenheim_Underworld_Resource_Titanbone", titanbone),
            Cost("Magenheim_Underworld_Resource_Shardstone", shardstone),
            Cost("Magenheim_Underworld_Resource_FractureCrystal", crystal));

    private static UnderworldEquipmentDefinition DecayArmour(UnderworldEquipmentSlot slot, int amber, int rotwood, int spore, int bone) =>
        ArmourPiece("Defiant", "Defiant", "defiant", UnderworldTerrainBiome.GreatDecay,
            UnderworldStationCatalog.CrownReliquaryPrefab, slot,
            "Endgame contamination-resistant armour for sustained Great Decay habitation.",
            Cost("Magenheim_Underworld_Resource_CarrionAmber", amber),
            Cost("Magenheim_Underworld_Resource_Rotwood", rotwood),
            Cost("Magenheim_Underworld_Resource_DecaySpore", spore),
            Cost("Magenheim_Underworld_Resource_BoneGravel", bone));

    private static UnderworldEquipmentCost Cost(string prefab, int amount) => new(prefab, amount);

    private static string SlotSuffix(UnderworldEquipmentSlot slot) =>
        slot == UnderworldEquipmentSlot.Helmet ? "Helmet" :
        slot == UnderworldEquipmentSlot.Chest ? "Chest" :
        slot == UnderworldEquipmentSlot.Legs ? "Legs" :
        slot == UnderworldEquipmentSlot.Cape ? "Cape" :
        throw new InvalidOperationException("Tool cannot be an armour suffix.");

    private static string ModelSlot(UnderworldEquipmentSlot slot) =>
        slot == UnderworldEquipmentSlot.Helmet ? "helmet" :
        slot == UnderworldEquipmentSlot.Chest ? "chest" :
        slot == UnderworldEquipmentSlot.Legs ? "legs" :
        slot == UnderworldEquipmentSlot.Cape ? "cape" :
        throw new InvalidOperationException("Tool cannot be an armour model slot.");

    private static string DisplaySlot(UnderworldEquipmentSlot slot) =>
        slot == UnderworldEquipmentSlot.Helmet ? "Helm" :
        slot == UnderworldEquipmentSlot.Chest ? "Cuirass" :
        slot == UnderworldEquipmentSlot.Legs ? "Leggings" :
        slot == UnderworldEquipmentSlot.Cape ? "Mantle" :
        throw new InvalidOperationException("Tool cannot be an armour display slot.");
}
