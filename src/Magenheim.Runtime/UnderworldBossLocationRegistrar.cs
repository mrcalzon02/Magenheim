using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers the six progression-critical Underworld boss arenas as ordinary Valheim locations.
/// ZoneSystem owns placement/generated-zone persistence; the existing families only compose content.
/// </summary>
internal sealed class UnderworldBossLocationRegistrar : IDisposable
{
    private readonly UnderworldRuntimeServices _services;
    private readonly ManualLogSource _log;
    private readonly BossLocation[] _locations;
    private bool _subscribed;

    internal UnderworldBossLocationRegistrar(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _locations = new[]
        {
            new BossLocation("Magenheim_Underworld_Boss_FirstBloom",
                FungalMotherbedFamily.CanonicalLocationId, new FungalMotherbedFamily(services)),
            new BossLocation("Magenheim_Underworld_Boss_BlackwaterMaw",
                BlackwaterDrownedRingFamily.CanonicalLocationId, new BlackwaterDrownedRingFamily(services)),
            new BossLocation("Magenheim_Underworld_Boss_FurnaceHeart",
                SulfurFurnaceHeartCalderaFamily.CanonicalLocationId, new SulfurFurnaceHeartCalderaFamily(services)),
            new BossLocation("Magenheim_Underworld_Boss_WhiteSilence",
                FrozenStillvaultFamily.CanonicalLocationId, new FrozenStillvaultFamily(services)),
            new BossLocation("Magenheim_Underworld_Boss_RiftTitan",
                FractureSuspendedCourtFamily.CanonicalLocationId, new FractureSuspendedCourtFamily(services)),
            new BossLocation("Magenheim_Underworld_Boss_CarrionCrown",
                GreatDecayCarrionCrownFamily.CanonicalLocationId, new GreatDecayCarrionCrownFamily(services)),
        };

        UnderworldNativeBossLocationRuntime.Configure(services, _locations, log);
    }

    internal void Register()
    {
        if (_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable += RegisterLocations;
        _subscribed = true;
    }

    private void RegisterLocations()
    {
        try
        {
            foreach (var location in _locations)
            {
                if (ZoneManager.Instance.GetZoneLocation(location.PrefabName) is not null ||
                    CustomLocation.IsCustomLocation(location.PrefabName))
                    throw new InvalidOperationException(
                        $"Occupied native Underworld boss-location identity '{location.PrefabName}'.");

                var container = ZoneManager.Instance.CreateLocationContainer(location.PrefabName)
                    ?? throw new InvalidOperationException(
                        $"Jotunn could not create Underworld boss location '{location.PrefabName}'.");

                var runtime = container.AddComponent<UnderworldNativeBossLocationRuntime>();
                runtime.Bind(location.Family.Kind);

                var config = new LocationConfig
                {
                    Biome = UnderworldTerrainRuntime.ToNativeBiome(location.Family.Biome),
                    BiomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
                    Quantity = 1,
                    Priotized = true,
                    ExteriorRadius = 44f,
                    MinAltitude = -1000f,
                    MinTerrainDelta = 0f,
                    MaxTerrainDelta = 1000f,
                    MinDistanceFromSimilar = 0f,
                    Group = "Magenheim_Underworld_Boss_" + location.Family.Biome,
                    ClearArea = true,
                    RandomRotation = false,
                    HasInterior = false,
                };

                if (!ZoneManager.Instance.AddCustomLocation(
                        new CustomLocation(container, fixReference: false, config)))
                    throw new InvalidOperationException(
                        $"Jotunn refused native Underworld boss location '{location.PrefabName}'.");
            }

            _log.LogInfo(
                $"Registered {_locations.Length} progression boss arenas through native Valheim ZoneSystem locations.");
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
        _subscribed = false;
    }

    internal sealed record BossLocation(
        string PrefabName,
        string CanonicalLocationId,
        IUnderworldBiomeStructureFamily Family);
}

/// <summary>
/// Runs only on an actual location instance in the Underworld Unity scene. The prefab prototype
/// and every Surface scene fail closed, preventing custom-biome content from leaking upward.
/// </summary>
internal sealed class UnderworldNativeBossLocationRuntime : MonoBehaviour
{
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static readonly Dictionary<string, IUnderworldBiomeStructureFamily> Families =
        new(StringComparer.Ordinal);

    [SerializeField] private string _familyKind = string.Empty;
    private bool _composed;

    internal static void Configure(
        UnderworldRuntimeServices services,
        IEnumerable<UnderworldBossLocationRegistrar.BossLocation> locations,
        ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        Families.Clear();
        foreach (var location in locations)
            Families.Add(location.Family.Kind, location.Family);
    }

    internal void Bind(string familyKind)
    {
        if (string.IsNullOrWhiteSpace(familyKind))
            throw new ArgumentException("Boss location family kind is required.", nameof(familyKind));
        _familyKind = familyKind;
    }

    private void Start()
    {
        if (_composed || _services is null || string.IsNullOrWhiteSpace(_familyKind)) return;
        if (!_services.WorldInstances.TryGetContextForScene(gameObject.scene.handle, out var context) ||
            context is null || !context.InstanceId.IsUnderworld)
            return;
        if (_services.InstanceLifecycle.Identity is not { } identity)
            return;
        if (!Families.TryGetValue(_familyKind, out var family))
            throw new InvalidOperationException($"Unknown Underworld boss-location family '{_familyKind}'.");

        var grid = new UnderworldInstanceChunkGrid(_services.TerrainDomain);
        var key = grid.KeyAt(transform.position.x, transform.position.z);
        if (!grid.IntersectsPlayableDomain(key))
        {
            _log?.LogWarning(
                $"Native location '{name}' landed outside the playable Underworld terrain domain; content was not composed.");
            return;
        }

        GameObject? content = null;
        try
        {
            using (ValheimWorldInstanceExecution.Enter(context))
                content = family.Compose(transform.position, identity, key);
            content.transform.SetParent(transform, worldPositionStays: true);
            _composed = true;
            _log?.LogInfo(
                $"Composed native Underworld boss location '{_familyKind}' at chunk {key.X},{key.Z}.");
        }
        catch
        {
            if (content) Destroy(content);
            throw;
        }
    }
}
