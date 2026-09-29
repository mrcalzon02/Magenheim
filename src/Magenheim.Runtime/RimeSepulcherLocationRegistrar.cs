using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class RimeSepulcherLocationRegistrar : IDisposable
{
    private readonly RimeSepulcherInteriorBinder _binder;
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal RimeSepulcherLocationRegistrar(
        RimeSepulcherInteriorBinder binder,
        ManualLogSource log)
    {
        _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Register()
    {
        if (_subscribed || _registered) return;
        if (UnderworldDungeonCatalog.FrozenCaverns.Status !=
            UnderworldDungeonStatus.RuntimeReady)
        {
            _log.LogDebug(
                "Rime Sepulcher remains gated while the Frozen dungeon is Planned.");
            return;
        }

        if (!RimeSepulcherRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException(
                "Rime Sepulcher RuntimeReady payloads are missing: " +
                string.Join(", ", missing));

        ZoneManager.OnVanillaLocationsAvailable += RegisterLocation;
        _subscribed = true;
    }

    private void RegisterLocation()
    {
        if (_registered) return;
        try
        {
            var definition = UnderworldDungeonCatalog.FrozenCaverns;
            definition.Validate();
            UnderworldFrozenRimeSepulcherCatalog.Validate();

            if (ZoneManager.Instance.GetZoneLocation(definition.PrefabName) is not null ||
                CustomLocation.IsCustomLocation(definition.PrefabName))
                throw new InvalidOperationException(
                    "Occupied Rime Sepulcher location identity " + definition.PrefabName);

            var container = ZoneManager.Instance.CreateLocationContainer(definition.PrefabName)
                ?? throw new InvalidOperationException(
                    "Could not create Rime Sepulcher location.");

            RimeSepulcherEntranceVisuals.Build(container);
            var interior = _binder.AttachInterior(container);
            interior.Validate();

            var config = new LocationConfig
            {
                Biome = UnderworldTerrainRuntime.ToNativeBiome(definition.Biome),
                BiomeArea = JotunnWorldgenAdapter.MapArea(
                    Magenheim.Core.Worldgen.SpawnArea.All),
                Quantity = definition.Quantity,
                Priotized = false,
                ExteriorRadius = CheckedFloat(
                    definition.ExteriorRadiusMeters,
                    nameof(definition.ExteriorRadiusMeters)),
                MinAltitude = -1000f,
                MaxAltitude = 1000f,
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
                InteriorEnvironment = interior.Environment,
            };

            if (!ZoneManager.Instance.AddCustomLocation(
                    new CustomLocation(container, fixReference: false, config)))
                throw new InvalidOperationException(
                    "Jotunn refused Rime Sepulcher location.");

            _registered = true;
            _log.LogInfo(
                $"Registered Rime Sepulcher: {definition.Quantity} Frozen Caverns locations, " +
                $"{definition.MinDistanceFromSimilarMeters:0}m spacing.");
        }
        catch (Exception exception)
        {
            _log.LogError("Rime Sepulcher location registration failed: " + exception);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static float CheckedFloat(double value, string field)
    {
        if (double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value < -float.MaxValue ||
            value > float.MaxValue)
            throw new InvalidOperationException(
                $"Rime Sepulcher field '{field}' cannot be represented by Valheim's float API.");
        return (float)value;
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable -= RegisterLocation;
        _subscribed = false;
    }
}
