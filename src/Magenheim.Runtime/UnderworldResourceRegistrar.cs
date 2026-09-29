using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers Underworld resource items and their natural pickups through Valheim/Jotunn vegetation.
/// World placement, generated-zone state and picked-state persistence remain native Valheim authority.
/// </summary>
internal sealed class UnderworldResourceRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly UnderworldDeepSigilCoreRegistrar _deepSigilCoreRegistrar;

    internal UnderworldResourceRegistrar(ManualLogSource log)
    {
        _log = log;
        _deepSigilCoreRegistrar = new UnderworldDeepSigilCoreRegistrar(log);
    }

    internal void Register()
    {
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _deepSigilCoreRegistrar.Register();
    }

    private void RegisterContent()
    {
        var items = 0;
        var pickups = 0;
        foreach (var entry in UnderworldResourceCatalog.All)
        {
            try
            {
                var donor = PrefabManager.Instance.GetPrefab(entry.ItemDonor);
                var source = PrefabManager.Instance.GetPrefab(entry.PickupDonor);
                if (!donor || !donor.GetComponent<ItemDrop>() || !source || !source.GetComponent<Pickable>() || !source.GetComponent<ZNetView>())
                    throw new InvalidOperationException("Missing native item/pickup donor for " + entry.Name);
                if (PrefabManager.Instance.GetPrefab(entry.Prefab) || CustomItem.IsCustomItem(entry.Prefab)
                    || PrefabManager.Instance.GetPrefab(entry.PickupPrefab))
                    throw new InvalidOperationException("Occupied resource identity " + entry.Prefab);
                var item = new CustomItem(entry.Prefab, entry.ItemDonor);
                var shared = item.ItemDrop.m_itemData.m_shared;
                shared.m_name = entry.Name;
                shared.m_description = entry.Biome + " raw material used by the Underworld refining and crafting economy.";
                shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
                shared.m_maxStackSize = 50;
                shared.m_weight = 1f;
                shared.m_value = 0;
                shared.m_dlc = string.Empty;
                shared.m_food = 0f; shared.m_foodStamina = 0f; shared.m_foodEitr = 0f;
                shared.m_foodBurnTime = 0f; shared.m_foodRegen = 0f;
                shared.m_consumeStatusEffect = null;
                var model = UnderworldResourceVisuals.ModelFor(entry.Prefab)
                    ?? throw new InvalidOperationException("Missing authored Underworld raw-resource visual mapping: " + entry.Prefab);
                shared.m_icons = new[] { EarthAssets.Icon(model) };
                UnderworldResourceVisuals.Apply(item.ItemPrefab, model);
                if (!ItemManager.Instance.AddItem(item)) throw new InvalidOperationException("Item registration refused: " + entry.Prefab);
                items++;
                var clone = PrefabManager.Instance.CreateClonedPrefab(entry.PickupPrefab, source);
                var pickable = clone.GetComponent<Pickable>();
                pickable.m_itemPrefab = item.ItemPrefab;
                pickable.m_overrideName = entry.Name;
                pickable.m_amount = 1;
                pickable.m_minAmountScaled = 1;
                pickable.m_dontScale = true;
                pickable.m_extraDrops = new DropTable();
                pickable.m_respawnTimeMinutes = 0f;
                pickable.m_respawnTimeInitMin = 0f;
                pickable.m_respawnTimeInitMax = 0f;
                pickable.m_defaultPicked = false;
                pickable.m_defaultEnabled = true;
                pickable.m_maxLevelBonusChance = 0f;
                pickable.m_bonusYieldAmount = 0;
                pickable.m_hideWhenPicked = UnderworldResourceVisuals.Apply(clone, model);
                // Keep both native Pickable lifecycles: authored visuals retain the picked ZDO;
                // one-shot donors without a hide target are removed by native ZNetView.Destroy.
                // ZoneSystem owns generated-zone persistence, so neither needs a custom respawn loop.
                var biomeResourceCount = System.Linq.Enumerable.Count(
                    UnderworldResourceCatalog.All,
                    value => value.Biome == entry.Biome);
                var targetPerZone = UnderworldResourcePlacementPlanner.SlotsPerCell / (float)biomeResourceCount;
                var minPerZone = Mathf.Max(0.25f, targetPerZone - 0.75f);
                var maxPerZone = targetPerZone + 0.75f;

                var vegetationConfig = new VegetationConfig
                {
                    Biome = UnderworldTerrainRuntime.ToNativeBiome(entry.Biome),
                    BiomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
                    BlockCheck = true,
                    ForcePlacement = false,
                    Min = minPerZone,
                    Max = maxPerZone,
                    MinAltitude = -1000f,
                    MaxAltitude = 1000f,
                    MinOceanDepth = 0f,
                    MaxOceanDepth = 0f,
                    MinTerrainDelta = 0f,
                    MaxTerrainDelta = 1000f,
                    TerrainDeltaRadius = 2f,
                    MinTilt = 0f,
                    MaxTilt = 55f,
                    InForest = false,
                    ForestThresholdMin = 0f,
                    ForestThresholdMax = 1f,
                    ScaleMin = 0.9f,
                    ScaleMax = 1.1f,
                    GroupSizeMin = 1,
                    GroupSizeMax = 1,
                    GroupRadius = 0f,
                    GroundOffset = 0f,
                };

                var vegetation = new CustomVegetation(clone, fixReference: true, vegetationConfig);
                if (!ZoneManager.Instance.AddCustomVegetation(vegetation))
                    throw new InvalidOperationException("Jotunn refused native vegetation registration for " + entry.PickupPrefab);
                pickups++;
            }
            catch (Exception error) { _log.LogError("Underworld resource " + entry.Name + ": " + error); }
        }
        _log.LogInfo($"Underworld resources: {items}/{UnderworldResourceCatalog.All.Count} items and {pickups} native ZoneSystem vegetation pickups registered across six Underworld-only biome flags. Placement and picked-state persistence are Valheim-owned.");
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
    }

    public void Dispose()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _deepSigilCoreRegistrar.Dispose();
    }
}
