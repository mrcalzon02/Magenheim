using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class DrownedVaultLocationRegistrar : IDisposable
{
    private readonly DrownedVaultInteriorBinder _binder;
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal DrownedVaultLocationRegistrar(
        DrownedVaultInteriorBinder binder,
        ManualLogSource log)
    {
        _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Register()
    {
        if (_subscribed || _registered) return;
        if (UnderworldDungeonCatalog.BlackwaterDeep.Status != UnderworldDungeonStatus.RuntimeReady)
        {
            _log.LogDebug(
                "Drowned Vault location remains gated while the Blackwater dungeon is Planned.");
            return;
        }

        if (!DrownedVaultRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException(
                "Drowned Vaults are RuntimeReady but authored model payloads are missing: " +
                string.Join(", ", missing));

        ZoneManager.OnVanillaLocationsAvailable += RegisterLocation;
        _subscribed = true;
    }

    private void RegisterLocation()
    {
        if (_registered) return;
        try
        {
            var definition = UnderworldDungeonCatalog.BlackwaterDeep;
            definition.Validate();
            UnderworldBlackwaterDrownedVaultsCatalog.Validate();
            DrownedVaultWaterRuntime.ValidateDonor();
            if (definition.Status != UnderworldDungeonStatus.RuntimeReady)
                throw new InvalidOperationException(
                    "Drowned Vault location admission requires RuntimeReady catalog status.");

            if (ZoneManager.Instance.GetZoneLocation(definition.PrefabName) is not null ||
                CustomLocation.IsCustomLocation(definition.PrefabName))
                throw new InvalidOperationException(
                    $"Occupied Drowned Vault location identity '{definition.PrefabName}'.");

            var container = ZoneManager.Instance.CreateLocationContainer(definition.PrefabName)
                ?? throw new InvalidOperationException(
                    $"Jotunn could not create Drowned Vault location '{definition.PrefabName}'.");

            DrownedVaultEntranceVisuals.Build(container);
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
                SnapToWater = true,
                HasInterior = interior.HasInterior,
                InteriorRadius = interior.InteriorRadius,
                InteriorEnvironment = interior.Environment,
            };

            if (!ZoneManager.Instance.AddCustomLocation(
                    new CustomLocation(container, fixReference: false, config)))
                throw new InvalidOperationException(
                    $"Jotunn refused Drowned Vault location '{definition.PrefabName}'.");

            _registered = true;
            _log.LogInfo(
                $"Registered Drowned Vaults in {definition.Biome}: " +
                $"{definition.Quantity} water-snapped locations, minimum spacing " +
                $"{definition.MinDistanceFromSimilarMeters:0}m.");
        }
        catch (Exception exception)
        {
            _log.LogError("Drowned Vault location registration failed: " + exception);
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
                $"Drowned Vault field '{field}' cannot be represented by Valheim's float API.");
        return (float)value;
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable -= RegisterLocation;
        _subscribed = false;
    }
}
