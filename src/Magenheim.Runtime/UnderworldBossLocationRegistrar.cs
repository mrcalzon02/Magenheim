using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

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
    internal static IReadOnlyList<BossLocation> Locations { get; private set; } = Array.Empty<BossLocation>();
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
        Locations = Array.AsReadOnly(_locations);

        UnderworldNativeStructureLocationRuntime.Configure(services, log);
        foreach (var location in _locations)
            UnderworldNativeStructureLocationRuntime.RegisterFamily(location.Family);
    }

    internal void Register()
    {
        if (_subscribed) return;
        UnderworldBossTrophyRegistrar.Register(_log);
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

                var runtime = container.AddComponent<UnderworldNativeStructureLocationRuntime>();
                runtime.Bind(location.Family.Kind);

                var config = new LocationConfig
                {
                    Biome = UnderworldTerrainRuntime.ToNativeBiome(location.Family.Biome),
                    BiomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
                    Quantity = 1,
                    Priotized = true,
                    ExteriorRadius = 24f,
                    MinAltitude = location.Family.Biome == UnderworldTerrainBiome.BlackwaterDeep ? -30f : 1f,
                    MaxAltitude = location.Family.Biome == UnderworldTerrainBiome.BlackwaterDeep ? -5f : 6000f,
                    MinTerrainDelta = 0f,
                    MaxTerrainDelta = 18f,
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
