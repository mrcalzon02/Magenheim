using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldDeepGateLocationRegistrar : IDisposable
{
    internal const string LocationName="Magenheim_DeepGateSite";private readonly ManualLogSource _log;private bool _subscribed;private bool _registered;
    internal UnderworldDeepGateLocationRegistrar(ManualLogSource log){_log=log??throw new ArgumentNullException(nameof(log));}
    internal void Register(){if(_registered||_subscribed)return;ZoneManager.OnVanillaLocationsAvailable+=OnVanillaLocationsAvailable;_subscribed=true;}
    public void Dispose(){if(!_subscribed)return;ZoneManager.OnVanillaLocationsAvailable-=OnVanillaLocationsAvailable;_subscribed=false;}
    private void OnVanillaLocationsAvailable()
    {
        if(_registered)return;try
        {
            if(ZoneManager.Instance.GetZoneLocation(LocationName)is not null||CustomLocation.IsCustomLocation(LocationName)){_registered=true;_log.LogWarning($"Deep Gate site identity '{LocationName}' is already occupied; existing content was left untouched.");return;}
            var gatePrefab=PrefabManager.Instance.GetPrefab(UnderworldDeepGateRegistrar.PrefabName)??throw new InvalidOperationException($"Deep Gate prefab '{UnderworldDeepGateRegistrar.PrefabName}' was not registered before location composition.");
            var container=ZoneManager.Instance.CreateLocationContainer(LocationName)??throw new InvalidOperationException($"Jotunn could not create Deep Gate location container '{LocationName}'.");
            var gate=UnityEngine.Object.Instantiate(gatePrefab,container.transform,false);gate.name=UnderworldDeepGateRegistrar.PrefabName;gate.transform.localPosition=Vector3.zero;gate.transform.localRotation=Quaternion.identity;
            var endpoint=gate.GetComponent<UnderworldGateEndpoint>()??gate.AddComponent<UnderworldGateEndpoint>();endpoint.Role=UnderworldGateRole.EnterUnderworld;
            var config=new LocationConfig{Biome=ZoneManager.AnyBiomeOf(Heightmap.Biome.Meadows,Heightmap.Biome.BlackForest,Heightmap.Biome.Swamp,Heightmap.Biome.Mountain,Heightmap.Biome.Plains,Heightmap.Biome.Mistlands),BiomeArea=Heightmap.BiomeArea.Everything,Quantity=6,Priotized=true,ExteriorRadius=38f,MinAltitude=8f,MinTerrainDelta=0f,MaxTerrainDelta=8f,MinDistanceFromSimilar=3500f,Group="Magenheim_UnderworldAccess",ClearArea=true,RandomRotation=true,HasInterior=false};
            var custom=new CustomLocation(container,fixReference:false,config);if(!ZoneManager.Instance.AddCustomLocation(custom))throw new InvalidOperationException($"Jotunn refused additive Deep Gate location registration for '{LocationName}'.");
            _registered=true;_log.LogInfo($"Registered persistent surface Deep Gate sites '{LocationName}' as paired Underworld entry endpoints.");
        }
        catch(Exception exception){_log.LogError($"Underworld Deep Gate location registration failed: {exception}");throw;}finally{Dispose();}
    }
}
