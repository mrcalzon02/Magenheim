using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldDeepGateLocationRegistrar : IDisposable
{
    internal const string LocationName = "Magenheim_DeepGateSite";
    private const float FootEmbedMetres = 0.08f;
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal UnderworldDeepGateLocationRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_registered || _subscribed) return;
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
            if (ZoneManager.Instance.GetZoneLocation(LocationName) is not null ||
                CustomLocation.IsCustomLocation(LocationName))
            {
                _registered = true;
                _log.LogWarning(
                    $"Deep Gate site identity '{LocationName}' is already occupied; existing content was left untouched.");
                return;
            }

            var gatePrefab = PrefabManager.Instance.GetPrefab(UnderworldDeepGateRegistrar.PrefabName)
                ?? throw new InvalidOperationException(
                    $"Deep Gate prefab '{UnderworldDeepGateRegistrar.PrefabName}' was not registered before location composition.");
            var container = ZoneManager.Instance.CreateLocationContainer(LocationName)
                ?? throw new InvalidOperationException(
                    $"Jotunn could not create Deep Gate location container '{LocationName}'.");

            var gate = UnityEngine.Object.Instantiate(gatePrefab, container.transform, false);
            gate.name = UnderworldDeepGateRegistrar.PrefabName;
            // UnderworldDeepGateRegistrar normalizes the visible Aesir foot to local Y=0. Bury it
            // only a few centimetres so terrain precision cannot leave a daylight seam.
            gate.transform.localPosition = Vector3.down * FootEmbedMetres;
            gate.transform.localRotation = Quaternion.identity;

            var endpoint = gate.GetComponent<UnderworldGateEndpoint>() ?? gate.AddComponent<UnderworldGateEndpoint>();
            endpoint.Role = UnderworldGateRole.EnterUnderworld;
            if (gate.GetComponent<UnderworldDeepGateProgressionRuntime>() == null)
                gate.AddComponent<UnderworldDeepGateProgressionRuntime>();

            // LastBossGate measures roughly 35.8 x 20.1m in plan. Restrict worldgen to a genuinely
            // level 22m-radius foundation instead of accepting the old 8m height delta across 38m,
            // which could place half the monument underground or floating. The location stays
            // upright; random Y rotation remains harmless and gives site variety.
            var config = new LocationConfig
            {
                Biome = ZoneManager.AnyBiomeOf(
                    Heightmap.Biome.Meadows,
                    Heightmap.Biome.BlackForest,
                    Heightmap.Biome.Swamp,
                    Heightmap.Biome.Mountain,
                    Heightmap.Biome.Plains,
                    Heightmap.Biome.Mistlands),
                BiomeArea = Heightmap.BiomeArea.Everything,
                Quantity = 6,
                Priotized = true,
                ExteriorRadius = 22f,
                MinAltitude = 8f,
                MinTerrainDelta = 0f,
                MaxTerrainDelta = 1.5f,
                MinDistanceFromSimilar = 3500f,
                Group = "Magenheim_UnderworldAccess",
                ClearArea = true,
                RandomRotation = true,
                SlopeRotation = false,
                SnapToWater = false,
                HasInterior = false,
            };

            var custom = new CustomLocation(container, fixReference: false, config);
            if (!ZoneManager.Instance.AddCustomLocation(custom))
                throw new InvalidOperationException(
                    $"Jotunn refused additive Deep Gate location registration for '{LocationName}'.");

            _registered = true;
            _log.LogInfo(
                $"Registered persistent surface Deep Gate sites '{LocationName}' on level terrain: " +
                $"{config.ExteriorRadius:0.#}m footprint radius, <= {config.MaxTerrainDelta:0.#}m terrain delta.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Underworld Deep Gate location registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }
}

/// <summary>Client-side presentation follows the server-synchronized global progression key.</summary>
internal sealed class UnderworldDeepGateProgressionRuntime : MonoBehaviour
{
    private UnderworldGateEndpoint? _endpoint;
    private bool _pinEnsured;

    private void Awake() => _endpoint = GetComponent<UnderworldGateEndpoint>();

    private void Update()
    {
        if (_endpoint == null ||
            _endpoint.Role != UnderworldGateRole.EnterUnderworld ||
            !UnderworldProgressionAuthority.IsUnlocked)
            return;

        if (_pinEnsured) return;
        UnderworldProgressionAuthority.EnsureSurfaceGatePin(transform.position);
        _pinEnsured = true;
    }
}
