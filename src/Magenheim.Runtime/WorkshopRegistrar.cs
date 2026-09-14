using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class WorkshopRegistrar : IDisposable
{
    internal const string StationPrefab = "Magenheim_GeologistWorkstation";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal WorkshopRegistrar(ManualLogSource log) => _log = log;

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterWorkshop;
        _subscribed = true;
    }

    private void RegisterWorkshop()
    {
        if (_registered) return;
        try
        {
            var station = AddPiece(StationPrefab, "piece_workbench", "workstation", "Geologist's Workstation",
                "A stone-topped bench for studying and arranging elemental minerals.",
                null, new[] { Cost("Wood", 10), Cost("Stone", 10), Cost("Flint", 2) });
            var craftingStation = station.GetComponent<CraftingStation>()
                ?? throw new InvalidOperationException("The geologist workstation clone has no CraftingStation.");
            craftingStation.m_name = "Geologist's Workstation";
            craftingStation.m_icon = EarthAssets.Icon("workstation");
            craftingStation.m_craftRequireRoof = false;
            craftingStation.m_craftRequireFire = false;
            craftingStation.m_showBasicRecipies = false;
            craftingStation.m_canRepair = false;
            craftingStation.m_craftingSkill = EarthContentRegistrar.CrystalShapingSkill;
            craftingStation.m_useDistance = 2f;
            craftingStation.m_hoverOffset = 1.2f;

            AddPiece("Magenheim_StationUpgrade_FracturingBlock", "piece_workbench_ext1", "fracturing-block", "Fracturing Block",
                "A banded stone block for controlled mineral fractures. Geologist's Workstation improvement.",
                craftingStation, new[] { Cost("Wood", 10), Cost("Stone", 8), Cost("Flint", 4) });
            AddPiece("Magenheim_StationUpgrade_FacetingWheel", "piece_workbench_ext1", "faceting-wheel", "Faceting Wheel",
                "A treadle-mounted stone wheel for shaping precise facets. Geologist's Workstation improvement.",
                craftingStation, new[] { Cost("FineWood", 10), Cost("Stone", 10), Cost("Bronze", 4) });
            AddPiece("Magenheim_StationUpgrade_ResonanceFrame", "piece_workbench_ext1", "resonance-frame", "Resonance Frame",
                "A suspended mineral within a metal frame. Geologist's Workstation improvement.",
                craftingStation, new[] { Cost("FineWood", 10), Cost("Iron", 4), Cost("Crystal", 5) });
            _registered = true;
            _log.LogInfo("Registered Geologist's Workstation and 3 original station upgrades in Hammer > Crafting.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Workshop registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static RequirementConfig Cost(string item, int amount) => new RequirementConfig(item, amount, 0, true);

    private static GameObject AddPiece(string name, string source, string asset, string displayName, string description,
        CraftingStation? extends, RequirementConfig[] costs)
    {
        if (PrefabManager.Instance.GetPrefab(name))
            throw new InvalidOperationException($"Cannot replace occupied workshop identity '{name}'.");
        var config = new PieceConfig
        {
            Name = displayName,
            Description = description,
            PieceTable = "Hammer",
            Category = "Crafting",
            CraftingStation = extends ? StationPrefab : "piece_workbench",
            ExtendStation = extends ? StationPrefab : string.Empty,
            Icon = EarthAssets.Icon(asset),
            Requirements = costs
        };
        var piece = new CustomPiece(name, source, config);
        var prefab = piece.PiecePrefab;
        piece.Piece.m_dlc = string.Empty;
        var visual = EarthAssets.ReplaceVisual(prefab, asset, buildingPiece: true);
        ConfigureWear(prefab, visual);
        ConfigureColliders(prefab, asset);
        if (extends)
        {
            var extension = prefab.GetComponent<StationExtension>()
                ?? throw new InvalidOperationException($"Missing StationExtension on {name}.");
            extension.m_craftingStation = extends;
            extension.m_maxStationDistance = 5f;
            extension.m_stack = false;
            extension.m_connectionOffset = new Vector3(0, .8f, 0);
            extension.m_hoverOffset = 1f;
        }
        if (!PieceManager.Instance.AddPiece(piece))
            throw new InvalidOperationException($"Jotunn refused workshop piece '{name}'.");
        return prefab;
    }

    private static void ConfigureWear(GameObject prefab, GameObject visual)
    {
        var wear = prefab.GetComponent<WearNTear>()
            ?? throw new InvalidOperationException($"Missing WearNTear on {prefab.name}.");
        wear.m_new = visual;
        wear.m_worn = UnityEngine.Object.Instantiate(visual, prefab.transform);
        wear.m_worn.name = visual.name + ".worn";
        wear.m_broken = UnityEngine.Object.Instantiate(visual, prefab.transform);
        wear.m_broken.name = visual.name + ".weathered";
        Tint(wear.m_worn, .82f);
        Tint(wear.m_broken, .65f);
        wear.m_worn.SetActive(false);
        wear.m_broken.SetActive(false);
        wear.m_health = 400f;
        wear.m_fragmentRoots = new[] { wear.m_new, wear.m_worn, wear.m_broken };
    }

    private static void Tint(GameObject variant, float brightness)
    {
        var renderer = variant.GetComponent<MeshRenderer>();
        var material = new Material(renderer.sharedMaterial) { name = variant.name + ".material" };
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(brightness, brightness, brightness, 1));
        renderer.sharedMaterial = material;
    }

    private static void ConfigureColliders(GameObject prefab, string asset)
    {
        var solids = prefab.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).ToArray();
        var layer = solids.Length > 0 ? solids[0].gameObject.layer : prefab.layer;
        foreach (var collider in solids) collider.enabled = false;
        var collisionRoot = new GameObject("magenheim.collision") { layer = layer };
        collisionRoot.transform.SetParent(prefab.transform, false);
        if (asset == "workstation")
        {
            Box(collisionRoot, new Vector3(0, .93f, 0), new Vector3(1.82f, .14f, .94f));
            Box(collisionRoot, new Vector3(0, .24f, 0), new Vector3(1.4f, .075f, .64f));
            foreach (var x in new[] { -.68f, .68f })
                foreach (var z in new[] { -.32f, .32f })
                    Box(collisionRoot, new Vector3(x, .43f, z), new Vector3(.17f, .86f, .17f));
        }
        else if (asset == "fracturing-block")
        {
            var collider = collisionRoot.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, .36f, 0);
            collider.radius = .37f;
            collider.height = .74f;
        }
        else if (asset == "faceting-wheel")
        {
            Box(collisionRoot, new Vector3(0, .40f, 0), new Vector3(.90f, .8f, .73f));
            Box(collisionRoot, new Vector3(0, 1.03f, 0), new Vector3(.68f, .68f, .22f));
        }
        else
        {
            foreach (var x in new[] { -.49f, .49f })
            {
                Box(collisionRoot, new Vector3(x, .88f, 0), new Vector3(.14f, 1.65f, .14f));
                Box(collisionRoot, new Vector3(x, .09f, 0), new Vector3(.22f, .18f, .83f));
            }
            Box(collisionRoot, new Vector3(0, 1.65f, 0), new Vector3(1.1f, .13f, .15f));
            Box(collisionRoot, new Vector3(0, 1.03f, 0), new Vector3(.76f, .82f, .14f));
        }
    }

    private static void Box(GameObject root, Vector3 center, Vector3 size)
    {
        var collider = root.AddComponent<BoxCollider>();
        collider.center = center;
        collider.size = size;
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterWorkshop;
        _subscribed = false;
    }
}
