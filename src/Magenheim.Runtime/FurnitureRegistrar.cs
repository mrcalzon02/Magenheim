using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the original geology/crystal furniture collection under Hammer > Furniture.</summary>
internal sealed class FurnitureRegistrar : IDisposable
{
    private const string SimpleEarthCrystal = "Magenheim_Crystal_Earth_Simple";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal FurnitureRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterFurniture;
        _subscribed = true;
    }

    private void RegisterFurniture()
    {
        if (_registered) return;
        try
        {
            foreach (var definition in Definitions())
                RegisterPiece(definition);

            _registered = true;
            _log.LogInfo("Registered 10 original Magenheim geology/crystal furniture pieces in Hammer > Furniture with explicit comfort groups and Crystal Shaping-gated material costs.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Magenheim furniture registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterPiece(FurnitureDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied furniture identity '{definition.PrefabName}'.");

        var source = ResolveSource(definition.SourceCandidates);
        if (PrefabManager.Instance.GetPrefab(source) is null)
            throw new InvalidOperationException($"Resolved furniture source '{source}' disappeared before cloning.");

        if (PrefabManager.Instance.GetPrefab(SimpleEarthCrystal) is null)
            throw new InvalidOperationException(
                $"Required shaped crystal material '{SimpleEarthCrystal}' is unavailable before furniture registration.");

        var config = new PieceConfig
        {
            Name = definition.DisplayName,
            Description = definition.Description,
            PieceTable = "Hammer",
            Category = "Furniture",
            CraftingStation = "piece_workbench",
            Requirements = definition.Requirements,
            Icon = FurnitureIcons.Icon(definition.ModelId),
        };

        var custom = new CustomPiece(definition.PrefabName, source, config);
        var prefab = custom.PiecePrefab;
        custom.Piece.m_name = definition.DisplayName;
        custom.Piece.m_description = definition.Description;
        custom.Piece.m_dlc = string.Empty;
        custom.Piece.m_comfort = definition.Comfort;
        custom.Piece.m_comfortGroup = ResolveComfortGroup(definition.ComfortGroup);

        var visual = FurnitureVisuals.Apply(prefab, definition.ModelId);
        ConfigureWear(prefab, visual);
        ConfigureColliders(prefab, definition.ModelId);
        PlacementSnapAuthority.AlignToBase(prefab);

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException($"Jotunn refused furniture piece '{definition.PrefabName}'.");
    }

    private static Piece.ComfortGroup ResolveComfortGroup(string name)
    {
        if (!Enum.TryParse(name, true, out Piece.ComfortGroup group) ||
            !Enum.IsDefined(typeof(Piece.ComfortGroup), group))
            throw new InvalidOperationException($"Required Valheim comfort group '{name}' is unavailable.");
        return group;
    }

    private static string ResolveSource(IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (PrefabManager.Instance.GetPrefab(candidate))
                return candidate;
        }
        throw new InvalidOperationException(
            "None of the compatible vanilla furniture source prefabs are available: " + string.Join(", ", candidates));
    }

    private static void ConfigureWear(GameObject prefab, GameObject visual)
    {
        var wear = prefab.GetComponent<WearNTear>();
        if (!wear) return;

        wear.m_new = visual;
        wear.m_worn = UnityEngine.Object.Instantiate(visual, prefab.transform);
        wear.m_worn.name = visual.name + ".worn";
        wear.m_broken = UnityEngine.Object.Instantiate(visual, prefab.transform);
        wear.m_broken.name = visual.name + ".weathered";
        Tint(wear.m_worn, .82f);
        Tint(wear.m_broken, .64f);
        wear.m_worn.SetActive(false);
        wear.m_broken.SetActive(false);
        wear.m_health = 350f;
        wear.m_fragmentRoots = new[] { wear.m_new, wear.m_worn, wear.m_broken };
    }

    private static void Tint(GameObject root, float brightness)
    {
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var source = renderer.sharedMaterial;
            if (!source) continue;
            var material = new Material(source) { name = source.name + ".weathered" };
            if (material.HasProperty("_Color"))
            {
                var color = material.GetColor("_Color");
                material.SetColor("_Color", new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a));
            }
            renderer.sharedMaterial = material;
        }
    }

    private static void ConfigureColliders(GameObject prefab, string modelId)
    {
        var existing = prefab.GetComponentsInChildren<Collider>(true).Where(collider => !collider.isTrigger).ToArray();
        var layer = existing.Length > 0 ? existing[0].gameObject.layer : prefab.layer;
        foreach (var collider in existing) collider.enabled = false;

        var root = new GameObject("magenheim.furniture.collision") { layer = layer };
        root.transform.SetParent(prefab.transform, false);

        switch (modelId)
        {
            case FurnitureVisuals.GeodeTable:
                Box(root, new Vector3(0f, .48f, 0f), new Vector3(1.85f, .96f, .92f));
                break;
            case FurnitureVisuals.GeodeChair:
                Box(root, new Vector3(0f, .65f, 0f), new Vector3(.66f, 1.35f, .66f));
                break;
            case FurnitureVisuals.CrystalBench:
                Box(root, new Vector3(0f, .42f, 0f), new Vector3(1.62f, .84f, .55f));
                break;
            case FurnitureVisuals.CrystalBed:
                Box(root, new Vector3(0f, .48f, 0f), new Vector3(1.08f, .96f, 2.10f));
                Box(root, new Vector3(0f, 1.03f, .88f), new Vector3(1.08f, .80f, .18f));
                break;
            case FurnitureVisuals.MineralShelf:
                Box(root, new Vector3(0f, .96f, 0f), new Vector3(1.34f, 1.94f, .52f));
                break;
            case FurnitureVisuals.LapidaryCabinet:
                Box(root, new Vector3(0f, .62f, 0f), new Vector3(1.42f, 1.25f, .74f));
                break;
            case FurnitureVisuals.GeoDesk:
                Box(root, new Vector3(0f, .46f, 0f), new Vector3(1.58f, .92f, .74f));
                break;
            case FurnitureVisuals.GeodePedestal:
                Box(root, new Vector3(0f, .62f, 0f), new Vector3(.76f, 1.28f, .76f));
                break;
            case FurnitureVisuals.CrystalDivider:
                Box(root, new Vector3(0f, .98f, 0f), new Vector3(1.78f, 2.00f, .32f));
                break;
            case FurnitureVisuals.CrystalThrone:
                Box(root, new Vector3(0f, .72f, 0f), new Vector3(1.12f, 1.44f, 1.00f));
                Box(root, new Vector3(0f, 1.63f, .30f), new Vector3(.96f, 1.20f, .24f));
                break;
            default:
                Box(root, new Vector3(0f, .50f, 0f), Vector3.one);
                break;
        }
    }

    private static void Box(GameObject root, Vector3 center, Vector3 size)
    {
        var collider = root.AddComponent<BoxCollider>();
        collider.center = center;
        collider.size = size;
    }

    private static RequirementConfig Cost(string item, int amount) =>
        new RequirementConfig(item, amount, 0, true);

    private static RequirementConfig Crystals(int amount) => Cost(SimpleEarthCrystal, amount);

    private static IReadOnlyList<FurnitureDefinition> Definitions() => new[]
    {
        new FurnitureDefinition(
            "Magenheim_Furniture_GeodeTable", FurnitureVisuals.GeodeTable, "Geode Table", 1, "Table",
            "A broad stone-topped table with a cut geode specimen set into its center.",
            new[] { "piece_table", "wood_floor" },
            Cost("FineWood", 8), Cost("Stone", 4), Crystals(8)),
        new FurnitureDefinition(
            "Magenheim_Furniture_GeodeChair", FurnitureVisuals.GeodeChair, "Geode Chair", 2, "Chair",
            "A rugged stone-and-timber chair crowned with small crystal points.",
            new[] { "piece_chair", "piece_bench01", "wood_floor" },
            Cost("FineWood", 4), Cost("Stone", 2), Crystals(5)),
        new FurnitureDefinition(
            "Magenheim_Furniture_CrystalBench", FurnitureVisuals.CrystalBench, "Crystal Bench", 2, "Chair",
            "A low geological bench with iron-bound stone and crystal end caps.",
            new[] { "piece_bench01", "piece_chair", "wood_floor" },
            Cost("FineWood", 6), Cost("Stone", 4), Crystals(8)),
        new FurnitureDefinition(
            "Magenheim_Furniture_CrystalBed", FurnitureVisuals.CrystalBed, "Crystal-set Bed", 2, "Bed",
            "A heavy timber bed with a stone headboard and softly luminous crystal finials.",
            new[] { "bed", "wood_floor" },
            Cost("FineWood", 8), Cost("DeerHide", 4), Cost("Stone", 4), Crystals(10)),
        new FurnitureDefinition(
            "Magenheim_Furniture_MineralShelf", FurnitureVisuals.MineralShelf, "Mineral Shelf", 1, "Table",
            "A three-tier specimen shelf for geodes, crystals, ore samples, and tools.",
            new[] { "piece_table", "wood_floor" },
            Cost("FineWood", 8), Cost("Iron", 2), Cost("Stone", 2), Crystals(10)),
        new FurnitureDefinition(
            "Magenheim_Furniture_LapidaryCabinet", FurnitureVisuals.LapidaryCabinet, "Lapidary Cabinet", 1, "Table",
            "A reinforced cabinet for mineral samples, shaping tools, and valuable crystal stock.",
            new[] { "piece_chest_wood", "piece_chest", "piece_table", "wood_floor" },
            Cost("FineWood", 10), Cost("Bronze", 2), Cost("Stone", 2), Crystals(12)),
        new FurnitureDefinition(
            "Magenheim_Furniture_GeoDesk", FurnitureVisuals.GeoDesk, "Geologist's Desk", 1, "Table",
            "A specimen desk with stone writing surface, metal inlay, and a crystal scribing point.",
            new[] { "piece_table", "wood_floor" },
            Cost("FineWood", 10), Cost("Stone", 4), Cost("Bronze", 1), Crystals(12)),
        new FurnitureDefinition(
            "Magenheim_Furniture_GeodePedestal", FurnitureVisuals.GeodePedestal, "Geode Pedestal", 1, "Table",
            "A short stone display plinth carrying a permanently mounted cracked geode specimen.",
            new[] { "piece_table", "wood_floor" },
            Cost("Stone", 8), Cost("Iron", 1), Crystals(8)),
        new FurnitureDefinition(
            "Magenheim_Furniture_CrystalDivider", FurnitureVisuals.CrystalDivider, "Crystal Screen", 1, "Table",
            "An open timber room divider crossed by metal rails and suspended crystal points.",
            new[] { "woodwall", "wood_wall", "wood_floor" },
            Cost("FineWood", 8), Cost("Iron", 2), Crystals(14)),
        new FurnitureDefinition(
            "Magenheim_Furniture_CrystalThrone", FurnitureVisuals.CrystalThrone, "Crystal Throne", 3, "Chair",
            "A ceremonial seat of dark stone, fine timber, iron, bronze, and a resonant crystal heart.",
            new[] { "piece_throne01", "piece_chair", "wood_floor" },
            Cost("FineWood", 12), Cost("Stone", 10), Cost("Iron", 4), Cost("Bronze", 2), Crystals(20)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterFurniture;
        _subscribed = false;
    }

    private sealed class FurnitureDefinition
    {
        internal FurnitureDefinition(
            string prefabName,
            string modelId,
            string displayName,
            int comfort,
            string comfortGroup,
            string description,
            IReadOnlyList<string> sourceCandidates,
            params RequirementConfig[] requirements)
        {
            PrefabName = prefabName;
            ModelId = modelId;
            DisplayName = displayName;
            Comfort = comfort;
            ComfortGroup = comfortGroup;
            Description = description;
            SourceCandidates = sourceCandidates;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string ModelId { get; }
        internal string DisplayName { get; }
        internal int Comfort { get; }
        internal string ComfortGroup { get; }
        internal string Description { get; }
        internal IReadOnlyList<string> SourceCandidates { get; }
        internal RequirementConfig[] Requirements { get; }
    }
}
