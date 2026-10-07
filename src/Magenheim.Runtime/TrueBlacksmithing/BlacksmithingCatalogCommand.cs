using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.TrueBlacksmithing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Magenheim.Runtime.TrueBlacksmithing;

/// <summary>Read-only production census; it does not activate or design recipes.</summary>
internal sealed class BlacksmithingCatalogCommand : ConsoleCommand
{
    private readonly TrueBlacksmithingModule _module;
    private readonly ManualLogSource _log;
    internal BlacksmithingCatalogCommand(TrueBlacksmithingModule module, ManualLogSource log)
    { _module = module; _log = log; }
    public override string Name => "magenheim_blacksmithing";
    public override string Help => "status | catalog -- inspect manufacturing definitions or capture registered vanilla/Magenheim recipes; no gameplay mutations";
    public override bool IsCheat => true;
    public override List<string> CommandOptionList() => new List<string> { "status", "catalog" };

    public override void Run(string[] args)
    {
        try
        {
            var directory = Path.Combine(Path.GetDirectoryName(typeof(BlacksmithingCatalogCommand).Assembly.Location)!, "default-data", "true-blacksmithing");
            var baseline = ManufacturingDataLoader.Read<ManufacturingCatalog>(Path.Combine(directory, "vanilla-catalog.json"));
            var grammar = ManufacturingDataLoader.Read<ManufacturingGrammar>(Path.Combine(directory, "manufacturing-grammar.json"));
            var verb = args.Length == 0 ? "status" : args[0].ToLowerInvariant();
            if (verb == "status")
            {
                Say($"True Blacksmithing: {_module.State}. {baseline.Recipes.Count} serialized equipment recipes; {grammar.Workshops.Count} workshop jobs; {grammar.Routes.Count} material-dependent routes. Manufacturing conversion is not active.");
                return;
            }
            if (verb != "catalog") { Say(Help); return; }
            if (!Player.m_localPlayer || !ObjectDB.instance || !ZNetScene.instance)
            { Say("Load a world before capturing registered recipes."); return; }
            var snapshot = Capture(ObjectDB.instance, baseline);
            snapshot.Validate();
            var output = Path.Combine(Paths.ConfigPath, "Magenheim", "true-blacksmithing");
            Directory.CreateDirectory(output);
            var path = Path.Combine(output, "registered-catalog-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".json");
            var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            settings.Converters.Add(new StringEnumConverter());
            // A new file preserves prior evidence; no recipe, item or save is written.
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(JsonConvert.SerializeObject(snapshot, settings));
            Say($"Captured {snapshot.Recipes.Count} registered recipes ({snapshot.Recipes.Count(x => x.OutputPrefab.StartsWith("Magenheim_", StringComparison.Ordinal))} Magenheim routes): {path}");
        }
        catch (Exception exception)
        {
            _log.LogError("True Blacksmithing read-only catalog failed; baseline crafting is unchanged: " + exception);
            Say("Catalog capture failed. See the log; baseline crafting remains unchanged.");
        }
    }

    private static ManufacturingCatalog Capture(ObjectDB db, ManufacturingCatalog baseline)
    {
        var result = new ManufacturingCatalog { SchemaVersion = 1, Scope = "RegisteredSessionObservation" };
        var known = baseline.Items.ToDictionary(x => x.Prefab, StringComparer.Ordinal);
        foreach (var prefab in db.m_items.Where(x => x).OrderBy(x => x.name, StringComparer.Ordinal))
        {
            var drop = prefab.GetComponent<ItemDrop>();
            if (!drop) continue;
            var shared = drop.m_itemData.m_shared;
            var owned = prefab.name.StartsWith("Magenheim_", StringComparison.Ordinal);
            var family = Family(shared);
            result.Items.Add(new EquipmentBaseline
            {
                Prefab = prefab.name, SourcePath = owned ? "registered:Magenheim" : known.ContainsKey(prefab.name) ? "registered:vanilla-candidate" : "registered:foreign-observation",
                ItemType = (int)shared.m_itemType, Family = family, NameKey = shared.m_name,
                MaxQuality = shared.m_maxQuality, MaxStack = shared.m_maxStackSize, Dlc = shared.m_dlc,
                Disposition = owned && family != EquipmentFamily.Material ? ManufacturingDisposition.Adapt : ManufacturingDisposition.Keep,
                Reason = owned ? "Preserve Magenheim output, crystal identity and dependency chain; adaptation requires complete preflight" : "Observed registered baseline; foreign definitions are not converted"
            });
        }
        var items = result.Items.ToDictionary(x => x.Prefab, StringComparer.Ordinal);
        var stations = new Dictionary<string, StationBaseline>(StringComparer.Ordinal);
        foreach (var recipe in db.m_recipes.Where(x => x && x.m_item).OrderBy(x => x.name, StringComparer.Ordinal))
        {
            var output = recipe.m_item.gameObject.name;
            if (!items.TryGetValue(output, out var item) || item.Family == EquipmentFamily.Material) continue;
            var owned = output.StartsWith("Magenheim_", StringComparison.Ordinal);
            if (!owned && !known.ContainsKey(output)) continue;
            var record = new RecipeBaseline
            {
                Id = recipe.name, OutputPrefab = output, SourcePath = "registered:ObjectDB-observation",
                Family = item.Family, Disposition = owned ? ManufacturingDisposition.Adapt : ManufacturingDisposition.Keep,
                Reason = owned ? "Native registered Magenheim dependency and material baseline; no replacement applied" : "Registered route observation, including foreign additions; no authority inferred from output identity",
                Enabled = recipe.m_enabled, OutputAmount = recipe.m_amount,
                QualityResultAmountMultiplier = recipe.m_qualityResultAmountMultiplier,
                UpgradeOnly = recipe.m_noCraftOnlyUpgrade, RequireOnlyOneIngredient = recipe.m_requireOnlyOneIngredient,
                CraftingStation = AddStation(recipe.m_craftingStation, stations), RepairStation = AddStation(recipe.m_repairStation, stations),
                MinStationLevel = recipe.m_minStationLevel
            };
            if (!record.Enabled) { record.Disposition = ManufacturingDisposition.Exempt; record.Reason = "Disabled registered route; preserve state"; }
            foreach (var cost in recipe.m_resources)
            {
                if (!cost.m_resItem) throw new InvalidDataException("Missing resource in registered recipe " + recipe.name);
                var budget = new MaterialBudget
                {
                    Prefab = cost.m_resItem.gameObject.name, Amount = cost.m_amount,
                    AmountPerLevel = cost.m_amountPerLevel, UpgraderResource = cost.m_upgraderResource,
                    ExtraAmountOnlyOneIngredient = cost.m_extraAmountOnlyOneIngredient, Recover = cost.m_recover
                };
                for (var quality = 1; quality <= item.MaxQuality; quality++) budget.QualityCosts.Add(cost.GetAmount(quality));
                record.Costs.Add(budget);
            }
            result.Recipes.Add(record);
        }
        result.Stations = stations.Values.OrderBy(x => x.Prefab, StringComparer.Ordinal).ToList();
        return result;
    }

    private static string AddStation(CraftingStation station, Dictionary<string, StationBaseline> stations)
    {
        if (!station) return "";
        var id = station.gameObject.name;
        if (!stations.ContainsKey(id)) stations.Add(id, new StationBaseline
        {
            Prefab = id, NameKey = station.m_name, SourcePath = "registered:CraftingStation", BaseLevel = 1,
            RequiresRoof = station.m_craftRequireRoof, RequiresFire = station.m_craftRequireFire, Disposition = ManufacturingDisposition.Keep
        });
        return id;
    }

    private static EquipmentFamily Family(ItemDrop.ItemData.SharedData shared)
    {
        var kind = (int)shared.m_itemType;
        if (kind == 6 || kind == 7 || kind == 11 || kind == 12 || kind == 17) return EquipmentFamily.Armour;
        if (kind == 9 || kind == 23) return EquipmentFamily.Ammunition;
        if (kind == 5) return EquipmentFamily.Shield;
        if (kind == 18 || kind == 24) return EquipmentFamily.Utility;
        if (kind == 15 || kind == 19) return EquipmentFamily.Tool;
        if (kind != 3 && kind != 4 && kind != 14 && kind != 20 && kind != 22) return EquipmentFamily.Material;
        if ((int)shared.m_animationState == 10) return EquipmentFamily.Crossbow;
        if ((int)shared.m_animationState == 2) return EquipmentFamily.Hammer;
        switch ((int)shared.m_skillType)
        {
            case 1: return EquipmentFamily.Sword;
            case 2: return EquipmentFamily.Knife;
            case 3: return EquipmentFamily.Mace;
            case 4: return EquipmentFamily.Polearm;
            case 5: return EquipmentFamily.Spear;
            case 7: return EquipmentFamily.Axe;
            case 8: return EquipmentFamily.Bow;
            case 9: case 10: return EquipmentFamily.Magical;
            case 12: return EquipmentFamily.Pickaxe;
            case 14: return EquipmentFamily.Crossbow;
            default: return EquipmentFamily.Tool;
        }
    }

    private void Say(string message) { Console.instance?.Print(message); _log.LogInfo("magenheim_blacksmithing: " + message); }
}
