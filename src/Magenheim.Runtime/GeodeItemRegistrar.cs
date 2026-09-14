using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Definitions;

namespace Magenheim.Runtime;

internal sealed class GeodeItemRegistrar : IDisposable
{
    private const string PlaceholderBasePrefab = "Stone";
    private const int DefaultStackSize = 20;
    private const float DefaultWeight = 2f;

    private readonly MagenheimDefinitionSet _definitions;
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal GeodeItemRegistrar(MagenheimDefinitionSet definitions, ManualLogSource log)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Register()
    {
        if (_subscribed || _registered)
            return;

        PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        _subscribed = true;
    }

    public void Dispose()
    {
        if (!_subscribed)
            return;

        PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        _subscribed = false;
    }

    private void OnVanillaPrefabsAvailable()
    {
        if (_registered)
            return;

        try
        {
            foreach (var geode in _definitions.Geodes)
                RegisterGeodeItem(geode);

            _registered = true;
            _log.LogInfo($"Registered {_definitions.Geodes.Count} Magenheim geode item prefab(s) from definition authority.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Magenheim geode item registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private void RegisterGeodeItem(GeodeDefinition geode)
    {
        if (CustomItem.IsCustomItem(geode.PrefabName))
            throw new InvalidOperationException($"Geode prefab '{geode.PrefabName}' is already registered as a custom item.");

        var customItem = new CustomItem(geode.PrefabName, PlaceholderBasePrefab);
        var shared = customItem.ItemDrop.m_itemData.m_shared;

        shared.m_name = BuildDisplayName(geode);
        shared.m_description = BuildDescription(geode);
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_maxStackSize = DefaultStackSize;
        shared.m_weight = DefaultWeight;
        shared.m_value = 0;

        ItemManager.Instance.AddItem(customItem);
    }

    private static string BuildDisplayName(GeodeDefinition geode)
    {
        if (geode.ElementWeights.Count == 1)
            return $"{geode.Biome} {geode.ElementWeights[0].Element} Geode";

        return $"{geode.Biome} Geode";
    }

    private static string BuildDescription(GeodeDefinition geode) =>
        $"An intact geode recovered from the {geode.Biome}. Its crystalline interior can be opened through Crystal Shaping.";
}
