using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Content-only native items and console pickup prototypes. No world placement or save layer.</summary>
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
                shared.m_description = entry.Biome + " raw material. Vanilla appearance placeholder; natural harvesting and recipes pending.";
                shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
                shared.m_maxStackSize = 50;
                shared.m_weight = 1f;
                shared.m_value = 0;
                shared.m_dlc = string.Empty;
                shared.m_food = 0f; shared.m_foodStamina = 0f; shared.m_foodEitr = 0f;
                shared.m_foodBurnTime = 0f; shared.m_foodRegen = 0f;
                shared.m_consumeStatusEffect = null;
                var model = UnderworldResourceVisuals.ModelFor(entry.Prefab);
                if (model is not null)
                {
                    shared.m_description = entry.Biome + " raw material. Natural harvesting and recipes pending.";
                    shared.m_icons = new[] { EarthAssets.Icon(model) };
                    UnderworldResourceVisuals.Apply(item.ItemPrefab, model);
                }
                if (!ItemManager.Instance.AddItem(item)) throw new InvalidOperationException("Item registration refused: " + entry.Prefab);
                items++;
                var clone = PrefabManager.Instance.CreateClonedPrefab(entry.PickupPrefab, source);
                var pickable = clone.GetComponent<Pickable>();
                pickable.m_itemPrefab = item.ItemPrefab;
                pickable.m_overrideName = entry.Name + " (resource prototype)";
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
                if (model is not null) pickable.m_hideWhenPicked = UnderworldResourceVisuals.Apply(clone, model);
                PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
                pickups++;
            }
            catch (Exception error) { _log.LogError("Underworld resource " + entry.Name + ": " + error); }
        }
        _log.LogInfo($"Underworld resource prototypes: {items}/{UnderworldResourceCatalog.All.Count} items and {pickups} native pickups. Console-only; scenery and creature donor loot unchanged.");
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
    }

    public void Dispose()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _deepSigilCoreRegistrar.Dispose();
    }
}
