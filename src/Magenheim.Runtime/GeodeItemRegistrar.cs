using System;
using System.Linq;
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
            _log.LogInfo($"Registered {_definitions.Geodes.Count} biome geode item prefab(s) from definition authority.");
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

    private static void RegisterGeodeItem(GeodeDefinition geode)
    {
        if (CustomItem.IsCustomItem(geode.PrefabName))
            throw new InvalidOperationException($"Geode prefab '{geode.PrefabName}' is already registered as a custom item.");

        var customItem = new CustomItem(geode.PrefabName, PlaceholderBasePrefab);
        var shared = customItem.ItemDrop.m_itemData.m_shared;
        var variant = ElementVisualPalette.GeodeVariant(geode);
        var tint = ElementVisualPalette.GeodeTint(geode);

        shared.m_name = BuildDisplayName(geode);
        shared.m_description = BuildDescription(geode);
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_maxStackSize = DefaultStackSize;
        shared.m_weight = DefaultWeight;
        shared.m_value = 0;
        shared.m_dlc = string.Empty;
        shared.m_icons = new[] { EarthAssets.Icon("geode", variant, tint) };
        EarthAssets.ReplaceVisual(customItem.ItemPrefab, "geode", variant: variant, tint: tint);

        if (!ItemManager.Instance.AddItem(customItem))
            throw new InvalidOperationException($"Jotunn refused geode item '{geode.PrefabName}'.");
    }

    private static string BuildDisplayName(GeodeDefinition geode) =>
        $"{ElementVisualPalette.DisplayBiome(geode.Biome)} Geode";

    private static string BuildDescription(GeodeDefinition geode)
    {
        var biome = ElementVisualPalette.DisplayBiome(geode.Biome);
        var total = geode.ElementWeights.Sum(weight => weight.Weight);
        var affinity = string.Join(", ", geode.ElementWeights
            .OrderByDescending(weight => weight.Weight)
            .Select(weight => $"{weight.Element} {weight.Weight / total:P0}"));
        return $"An intact geode recovered from the {biome}. Open it at a Geologist's Workstation to reveal Rough crystals. " +
               $"Observed affinities: {affinity}.";
    }
}
