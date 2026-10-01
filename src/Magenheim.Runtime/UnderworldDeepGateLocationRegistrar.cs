using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldDeepGateLocationRegistrar : IDisposable
{
    internal const string LocationName = "Magenheim_DeepGateSite";
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
            var donor = FindNativeGateLocation();
            var nativeClone = donor is null ? null : ZoneManager.Instance.CreateClonedLocation(LocationName, donor.m_prefab.Name);
            var container = nativeClone?.Prefab ?? ZoneManager.Instance.CreateLocationContainer(LocationName)
                ?? throw new InvalidOperationException(
                    $"Jotunn could not create Deep Gate location container '{LocationName}'.");

            var gate = container;
            if (nativeClone is null)
            {
                var arch = UnityEngine.Object.Instantiate(gatePrefab, container.transform, false);
                arch.name = UnderworldDeepGateRegistrar.PrefabName;
                arch.transform.localPosition = Vector3.down * .08f;
                _log.LogWarning("Native cold-biome gate location was not resolved; retained arch fallback. Full gate assembly acceptance remains pending.");
            }
            else
            {
                // Retain the entire authored exterior: stones, bowl, chains, effects and network
                // behaviours. Only the destination changes from the vanilla prison to instance 1.
                foreach (var generator in gate.GetComponentsInChildren<DungeonGenerator>(true))
                    generator.enabled = false;
                foreach (var location in gate.GetComponentsInChildren<Location>(true))
                {
                    location.m_generator = null;
                    location.m_hasInterior = false;
                    location.m_interiorRadius = 0f;
                    if (location.m_interiorTransform && location.m_interiorTransform.IsChildOf(gate.transform) &&
                        location.m_interiorTransform != gate.transform)
                        location.m_interiorTransform.gameObject.SetActive(false);
                }
                _log.LogInfo($"Deep Gate copied complete native location '{donor!.m_prefab.Name}', with {gate.GetComponentsInChildren<Teleport>(true).Length} native transport points.");
            }
            UnderworldDeepGateRegistrar.TraversalAssembly = container;

            var endpoint = gate.GetComponent<UnderworldGateEndpoint>() ?? gate.AddComponent<UnderworldGateEndpoint>();
            endpoint.Role = UnderworldGateRole.EnterUnderworld;
            if (gate.GetComponent<UnderworldDeepGateProgressionRuntime>() == null)
                gate.AddComponent<UnderworldDeepGateProgressionRuntime>();

            // LastBossGate measures roughly 35.8 x 20.1m in plan. Restrict worldgen to a genuinely
            // level 22m-radius foundation instead of accepting the old 8m height delta across 38m,
            // which could place half the monument underground or floating. The location stays
            // upright; random Y rotation remains harmless and gives site variety.
            var config = donor is null ? new LocationConfig() : new LocationConfig(donor);
            config.Biome = ZoneManager.AnyBiomeOf(
                    Heightmap.Biome.Mountain,
                    Heightmap.Biome.DeepNorth);
            config.Quantity = 6;
            config.Priotized = true;
            config.MinDistance = Mathf.Max(config.MinDistance, 2500f);
            config.MinDistanceFromSimilar = 3500f;
            config.Group = "Magenheim_UnderworldAccess";
            config.HasInterior = false;
            if (donor is null)
            {
                config.ExteriorRadius = 22f;
                config.MinAltitude = 8f;
                config.MaxTerrainDelta = .75f;
                config.ClearArea = true;
                config.RandomRotation = true;
            }

            var custom = nativeClone ?? new CustomLocation(container, fixReference: false, config);
            if (nativeClone is not null)
            {
                // Mutate only our private registered row; native placement fields remain intact.
                var placement = custom.ZoneLocation;
                placement.m_biome = config.Biome;
                placement.m_quantity = config.Quantity;
                placement.m_prioritized = true;
                placement.m_minDistance = config.MinDistance;
                placement.m_minDistanceFromSimilar = config.MinDistanceFromSimilar;
                placement.m_group = config.Group;
                placement.m_interiorRadius = 0f;
            }
            else if (!ZoneManager.Instance.AddCustomLocation(custom))
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

    private static ZoneSystem.ZoneLocation? FindNativeGateLocation()
    {
        var zone = ZoneSystem.instance;
        if (!zone) return null;
        foreach (var row in zone.m_locations.ToArray())
        {
            if (row is null || !row.m_enable || !row.m_prefab.IsValid ||
                (row.m_biome & (Heightmap.Biome.DeepNorth | Heightmap.Biome.Mountain)) == 0)
                continue;
            row.m_prefab.Load();
            try
            {
                var prefab = row.m_prefab.Asset;
                if (prefab && prefab.GetComponentsInChildren<Transform>(true).Any(transform =>
                        transform.name.StartsWith("LastBossGate", StringComparison.Ordinal)))
                    return row;
            }
            finally { row.m_prefab.Release(); }
        }
        return null;
    }
}

/// <summary>Client-side presentation follows the server-synchronized global progression key.</summary>
internal sealed class UnderworldDeepGateProgressionRuntime : MonoBehaviour
{
    private UnderworldGateEndpoint? _endpoint;
    private bool _pinEnsured;

    private void Awake() => _endpoint = GetComponent<UnderworldGateEndpoint>();

    private void Start()
    {
        if (_endpoint is null || _endpoint.Role != UnderworldGateRole.EnterUnderworld ||
            gameObject.scene.name.StartsWith("Magenheim_Underworld_", StringComparison.Ordinal) ||
            UnderworldSurfaceGateMigration.IsAllowed(transform.position)) return;
        // Old world saves can still load the obsolete placed site. Retire only this owned gate.
        foreach (var view in GetComponentsInChildren<ZNetView>(true))
            if (view && view.IsValid() && view.IsOwner() && ZNetScene.instance)
                ZNetScene.instance.Destroy(view.gameObject);
        Destroy(gameObject);
    }

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
