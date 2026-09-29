using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class CinderworksLocationRegistrar:IDisposable
{
    private readonly CinderworksInteriorBinder _binder; private readonly ManualLogSource _log;
    private bool _subscribed,_registered;
    internal CinderworksLocationRegistrar(CinderworksInteriorBinder binder,ManualLogSource log)
    {_binder=binder??throw new ArgumentNullException(nameof(binder));_log=log??throw new ArgumentNullException(nameof(log));}

    internal void Register()
    {
        if(_subscribed||_registered)return;
        if(UnderworldDungeonCatalog.SulfurousWastes.Status!=UnderworldDungeonStatus.RuntimeReady)
        {_log.LogDebug("Cinderworks remains gated while the Sulfur dungeon is Planned.");return;}
        if(!CinderworksRoomVisuals.TryGetMissingPayloads(out var missing))
            throw new InvalidOperationException("Cinderworks RuntimeReady payloads missing: "+string.Join(", ",missing));
        ZoneManager.OnVanillaLocationsAvailable+=RegisterLocation;_subscribed=true;
    }

    private void RegisterLocation()
    {
        if(_registered)return;
        try
        {
            var d=UnderworldDungeonCatalog.SulfurousWastes;d.Validate();UnderworldSulfurCinderworksCatalog.Validate();
            if(ZoneManager.Instance.GetZoneLocation(d.PrefabName)is not null||CustomLocation.IsCustomLocation(d.PrefabName))
                throw new InvalidOperationException("Occupied Cinderworks location identity "+d.PrefabName);
            var container=ZoneManager.Instance.CreateLocationContainer(d.PrefabName)??throw new InvalidOperationException("Could not create Cinderworks location.");
            CinderworksEntranceVisuals.Build(container);var interior=_binder.AttachInterior(container);interior.Validate();
            var config=new LocationConfig{
                Biome=UnderworldTerrainRuntime.ToNativeBiome(d.Biome),
                BiomeArea=JotunnWorldgenAdapter.MapArea(Magenheim.Core.Worldgen.SpawnArea.All),
                Quantity=d.Quantity,Priotized=false,ExteriorRadius=(float)d.ExteriorRadiusMeters,
                MinAltitude=-1000f,MaxAltitude=1000f,MinTerrainDelta=0f,MaxTerrainDelta=(float)d.MaxTerrainDeltaMeters,
                MinDistanceFromSimilar=(float)d.MinDistanceFromSimilarMeters,Group=d.Id,ClearArea=false,RandomRotation=true,
                HasInterior=interior.HasInterior,InteriorRadius=interior.InteriorRadius,InteriorEnvironment=interior.Environment};
            if(!ZoneManager.Instance.AddCustomLocation(new CustomLocation(container,fixReference:false,config)))
                throw new InvalidOperationException("Jotunn refused Cinderworks location.");
            _registered=true;_log.LogInfo($"Registered Cinderworks: {d.Quantity} Sulfur locations, {d.MinDistanceFromSimilarMeters:0}m spacing.");
        }
        catch(Exception e){_log.LogError("Cinderworks location registration failed: "+e);throw;}
        finally{Dispose();}
    }
    public void Dispose(){if(!_subscribed)return;ZoneManager.OnVanillaLocationsAvailable-=RegisterLocation;_subscribed=false;}
}
