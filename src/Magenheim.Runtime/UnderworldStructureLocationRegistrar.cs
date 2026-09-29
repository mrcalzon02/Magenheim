using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Migrates repeatable biome structures, navigation landmarks and Deep Sigils onto Valheim's
/// ordinary location generator. The family composers remain unchanged content authority.
/// </summary>
internal sealed class UnderworldStructureLocationRegistrar : IDisposable
{
    private enum LocationClass { Repeatable, Landmark, DeepSigil }

    private sealed record LocationSpec(
        IUnderworldBiomeStructureFamily Family,
        LocationClass Class);

    private readonly ManualLogSource _log;
    private readonly LocationSpec[] _locations;
    private bool _subscribed;

    internal UnderworldStructureLocationRegistrar(UnderworldRuntimeServices services, ManualLogSource log)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _locations = new[]
        {
            // Fungal Forest.
            new LocationSpec(new FungalSporeCairnFamily(), LocationClass.Repeatable),
            new LocationSpec(new FungalRootMassFamily(), LocationClass.Repeatable),
            new LocationSpec(new FungalSplitPillarLandmarkFamily(), LocationClass.Landmark),
            new LocationSpec(new FungalDeepSigilFamily(), LocationClass.DeepSigil),

            // Blackwater Deep.
            new LocationSpec(new BlackwaterSparsePocketFamily(), LocationClass.Repeatable),
            new LocationSpec(new BlackwaterWorldrootSpanLandmarkFamily(), LocationClass.Landmark),
            new LocationSpec(new BlackwaterDeepSigilFamily(), LocationClass.DeepSigil),

            // Sulfurous Wastes.
            new LocationSpec(new SulfurVentCairnFamily(), LocationClass.Repeatable),
            new LocationSpec(new SulfurThreeLavafallsLandmarkFamily(), LocationClass.Landmark),
            new LocationSpec(new SulfurDeepSigilFamily(), LocationClass.DeepSigil),

            // Frozen Caverns.
            new LocationSpec(new FrozenShardFanFamily(), LocationClass.Repeatable),
            new LocationSpec(new FrozenWallLandmarkFamily(), LocationClass.Landmark),
            new LocationSpec(new FrozenDeepSigilFamily(), LocationClass.DeepSigil),

            // Fracture Zones.
            new LocationSpec(new FractureFaultLineFamily(), LocationClass.Repeatable),
            new LocationSpec(new FractureCliffMonasteryFamily(), LocationClass.Repeatable),
            new LocationSpec(new FractureSuspendedRoadStationFamily(), LocationClass.Repeatable),
            new LocationSpec(new FractureAnchorTowerFamily(), LocationClass.Repeatable),
            new LocationSpec(new FractureGreatBridgeFamily(), LocationClass.Repeatable),
            new LocationSpec(new BrokenAncientBridgeLandmarkFamily(), LocationClass.Landmark),
            new LocationSpec(new FractureDeepSigilFamily(), LocationClass.DeepSigil),

            // Great Decay.
            new LocationSpec(new DecayRootCorridorFamily(), LocationClass.Repeatable),
            new LocationSpec(new GreatDecayVanishingRoadLandmarkFamily(), LocationClass.Landmark),
            new LocationSpec(new GreatDecayDeepSigilFamily(), LocationClass.DeepSigil),
        };

        UnderworldNativeStructureLocationRuntime.Configure(services, log);
        foreach (var location in _locations)
            UnderworldNativeStructureLocationRuntime.RegisterFamily(location.Family);
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
                RegisterLocation(location);

            var repeatable = Array.FindAll(_locations, value => value.Class == LocationClass.Repeatable).Length;
            var landmarks = Array.FindAll(_locations, value => value.Class == LocationClass.Landmark).Length;
            var sigils = Array.FindAll(_locations, value => value.Class == LocationClass.DeepSigil).Length;
            _log.LogInfo(
                $"Registered {repeatable} repeatable structures, {landmarks} navigation landmarks and " +
                $"{sigils} Deep Sigils through native Valheim ZoneSystem locations.");
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterLocation(LocationSpec location)
    {
        var family = location.Family;
        var prefabName = "Magenheim_Underworld_Location_" + family.Kind.Replace('-', '_');

        if (ZoneManager.Instance.GetZoneLocation(prefabName) is not null ||
            CustomLocation.IsCustomLocation(prefabName))
            throw new InvalidOperationException(
                $"Occupied native Underworld location identity '{prefabName}'.");

        var container = ZoneManager.Instance.CreateLocationContainer(prefabName)
            ?? throw new InvalidOperationException(
                $"Jotunn could not create native Underworld location '{prefabName}'.");
        container.AddComponent<UnderworldNativeStructureLocationRuntime>().Bind(family.Kind);

        var quantity = location.Class switch
        {
            LocationClass.DeepSigil => 1,
            LocationClass.Landmark => 4,
            _ => 24,
        };
        var radius = location.Class switch
        {
            LocationClass.DeepSigil => 36f,
            LocationClass.Landmark => 72f,
            _ => 44f,
        };
        var spacing = location.Class switch
        {
            LocationClass.DeepSigil => 0f,
            LocationClass.Landmark => 1200f,
            _ => 320f,
        };

        var config = new LocationConfig
        {
            Biome = UnderworldTerrainRuntime.ToNativeBiome(family.Biome),
            BiomeArea = JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
            Quantity = quantity,
            Priotized = location.Class != LocationClass.Repeatable,
            ExteriorRadius = radius,
            MinAltitude = -1000f,
            MinTerrainDelta = 0f,
            MaxTerrainDelta = 1000f,
            MinDistanceFromSimilar = spacing,
            Group = "Magenheim_Underworld_" + location.Class + "_" + family.Biome,
            ClearArea = false,
            RandomRotation = true,
            HasInterior = false,
        };

        if (!ZoneManager.Instance.AddCustomLocation(
                new CustomLocation(container, fixReference: false, config)))
            throw new InvalidOperationException(
                $"Jotunn refused native Underworld location '{prefabName}'.");
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
        _subscribed = false;
    }
}
