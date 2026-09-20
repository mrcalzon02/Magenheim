using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the dedicated Magenheim enchanting altar: a low ritual dais used for socketing
/// and crystal installation while leaving mineral refinement at the Geologist's Workstation.
/// </summary>
internal sealed class CrystalEnchantingDaisRegistrar : IDisposable
{
    internal const string PrefabName = "Magenheim_CrystalEnchantingDais";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal CrystalEnchantingDaisRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterDais;
        _subscribed = true;
    }

    private void RegisterDais()
    {
        if (_registered) return;

        try
        {
            if (PrefabManager.Instance.GetPrefab(PrefabName))
                throw new InvalidOperationException($"Cannot replace occupied enchanting dais identity '{PrefabName}'.");
            if (PrefabManager.Instance.GetPrefab("piece_workbench") is null)
                throw new InvalidOperationException("Vanilla workbench source is unavailable for Crystal Enchanting Dais registration.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must exist before the Crystal Enchanting Dais is registered.");
            if (PrefabManager.Instance.GetPrefab(CrystalAlchemyRegistrar.CrystalDustPrefab) is null)
                throw new InvalidOperationException("Crystal Dust must exist before the Crystal Enchanting Dais is registered.");

            var config = new PieceConfig
            {
                Name = "Crystal Enchanting Dais",
                Description =
                    "A low ritual altar patterned after the ancient central standing-stone dais. " +
                    "Iron channels and elemental crystal nodes focus the recessed central crystal. " +
                    "This is where crystals are set into gear: open a socket, fit a crystal, or draw one back out. " +
                    "Nothing is crafted or refined here.",
                PieceTable = "Hammer",
                Category = "Crafting",
                CraftingStation = WorkshopRegistrar.StationPrefab,
                Icon = CrystalEnchantingDaisIcons.Icon(),
                Requirements = new[]
                {
                    Cost("Stone", 20),
                    Cost("Iron", 8),
                    Cost(StructuralCrystalRegistrar.PrefabName, 4),
                    Cost(CrystalAlchemyRegistrar.CrystalDustPrefab, 4),
                }
            };

            var custom = new CustomPiece(PrefabName, "piece_workbench", config);
            custom.Piece.m_dlc = string.Empty;
            var prefab = custom.PiecePrefab;
            var visual = CrystalEnchantingDaisVisuals.Apply(prefab);

            var station = prefab.GetComponent<CraftingStation>()
                ?? throw new InvalidOperationException("Crystal Enchanting Dais clone has no CraftingStation component.");
            station.m_name = "Crystal Enchanting Dais";
            station.m_icon = CrystalEnchantingDaisIcons.Icon();
            station.m_craftRequireRoof = false;
            station.m_craftRequireFire = false;
            station.m_showBasicRecipies = false;
            station.m_canRepair = false;
            station.m_craftingSkill = EarthContentRegistrar.CrystalShapingSkill;
            station.m_useDistance = 2.8f;
            station.m_hoverOffset = .62f;

            ConfigureCollider(prefab);
            PlacementSnapAuthority.FitRadialBase(prefab, 1.82f);
            ConfigureWear(prefab, visual);

            if (!PieceManager.Instance.AddPiece(custom))
                throw new InvalidOperationException($"Jotunn refused Crystal Enchanting Dais piece '{PrefabName}'.");

            _registered = true;
            _log.LogInfo("Registered Crystal Enchanting Dais as a dedicated Magenheim socket/enchantment station.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Crystal Enchanting Dais registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void ConfigureCollider(GameObject prefab)
    {
        var solids = prefab.GetComponentsInChildren<Collider>(true)
            .Where(collider => !collider.isTrigger)
            .ToArray();
        var layer = solids.Length > 0 ? solids[0].gameObject.layer : prefab.layer;
        foreach (var collider in solids) collider.enabled = false;

        var collisionRoot = new GameObject("magenheim.crystal-enchanting-dais.collision") { layer = layer };
        collisionRoot.transform.SetParent(prefab.transform, false);

        var baseCollider = collisionRoot.AddComponent<BoxCollider>();
        baseCollider.center = new Vector3(0f, .23f, 0f);
        baseCollider.size = new Vector3(3.64f, .46f, 3.64f);
    }

    private static void ConfigureWear(GameObject prefab, GameObject visual)
    {
        var wear = prefab.GetComponent<WearNTear>();
        if (!wear) return;

        wear.m_health = Mathf.Max(wear.m_health, 1400f);
        wear.m_new = visual;

        var worn = UnityEngine.Object.Instantiate(visual, prefab.transform);
        worn.name = visual.name + ".worn";
        TintVariant(worn, .82f);
        worn.SetActive(false);

        var broken = UnityEngine.Object.Instantiate(visual, prefab.transform);
        broken.name = visual.name + ".weathered";
        TintVariant(broken, .66f);
        broken.SetActive(false);

        wear.m_worn = worn;
        wear.m_broken = broken;
        wear.m_fragmentRoots = new[] { wear.m_new, wear.m_worn, wear.m_broken };
    }

    private static void TintVariant(GameObject root, float brightness)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var material = new Material(renderer.sharedMaterial)
            {
                name = renderer.sharedMaterial.name + ".weathered"
            };
            if (material.HasProperty("_Color"))
            {
                var color = material.GetColor("_Color");
                material.SetColor("_Color", new Color(
                    color.r * brightness,
                    color.g * brightness,
                    color.b * brightness,
                    color.a));
            }
            renderer.sharedMaterial = material;
        }
    }

    private static RequirementConfig Cost(string item, int amount) =>
        new RequirementConfig(item, amount, 0, true);

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterDais;
        _subscribed = false;
    }
}
