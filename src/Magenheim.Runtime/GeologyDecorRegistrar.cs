using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the compact geology/crystal decor collection under Hammer > Furniture.</summary>
internal sealed class GeologyDecorRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal GeologyDecorRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterDecor;
        _subscribed = true;
    }

    private void RegisterDecor()
    {
        if (_registered) return;
        try
        {
            foreach (var definition in Definitions())
                RegisterPiece(definition);

            _registered = true;
            _log.LogInfo("Registered 10 additional Magenheim geology/crystal decor pieces in Hammer > Furniture.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Magenheim geology decor registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterPiece(DecorDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied decor identity '{definition.PrefabName}'.");

        var source = ResolveSource(definition.SourceCandidates);
        if (PrefabManager.Instance.GetPrefab(source) is null)
            throw new InvalidOperationException($"Resolved decor source '{source}' disappeared before cloning.");

        var config = new PieceConfig
        {
            Name = definition.DisplayName,
            Description = definition.Description,
            PieceTable = "Hammer",
            Category = "Furniture",
            CraftingStation = "piece_workbench",
            Requirements = definition.Requirements,
            Icon = EarthAssets.Icon(definition.ModelId),
        };

        var custom = new CustomPiece(definition.PrefabName, source, config);
        var prefab = custom.PiecePrefab;
        custom.Piece.m_dlc = string.Empty;

        var visual = GeologyDecorVisuals.Apply(prefab, definition.ModelId);
        ConfigureWear(prefab, visual);
        ConfigureColliders(prefab, definition.ModelId);

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException($"Jotunn refused geology decor piece '{definition.PrefabName}'.");
    }

    private static string ResolveSource(IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (PrefabManager.Instance.GetPrefab(candidate))
                return candidate;
        }
        throw new InvalidOperationException(
            "None of the compatible vanilla decor source prefabs are available: " + string.Join(", ", candidates));
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
        wear.m_health = 300f;
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

        var root = new GameObject("magenheim.geology-decor.collision") { layer = layer };
        root.transform.SetParent(prefab.transform, false);

        switch (modelId)
        {
            case GeologyDecorVisuals.GeodeBowl:
                Box(root, new Vector3(0f, .25f, 0f), new Vector3(.82f, .50f, .82f));
                break;
            case GeologyDecorVisuals.CutGeodePlaque:
                Box(root, new Vector3(0f, .72f, 0f), new Vector3(.98f, 1.20f, .22f));
                break;
            case GeologyDecorVisuals.CrystalEndTable:
                Box(root, new Vector3(0f, .36f, 0f), new Vector3(.82f, .72f, .82f));
                break;
            case GeologyDecorVisuals.GeologistStool:
                Box(root, new Vector3(0f, .34f, 0f), new Vector3(.66f, .68f, .66f));
                break;
            case GeologyDecorVisuals.MineralDisplayCase:
                Box(root, new Vector3(0f, .78f, 0f), new Vector3(1.36f, 1.58f, .66f));
                break;
            case GeologyDecorVisuals.CrystalWallSconce:
                Box(root, new Vector3(0f, .68f, -.22f), new Vector3(.42f, .76f, .62f));
                break;
            case GeologyDecorVisuals.StrataMapTable:
                Box(root, new Vector3(0f, .44f, 0f), new Vector3(1.78f, .88f, 1.04f));
                break;
            case GeologyDecorVisuals.SpecimenSideboard:
                Box(root, new Vector3(0f, .55f, 0f), new Vector3(1.84f, 1.12f, .72f));
                break;
            case GeologyDecorVisuals.CrystalCoatRack:
                Box(root, new Vector3(0f, .95f, 0f), new Vector3(.92f, 1.95f, .92f));
                break;
            case GeologyDecorVisuals.GeodeHearthMantel:
                Box(root, new Vector3(0f, .86f, 0f), new Vector3(2.05f, 1.76f, .62f));
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

    private static IReadOnlyList<DecorDefinition> Definitions() => new[]
    {
        new DecorDefinition(
            "Magenheim_Decor_GeodeBowl", GeologyDecorVisuals.GeodeBowl, "Geode Bowl",
            "A low stone specimen bowl holding a small cluster of shaped crystal points.",
            new[] { "piece_table", "wood_floor" },
            Cost("Stone", 3), Cost("Iron", 1), Cost(StructuralCrystalRegistrar.PrefabName, 1)),
        new DecorDefinition(
            "Magenheim_Decor_CutGeodePlaque", GeologyDecorVisuals.CutGeodePlaque, "Cut-Geode Wall Plaque",
            "A framed cut geode mounted as a geological wall specimen.",
            new[] { "woodwall", "wood_wall", "piece_table" },
            Cost("FineWood", 2), Cost("Stone", 2), Cost("Iron", 1), Cost(StructuralCrystalRegistrar.PrefabName, 1)),
        new DecorDefinition(
            "Magenheim_Decor_CrystalEndTable", GeologyDecorVisuals.CrystalEndTable, "Crystal End Table",
            "A compact octagonal stone table with a timber pedestal and crystals tucked beneath the slab.",
            new[] { "piece_table", "wood_floor" },
            Cost("FineWood", 4), Cost("Stone", 3), Cost("Iron", 1), Cost(StructuralCrystalRegistrar.PrefabName, 1)),
        new DecorDefinition(
            "Magenheim_Decor_GeologistStool", GeologyDecorVisuals.GeologistStool, "Geologist's Stool",
            "A three-legged field stool with a stone seat and an underslung crystal specimen.",
            new[] { "piece_chair", "piece_bench01", "wood_floor" },
            Cost("FineWood", 3), Cost("Stone", 2), Cost("Iron", 1), Cost(StructuralCrystalRegistrar.PrefabName, 1)),
        new DecorDefinition(
            "Magenheim_Decor_MineralDisplayCase", GeologyDecorVisuals.MineralDisplayCase, "Mineral Display Case",
            "An iron-framed specimen cabinet displaying cut crystals and a small geode.",
            new[] { "piece_chest_wood", "piece_chest", "piece_table", "wood_floor" },
            Cost("FineWood", 8), Cost("Stone", 3), Cost("Iron", 4), Cost("Bronze", 2), Cost(StructuralCrystalRegistrar.PrefabName, 2)),
        new DecorDefinition(
            "Magenheim_Decor_CrystalWallSconce", GeologyDecorVisuals.CrystalWallSconce, "Crystal Wall Sconce",
            "A stone and bronze wall bracket supporting a luminous faceted crystal.",
            new[] { "woodwall", "wood_wall", "piece_table" },
            Cost("Stone", 2), Cost("Iron", 1), Cost("Bronze", 1), Cost(StructuralCrystalRegistrar.PrefabName, 1), Cost("Resin", 2)),
        new DecorDefinition(
            "Magenheim_Decor_StrataMapTable", GeologyDecorVisuals.StrataMapTable, "Strata Map Table",
            "A broad geological survey table inlaid with layered stone strata and a crystal compass point.",
            new[] { "piece_table", "wood_floor" },
            Cost("FineWood", 8), Cost("Stone", 6), Cost("Bronze", 2), Cost(StructuralCrystalRegistrar.PrefabName, 1)),
        new DecorDefinition(
            "Magenheim_Decor_SpecimenSideboard", GeologyDecorVisuals.SpecimenSideboard, "Specimen Sideboard",
            "A low reinforced cabinet with a stone top reserved for prized mineral samples.",
            new[] { "piece_chest_wood", "piece_chest", "piece_table", "wood_floor" },
            Cost("FineWood", 8), Cost("Stone", 4), Cost("Iron", 2), Cost("Bronze", 2), Cost(StructuralCrystalRegistrar.PrefabName, 2)),
        new DecorDefinition(
            "Magenheim_Decor_CrystalCoatRack", GeologyDecorVisuals.CrystalCoatRack, "Crystal Coat Rack",
            "A free-standing iron-bound timber rack whose hook ends are capped with crystal points.",
            new[] { "piece_table", "wood_floor" },
            Cost("FineWood", 5), Cost("Stone", 3), Cost("Iron", 2), Cost("Bronze", 1), Cost(StructuralCrystalRegistrar.PrefabName, 2)),
        new DecorDefinition(
            "Magenheim_Decor_GeodeHearthMantel", GeologyDecorVisuals.GeodeHearthMantel, "Geode Hearth Mantel",
            "A heavy stone hearth surround crossed by metal bands, mounted geodes, and a crystal seam.",
            new[] { "woodwall", "wood_wall", "piece_table" },
            Cost("Stone", 12), Cost("Iron", 2), Cost("Bronze", 2), Cost(StructuralCrystalRegistrar.PrefabName, 2)),
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterDecor;
        _subscribed = false;
    }

    private sealed class DecorDefinition
    {
        internal DecorDefinition(
            string prefabName,
            string modelId,
            string displayName,
            string description,
            IReadOnlyList<string> sourceCandidates,
            params RequirementConfig[] requirements)
        {
            PrefabName = prefabName;
            ModelId = modelId;
            DisplayName = displayName;
            Description = description;
            SourceCandidates = sourceCandidates;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string ModelId { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal IReadOnlyList<string> SourceCandidates { get; }
        internal RequirementConfig[] Requirements { get; }
    }
}
