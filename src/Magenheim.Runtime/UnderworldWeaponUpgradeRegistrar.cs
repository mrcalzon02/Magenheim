using System;
using System.IO;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the first playable Underworld weapon upgrades. The source prefab is always the
/// required Magenheim crystal-grade chassis, so attack behavior, held alignment and the existing
/// authored weapon silhouette are inherited before biome materials visibly accent that chassis.
/// </summary>
internal sealed class UnderworldWeaponUpgradeRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldWeaponUpgradeRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterWeapons;
        _subscribed = true;
    }

    private void RegisterWeapons()
    {
        if (_registered) return;
        var registered = 0;
        try
        {
            foreach (var definition in UnderworldWeaponUpgradeCatalog.FungalForestSlice)
            {
                RegisterWeapon(definition);
                registered++;
            }

            _registered = true;
            _log.LogInfo("Registered " + registered +
                " Fungal Forest crystal-chassis weapon upgrades at the Mycelial Bench.");
        }
        catch (Exception exception)
        {
            _log.LogError("Underworld weapon upgrade registration failed after " + registered +
                " items: " + exception);
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterWeapon(UnderworldWeaponUpgradeDefinition definition)
    {
        RequirePrefab(definition.StationPrefab);
        RequirePrefab(definition.BasePrefab);
        foreach (var ingredient in definition.Ingredients) RequirePrefab(ingredient.Prefab);

        if (PrefabManager.Instance.GetPrefab(definition.Prefab) || CustomItem.IsCustomItem(definition.Prefab))
            throw new InvalidOperationException("Occupied Underworld weapon identity: " + definition.Prefab);

        var item = new CustomItem(definition.Prefab, definition.BasePrefab);
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = definition.Name;
        shared.m_description =
            "A " + definition.Biome +
            " reworking of its Magenheim crystal-grade predecessor. The crystal weapon remains the structural chassis; " +
            "biome materials are added around that inherited design rather than replacing the progression that came before.";
        shared.m_value = 0;
        shared.m_dlc = string.Empty;

        if (HasAuthoredModel(definition.ModelId))
        {
            ModelAssets.Load(item.ItemPrefab, definition.ModelId, item: true);
            if (HasAuthoredIcon(definition.ModelId))
                shared.m_icons = new[] { EarthAssets.Icon(definition.ModelId) };
        }
        else
        {
            // Transitional fallback only. The recipe ancestry is already real; until a derivative
            // .blend has been exported, keep the inherited crystal chassis visible and accent it.
            ApplyAccent(item.ItemPrefab, definition.Accent);
        }

        if (!ItemManager.Instance.AddItem(item))
            throw new InvalidOperationException("Jotunn refused Underworld weapon item " + definition.Prefab);

        var config = new RecipeConfig
        {
            Name = "Magenheim_Recipe_" + definition.Prefab,
            Item = definition.Prefab,
            Amount = 1,
            CraftingStation = definition.StationPrefab,
            RepairStation = definition.StationPrefab,
            MinStationLevel = 1,
            Enabled = true,
        };
        config.AddRequirement(definition.BasePrefab, 1);
        foreach (var ingredient in definition.Ingredients)
            config.AddRequirement(ingredient.Prefab, ingredient.Amount);

        if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
            throw new InvalidOperationException("Jotunn refused Underworld weapon recipe " + config.Name);
    }

    private static void ApplyAccent(GameObject prefab, UnderworldWeaponAccent accent)
    {
        AccentPalette(accent, out var structureTint, out var crystalTint, out var emissionTint);

        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var material = renderer.sharedMaterial;
            if (!material) continue;
            var materialName = material.name ?? string.Empty;
            // Classify by the final family token, not by substring. Every material namespace
            // begins with "magenheim.crystal-weapon", so substring matching ".crystal" incorrectly
            // classified the timber, leather and metal parts as crystal too.
            var isCrystal = HasFamily(materialName, "crystal");
            var isStructure =
                HasFamily(materialName, "timber") ||
                HasFamily(materialName, "leather") ||
                HasFamily(materialName, "metal") ||
                HasFamily(materialName, "silver");
            if (!isCrystal && !isStructure) continue;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var tint = isCrystal ? crystalTint : structureTint;
            if (material.HasProperty("_Color")) block.SetColor("_Color", tint);
            if (material.HasProperty("_BaseColor")) block.SetColor("_BaseColor", tint);
            if (isCrystal && material.HasProperty("_EmissionColor"))
                block.SetColor("_EmissionColor", emissionTint);
            renderer.SetPropertyBlock(block);
        }
    }

    private static bool HasFamily(string value, string family) =>
        value.EndsWith("." + family, StringComparison.OrdinalIgnoreCase);

    private static bool HasAuthoredModel(string modelId)
    {
        var directory = Path.GetDirectoryName(typeof(UnderworldWeaponUpgradeRegistrar).Assembly.Location);
        if (string.IsNullOrEmpty(directory)) return false;
        return File.Exists(Path.Combine(directory, "assets", "models", "runtime", modelId + ".model.json"));
    }

    private static bool HasAuthoredIcon(string modelId)
    {
        var directory = Path.GetDirectoryName(typeof(UnderworldWeaponUpgradeRegistrar).Assembly.Location);
        if (string.IsNullOrEmpty(directory)) return false;
        return File.Exists(Path.Combine(directory, "assets", "earth", modelId + ".icon.png"));
    }

    private static void AccentPalette(
        UnderworldWeaponAccent accent,
        out Color structureTint,
        out Color crystalTint,
        out Color emissionTint)
    {
        switch (accent)
        {
            case UnderworldWeaponAccent.Flowstone:
                structureTint = new Color(0.48f, 0.55f, 0.58f, 1f);
                crystalTint = new Color(0.62f, 0.78f, 0.82f, 1f);
                emissionTint = new Color(0.12f, 0.28f, 0.34f, 1f);
                return;
            case UnderworldWeaponAccent.Emberiron:
                structureTint = new Color(0.48f, 0.30f, 0.20f, 1f);
                crystalTint = new Color(0.92f, 0.52f, 0.24f, 1f);
                emissionTint = new Color(0.52f, 0.16f, 0.05f, 1f);
                return;
            case UnderworldWeaponAccent.Rime:
                structureTint = new Color(0.58f, 0.68f, 0.74f, 1f);
                crystalTint = new Color(0.72f, 0.90f, 1f, 1f);
                emissionTint = new Color(0.18f, 0.42f, 0.58f, 1f);
                return;
            case UnderworldWeaponAccent.Fracture:
                structureTint = new Color(0.46f, 0.42f, 0.50f, 1f);
                crystalTint = new Color(0.78f, 0.64f, 0.96f, 1f);
                emissionTint = new Color(0.34f, 0.18f, 0.56f, 1f);
                return;
            case UnderworldWeaponAccent.Decay:
                structureTint = new Color(0.48f, 0.42f, 0.24f, 1f);
                crystalTint = new Color(0.92f, 0.68f, 0.30f, 1f);
                emissionTint = new Color(0.42f, 0.24f, 0.06f, 1f);
                return;
            default:
                structureTint = new Color(0.48f, 0.58f, 0.34f, 1f);
                crystalTint = new Color(0.64f, 0.92f, 0.70f, 1f);
                emissionTint = new Color(0.14f, 0.36f, 0.20f, 1f);
                return;
        }
    }

    private static void RequirePrefab(string prefab)
    {
        if (PrefabManager.Instance.GetPrefab(prefab) is null && !CustomItem.IsCustomItem(prefab))
            throw new InvalidOperationException("Required Underworld weapon dependency unavailable: " + prefab);
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterWeapons;
        _subscribed = false;
    }
}
