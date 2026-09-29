using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Admits the established Deep Fracture expedition as a sparse Fracture Zones dungeon inside the
/// dedicated Underworld instance. It reuses the exact same interior binder/encounter authority as
/// Surface Deep Fractures; only the entrance placement authority is Underworld-specific.
/// </summary>
internal sealed class UnderworldDeepFractureLocationRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly IDeepFractureInteriorBinder _interiorBinder;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldDeepFractureLocationRegistrar(
        IDeepFractureInteriorBinder interiorBinder,
        ManualLogSource log)
    {
        _interiorBinder = interiorBinder ?? throw new ArgumentNullException(nameof(interiorBinder));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        UnderworldDungeonCatalog.Validate();
    }

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
            var definition = UnderworldDungeonCatalog.DeepFracture;
            if (definition.Status != UnderworldDungeonStatus.RuntimeReady)
                throw new InvalidOperationException(
                    "Underworld Deep Fracture registration requires a runtime-ready dungeon definition.");

            if (ZoneManager.Instance.GetZoneLocation(definition.PrefabName) is not null ||
                CustomLocation.IsCustomLocation(definition.PrefabName))
                throw new InvalidOperationException(
                    $"Occupied Underworld dungeon location identity '{definition.PrefabName}'.");

            var container = ZoneManager.Instance.CreateLocationContainer(definition.PrefabName)
                ?? throw new InvalidOperationException(
                    $"Jotunn could not create Underworld dungeon location '{definition.PrefabName}'.");

            DeepFractureEntranceVisuals.Build(container);
            var origin = container.AddComponent<DeepFractureEntranceOrigin>();
            origin.Bind("Fracture Zones");

            var interior = _interiorBinder.AttachInterior(container);
            interior.Validate();

            var config = new LocationConfig
            {
                Biome = UnderworldTerrainRuntime.ToNativeBiome(definition.Biome),
                BiomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
                Quantity = definition.Quantity,
                Priotized = false,
                ExteriorRadius = CheckedFloat(
                    definition.ExteriorRadiusMeters,
                    nameof(definition.ExteriorRadiusMeters)),
                MinAltitude = -1000f,
                MinTerrainDelta = 0f,
                MaxTerrainDelta = CheckedFloat(
                    definition.MaxTerrainDeltaMeters,
                    nameof(definition.MaxTerrainDeltaMeters)),
                MinDistanceFromSimilar = CheckedFloat(
                    definition.MinDistanceFromSimilarMeters,
                    nameof(definition.MinDistanceFromSimilarMeters)),
                Group = definition.Id,
                ClearArea = false,
                RandomRotation = true,
                HasInterior = interior.HasInterior,
                InteriorRadius = interior.InteriorRadius,
                InteriorEnvironment = interior.InteriorEnvironment,
            };

            if (!ZoneManager.Instance.AddCustomLocation(
                    new CustomLocation(container, fixReference: false, config)))
                throw new InvalidOperationException(
                    $"Jotunn refused Underworld Deep Fracture location '{definition.PrefabName}'.");

            _registered = true;
            _log.LogInfo(
                $"Registered {definition.Quantity} sparse Deep Fracture dungeon entrances in " +
                $"{definition.Biome}, minimum spacing {definition.MinDistanceFromSimilarMeters:0}m.");
        }
        catch (Exception exception)
        {
            _log.LogError("Underworld Deep Fracture registration failed: " + exception);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static float CheckedFloat(double value, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) ||
            value < -float.MaxValue || value > float.MaxValue)
            throw new InvalidOperationException(
                $"Underworld dungeon field '{field}' cannot be represented by Valheim's float API.");
        return (float)value;
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable -= RegisterLocation;
        _subscribed = false;
    }
}
