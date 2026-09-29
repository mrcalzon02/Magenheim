using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.DarkThrone;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers a small set of Mistlands boss-location stones that use Valheim's native Vegvisir
/// discovery flow to reveal the unique Dark Throne on the player's map.
/// </summary>
internal sealed class NowhereKingBossStoneRegistrar : IDisposable
{
    internal const string LocationPrefabName = "Magenheim_NowhereKingBossStone";
    private const string SourceVegvisir = "Vegvisir_SeekerQueen";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal NowhereKingBossStoneRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        ZoneManager.OnVanillaLocationsAvailable += RegisterLocation;
        _subscribed = true;
    }

    private void RegisterLocation()
    {
        if (_registered) return;
        try
        {
            if (ZoneManager.Instance.GetZoneLocation(LocationPrefabName) is not null ||
                CustomLocation.IsCustomLocation(LocationPrefabName))
                throw new InvalidOperationException(
                    $"Occupied Nowhere King boss-stone location identity '{LocationPrefabName}'.");

            var source = PrefabManager.Instance.GetPrefab(SourceVegvisir)
                ?? PrefabManager.Instance.GetPrefab("Vegvisir_Eikthyr")
                ?? throw new InvalidOperationException(
                    "Nowhere King boss stone requires a vanilla Vegvisir prefab.");

            var container = ZoneManager.Instance.CreateLocationContainer(LocationPrefabName)
                ?? throw new InvalidOperationException(
                    $"Jotunn could not create boss-stone location container '{LocationPrefabName}'.");

            var stone = UnityEngine.Object.Instantiate(source, container.transform, false);
            stone.name = "Magenheim_NowhereKing_LocationStone";
            stone.transform.localPosition = Vector3.zero;
            stone.transform.localRotation = Quaternion.identity;

            var vegvisir = stone.GetComponent<Vegvisir>()
                ?? stone.GetComponentInChildren<Vegvisir>(true)
                ?? throw new InvalidOperationException(
                    $"Vanilla Vegvisir donor '{source.name}' exposes no Vegvisir component.");

            vegvisir.m_name = "Stone of the Last King";
            vegvisir.m_hoverName = "The Dark Throne";
            vegvisir.m_useText = "Reveal the Dark Throne";
            vegvisir.m_locations.Clear();
            vegvisir.m_locations.Add(new Vegvisir.VegvisrLocation
            {
                m_locationName = DarkThroneLocationCatalog.DarkThrone.PrefabName,
                m_pinName = "The Dark Throne",
                m_pinType = Minimap.PinType.Boss,
                m_discoverAll = false,
                m_showMap = true,
            });

            // Corrupt the donor's presentation without touching the shared vanilla prefab.
            foreach (var renderer in stone.GetComponentsInChildren<Renderer>(true))
            {
                var sourceMaterial = renderer.sharedMaterial;
                if (!sourceMaterial) continue;
                var material = new Material(sourceMaterial)
                {
                    name = "magenheim.nowhere-king-boss-stone." + renderer.name
                };
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", new Color(.035f, .018f, .045f, 1f));
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(.14f, .015f, .30f, 1f));
                }
                renderer.sharedMaterial = material;
            }

            var glowObject = new GameObject("Magenheim_NowhereKingBossStone_Glow");
            glowObject.transform.SetParent(stone.transform, false);
            glowObject.transform.localPosition = Vector3.up * 1.15f;
            var glow = glowObject.AddComponent<Light>();
            glow.color = new Color(.42f, .08f, .72f);
            glow.range = 5.5f;
            glow.intensity = 1.2f;

            var definition = DarkThroneLocationCatalog.DarkThrone;
            var config = new LocationConfig
            {
                Biome = JotunnWorldgenAdapter.MapBiome(definition.Biome),
                BiomeArea = JotunnWorldgenAdapter.MapArea(definition.BiomeArea),
                Quantity = 5,
                Priotized = false,
                ExteriorRadius = 5f,
                MinAltitude = 2f,
                MinTerrainDelta = 0f,
                MaxTerrainDelta = 12f,
                MinDistanceFromSimilar = 1200f,
                Group = "magenheim.nowhere_king_boss_stone",
                ClearArea = false,
                RandomRotation = true,
                HasInterior = false,
            };

            if (!ZoneManager.Instance.AddCustomLocation(
                    new CustomLocation(container, fixReference: false, config)))
                throw new InvalidOperationException(
                    $"Jotunn refused Nowhere King boss-stone location '{LocationPrefabName}'.");

            _registered = true;
            _log.LogInfo(
                $"Registered {config.Quantity} Mistlands Nowhere King boss stones revealing " +
                $"'{DarkThroneLocationCatalog.DarkThrone.PrefabName}'.");
        }
        catch (Exception exception)
        {
            _log.LogError("Nowhere King boss-stone registration failed: " + exception);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable -= RegisterLocation;
        _subscribed = false;
    }
}
