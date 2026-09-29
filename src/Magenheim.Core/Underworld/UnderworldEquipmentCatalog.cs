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
            Cost(UnderworldBiomeRefinementCatalog.FlowstonePlate, 4),
            Cost(UnderworldBiomeRefinementCatalog.BrinedPearl, 2),
            Cost(UnderworldBiomeRefinementCatalog.PaleCord, 5)),

        Tool("SlagPick", "Slag Pick", "underworld-tool-slag-pick",
            UnderworldTerrainBiome.SulfurousWastes, UnderworldStationCatalog.FurnaceHeartForgePrefab,
            "Heat-cured mining tool intended to open Fracture Zone seams rather than merely mine faster.",
            Cost(UnderworldBiomeRefinementCatalog.EmberironBar, 7),
            Cost(UnderworldBiomeRefinementCatalog.TemperedSlag, 4),
            Cost(UnderworldBiomeRefinementCatalog.CharredRootGrip, 2)),

        Tool("RimeChisel", "Rime Chisel", "underworld-tool-rime-chisel",
            UnderworldTerrainBiome.FrozenCaverns, UnderworldStationCatalog.SilenceTablePrefab,
            "Precision harvesting tool intended to cut Clear Ice without shattering the deposit.",
            Cost(UnderworldBiomeRefinementCatalog.RimesilverBar, 4),
            Cost(UnderworldBiomeRefinementCatalog.IceglassLens, 3),
            Cost(UnderworldBiomeRefinementCatalog.RimewoodLaminate, 2)),

        Tool("AnchorSpike", "Anchor Spike", "underworld-tool-anchor-spike",
            UnderworldTerrainBiome.FractureZones, UnderworldStationCatalog.AnchorForgePrefab,
            "Heavy deployable anchor intended to stabilize a local patch of tremor-prone ground.",
            Cost(UnderworldBiomeRefinementCatalog.TitanbonePlate, 4),
            Cost(UnderworldBiomeRefinementCatalog.ShardstoneBlock, 6),
            Cost(UnderworldBiomeRefinementCatalog.FracturePrism, 3)),

        Tool("DefiantCenser", "Defiant Censer", "underworld-tool-defiant-censer",
            UnderworldTerrainBiome.GreatDecay, UnderworldStationCatalog.CrownReliquaryPrefab,
            "Carried reliquary intended to suppress contamination locally and make Great Decay habitation possible.",
            Cost(UnderworldBiomeRefinementCatalog.CarrionAmberSeal, 4),
            Cost(UnderworldBiomeRefinementCatalog.RotwoodLaminate, 3),
            Cost(UnderworldBiomeRefinementCatalog.OssuaryComposite, 2)),
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

        BlackwaterArmour(UnderworldEquipmentSlot.Helmet, 2, 4, 2),
        BlackwaterArmour(UnderworldEquipmentSlot.Chest, 4, 9, 3),
        BlackwaterArmour(UnderworldEquipmentSlot.Legs, 3, 8, 2),
        BlackwaterArmour(UnderworldEquipmentSlot.Cape, 2, 7, 3),

        SulfurArmour(UnderworldEquipmentSlot.Helmet, 4, 3, 1),
        SulfurArmour(UnderworldEquipmentSlot.Chest, 8, 5, 2),
        SulfurArmour(UnderworldEquipmentSlot.Legs, 7, 4, 2),
        SulfurArmour(UnderworldEquipmentSlot.Cape, 5, 3, 2),

        FrozenArmour(UnderworldEquipmentSlot.Helmet, 4, 3, 2),
        FrozenArmour(UnderworldEquipmentSlot.Chest, 8, 6, 4),
        FrozenArmour(UnderworldEquipmentSlot.Legs, 7, 5, 4),
        FrozenArmour(UnderworldEquipmentSlot.Cape, 5, 6, 3),

        FractureArmour(UnderworldEquipmentSlot.Helmet, 4, 3, 2),
        FractureArmour(UnderworldEquipmentSlot.Chest, 8, 5, 4),
        FractureArmour(UnderworldEquipmentSlot.Legs, 7, 4, 4),
        FractureArmour(UnderworldEquipmentSlot.Cape, 5, 4, 3),

        DecayArmour(UnderworldEquipmentSlot.Helmet, 4, 3, 2),
        DecayArmour(UnderworldEquipmentSlot.Chest, 8, 5, 4),
        DecayArmour(UnderworldEquipmentSlot.Legs, 7, 4, 4),
        DecayArmour(UnderworldEquipmentSlot.Cape, 6, 4, 3),
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

    private static UnderworldEquipmentDefinition BlackwaterArmour(
        UnderworldEquipmentSlot slot, int plate, int cord, int pearl) =>
        ArmourPiece("Palewater", "Palewater", "palewater", UnderworldTerrainBiome.BlackwaterDeep,
            UnderworldStationCatalog.TidalBasinPrefab, slot,
            slot == UnderworldEquipmentSlot.Helmet ? "Sealed hood; planned breath and wet-resistance contribution." :
            slot == UnderworldEquipmentSlot.Chest ? "Flowstone-weighted diving coat; planned breath, wet-resistance and swim contribution." :
            slot == UnderworldEquipmentSlot.Legs ? "Pale-fibre diving leggings; planned swim contribution." :
            "Water-shedding mantle completing the Blackwater set.",
            Cost(UnderworldBiomeRefinementCatalog.FlowstonePlate, plate),
            Cost(UnderworldBiomeRefinementCatalog.PaleCord, cord),
            Cost(UnderworldBiomeRefinementCatalog.BrinedPearl, pearl));

    private static UnderworldEquipmentDefinition SulfurArmour(UnderworldEquipmentSlot slot, int iron, int slag, int grip) =>
        ArmourPiece("Emberiron", "Emberiron", "emberiron", UnderworldTerrainBiome.SulfurousWastes,
            UnderworldStationCatalog.FurnaceHeartForgePrefab, slot,
            "Heavy heat-resistant armour; planned to stack with Furnace Blood.",
            Cost(UnderworldBiomeRefinementCatalog.EmberironBar, iron),
            Cost(UnderworldBiomeRefinementCatalog.TemperedSlag, slag),
            Cost(UnderworldBiomeRefinementCatalog.CharredRootGrip, grip));

    private static UnderworldEquipmentDefinition FrozenArmour(UnderworldEquipmentSlot slot, int silver, int ice, int wood) =>
        ArmourPiece("Rimeward", "Rimeward", "rimeward", UnderworldTerrainBiome.FrozenCaverns,
            UnderworldStationCatalog.SilenceTablePrefab, slot,
            "Cold-resistant precision armour; planned Rimebound stacking and quieter movement.",
            Cost(UnderworldBiomeRefinementCatalog.RimesilverBar, silver),
            Cost(UnderworldBiomeRefinementCatalog.IceglassLens, ice),
            Cost(UnderworldBiomeRefinementCatalog.RimewoodLaminate, wood));

    private static UnderworldEquipmentDefinition FractureArmour(UnderworldEquipmentSlot slot, int titanbone, int shardstone, int crystal) =>
        ArmourPiece("Stoneanchor", "Stoneanchor", "stoneanchor", UnderworldTerrainBiome.FractureZones,
            UnderworldStationCatalog.AnchorForgePrefab, slot,
            "Heavy anchored armour; planned tremor immunity and knockback resistance.",
            Cost(UnderworldBiomeRefinementCatalog.TitanbonePlate, titanbone),
            Cost(UnderworldBiomeRefinementCatalog.ShardstoneBlock, shardstone),
            Cost(UnderworldBiomeRefinementCatalog.FracturePrism, crystal));

    private static UnderworldEquipmentDefinition DecayArmour(UnderworldEquipmentSlot slot, int amber, int rotwood, int bone) =>
        ArmourPiece("Defiant", "Defiant", "defiant", UnderworldTerrainBiome.GreatDecay,
            UnderworldStationCatalog.CrownReliquaryPrefab, slot,
            "Endgame contamination-resistant armour for sustained Great Decay habitation.",
            Cost(UnderworldBiomeRefinementCatalog.CarrionAmberSeal, amber),
            Cost(UnderworldBiomeRefinementCatalog.RotwoodLaminate, rotwood),
            Cost(UnderworldBiomeRefinementCatalog.OssuaryComposite, bone));

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
