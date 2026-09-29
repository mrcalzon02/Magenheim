using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// First Underworld economy station. It is intentionally donor-backed for this slice:
/// the station behavior is native Valheim, while its recipe inputs are already real
/// Fungal Forest resources. Authored station geometry is a later visual replacement.
/// </summary>
internal sealed class UnderworldMycelialBenchRegistrar : IDisposable
{
    private const string DonorPrefab = "piece_workbench";
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldMycelialBenchRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterBench;
        _subscribed = true;
    }

    private void RegisterBench()
    {
        if (_registered) return;
        try
        {
            RequirePrefab(DonorPrefab);
            RequirePrefab("Magenheim_Underworld_Resource_WorldrootTimber");
            RequirePrefab("Magenheim_Underworld_Resource_Understone");
            RequirePrefab("Magenheim_Underworld_Resource_SpireFibre");

            if (PrefabManager.Instance.GetPrefab(UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab))
                throw new InvalidOperationException("Occupied Mycelial Bench prefab identity.");

            var config = new PieceConfig
            {
                Name = "Mycelial Bench",
                Description = "The first Underworld crafting station, grown from Worldroot and anchored in Understone.",
                PieceTable = "Hammer",
                Category = "Crafting",
                CraftingStation = string.Empty,
                Requirements = new[]
                {
                    new RequirementConfig("Magenheim_Underworld_Resource_WorldrootTimber", 10, 0, true),
                    new RequirementConfig("Magenheim_Underworld_Resource_Understone", 6, 0, true),
                    new RequirementConfig("Magenheim_Underworld_Resource_SpireFibre", 4, 0, true),
                },
            };

            var custom = new CustomPiece(
                UnderworldWeaponUpgradeCatalog.MycelialBenchPrefab,
                DonorPrefab,
                config);

            var station = custom.PiecePrefab.GetComponent<CraftingStation>()
                ?? throw new InvalidOperationException("Workbench donor lost CraftingStation behavior.");
            station.m_name = "Mycelial Bench";

            ApplyWorldrootAccent(custom.PiecePrefab);

            if (!PieceManager.Instance.AddPiece(custom))
                throw new InvalidOperationException("Jotunn refused the Mycelial Bench.");

            _registered = true;
            _log.LogInfo("Registered Mycelial Bench from real Worldroot Timber, Understone and Spire Fibre.");
        }
        catch (Exception exception)
        {
            _log.LogError("Mycelial Bench registration failed: " + exception);
        }
        finally
        {
            Dispose();
        }
    }

    private static void ApplyWorldrootAccent(GameObject prefab)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var material = renderer.sharedMaterial;
            if (!material) continue;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var tint = new Color(0.48f, 0.58f, 0.34f, 1f);
            if (material.HasProperty("_Color")) block.SetColor("_Color", tint);
            if (material.HasProperty("_BaseColor")) block.SetColor("_BaseColor", tint);
            renderer.SetPropertyBlock(block);
        }
    }

    private static void RequirePrefab(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null && !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException("Required Mycelial Bench dependency unavailable: " + prefab);
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterBench;
        _subscribed = false;
    }
}
