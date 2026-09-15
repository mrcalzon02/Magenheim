using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.DarkThrone;

namespace Magenheim.Runtime;

/// <summary>
/// Additive-only worldgen boundary for the unique Dark Throne. Existing vanilla or foreign
/// locations are never replaced or mutated if the Magenheim identity is already occupied.
/// </summary>
internal sealed class DarkThroneLocationRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly DarkThroneLocationDefinition _definition;
    private bool _subscribed;
    private bool _registered;

    internal DarkThroneLocationRegistrar(ManualLogSource log, DarkThroneLocationDefinition? definition = null)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _definition = definition ?? DarkThroneLocationCatalog.DarkThrone;
        _definition.Validate();
    }

    internal void Register()
    {
        if (_subscribed || _registered) return;
        ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationsAvailable;
        _subscribed = true;
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable -= OnVanillaLocationsAvailable;
        _subscribed = false;
    }

    private void OnVanillaLocationsAvailable()
    {
        if (_registered) return;
        try
        {
            if (IdentityOccupied())
            {
                _registered = true;
                _log.LogWarning($"Skipped Dark Throne '{_definition.PrefabName}' because that host location identity is already occupied; existing content was left untouched.");
                return;
            }

            var container = ZoneManager.Instance.CreateLocationContainer(_definition.PrefabName)
                ?? throw new InvalidOperationException($"Jotunn could not create Dark Throne location container '{_definition.PrefabName}'.");
            DarkThroneVisuals.Build(container);

            // Recheck after prefab construction so a concurrent registrar cannot be overwritten.
            if (IdentityOccupied())
                throw new InvalidOperationException($"Dark Throne identity '{_definition.PrefabName}' became occupied during registration; Magenheim will not replace it.");

            var custom = new CustomLocation(container, fixReference: false, BuildConfig());
            if (!ZoneManager.Instance.AddCustomLocation(custom))
                throw new InvalidOperationException($"Jotunn refused additive Dark Throne registration for '{_definition.PrefabName}'.");

            _registered = true;
            _log.LogInfo($"Registered unique Dark Throne location '{_definition.PrefabName}' in Mistlands with exterior radius {_definition.ExteriorRadius:0.#}m.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Dark Throne location registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private bool IdentityOccupied() =>
        ZoneManager.Instance.GetZoneLocation(_definition.PrefabName) is not null
        || CustomLocation.IsCustomLocation(_definition.PrefabName);

    private LocationConfig BuildConfig()
    {
        _definition.Validate();
        return new LocationConfig
        {
            Biome = JotunnWorldgenAdapter.MapBiome(_definition.Biome),
            BiomeArea = JotunnWorldgenAdapter.MapArea(_definition.BiomeArea),
            Quantity = _definition.Quantity,
            Priotized = _definition.Prioritized,
            ExteriorRadius = ToFloat(_definition.ExteriorRadius, nameof(_definition.ExteriorRadius)),
            MinAltitude = ToFloat(_definition.MinAltitude, nameof(_definition.MinAltitude)),
            MinTerrainDelta = 0f,
            MaxTerrainDelta = ToFloat(_definition.MaxTerrainDelta, nameof(_definition.MaxTerrainDelta)),
            MinDistanceFromSimilar = ToFloat(_definition.MinDistanceFromSimilar, nameof(_definition.MinDistanceFromSimilar)),
            Group = _definition.Group,
            ClearArea = _definition.ClearArea,
            RandomRotation = _definition.RandomRotation,
            HasInterior = false,
        };
    }

    private static float ToFloat(double value, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < -float.MaxValue || value > float.MaxValue)
            throw new InvalidOperationException($"Dark Throne location field '{field}' cannot be represented by Jotunn's float API.");
        return (float)value;
    }
}
