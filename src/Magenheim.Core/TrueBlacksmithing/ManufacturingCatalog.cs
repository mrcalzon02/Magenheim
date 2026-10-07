using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.TrueBlacksmithing;

public enum ManufacturingDisposition { Keep, Replace, Adapt, Process, Exempt }
public enum EquipmentFamily { Material, Sword, Knife, Mace, Hammer, Polearm, Spear, Axe, Pickaxe, Bow, Crossbow, Magical, Armour, Shield, Ammunition, Tool, Utility }
public enum WorkshopOperation { HandWork, Knapping, Carving, Leatherworking, TextileWork, Casting, Heating, Forging, ArmourForming, Quenching, Tempering, Grinding, Finishing, Fitting, Assembly, ExoticTreatment }
public enum ProcessInteraction { RecipeMenu, DirectInteraction }

/// <summary>Authored manufacturing inputs, independent of Unity and game registries.</summary>
public sealed class ManufacturingCatalog
{
    public int SchemaVersion { get; set; }
    public string Scope { get; set; } = "";
    public Dictionary<string, string> SourceHashes { get; set; } = new Dictionary<string, string>();
    public List<EquipmentBaseline> Items { get; set; } = new List<EquipmentBaseline>();
    public List<RecipeBaseline> Recipes { get; set; } = new List<RecipeBaseline>();
    public List<StationBaseline> Stations { get; set; } = new List<StationBaseline>();

    public void Validate()
    {
        if (SchemaVersion != 1 || string.IsNullOrWhiteSpace(Scope)) throw new InvalidOperationException("Unsupported manufacturing catalog schema/scope.");
        var items = Unique(Items, x => x.Prefab, "item");
        var stations = Unique(Stations, x => x.Prefab, "station");
        Unique(Recipes, x => x.Id, "recipe");
        foreach (var item in Items)
        {
            if (item.MaxQuality < 1 || item.MaxStack < 1 || string.IsNullOrWhiteSpace(item.Reason)
                || !Enum.IsDefined(typeof(EquipmentFamily), item.Family) || !Enum.IsDefined(typeof(ManufacturingDisposition), item.Disposition))
                throw new InvalidOperationException("Invalid or unclassified item: " + item.Prefab);
        }
        foreach (var station in Stations)
            if (station.BaseLevel < 1 || station.Disposition != ManufacturingDisposition.Keep)
                throw new InvalidOperationException("Invalid station baseline: " + station.Prefab);
        foreach (var recipe in Recipes)
        {
            if (!items.TryGetValue(recipe.OutputPrefab, out var output) || recipe.OutputAmount < 1 || recipe.MinStationLevel < 1
                || recipe.QualityResultAmountMultiplier <= 0 || float.IsNaN(recipe.QualityResultAmountMultiplier)
                || float.IsInfinity(recipe.QualityResultAmountMultiplier) || string.IsNullOrWhiteSpace(recipe.Reason)
                || !Enum.IsDefined(typeof(ManufacturingDisposition), recipe.Disposition) || !Enum.IsDefined(typeof(EquipmentFamily), recipe.Family))
                throw new InvalidOperationException("Invalid recipe baseline: " + recipe.Id);
            if (!recipe.Enabled && recipe.Disposition != ManufacturingDisposition.Exempt)
                throw new InvalidOperationException("Disabled recipe must remain exempt: " + recipe.Id);
            foreach (var station in new[] { recipe.CraftingStation, recipe.RepairStation })
                if (station.Length != 0 && !stations.ContainsKey(station)) throw new InvalidOperationException("Unresolved station: " + station);
            Unique(recipe.Costs, x => x.Prefab, "ingredient");
            foreach (var cost in recipe.Costs)
            {
                if (!items.ContainsKey(cost.Prefab) || cost.Amount < 0 || cost.AmountPerLevel < 0 || cost.ExtraAmountOnlyOneIngredient < 0
                    || cost.QualityCosts.Count != output.MaxQuality)
                    throw new InvalidOperationException("Invalid material ledger: " + recipe.Id + "/" + cost.Prefab);
                for (var quality = 1; quality <= output.MaxQuality; quality++)
                    if (cost.QualityCosts[quality - 1] != cost.GetNativeAmount(quality))
                        throw new InvalidOperationException("Material ledger differs from native quality costs: " + recipe.Id);
            }
        }
    }

    internal static Dictionary<string, T> Unique<T>(IEnumerable<T> values, Func<T, string> key, string kind)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var id = key(value);
            if (string.IsNullOrWhiteSpace(id) || result.ContainsKey(id)) throw new InvalidOperationException("Missing/duplicate " + kind + " identity: " + id);
            result.Add(id, value);
        }
        return result;
    }
}

public sealed class EquipmentBaseline
{
    public string Prefab { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public int ItemType { get; set; }
    public EquipmentFamily Family { get; set; }
    public string NameKey { get; set; } = "";
    public int MaxQuality { get; set; }
    public int MaxStack { get; set; }
    public string Dlc { get; set; } = "";
    public ManufacturingDisposition Disposition { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class RecipeBaseline
{
    public string Id { get; set; } = "";
    public string OutputPrefab { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public EquipmentFamily Family { get; set; }
    public ManufacturingDisposition Disposition { get; set; }
    public string Reason { get; set; } = "";
    public bool Enabled { get; set; }
    public int OutputAmount { get; set; }
    public float QualityResultAmountMultiplier { get; set; }
    public bool UpgradeOnly { get; set; }
    public bool RequireOnlyOneIngredient { get; set; }
    public string CraftingStation { get; set; } = "";
    public string RepairStation { get; set; } = "";
    public int MinStationLevel { get; set; }
    public List<MaterialBudget> Costs { get; set; } = new List<MaterialBudget>();
}

public sealed class MaterialBudget
{
    public string Prefab { get; set; } = "";
    public int Amount { get; set; }
    public int AmountPerLevel { get; set; }
    public bool UpgraderResource { get; set; }
    public int ExtraAmountOnlyOneIngredient { get; set; }
    public bool Recover { get; set; }
    public List<int> QualityCosts { get; set; } = new List<int>();

    public int GetNativeAmount(int quality)
    {
        if (quality < 1) throw new ArgumentOutOfRangeException(nameof(quality));
        if (quality == 1) return Amount;
        // Mirrors inspected Valheim 1.0 Piece.Requirement.GetAmount, including its
        // non-linear quality-four boundary and special upgrader resource base.
        float multiplier = quality < 4 ? quality - 1 : 4f + (quality - 4) / 2f;
        return checked((int)Math.Floor(multiplier * AmountPerLevel + (UpgraderResource ? Amount : 0)));
    }
}

public sealed class StationBaseline
{
    public string Prefab { get; set; } = "";
    public string NameKey { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public int BaseLevel { get; set; }
    public bool RequiresRoof { get; set; }
    public bool RequiresFire { get; set; }
    public ManufacturingDisposition Disposition { get; set; }
}

public sealed class ManufacturingGrammar
{
    public int SchemaVersion { get; set; }
    public List<WorkshopDefinition> Workshops { get; set; } = new List<WorkshopDefinition>();
    public List<ComponentFamilyDefinition> Components { get; set; } = new List<ComponentFamilyDefinition>();
    public List<ManufacturingRouteDefinition> Routes { get; set; } = new List<ManufacturingRouteDefinition>();

    public void Validate()
    {
        if (SchemaVersion != 1) throw new InvalidOperationException("Unsupported manufacturing grammar schema.");
        var workshops = ManufacturingCatalog.Unique(Workshops, x => x.Id, "workshop");
        var components = ManufacturingCatalog.Unique(Components, x => x.Id, "component family");
        ManufacturingCatalog.Unique(Routes, x => x.Id, "route");
        var owners = new Dictionary<WorkshopOperation, string>();
        foreach (var workshop in Workshops)
        {
            if (string.IsNullOrWhiteSpace(workshop.PhysicalJob) || workshop.Operations.Count == 0
                || !Enum.IsDefined(typeof(ProcessInteraction), workshop.Interaction)) throw new InvalidOperationException("Workshop has no physical job: " + workshop.Id);
            foreach (var operation in workshop.Operations)
            {
                if (!Enum.IsDefined(typeof(WorkshopOperation), operation) || owners.ContainsKey(operation)) throw new InvalidOperationException("Operation lacks a unique workshop owner: " + operation);
                owners.Add(operation, workshop.Id);
            }
        }
        foreach (WorkshopOperation operation in Enum.GetValues(typeof(WorkshopOperation)))
            if (!owners.ContainsKey(operation)) throw new InvalidOperationException("Unowned manufacturing operation: " + operation);
        foreach (var component in Components)
        {
            if (string.IsNullOrWhiteSpace(component.AdmissionReason) || component.OutputForms.Count == 0
                || !workshops.ContainsKey(component.Workshop)) throw new InvalidOperationException("Unjustified component family: " + component.Id);
        }
        foreach (var route in Routes)
        {
            if (route.Operations.Count == 0 || route.Families.Count == 0 || string.IsNullOrWhiteSpace(route.MaterialCondition))
                throw new InvalidOperationException("Incomplete manufacturing route: " + route.Id);
            foreach (var operation in route.Operations)
                if (!owners.ContainsKey(operation)) throw new InvalidOperationException("Unknown route operation: " + route.Id);
            foreach (var family in route.Families)
                if (!Enum.IsDefined(typeof(EquipmentFamily), family)) throw new InvalidOperationException("Unknown equipment family: " + route.Id);
            foreach (var component in route.Components)
                if (!components.ContainsKey(component)) throw new InvalidOperationException("Unknown route component: " + component);
        }
        foreach (EquipmentFamily family in Enum.GetValues(typeof(EquipmentFamily)))
            if (family != EquipmentFamily.Material && !Routes.Any(x => x.Families.Contains(family)))
                throw new InvalidOperationException("Equipment family has no manufacturing grammar: " + family);
        foreach (var component in Components)
            if (!Routes.Any(x => x.Components.Contains(component.Id))) throw new InvalidOperationException("Orphan component family: " + component.Id);
    }
}

public sealed class WorkshopDefinition
{
    public string Id { get; set; } = "";
    public string PhysicalJob { get; set; } = "";
    public string NativeStation { get; set; } = "";
    public ProcessInteraction Interaction { get; set; }
    public List<WorkshopOperation> Operations { get; set; } = new List<WorkshopOperation>();
}
public sealed class ComponentFamilyDefinition
{
    public string Id { get; set; } = "";
    public string Workshop { get; set; } = "";
    public string AdmissionReason { get; set; } = "";
    public List<string> OutputForms { get; set; } = new List<string>();
}
public sealed class ManufacturingRouteDefinition
{
    public string Id { get; set; } = "";
    public string MaterialCondition { get; set; } = "";
    public List<EquipmentFamily> Families { get; set; } = new List<EquipmentFamily>();
    public List<WorkshopOperation> Operations { get; set; } = new List<WorkshopOperation>();
    public List<string> Components { get; set; } = new List<string>();
}
