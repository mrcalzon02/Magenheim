using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the rainbow Crystal Hearth and modular crystal construction pieces.</summary>
internal sealed class CrystalArchitectureRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal CrystalArchitectureRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterArchitecture;
        _subscribed = true;
    }

    private void RegisterArchitecture()
    {
        if (_registered) return;
        try
        {
            foreach (var definition in Definitions())
                RegisterPiece(definition);

            _registered = true;
            _log.LogInfo("Registered Crystal Hearth plus 2/4/8m rainbow crystal beams and 2/4/8m square crystal foundations.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Magenheim crystal architecture registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterPiece(ArchitectureDefinition definition)
    {
        if (PrefabManager.Instance.GetPrefab(definition.PrefabName))
            throw new InvalidOperationException($"Cannot replace occupied architecture identity '{definition.PrefabName}'.");

        var source = ResolveSource(definition.SourceCandidates);
        var config = new PieceConfig
        {
            Name = definition.DisplayName,
            Description = definition.Description,
            PieceTable = "Hammer",
            Category = definition.Category,
            CraftingStation = definition.CraftingStation,
            Requirements = definition.Requirements,
            Icon = CrystalArchitectureIcons.Icon(definition.ModelId)
        };

        var custom = new CustomPiece(definition.PrefabName, source, config);
        var prefab = custom.PiecePrefab;
        custom.Piece.m_dlc = string.Empty;

        var visual = CrystalArchitectureVisuals.Apply(prefab, definition.ModelId);
        ConfigureWear(prefab, visual, definition.Kind == ArchitectureKind.Hearth ? 650f : 900f);
        ConfigureColliders(prefab, definition);
        ConfigureSnapPoints(prefab, definition);

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException($"Jotunn refused crystal architecture piece '{definition.PrefabName}'.");
    }

    private static string ResolveSource(IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (PrefabManager.Instance.GetPrefab(candidate))
                return candidate;
        }
        throw new InvalidOperationException(
            "None of the compatible vanilla architecture source prefabs are available: " + string.Join(", ", candidates));
    }

    private static void ConfigureWear(GameObject prefab, GameObject visual, float health)
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
        wear.m_health = health;
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
            if (material.HasProperty("_EmissionColor"))
            {
                var emission = material.GetColor("_EmissionColor");
                material.SetColor("_EmissionColor", new Color(emission.r * brightness, emission.g * brightness, emission.b * brightness, emission.a));
            }
            renderer.sharedMaterial = material;
        }
    }

    private static void ConfigureColliders(GameObject prefab, ArchitectureDefinition definition)
    {
        var solids = prefab.GetComponentsInChildren<Collider>(true).Where(collider => !collider.isTrigger).ToArray();
        var layer = solids.Length > 0 ? solids[0].gameObject.layer : prefab.layer;
        foreach (var collider in solids) collider.enabled = false;

        var root = new GameObject("magenheim.crystal-architecture.collision") { layer = layer };
        root.transform.SetParent(prefab.transform, false);

        switch (definition.Kind)
        {
            case ArchitectureKind.Hearth:
                Box(root, new Vector3(0f, .55f, .20f), new Vector3(2.25f, 1.15f, 1.45f));
                break;
            case ArchitectureKind.Beam:
                Box(root, new Vector3(0f, definition.Size * .5f, 0f), new Vector3(.52f, definition.Size, .52f));
                break;
            case ArchitectureKind.Foundation:
                Box(root, new Vector3(0f, .17f, 0f), new Vector3(definition.Size, .34f, definition.Size));
                break;
        }
    }

    private static void ConfigureSnapPoints(GameObject prefab, ArchitectureDefinition definition)
    {
        switch (definition.Kind)
        {
            case ArchitectureKind.Beam:
                PlacementSnapAuthority.FitVerticalBeam(prefab, definition.Size);
                break;
            case ArchitectureKind.Foundation:
                PlacementSnapAuthority.FitFoundation(prefab, definition.Size);
                break;
            case ArchitectureKind.Hearth:
                PlacementSnapAuthority.AlignToBase(prefab);
                break;
            default:
                throw new ArgumentOutOfRangeException();
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

    private static IReadOnlyList<ArchitectureDefinition> Definitions() => new[]
    {
        new ArchitectureDefinition(
            "Magenheim_CrystalHearth", CrystalArchitectureVisuals.CrystalHearth,
            "Crystal Hearth", "A broad stone-and-crystal hearth whose living flame burns in a vivid rainbow spectrum.",
            ArchitectureKind.Hearth, 0f, "Furniture", "piece_stonecutter",
            new[] { "hearth", "fire_pit", "bonfire" },
            Cost("Stone", 20), Cost("Iron", 4), Cost("Crystal", 12), Cost("SurtlingCore", 2)),
        new ArchitectureDefinition(
            "Magenheim_CrystalBeam_2m", CrystalArchitectureVisuals.CrystalBeam2,
            "Rainbow Crystal Beam 2m", "A two-meter structural beam grown from fused rainbow crystal facets.",
            ArchitectureKind.Beam, 2f, "Building", "piece_workbench",
            new[] { "wood_pole", "wood_pole2", "wood_beam", "wood_beam_1" },
            Cost("Crystal", 3), Cost("Iron", 1)),
        new ArchitectureDefinition(
            "Magenheim_CrystalBeam_4m", CrystalArchitectureVisuals.CrystalBeam4,
            "Rainbow Crystal Beam 4m", "A four-meter structural beam grown from fused rainbow crystal facets.",
            ArchitectureKind.Beam, 4f, "Building", "piece_workbench",
            new[] { "wood_pole", "wood_pole2", "wood_beam", "wood_beam_1" },
            Cost("Crystal", 6), Cost("Iron", 2)),
        new ArchitectureDefinition(
            "Magenheim_CrystalBeam_8m", CrystalArchitectureVisuals.CrystalBeam8,
            "Rainbow Crystal Beam 8m", "An eight-meter structural beam grown from fused rainbow crystal facets for monumental crystal architecture.",
            ArchitectureKind.Beam, 8f, "Building", "piece_workbench",
            new[] { "wood_pole", "wood_pole2", "wood_beam", "wood_beam_1" },
            Cost("Crystal", 12), Cost("Iron", 4)),
        new ArchitectureDefinition(
            "Magenheim_CrystalFoundation_2m", CrystalArchitectureVisuals.CrystalFoundation2,
            "Rainbow Crystal Foundation 2x2m", "A two-meter square crystalline foundation with rainbow facets locked inside an iron edge frame.",
            ArchitectureKind.Foundation, 2f, "Building", "piece_stonecutter",
            new[] { "stone_floor_2x2", "blackmarble_floor", "wood_floor" },
            Cost("Stone", 4), Cost("Crystal", 4), Cost("Iron", 1)),
        new ArchitectureDefinition(
            "Magenheim_CrystalFoundation_4m", CrystalArchitectureVisuals.CrystalFoundation4,
            "Rainbow Crystal Foundation 4x4m", "A four-meter square crystalline foundation assembled from broad rainbow facets.",
            ArchitectureKind.Foundation, 4f, "Building", "piece_stonecutter",
            new[] { "stone_floor_2x2", "blackmarble_floor", "wood_floor" },
            Cost("Stone", 12), Cost("Crystal", 12), Cost("Iron", 3)),
        new ArchitectureDefinition(
            "Magenheim_CrystalFoundation_8m", CrystalArchitectureVisuals.CrystalFoundation8,
            "Rainbow Crystal Foundation 8x8m", "An eight-meter square monumental crystalline foundation for large halls and crystal structures.",
            ArchitectureKind.Foundation, 8f, "Building", "piece_stonecutter",
            new[] { "stone_floor_2x2", "blackmarble_floor", "wood_floor" },
            Cost("Stone", 32), Cost("Crystal", 32), Cost("Iron", 8))
    };

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterArchitecture;
        _subscribed = false;
    }

    private enum ArchitectureKind
    {
        Hearth,
        Beam,
        Foundation
    }

    private sealed class ArchitectureDefinition
    {
        internal ArchitectureDefinition(
            string prefabName,
            string modelId,
            string displayName,
            string description,
            ArchitectureKind kind,
            float size,
            string category,
            string craftingStation,
            IReadOnlyList<string> sourceCandidates,
            params RequirementConfig[] requirements)
        {
            PrefabName = prefabName;
            ModelId = modelId;
            DisplayName = displayName;
            Description = description;
            Kind = kind;
            Size = size;
            Category = category;
            CraftingStation = craftingStation;
            SourceCandidates = sourceCandidates;
            Requirements = requirements;
        }

        internal string PrefabName { get; }
        internal string ModelId { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        internal ArchitectureKind Kind { get; }
        internal float Size { get; }
        internal string Category { get; }
        internal string CraftingStation { get; }
        internal IReadOnlyList<string> SourceCandidates { get; }
        internal RequirementConfig[] Requirements { get; }
    }
}
